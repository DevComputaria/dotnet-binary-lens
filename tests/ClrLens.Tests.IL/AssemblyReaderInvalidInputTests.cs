using ClrLens.PE;

namespace ClrLens.Tests.IL;

public sealed class AssemblyReaderInvalidInputTests
{
    [Fact]
    public async Task ReadAsync_MissingFileReportsFileNotFound()
    {
        var result = await new AssemblyReader().ReadAsync(Path.Combine(TestSupport.RepoRoot, "does-not-exist.dll"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, d => d.Code == AssemblyDiagnosticCode.FileNotFound);
    }

    [Fact]
    public async Task ReadAsync_InvalidMetadataReportsInvalidPeOrMissingMetadata()
    {
        var result = await new AssemblyReader().ReadAsync(TestSupport.InvalidMetadataFixturePath);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, d => d.Code is AssemblyDiagnosticCode.InvalidPe or AssemblyDiagnosticCode.MissingMetadata or AssemblyDiagnosticCode.MetadataReadFailure);
    }

    [Fact]
    public async Task ReadAsync_FileTooLargeAndTimeoutAreReported()
    {
        var reader = new AssemblyReader();

        var tooLarge = await reader.ReadAsync(TestSupport.FixtureAssemblyPath, new AssemblyIngestionOptions(MaxFileSizeBytes: 1));
        Assert.Contains(tooLarge.Diagnostics, d => d.Code == AssemblyDiagnosticCode.FileTooLarge);

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var timeout = await reader.ReadAsync(TestSupport.FixtureAssemblyPath, cancellationToken: cancelled.Token);
        Assert.Contains(timeout.Diagnostics, d => d.Code == AssemblyDiagnosticCode.Timeout);
    }
}
