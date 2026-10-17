namespace ClrLens.Verification;

public abstract record BitVectorExpression(int Width)
{
    protected static void ValidateWidth(int width)
    {
        if (width is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(width), "Bit-vector width must be between 1 and 64.");
    }
}

public sealed record BitVectorVariable : BitVectorExpression
{
    public BitVectorVariable(string name, int width) : base(width)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateWidth(width);
        Name = name;
    }

    public string Name { get; }
}

public sealed record BitVectorConstant : BitVectorExpression
{
    public BitVectorConstant(ulong value, int width) : base(width)
    {
        ValidateWidth(width);
        Value = value;
    }

    public ulong Value { get; }
}

public enum BitVectorOperation
{
    Add,
    CheckedSignedAdd,
    CheckedUnsignedAdd,
    Subtract,
    CheckedSignedSubtract,
    CheckedUnsignedSubtract,
    Multiply,
    CheckedSignedMultiply,
    CheckedUnsignedMultiply,
    SignedDivide,
    UnsignedDivide,
    SignedRemainder,
    UnsignedRemainder,
    And,
    Or,
    Xor,
    ShiftLeft,
    ArithmeticShiftRight,
    LogicalShiftRight
}

public sealed record BitVectorBinary : BitVectorExpression
{
    public BitVectorBinary(BitVectorOperation operation, BitVectorExpression left, BitVectorExpression right) : base(left.Width)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Width != right.Width) throw new ArgumentException("Bit-vector operands must have equal widths.", nameof(right));
        Operation = operation;
        Left = left;
        Right = right;
    }

    public BitVectorOperation Operation { get; }
    public BitVectorExpression Left { get; }
    public BitVectorExpression Right { get; }
}

public sealed record BitVectorNot : BitVectorExpression
{
    public BitVectorNot(BitVectorExpression operand) : base(operand.Width)
    {
        ArgumentNullException.ThrowIfNull(operand);
        Operand = operand;
    }

    public BitVectorExpression Operand { get; }
}
