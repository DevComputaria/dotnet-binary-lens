using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;
using ClrLens.IR;
using ClrLens.PE;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.IL;

public static class FixtureHarness
{
    public static async Task<string> RunAsync()
    {
        var fixtureAssemblyPath = typeof(FixtureMarker).Assembly.Location;
        var reader = new AssemblyReader();
        var valid = await reader.ReadAsync(fixtureAssemblyPath);
        Ensure(valid.IsSuccess, string.Join("; ", valid.Diagnostics.Select(d => d.Message)));
        var model = valid.Model ?? throw new InvalidOperationException("Valid fixture did not produce an AssemblyModel.");
        Ensure(model.Identity.Sha256.Length == 64, "Assembly hash was not captured.");
        Ensure(model.TypeCount >= 6, "Expected fixture types were not discovered.");
        Ensure(model.MethodCount >= 18, "Expected fixture methods were not discovered.");
        Ensure(model.HasMetadata, "Managed metadata should be available for offset-based analysis.");
        Ensure(model.AssemblyReferences.Count > 0, "Assembly references were not captured.");

        var irNodes = 0;
        var allocationNodes = 0;
        var irExceptionRegions = 0;
        var distinctValues = 0;
        var cfgCount = 0;
        var loopCount = 0;
        var exceptionalEdgeCount = 0;
        var callSiteCount = 0;
        var externalCallCount = 0;
        var callSccCount = 0;
        using (var stream = File.OpenRead(fixtureAssemblyPath))
        using (var peReader = new PEReader(stream))
        {
            var metadata = peReader.GetMetadataReader();
            var decoder = new CilDecoder();
            var irBuilder = new CilToIrBuilder();
            var decodedMethods = 0;
            var exceptionRegions = 0;
            var bodies = new Dictionary<MethodDefinitionHandle, DecodedMethodBody>();
            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress == 0) continue;
                var decoded = decoder.Decode(peReader, method);
                bodies[handle] = decoded;
                Ensure(!decoded.Diagnostics.Any(d => d.Code == CilDiagnosticCode.InvalidBranchTarget), $"Invalid branch in method token {handle.GetHashCode()}.");
                var ir = irBuilder.Build(decoded);
                var cfg = new ControlFlowGraphBuilder().Build(decoded);
                Ensure(ir.Nodes.All(node => node.Offset >= 0), "IR node provenance offset was invalid.");
                Ensure(ir.Nodes.Select(node => node.Offset).Distinct().Count() <= ir.Nodes.Count, "IR node provenance was not preserved.");
                Ensure(ir.ExceptionRegions.Count == decoded.ExceptionRegions.Count, "IR exception regions were not preserved.");
                Ensure(cfg.Blocks.Count > 0, "CFG did not create basic blocks.");
                Ensure(cfg.Edges.All(edge => cfg.Blocks.Any(block => block.Id == edge.From) && cfg.Blocks.Any(block => block.Id == edge.To)), "CFG contains an invalid edge.");
                irNodes += ir.Nodes.Count;
                allocationNodes += ir.Nodes.Count(node => node.CanAllocate);
                distinctValues += ir.Nodes.SelectMany(node => node.Result is { } result ? [result] : Array.Empty<ValueId>()).Distinct().Count();
                irExceptionRegions += ir.ExceptionRegions.Count;
                cfgCount++;
                loopCount += cfg.NaturalLoops.Count;
                exceptionalEdgeCount += cfg.ExceptionalEdges.Count();
                decodedMethods++;
                exceptionRegions += decoded.ExceptionRegions.Count;
            }

            Ensure(decodedMethods > 0, "No method bodies were decoded.");
            Ensure(exceptionRegions > 0, "Exception handling regions were not preserved.");
            Ensure(irNodes > 0, "No IR nodes were created.");
            Ensure(allocationNodes > 0, "Allocation instructions were not represented in IR.");
            Ensure(distinctValues > 0, "SSA-like values were not created.");
            Ensure(irExceptionRegions > 0, "IR did not retain exception regions.");
            Ensure(cfgCount > 0, "No CFGs were created.");
            Ensure(loopCount > 0, "Natural loops were not detected.");
            Ensure(exceptionalEdgeCount > 0, "Exceptional CFG edges were not created.");

            var callGraph = CallGraphBuilder.Build(metadata, bodies);
            Ensure(callGraph.CallSites.Count > 0, "Call graph did not record call sites.");
            Ensure(callGraph.CallSites.All(site => site.IlOffset >= 0), "Call site offset provenance was invalid.");
            Ensure(callGraph.ExternalCalls.Any(), "External calls were not preserved.");
            Ensure(callGraph.ExternalCalls.All(site => site.Effects != UnknownEffect.None), "External calls have no conservative effects.");
            Ensure(callGraph.Edges.Count == bodies.Count, "Call graph did not create a node for every method body.");
            callSiteCount = callGraph.CallSites.Count;
            externalCallCount = callGraph.ExternalCalls.Count();
            callSccCount = callGraph.StronglyConnectedComponents.Count;
        }

        var invalidMetadataPath = Path.Combine(TestSupport.RepoRoot, "tests", "fixtures", "unsupported", "invalid-metadata.bin");
        Ensure(File.Exists(invalidMetadataPath), "Invalid metadata fixture is missing.");
        var invalid = await reader.ReadAsync(invalidMetadataPath);
        Ensure(!invalid.IsSuccess, "Invalid metadata fixture was accepted.");
        Ensure(invalid.Diagnostics.Any(d => d.Code is AssemblyDiagnosticCode.InvalidPe or AssemblyDiagnosticCode.MissingMetadata), "Invalid input diagnostic was not structured.");

        var missing = await reader.ReadAsync(Path.Combine(TestSupport.RepoRoot, "does-not-exist.dll"));
        Ensure(!missing.IsSuccess && missing.Diagnostics.Any(d => d.Code == AssemblyDiagnosticCode.FileNotFound), "Missing input diagnostic was not structured.");

        return $"T08 call graph harness: PASS ({model.Identity.Name}, CFGs={cfgCount}, loops={loopCount}, exceptionalEdges={exceptionalEdgeCount}, callSites={callSiteCount}, externalCalls={externalCallCount}, callSccs={callSccCount})";
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
