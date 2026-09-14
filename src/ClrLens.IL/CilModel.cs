using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace ClrLens.IL;

public enum CilDiagnosticCode
{
    InvalidOpcode,
    TruncatedOperand,
    InvalidBranchTarget,
    StackUnderflow,
    StackHeightMismatch,
    VariableStackBehavior,
    InvalidMethodBody
}

public sealed record CilDiagnostic(
    CilDiagnosticCode Code,
    int Offset,
    string Message);

public sealed record CilInstruction(
    int Offset,
    OpCode OpCode,
    object? Operand,
    int Size,
    IReadOnlyList<int> BranchTargets);

public sealed record CilExceptionRegion(
    ExceptionRegionKind Kind,
    int TryOffset,
    int TryLength,
    int HandlerOffset,
    int HandlerLength,
    int FilterOffset,
    EntityHandle CatchType);

public sealed record DecodedMethodBody(
    int RelativeVirtualAddress,
    int MaxStack,
    bool InitLocals,
    IReadOnlyList<CilInstruction> Instructions,
    IReadOnlyList<CilExceptionRegion> ExceptionRegions,
    IReadOnlyList<CilDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

public sealed record MethodBodyReadResult(
    IReadOnlyDictionary<MethodDefinitionHandle, DecodedMethodBody> Bodies,
    IReadOnlyList<CilDiagnostic> Diagnostics);
