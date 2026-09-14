using ClrLens.PE;
using ClrLens.IL;
using ClrLens.SampleFixtures;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var fixtureAssemblyPath = typeof(FixtureMarker).Assembly.Location;
var reader = new AssemblyReader();
var valid = await reader.ReadAsync(fixtureAssemblyPath);
Assert(valid.IsSuccess, string.Join("; ", valid.Diagnostics.Select(d => d.Message)));
var model = valid.Model ?? throw new InvalidOperationException("Valid fixture did not produce an AssemblyModel.");
Assert(model.Identity.Sha256.Length == 64, "Assembly hash was not captured.");
Assert(model.TypeCount >= 6, "Expected fixture types were not discovered.");
Assert(model.MethodCount >= 18, "Expected fixture methods were not discovered.");
Assert(model.HasMetadata, "Managed metadata should be available for offset-based analysis.");
Assert(model.AssemblyReferences.Count > 0, "Assembly references were not captured.");

using (var stream = File.OpenRead(fixtureAssemblyPath))
using (var peReader = new PEReader(stream))
{
    var metadata = peReader.GetMetadataReader();
    var decoder = new CilDecoder();
    var decodedMethods = 0;
    var exceptionRegions = 0;
    foreach (var handle in metadata.MethodDefinitions)
    {
        var method = metadata.GetMethodDefinition(handle);
        if (method.RelativeVirtualAddress == 0) continue;
        var decoded = decoder.Decode(peReader, method);
        Assert(!decoded.Diagnostics.Any(d => d.Code == CilDiagnosticCode.InvalidBranchTarget), $"Invalid branch in method token {handle.GetHashCode()}.");
        decodedMethods++;
        exceptionRegions += decoded.ExceptionRegions.Count;
    }

    Assert(decodedMethods > 0, "No method bodies were decoded.");
    Assert(exceptionRegions > 0, "Exception handling regions were not preserved.");
}

var invalidMetadataPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "unsupported", "invalid-metadata.bin");
Assert(File.Exists(invalidMetadataPath), "Invalid metadata fixture is missing.");
var invalid = await reader.ReadAsync(invalidMetadataPath);
Assert(!invalid.IsSuccess, "Invalid metadata fixture was accepted.");
Assert(invalid.Diagnostics.Any(d => d.Code is AssemblyDiagnosticCode.InvalidPe or AssemblyDiagnosticCode.MissingMetadata), "Invalid input diagnostic was not structured.");

var missing = await reader.ReadAsync(Path.Combine(Directory.GetCurrentDirectory(), "does-not-exist.dll"));
Assert(!missing.IsSuccess && missing.Diagnostics.Any(d => d.Code == AssemblyDiagnosticCode.FileNotFound), "Missing input diagnostic was not structured.");

Console.WriteLine($"T05 decoder harness: PASS ({model.Identity.Name}, {model.TypeCount} types, {model.MethodCount} methods, PDB={model.HasPdb})");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
