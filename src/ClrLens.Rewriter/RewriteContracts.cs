using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClrLens.Rewriter;

public enum RewriteRule
{
    ConstantFolding,
    ConstantPropagation,
    BranchFolding,
    NopRemoval,
    UnreachableCodeRemoval,
    RedundantLocalCopyRemoval
}

public enum ProofStatus
{
    Proven,
    Counterexample,
    Unknown
}

public enum StrongNameStatus
{
    NotSigned,
    Blocked,
    Resigned
}

public enum ReadyToRunStatus
{
    NotReadyToRun,
    Blocked
}

public sealed record IlDiff(
    string Method,
    RewriteRule Rule,
    IReadOnlyList<int> OriginalOffsets,
    string Before,
    string After);

public sealed record Counterexample(
    IReadOnlyDictionary<string, string> Inputs,
    string OriginalResult,
    string RewrittenResult,
    string Reason);

public sealed record TransformationProof(
    string Rule,
    string Method,
    IReadOnlyList<int> Offsets,
    IReadOnlyList<string> Preconditions,
    ProofStatus Status,
    string? Detail = null,
    Counterexample? Counterexample = null);

public sealed record RewriteProof(
    string InputSha256,
    string? OutputSha256,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TransformationProof> Transformations,
    StrongNameStatus StrongName,
    ReadyToRunStatus ReadyToRun,
    bool UnknownEffectsBlocked,
    IReadOnlyList<string> Diagnostics)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void WriteJson(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(this, SerializerOptions);
        File.WriteAllText(path, json);
    }
}

public sealed record RewriteOptions
{
    public IReadOnlySet<RewriteRule> Rules { get; init; } = new HashSet<RewriteRule>(Enum.GetValues<RewriteRule>());
    public bool HasUnknownEffects { get; init; }
    public byte[]? StrongNameKeyBlob { get; init; }
    public bool ReadSymbols { get; init; } = true;
}

public sealed record RewriteResult(
    bool Succeeded,
    string? OutputPath,
    IReadOnlyList<IlDiff> Diff,
    RewriteProof Proof);

public interface IAssemblyWriter
{
    RewriteResult Rewrite(string inputPath, string outputPath, RewriteOptions? options = null);
}
