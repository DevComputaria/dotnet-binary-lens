using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ClrLens.IL;

public sealed class CilDecoder
{
    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = BuildOpcodeMap();

    public DecodedMethodBody Decode(PEReader peReader, MethodDefinition method)
    {
        ArgumentNullException.ThrowIfNull(peReader);
        var diagnostics = new List<CilDiagnostic>();
        if (method.RelativeVirtualAddress == 0)
            return new DecodedMethodBody(0, 0, false, [], [], [new(CilDiagnosticCode.InvalidMethodBody, 0, "Method has no body.")]);

        try
        {
            var body = peReader.GetMethodBody(method.RelativeVirtualAddress);
            var instructions = DecodeInstructions(body.GetILBytes() ?? Array.Empty<byte>(), diagnostics);
            var offsets = instructions.Select(instruction => instruction.Offset).ToHashSet();
            ValidateBranchTargets(instructions, offsets, diagnostics);
            ValidateStack(instructions, diagnostics);
            var regions = body.ExceptionRegions.Select(region => new CilExceptionRegion(
                region.Kind,
                region.TryOffset,
                region.TryLength,
                region.HandlerOffset,
                region.HandlerLength,
                region.FilterOffset,
                region.CatchType)).ToArray();

            return new DecodedMethodBody(
                method.RelativeVirtualAddress,
                body.MaxStack,
                body.LocalSignature.IsNil == false,
                instructions,
                regions,
                diagnostics);
        }
        catch (BadImageFormatException exception)
        {
            diagnostics.Add(new(CilDiagnosticCode.InvalidMethodBody, 0, exception.Message));
            return new DecodedMethodBody(method.RelativeVirtualAddress, 0, false, [], [], diagnostics);
        }
    }

    private static List<CilInstruction> DecodeInstructions(byte[] il, List<CilDiagnostic> diagnostics)
    {
        var instructions = new List<CilInstruction>();
        var offset = 0;
        while (offset < il.Length)
        {
            var start = offset;
            if (!TryReadOpcode(il, ref offset, out var opCode))
            {
                diagnostics.Add(new(CilDiagnosticCode.InvalidOpcode, start, "Unknown CIL opcode."));
                break;
            }

            if (!TryReadOperand(il, ref offset, opCode.OperandType, out var operand, out var targets))
            {
                diagnostics.Add(new(CilDiagnosticCode.TruncatedOperand, start, $"Operand for {opCode.Name} is truncated."));
                break;
            }

            instructions.Add(new CilInstruction(start, opCode, operand, offset - start, targets));
        }

        return instructions;
    }

    private static bool TryReadOpcode(byte[] il, ref int offset, out OpCode opCode)
    {
        opCode = default;
        if (offset >= il.Length) return false;
        ushort value = il[offset++];
        if (value == 0xFE)
        {
            if (offset >= il.Length) return false;
            value = (ushort)(0xFE00 | il[offset++]);
        }
        return OpCodesByValue.TryGetValue(value, out opCode);
    }

    private static bool TryReadOperand(byte[] il, ref int offset, OperandType type, out object? operand, out IReadOnlyList<int> targets)
    {
        operand = null;
        var branchTargets = new List<int>();
        targets = branchTargets;
        int start = offset;

        try
        {
            switch (type)
            {
                case OperandType.InlineNone: return true;
                case OperandType.ShortInlineI: operand = (sbyte)il[offset++]; return true;
                case OperandType.InlineI: operand = ReadInt32(il, ref offset); return true;
                case OperandType.InlineI8: operand = ReadInt64(il, ref offset); return true;
                case OperandType.ShortInlineR: operand = ReadSingle(il, ref offset); return true;
                case OperandType.InlineR: operand = ReadDouble(il, ref offset); return true;
                case OperandType.ShortInlineVar: operand = il[offset++]; return true;
                case OperandType.InlineVar: operand = ReadUInt16(il, ref offset); return true;
                case OperandType.InlineString:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    operand = ReadInt32(il, ref offset); return true;
                case OperandType.ShortInlineBrTarget:
                {
                    var delta = (sbyte)il[offset++];
                    var target = offset + delta;
                    operand = target;
                    branchTargets.Add(target);
                    return true;
                }
                case OperandType.InlineBrTarget:
                {
                    var delta = ReadInt32(il, ref offset);
                    var target = offset + delta;
                    operand = target;
                    branchTargets.Add(target);
                    return true;
                }
                case OperandType.InlineSwitch:
                {
                    var count = ReadInt32(il, ref offset);
                    if (count < 0 || count > il.Length) return false;
                    var deltas = new int[count];
                    for (var i = 0; i < count; i++) deltas[i] = ReadInt32(il, ref offset);
                    var baseOffset = offset;
                    var switchTargets = deltas.Select(delta => baseOffset + delta).ToArray();
                    operand = switchTargets;
                    branchTargets.AddRange(switchTargets);
                    return true;
                }
                default: return false;
            }
        }
        catch (IndexOutOfRangeException)
        {
            offset = start;
            return false;
        }
    }

    private static void ValidateBranchTargets(IEnumerable<CilInstruction> instructions, HashSet<int> offsets, List<CilDiagnostic> diagnostics)
    {
        foreach (var instruction in instructions)
            foreach (var target in instruction.BranchTargets)
                if (!offsets.Contains(target))
                    diagnostics.Add(new(CilDiagnosticCode.InvalidBranchTarget, instruction.Offset, $"Branch target {target} is not an instruction boundary."));
    }

    private static void ValidateStack(IReadOnlyList<CilInstruction> instructions, List<CilDiagnostic> diagnostics)
    {
        var heights = new Dictionary<int, int> { [0] = 0 };
        var work = new Queue<CilInstruction>(instructions.Where(instruction => instruction.Offset == 0));
        var byOffset = instructions.ToDictionary(instruction => instruction.Offset);

        while (work.Count > 0)
        {
            var instruction = work.Dequeue();
            var height = heights[instruction.Offset];
            if (!TryGetStackDelta(instruction.OpCode, out var pop, out var push))
            {
                diagnostics.Add(new(CilDiagnosticCode.VariableStackBehavior, instruction.Offset, $"Stack behavior for {instruction.OpCode.Name} requires signature-aware validation."));
                continue;
            }
            if (height < pop)
            {
                diagnostics.Add(new(CilDiagnosticCode.StackUnderflow, instruction.Offset, $"{instruction.OpCode.Name} pops {pop} values from stack height {height}."));
                continue;
            }

            var nextHeight = height - pop + push;
            foreach (var successor in Successors(instruction, instructions))
            {
                if (!byOffset.ContainsKey(successor)) continue;
                if (heights.TryGetValue(successor, out var existing))
                {
                    if (existing != nextHeight)
                        diagnostics.Add(new(CilDiagnosticCode.StackHeightMismatch, successor, $"Stack height {existing} conflicts with incoming height {nextHeight}."));
                }
                else
                {
                    heights[successor] = nextHeight;
                    work.Enqueue(byOffset[successor]);
                }
            }
        }
    }

    private static IEnumerable<int> Successors(CilInstruction instruction, IReadOnlyList<CilInstruction> instructions)
    {
        if (instruction.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget or OperandType.InlineSwitch)
            foreach (var target in instruction.BranchTargets) yield return target;

        if (instruction.OpCode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw))
        {
            var next = instructions.FirstOrDefault(candidate => candidate.Offset > instruction.Offset);
            if (next is not null) yield return next.Offset;
        }
    }

    private static bool TryGetStackDelta(OpCode opCode, out int pop, out int push)
    {
        if (opCode.StackBehaviourPop == StackBehaviour.Varpop || opCode.StackBehaviourPush == StackBehaviour.Varpush)
        {
            pop = push = 0;
            return false;
        }
        pop = StackCount(opCode.StackBehaviourPop);
        push = StackCount(opCode.StackBehaviourPush);
        return true;
    }

    private static int StackCount(StackBehaviour behavior) => behavior switch
    {
        StackBehaviour.Pop0 or StackBehaviour.Push0 => 0,
        StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref or StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushr4 or StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1,
        StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi or StackBehaviour.Push1_push1 or StackBehaviour.Pushi8 => 2,
        StackBehaviour.Popref_popi_pop1 or StackBehaviour.Popref_popi_popi or StackBehaviour.Popref_popi_popi8 or StackBehaviour.Popref_popi_popr4 or StackBehaviour.Popref_popi_popr8 or StackBehaviour.Popref_popi_popref => 3,
        _ => 0
    };

    private static Dictionary<ushort, OpCode> BuildOpcodeMap()
    {
        var result = new Dictionary<ushort, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            if (field.FieldType == typeof(OpCode) && field.GetValue(null) is OpCode opCode)
                result[(ushort)opCode.Value] = opCode;
        return result;
    }

    private static int ReadInt32(byte[] il, ref int offset) { var value = BitConverter.ToInt32(il, offset); offset += 4; return value; }
    private static long ReadInt64(byte[] il, ref int offset) { var value = BitConverter.ToInt64(il, offset); offset += 8; return value; }
    private static ushort ReadUInt16(byte[] il, ref int offset) { var value = BitConverter.ToUInt16(il, offset); offset += 2; return value; }
    private static float ReadSingle(byte[] il, ref int offset) { var value = BitConverter.ToSingle(il, offset); offset += 4; return value; }
    private static double ReadDouble(byte[] il, ref int offset) { var value = BitConverter.ToDouble(il, offset); offset += 8; return value; }
}
