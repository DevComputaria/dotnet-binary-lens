namespace ClrLens.SampleFixtures;

public static class LoopPatterns
{
    public static int LinearLoop(int count)
    {
        var total = 0;
        for (var i = 0; i < count; i++) total += i;
        return total;
    }

    public static int NestedLoop(int outer, int inner)
    {
        var total = 0;
        for (var i = 0; i < outer; i++)
            for (var j = 0; j < inner; j++) total += i + j;
        return total;
    }

    public static int Branch(int value) => value > 0 ? value : -value;

    public static int ExceptionFlow(string? value)
    {
        try { return value!.Length; }
        catch (NullReferenceException) { return 0; }
        finally { GC.KeepAlive(value); }
    }

    public static byte[] Allocate(int size) => new byte[size];

    public static object BoxValue(int value) => value;

    public static byte[] MaterializeExternal(Stream input) => ReadAll(input);

    public static List<T> MaterializeGeneric<T>(IEnumerable<T> source) => source.ToList();

    public static Func<int, int> CreateDelegate(int offset) => value => value + offset;

    private static byte[] ReadAll(Stream input)
    {
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        return memory.ToArray();
    }
}

public static class RetentionPatterns
{
    private static readonly List<object> Cache = [];

    public static void Retain(object value) => Cache.Add(value);

    public static int CacheCount => Cache.Count;
}

public sealed class GenericContainer<T>
{
    public required T Value { get; init; }
}

public static class FixtureMarker
{
    public const string Coverage = "loops;eh;branches;allocations;boxing;external-payload;static-retention;generics;delegates";
}
