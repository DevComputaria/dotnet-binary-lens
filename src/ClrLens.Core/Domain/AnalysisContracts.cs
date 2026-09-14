namespace ClrLens.Core.Domain;

public enum EvidenceKind
{
    Proven,
    ProvenUnderAssumptions,
    StaticUpperBound,
    StaticLowerBound,
    Inferred,
    Symbolic,
    Estimated,
    Observed,
    Heuristic,
    ExternalContract,
    Unknown
}

public enum RemediationType
{
    AutoFix,
    Review,
    SourceChange,
    Informational
}

public enum Severity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum AnalysisCostMode
{
    Typical,
    Pessimistic,
    Observed
}

public enum AnalysisScenario
{
    Baseline,
    Stress,
    Untrusted
}

public enum ContractTrust
{
    Code,
    LibrarySummary,
    External,
    Runtime
}

public sealed record AssemblyIdentity(
    string Name,
    string Sha256,
    string TargetFramework,
    string Architecture,
    string? Version = null,
    bool HasPdb = false,
    bool IsStrongNamed = false,
    bool IsReadyToRun = false);

public sealed record AnalysisLocation(
    string? TypeName,
    string? MethodName,
    int? MetadataToken,
    int? IlStart,
    int? IlEnd,
    IReadOnlyList<string> BasicBlockIds,
    IReadOnlyList<string> CallPath);

public sealed record AnalysisAssumption(
    string Name,
    string Value,
    string Source,
    EvidenceKind Evidence);

public sealed record Provenance(
    string ToolVersion,
    string RuntimeModel,
    DateTimeOffset AnalysisDate,
    string InputSha256,
    string? SourceDocument = null);

public sealed record CostModel(
    string? Formula,
    string? Complexity,
    string? Unit,
    IReadOnlyDictionary<string, string> Parameters);

public sealed record Finding(
    string Id,
    string Category,
    Severity Severity,
    double Confidence,
    AnalysisLocation Location,
    EvidenceKind Evidence,
    RemediationType Remediation,
    string Title,
    string Description,
    string WhyThisMatters,
    CostModel? Model,
    IReadOnlyList<AnalysisAssumption> Assumptions,
    IReadOnlyList<string> Recommendations,
    bool Suppressed = false,
    string? SuppressionId = null);

public sealed record AnalysisScenarioConfiguration(
    AnalysisScenario Scenario,
    AnalysisCostMode CostMode,
    int? ExpectedConcurrency = null,
    long? ContainerMemoryBytes = null,
    long LohThresholdBytes = 85_000);

public sealed record BoundContract(
    string Symbol,
    decimal Max,
    string Unit,
    string Source,
    string Scope,
    ContractTrust Trust,
    DateOnly? Expires = null,
    string? Owner = null,
    string? Ticket = null,
    EvidenceKind Evidence = EvidenceKind.ExternalContract);

public sealed record Suppression(
    string Id,
    string Target,
    string Reason,
    string Owner,
    string Scope,
    DateOnly Expires,
    string? Ticket = null);

public sealed record AnalysisSummary(
    int Methods,
    int Instructions,
    int Loops,
    int AllocationSites,
    double CpuRisk,
    double MemoryRisk,
    int FindingCount);

public sealed record AnalysisReport(
    string SchemaVersion,
    AssemblyIdentity Assembly,
    Provenance Provenance,
    AnalysisScenarioConfiguration Configuration,
    AnalysisSummary Summary,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<BoundContract> Contracts,
    IReadOnlyList<Suppression> Suppressions);
