using System.Globalization;
using ClrLens.IL;

namespace ClrLens.Analysis;

public enum TerminationState
{
    Proven,
    ProvenUnderAssumptions,
    NonTerminationProven,
    NonTerminationPossible,
    Unknown
}

public sealed record ScalarRecurrence(
    int LoopHeader,
    int? Local,
    string Start,
    string Step,
    string Expression);

public sealed record LoopBound(
    int LoopHeader,
    string? Symbol,
    string? Comparison,
    string Evidence);

public sealed record ScalarEvolutionResult(
    int LoopHeader,
    int Latch,
    int BackedgeTakenCount,
    string? TripCount,
    IReadOnlyList<ScalarRecurrence> Recurrences,
    LoopBound? Bound,
    TerminationState Termination,
    IReadOnlyList<string> Diagnostics);

public sealed class ScalarEvolutionAnalyzer
{
    public static IReadOnlyList<ScalarEvolutionResult> Analyze(DecodedMethodBody body, ControlFlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(graph);
        var results = new List<ScalarEvolutionResult>();
        foreach (var loop in graph.NaturalLoops.OrderBy(loop => loop.Header).ThenBy(loop => loop.Latch))
        {
            var instructions = loop.Nodes.SelectMany(id => graph.Blocks[id].Instructions).OrderBy(instruction => instruction.Offset).ToArray();
            var recurrences = FindRecurrences(loop, instructions);
            var bound = FindBound(loop, instructions);
            var backedgeCount = CountBackedgeExecutions(loop, graph);
            var termination = InferTermination(recurrences, bound);
            var tripCount = BuildTripCount(recurrences, bound, termination);
            var diagnostics = new List<string>();
            if (recurrences.Count == 0) diagnostics.Add("No induction-variable recurrence was proven.");
            if (bound is null) diagnostics.Add("Loop bound was not resolved; trip count remains unknown.");
            results.Add(new ScalarEvolutionResult(loop.Header, loop.Latch, backedgeCount, tripCount, recurrences, bound, termination, diagnostics));
        }
        return results;
    }

    private static IReadOnlyList<ScalarRecurrence> FindRecurrences(NaturalLoop loop, IReadOnlyList<CilInstruction> instructions)
    {
        var result = new List<ScalarRecurrence>();
        for (var index = 0; index + 3 < instructions.Count; index++)
        {
            var first = instructions[index];
            var second = instructions[index + 1];
            var third = instructions[index + 2];
            var fourth = instructions[index + 3];
            if (!IsLoadLocal(first.OpCode.Name) || !IsIntegerConstant(second, out var step)) continue;
            if (third.OpCode.Name is not ("add" or "add.ovf" or "sub" or "sub.ovf")) continue;
            if (!IsStoreLocal(fourth.OpCode.Name, out var local)) continue;
            if (third.OpCode.Name.StartsWith("sub", StringComparison.Ordinal)) step = -step;
            var start = FindInitialValue(instructions, local);
            result.Add(new ScalarRecurrence(loop.Header, local, start, step.ToString(CultureInfo.InvariantCulture), $"{{{start},+,{step}}}<L{loop.Header}>"));
        }
        return result.DistinctBy(recurrence => (recurrence.Local, recurrence.Step)).ToArray();
    }

    private static LoopBound? FindBound(NaturalLoop loop, IReadOnlyList<CilInstruction> instructions)
    {
        var comparison = instructions.FirstOrDefault(instruction => instruction.OpCode.Name is "blt" or "blt.s" or "blt.un" or "blt.un.s" or "bge" or "bge.s" or "bge.un" or "bge.un.s" or "ble" or "ble.s" or "ble.un" or "ble.un.s" or "bgt" or "bgt.s" or "bgt.un" or "bgt.un.s" or "beq" or "beq.s" or "bne.un" or "bne.un.s" or "clt" or "clt.un" or "cgt" or "cgt.un");
        if (comparison is null) return null;
        var symbol = instructions.TakeWhile(instruction => instruction.Offset <= comparison.Offset)
            .Reverse()
            .Select(instruction => ArgumentSymbol(instruction))
            .FirstOrDefault(value => value is not null);
        return new LoopBound(loop.Header, symbol, comparison.OpCode.Name, symbol is null ? "Comparison found but bound symbol is unknown." : "Argument/local comparison found in loop condition.");
    }

    private static string? BuildTripCount(IReadOnlyList<ScalarRecurrence> recurrences, LoopBound? bound, TerminationState termination)
    {
        var recurrence = recurrences.Count == 0 ? null : recurrences[0];
        if (recurrence is null || bound?.Symbol is null || termination == TerminationState.Unknown) return null;
        return $"ceil(({bound.Symbol} - ({recurrence.Start})) / ({recurrence.Step}))";
    }

    private static TerminationState InferTermination(IReadOnlyList<ScalarRecurrence> recurrences, LoopBound? bound)
    {
        if (recurrences.Count == 0 || bound is null) return TerminationState.Unknown;
        if (recurrences.All(recurrence => recurrence.Step != "0")) return TerminationState.ProvenUnderAssumptions;
        return TerminationState.Unknown;
    }

    private static int CountBackedgeExecutions(NaturalLoop loop, ControlFlowGraph graph) =>
        graph.BackEdges.Count(edge => edge is { From: var from, To: var to } && from == loop.Latch && to == loop.Header);

    private static string FindInitialValue(IReadOnlyList<CilInstruction> instructions, int? local)
    {
        if (local is null) return "unknown";
        for (var index = 0; index + 1 < instructions.Count; index++)
        {
            if (IsIntegerConstant(instructions[index], out var value) && IsStoreLocal(instructions[index + 1].OpCode.Name, out var candidate) && candidate == local)
                return value.ToString(CultureInfo.InvariantCulture);
        }
        return "0";
    }

    private static string? ArgumentSymbol(CilInstruction instruction) => instruction.OpCode.Name switch
    {
        "ldarg.0" => "arg0",
        "ldarg.1" => "arg1",
        "ldarg.2" => "arg2",
        "ldarg.3" => "arg3",
        "ldarg" or "ldarg.s" => $"arg{instruction.Operand}",
        _ => null
    };

    private static bool IsLoadLocal(string? name) => name is "ldloc.0" or "ldloc.1" or "ldloc.2" or "ldloc.3" or "ldloc" or "ldloc.s";
    private static bool IsStoreLocal(string? name, out int? local)
    {
        local = name switch { "stloc.0" => 0, "stloc.1" => 1, "stloc.2" => 2, "stloc.3" => 3, "stloc" or "stloc.s" => null, _ => -1 };
        return local != -1;
    }

    private static bool IsIntegerConstant(CilInstruction instruction, out int value)
    {
        value = instruction.OpCode.Name switch
        {
            "ldc.i4.m1" => -1,
            "ldc.i4.0" => 0,
            "ldc.i4.1" => 1,
            "ldc.i4.2" => 2,
            "ldc.i4.3" => 3,
            "ldc.i4.4" => 4,
            "ldc.i4.5" => 5,
            "ldc.i4.6" => 6,
            "ldc.i4.7" => 7,
            "ldc.i4.8" => 8,
            "ldc.i4.s" or "ldc.i4" when instruction.Operand is int number => number,
            _ => int.MinValue
        };
        return value != int.MinValue;
    }
}
