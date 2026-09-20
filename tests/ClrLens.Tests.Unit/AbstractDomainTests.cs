using ClrLens.Analysis;
using ClrLens.IR;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class AbstractDomainTests
{
    [Fact]
    public void IntervalJoinIsMonotonicAndWidensOutwardBounds()
    {
        var current = new Interval(0, 10);
        var next = new Interval(-2, 12);
        var joined = current.Join(next);
        var widened = current.Widen(next);

        Assert.True(current.LessOrEqual(joined));
        Assert.True(next.LessOrEqual(joined));
        Assert.Equal(long.MinValue, widened.Min);
        Assert.Equal(long.MaxValue, widened.Max);
    }

    [Fact]
    public void CongruenceJoinLosesPrecisionWhenResiduesDiffer()
    {
        var even = new Congruence(2, 0);
        var odd = new Congruence(2, 1);

        Assert.True(even.Join(odd).IsTop);
        Assert.True(even.LessOrEqual(Congruence.Top));
    }

    [Fact]
    public void AbstractValueJoinPreservesFixedWidthOnlyWhenCompatible()
    {
        var left = Value(32, false, new Interval(0, 10));
        var right = Value(32, false, new Interval(10, 20));
        var joined = left.Join(right);

        Assert.Equal(32, joined.BitWidth);
        Assert.False(joined.IsUnsigned);
        Assert.Equal(new Interval(0, 20), joined.Range);
    }

    [Fact]
    public void FixpointEngineConvergesAndReportsPrecisionLossOnWidening()
    {
        var initial = new AbstractState();
        var value = new ValueId(1);
        initial.Set(value, Value(32, false, new Interval(0, 0)));

        var result = FixpointEngine.Run(initial, state =>
        {
            var current = state.Values[value];
            state.Set(value, current with { Range = current.Range.Add(new Interval(1, 1)) });
            return state;
        }, maxIterations: 12, wideningAfter: 2);

        Assert.True(result.Converged);
        Assert.InRange(result.Iterations, 1, 12);
        Assert.True(result.State.LostPrecision);
    }

    private static AbstractValue Value(int width, bool unsigned, Interval range) => new(
        "System.Int32", width, unsigned, range, Congruence.Top,
        NullabilityState.Unknown, new Cardinality(range), AllocationState.None,
        EscapeState.NoEscape, AliasState.NoAlias, ConcurrencyState.Sequential);
}
