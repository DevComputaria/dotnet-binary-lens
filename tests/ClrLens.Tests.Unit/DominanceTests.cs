using ClrLens.Analysis;

namespace ClrLens.Tests.Unit;

public sealed class DominanceTests
{
    [Fact]
    public void Build_ComputesDominatorsAndPostDominators()
    {
        var decoded = TestSupport.DecodeFixtureMethod("LinearLoop");

        var cfg = new ControlFlowGraphBuilder().Build(decoded);
        var entry = Assert.Single(cfg.Blocks, b => b.IsEntry);

        Assert.Contains(entry.Id, cfg.Dominators[entry.Id]);
        Assert.All(cfg.Blocks, block => Assert.Contains(block.Id, cfg.Dominators[block.Id]));
        Assert.All(cfg.Blocks.Where(b => b.IsExit), block => Assert.Equal(new[] { block.Id }, cfg.PostDominators[block.Id]));
    }
}
