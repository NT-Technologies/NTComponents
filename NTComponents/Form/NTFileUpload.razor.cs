using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A Material 3 aligned file upload field for static form posts and interactive uploads.
/// </summary>
/// <remarks>
///     Use static rendering when a surrounding HTML form owns the upload endpoint. Use <see cref="OnUploadFile" /> for
///     InteractiveServer and InteractiveWebAssembly uploads. The component opens a progress-aware stream and the consumer
///     decides where that stream is copied, posted, scanned, encrypted, or otherwise processed.
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a native file-input fallback and enhances upload processing when interactive.",
    CompatibilityDetails = "Static SSR can emit the file input shell for native form posts. Blazor IBrowserFile processing, progress reporting, and upload callbacks require an interactive render mode.")]
public partial class NTFileUpload {
    private static readonly TimeSpan MinimumProcessingIndicatorDuration = TimeSpan.FromMilliseconds(150);

    private static readonly HashSet<string> FileExplicitControlAttributeNames = new(StringComparer.OrdinalIgnoreCase) {
        "id",
        "type",
        "name",
        "class",
        "multiple",
        "accept",
        "disabled",
        "required",
        "aria-labelledby",
        "aria-describedby",
        "aria-invalid",
        "aria-errormessage"
    };

    private readonly List<FileUploadItem> _items = [];
    private string _displayValue = "No file selected";
    private string? _elementStyle;
    private int _inputKey;
    private bool _isUploading;
    private string? _selectionError;

    /// <summary>
    ///     Gets or sets accepted file type hints, using the native <c>accept</c> attribute format.
    /// </summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the selected files should upload immediately after selection.
    /// </summary>
    [Parameter]
    public bool AutoUpload { get; set; }

    /// <summary>
    ///     Gets or sets the visible selection affordance text rendered inside the field.
    /// </summary>
    [Parameter]
    public string ChooseButtonText { get; set; } = "Choose file";

    /// <summary>
    ///     Gets or sets the maximum number of files accepted by the component.
    /// </summary>
    [Parameter]
    public int MaximumFileCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the maximum file size opened by interactive uploads.
    /// </summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 512_000;

    /// <summary>
    ///     Gets or sets whether multiple file selection is allowed.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    ///     Gets a callback invoked when consumer upload processing completes.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<NTFileUploadEventArgs>> OnCompleted { get; set; }

    /// <summary>
    ///     Gets a callback invoked when a selected file fails validation or upload processing.
    /// </summary>
    [Parameter]
    public EventCallback<NTFileUploadEventArgs> OnFileError { get; set; }

    /// <summary>
    ///     Gets a callback invoked when interactive upload progress changes.
    /// </summary>
    [Parameter]
    public EventCallback<NTFileUploadProgressDetails> OnProgressChanged { get; set; }

    /// <summary>
    ///     Gets a callback invoked for InteractiveServer and InteractiveWebAssembly uploads. Read <see cref="NTFileUploadEventArgs.Stream" /> to drive progress updates while the browser file is copied.
    /// </summary>
    [Parameter]
    public EventCallback<NTFileUploadEventArgs> OnUploadFile { get; set; }

    /// <summary>
    ///     Gets or sets the status text shown after a file is selected and before upload begins.
    /// </summary>
    [Parameter]
    public string ReadyText { get; set; } = "Ready";

    /// <summary>
    ///     Gets or sets the visible placeholder shown before files are selected.
    /// </summary>
    [Parameter]
    public string EmptyText { get; set; } = "No file selected";

    /// <summary>
    ///     Gets or sets the optional progress item template.
    /// </summary>
    [Parameter]
    public RenderFragment<NTFileUploadProgressDetails>? ProgressTemplate { get; set; }

    /// <summary>
    ///     Gets or sets the status text shown after the browser upload stream has been fully read and the consumer callback is still processing.
    /// </summary>
    [Parameter]
    public string ProcessingText { get; set; } = "Processing...";

    /// <summary>
    ///     Gets or sets a value indicating whether the upload action button is shown for interactive uploads.
    /// </summary>
    [Parameter]
    public bool ShowUploadButton { get; set; } = true;

    /// <summary>
    ///     Gets or sets the status text shown while the browser upload stream is being read.
    /// </summary>
    [Parameter]
    public string UploadingText { get; set; } = "Uploading";

    /// <summary>
    ///     Gets or sets the upload action button text.
    /// </summary>
    [Parameter]
    public string UploadButtonText { get; set; } = "Upload";

    /// <summary>
    ///     Gets or sets the status text shown after the consumer upload callback completes.
    /// </summary>
    [Parameter]
    public string UploadCompleteText { get; set; } = "Complete";

    /// <inheritdoc />
    protected override IEnumerable<string> ExplicitControlAttributeNames => FileExplicitControlAttributeNames;

    /// <inheritdoc />
    protected override bool HasFloatingValue => _items.Count > 0 || base.HasFloatingValue;

    private bool AllowsMultiple => Multiple || HasBooleanAttribute("multiple");

    private string DisplayErrorText => _selectionError ?? CurrentErrorText ?? string.Empty;

    private bool HasUploadHandler => OnUploadFile.HasDelegate;

    private string? InputLabelledBy => HasLabel ? LabelId : null;

    private string InputClass => string.IsNullOrEmpty(CssClass) ? "nt-input-control nt-file-upload-input" : $"nt-input-control nt-file-upload-input {CssClass}";

    private bool IsSelectionDisabled => FieldDisabled || FieldReadOnly || _isUploading;

    private bool UploadButtonDisabled => IsSelectionDisabled || _items.Count == 0 || _items.Any(item => item.HasError) || !HasUploadHandler;

    /// <summary>
    ///     Gets cached inline CSS variable overrides for the field.
    /// </summary>
    protected new string? ElementStyle => _elementStyle;

    /// <summary>
    ///     Clears the selected files and resets the native file input.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ClearAsync() {
        _selectionError = null;
        _items.Clear();
        CurrentValue = null;
        RefreshDisplayValue();
        _inputKey++;
        await BindAfter.InvokeAsync(CurrentValue);
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(IReadOnlyList<IBrowserFile>? value) => value is { Count: > 0 } ? FormatFileNames(value) : null;

    /// <inheritdoc />
    protected override void BuildAdditionalRootClasses(StringBuilder builder) {
        builder.Append(" nt-file-upload");
        if (!string.IsNullOrWhiteSpace(EmptyText)) {
            builder.Append(" nt-input-has-placeholder");
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out IReadOnlyList<IBrowserFile>? result, [NotNullWhen(false)] out string? validationErrorMessage) {
        result = CurrentValue;
        validationErrorMessage = null;
        return true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        _elementStyle = base.ElementStyle;
        RefreshDisplayValue();
    }

    private NTFileUploadProgressDetails CreateProgressDetails(FileUploadItem item) => new() {
        BrowserFile = item.BrowserFile,
        IsIndeterminate = item.IsIndeterminate,
        Name = item.Name,
        Percent = item.Percent,
        Size = item.Size,
        Status = item.Status
    };

    private NTFileUploadEventArgs CreateUploadArgs(FileUploadItem item, int index, IReadOnlyList<NTFileUploadEventArgs> allFiles) => new() {
        AllFiles = allFiles,
        BrowserFile = item.BrowserFile,
        ContentType = item.BrowserFile.ContentType,
        Index = index,
        LastModified = item.BrowserFile.LastModified,
        Name = item.Name,
        ProgressPercent = item.Percent,
        ProgressTitle = item.Status,
        Size = item.Size
    };

    private bool HasBooleanAttribute(string name) => TryGetAdditionalAttribute(name, out var value) && value is not false && !string.Equals(value?.ToString(), "false", StringComparison.OrdinalIgnoreCase);

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs args) {
        _selectionError = null;
        if (MaximumFileCount <= 0) {
            await RejectSelectionAsync("MaximumFileCount must be greater than zero.");
            return;
        }

        if (args.FileCount > MaximumFileCount) {
            await RejectSelectionAsync($"Select {MaximumFileCount} file{(MaximumFileCount == 1 ? string.Empty : "s")} or fewer.");
            return;
        }

        IReadOnlyList<IBrowserFile> files;
        try {
            files = args.GetMultipleFiles(MaximumFileCount);
        }
        catch (InvalidOperationException exception) {
            await RejectSelectionAsync(exception.Message);
            return;
        }

        var oversizedFiles = files.Where(file => file.Size > MaximumFileSize).ToArray();
        if (oversizedFiles.Length > 0) {
            await RejectOversizedSelectionAsync(oversizedFiles);
            return;
        }

        _items.Clear();
        foreach (var file in files) {
            _items.Add(new FileUploadItem(file, ReadyText));
        }

        CurrentValue = files;
        RefreshDisplayValue();
        await BindAfter.InvokeAsync(CurrentValue);

        if (AutoUpload && HasUploadHandler) {
            await UploadAsync();
        }
    }

    private async Task RejectOversizedSelectionAsync(IReadOnlyList<IBrowserFile> files) {
        var message = BuildFileTooLargeMessage(files);
        _selectionError = message;
        _items.Clear();
        foreach (var file in files) {
            _items.Add(new FileUploadItem(file, "Too large") {
                HasError = true,
                Percent = 100
            });
        }

        CurrentValue = null;
        RefreshDisplayValue();
        _inputKey++;
        await BindAfter.InvokeAsync(CurrentValue);
        if (OnFileError.HasDelegate) {
            var file = files[0];
            await OnFileError.InvokeAsync(new NTFileUploadEventArgs {
                BrowserFile = file,
                ContentType = file.ContentType,
                ErrorMessage = message,
                LastModified = file.LastModified,
                Name = file.Name,
                ProgressPercent = 100,
                ProgressTitle = "Too large",
                Size = file.Size
            });
        }
    }

    private async Task RejectSelectionAsync(string message) {
        await ClearAsync();
        _selectionError = message;
        if (OnFileError.HasDelegate) {
            await OnFileError.InvokeAsync(new NTFileUploadEventArgs { ErrorMessage = message });
        }
    }

    private void RefreshDisplayValue() {
        _displayValue = CurrentValue is { Count: > 0 } files
            ? FormatFileNames(files)
            : _items.Count > 0
                ? string.Join(", ", _items.Select(item => item.Name))
                : EmptyText;
    }

    private static string FormatFileNames(IEnumerable<IBrowserFile> files) => string.Join(", ", files.Select(file => file.Name));

    private string BuildFileTooLargeMessage(IReadOnlyList<IBrowserFile> files) {
        var maximumSize = FormatFileSize(MaximumFileSize);
        return files.Count == 1
            ? $"{files[0].Name} is too large. Maximum file size is {maximumSize}."
            : $"{files.Count} files are too large. Maximum file size is {maximumSize}.";
    }

    private static string FormatFileSize(long bytes) {
        if (bytes >= 1024L * 1024L) {
            return $"{bytes / (1024d * 1024d):0.##} MB";
        }

        if (bytes >= 1024L) {
            return $"{bytes / 1024d:0.##} KB";
        }

        return string.Create(CultureInfo.InvariantCulture, $"{bytes} bytes");
    }

    private async Task UploadAsync() {
        if (_isUploading || _items.Count == 0 || !HasUploadHandler) {
            return;
        }

        _isUploading = true;
        var completed = _items.Select((item, index) => CreateUploadArgs(item, index, [])).ToArray();
        for (var i = 0; i < completed.Length; i++) {
            completed[i].AllFiles = completed;
        }

        try {
            for (var i = 0; i < _items.Count; i++) {
                await UploadCallbackFileAsync(_items[i], completed[i]);

                if (completed[i].IsCancelled) {
                    break;
                }
            }

            if (OnCompleted.HasDelegate) {
                await OnCompleted.InvokeAsync(completed);
            }
        }
        finally {
            _isUploading = false;
        }
    }

    private async Task UploadCallbackFileAsync(FileUploadItem item, NTFileUploadEventArgs args) {
        await UpdateProgressAsync(item, 0, UploadingText, false);
        try {
            await using var inner = item.BrowserFile.OpenReadStream(MaximumFileSize);
            await using var tracked = new NTUploadProgressStream(inner, bytesRead => UpdateReadProgressAsync(item, args, bytesRead), () => UpdateProcessingAsync(item, args));
            args.Stream = tracked;

            await OnUploadFile.InvokeAsync(args);
            if (!args.IsCancelled) {
                await ShowProcessingAsync(item, args);
            }

            args.ProgressPercent = 100;
            args.ProgressTitle = args.IsCancelled ? "Canceled" : UploadCompleteText;
            await UpdateProgressAsync(item, 100, args.IsCancelled ? "Canceled" : UploadCompleteText, false);
        }
        catch (Exception exception) {
            args.ErrorMessage = exception.Message;
            await UpdateProgressAsync(item, item.Percent, "Failed", false);
            if (OnFileError.HasDelegate) {
                await OnFileError.InvokeAsync(args);
            }
        }
        finally {
            args.Stream = null;
        }
    }

    private async Task ShowProcessingAsync(FileUploadItem item, NTFileUploadEventArgs args) {
        await UpdateProcessingAsync(item, args, true);
        await Task.Delay(MinimumProcessingIndicatorDuration);
    }

    private Task UpdateReadProgressAsync(FileUploadItem item, NTFileUploadEventArgs args, int bytesRead) {
        item.BytesRead = Math.Min(item.Size, item.BytesRead + bytesRead);
        var percent = item.Size <= 0 ? 100 : (int)Math.Min(100, item.BytesRead * 100 / item.Size);
        if (percent >= 100) {
            return UpdateProcessingAsync(item, args);
        }

        args.ProgressPercent = percent;
        args.ProgressTitle = UploadingText;
        return UpdateProgressAsync(item, percent, UploadingText, false);
    }

    private Task UpdateProcessingAsync(FileUploadItem item, NTFileUploadEventArgs args, bool forceRender = false) {
        args.ProgressPercent = 100;
        args.ProgressTitle = ProcessingText;
        return UpdateProgressAsync(item, 100, ProcessingText, true, forceRender);
    }

    private async Task UpdateProgressAsync(FileUploadItem item, int percent, string status, bool isIndeterminate, bool forceRender = false) {
        await InvokeAsync(async () => {
            percent = Math.Clamp(percent, 0, 100);
            var isUnchanged = item.Percent == percent && item.IsIndeterminate == isIndeterminate && string.Equals(item.Status, status, StringComparison.Ordinal);
            if (isUnchanged && !forceRender) {
                return;
            }

            item.Percent = percent;
            item.IsIndeterminate = isIndeterminate;
            item.Status = status;
            if (!isUnchanged && OnProgressChanged.HasDelegate) {
                await OnProgressChanged.InvokeAsync(CreateProgressDetails(item));
            }

            StateHasChanged();
        });
    }

    private sealed class FileUploadItem(IBrowserFile browserFile, string status) {
        public IBrowserFile BrowserFile { get; } = browserFile;

        public long BytesRead { get; set; }

        public string Id { get; } = $"{browserFile.Name}:{browserFile.Size}:{browserFile.LastModified.UtcTicks}";

        public string Name { get; } = browserFile.Name;

        public bool IsIndeterminate { get; set; }

        public bool HasError { get; set; }

        public int Percent { get; set; }

        public bool ShouldShowPercent => Percent < 100 && !IsIndeterminate && (!HasError || BytesRead > 0);

        public long Size { get; } = browserFile.Size;

        public string Status { get; set; } = status;
    }

}

/// <summary>
///     Upload progress item details for <see cref="NTFileUpload" />.
/// </summary>
public readonly record struct NTFileUploadProgressDetails {
    /// <summary>
    ///     Gets the selected browser file.
    /// </summary>
    public IBrowserFile BrowserFile { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the current progress indicator should render as indeterminate.
    /// </summary>
    public bool IsIndeterminate { get; init; }

    /// <summary>
    ///     Gets the selected file name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Gets the current progress percentage.
    /// </summary>
    public int Percent { get; init; }

    /// <summary>
    ///     Gets the file size in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    ///     Gets the current progress status.
    /// </summary>
    public string Status { get; init; }
}

/// <summary>
///     Upload event details for <see cref="NTFileUpload" />.
/// </summary>
public sealed class NTFileUploadEventArgs : EventArgs {
    /// <summary>
    ///     Gets all files participating in the current upload batch.
    /// </summary>
    public IReadOnlyList<NTFileUploadEventArgs> AllFiles { get; internal set; } = [];

    /// <summary>
    ///     Gets the selected browser file.
    /// </summary>
    public IBrowserFile? BrowserFile { get; internal set; }

    /// <summary>
    ///     Gets the selected file MIME type.
    /// </summary>
    public string ContentType { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the error message when validation or upload processing fails.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    ///     Gets the index in the current upload batch.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    ///     Gets or sets a value indicating whether remaining uploads should stop.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    ///     Gets the selected file last-modified timestamp.
    /// </summary>
    public DateTimeOffset LastModified { get; internal set; }

    /// <summary>
    ///     Gets the selected file name.
    /// </summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the current overall progress percentage.
    /// </summary>
    public int ProgressPercent { get; internal set; }

    /// <summary>
    ///     Gets the current progress title.
    /// </summary>
    public string ProgressTitle { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the selected file size in bytes.
    /// </summary>
    public long Size { get; internal set; }

    /// <summary>
    ///     Gets the progress-aware stream for the selected file during <see cref="NTFileUpload.OnUploadFile" />.
    /// </summary>
    public Stream? Stream { get; internal set; }
}
