using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.IL;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.IL;

internal static class TestSupport
{
    public static string RepoRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "ClrLens.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }

    public static string FixtureAssemblyPath => typeof(FixtureMarker).Assembly.Location;

    public static string ManifestPath => Path.Combine(RepoRoot, "tests", "fixtures", "fixture-manifest.json");

    public static string InvalidMetadataFixturePath => Path.Combine(RepoRoot, "tests", "fixtures", "unsupported", "invalid-metadata.bin");

    public static DecodedMethodBody DecodeFixtureMethod(string methodName)
    {
        using var stream = File.OpenRead(FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var decoder = new CilDecoder();

        foreach (var handle in metadata.MethodDefinitions)
        {
            var definition = metadata.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0)
            {
                continue;
            }

            if (metadata.GetString(definition.Name) == methodName)
            {
                return decoder.Decode(peReader, definition);
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' not found in fixture assembly.");
    }

    public static IReadOnlyDictionary<MethodDefinitionHandle, DecodedMethodBody> DecodeAllMethods(PEReader peReader, MetadataReader metadata)
    {
        var decoder = new CilDecoder();
        var bodies = new Dictionary<MethodDefinitionHandle, DecodedMethodBody>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            bodies[handle] = decoder.Decode(peReader, method);
        }

        return bodies;
    }
}
