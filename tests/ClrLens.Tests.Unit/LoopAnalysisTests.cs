using System.Reflection.Emit;
using ClrLens.Analysis;
using ClrLens.IL;

namespace ClrLens.Tests.Unit;

public sealed class LoopAnalysisTests
{
    [Fact]
    public void Build_DetectsNaturalLoopsForFixtureMethod()
    {
        var decoded = TestSupport.DecodeFixtureMethod("NestedLoop");

        var cfg = new ControlFlowGraphBuilder().Build(decoded);

        Assert.NotEmpty(cfg.StronglyConnectedComponents);
        Assert.NotEmpty(cfg.BackEdges);
        Assert.NotEmpty(cfg.NaturalLoops);
        Assert.All(cfg.NaturalLoops, loop =>
        {
            Assert.Contains(loop.Header, loop.Nodes);
            Assert.Contains(loop.Latch, loop.Nodes);
            Assert.True(loop.Depth >= 1);
        });
    }

    [Fact]
    public void Build_KeepsIrreducibleCycleAsSccWithoutNaturalLoopClassification()
    {
        var body = new DecodedMethodBody(
            1,
            8,
            true,
            [
                new CilInstruction(0, OpCodes.Ldc_I4_0, null, 1, []),
                new CilInstruction(1, OpCodes.Brtrue_S, 10, 2, [10]),
                new CilInstruction(3, OpCodes.Br_S, 20, 2, [20]),
                new CilInstruction(10, OpCodes.Br_S, 30, 2, [30]),
                new CilInstruction(20, OpCodes.Br_S, 30, 2, [30]),
                new CilInstruction(30, OpCodes.Switch, new[] { 10, 20 }, 9, [10, 20]),
                new CilInstruction(40, OpCodes.Ret, null, 1, [])
            ],
            [],
            []);

        var cfg = new ControlFlowGraphBuilder().Build(body);

        Assert.Contains(cfg.StronglyConnectedComponents, scc => scc.SetEquals(new HashSet<int> { 2, 3, 4 }));
        Assert.Empty(cfg.NaturalLoops);
    }
}
