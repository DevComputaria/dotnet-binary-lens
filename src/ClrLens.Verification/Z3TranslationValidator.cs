using Microsoft.Z3;
using ClrLens.Rewriter;

namespace ClrLens.Verification;

public sealed record TranslationValidationRequest(
    string Rule,
    string Method,
    IReadOnlyList<int> Offsets,
    IReadOnlyList<string> Preconditions,
    BitVectorExpression Original,
    BitVectorExpression Rewritten,
    TimeSpan Timeout);

public interface ISemanticProver
{
    TransformationProof Prove(TranslationValidationRequest request, CancellationToken cancellationToken = default);
}

public sealed class Z3TranslationValidator : ISemanticProver
{
    public TransformationProof Prove(TranslationValidationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Original.Width != request.Rewritten.Width)
            return Unknown(request, "Original and rewritten expressions have different bit widths.");
        if (request.Timeout <= TimeSpan.Zero)
            return Unknown(request, "Solver timeout elapsed before validation started.");
        if (cancellationToken.IsCancellationRequested)
            return Unknown(request, "Translation validation was cancelled.");

        try
        {
            using var context = new Context();
            using var solver = context.MkSolver();
            using var parameters = context.MkParams();
            parameters.Add("timeout", (uint)Math.Min(request.Timeout.TotalMilliseconds, uint.MaxValue));
            solver.Parameters = parameters;

            var variables = new Dictionary<string, BitVecExpr>(StringComparer.Ordinal);
            var original = Translate(context, request.Original, variables);
            var rewritten = Translate(context, request.Rewritten, variables);
            var exceptionMismatch = context.MkNot(context.MkEq(original.Exceptional, rewritten.Exceptional));
            var resultMismatch = context.MkAnd(
                context.MkNot(original.Exceptional),
                context.MkNot(rewritten.Exceptional),
                context.MkNot(context.MkEq(original.Value, rewritten.Value)));
            solver.Assert(context.MkOr(exceptionMismatch, resultMismatch));

            var status = solver.Check();
            if (status == Status.UNSATISFIABLE)
                return new TransformationProof(request.Rule, request.Method, request.Offsets, request.Preconditions,
                    ProofStatus.Proven, $"Z3 proved equivalence using {request.Original.Width}-bit vectors.");
            if (status == Status.UNKNOWN)
                return Unknown(request, $"Z3 returned UNKNOWN: {solver.ReasonUnknown}.");

            using var model = solver.Model;
            var inputs = variables.ToDictionary(
                item => item.Key,
                item => model.Evaluate(item.Value, true).ToString(),
                StringComparer.Ordinal);
            var originalResult = Outcome(model, original);
            var rewrittenResult = Outcome(model, rewritten);
            var counterexample = new Counterexample(inputs, originalResult, rewrittenResult, "The original and rewritten bit-vector results differ.");
            return new TransformationProof(request.Rule, request.Method, request.Offsets, request.Preconditions,
                ProofStatus.Counterexample, "Z3 found a divergent input.", counterexample);
        }
        catch (Z3Exception exception)
        {
            return Unknown(request, $"Z3 could not validate the transformation: {exception.Message}");
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException)
        {
            return Unknown(request, $"Z3 native backend is unavailable: {exception.Message}");
        }
    }

    private static SymbolicValue Translate(Context context, BitVectorExpression expression, Dictionary<string, BitVecExpr> variables) => expression switch
    {
        BitVectorVariable variable => new(GetVariable(context, variable, variables), context.MkFalse()),
        BitVectorConstant constant => new(context.MkBV(constant.Value, (uint)constant.Width), context.MkFalse()),
        BitVectorNot unary => TranslateNot(context, unary, variables),
        BitVectorBinary binary => TranslateBinary(context, binary, variables),
        _ => throw new Z3Exception($"Unsupported symbolic expression {expression.GetType().Name}.")
    };

    private static SymbolicValue TranslateNot(Context context, BitVectorNot unary, Dictionary<string, BitVecExpr> variables)
    {
        var operand = Translate(context, unary.Operand, variables);
        return new SymbolicValue(context.MkBVNot(operand.Value), operand.Exceptional);
    }

    private static BitVecExpr GetVariable(Context context, BitVectorVariable variable, Dictionary<string, BitVecExpr> variables)
    {
        if (variables.TryGetValue(variable.Name, out var existing))
        {
            if (existing.SortSize != variable.Width) throw new Z3Exception($"Variable {variable.Name} is used with inconsistent widths.");
            return existing;
        }
        return variables[variable.Name] = context.MkBVConst(variable.Name, (uint)variable.Width);
    }

    private static SymbolicValue TranslateBinary(Context context, BitVectorBinary binary, Dictionary<string, BitVecExpr> variables)
    {
        var left = Translate(context, binary.Left, variables);
        var right = Translate(context, binary.Right, variables);
        var value = binary.Operation switch
        {
            BitVectorOperation.Add or BitVectorOperation.CheckedSignedAdd or BitVectorOperation.CheckedUnsignedAdd => context.MkBVAdd(left.Value, right.Value),
            BitVectorOperation.Subtract or BitVectorOperation.CheckedSignedSubtract or BitVectorOperation.CheckedUnsignedSubtract => context.MkBVSub(left.Value, right.Value),
            BitVectorOperation.Multiply or BitVectorOperation.CheckedSignedMultiply or BitVectorOperation.CheckedUnsignedMultiply => context.MkBVMul(left.Value, right.Value),
            BitVectorOperation.SignedDivide => context.MkBVSDiv(left.Value, right.Value),
            BitVectorOperation.UnsignedDivide => context.MkBVUDiv(left.Value, right.Value),
            BitVectorOperation.SignedRemainder => context.MkBVSRem(left.Value, right.Value),
            BitVectorOperation.UnsignedRemainder => context.MkBVURem(left.Value, right.Value),
            BitVectorOperation.And => context.MkBVAND(left.Value, right.Value),
            BitVectorOperation.Or => context.MkBVOR(left.Value, right.Value),
            BitVectorOperation.Xor => context.MkBVXOR(left.Value, right.Value),
            BitVectorOperation.ShiftLeft => context.MkBVSHL(left.Value, right.Value),
            BitVectorOperation.ArithmeticShiftRight => context.MkBVASHR(left.Value, right.Value),
            BitVectorOperation.LogicalShiftRight => context.MkBVLSHR(left.Value, right.Value),
            _ => throw new Z3Exception($"Unsupported bit-vector operation {binary.Operation}.")
        };
        var localException = OperationException(context, binary.Operation, left.Value, right.Value, value);
        return new SymbolicValue(value, context.MkOr(left.Exceptional, right.Exceptional, localException));
    }

    private static BoolExpr OperationException(Context context, BitVectorOperation operation, BitVecExpr left, BitVecExpr right, BitVecExpr result)
    {
        var width = left.SortSize;
        var zero = context.MkBV(0, width);
        var divideByZero = context.MkEq(right, zero);
        var signedDivisionOverflow = context.MkAnd(
            context.MkEq(left, context.MkBV(1UL << ((int)width - 1), width)),
            context.MkEq(right, context.MkBV(ulong.MaxValue, width)));
        return operation switch
        {
            BitVectorOperation.SignedDivide or BitVectorOperation.SignedRemainder => context.MkOr(divideByZero, signedDivisionOverflow),
            BitVectorOperation.UnsignedDivide or BitVectorOperation.UnsignedRemainder => divideByZero,
            BitVectorOperation.CheckedSignedAdd => SignedAddOverflow(context, left, right, result),
            BitVectorOperation.CheckedUnsignedAdd => context.MkBVULT(result, left),
            BitVectorOperation.CheckedSignedSubtract => SignedSubtractOverflow(context, left, right, result),
            BitVectorOperation.CheckedUnsignedSubtract => context.MkBVULT(left, right),
            BitVectorOperation.CheckedSignedMultiply => MultiplyOverflow(context, left, right, true),
            BitVectorOperation.CheckedUnsignedMultiply => MultiplyOverflow(context, left, right, false),
            _ => context.MkFalse()
        };
    }

    private static BoolExpr SignedAddOverflow(Context context, BitVecExpr left, BitVecExpr right, BitVecExpr result)
    {
        var bit = left.SortSize - 1;
        var leftSign = context.MkExtract(bit, bit, left);
        var rightSign = context.MkExtract(bit, bit, right);
        var resultSign = context.MkExtract(bit, bit, result);
        return context.MkAnd(context.MkEq(leftSign, rightSign), context.MkNot(context.MkEq(leftSign, resultSign)));
    }

    private static BoolExpr SignedSubtractOverflow(Context context, BitVecExpr left, BitVecExpr right, BitVecExpr result)
    {
        var bit = left.SortSize - 1;
        var leftSign = context.MkExtract(bit, bit, left);
        var rightSign = context.MkExtract(bit, bit, right);
        var resultSign = context.MkExtract(bit, bit, result);
        return context.MkAnd(context.MkNot(context.MkEq(leftSign, rightSign)), context.MkNot(context.MkEq(leftSign, resultSign)));
    }

    private static BoolExpr MultiplyOverflow(Context context, BitVecExpr left, BitVecExpr right, bool signed)
    {
        var width = left.SortSize;
        var extendedLeft = signed ? context.MkSignExt(width, left) : context.MkZeroExt(width, left);
        var extendedRight = signed ? context.MkSignExt(width, right) : context.MkZeroExt(width, right);
        var wideResult = context.MkBVMul(extendedLeft, extendedRight);
        var low = context.MkExtract(width - 1, 0, wideResult);
        var roundTrip = signed ? context.MkSignExt(width, low) : context.MkZeroExt(width, low);
        return context.MkNot(context.MkEq(wideResult, roundTrip));
    }

    private static string Outcome(Model model, SymbolicValue value) =>
        model.Evaluate(value.Exceptional, true).IsTrue ? "throw:ArithmeticException" : model.Evaluate(value.Value, true).ToString();

    private static TransformationProof Unknown(TranslationValidationRequest request, string detail) =>
        new(request.Rule, request.Method, request.Offsets, request.Preconditions, ProofStatus.Unknown, detail);

    private readonly record struct SymbolicValue(BitVecExpr Value, BoolExpr Exceptional);
}

public sealed class TranslationValidationPipeline
{
    private readonly ISemanticProver prover;

    public TranslationValidationPipeline(ISemanticProver prover) => this.prover = prover ?? throw new ArgumentNullException(nameof(prover));

    public RewriteProof ValidateAndAttach(RewriteProof proof, IEnumerable<TranslationValidationRequest> requests, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proof);
        ArgumentNullException.ThrowIfNull(requests);
        var validations = requests.Select(request => prover.Prove(request, cancellationToken)).ToArray();
        return proof with { Transformations = proof.Transformations.Concat(validations).ToArray() };
    }
}
