namespace ClrLens.Tests.IL;

public sealed class HarnessIntegrationTests
{
    [Fact]
    public async Task RunAsync_Passes()
    {
        var result = await FixtureHarness.RunAsync();

        Assert.Contains("PASS", result, StringComparison.Ordinal);
    }
}
