using System.Reflection.Metadata;

namespace ClrLens.Tests.IL;

public sealed class CilExceptionRegionTests
{
    [Fact]
    public void Decode_PreservesExceptionRegions()
    {
        var decoded = TestSupport.DecodeFixtureMethod("ExceptionFlow");

        Assert.NotEmpty(decoded.ExceptionRegions);
        Assert.Contains(decoded.ExceptionRegions, r => r.Kind == ExceptionRegionKind.Catch);
        Assert.Contains(decoded.ExceptionRegions, r => r.Kind == ExceptionRegionKind.Finally);
        Assert.All(decoded.ExceptionRegions, region =>
        {
            Assert.True(region.TryLength > 0);
            Assert.True(region.HandlerLength > 0);
        });
    }
}
