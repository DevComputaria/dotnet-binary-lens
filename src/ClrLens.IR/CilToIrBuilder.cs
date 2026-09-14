using System.Globalization;
using System.Reflection.Emit;
using ClrLens.IL;

namespace ClrLens.IR;

public sealed class CilToIrBuilder
{
    public MethodIr Build(DecodedMethodBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var nodes = new List<IrNode>();
        var diagnostics = body.Diagnostics
            .Select(diagnostic => new IrDiagnostic(diagnostic.Offset, diagnostic.Message, diagnostic.Code is CilDiagnosticCode.InvalidOpcode or CilDiagnosticCode.TruncatedOperand or CilDiagnosticCode.InvalidBranchTarget or CilDiagnosticCode.StackUnderflow or CilDiagnosticCode.StackHeightMismatch))
            .ToList();
        var stack = new Stack<ValueId>();
        var locals = new Dictionary<int, ValueId>();
        var nextValue = 0;

        ValueId NewValue() => new(nextValue++);
        ValueId UnknownValue() => NewValue();

        foreach (var instruction in body.Instructions)
        {
            var op = instruction.OpCode;
            switch (op.Name)
            {
                case "nop":
                    nodes.Add(Node(instruction, IrOperationKind.Unknown, null, [], null, false, false, false));
                    break;
                case "ldc.i4.m1":
                case "ldc.i4.0":
                case "ldc.i4.1":
                case "ldc.i4.2":
                case "ldc.i4.3":
                case "ldc.i4.4":
                case "ldc.i4.5":
                case "ldc.i4.6":
                case "ldc.i4.7":
                case "ldc.i4.8":
                case "ldc.i4.s":
                case "ldc.i4":
                case "ldc.i8":
                case "ldc.r4":
                case "ldc.r8":
                {
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Constant, result, [], instruction.Operand, false, false, false));
                    break;
                }
                case "ldnull":
                {
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Constant, result, [], null, false, false, false));
                    break;
                }
                case "ldarg.0":
                case "ldarg.1":
                case "ldarg.2":
                case "ldarg.3":
                case "ldarg.s":
                case "ldarg":
                {
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.LoadArgument, result, [], instruction.Operand ?? op.Name, false, false, false));
                    break;
                }
                case "ldloc.0":
                case "ldloc.1":
                case "ldloc.2":
                case "ldloc.3":
                case "ldloc.s":
                case "ldloc":
                {
                    var local = LocalIndex(op, instruction.Operand);
                    var result = locals.TryGetValue(local, out var value) ? value : UnknownValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.LoadLocal, result, [], local, false, false, false));
                    break;
                }
                case "stloc.0":
                case "stloc.1":
                case "stloc.2":
                case "stloc.3":
                case "stloc.s":
                case "stloc":
                {
                    var input = Pop(stack, instruction, diagnostics);
                    var local = LocalIndex(op, instruction.Operand);
                    if (input is not null) locals[local] = input.Value;
                    nodes.Add(Node(instruction, IrOperationKind.StoreLocal, null, Values(input), local, false, false, false));
                    break;
                }
                case "dup":
                {
                    var input = stack.Count > 0 ? stack.Peek() : UnknownValue();
                    stack.Push(input);
                    nodes.Add(Node(instruction, IrOperationKind.Unknown, input, [input], null, false, false, false));
                    break;
                }
                case "pop":
                {
                    var input = Pop(stack, instruction, diagnostics);
                    nodes.Add(Node(instruction, IrOperationKind.StackDiscard, null, Values(input), null, false, true, false));
                    break;
                }
                case "add": case "add.ovf": case "add.ovf.un": case "sub": case "sub.ovf": case "sub.ovf.un":
                case "mul": case "mul.ovf": case "mul.ovf.un": case "div": case "div.un": case "rem": case "rem.un":
                case "and": case "or": case "xor": case "shl": case "shr": case "shr.un":
                {
                    var right = Pop(stack, instruction, diagnostics);
                    var left = Pop(stack, instruction, diagnostics);
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Binary, result, Values(left, right), op.Name, op.Name is "div" or "div.un" or "rem" or "rem.un", false, false));
                    break;
                }
                case "neg": case "not":
                {
                    var input = Pop(stack, instruction, diagnostics);
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Unary, result, Values(input), op.Name, false, false, false));
                    break;
                }
                case "newobj": case "newarr":
                {
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Allocate, result, [], instruction.Operand, true, true, true));
                    break;
                }
                case "box": case "unbox": case "unbox.any": case "castclass": case "isinst":
                {
                    var input = Pop(stack, instruction, diagnostics);
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Box, result, Values(input), instruction.Operand, true, op.Name is "box", op.Name is "box"));
                    break;
                }
                case "ldfld": case "ldflda": case "ldsfld": case "ldsflda":
                {
                    var input = op.Name!.StartsWith("ldfld", StringComparison.Ordinal)
                        ? Pop(stack, instruction, diagnostics)
                        : null;
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.LoadField, result, Values(input), instruction.Operand, true, false, false));
                    break;
                }
                case "stfld": case "stsfld":
                {
                    var value = Pop(stack, instruction, diagnostics);
                    var instance = op.Name == "stfld" ? Pop(stack, instruction, diagnostics) : null;
                    nodes.Add(Node(instruction, IrOperationKind.StoreField, null, Values(instance, value), instruction.Operand, true, true, false));
                    break;
                }
                case "ceq": case "cgt": case "cgt.un": case "clt": case "clt.un":
                {
                    var right = Pop(stack, instruction, diagnostics);
                    var left = Pop(stack, instruction, diagnostics);
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Binary, result, Values(left, right), op.Name, false, false, false));
                    break;
                }
                case "conv.i1": case "conv.i2": case "conv.i4": case "conv.i8": case "conv.u1": case "conv.u2":
                case "conv.u4": case "conv.u8": case "conv.i": case "conv.u": case "conv.r4": case "conv.r8":
                {
                    var input = Pop(stack, instruction, diagnostics);
                    var result = NewValue();
                    stack.Push(result);
                    nodes.Add(Node(instruction, IrOperationKind.Unary, result, Values(input), op.Name, true, false, false));
                    break;
                }
                case "call": case "callvirt": case "calli":
                {
                    var result = op.StackBehaviourPush == StackBehaviour.Varpush ? NewValue() : (ValueId?)null;
                    if (result is not null) stack.Push(result.Value);
                    diagnostics.Add(new IrDiagnostic(instruction.Offset, $"Signature-aware stack behavior required for {op.Name}."));
                    nodes.Add(Node(instruction, IrOperationKind.Call, result, [], instruction.Operand, true, true, false));
                    break;
                }
                case "br": case "br.s": case "leave": case "leave.s":
                    nodes.Add(Node(instruction, IrOperationKind.Branch, null, [], instruction.Operand, false, true, false));
                    break;
                case "brtrue": case "brtrue.s": case "brfalse": case "brfalse.s":
                    nodes.Add(Node(instruction, IrOperationKind.ConditionalBranch, null, Values(Pop(stack, instruction, diagnostics)), instruction.Operand, false, true, false));
                    break;
                case "switch":
                    nodes.Add(Node(instruction, IrOperationKind.Switch, null, Values(Pop(stack, instruction, diagnostics)), instruction.Operand, false, true, false));
                    break;
                case "ret":
                    nodes.Add(Node(instruction, IrOperationKind.Return, null, Values(PopIfPresent(stack)), null, false, true, false));
                    break;
                case "throw": case "rethrow":
                    nodes.Add(Node(instruction, IrOperationKind.Throw, null, Values(PopIfPresent(stack)), null, true, true, false));
                    break;
                default:
                    nodes.Add(Node(instruction, IrOperationKind.Unknown, null, [], instruction.Operand, true, true, false));
                    diagnostics.Add(new IrDiagnostic(instruction.Offset, $"Opcode {op.Name} was preserved as unknown IR."));
                    break;
            }
        }

        return new MethodIr(nodes, locals, diagnostics, body.ExceptionRegions);

        IrNode Node(CilInstruction instruction, IrOperationKind kind, ValueId? result, IReadOnlyList<ValueId> inputs, object? operand, bool canThrow, bool sideEffect, bool allocation) =>
            new(instruction.Offset, kind, result, inputs, operand, null, instruction.OpCode, canThrow, sideEffect, allocation);
    }

    private static int LocalIndex(OpCode opCode, object? operand) => opCode.Name switch
    {
        "ldloc.0" or "stloc.0" => 0,
        "ldloc.1" or "stloc.1" => 1,
        "ldloc.2" or "stloc.2" => 2,
        "ldloc.3" or "stloc.3" => 3,
        _ => Convert.ToInt32(operand ?? 0, CultureInfo.InvariantCulture)
    };

    private static ValueId? Pop(Stack<ValueId> stack, CilInstruction instruction, List<IrDiagnostic> diagnostics)
    {
        if (stack.Count == 0)
        {
            diagnostics.Add(new IrDiagnostic(instruction.Offset, $"IR stack underflow at {instruction.OpCode.Name}.", true));
            return null;
        }
        return stack.Pop();
    }

    private static ValueId? PopIfPresent(Stack<ValueId> stack) => stack.Count == 0 ? null : stack.Pop();
    private static ValueId[] Values(params ValueId?[] values) => values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
}
