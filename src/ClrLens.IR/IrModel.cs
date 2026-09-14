using System.Reflection.Emit;
using ClrLens.IL;

namespace ClrLens.IR;

public readonly record struct ValueId(int Value);

public enum IrOperationKind
{
    Unknown,
    Constant,
    LoadArgument,
    LoadLocal,
    StoreLocal,
    Binary,
    Unary,
    Call,
    Allocate,
    Box,
    LoadField,
    StoreField,
    Branch,
    ConditionalBranch,
    Switch,
    Return,
    Throw,
    StackDiscard
}

public sealed record IrNode(
    int Offset,
    IrOperationKind Operation,
    ValueId? Result,
    IReadOnlyList<ValueId> Inputs,
    object? Operand,
    string? ResultType,
    OpCode OriginalOpCode,
    bool CanThrow,
    bool HasSideEffect,
    bool CanAllocate);

public sealed record IrDiagnostic(
    int Offset,
    string Message,
    bool IsError = false);

public sealed record MethodIr(
    IReadOnlyList<IrNode> Nodes,
    IReadOnlyDictionary<int, ValueId> LocalValues,
    IReadOnlyList<IrDiagnostic> Diagnostics,
    IReadOnlyList<CilExceptionRegion> ExceptionRegions)
{
    public bool IsValid => Diagnostics.All(diagnostic => !diagnostic.IsError);
}
