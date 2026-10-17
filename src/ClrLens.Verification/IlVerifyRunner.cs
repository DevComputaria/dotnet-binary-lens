using System.Diagnostics;

namespace ClrLens.Verification;

public sealed record IlVerifyOptions
{
    public string ExecutablePath { get; init; } = "ilverify";
    public IReadOnlyList<string> ReferencePaths { get; init; } = [];
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed record IlVerificationResult(bool Succeeded, bool TimedOut, int? ExitCode, string Output);

public interface IIlVerifier
{
    Task<IlVerificationResult> VerifyAsync(string assemblyPath, IlVerifyOptions? options = null, CancellationToken cancellationToken = default);
}

public sealed class IlVerifyRunner : IIlVerifier
{
    public async Task<IlVerificationResult> VerifyAsync(string assemblyPath, IlVerifyOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        options ??= new IlVerifyOptions();
        if (!File.Exists(assemblyPath)) throw new FileNotFoundException("Assembly to verify was not found.", assemblyPath);

        var startInfo = new ProcessStartInfo(options.ExecutablePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var reference in ResolveReferences(assemblyPath, options.ReferencePaths))
        {
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add(reference);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start()) return new IlVerificationResult(false, false, null, "ILVerify did not start.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new IlVerificationResult(false, false, null, $"ILVerify could not be started: {exception.Message}");
        }

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            return new IlVerificationResult(false, true, null, "ILVerify timed out.");
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        var output = string.Join(Environment.NewLine, new[] { await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false) }.Where(text => !string.IsNullOrWhiteSpace(text)));
        return new IlVerificationResult(process.ExitCode == 0, false, process.ExitCode, output);
    }

    public static IReadOnlyList<string> ResolveReferences(string assemblyPath, IEnumerable<string> configured)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in configured)
            if (File.Exists(path)) paths.TryAdd(Path.GetFileName(path), Path.GetFullPath(path));
        var assemblyDirectory = Path.GetDirectoryName(Path.GetFullPath(assemblyPath));
        if (assemblyDirectory is not null)
            foreach (var path in Directory.EnumerateFiles(assemblyDirectory, "*.dll")) paths.TryAdd(Path.GetFileName(path), path);
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDirectory is not null)
            foreach (var path in Directory.EnumerateFiles(runtimeDirectory, "*.dll")) paths.TryAdd(Path.GetFileName(path), path);
        paths.Remove(Path.GetFileName(assemblyPath));
        return paths.Values.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
