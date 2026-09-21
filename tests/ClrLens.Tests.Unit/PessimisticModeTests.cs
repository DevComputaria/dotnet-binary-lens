using ClrLens.Analysis;
using ClrLens.Core.Domain;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class PessimisticModeTests
{
    [Fact]
    public void UnknownExternalPayloadRemainsSymbolicInPessimisticMode()
    {
        var value = new ExternalInputBound(
            InputBoundKind.UnknownExternal,
            "payloadBytes",
            "ReadToEnd",
            "Unknown external payload length");

        var worstCase = PessimisticAnalysis.WorstCaseUpperBound(value, AnalysisScenario.Untrusted, AnalysisCostMode.Pessimistic);

        Assert.Equal(InputBoundKind.UnknownExternal, value.Kind);
        Assert.Equal("UnknownExternal", worstCase.Symbol);
        Assert.Equal(AnalysisScenario.Untrusted, worstCase.Scenario);
    }

    [Fact]
    public void MaterializationPatternsAreDetectedAsUnboundedIOMaterialization()
    {
        Assert.True(MaterializationClassifier.IsMaterialization("ReadToEnd"));
        Assert.True(MaterializationClassifier.IsMaterialization("ToArray"));
        Assert.True(MaterializationClassifier.IsMaterialization("ToList"));
        Assert.False(MaterializationClassifier.IsMaterialization("Contains"));
    }

    [Fact]
    public void ScenarioConfigurationsRemainIndependent()
    {
        var baseline = new AnalysisScenarioConfiguration(AnalysisScenario.Baseline, AnalysisCostMode.Pessimistic, ExpectedConcurrency: 8, ContainerMemoryBytes: 2 * 1024 * 1024, LohThresholdBytes: 85_000);
        var stress = new AnalysisScenarioConfiguration(AnalysisScenario.Stress, AnalysisCostMode.Pessimistic, ExpectedConcurrency: 32, ContainerMemoryBytes: 32 * 1024 * 1024, LohThresholdBytes: 85_000);
        var untrusted = new AnalysisScenarioConfiguration(AnalysisScenario.Untrusted, AnalysisCostMode.Pessimistic, ExpectedConcurrency: 1, ContainerMemoryBytes: null, LohThresholdBytes: 85_000);

        Assert.NotEqual(baseline.ExpectedConcurrency, stress.ExpectedConcurrency);
        Assert.Null(untrusted.ContainerMemoryBytes);
        Assert.Equal(AnalysisScenario.Untrusted, untrusted.Scenario);
    }
}
