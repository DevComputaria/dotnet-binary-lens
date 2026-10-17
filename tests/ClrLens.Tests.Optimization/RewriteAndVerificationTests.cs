using System.Security.Cryptography;
using ClrLens.Rewriter;
using ClrLens.SampleFixtures;
using ClrLens.Verification;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace ClrLens.Tests.Optimization;

public sealed class RewriteAndVerificationTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), "clrlens-tests-" + Guid.NewGuid().ToString("N"));

    public RewriteAndVerificationTests() => Directory.CreateDirectory(temporaryDirectory);

    [Fact]
    public void Rewriter_folds_constants_preserves_input_and_emits_evidence()
    {
        var input = CreateFixture();
        var output = Path.Combine(temporaryDirectory, "rewritten.dll");
        var originalHash = Hash(input);

        var result = new CecilAssemblyWriter().Rewrite(input, output, new RewriteOptions { ReadSymbols = false });

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(output));
        Assert.Equal(originalHash, Hash(input));
        Assert.NotEqual(originalHash, Hash(output));
        Assert.Contains(result.Diff, diff => diff.Rule == RewriteRule.ConstantFolding);
        Assert.Contains(result.Diff, diff => diff.Rule == RewriteRule.ConstantPropagation);
        Assert.All(result.Proof.Transformations, proof => Assert.Equal(ProofStatus.Proven, proof.Status));
        Assert.Equal(originalHash, result.Proof.InputSha256);

        using var rewritten = AssemblyDefinition.ReadAssembly(output);
        var instructions = rewritten.MainModule.Types.Single(type => type.Name == "Fixture").Methods.Single(method => method.Name == "Fold").Body.Instructions;
        Assert.Contains(instructions, instruction => instruction.OpCode.Code == Code.Ldc_I4 && (int)instruction.Operand == 5);
        Assert.DoesNotContain(instructions, instruction => instruction.OpCode.Code == Code.Add);
        Assert.DoesNotContain(instructions, instruction => instruction.OpCode.Code == Code.Nop);

        var propagated = rewritten.MainModule.Types.Single(type => type.Name == "Fixture").Methods.Single(method => method.Name == "Propagate").Body.Instructions;
        Assert.Contains(propagated, instruction => instruction.OpCode.Code == Code.Ldc_I4 && (int)instruction.Operand == 6);
        Assert.DoesNotContain(propagated, instruction => instruction.OpCode.Code == Code.Add);
    }

    [Fact]
    public void Rewriter_never_overwrites_input_and_blocks_unknown_effects()
    {
        var input = CreateFixture();
        var writer = new CecilAssemblyWriter();
        Assert.Throws<InvalidOperationException>(() => writer.Rewrite(input, input));

        var output = Path.Combine(temporaryDirectory, "blocked.dll");
        var result = writer.Rewrite(input, output, new RewriteOptions { HasUnknownEffects = true });

        Assert.False(result.Succeeded);
        Assert.True(result.Proof.UnknownEffectsBlocked);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void Rewriter_preserves_portable_symbols_when_available()
    {
        var input = typeof(LoopPatterns).Assembly.Location;
        Assert.True(File.Exists(Path.ChangeExtension(input, ".pdb")));
        var output = Path.Combine(temporaryDirectory, "symbols.dll");

        var result = new CecilAssemblyWriter().Rewrite(input, output);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(Path.ChangeExtension(output, ".pdb")));
    }

    [Fact]
    public void Translation_validator_proves_equivalence_and_returns_counterexample()
    {
        var x = new BitVectorVariable("x", 32);
        var zero = new BitVectorConstant(0, 32);
        var one = new BitVectorConstant(1, 32);
        var validator = new Z3TranslationValidator();

        var proven = validator.Prove(Request(new BitVectorBinary(BitVectorOperation.Add, x, zero), x));
        var rejected = validator.Prove(Request(new BitVectorBinary(BitVectorOperation.Add, x, one), x));
        var timedOut = validator.Prove(Request(x, x) with { Timeout = TimeSpan.Zero });
        var maximum = new BitVectorConstant(int.MaxValue, 32);
        var checkedAdd = new BitVectorBinary(BitVectorOperation.CheckedSignedAdd, maximum, one);
        var uncheckedAdd = new BitVectorBinary(BitVectorOperation.Add, maximum, one);
        var overflow = validator.Prove(Request(checkedAdd, uncheckedAdd));

        Assert.Equal(ProofStatus.Proven, proven.Status);
        Assert.Equal(ProofStatus.Counterexample, rejected.Status);
        Assert.NotNull(rejected.Counterexample);
        Assert.Contains("x", rejected.Counterexample.Inputs.Keys);
        Assert.Equal(ProofStatus.Unknown, timedOut.Status);
        Assert.Equal(ProofStatus.Counterexample, overflow.Status);
        Assert.Contains("throw", overflow.Counterexample!.OriginalResult, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validation_gates_publication_and_can_compare_returns()
    {
        var input = CreateFixture();
        var rewritten = Path.Combine(temporaryDirectory, "pending.dll");
        new CecilAssemblyWriter().Rewrite(input, rewritten, new RewriteOptions { ReadSymbols = false });
        var invocation = new DifferentialInvocation("Fixture", "Fold", []);

        var passed = await new AssemblyValidationPipeline(new StubIlVerifier(true)).ValidateAsync(input, rewritten, differentialInvocations: [invocation]);
        Assert.True(passed.Approved);
        Assert.Equal(GateStatus.Passed, passed.IlVerification);
        Assert.Equal(GateStatus.Passed, passed.ApiCompatibility);
        Assert.Equal(GateStatus.Passed, passed.Differential);

        var published = Path.Combine(temporaryDirectory, "published.dll");
        Assert.Equal(published, ValidatedAssemblyPublisher.Publish(rewritten, published, passed));
        Assert.True(File.Exists(published));

        var failed = await new AssemblyValidationPipeline(new StubIlVerifier(false)).ValidateAsync(input, rewritten);
        Assert.False(failed.Approved);
        Assert.Throws<InvalidOperationException>(() => ValidatedAssemblyPublisher.Publish(rewritten, Path.Combine(temporaryDirectory, "rejected.dll"), failed));
    }

    [Fact]
    public async Task Verified_pipeline_publishes_only_when_semantic_proof_passes()
    {
        var input = CreateFixture();
        var x = new BitVectorVariable("x", 32);
        var zero = new BitVectorConstant(0, 32);
        var one = new BitVectorConstant(1, 32);
        var pipeline = new VerifiedRewritePipeline(new CecilAssemblyWriter(), new StubIlVerifier(true));

        var approvedPath = Path.Combine(temporaryDirectory, "approved.dll");
        var approved = await pipeline.RewriteValidatePublishAsync(
            input,
            approvedPath,
            new RewriteOptions { ReadSymbols = false },
            semanticValidations: [Request(new BitVectorBinary(BitVectorOperation.Add, x, zero), x)],
            differentialInvocations: [new DifferentialInvocation("Fixture", "Fold", [])]);
        Assert.True(approved.Succeeded);
        Assert.True(File.Exists(approvedPath));
        Assert.Equal(GateStatus.Passed, approved.Validation!.SemanticProof);

        var rejectedPath = Path.Combine(temporaryDirectory, "counterexample.dll");
        var proofPath = Path.Combine(temporaryDirectory, "counterexample-proof.json");
        var rejected = await pipeline.RewriteValidatePublishAsync(
            input,
            rejectedPath,
            new RewriteOptions { ReadSymbols = false },
            semanticValidations: [Request(new BitVectorBinary(BitVectorOperation.Add, x, one), x)],
            proofPath: proofPath);
        Assert.False(rejected.Succeeded);
        Assert.Equal(GateStatus.Failed, rejected.Validation!.SemanticProof);
        Assert.False(File.Exists(rejectedPath));
        Assert.True(File.Exists(proofPath));
        Assert.Contains(rejected.Proof.Transformations, item => item.Status == ProofStatus.Counterexample);
    }

    [Fact]
    public async Task IlVerify_accepts_rewritten_fixture_when_tool_is_available()
    {
        var executable = Environment.GetEnvironmentVariable("CLRLENS_ILVERIFY_PATH");
        if (string.IsNullOrWhiteSpace(executable)) return;
        var input = CreateFixture();
        var rewritten = Path.Combine(temporaryDirectory, "ilverify.dll");
        new CecilAssemblyWriter().Rewrite(input, rewritten, new RewriteOptions { ReadSymbols = false });

        var result = await new IlVerifyRunner().VerifyAsync(rewritten, new IlVerifyOptions { ExecutablePath = executable });

        Assert.True(result.Succeeded, result.Output);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
        GC.SuppressFinalize(this);
    }

    private string CreateFixture()
    {
        var path = Path.Combine(temporaryDirectory, "original-" + Guid.NewGuid().ToString("N") + ".dll");
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("RewriteFixture", new Version(1, 0)), "RewriteFixture", ModuleKind.Dll);
        var type = new TypeDefinition(string.Empty, "Fixture", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, assembly.MainModule.TypeSystem.Object);
        assembly.MainModule.Types.Add(type);
        var method = new MethodDefinition("Fold", MethodAttributes.Public | MethodAttributes.Static, assembly.MainModule.TypeSystem.Int32);
        type.Methods.Add(method);
        var processor = method.Body.GetILProcessor();
        processor.Append(processor.Create(OpCodes.Nop));
        processor.Append(processor.Create(OpCodes.Ldc_I4, 2));
        processor.Append(processor.Create(OpCodes.Ldc_I4, 3));
        processor.Append(processor.Create(OpCodes.Add));
        processor.Append(processor.Create(OpCodes.Ret));

        var propagate = new MethodDefinition("Propagate", MethodAttributes.Public | MethodAttributes.Static, assembly.MainModule.TypeSystem.Int32);
        type.Methods.Add(propagate);
        propagate.Body.InitLocals = true;
        var local = new VariableDefinition(assembly.MainModule.TypeSystem.Int32);
        propagate.Body.Variables.Add(local);
        var propagateProcessor = propagate.Body.GetILProcessor();
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Ldc_I4, 4));
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Stloc, local));
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Ldloc, local));
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Ldc_I4, 2));
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Add));
        propagateProcessor.Append(propagateProcessor.Create(OpCodes.Ret));
        assembly.Write(path);
        return path;
    }

    private static TranslationValidationRequest Request(BitVectorExpression original, BitVectorExpression rewritten) =>
        new("test", "Fixture.Fold", [0], ["test precondition"], original, rewritten, TimeSpan.FromSeconds(2));

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private sealed class StubIlVerifier(bool succeeds) : IIlVerifier
    {
        public Task<IlVerificationResult> VerifyAsync(string assemblyPath, IlVerifyOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IlVerificationResult(succeeds, false, succeeds ? 0 : 1, succeeds ? string.Empty : "invalid IL"));
    }
}
