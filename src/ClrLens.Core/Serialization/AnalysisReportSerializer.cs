using System.Text.Json;
using System.Text.Json.Serialization;
using ClrLens.Core.Domain;

namespace ClrLens.Core.Serialization;

public static class AnalysisReportSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(AnalysisReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, Options);
    }

    public static AnalysisReport Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<AnalysisReport>(json, Options)
            ?? throw new JsonException("The analysis report payload was empty.");
    }
}
