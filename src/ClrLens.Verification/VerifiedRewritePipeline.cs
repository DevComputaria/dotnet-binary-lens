using ClrLens.Rewriter;

namespace ClrLens.Verification;

public sealed record VerifiedRewriteResult(
    RewriteResult Rewrite,
    RewriteProof Proof,
    AssemblyValidationResult? Validation,
    string? PublishedPath)
{
    public bool Succeeded => Rewrite.Succeeded && Validation?.Approved == true && PublishedPath is not null;
}

public sealed class VerifiedRewritePipeline
{
    private readonly IAssemblyWriter writer;
    private readonly AssemblyValidationPipeline validation;
    private readonly TranslationValidationPipeline translationValidation;

    public VerifiedRewritePipeline(IAssemblyWriter writer, IIlVerifier ilVerifier, ISemanticProver? semanticProver = null)
    {
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        validation = new AssemblyValidationPipeline(ilVerifier ?? throw new ArgumentNullException(nameof(ilVerifier)));
        translationValidation = new TranslationValidationPipeline(semanticProver ?? new Z3TranslationValidator());
    }

    public async Task<VerifiedRewriteResult> RewriteValidatePublishAsync(
        string inputPath,
        string destinationPath,
        RewriteOptions? rewriteOptions = null,
        IlVerifyOptions? ilVerifyOptions = null,
        IReadOnlyList<TranslationValidationRequest>? semanticValidations = null,
        IReadOnlyList<DifferentialInvocation>? differentialInvocations = null,
        string? proofPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var input = Path.GetFullPath(inputPath);
        var destination = Path.GetFullPath(destinationPath);
        if (StringComparer.OrdinalIgnoreCase.Equals(input, destination))
            throw new InvalidOperationException("The verified output must not overwrite the input assembly.");
        if (File.Exists(destination)) throw new IOException($"Destination already exists: {destination}");
        if (proofPath is not null)
        {
            var fullProofPath = Path.GetFullPath(proofPath);
            if (StringComparer.OrdinalIgnoreCase.Equals(fullProofPath, destination))
                throw new InvalidOperationException("Proof and assembly output paths must differ.");
            if (File.Exists(fullProofPath)) throw new IOException($"Proof output already exists: {fullProofPath}");
        }

        var directory = Path.GetDirectoryName(destination) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);
        var pending = Path.Combine(directory, $".{Path.GetFileNameWithoutExtension(destination)}.pending-{Guid.NewGuid():N}.dll");
        var pendingPdb = Path.ChangeExtension(pending, ".pdb");
        try
        {
            var rewrite = writer.Rewrite(input, pending, rewriteOptions);
            var proof = semanticValidations is { Count: > 0 }
                ? translationValidation.ValidateAndAttach(rewrite.Proof, semanticValidations, cancellationToken)
                : rewrite.Proof;
            if (proofPath is not null) proof.WriteJson(proofPath);
            if (!rewrite.Succeeded) return new VerifiedRewriteResult(rewrite, proof, null, null);

            var validationResult = await validation.ValidateAsync(
                input,
                pending,
                ilVerifyOptions,
                differentialInvocations,
                proof,
                cancellationToken).ConfigureAwait(false);
            if (!validationResult.Approved) return new VerifiedRewriteResult(rewrite with { OutputPath = null }, proof, validationResult, null);

            var published = ValidatedAssemblyPublisher.Publish(pending, destination, validationResult);
            return new VerifiedRewriteResult(rewrite with { OutputPath = published }, proof, validationResult, published);
        }
        finally
        {
            if (File.Exists(pending)) File.Delete(pending);
            if (File.Exists(pendingPdb)) File.Delete(pendingPdb);
        }
    }
}
