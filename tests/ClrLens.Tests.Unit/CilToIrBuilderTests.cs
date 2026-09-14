using System.Reflection.Emit;
using ClrLens.IL;
using ClrLens.IR;

namespace ClrLens.Tests.Unit;

public sealed class CilToIrBuilderTests
{
    [Fact]
    public void Build_MapsCoreOperationsAndPreservesOffsets()
    {
        var body = new DecodedMethodBody(
            1,
            8,
            true,
            [
                new CilInstruction(0, OpCodes.Ldc_I4_1, null, 1, []),
                new CilInstruction(1, OpCodes.Stloc_0, null, 1, []),
                new CilInstruction(2, OpCodes.Ldloc_0, null, 1, []),
                new CilInstruction(3, OpCodes.Ldc_I4_2, null, 1, []),
                new CilInstruction(4, OpCodes.Add, null, 1, []),
                new CilInstruction(5, OpCodes.Ret, null, 1, [])
            ],
            [],
            []);

        var ir = new CilToIrBuilder().Build(body);

        Assert.Collection(ir.Nodes,
            n => Assert.Equal(IrOperationKind.Constant, n.Operation),
            n => Assert.Equal(IrOperationKind.StoreLocal, n.Operation),
            n => Assert.Equal(IrOperationKind.LoadLocal, n.Operation),
            n => Assert.Equal(IrOperationKind.Constant, n.Operation),
            n => Assert.Equal(IrOperationKind.Binary, n.Operation),
            n => Assert.Equal(IrOperationKind.Return, n.Operation));
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, ir.Nodes.Select(n => n.Offset));
        Assert.True(ir.LocalValues.ContainsKey(0));
        Assert.DoesNotContain(ir.Diagnostics, d => d.IsError);
    }

    [Fact]
    public void Build_ProducesDiagnosticsForSignatureAwareCallsAndMarksAllocation()
    {
        var body = new DecodedMethodBody(
            1,
            8,
            true,
            [
                new CilInstruction(0, OpCodes.Newobj, 123, 5, []),
                new CilInstruction(5, OpCodes.Box, 456, 5, []),
                new CilInstruction(10, OpCodes.Call, 789, 5, []),
                new CilInstruction(15, OpCodes.Pop, null, 1, []),
                new CilInstruction(16, OpCodes.Ret, null, 1, [])
            ],
            [],
            []);

        var ir = new CilToIrBuilder().Build(body);

        Assert.Contains(ir.Nodes, n => n.Operation == IrOperationKind.Allocate && n.CanAllocate);
        Assert.Contains(ir.Nodes, n => n.Operation == IrOperationKind.Box);
        Assert.Contains(ir.Diagnostics, d => d.Message.Contains("Signature-aware stack behavior", StringComparison.Ordinal));
    }
}
