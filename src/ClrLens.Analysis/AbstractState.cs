using ClrLens.IR;

namespace ClrLens.Analysis;

public sealed class AbstractState
{
    private readonly Dictionary<ValueId, AbstractValue> values = new();

    public IReadOnlyDictionary<ValueId, AbstractValue> Values => values;
    public bool LostPrecision { get; private set; }
    public IReadOnlyList<string> PrecisionNotes => precisionNotes;
    private readonly List<string> precisionNotes = [];

    public void Set(ValueId id, AbstractValue value) => values[id] = value;

    public bool TryGet(ValueId id, out AbstractValue value) => values.TryGetValue(id, out value);

    public AbstractState Join(AbstractState other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = Clone();
        foreach (var pair in other.values)
        {
            if (result.values.TryGetValue(pair.Key, out var current)) result.values[pair.Key] = current.Join(pair.Value);
            else result.values[pair.Key] = pair.Value;
        }
        result.LostPrecision = LostPrecision || other.LostPrecision;
        result.precisionNotes.AddRange(precisionNotes);
        result.precisionNotes.AddRange(other.precisionNotes);
        return result;
    }

    public AbstractState Widen(AbstractState next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var result = Clone();
        foreach (var pair in next.values)
        {
            if (result.values.TryGetValue(pair.Key, out var current)) result.values[pair.Key] = current.Widen(pair.Value);
            else result.values[pair.Key] = pair.Value;
        }
        result.LostPrecision = LostPrecision || next.LostPrecision;
        if (!result.values.SequenceEqual(next.values)) result.AddPrecisionNote("Widening changed one or more abstract values.");
        return result;
    }

    public AbstractState Narrow(AbstractState next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var result = Clone();
        foreach (var pair in next.values)
        {
            if (result.values.TryGetValue(pair.Key, out var current))
            {
                var range = current.Range.Narrow(pair.Value.Range);
                result.values[pair.Key] = pair.Value with { Range = range, Cardinality = new Cardinality(range) };
            }
        }
        return result;
    }

    public void AddPrecisionNote(string note)
    {
        LostPrecision = true;
        if (!precisionNotes.Contains(note, StringComparer.Ordinal)) precisionNotes.Add(note);
    }

    public AbstractState Clone()
    {
        var result = new AbstractState();
        foreach (var pair in values) result.values[pair.Key] = pair.Value;
        result.LostPrecision = LostPrecision;
        result.precisionNotes.AddRange(precisionNotes);
        return result;
    }
}

public sealed record FixpointResult(
    AbstractState State,
    bool Converged,
    int Iterations,
    IReadOnlyList<string> Diagnostics);

public sealed class FixpointEngine
{
    public static FixpointResult Run(
        AbstractState initial,
        Func<AbstractState, AbstractState> transfer,
        int maxIterations = 100,
        int wideningAfter = 8)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(wideningAfter);

        var current = initial.Clone();
        var diagnostics = new List<string>();
        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            var next = transfer(current.Clone());
            var candidate = iteration > wideningAfter ? current.Widen(next) : current.Join(next);
            if (Equivalent(current, candidate)) return new(candidate, true, iteration, diagnostics);
            current = candidate;
        }

        current.AddPrecisionNote($"Fixpoint iteration limit {maxIterations} reached.");
        diagnostics.Add($"Fixpoint did not converge within {maxIterations} iterations.");
        return new(current, false, maxIterations, diagnostics);
    }

    private static bool Equivalent(AbstractState left, AbstractState right) =>
        left.Values.Count == right.Values.Count && left.Values.All(pair => right.Values.TryGetValue(pair.Key, out var value) && pair.Value.Equals(value));
}
