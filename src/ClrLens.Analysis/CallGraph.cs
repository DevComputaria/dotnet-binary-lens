using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ClrLens.IL;

namespace ClrLens.Analysis;

[Flags]
public enum UnknownEffect
{
    None = 0,
    MayAllocate = 1,
    MayThrow = 2,
    MayWriteMemory = 4,
    MayRetainArguments = 8,
    MayBlock = 16,
    MaySpawnWork = 32,
    UnknownCpu = 64
}

public enum CallResolutionKind
{
    InternalDefinition,
    ExternalMemberReference,
    ExternalMethodSpecification,
    Unknown
}

public sealed record CallSite(
    MethodDefinitionHandle Caller,
    int IlOffset,
    OpCode OpCode,
    int MetadataToken,
    string TargetName,
    CallResolutionKind Resolution,
    MethodDefinitionHandle? InternalTarget,
    UnknownEffect Effects);

public sealed record CallGraph(
    IReadOnlyList<CallSite> CallSites,
    IReadOnlyDictionary<MethodDefinitionHandle, IReadOnlySet<MethodDefinitionHandle>> Edges,
    IReadOnlyList<IReadOnlySet<MethodDefinitionHandle>> StronglyConnectedComponents)
{
    public IEnumerable<CallSite> InternalCalls => CallSites.Where(site => site.Resolution == CallResolutionKind.InternalDefinition);
    public IEnumerable<CallSite> ExternalCalls => CallSites.Where(site => site.Resolution != CallResolutionKind.InternalDefinition);
}

public sealed class CallGraphBuilder
{
    public static CallGraph Build(MetadataReader metadata, IReadOnlyDictionary<MethodDefinitionHandle, DecodedMethodBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(bodies);

        var calls = new List<CallSite>();
        var edges = bodies.Keys.ToDictionary(handle => handle, _ => new HashSet<MethodDefinitionHandle>());
        foreach (var pair in bodies.OrderBy(pair => MetadataTokens.GetToken(pair.Key)))
        {
            foreach (var instruction in pair.Value.Instructions.Where(IsCall))
            {
                var token = instruction.Operand is int value ? value : 0;
                var site = Resolve(metadata, pair.Key, instruction, token);
                calls.Add(site);
                if (site.InternalTarget is { } target) edges[pair.Key].Add(target);
            }
        }

        var readonlyEdges = edges.ToDictionary(pair => pair.Key, pair => (IReadOnlySet<MethodDefinitionHandle>)pair.Value);
        return new CallGraph(calls, readonlyEdges, ComputeSccs(edges));
    }

    private static bool IsCall(CilInstruction instruction) => instruction.OpCode.Name is "call" or "callvirt" or "calli" or "newobj";

    private static CallSite Resolve(MetadataReader metadata, MethodDefinitionHandle caller, CilInstruction instruction, int token)
    {
        if (token == 0)
            return new CallSite(caller, instruction.Offset, instruction.OpCode, token, "<unknown>", CallResolutionKind.Unknown, null, EffectsFor(instruction.OpCode, null));

        try
        {
            var handle = MetadataTokens.EntityHandle(token);
            if (handle.Kind == HandleKind.MethodDefinition)
            {
                var target = (MethodDefinitionHandle)handle;
                var method = metadata.GetMethodDefinition(target);
                var methodName = metadata.GetString(method.Name);
                return new CallSite(caller, instruction.Offset, instruction.OpCode, token, methodName, CallResolutionKind.InternalDefinition, target, EffectsFor(instruction.OpCode, methodName));
            }

            if (handle.Kind == HandleKind.MemberReference)
            {
                var member = metadata.GetMemberReference((MemberReferenceHandle)handle);
                return new CallSite(caller, instruction.Offset, instruction.OpCode, token, metadata.GetString(member.Name), CallResolutionKind.ExternalMemberReference, null, EffectsFor(instruction.OpCode, metadata.GetString(member.Name)));
            }

            if (handle.Kind == HandleKind.MethodSpecification)
                return new CallSite(caller, instruction.Offset, instruction.OpCode, token, "<method-specification>", CallResolutionKind.ExternalMethodSpecification, null, EffectsFor(instruction.OpCode, null));
        }
        catch (ArgumentException)
        {
            // Keep the call site as unknown; malformed/unresolved metadata must not disappear.
        }

        return new CallSite(caller, instruction.Offset, instruction.OpCode, token, "<unknown>", CallResolutionKind.Unknown, null, EffectsFor(instruction.OpCode, null));
    }

    private static UnknownEffect EffectsFor(OpCode opCode, string? methodName)
    {
        var effects = UnknownEffect.MayThrow | UnknownEffect.MayWriteMemory | UnknownEffect.UnknownCpu;
        if (opCode.Name == "newobj") effects |= UnknownEffect.MayAllocate;
        if (opCode.Name == "calli" || methodName is null) effects |= UnknownEffect.MayAllocate | UnknownEffect.MayRetainArguments | UnknownEffect.MayBlock | UnknownEffect.MaySpawnWork;
        if (methodName is "Wait" or "WaitAsync" or "Sleep" or "GetResult" or "get_Result") effects |= UnknownEffect.MayBlock;
        if (methodName is "Run" or "StartNew" or "WhenAll") effects |= UnknownEffect.MaySpawnWork;
        return effects;
    }

    private static IReadOnlyList<IReadOnlySet<MethodDefinitionHandle>> ComputeSccs(IReadOnlyDictionary<MethodDefinitionHandle, HashSet<MethodDefinitionHandle>> graph)
    {
        var index = 0;
        var stack = new Stack<MethodDefinitionHandle>();
        var onStack = new HashSet<MethodDefinitionHandle>();
        var indexes = new Dictionary<MethodDefinitionHandle, int>();
        var lowLinks = new Dictionary<MethodDefinitionHandle, int>();
        var components = new List<IReadOnlySet<MethodDefinitionHandle>>();

        void Visit(MethodDefinitionHandle node)
        {
            indexes[node] = lowLinks[node] = index++;
            stack.Push(node);
            onStack.Add(node);
            foreach (var successor in graph[node])
            {
                if (!indexes.TryGetValue(successor, out _))
                {
                    Visit(successor);
                    lowLinks[node] = Math.Min(lowLinks[node], lowLinks[successor]);
                }
                else if (onStack.Contains(successor))
                {
                    lowLinks[node] = Math.Min(lowLinks[node], indexes[successor]);
                }
            }

            if (lowLinks[node] != indexes[node]) return;
            var component = new HashSet<MethodDefinitionHandle>();
            MethodDefinitionHandle current;
            do
            {
                current = stack.Pop();
                onStack.Remove(current);
                component.Add(current);
            } while (!current.Equals(node));
            components.Add(component);
        }

        foreach (var node in graph.Keys.OrderBy(handle => MetadataTokens.GetToken(handle)))
            if (!indexes.TryGetValue(node, out _)) Visit(node);
        return components;
    }
}
