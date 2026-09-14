using System.Reflection;
using System.Reflection.Emit;
using ClrLens.IL;

namespace ClrLens.Tests.IL;

public sealed class CilStackValidationTests
{
    [Fact]
    public void ValidateBranchTargets_ReportsInvalidBranchTarget()
    {
        var method = typeof(CilDecoder).GetMethod("ValidateBranchTargets", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("CilDecoder.ValidateBranchTargets");

        var instructions = new[]
        {
            new CilInstruction(0, OpCodes.Br_S, 99, 2, [99]),
            new CilInstruction(2, OpCodes.Ret, null, 1, [])
        };
        var diagnostics = new List<CilDiagnostic>();

        method.Invoke(null, [instructions, instructions.Select(i => i.Offset).ToHashSet(), diagnostics]);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(CilDiagnosticCode.InvalidBranchTarget, diagnostic.Code);
        Assert.Contains("Branch target", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateStack_ReportsUnderflowHeightMismatchAndVariableBehavior()
    {
        var method = typeof(CilDecoder).GetMethod("ValidateStack", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("CilDecoder.ValidateStack");

        var mismatchInstructions = new[]
        {
            new CilInstruction(0, OpCodes.Ldc_I4_0, null, 1, []),
            new CilInstruction(1, OpCodes.Brtrue_S, 5, 2, [5]),
            new CilInstruction(3, OpCodes.Ldc_I4_1, null, 1, []),
            new CilInstruction(4, OpCodes.Br_S, 5, 2, [5]),
            new CilInstruction(5, OpCodes.Pop, null, 1, []),
            new CilInstruction(6, OpCodes.Ret, null, 1, [])
        };
        var variableInstructions = new[]
        {
            new CilInstruction(0, OpCodes.Call, 0x06000001, 5, []),
            new CilInstruction(5, OpCodes.Ret, null, 1, [])
        };
        var diagnostics = new List<CilDiagnostic>();

        method.Invoke(null, [mismatchInstructions, diagnostics]);
        method.Invoke(null, [variableInstructions, diagnostics]);

        Assert.Contains(diagnostics, d => d.Code == CilDiagnosticCode.StackUnderflow);
        Assert.Contains(diagnostics, d => d.Code == CilDiagnosticCode.StackHeightMismatch);
        Assert.Contains(diagnostics, d => d.Code == CilDiagnosticCode.VariableStackBehavior);
    }
}
