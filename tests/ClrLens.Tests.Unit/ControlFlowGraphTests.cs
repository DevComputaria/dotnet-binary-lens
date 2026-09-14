using ClrLens.Analysis;

namespace ClrLens.Tests.Unit;

public sealed class ControlFlowGraphTests
{
    [Fact]
    public void Build_CreatesBlocksAndValidEdges()
    {
        var decoded = TestSupport.DecodeFixtureMethod("ExceptionFlow");

        var cfg = new ControlFlowGraphBuilder().Build(decoded);

        Assert.NotEmpty(cfg.Blocks);
        Assert.Contains(cfg.Blocks, b => b.IsEntry);
        Assert.All(cfg.Edges, edge =>
        {
            Assert.Contains(cfg.Blocks, block => block.Id == edge.From);
            Assert.Contains(cfg.Blocks, block => block.Id == edge.To);
        });
        Assert.NotEmpty(cfg.ExceptionalEdges);
    }
}
