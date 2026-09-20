using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.IL;
using ClrLens.SampleFixtures;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class CilDecoderUnitTests
{
    [Fact]
    public void DecodesLinearAndNestedLoopsWithStableOffsets()
    {
        using var context = FixtureContext.Create();
        var decoder = new CilDecoder();
        var body = decoder.Decode(context.Reader, context.GetMethod("NestedLoop"));

        Assert.NotEmpty(body.Instructions);
        Assert.Equal(body.Instructions.Count, body.Instructions.Select(instruction => instruction.Offset).Distinct().Count());
        Assert.DoesNotContain(body.Diagnostics, diagnostic => diagnostic.Code == CilDiagnosticCode.InvalidBranchTarget);
        Assert.Contains(body.Instructions, instruction => instruction.OpCode.Name is "br" or "br.s");
    }

    [Fact]
    public void PreservesExceptionRegions()
    {
        using var context = FixtureContext.Create();
        var body = new CilDecoder().Decode(context.Reader, context.GetMethod("ExceptionFlow"));

        Assert.NotEmpty(body.ExceptionRegions);
        Assert.Contains(body.ExceptionRegions, region => region.Kind is ExceptionRegionKind.Catch or ExceptionRegionKind.Finally);
    }

    private sealed class FixtureContext : IDisposable
    {
        private readonly FileStream stream;
        private readonly PEReader peReader;
        private readonly MetadataReader metadata;

        private FixtureContext(string path)
        {
            stream = File.OpenRead(path);
            peReader = new PEReader(stream);
            metadata = peReader.GetMetadataReader();
            Reader = peReader;
        }

        public PEReader Reader { get; }

        public static FixtureContext Create() => new(typeof(FixtureMarker).Assembly.Location);

        public MethodDefinition GetMethod(string name)
        {
            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (metadata.GetString(method.Name) == name) return method;
            }

            throw new InvalidOperationException($"Fixture method '{name}' was not found.");
        }

        public void Dispose()
        {
            peReader.Dispose();
            stream.Dispose();
        }
    }
}
