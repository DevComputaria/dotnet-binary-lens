using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using ClrLens.Rewriter;
using Mono.Cecil;

namespace ClrLens.Verification;

public enum GateStatus
{
    Passed,
    Failed,
    NotRun
}

public sealed record ApiCompatibilityResult(bool Compatible, IReadOnlyList<string> MissingMembers, IReadOnlyList<string> AddedMembers);

public sealed record DifferentialInvocation(
    string TypeName,
    string MethodName,
    object?[] Arguments,
    IReadOnlyList<string>? ObservedStaticFields = null);

public sealed record DifferentialResult(bool Equivalent, string OriginalOutcome, string RewrittenOutcome);

public sealed record AssemblyValidationResult(
    bool Approved,
    GateStatus SemanticProof,
    GateStatus IlVerification,
    GateStatus ApiCompatibility,
    GateStatus Differential,
    string StrongNameStatus,
    string ReadyToRunStatus,
    IReadOnlyList<string> Diagnostics);

public sealed class ApiCompatibilityValidator
{
    public static ApiCompatibilityResult Compare(string originalPath, string rewrittenPath)
    {
        var original = PublicSurface(originalPath);
        var rewritten = PublicSurface(rewrittenPath);
        return new ApiCompatibilityResult(
            original.SetEquals(rewritten),
            original.Except(rewritten, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            rewritten.Except(original, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    private static HashSet<string> PublicSurface(string path)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(path);
        var result = new HashSet<string>(StringComparer.Ordinal)
        {
            $"A:{assembly.Name.Name}:{assembly.Name.Version}:{assembly.Name.Culture}"
        };
        foreach (var type in AllTypes(assembly.MainModule.Types).Where(type => type.IsPublic || type.IsNestedPublic))
        {
            result.Add($"T:{type.FullName}:{type.Attributes}");
            foreach (var method in type.Methods.Where(method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly))
                result.Add($"M:{method.FullName}:{method.Attributes}");
            foreach (var field in type.Fields.Where(field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly))
                result.Add($"F:{field.FullName}:{field.Attributes}:{field.Constant}");
            foreach (var property in type.Properties.Where(property => IsVisible(property.GetMethod) || IsVisible(property.SetMethod)))
                result.Add($"P:{property.FullName}");
            foreach (var @event in type.Events.Where(@event => IsVisible(@event.AddMethod) || IsVisible(@event.RemoveMethod)))
                result.Add($"E:{@event.FullName}");
        }
        return result;
    }

    private static bool IsVisible(MethodDefinition? method) => method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }
}

public sealed class DifferentialHarness
{
    public static DifferentialResult Compare(string originalPath, string rewrittenPath, DifferentialInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        var original = Invoke(originalPath, invocation);
        var rewritten = Invoke(rewrittenPath, invocation);
        return new DifferentialResult(StringComparer.Ordinal.Equals(original, rewritten), original, rewritten);
    }

    private static string Invoke(string path, DifferentialInvocation invocation)
    {
        var loadContext = new AssemblyLoadContext($"ClrLens.Differential.{Guid.NewGuid():N}", true);
        var assemblyDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        loadContext.Resolving += (_, name) =>
        {
            var dependencyPath = Path.Combine(assemblyDirectory, name.Name + ".dll");
            return File.Exists(dependencyPath) ? loadContext.LoadFromAssemblyPath(dependencyPath) : null;
        };
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(path));
            var type = assembly.GetType(invocation.TypeName, true)!;
            var method = type.GetMethod(invocation.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(invocation.TypeName, invocation.MethodName);
            try
            {
                var value = NormalizeReturn(method.Invoke(null, invocation.Arguments));
                return $"return:{value?.GetType().FullName ?? "null"}:{value}{ObserveStaticFields(type, invocation.ObservedStaticFields)}";
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                return $"throw:{exception.InnerException.GetType().FullName}:{exception.InnerException.Message}{ObserveStaticFields(type, invocation.ObservedStaticFields)}";
            }
            catch (Exception exception)
            {
                return $"throw:{exception.GetType().FullName}:{exception.Message}{ObserveStaticFields(type, invocation.ObservedStaticFields)}";
            }
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static object? NormalizeReturn(object? value)
    {
        if (value is Task task)
        {
            task.GetAwaiter().GetResult();
            return task.GetType().IsGenericType ? task.GetType().GetProperty("Result")!.GetValue(task) : null;
        }
        if (value is not null && value.GetType().FullName?.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal) == true)
        {
            var valueTaskResult = (Task)value.GetType().GetMethod("AsTask")!.Invoke(value, null)!;
            valueTaskResult.GetAwaiter().GetResult();
            return valueTaskResult.GetType().IsGenericType ? valueTaskResult.GetType().GetProperty("Result")!.GetValue(valueTaskResult) : null;
        }
        return value;
    }

    private static string ObserveStaticFields(Type type, IReadOnlyList<string>? fieldNames)
    {
        if (fieldNames is not { Count: > 0 }) return string.Empty;
        var values = fieldNames.Select(name =>
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingFieldException(type.FullName, name);
            return $"{name}={field.GetValue(null)}";
        });
        return "; effects:" + string.Join(",", values);
    }
}

public sealed class AssemblyValidationPipeline
{
    private readonly IIlVerifier ilVerifier;

    public AssemblyValidationPipeline(IIlVerifier ilVerifier)
    {
        this.ilVerifier = ilVerifier ?? throw new ArgumentNullException(nameof(ilVerifier));
    }

    public async Task<AssemblyValidationResult> ValidateAsync(
        string originalPath,
        string rewrittenPath,
        IlVerifyOptions? ilVerifyOptions = null,
        IReadOnlyList<DifferentialInvocation>? differentialInvocations = null,
        RewriteProof? proof = null,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<string>();
        var semanticStatus = proof is null ? GateStatus.NotRun : proof.Transformations.All(item => item.Status == ProofStatus.Proven) ? GateStatus.Passed : GateStatus.Failed;
        if (semanticStatus == GateStatus.Failed)
            diagnostics.AddRange(proof!.Transformations.Where(item => item.Status != ProofStatus.Proven)
                .Select(item => $"Semantic proof {item.Status}: {item.Method} / {item.Rule}: {item.Detail}"));
        var il = await ilVerifier.VerifyAsync(rewrittenPath, ilVerifyOptions, cancellationToken).ConfigureAwait(false);
        if (!il.Succeeded) diagnostics.Add(il.Output);

        var api = ApiCompatibilityValidator.Compare(originalPath, rewrittenPath);
        if (!api.Compatible)
        {
            diagnostics.AddRange(api.MissingMembers.Select(member => $"Missing API: {member}"));
            diagnostics.AddRange(api.AddedMembers.Select(member => $"Added API: {member}"));
        }

        var differentialStatus = GateStatus.NotRun;
        if (differentialInvocations is { Count: > 0 })
        {
            differentialStatus = GateStatus.Passed;
            foreach (var invocation in differentialInvocations)
            {
                var result = DifferentialHarness.Compare(originalPath, rewrittenPath, invocation);
                if (result.Equivalent) continue;
                differentialStatus = GateStatus.Failed;
                diagnostics.Add($"Differential mismatch for {invocation.TypeName}.{invocation.MethodName}: {result.OriginalOutcome} != {result.RewrittenOutcome}");
            }
        }

        var strongName = InspectStrongName(originalPath, rewrittenPath, diagnostics);
        var readyToRun = IsReadyToRun(rewrittenPath) ? "ReadyToRun (rewrite must be rejected)" : "IL-only";
        if (IsReadyToRun(rewrittenPath)) diagnostics.Add("Rewritten image still contains a ReadyToRun native header.");
        var approved = semanticStatus != GateStatus.Failed && il.Succeeded && api.Compatible && differentialStatus != GateStatus.Failed && !IsReadyToRun(rewrittenPath) && !strongName.StartsWith("Invalid", StringComparison.Ordinal);
        return new AssemblyValidationResult(
            approved,
            semanticStatus,
            il.Succeeded ? GateStatus.Passed : GateStatus.Failed,
            api.Compatible ? GateStatus.Passed : GateStatus.Failed,
            differentialStatus,
            strongName,
            readyToRun,
            diagnostics);
    }

    private static string InspectStrongName(string originalPath, string rewrittenPath, List<string> diagnostics)
    {
        using var original = AssemblyDefinition.ReadAssembly(originalPath);
        using var rewritten = AssemblyDefinition.ReadAssembly(rewrittenPath);
        if (!original.Name.HasPublicKey) return rewritten.Name.HasPublicKey ? "New public key present" : "Not signed";
        if (!rewritten.Name.HasPublicKey)
        {
            diagnostics.Add("The original strong name public key was removed.");
            return "Invalid: public key removed";
        }
        if (!original.Name.PublicKey.SequenceEqual(rewritten.Name.PublicKey))
        {
            diagnostics.Add("The strong name public key changed.");
            return "Invalid: public key changed";
        }
        using var stream = File.OpenRead(rewrittenPath);
        using var reader = new PEReader(stream);
        if (reader.PEHeaders.CorHeader?.StrongNameSignatureDirectory.Size <= 0)
        {
            diagnostics.Add("The rewritten image has no strong-name signature directory.");
            return "Invalid: signature missing";
        }
        return "Resigned; public key and signature directory preserved";
    }

    private static bool IsReadyToRun(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);
        return reader.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory.Size > 0;
    }
}

public sealed class ValidatedAssemblyPublisher
{
    public static string Publish(string pendingAssemblyPath, string destinationPath, AssemblyValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation.Approved) throw new InvalidOperationException("A failed validation result cannot be published.");
        var pending = Path.GetFullPath(pendingAssemblyPath);
        var destination = Path.GetFullPath(destinationPath);
        if (StringComparer.OrdinalIgnoreCase.Equals(pending, destination)) throw new InvalidOperationException("Pending and destination paths must differ.");
        if (!File.Exists(pending)) throw new FileNotFoundException("Pending assembly was not found.", pending);
        if (File.Exists(destination)) throw new IOException($"Destination already exists: {destination}");
        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var pendingPdb = Path.ChangeExtension(pending, ".pdb");
        var destinationPdb = Path.ChangeExtension(destination, ".pdb");
        if (File.Exists(pendingPdb) && File.Exists(destinationPdb)) throw new IOException($"Destination symbols already exist: {destinationPdb}");
        try
        {
            File.Copy(pending, destination, false);
            if (File.Exists(pendingPdb)) File.Copy(pendingPdb, destinationPdb, false);
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            if (File.Exists(destinationPdb)) File.Delete(destinationPdb);
            throw;
        }
        return destination;
    }
}
