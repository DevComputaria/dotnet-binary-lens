using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using ClrLens.Core.Domain;

namespace ClrLens.PE;

public sealed record AssemblyIngestionOptions(
    long MaxFileSizeBytes = 256L * 1024 * 1024,
    int MaxMetadataRows = 2_000_000,
    TimeSpan? Timeout = null);

public enum AssemblyDiagnosticCode
{
    FileNotFound,
    FileTooLarge,
    InvalidPe,
    MissingMetadata,
    MetadataReadFailure,
    UnsupportedFormat,
    Timeout,
    UnexpectedFailure
}

public sealed record AssemblyDiagnostic(
    AssemblyDiagnosticCode Code,
    string Message,
    bool IsError = true);

public sealed record AssemblyModel(
    AssemblyIdentity Identity,
    IReadOnlyList<string> AssemblyReferences,
    int TypeCount,
    int MethodCount,
    bool HasMetadata,
    bool HasPdb,
    bool IsStrongNamed,
    bool IsReadyToRun,
    string FilePath);

public sealed record AssemblyIngestionResult(
    AssemblyModel? Model,
    IReadOnlyList<AssemblyDiagnostic> Diagnostics)
{
    public bool IsSuccess => Model is not null && Diagnostics.All(d => !d.IsError);
}

public sealed class AssemblyReader
{
    public async Task<AssemblyIngestionResult> ReadAsync(
        string filePath,
        AssemblyIngestionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AssemblyIngestionOptions();
        if (string.IsNullOrWhiteSpace(filePath))
            return Failure(AssemblyDiagnosticCode.FileNotFound, "Assembly path is required.");

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return Failure(AssemblyDiagnosticCode.FileNotFound, $"Assembly was not found: {filePath}");
            if (fileInfo.Length > options.MaxFileSizeBytes)
                return Failure(AssemblyDiagnosticCode.FileTooLarge, $"Assembly exceeds the configured limit of {options.MaxFileSizeBytes} bytes.");

            using var timeout = options.Timeout is { } duration
                ? new CancellationTokenSource(duration)
                : null;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout?.Token ?? CancellationToken.None);
            var token = linked.Token;
            token.ThrowIfCancellationRequested();

            var bytes = await File.ReadAllBytesAsync(filePath, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var hash = Convert.ToHexString(SHA256.HashData(bytes));

            using var stream = new MemoryStream(bytes, writable: false);
            using var peReader = new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
            if (!peReader.HasMetadata)
                return Failure(AssemblyDiagnosticCode.MissingMetadata, "The PE image does not contain CLI metadata.");

            var metadata = peReader.GetMetadataReader();
            if (metadata.TypeDefinitions.Count > options.MaxMetadataRows || metadata.MethodDefinitions.Count > options.MaxMetadataRows)
                return Failure(AssemblyDiagnosticCode.MetadataReadFailure, "Metadata row count exceeds the configured limit.");

            var assemblyDefinition = metadata.GetAssemblyDefinition();
            var name = metadata.GetString(assemblyDefinition.Name);
            var version = assemblyDefinition.Version.ToString();
            var targetFramework = ReadTargetFramework(metadata, assemblyDefinition) ?? "unknown";
            var architecture = peReader.PEHeaders.CoffHeader?.Machine.ToString() ?? "unknown";
            var hasPdb = peReader.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.CodeView);
            var corHeader = peReader.PEHeaders.CorHeader;
            var strongNamed = corHeader is not null && corHeader.StrongNameSignatureDirectory.Size > 0;
            var readyToRun = corHeader is not null && corHeader.ManagedNativeHeaderDirectory.Size > 0;
            var references = metadata.AssemblyReferences
                .Select(metadata.GetAssemblyReference)
                .Select(reference => metadata.GetString(reference.Name))
                .Order(StringComparer.Ordinal)
                .ToArray();

            var identity = new AssemblyIdentity(name, hash, targetFramework, architecture, version, hasPdb, strongNamed, readyToRun);
            var model = new AssemblyModel(identity, references, metadata.TypeDefinitions.Count, metadata.MethodDefinitions.Count, true, hasPdb, strongNamed, readyToRun, Path.GetFullPath(filePath));
            return new AssemblyIngestionResult(model, []);
        }
        catch (OperationCanceledException)
        {
            return Failure(AssemblyDiagnosticCode.Timeout, "Assembly ingestion was cancelled or exceeded its timeout.");
        }
        catch (BadImageFormatException exception)
        {
            return Failure(AssemblyDiagnosticCode.InvalidPe, $"The input is not a valid managed PE image: {exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(AssemblyDiagnosticCode.MetadataReadFailure, $"Could not read the assembly: {exception.Message}");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            return Failure(AssemblyDiagnosticCode.MetadataReadFailure, $"Could not read assembly metadata: {exception.Message}");
        }
    }

    private static string? ReadTargetFramework(MetadataReader metadata, AssemblyDefinition assemblyDefinition)
    {
        foreach (var attributeHandle in assemblyDefinition.GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(attributeHandle);
            var attributeType = GetAttributeTypeName(metadata, attribute.Constructor);
            if (!string.Equals(attributeType, "System.Runtime.Versioning.TargetFrameworkAttribute", StringComparison.Ordinal))
                continue;

            var reader = metadata.GetBlobReader(attribute.Value);
            if (reader.ReadUInt16() != 1)
                return null;
            return reader.ReadSerializedString();
        }

        return null;
    }

    private static string? GetAttributeTypeName(MetadataReader metadata, EntityHandle constructor)
    {
        if (constructor.Kind != HandleKind.MemberReference)
            return null;

        var member = metadata.GetMemberReference((MemberReferenceHandle)constructor);
        return member.Parent.Kind switch
        {
            HandleKind.TypeReference => GetTypeReferenceName(metadata, (TypeReferenceHandle)member.Parent),
            HandleKind.TypeDefinition => GetTypeDefinitionName(metadata, (TypeDefinitionHandle)member.Parent),
            _ => null
        };
    }

    private static string GetTypeReferenceName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var reference = metadata.GetTypeReference(handle);
        return CombineName(metadata.GetString(reference.Namespace), metadata.GetString(reference.Name));
    }

    private static string GetTypeDefinitionName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var definition = metadata.GetTypeDefinition(handle);
        return CombineName(metadata.GetString(definition.Namespace), metadata.GetString(definition.Name));
    }

    private static string CombineName(string @namespace, string name) => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

    private static AssemblyIngestionResult Failure(AssemblyDiagnosticCode code, string message) =>
        new(null, [new AssemblyDiagnostic(code, message)]);
}
