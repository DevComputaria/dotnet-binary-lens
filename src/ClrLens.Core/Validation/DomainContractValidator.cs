using ClrLens.Core.Domain;

namespace ClrLens.Core.Validation;

public static class DomainContractValidator
{
    public static EvidenceKind ClassifyContract(BoundContract contract, bool provenLocally)
    {
        ArgumentNullException.ThrowIfNull(contract);

        if (contract.Trust == ContractTrust.External)
            return provenLocally ? EvidenceKind.ProvenUnderAssumptions : EvidenceKind.ExternalContract;

        return provenLocally ? EvidenceKind.ProvenUnderAssumptions : EvidenceKind.StaticUpperBound;
    }

    public static Finding ApplySuppression(Finding finding, Suppression suppression, DateOnly? today = null)
    {
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(suppression);

        var currentDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (suppression.Expires < currentDate)
            throw new ArgumentException($"suppression '{suppression.Id}' is expired.", nameof(suppression));

        if (finding.Evidence == EvidenceKind.Unknown)
        {
            return finding with
            {
                Suppressed = true,
                SuppressionId = suppression.Id,
                Description = $"{finding.Description} Suppressed: {suppression.Reason}",
                Title = $"{finding.Title} (suppressed)"
            };
        }

        return finding with
        {
            Suppressed = true,
            SuppressionId = suppression.Id,
            Description = $"{finding.Description} Suppressed under external contract: {suppression.Reason}",
            Title = $"{finding.Title} (suppressed)"
        };
    }

    public static IReadOnlyList<string> Validate(AnalysisReport report, DateOnly? today = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        var errors = new List<string>();
        var currentDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.IsNullOrWhiteSpace(report.SchemaVersion))
            errors.Add("schemaVersion is required.");

        if (string.IsNullOrWhiteSpace(report.Assembly.Name))
            errors.Add("assembly.name is required.");

        if (!IsSha256(report.Assembly.Sha256))
            errors.Add("assembly.sha256 must be a 64-character hexadecimal SHA-256 value.");

        if (string.IsNullOrWhiteSpace(report.Provenance.ToolVersion))
            errors.Add("provenance.toolVersion is required.");

        foreach (var contract in report.Contracts)
        {
            if (string.IsNullOrWhiteSpace(contract.Symbol)) errors.Add("contract.symbol is required.");
            if (contract.Max < 0) errors.Add($"contract '{contract.Symbol}' max cannot be negative.");
            if (string.IsNullOrWhiteSpace(contract.Unit)) errors.Add($"contract '{contract.Symbol}' unit is required.");
            if (string.IsNullOrWhiteSpace(contract.Source)) errors.Add($"contract '{contract.Symbol}' source is required.");
            if (string.IsNullOrWhiteSpace(contract.Scope)) errors.Add($"contract '{contract.Symbol}' scope is required.");
            if (contract.Expires is not null && contract.Expires < currentDate)
                errors.Add($"contract '{contract.Symbol}' is expired.");
            if (contract.Evidence != EvidenceKind.ExternalContract && contract.Trust == ContractTrust.External)
                errors.Add($"contract '{contract.Symbol}' must use ExternalContract evidence.");
        }

        foreach (var suppression in report.Suppressions)
        {
            if (string.IsNullOrWhiteSpace(suppression.Id)) errors.Add("suppression.id is required.");
            if (string.IsNullOrWhiteSpace(suppression.Target)) errors.Add("suppression.target is required.");
            if (string.IsNullOrWhiteSpace(suppression.Reason)) errors.Add($"suppression '{suppression.Id}' reason is required.");
            if (string.IsNullOrWhiteSpace(suppression.Owner)) errors.Add($"suppression '{suppression.Id}' owner is required.");
            if (string.IsNullOrWhiteSpace(suppression.Scope)) errors.Add($"suppression '{suppression.Id}' scope is required.");
            if (suppression.Expires < currentDate) errors.Add($"suppression '{suppression.Id}' is expired.");
        }

        return errors;
    }

    public static void EnsureValid(AnalysisReport report, DateOnly? today = null)
    {
        var errors = Validate(report, today);
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(report));
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }
}
