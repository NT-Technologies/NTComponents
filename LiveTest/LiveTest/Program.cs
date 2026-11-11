using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LiveTest.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.AddNTServices();
// LiveTest is a local development host, so everyone may use the debug console. Production apps should set Authorize to a rule such as a developer role.
builder.Services.AddNTDebugConsoleForServer(options => options.AllowAll());
builder.Services.AddScoped(sp => {
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
}
else {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LiveTest.Client._Imports).Assembly);

const long maxUploadFileSizeBytes = 50L * 1024 * 1024;
const long maxMultipartBodyLengthBytes = maxUploadFileSizeBytes + (1024 * 1024);
const int streamBufferSize = 64 * 1024;
const int maxMultipartBoundaryLength = 256;
const int maxFormFieldValueBytes = 128;

var maxConcurrentUploads = Math.Clamp(Environment.ProcessorCount, 2, 16);
var uploadConcurrencyLimiter = new SemaphoreSlim(maxConcurrentUploads, maxConcurrentUploads);
var uploadQueueTimeout = TimeSpan.FromSeconds(20);
var sseKeepAliveInterval = TimeSpan.FromSeconds(15);
var idleSessionTimeout = TimeSpan.FromMinutes(10);
var completedSessionRetention = TimeSpan.FromMinutes(2);
var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(uploadRoot);

var uploadSessions = new ConcurrentDictionary<string, UploadProgressSession>(StringComparer.Ordinal);
var activeUploads = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
var lastCleanupUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

app.MapPost("/api/uploads/process", async (HttpRequest request, CancellationToken cancellationToken) => {
    CleanupExpiredSessions(DateTimeOffset.UtcNow);

    if (!TryGetMultipartBoundary(request.ContentType, out var boundary)) {
        return Fail(StatusCodes.Status400BadRequest, "invalid_content_type", "Expected multipart/form-data with a valid boundary.");
    }

    if (boundary.Length > maxMultipartBoundaryLength) {
        return Fail(StatusCodes.Status400BadRequest, "invalid_boundary", "Multipart boundary is too long.");
    }

    if (request.ContentLength is > maxMultipartBodyLengthBytes) {
        return Fail(StatusCodes.Status413PayloadTooLarge, "payload_too_large", $"Request exceeds {maxMultipartBodyLengthBytes} bytes.");
    }

    var reader = new MultipartReader(boundary, request.Body) {
        HeadersCountLimit = 16,
        HeadersLengthLimit = 16 * 1024,
        BodyLengthLimit = maxUploadFileSizeBytes
    };

    var uploadId = string.Empty;
    var uploadRegistered = false;
    var limiterAcquired = false;
    var hasFile = false;
    var uploadSucceeded = false;
    var uploadStartedAt = DateTimeOffset.UtcNow;
    var declaredFileSizeBytes = 0L;
    var hasDeclaredFileSize = false;
    var bytesProcessed = 0L;
    var lastPercent = 0;
    var storedPath = string.Empty;

    try {
        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section) {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition)) {
                throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_form_data", "Invalid content-disposition header.");
            }

            if (!string.Equals(contentDisposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase)) {
                throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_form_data", "Invalid multipart section disposition.");
            }

            var fieldName = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fieldName)) {
                throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_form_data", "Multipart section is missing a field name.");
            }

            var hasFileName =
                !string.IsNullOrWhiteSpace(HeaderUtilities.RemoveQuotes(contentDisposition.FileName).Value) ||
                !string.IsNullOrWhiteSpace(HeaderUtilities.RemoveQuotes(contentDisposition.FileNameStar).Value);

            if (hasFileName) {
                if (!string.Equals(fieldName, "file", StringComparison.Ordinal)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "unexpected_field", "Only multipart field 'file' is allowed for file content.");
                }

                if (hasFile) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_count", "Exactly one file is required.");
                }

                if (string.IsNullOrEmpty(uploadId)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "missing_upload_id", "uploadId must be provided before file content.");
                }

                hasFile = true;

                if (!hasDeclaredFileSize) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "missing_file_size", "fileSize must be provided before file content.");
                }

                if (declaredFileSizeBytes <= 0) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "empty_file", "Empty files are not allowed.");
                }

                if (declaredFileSizeBytes > maxUploadFileSizeBytes) {
                    throw new UploadValidationException(StatusCodes.Status413PayloadTooLarge, "file_too_large", $"File exceeds {maxUploadFileSizeBytes} bytes.");
                }

                var rawFileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileNameStar).Value;
                if (string.IsNullOrWhiteSpace(rawFileName)) {
                    rawFileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName).Value;
                }

                var originalFileName = SanitizeFileName(rawFileName);
                if (string.IsNullOrWhiteSpace(originalFileName)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_name", "A valid file name is required.");
                }

                var storedFileName = BuildStoredFileName(uploadId, originalFileName);
                storedPath = Path.Combine(uploadRoot, storedFileName);

                await PublishAsync(uploadId, 0, "started", CancellationToken.None, "Server started processing.", bytesProcessed: 0, totalBytes: declaredFileSizeBytes);

                await using var output = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, streamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = ArrayPool<byte>.Shared.Rent(streamBufferSize);

                try {
                    while (true) {
                        var read = await section.Body.ReadAsync(buffer.AsMemory(0, streamBufferSize), cancellationToken);
                        if (read == 0) {
                            break;
                        }

                        bytesProcessed += read;
                        if (bytesProcessed > maxUploadFileSizeBytes) {
                            throw new UploadValidationException(StatusCodes.Status413PayloadTooLarge, "file_too_large", $"File exceeded {maxUploadFileSizeBytes} bytes while streaming.");
                        }

                        if (bytesProcessed > declaredFileSizeBytes) {
                            throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_size", "Uploaded bytes exceed declared fileSize.");
                        }

                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        hash.AppendData(buffer, 0, read);

                        var percent = (int)Math.Clamp((bytesProcessed * 100L) / declaredFileSizeBytes, 0L, 100L);
                        if (percent != lastPercent) {
                            lastPercent = percent;
                            await PublishAsync(uploadId, percent, "progress", CancellationToken.None, bytesProcessed: bytesProcessed, totalBytes: declaredFileSizeBytes);
                        }
                    }
                }
                finally {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
                }

                await output.FlushAsync(cancellationToken);

                if (bytesProcessed <= 0) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "empty_file", "Empty files are not allowed.");
                }

                if (bytesProcessed != declaredFileSizeBytes) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_size", "Uploaded bytes do not match declared fileSize.");
                }

                if (await reader.ReadNextSectionAsync(cancellationToken) is not null) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "unexpected_field", "Only uploadId, fileSize, and one file are allowed.");
                }

                var sha256 = Convert.ToHexString(hash.GetHashAndReset());
                var uploadCompletedAt = DateTimeOffset.UtcNow;
                await PublishAsync(uploadId, 100, "completed", CancellationToken.None, "Upload completed successfully.", isCompleted: true, bytesProcessed: bytesProcessed, totalBytes: declaredFileSizeBytes);

                uploadSucceeded = true;
                return Results.Ok(new UploadResult(
                    uploadId,
                    originalFileName,
                    storedFileName,
                    bytesProcessed,
                    sha256,
                    uploadStartedAt,
                    uploadCompletedAt));
            }

            var fieldValue = await ReadFormFieldValueAsync(section.Body, maxFormFieldValueBytes, cancellationToken);

            if (string.Equals(fieldName, "uploadId", StringComparison.Ordinal)) {
                if (!string.IsNullOrEmpty(uploadId)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "duplicate_upload_id_field", "uploadId can only be provided once.");
                }

                if (!IsValidUploadId(fieldValue)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_upload_id", "uploadId must be a 32-character hex string.");
                }

                uploadId = fieldValue;
                uploadStartedAt = DateTimeOffset.UtcNow;

                if (!activeUploads.TryAdd(uploadId, 0)) {
                    await PublishAsync(uploadId, 0, "duplicate_upload_id", CancellationToken.None, "An upload with this uploadId is already in progress.", isCompleted: true, isError: true);
                    return Fail(StatusCodes.Status409Conflict, "duplicate_upload_id", "An upload with this uploadId is already in progress.");
                }
                uploadRegistered = true;

                limiterAcquired = await uploadConcurrencyLimiter.WaitAsync(uploadQueueTimeout, cancellationToken);
                if (!limiterAcquired) {
                    await PublishAsync(uploadId, 0, "server_busy", CancellationToken.None, "Upload queue is full. Please retry.", isCompleted: true, isError: true);
                    return Fail(StatusCodes.Status503ServiceUnavailable, "server_busy", "Upload queue is full. Please retry.");
                }

                continue;
            }

            if (string.Equals(fieldName, "fileSize", StringComparison.Ordinal)) {
                if (hasDeclaredFileSize) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "duplicate_file_size_field", "fileSize can only be provided once.");
                }

                if (!long.TryParse(fieldValue, NumberStyles.None, CultureInfo.InvariantCulture, out declaredFileSizeBytes)) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_size", "fileSize must be a positive integer.");
                }

                if (declaredFileSizeBytes <= 0) {
                    throw new UploadValidationException(StatusCodes.Status400BadRequest, "invalid_file_size", "fileSize must be greater than zero.");
                }

                if (declaredFileSizeBytes > maxUploadFileSizeBytes) {
                    throw new UploadValidationException(StatusCodes.Status413PayloadTooLarge, "file_too_large", $"File exceeds {maxUploadFileSizeBytes} bytes.");
                }

                hasDeclaredFileSize = true;
                continue;
            }

            throw new UploadValidationException(StatusCodes.Status400BadRequest, "unexpected_field", $"Unexpected multipart field '{fieldName}'.");
        }

        if (string.IsNullOrEmpty(uploadId)) {
            return Fail(StatusCodes.Status400BadRequest, "missing_upload_id", "uploadId is required.");
        }

        await PublishAsync(uploadId, 0, "validation_failed", CancellationToken.None, "Exactly one file is required.", isCompleted: true, isError: true, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        return Fail(StatusCodes.Status400BadRequest, "invalid_file_count", "Exactly one file is required.");
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        if (!string.IsNullOrEmpty(uploadId)) {
            await PublishAsync(uploadId, lastPercent, "aborted", CancellationToken.None, "Upload request was canceled.", isCompleted: true, isError: true, bytesProcessed: bytesProcessed, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        }
        return Fail(499, "upload_aborted", "Upload request was canceled.");
    }
    catch (UploadValidationException ex) {
        if (!string.IsNullOrEmpty(uploadId)) {
            await PublishAsync(uploadId, lastPercent, "validation_failed", CancellationToken.None, ex.Message, isCompleted: true, isError: true, bytesProcessed: bytesProcessed, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        }
        return Fail(ex.StatusCode, ex.Code, ex.Message);
    }
    catch (InvalidDataException ex) {
        if (!string.IsNullOrEmpty(uploadId)) {
            await PublishAsync(uploadId, lastPercent, "validation_failed", CancellationToken.None, ex.Message, isCompleted: true, isError: true, bytesProcessed: bytesProcessed, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        }
        return Fail(StatusCodes.Status400BadRequest, "invalid_form_data", ex.Message);
    }
    catch (IOException) {
        if (!string.IsNullOrEmpty(uploadId)) {
            await PublishAsync(uploadId, lastPercent, "io_error", CancellationToken.None, "I/O failure while storing upload.", isCompleted: true, isError: true, bytesProcessed: bytesProcessed, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        }
        return Fail(StatusCodes.Status500InternalServerError, "io_error", "I/O failure while storing upload.");
    }
    catch (Exception ex) {
        if (!string.IsNullOrEmpty(uploadId)) {
            await PublishAsync(uploadId, lastPercent, "error", CancellationToken.None, "Unexpected upload failure.", isCompleted: true, isError: true, bytesProcessed: bytesProcessed, totalBytes: hasDeclaredFileSize ? declaredFileSizeBytes : null);
        }
        app.Logger.LogError(ex, "Upload failed for uploadId {UploadId}", uploadId);
        return Fail(StatusCodes.Status500InternalServerError, "server_error", "Unexpected upload failure.");
    }
    finally {
        if (!uploadSucceeded && !string.IsNullOrWhiteSpace(storedPath)) {
            try {
                File.Delete(storedPath);
            }
            catch (Exception ex) {
                app.Logger.LogWarning(ex, "Failed to delete partial upload file {StoredPath}", storedPath);
            }
        }

        if (limiterAcquired) {
            uploadConcurrencyLimiter.Release();
        }

        if (uploadRegistered) {
            activeUploads.TryRemove(uploadId, out _);
        }
    }
}).DisableAntiforgery().WithMetadata(new RequestFormLimitsAttribute {
    MultipartBodyLengthLimit = maxMultipartBodyLengthBytes
});

app.MapPost("/api/uploads/static-test", async (HttpRequest request, CancellationToken cancellationToken) => {
    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("staticFile");

    if (file is null) {
        return Results.BadRequest(new UploadError("missing_file", "staticFile is required."));
    }

    return Results.Json(new StaticUploadResult(file.Name, file.FileName, file.Length, file.ContentType));
}).DisableAntiforgery();

await app.RunAsync();

UploadProgressSession GetOrCreateSession(string uploadId) {
    var now = DateTimeOffset.UtcNow;
    CleanupExpiredSessions(now);

    var session = uploadSessions.GetOrAdd(uploadId, static _ =>
        new UploadProgressSession(Channel.CreateBounded<UploadProgressEvent>(new BoundedChannelOptions(256) {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        }), DateTimeOffset.UtcNow));

    session.Touch(now);
    return session;
}

void CleanupExpiredSessions(DateTimeOffset now) {
    var nowMs = now.ToUnixTimeMilliseconds();
    var previousMs = Interlocked.Read(ref lastCleanupUnixMs);
    if (nowMs - previousMs < 30_000) {
        return;
    }

    if (Interlocked.CompareExchange(ref lastCleanupUnixMs, nowMs, previousMs) != previousMs) {
        return;
    }

    foreach (var kvp in uploadSessions) {
        var session = kvp.Value;
        var ttl = session.IsTerminal ? completedSessionRetention : idleSessionTimeout;
        if (now - session.LastTouchedUtc <= ttl) {
            continue;
        }

        if (uploadSessions.TryRemove(kvp.Key, out var removedSession)) {
            removedSession.Channel.Writer.TryComplete();
        }
    }
}

async ValueTask PublishAsync(
    string uploadId,
    int percent,
    string stage,
    CancellationToken cancellationToken,
    string? message = null,
    bool isCompleted = false,
    bool isError = false,
    long? bytesProcessed = null,
    long? totalBytes = null) {
    var session = GetOrCreateSession(uploadId);
    var progress = new UploadProgressEvent(
        uploadId,
        Math.Clamp(percent, 0, 100),
        stage,
        isCompleted,
        isError,
        message,
        bytesProcessed,
        totalBytes,
        DateTimeOffset.UtcNow);

    session.Record(progress);

    try {
        await session.Channel.Writer.WriteAsync(progress, cancellationToken);
    }
    catch (ChannelClosedException) {
        // Ignore closed channels; late listeners can still read LastEvent from session.
    }
}

IResult Fail(int statusCode, string code, string message)
    => Results.Json(new UploadError(code, message), statusCode: statusCode);

static bool TryGetMultipartBoundary(string? contentType, out string boundary) {
    boundary = string.Empty;

    if (string.IsNullOrWhiteSpace(contentType)) {
        return false;
    }

    if (!Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(contentType, out var mediaType)) {
        return false;
    }

    if (!string.Equals(mediaType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
        return false;
    }

    boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value ?? string.Empty;
    return !string.IsNullOrWhiteSpace(boundary);
}

static async Task<string> ReadFormFieldValueAsync(Stream stream, int maxBytes, CancellationToken cancellationToken) {
    if (maxBytes <= 0) {
        throw new ArgumentOutOfRangeException(nameof(maxBytes));
    }

    var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(maxBytes + 1, 1024));
    try {
        using var ms = new MemoryStream();

        while (true) {
            var remaining = (maxBytes + 1) - (int)ms.Length;
            if (remaining <= 0) {
                throw new InvalidDataException($"Form field exceeds {maxBytes} bytes.");
            }

            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), cancellationToken);
            if (read == 0) {
                break;
            }

            ms.Write(buffer, 0, read);
        }

        if (ms.Length > maxBytes) {
            throw new InvalidDataException($"Form field exceeds {maxBytes} bytes.");
        }

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length).Trim();
    }
    finally {
        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
    }
}

static bool IsValidUploadId(string uploadId) {
    if (uploadId.Length != 32) {
        return false;
    }

    foreach (var ch in uploadId) {
        if (!Uri.IsHexDigit(ch)) {
            return false;
        }
    }

    return true;
}

static string SanitizeFileName(string? rawFileName) {
    var fileName = Path.GetFileName(rawFileName ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(fileName)) {
        return string.Empty;
    }

    var invalidChars = Path.GetInvalidFileNameChars();
    var sanitizedChars = fileName.Select(ch => invalidChars.Contains(ch) || char.IsControl(ch) ? '_' : ch).ToArray();
    var sanitized = new string(sanitizedChars);

    return sanitized.Length > 128 ? sanitized[..128] : sanitized;
}

static string BuildStoredFileName(string uploadId, string originalFileName) {
    var extension = NormalizeExtension(Path.GetExtension(originalFileName));
    return $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{uploadId}{extension}";
}

static string NormalizeExtension(string extension) {
    if (string.IsNullOrWhiteSpace(extension)) {
        return ".bin";
    }

    var normalized = extension.Trim();
    if (!normalized.StartsWith('.')) {
        normalized = "." + normalized;
    }

    if (normalized.Length > 16) {
        return ".bin";
    }

    for (var i = 1; i < normalized.Length; i++) {
        var ch = normalized[i];
        if (!char.IsLetterOrDigit(ch) && ch != '-' && ch != '_') {
            return ".bin";
        }
    }

    return normalized.ToLowerInvariant();
}

public sealed record UploadResult(
    string UploadId,
    string OriginalFileName,
    string StoredFileName,
    long BytesProcessed,
    string Sha256,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);

public sealed record UploadProgressEvent(
    string UploadId,
    int Percent,
    string Stage,
    bool IsCompleted,
    bool IsError,
    string? Message,
    long? BytesProcessed,
    long? TotalBytes,
    DateTimeOffset TimestampUtc);

public sealed record UploadError(string Code, string Message);

public sealed record StaticUploadResult(string FieldName, string FileName, long Size, string ContentType);

internal sealed class UploadValidationException : Exception {
    public UploadValidationException(int statusCode, string code, string message) : base(message) {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}

internal sealed class UploadProgressSession {
    private long _lastTouchedUnixMs;
    private int _isTerminal;

    public UploadProgressSession(Channel<UploadProgressEvent> channel, DateTimeOffset createdAtUtc) {
        Channel = channel;
        Touch(createdAtUtc);
    }

    public Channel<UploadProgressEvent> Channel { get; }

    public bool IsTerminal => Volatile.Read(ref _isTerminal) == 1;

    public UploadProgressEvent? LastEvent { get; private set; }

    public DateTimeOffset LastTouchedUtc => DateTimeOffset.FromUnixTimeMilliseconds(Volatile.Read(ref _lastTouchedUnixMs));

    public void Touch(DateTimeOffset now) => Volatile.Write(ref _lastTouchedUnixMs, now.ToUnixTimeMilliseconds());

    public void Record(UploadProgressEvent progress) {
        LastEvent = progress;
        Touch(progress.TimestampUtc);

        if (progress.IsCompleted || progress.IsError) {
            Volatile.Write(ref _isTerminal, 1);
        }
    }
}

public partial class Program { }
