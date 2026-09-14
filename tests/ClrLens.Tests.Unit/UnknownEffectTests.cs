using System.Reflection;
using System.Reflection.Emit;
using ClrLens.Analysis;

namespace ClrLens.Tests.Unit;

public sealed class UnknownEffectTests
{
    [Fact]
    public void EffectsFor_NewObjIncludesAllocation()
    {
        var effects = InvokeEffectsFor(OpCodes.Newobj, ".ctor");

        Assert.True(effects.HasFlag(UnknownEffect.MayAllocate));
        Assert.True(effects.HasFlag(UnknownEffect.MayThrow));
    }

    [Fact]
    public void EffectsFor_WaitAndRunAddSpecificFlags()
    {
        var wait = InvokeEffectsFor(OpCodes.Call, "Wait");
        var run = InvokeEffectsFor(OpCodes.Call, "Run");
        var unknown = InvokeEffectsFor(OpCodes.Calli, null);

        Assert.True(wait.HasFlag(UnknownEffect.MayBlock));
        Assert.True(run.HasFlag(UnknownEffect.MaySpawnWork));
        Assert.True(unknown.HasFlag(UnknownEffect.MayRetainArguments));
    }

    private static UnknownEffect InvokeEffectsFor(OpCode opCode, string? methodName)
    {
        var method = typeof(CallGraphBuilder).GetMethod("EffectsFor", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("CallGraphBuilder.EffectsFor");
        return (UnknownEffect)method.Invoke(null, [opCode, methodName])!;
    }
}
