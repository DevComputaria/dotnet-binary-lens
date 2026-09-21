using System.Text.Json;
using System.Text.Json.Serialization;
using ClrLens.Core.Domain;

namespace ClrLens.Reporting;

public static class SarifReportSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string Serialize(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        var results = findings.Select(finding => new
        {
            ruleId = finding.Id,
            level = finding.Severity switch
            {
                Severity.Critical or Severity.High => "error",
                Severity.Medium => "warning",
                _ => "note"
            },
            message = new { text = $"{finding.Title}: {finding.Description} {finding.WhyThisMatters}" },
            locations = new[]
            {
                new
                {
                    physicalLocation = new
                    {
                        artifactLocation = new { uri = finding.Location.TypeName ?? "assembly" },
                        region = new { startLine = 1, startColumn = Math.Max(1, finding.Location.IlStart ?? 0) }
                    }
                }
            },
            properties = new
            {
                evidence = finding.Evidence.ToString(),
                remediation = finding.Remediation.ToString(),
                confidence = finding.Confidence,
                formula = finding.Model?.Formula,
                suppressed = finding.Suppressed,
                suppressionId = finding.SuppressionId,
                suppressionStatus = finding.Suppressed
                    ? (finding.Evidence == EvidenceKind.ExternalContract || finding.Evidence == EvidenceKind.ProvenUnderAssumptions ? "SUPPRESSED_UNDER_EXTERNAL_CONTRACT" : "SUPPRESSED")
                    : "ACTIVE"
            }
        }).ToArray();

        var document = new
        {
            version = "2.1.0",
            @schema = "https://json.schemastore.org/sarif-2.1.0.json",
            runs = new[]
            {
                new
                {
                    tool = new { driver = new { name = "CLR Lens", informationUri = "https://clrlens.dev" } },
                    results
                }
            }
        };

        return JsonSerializer.Serialize(document, Options);
    }
}
