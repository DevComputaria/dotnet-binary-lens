using ClrLens.PE;
using ClrLens.IL;
using ClrLens.IR;
using ClrLens.Analysis;
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

var irNodes = 0;
var allocationNodes = 0;
    var irExceptionRegions = 0;
    var distinctValues = 0;
    var cfgCount = 0;
    var loopCount = 0;
    var exceptionalEdgeCount = 0;
using (var stream = File.OpenRead(fixtureAssemblyPath))
using (var peReader = new PEReader(stream))
{
    var metadata = peReader.GetMetadataReader();
    var decoder = new CilDecoder();
    var irBuilder = new CilToIrBuilder();
    var decodedMethods = 0;
    var exceptionRegions = 0;
    foreach (var handle in metadata.MethodDefinitions)
    {
        var method = metadata.GetMethodDefinition(handle);
        if (method.RelativeVirtualAddress == 0) continue;
        var decoded = decoder.Decode(peReader, method);
        Assert(!decoded.Diagnostics.Any(d => d.Code == CilDiagnosticCode.InvalidBranchTarget), $"Invalid branch in method token {handle.GetHashCode()}.");
        var ir = irBuilder.Build(decoded);
        var cfg = new ControlFlowGraphBuilder().Build(decoded);
        Assert(ir.Nodes.All(node => node.Offset >= 0), "IR node provenance offset was invalid.");
        Assert(ir.Nodes.Select(node => node.Offset).Distinct().Count() <= ir.Nodes.Count, "IR node provenance was not preserved.");
        Assert(ir.ExceptionRegions.Count == decoded.ExceptionRegions.Count, "IR exception regions were not preserved.");
        Assert(cfg.Blocks.Count > 0, "CFG did not create basic blocks.");
        Assert(cfg.Edges.All(edge => cfg.Blocks.Any(block => block.Id == edge.From) && cfg.Blocks.Any(block => block.Id == edge.To)), "CFG contains an invalid edge.");
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

    Assert(decodedMethods > 0, "No method bodies were decoded.");
    Assert(exceptionRegions > 0, "Exception handling regions were not preserved.");
    Assert(irNodes > 0, "No IR nodes were created.");
    Assert(allocationNodes > 0, "Allocation instructions were not represented in IR.");
    Assert(distinctValues > 0, "SSA-like values were not created.");
    Assert(irExceptionRegions > 0, "IR did not retain exception regions.");
    Assert(cfgCount > 0, "No CFGs were created.");
    Assert(loopCount > 0, "Natural loops were not detected.");
    Assert(exceptionalEdgeCount > 0, "Exceptional CFG edges were not created.");
}

var invalidMetadataPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "unsupported", "invalid-metadata.bin");
Assert(File.Exists(invalidMetadataPath), "Invalid metadata fixture is missing.");
var invalid = await reader.ReadAsync(invalidMetadataPath);
Assert(!invalid.IsSuccess, "Invalid metadata fixture was accepted.");
Assert(invalid.Diagnostics.Any(d => d.Code is AssemblyDiagnosticCode.InvalidPe or AssemblyDiagnosticCode.MissingMetadata), "Invalid input diagnostic was not structured.");

var missing = await reader.ReadAsync(Path.Combine(Directory.GetCurrentDirectory(), "does-not-exist.dll"));
Assert(!missing.IsSuccess && missing.Diagnostics.Any(d => d.Code == AssemblyDiagnosticCode.FileNotFound), "Missing input diagnostic was not structured.");

Console.WriteLine($"T07 CFG harness: PASS ({model.Identity.Name}, CFGs={cfgCount}, loops={loopCount}, exceptionalEdges={exceptionalEdgeCount}, IR nodes={irNodes}, values={distinctValues})");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
