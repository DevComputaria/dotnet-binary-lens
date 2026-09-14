using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using ClrLens.SampleFixtures;

var fixtureAssemblyPath = typeof(FixtureMarker).Assembly.Location;
var fixtureBytes = await File.ReadAllBytesAsync(fixtureAssemblyPath);
var fixtureHash = Convert.ToHexString(SHA256.HashData(fixtureBytes));

using var fixtureStream = File.OpenRead(fixtureAssemblyPath);
using var peReader = new PEReader(fixtureStream);
if (!peReader.HasMetadata)
    throw new InvalidDataException("The sample fixture has no managed metadata.");

var metadata = peReader.GetMetadataReader();
var typeNames = metadata.TypeDefinitions
    .Select(metadata.GetTypeDefinition)
    .Select(type => metadata.GetString(type.Name))
    .ToHashSet(StringComparer.Ordinal);

var methodNames = metadata.MethodDefinitions
    .Select(metadata.GetMethodDefinition)
    .Select(method => metadata.GetString(method.Name))
    .ToHashSet(StringComparer.Ordinal);

var requiredTypes = new[] { "LoopPatterns", "RetentionPatterns", "GenericContainer`1" };
var requiredMethods = new[]
{
    "LinearLoop", "NestedLoop", "ExceptionFlow", "Allocate", "BoxValue",
    "MaterializeExternal", "MaterializeGeneric", "CreateDelegate", "Retain"
};

foreach (var requiredType in requiredTypes)
    Assert(typeNames.Contains(requiredType), $"Missing fixture type: {requiredType}");
foreach (var requiredMethod in requiredMethods)
    Assert(methodNames.Contains(requiredMethod), $"Missing fixture method: {requiredMethod}");

var invalidMetadataPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "unsupported", "invalid-metadata.bin");
Assert(File.Exists(invalidMetadataPath), "Invalid metadata fixture is missing.");
using (var invalidStream = File.OpenRead(invalidMetadataPath))
{
    try
    {
        using var invalidReader = new PEReader(invalidStream);
        Assert(!invalidReader.HasMetadata, "Invalid fixture unexpectedly contains metadata.");
    }
    catch (BadImageFormatException)
    {
        // Expected: malformed/non-PE input must be handled as unsupported.
    }
}

Console.WriteLine($"Fixture harness: PASS ({metadata.TypeDefinitions.Count} types, {metadata.MethodDefinitions.Count} methods, SHA256={fixtureHash})");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
