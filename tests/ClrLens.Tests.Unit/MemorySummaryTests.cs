using ClrLens.Analysis;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class MemorySummaryTests
{
    [Fact]
    public void AllocationVolumeIsSeparatedFromLiveAndRetainedMemory()
    {
        var summary = new MemorySummary(
            "v1",
            AllocationVolumeBytes: 128_000,
            LiveManagedBytes: 32_000,
            RetainedBytes: 96_000,
            PeakWorkingSetEstimateBytes: 140_000,
            NativeMemoryBytes: 12_000,
            LargeObjectHeapBytes: 96_000,
            StaticCacheRootPaths: ["StaticCache:RetentionPatterns"],
            LohThresholdBytes: 85_000,
            ConcurrencyFactor: 4,
            SummaryId: "mem:v1");

        Assert.Equal(128_000, summary.AllocationVolumeBytes);
        Assert.Equal(32_000, summary.LiveManagedBytes);
        Assert.Equal(96_000, summary.RetainedBytes);
        Assert.NotEqual(summary.AllocationVolumeBytes, summary.LiveManagedBytes);
        Assert.NotEqual(summary.AllocationVolumeBytes, summary.RetainedBytes);
    }

    [Fact]
    public void StaticCacheShowsTheGcRootPath()
    {
        var heap = new AbstractHeap(
            [
                new AbstractHeapObject("cache-1", 64_000, EscapeState.GlobalEscape, "StaticCache:RetentionPatterns", true),
                new AbstractHeapObject("temp-1", 8_000, EscapeState.MethodEscape, "MethodScope:Compute", false)
            ]);

        var summary = MemorySummaryAnalyzer.Analyze(heap, lohThresholdBytes: 85_000, concurrencyFactor: 2);

        Assert.Contains("StaticCache:RetentionPatterns", summary.StaticCacheRootPaths);
        Assert.Single(summary.StaticCacheRootPaths);
    }

    [Fact]
    public void LohThresholdIsConfigurableAndVersionedInFindingModel()
    {
        var heap = new AbstractHeap(
            [
                new AbstractHeapObject("loh-1", 90_000, EscapeState.ThreadEscape, "ThreadLocal:LargeBuffer", false)
            ]);

        var summary = MemorySummaryAnalyzer.Analyze(heap, lohThresholdBytes: 85_000, concurrencyFactor: 1);
        var model = summary.ToCostModel();

        Assert.True(summary.IsLargeObjectHeap); 
        Assert.Equal(85_000, summary.LohThresholdBytes);
        Assert.Equal("v1", summary.Version);
        Assert.Equal("v1", model.Parameters["summaryVersion"]);
        Assert.Equal("mem:v1", model.Parameters["summaryId"]);
    }
}
