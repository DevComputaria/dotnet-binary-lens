namespace ClrLens.Analysis;

public readonly record struct Interval
{
    public static Interval Bottom => new(true, 0, 0);
    public static Interval Top => new(false, long.MinValue, long.MaxValue);

    public bool IsBottom { get; }
    public long Min { get; }
    public long Max { get; }
    public bool IsTop => !IsBottom && Min == long.MinValue && Max == long.MaxValue;

    public Interval(long min, long max)
    {
        if (min > max) throw new ArgumentException("Interval minimum cannot exceed maximum.");
        IsBottom = false;
        Min = min;
        Max = max;
    }

    private Interval(bool isBottom, long min, long max)
    {
        IsBottom = isBottom;
        Min = min;
        Max = max;
    }

    public bool Contains(long value) => !IsBottom && value >= Min && value <= Max;

    public bool LessOrEqual(Interval other)
    {
        if (IsBottom) return true;
        if (other.IsBottom) return IsBottom;
        return !other.IsBottom && other.Min <= Min && other.Max >= Max;
    }

    public Interval Join(Interval other)
    {
        if (IsBottom) return other;
        if (other.IsBottom) return this;
        return new(Math.Min(Min, other.Min), Math.Max(Max, other.Max));
    }

    public Interval Widen(Interval next)
    {
        if (IsBottom) return next;
        if (next.IsBottom) return this;
        var min = next.Min < Min ? long.MinValue : Min;
        var max = next.Max > Max ? long.MaxValue : Max;
        return new(min, max);
    }

    public Interval Narrow(Interval next)
    {
        if (IsBottom || next.IsBottom) return Bottom;
        return new(Math.Max(Min, next.Min), Math.Min(Max, next.Max));
    }

    public Interval Add(Interval other)
    {
        if (IsBottom || other.IsBottom) return Bottom;
        return new(SaturatingAdd(Min, other.Min), SaturatingAdd(Max, other.Max));
    }

    public Interval Subtract(Interval other)
    {
        if (IsBottom || other.IsBottom) return Bottom;
        return new(SaturatingSubtract(Min, other.Max), SaturatingSubtract(Max, other.Min));
    }

    private static long SaturatingAdd(long left, long right)
    {
        try { return checked(left + right); }
        catch (OverflowException) { return left < 0 ? long.MinValue : long.MaxValue; }
    }

    private static long SaturatingSubtract(long left, long right)
    {
        try { return checked(left - right); }
        catch (OverflowException) { return left < 0 ? long.MinValue : long.MaxValue; }
    }
}

public readonly record struct Congruence
{
    public static Congruence Top => new(0, 0, true);
    public static Congruence Constant(long value) => new(1, value, false);

    public int Modulus { get; }
    public long Remainder { get; }
    public bool IsTop { get; }

    public Congruence(int modulus, long remainder, bool isTop = false)
    {
        if (!isTop && modulus < 1) throw new ArgumentOutOfRangeException(nameof(modulus));
        Modulus = modulus;
        Remainder = isTop ? 0 : Normalize(remainder, modulus);
        IsTop = isTop;
    }

    public bool LessOrEqual(Congruence other)
    {
        if (IsTop) return other.IsTop;
        if (other.IsTop) return true;
        return other.Modulus % Modulus == 0 && Remainder % other.Modulus == other.Remainder;
    }

    public Congruence Join(Congruence other)
    {
        if (IsTop || other.IsTop) return Top;
        if (Modulus == other.Modulus && Remainder == other.Remainder) return this;
        return Top;
    }

    private static long Normalize(long value, int modulus)
    {
        var remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }
}

public enum NullabilityState { Unknown, Null, NonNull, MaybeNull }
public enum AllocationState { None, Managed, Array, Unknown }
public enum EscapeState { NoEscape, MethodEscape, ThreadEscape, GlobalEscape, Unknown }
public enum AliasState { NoAlias, MayAlias, MustAlias, Unknown }
public enum ConcurrencyState { Sequential, Concurrent, Unknown }

public readonly record struct Cardinality(Interval Bounds)
{
    public static Cardinality Unknown => new(Interval.Top);
    public static Cardinality Empty => new(new Interval(0, 0));
    public Cardinality Join(Cardinality other) => new(Bounds.Join(other.Bounds));
    public Cardinality Widen(Cardinality next) => new(Bounds.Widen(next.Bounds));
}

public readonly record struct AbstractValue(
    string Type,
    int BitWidth,
    bool IsUnsigned,
    Interval Range,
    Congruence Congruence,
    NullabilityState Nullability,
    Cardinality Cardinality,
    AllocationState Allocation,
    EscapeState Escape,
    AliasState Alias,
    ConcurrencyState Concurrency,
    string? Symbol = null)
{
    public bool LessOrEqual(AbstractValue other) =>
        Type == other.Type &&
        BitWidth == other.BitWidth &&
        IsUnsigned == other.IsUnsigned &&
        Range.LessOrEqual(other.Range) &&
        Congruence.LessOrEqual(other.Congruence) &&
        NullabilityLessOrEqual(Nullability, other.Nullability) &&
        Cardinality.Bounds.LessOrEqual(other.Cardinality.Bounds) &&
        AllocationLessOrEqual(Allocation, other.Allocation) &&
        EscapeLessOrEqual(Escape, other.Escape) &&
        AliasLessOrEqual(Alias, other.Alias) &&
        ConcurrencyLessOrEqual(Concurrency, other.Concurrency);

    public AbstractValue Join(AbstractValue other) => new(
        Type == other.Type ? Type : "System.Object",
        BitWidth == other.BitWidth ? BitWidth : 0,
        IsUnsigned == other.IsUnsigned && BitWidth == other.BitWidth && IsUnsigned,
        Range.Join(other.Range),
        Congruence.Join(other.Congruence),
        JoinNullability(Nullability, other.Nullability),
        Cardinality.Join(other.Cardinality),
        JoinAllocation(Allocation, other.Allocation),
        JoinEscape(Escape, other.Escape),
        JoinAlias(Alias, other.Alias),
        JoinConcurrency(Concurrency, other.Concurrency));

    public AbstractValue Widen(AbstractValue next) => JoinWith(next, (current, incoming) => current.Widen(incoming));

    private AbstractValue JoinWith(AbstractValue next, Func<Interval, Interval, Interval> rangeOperator) => new(
        Type == next.Type ? Type : "System.Object",
        BitWidth == next.BitWidth ? BitWidth : 0,
        IsUnsigned == next.IsUnsigned && BitWidth == next.BitWidth && IsUnsigned,
        rangeOperator(Range, next.Range),
        Congruence.Join(next.Congruence),
        JoinNullability(Nullability, next.Nullability),
        new Cardinality(rangeOperator(Cardinality.Bounds, next.Cardinality.Bounds)),
        JoinAllocation(Allocation, next.Allocation), JoinEscape(Escape, next.Escape),
        JoinAlias(Alias, next.Alias), JoinConcurrency(Concurrency, next.Concurrency));

    private static bool NullabilityLessOrEqual(NullabilityState current, NullabilityState other) => current == other || other is NullabilityState.Unknown or NullabilityState.MaybeNull || current == NullabilityState.Null && other == NullabilityState.MaybeNull || current == NullabilityState.NonNull && other == NullabilityState.MaybeNull;
    private static NullabilityState JoinNullability(NullabilityState left, NullabilityState right) => left == right ? left : NullabilityState.MaybeNull;
    private static bool AllocationLessOrEqual(AllocationState current, AllocationState other) => current == other || other == AllocationState.Unknown;
    private static AllocationState JoinAllocation(AllocationState left, AllocationState right) => left == right ? left : AllocationState.Unknown;
    private static bool EscapeLessOrEqual(EscapeState current, EscapeState other) => current == other || other == EscapeState.Unknown || (current == EscapeState.NoEscape && other == EscapeState.MethodEscape);
    private static EscapeState JoinEscape(EscapeState left, EscapeState right) => left == right ? left : EscapeState.Unknown;
    private static bool AliasLessOrEqual(AliasState current, AliasState other) => current == other || other == AliasState.Unknown || current == AliasState.NoAlias && other == AliasState.MayAlias;
    private static AliasState JoinAlias(AliasState left, AliasState right) => left == right ? left : AliasState.Unknown;
    private static bool ConcurrencyLessOrEqual(ConcurrencyState current, ConcurrencyState other) => current == other || other == ConcurrencyState.Unknown;
    private static ConcurrencyState JoinConcurrency(ConcurrencyState left, ConcurrencyState right) => left == right ? left : ConcurrencyState.Unknown;
}
