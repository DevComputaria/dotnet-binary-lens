using System.Reflection.Metadata;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using ClrLens.Analysis;
using ClrLens.Core.Domain;
using ClrLens.IL;

namespace ClrLens.Rules;

public sealed record StructuralMethodContext(
    MethodDefinitionHandle Method,
    string MethodName,
    DecodedMethodBody Body,
    ControlFlowGraph ControlFlow,
    IReadOnlyList<CallSite> CallSites);

public sealed class StructuralFindingsAnalyzer
{
    public IReadOnlyList<Finding> Analyze(StructuralMethodContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var findings = new List<Finding>();
        var loops = context.ControlFlow.NaturalLoops;

        foreach (var loop in loops.Where(loop => loop.Depth >= 2))
        {
            var body = BlockInstructions(context.ControlFlow, loop.Nodes).ToArray();
            findings.Add(CreateFinding(
                "CPU001",
                "CPU",
                Severity.High,
                context,
                body.FirstOrDefault()?.Offset ?? 0,
                body.LastOrDefault()?.Offset ?? 0,
                "Excessive loop nesting",
                "Nested natural loops were detected in the same method.",
                "The inner region may execute multiplicatively with the outer loop bound.",
                new CostModel("N1 × N2 × bodyCost", "O(N1×N2)", "SCU", new Dictionary<string, string> { ["loopDepth"] = loop.Depth.ToString() }),
                RemediationType.SourceChange,
                ["Review collection traversal and consider an algorithmic redesign."]));
        }

        foreach (var allocation in context.Body.Instructions.Where(IsAllocation))
        {
            var containing = loops.Where(loop => loop.Nodes.Any(blockId => context.ControlFlow.Blocks[blockId].Instructions.Any(instruction => instruction.Offset == allocation.Offset))).ToArray();
            if (containing.Length == 0) continue;
            findings.Add(CreateFinding(
                "MEM001",
                "Memory",
                Severity.High,
                context,
                allocation.Offset,
                allocation.Offset,
                "Allocation inside loop",
                $"The instruction {allocation.OpCode.Name} allocates inside a natural loop.",
                "Repeated allocation can increase allocation rate and GC pressure.",
                new CostModel("Iterations × allocationSize", "O(N)", "bytes", new Dictionary<string, string> { ["opcode"] = allocation.OpCode.Name! }),
                RemediationType.SourceChange,
                ["Consider buffer reuse, pooling or streaming when ownership semantics allow it."]));
        }

        foreach (var loop in loops)
        {
            var loopInstructions = BlockInstructions(context.ControlFlow, loop.Nodes).ToArray();
            if (loopInstructions.Length == 0 || !HasProgress(loopInstructions))
            {
                var latch = context.ControlFlow.Blocks[loop.Latch];
                if (latch.Instructions.All(instruction => !IsObservableProgress(instruction)))
                    findings.Add(CreateFinding(
                        "CPU004",
                        "CPU",
                        Severity.High,
                        context,
                        latch.StartOffset,
                        latch.Instructions[^1].Offset,
                        "Busy wait or progress-free loop",
                        "A loop has no recognizable call, allocation, field write or induction progress.",
                        "The loop may consume a logical processor while waiting for external state.",
                        new CostModel("Visits(loop)", "O(Unbounded)", "SCU", new Dictionary<string, string>()),
                        RemediationType.SourceChange,
                        ["Use an awaitable wait, blocking primitive, yield or observable progress mechanism."]));
            }

            var calls = context.CallSites.Where(call => loop.Nodes.Any(blockId => context.ControlFlow.Blocks[blockId].Instructions.Any(instruction => instruction.Offset == call.IlOffset))).ToArray();
            if (calls.Length > 0)
                findings.Add(CreateFinding(
                    "CPU005",
                    "CPU",
                    Severity.Medium,
                    context,
                    calls.Min(call => call.IlOffset),
                    calls.Max(call => call.IlOffset),
                    "Repeated call inside loop",
                    "One or more calls execute inside a natural loop.",
                    "Call cost and side effects may be multiplied by the loop trip count.",
                    new CostModel("TripCount × callCost", "O(N×callCost)", "SCU", new Dictionary<string, string> { ["calls"] = calls.Length.ToString() }),
                    RemediationType.Review,
                    ["Review the call summary and determine whether it can be hoisted, cached or reduced."]));
        }

        return findings;
    }

    private static IEnumerable<CilInstruction> BlockInstructions(ControlFlowGraph graph, IEnumerable<int> nodes) =>
        nodes.SelectMany(id => graph.Blocks[id].Instructions).OrderBy(instruction => instruction.Offset);

    private static bool IsAllocation(CilInstruction instruction) => instruction.OpCode.Name is "newobj" or "newarr" or "box";

    private static bool HasProgress(IEnumerable<CilInstruction> instructions) => instructions.Any(instruction => instruction.OpCode.Name is "add" or "add.ovf" or "sub" or "sub.ovf" or "stloc" or "stloc.s" or "stloc.0" or "stloc.1" or "stloc.2" or "stloc.3");

    private static bool IsObservableProgress(CilInstruction instruction) => instruction.OpCode.FlowControl is FlowControl.Call or FlowControl.Return or FlowControl.Throw || instruction.OpCode.Name is "newobj" or "newarr" or "stfld" or "stsfld" or "stloc" or "stloc.s" or "stloc.0" or "stloc.1" or "stloc.2" or "stloc.3";

    private static Finding CreateFinding(string id, string category, Severity severity, StructuralMethodContext context, int ilStart, int ilEnd, string title, string description, string why, CostModel model, RemediationType remediation, IReadOnlyList<string> recommendations) =>
        new(id, category, severity, 0.9, new AnalysisLocation(null, context.MethodName, MetadataTokens.GetToken(context.Method), ilStart, ilEnd, [], []), EvidenceKind.Inferred, remediation, title, description, why, model, [], recommendations);
}
