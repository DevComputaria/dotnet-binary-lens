using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClrLens.Rewriter;

public sealed class CecilAssemblyWriter : IAssemblyWriter
{
    public RewriteResult Rewrite(string inputPath, string outputPath, RewriteOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        options ??= new RewriteOptions();

        var inputFullPath = Path.GetFullPath(inputPath);
        var outputFullPath = Path.GetFullPath(outputPath);
        if (StringComparer.OrdinalIgnoreCase.Equals(inputFullPath, outputFullPath))
            throw new InvalidOperationException("The rewritten assembly must not overwrite the input assembly.");
        if (!File.Exists(inputFullPath))
            throw new FileNotFoundException("Input assembly was not found.", inputFullPath);
        if (File.Exists(outputFullPath))
            throw new IOException($"Output already exists: {outputFullPath}");

        var inputHash = Hash(inputFullPath);
        var diagnostics = new List<string>();
        var proofs = new List<TransformationProof>();
        var diffs = new List<IlDiff>();

        if (IsReadyToRun(inputFullPath))
        {
            diagnostics.Add("Rewrite blocked for ReadyToRun images; rebuild or strip R2R explicitly first.");
            return Failure(inputHash, StrongNameStatus.NotSigned, ReadyToRunStatus.Blocked, false, diagnostics, proofs, diffs);
        }

        var pdbPath = Path.ChangeExtension(inputFullPath, ".pdb");
        var readSymbols = options.ReadSymbols && File.Exists(pdbPath);
        using var assembly = AssemblyDefinition.ReadAssembly(inputFullPath, new ReaderParameters
        {
            InMemory = true,
            ReadSymbols = readSymbols,
            ReadingMode = ReadingMode.Immediate
        });

        var signed = assembly.Name.HasPublicKey;
        if (options.HasUnknownEffects)
        {
            diagnostics.Add("Rewrite blocked because the candidate region contains UnknownEffect.");
            return Failure(inputHash, signed ? StrongNameStatus.Blocked : StrongNameStatus.NotSigned, ReadyToRunStatus.NotReadyToRun, true, diagnostics, proofs, diffs);
        }
        if (signed && options.StrongNameKeyBlob is null)
        {
            diagnostics.Add("Rewrite blocked because the assembly is strong-named and no explicit signing key was supplied.");
            return Failure(inputHash, StrongNameStatus.Blocked, ReadyToRunStatus.NotReadyToRun, false, diagnostics, proofs, diffs);
        }

        foreach (var module in assembly.Modules)
        foreach (var type in AllTypes(module.Types))
        foreach (var method in type.Methods.Where(method => method.HasBody))
            RewriteMethod(method, options.Rules, diffs, proofs, diagnostics);

        var outputDirectory = Path.GetDirectoryName(outputFullPath);
        if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);
        var writerParameters = new WriterParameters { WriteSymbols = readSymbols };
        if (signed) writerParameters.StrongNameKeyBlob = options.StrongNameKeyBlob;

        var temporaryPath = outputFullPath + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var temporaryPdbPath = Path.ChangeExtension(temporaryPath, ".pdb");
        var outputPdbPath = Path.ChangeExtension(outputFullPath, ".pdb");
        if (readSymbols && File.Exists(outputPdbPath)) throw new IOException($"Output symbols already exist: {outputPdbPath}");
        try
        {
            assembly.Write(temporaryPath, writerParameters);
            File.Move(temporaryPath, outputFullPath);
            if (readSymbols && File.Exists(temporaryPdbPath)) File.Move(temporaryPdbPath, outputPdbPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            if (File.Exists(temporaryPdbPath)) File.Delete(temporaryPdbPath);
        }

        var proof = new RewriteProof(
            inputHash,
            Hash(outputFullPath),
            DateTimeOffset.UtcNow,
            proofs,
            signed ? StrongNameStatus.Resigned : StrongNameStatus.NotSigned,
            ReadyToRunStatus.NotReadyToRun,
            false,
            diagnostics);
        return new RewriteResult(true, outputFullPath, diffs, proof);
    }

    private static void RewriteMethod(
        MethodDefinition method,
        IReadOnlySet<RewriteRule> enabled,
        List<IlDiff> diffs,
        List<TransformationProof> proofs,
        List<string> diagnostics)
    {
        if (method.Body.HasExceptionHandlers)
        {
            diagnostics.Add($"Skipped {method.FullName}: exception regions are preserved conservatively.");
            return;
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            if (enabled.Contains(RewriteRule.ConstantPropagation))
                changed |= PropagateConstants(method, diffs, proofs);
            if (enabled.Contains(RewriteRule.ConstantFolding))
                changed |= FoldConstants(method, diffs, proofs);
            if (enabled.Contains(RewriteRule.BranchFolding))
                changed |= FoldBranches(method, diffs, proofs);
            if (enabled.Contains(RewriteRule.RedundantLocalCopyRemoval))
                changed |= RemoveRedundantCopies(method, diffs, proofs);
        }

        if (enabled.Contains(RewriteRule.UnreachableCodeRemoval))
            RemoveUnreachable(method, diffs, proofs);
        if (enabled.Contains(RewriteRule.NopRemoval))
            RemoveNops(method, diffs, proofs);
    }

    private static bool PropagateConstants(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        var targets = BranchTargets(method);
        for (var index = 0; index + 2 < instructions.Count; index++)
        {
            var constant = instructions[index];
            var store = instructions[index + 1];
            var load = instructions[index + 2];
            if (targets.Contains(store) || targets.Contains(load) || !TryInt32(constant, out var value)) continue;
            if (!TryLocal(store, false, out var stored) || !TryLocal(load, true, out var loaded) || stored != loaded) continue;
            var before = Format(load);
            var offset = load.Offset;
            SetInt32(load, value);
            AddEvidence(method, RewriteRule.ConstantPropagation, [offset], before, Format(load),
                ["A constant is stored and immediately reloaded from the same local.", "No control-flow edge enters between the store and load."], diffs, proofs);
            return true;
        }
        return false;
    }

    private static bool FoldConstants(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        var targets = BranchTargets(method);
        for (var index = 0; index + 2 < instructions.Count; index++)
        {
            var left = instructions[index];
            var right = instructions[index + 1];
            var operation = instructions[index + 2];
            if (targets.Contains(right) || targets.Contains(operation) || !TryInt32(left, out var a) || !TryInt32(right, out var b)) continue;
            if (!TryFold(operation.OpCode.Code, a, b, out var result)) continue;

            var before = $"{Format(left)} | {Format(right)} | {Format(operation)}";
            SetInt32(left, result);
            instructions.Remove(right);
            instructions.Remove(operation);
            var offsets = new[] { left.Offset, right.Offset, operation.Offset };
            AddEvidence(method, RewriteRule.ConstantFolding, offsets, before, Format(left),
                ["Both operands are Int32 constants.", "The operation has no observable side effects.", "Checked overflow and exceptional division are not folded."], diffs, proofs);
            return true;
        }
        return false;
    }

    private static bool FoldBranches(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        var targets = BranchTargets(method);
        for (var index = 0; index + 1 < instructions.Count; index++)
        {
            var constant = instructions[index];
            var branch = instructions[index + 1];
            if (targets.Contains(constant) || targets.Contains(branch) || !TryInt32(constant, out var value)) continue;
            if (branch.OpCode.Code is not (Code.Brtrue or Code.Brtrue_S or Code.Brfalse or Code.Brfalse_S)) continue;
            var taken = branch.OpCode.Code is Code.Brtrue or Code.Brtrue_S ? value != 0 : value == 0;
            var before = $"{Format(constant)} | {Format(branch)}";
            var offsets = new[] { constant.Offset, branch.Offset };
            instructions.Remove(constant);
            if (taken)
                branch.OpCode = OpCodes.Br;
            else
                instructions.Remove(branch);
            AddEvidence(method, RewriteRule.BranchFolding, offsets, before, taken ? Format(branch) : "<fallthrough>",
                ["The branch condition is an Int32 constant.", "Neither removed instruction is an external branch target."], diffs, proofs);
            return true;
        }
        return false;
    }

    private static bool RemoveRedundantCopies(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        var targets = BranchTargets(method);
        for (var index = 0; index + 1 < instructions.Count; index++)
        {
            var load = instructions[index];
            var store = instructions[index + 1];
            if (targets.Contains(load) || targets.Contains(store)) continue;
            if (!TryLocal(load, true, out var loaded) || !TryLocal(store, false, out var stored) || loaded != stored) continue;
            var before = $"{Format(load)} | {Format(store)}";
            var offsets = new[] { load.Offset, store.Offset };
            instructions.Remove(load);
            instructions.Remove(store);
            AddEvidence(method, RewriteRule.RedundantLocalCopyRemoval, offsets, before, "<removed>",
                ["The value is loaded and immediately stored to the same local.", "Neither instruction is a branch target."], diffs, proofs);
            return true;
        }
        return false;
    }

    private static void RemoveUnreachable(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        if (instructions.Count == 0) return;
        var reachable = new HashSet<Instruction>();
        var pending = new Stack<Instruction>();
        pending.Push(instructions[0]);
        while (pending.Count > 0)
        {
            var instruction = pending.Pop();
            if (!reachable.Add(instruction)) continue;
            if (instruction.Operand is Instruction target) pending.Push(target);
            if (instruction.Operand is Instruction[] targets)
                foreach (var item in targets) pending.Push(item);
            if (instruction.OpCode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw)
                && instruction.Next is not null) pending.Push(instruction.Next);
        }

        var removed = instructions.Where(instruction => !reachable.Contains(instruction)).ToArray();
        if (removed.Length == 0) return;
        foreach (var instruction in removed) instructions.Remove(instruction);
        AddEvidence(method, RewriteRule.UnreachableCodeRemoval, removed.Select(item => item.Offset).ToArray(),
            string.Join(" | ", removed.Select(Format)), "<removed>",
            ["Instructions are unreachable from the method entry in the control-flow graph.", "The method contains no exception regions."], diffs, proofs);
    }

    private static void RemoveNops(MethodDefinition method, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        var instructions = method.Body.Instructions;
        var targets = BranchTargets(method);
        var removable = instructions.Where(instruction => instruction.OpCode.Code == Code.Nop && !targets.Contains(instruction)).ToArray();
        foreach (var instruction in removable)
        {
            instructions.Remove(instruction);
            AddEvidence(method, RewriteRule.NopRemoval, [instruction.Offset], Format(instruction), "<removed>",
                ["The NOP is not a branch target.", "The method contains no exception regions."], diffs, proofs);
        }
    }

    private static HashSet<Instruction> BranchTargets(MethodDefinition method)
    {
        var result = new HashSet<Instruction>();
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is Instruction target) result.Add(target);
            if (instruction.Operand is Instruction[] targets) result.UnionWith(targets);
        }
        return result;
    }

    private static bool TryFold(Code code, int left, int right, out int result)
    {
        result = 0;
        try
        {
            result = code switch
            {
                Code.Add => unchecked(left + right),
                Code.Sub => unchecked(left - right),
                Code.Mul => unchecked(left * right),
                Code.Div when right != 0 && (left != int.MinValue || right != -1) => left / right,
                Code.Div_Un when right != 0 => unchecked((int)((uint)left / (uint)right)),
                Code.Rem when right != 0 && (left != int.MinValue || right != -1) => left % right,
                Code.Rem_Un when right != 0 => unchecked((int)((uint)left % (uint)right)),
                Code.And => left & right,
                Code.Or => left | right,
                Code.Xor => left ^ right,
                Code.Shl => left << (right & 31),
                Code.Shr => left >> (right & 31),
                Code.Shr_Un => unchecked((int)((uint)left >> (right & 31))),
                _ => throw new InvalidOperationException()
            };
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryInt32(Instruction instruction, out int value)
    {
        value = instruction.OpCode.Code switch
        {
            Code.Ldc_I4_M1 => -1,
            Code.Ldc_I4_0 => 0,
            Code.Ldc_I4_1 => 1,
            Code.Ldc_I4_2 => 2,
            Code.Ldc_I4_3 => 3,
            Code.Ldc_I4_4 => 4,
            Code.Ldc_I4_5 => 5,
            Code.Ldc_I4_6 => 6,
            Code.Ldc_I4_7 => 7,
            Code.Ldc_I4_8 => 8,
            Code.Ldc_I4_S => (sbyte)instruction.Operand,
            Code.Ldc_I4 => (int)instruction.Operand,
            _ => 0
        };
        return instruction.OpCode.Code is >= Code.Ldc_I4_M1 and <= Code.Ldc_I4;
    }

    private static void SetInt32(Instruction instruction, int value)
    {
        instruction.OpCode = OpCodes.Ldc_I4;
        instruction.Operand = value;
    }

    private static bool TryLocal(Instruction instruction, bool load, out int index)
    {
        index = instruction.OpCode.Code switch
        {
            Code.Ldloc_0 or Code.Stloc_0 => 0,
            Code.Ldloc_1 or Code.Stloc_1 => 1,
            Code.Ldloc_2 or Code.Stloc_2 => 2,
            Code.Ldloc_3 or Code.Stloc_3 => 3,
            Code.Ldloc or Code.Ldloc_S or Code.Stloc or Code.Stloc_S when instruction.Operand is VariableDefinition variable => variable.Index,
            _ => -1
        };
        return index >= 0 && (load
            ? instruction.OpCode.Code is Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc or Code.Ldloc_S
            : instruction.OpCode.Code is Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3 or Code.Stloc or Code.Stloc_S);
    }

    private static void AddEvidence(MethodDefinition method, RewriteRule rule, IReadOnlyList<int> offsets, string before, string after,
        IReadOnlyList<string> preconditions, List<IlDiff> diffs, List<TransformationProof> proofs)
    {
        diffs.Add(new IlDiff(method.FullName, rule, offsets, before, after));
        proofs.Add(new TransformationProof(rule.ToString(), method.FullName, offsets, preconditions, ProofStatus.Proven, "Validated local rewrite rule."));
    }

    private static string Format(Instruction instruction) => $"IL_{instruction.Offset:x4}: {instruction.OpCode.Name}{(instruction.Operand is null ? string.Empty : " " + instruction.Operand)}";

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }

    private static bool IsReadyToRun(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);
        return reader.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory.Size > 0;
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static RewriteResult Failure(string hash, StrongNameStatus strongName, ReadyToRunStatus readyToRun, bool unknownEffects,
        IReadOnlyList<string> diagnostics, IReadOnlyList<TransformationProof> proofs, IReadOnlyList<IlDiff> diffs) =>
        new(false, null, diffs, new RewriteProof(hash, null, DateTimeOffset.UtcNow, proofs, strongName, readyToRun, unknownEffects, diagnostics));
}
