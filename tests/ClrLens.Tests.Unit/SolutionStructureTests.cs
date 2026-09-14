using System.Xml.Linq;

namespace ClrLens.Tests.Unit;

public sealed class SolutionStructureTests
{
    [Fact]
    public void CoreProject_DoesNotReferenceForbiddenModules()
    {
        var coreProject = Path.Combine(TestSupport.RepoRoot, "src", "ClrLens.Core", "ClrLens.Core.csproj");
        var text = File.ReadAllText(coreProject);

        Assert.DoesNotContain("ClrLens.Rewriter", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClrLens.Runtime", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ClrLens.Cli", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DotnetTestProjects_AreDiscoverableInSolution_AndBenchmarksRemainSeparate()
    {
        var solution = File.ReadAllText(Path.Combine(TestSupport.RepoRoot, "ClrLens.sln"));

        Assert.Contains("ClrLens.Tests.Unit", solution, StringComparison.Ordinal);
        Assert.Contains("ClrLens.Tests.IL", solution, StringComparison.Ordinal);
        Assert.Contains("ClrLens.Tests.Optimization", solution, StringComparison.Ordinal);
        Assert.Contains("ClrLens.Tests.Regression", solution, StringComparison.Ordinal);

        var benchmarkProject = XDocument.Load(Path.Combine(TestSupport.RepoRoot, "tests", "ClrLens.Tests.Benchmarks", "ClrLens.Tests.Benchmarks.csproj"));
        Assert.DoesNotContain(
            benchmarkProject.Descendants("PackageReference"),
            p => string.Equals((string?)p.Attribute("Include"), "xunit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SolutionDefaults_UseNet8AndNullableEnabled()
    {
        var props = File.ReadAllText(Path.Combine(TestSupport.RepoRoot, "Directory.Build.props"));
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", props, StringComparison.Ordinal);
        Assert.Contains("<Nullable>enable</Nullable>", props, StringComparison.Ordinal);
    }
}
