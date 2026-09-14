using System.Reflection.Emit;
using System.Reflection.Metadata;
using ClrLens.IL;

namespace ClrLens.Analysis;

public enum ControlFlowEdgeKind
{
    Normal,
    Exceptional
}

public sealed record BasicBlock(
    int Id,
    int StartOffset,
    IReadOnlyList<CilInstruction> Instructions,
    bool IsEntry,
    bool IsExit);

public sealed record ControlFlowEdge(
    int From,
    int To,
    ControlFlowEdgeKind Kind);

public sealed record NaturalLoop(
    int Header,
    int Latch,
    IReadOnlySet<int> Nodes,
    int Depth);

public sealed record ControlFlowGraph(
    IReadOnlyList<BasicBlock> Blocks,
    IReadOnlyList<ControlFlowEdge> Edges,
    IReadOnlyDictionary<int, IReadOnlySet<int>> Dominators,
    IReadOnlyDictionary<int, IReadOnlySet<int>> PostDominators,
    IReadOnlyList<IReadOnlySet<int>> StronglyConnectedComponents,
    IReadOnlyList<(int From, int To)> BackEdges,
    IReadOnlyList<NaturalLoop> NaturalLoops)
{
    public IEnumerable<ControlFlowEdge> NormalEdges => Edges.Where(edge => edge.Kind == ControlFlowEdgeKind.Normal);
    public IEnumerable<ControlFlowEdge> ExceptionalEdges => Edges.Where(edge => edge.Kind == ControlFlowEdgeKind.Exceptional);
}

public sealed class ControlFlowGraphBuilder
{
    public ControlFlowGraph Build(DecodedMethodBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Instructions.Count == 0)
            return new([], [], new Dictionary<int, IReadOnlySet<int>>(), new Dictionary<int, IReadOnlySet<int>>(), [], [], []);

        var instructions = body.Instructions.OrderBy(instruction => instruction.Offset).ToArray();
        var leaders = FindLeaders(instructions, body.ExceptionRegions);
        var blocks = BuildBlocks(instructions, leaders);
        var blockByOffset = blocks.ToDictionary(block => block.StartOffset);
        var instructionToBlock = blocks.SelectMany(block => block.Instructions.Select(instruction => (instruction.Offset, block.Id))).ToDictionary(item => item.Offset, item => item.Id);
        var edges = BuildEdges(blocks, instructionToBlock, body.ExceptionRegions);
        var normalEdges = edges.Where(edge => edge.Kind == ControlFlowEdgeKind.Normal).ToArray();
        var successors = blocks.ToDictionary(block => block.Id, block => normalEdges.Where(edge => edge.From == block.Id).Select(edge => edge.To).ToHashSet());
        var predecessors = blocks.ToDictionary(block => block.Id, block => normalEdges.Where(edge => edge.To == block.Id).Select(edge => edge.From).ToHashSet());
        var dominators = ComputeDominators(blocks, predecessors);
        var postDominators = ComputePostDominators(blocks, successors);
        var components = ComputeSccs(blocks, successors);
        var backEdges = normalEdges.Where(edge => dominators[edge.From].Contains(edge.To)).Select(edge => (edge.From, edge.To)).ToArray();
        var loops = backEdges.Select(edge => BuildNaturalLoop(edge.From, edge.To, predecessors, dominators)).OrderBy(loop => loop.Header).ThenBy(loop => loop.Latch).ToArray();
        var loopsWithDepth = loops.Select(loop => loop with { Depth = loops.Count(other => other.Nodes.IsSupersetOf(loop.Nodes) && other.Nodes.Count > loop.Nodes.Count) + 1 }).ToArray();

        return new ControlFlowGraph(blocks, edges, dominators, postDominators, components, backEdges, loopsWithDepth);
    }

    private static HashSet<int> FindLeaders(IReadOnlyList<CilInstruction> instructions, IReadOnlyList<CilExceptionRegion> regions)
    {
        var leaders = new HashSet<int> { instructions[0].Offset };
        var offsets = instructions.Select(instruction => instruction.Offset).ToHashSet();
        foreach (var instruction in instructions)
        {
            foreach (var target in instruction.BranchTargets)
                if (offsets.Contains(target)) leaders.Add(target);
            if (instruction.BranchTargets.Count > 0 || instruction.OpCode.FlowControl is FlowControl.Return or FlowControl.Throw)
            {
                var next = instructions.FirstOrDefault(candidate => candidate.Offset > instruction.Offset);
                if (next is not null) leaders.Add(next.Offset);
            }
        }
        foreach (var region in regions)
        {
            leaders.Add(region.TryOffset);
            leaders.Add(region.HandlerOffset);
            if (region.Kind == ExceptionRegionKind.Filter) leaders.Add(region.FilterOffset);
            AddIfInstructionBoundary(leaders, offsets, region.TryOffset + region.TryLength);
            AddIfInstructionBoundary(leaders, offsets, region.HandlerOffset + region.HandlerLength);
        }
        return leaders;
    }

    private static void AddIfInstructionBoundary(ISet<int> leaders, IReadOnlySet<int> offsets, int offset)
    {
        if (offsets.Contains(offset)) leaders.Add(offset);
    }

    private static List<BasicBlock> BuildBlocks(IReadOnlyList<CilInstruction> instructions, IReadOnlySet<int> leaders)
    {
        var result = new List<BasicBlock>();
        var orderedLeaders = leaders.OrderBy(value => value).ToArray();
        for (var index = 0; index < orderedLeaders.Length; index++)
        {
            var start = orderedLeaders[index];
            var end = index + 1 < orderedLeaders.Length ? orderedLeaders[index + 1] : int.MaxValue;
            var blockInstructions = instructions.Where(instruction => instruction.Offset >= start && instruction.Offset < end).ToArray();
            if (blockInstructions.Length == 0) continue;
            var last = blockInstructions[^1].OpCode.FlowControl;
            result.Add(new BasicBlock(result.Count, start, blockInstructions, result.Count == 0, last is FlowControl.Return or FlowControl.Throw));
        }
        return result;
    }

    private static List<ControlFlowEdge> BuildEdges(IReadOnlyList<BasicBlock> blocks, IReadOnlyDictionary<int, int> instructionToBlock, IReadOnlyList<CilExceptionRegion> regions)
    {
        var edges = new HashSet<ControlFlowEdge>();
        foreach (var block in blocks)
        {
            var last = block.Instructions[^1];
            foreach (var target in last.BranchTargets)
                if (instructionToBlock.TryGetValue(target, out var targetBlock)) edges.Add(new(block.Id, targetBlock, ControlFlowEdgeKind.Normal));
            if (last.OpCode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw))
            {
                var next = blocks.FirstOrDefault(candidate => candidate.StartOffset > block.StartOffset);
                if (next is not null) edges.Add(new(block.Id, next.Id, ControlFlowEdgeKind.Normal));
            }
        }

        foreach (var region in regions)
        {
            var handlerOffsets = new List<int> { region.HandlerOffset };
            if (region.Kind == ExceptionRegionKind.Filter) handlerOffsets.Add(region.FilterOffset);
            foreach (var block in blocks)
            {
                var mayThrow = block.Instructions.Any(instruction => CanThrow(instruction.OpCode));
                var inTry = block.Instructions.Any(instruction => instruction.Offset >= region.TryOffset && instruction.Offset < region.TryOffset + region.TryLength);
                if (!mayThrow || !inTry) continue;
                foreach (var handlerOffset in handlerOffsets)
                    if (instructionToBlock.TryGetValue(handlerOffset, out var handlerBlock)) edges.Add(new(block.Id, handlerBlock, ControlFlowEdgeKind.Exceptional));
            }
        }
        return edges.OrderBy(edge => edge.From).ThenBy(edge => edge.To).ThenBy(edge => edge.Kind).ToList();
    }

    private static bool CanThrow(OpCode opCode) => opCode.FlowControl is FlowControl.Call or FlowControl.Throw || opCode.Name is "newobj" or "newarr" or "ldfld" or "ldsfld" or "stfld" or "stsfld" or "ldelem" or "stelem";

    private static Dictionary<int, IReadOnlySet<int>> ComputeDominators(IReadOnlyList<BasicBlock> blocks, IReadOnlyDictionary<int, HashSet<int>> predecessors)
    {
        var all = blocks.Select(block => block.Id).ToHashSet();
        var result = blocks.ToDictionary(block => block.Id, block => (IReadOnlySet<int>)(block.IsEntry ? new HashSet<int> { block.Id } : all.ToHashSet()));
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in blocks.Where(block => !block.IsEntry))
            {
                var incoming = predecessors[block.Id].Select(predecessor => result[predecessor].ToHashSet()).ToList();
                var intersection = incoming.Count == 0 ? [] : incoming.Skip(1).Aggregate(incoming[0], (current, next) => current.Intersect(next).ToHashSet());
                intersection.Add(block.Id);
                if (!result[block.Id].SetEquals(intersection)) { result[block.Id] = intersection; changed = true; }
            }
        }
        return result;
    }

    private static Dictionary<int, IReadOnlySet<int>> ComputePostDominators(IReadOnlyList<BasicBlock> blocks, IReadOnlyDictionary<int, HashSet<int>> successors)
    {
        var all = blocks.Select(block => block.Id).ToHashSet();
        var exits = blocks.Where(block => successors[block.Id].Count == 0).Select(block => block.Id).ToHashSet();
        var result = blocks.ToDictionary(block => block.Id, block => (IReadOnlySet<int>)(exits.Contains(block.Id) ? new HashSet<int> { block.Id } : all.ToHashSet()));
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in blocks.Where(block => !exits.Contains(block.Id)).Reverse())
            {
                var outgoing = successors[block.Id].Select(successor => result[successor].ToHashSet()).ToList();
                var intersection = outgoing.Count == 0 ? [] : outgoing.Skip(1).Aggregate(outgoing[0], (current, next) => current.Intersect(next).ToHashSet());
                intersection.Add(block.Id);
                if (!result[block.Id].SetEquals(intersection)) { result[block.Id] = intersection; changed = true; }
            }
        }
        return result;
    }

    private static List<IReadOnlySet<int>> ComputeSccs(IReadOnlyList<BasicBlock> blocks, IReadOnlyDictionary<int, HashSet<int>> successors)
    {
        var index = 0; var stack = new Stack<int>(); var onStack = new HashSet<int>(); var indexes = new Dictionary<int, int>(); var low = new Dictionary<int, int>(); var result = new List<IReadOnlySet<int>>();
        void Visit(int node)
        {
            indexes[node] = low[node] = index++; stack.Push(node); onStack.Add(node);
            foreach (var successor in successors[node])
            {
                if (!indexes.TryGetValue(successor, out _)) { Visit(successor); low[node] = Math.Min(low[node], low[successor]); }
                else if (onStack.Contains(successor)) low[node] = Math.Min(low[node], indexes[successor]);
            }
            if (low[node] != indexes[node]) return;
            var component = new HashSet<int>(); int current;
            do { current = stack.Pop(); onStack.Remove(current); component.Add(current); } while (current != node);
            result.Add(component);
        }
        foreach (var block in blocks) if (!indexes.TryGetValue(block.Id, out _)) Visit(block.Id);
        return result.OrderBy(component => component.Min()).ToList();
    }

    private static NaturalLoop BuildNaturalLoop(int latch, int header, IReadOnlyDictionary<int, HashSet<int>> predecessors, IReadOnlyDictionary<int, IReadOnlySet<int>> dominators)
    {
        var nodes = new HashSet<int> { header, latch }; var work = new Stack<int>(); work.Push(latch);
        while (work.Count > 0)
            foreach (var predecessor in predecessors[work.Pop()])
                if (dominators[predecessor].Contains(header) && nodes.Add(predecessor)) work.Push(predecessor);
        return new NaturalLoop(header, latch, nodes, 0);
    }
}
