using System.Reflection.Emit;
using ClrLens.IL;
using ClrLens.IR;

namespace ClrLens.Tests.Unit;

public sealed class IrModelTests
{
    [Fact]
    public void MethodIr_PreservesExceptionRegions()
    {
        var region = new CilExceptionRegion(System.Reflection.Metadata.ExceptionRegionKind.Catch, 0, 4, 4, 4, 0, default);
        var methodIr = new MethodIr([], new Dictionary<int, ValueId>(), [], [region]);

        Assert.Single(methodIr.ExceptionRegions);
        Assert.Equal(region, methodIr.ExceptionRegions[0]);
    }

    [Fact]
    public void IrNode_PreservesOffsetOperationAndEffects()
    {
        var node = new IrNode(12, IrOperationKind.Allocate, new ValueId(1), [new ValueId(0)], 123, "System.Object", OpCodes.Newobj, true, true, true);

        Assert.Equal(12, node.Offset);
        Assert.Equal(IrOperationKind.Allocate, node.Operation);
        Assert.True(node.CanThrow);
        Assert.True(node.HasSideEffect);
        Assert.True(node.CanAllocate);
    }
}
