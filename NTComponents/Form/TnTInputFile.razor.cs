/*
This code is a derivative of https://github.com/microsoft/fluentui-blazor/blob/dev/src/Core/Components/InputFile/FluentInputFile.razor.cs
MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE
 */
#pragma warning disable CS0618

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents;

/// <summary>
///     Provides a file input component for uploading files, supporting multiple modes and progress tracking.
/// </summary>
public partial class TnTInputFile {

    /// <summary>
    ///     Gets or sets the appearance of the form input.
    /// </summary>
    [Parameter]
    public FormAppearance Appearance { get; set; }

    /// <summary>
    ///     Gets or sets whether the input should automatically receive focus.
    /// </summary>
    [Parameter]
    public bool? AutoFocus { get; set; }

    /// <summary>
    ///     Gets or sets the background color of the input.
    /// </summary>
    [Parameter]
    public TnTColor BackgroundColor { get; set; } = TnTColor.SurfaceContainerHighest;

    /// <summary>
    ///     Gets or sets the buffer size used when reading files.
    /// </summary>
    [Parameter]
    public uint BufferSize { get; set; } = 10 * 1024;

    /// <summary>
    ///     Gets or sets a value indicating whether the input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    ///     Gets the .NET object reference for JS interop.
    /// </summary>
    private DotNetObjectReference<TnTInputFile>? DotNetObjectRef { get; set; }

    DotNetObjectReference<TnTInputFile>? INTPageScriptComponent<TnTInputFile>.DotNetObjectRef => DotNetObjectRef;

    /// <summary>
    ///     Gets the CSS class string for the input element.
    /// </summary>
    public string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes?.ToDictionary())
        .AddClass("tnt-input")
        .AddClass(GetAppearanceClass(_tntForm, Appearance))
        .AddRipple(EnableRipple)
        .AddDisabled(FieldDisabled)
        .Build();

    /// <summary>
    ///     Gets or sets the id attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>
    ///     Gets or sets the lang attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementLang { get; set; }

    /// <summary>
    ///     Gets or sets the name attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementName { get; set; }

    /// <summary>
    ///     Gets the style string for the input element.
    /// </summary>
    public string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes?.ToDictionary())
        .AddVariable("tnt-input-on-tint-color", OnTintColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-tint-color", TintColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-background-color", BackgroundColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-text-color", TextColor.ToCssTnTColorVariable())
        .AddVariable("tnt-input-error-color", ErrorColor.ToCssTnTColorVariable())
        .Build();

    /// <summary>
    ///     Gets or sets the title attribute for the input element.
    /// </summary>
    [Parameter]
    public string? ElementTitle { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the ripple effect is enabled.
    /// </summary>
    [Parameter]
    public bool EnableRipple { get; set; }

    /// <summary>
    ///     Gets or sets the icon displayed at the end of the input.
    /// </summary>
    [Parameter]
    public NTIcon? EndIcon { get; set; }

    /// <summary>
    ///     Gets or sets the error color for the input.
    /// </summary>
    [Parameter]
    public TnTColor ErrorColor { get; set; } = TnTColor.Error;

    /// <inheritdoc />
    public bool FieldDisabled => _tntForm?.Disabled is not null ? _tntForm.Disabled : Disabled;

    /// <inheritdoc />
    public bool FieldReadonly => _tntForm?.ReadOnly is not null ? _tntForm.ReadOnly : ReadOnly;

    /// <summary>
    ///     Gets the isolated JavaScript module reference for this component.
    /// </summary>
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <summary>
    ///     Gets the path to the JavaScript module for this component.
    /// </summary>
    public string? JsModulePath => "./_content/NTComponents/Form/TnTInputFile.razor.js";

    /// <summary>
    ///     Gets or sets the label for the input element.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of files that can be uploaded at once.
    /// </summary>
    [Parameter]
    public int MaximumFileCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the maximum allowed file size in bytes.
    /// </summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    ///     Gets or sets the file input mode.
    /// </summary>
    [Parameter]
    public InputFileMode Mode { get; set; } = InputFileMode.SaveToTemporaryFolder;

    /// <summary>
    ///     Occurs when all files have been uploaded or the operation is completed.
    /// </summary>
    [Parameter]
    public EventCallback<IEnumerable<TnTInputFileEventArgs>> OnCompleted { get; set; }

    /// <summary>
    ///     Occurs when the file count exceeds the maximum allowed.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnFileCountExceeded { get; set; }

    /// <summary>
    ///     Occurs when an error occurs during file upload.
    /// </summary>
    [Parameter]
    public EventCallback<TnTInputFileEventArgs> OnFileError { get; set; }

    /// <summary>
    ///     Occurs when a file is uploaded.
    /// </summary>
    [Parameter]
    public EventCallback<TnTInputFileEventArgs> OnFileUploaded { get; set; }

    /// <summary>
    ///     Occurs when the input file changes.
    /// </summary>
    [Parameter]
    public EventCallback<InputFileChangeEventArgs> OnInputFileChange { get; set; }

    /// <summary>
    ///     Occurs when the upload progress changes.
    /// </summary>
    [Parameter]
    public EventCallback<TnTInputFileEventArgs> OnProgressChange { get; set; }

    /// <summary>
    ///     Gets or sets the color used for the on-tint.
    /// </summary>
    [Parameter]
    public TnTColor OnTintColor { get; set; } = TnTColor.OnPrimary;

    /// <summary>
    ///     Gets or sets the current progress percent.
    /// </summary>
    [Parameter]
    public int ProgressPercent { get; set; } = 0;

    /// <summary>
    ///     Occurs when the progress percent changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> ProgressPercentChanged { get; set; }

    /// <summary>
    ///     Gets or sets the template for displaying progress.
    /// </summary>
    [Parameter]
    public RenderFragment<ProgressFileDetails>? ProgressTemplate { get; set; }

    /// <summary>
    ///     Gets the progress title or status message.
    /// </summary>
    public string ProgressTitle { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the input is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to show upload progress.
    /// </summary>
    [Parameter]
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    ///     Gets or sets the icon displayed at the start of the input.
    /// </summary>
    [Parameter]
    public NTIcon? StartIcon { get; set; }

    /// <summary>
    ///     Text that provides additional information about the input, such as usage instructions or validation hints.
    /// </summary>
    [Parameter]
    public string? SupportingText { get; set; }

    /// <summary>
    ///     Gets or sets the text color for the input.
    /// </summary>
    [Parameter]
    public TnTColor TextColor { get; set; } = TnTColor.OnSurface;

    /// <summary>
    ///     Gets or sets the tint color for the input.
    /// </summary>
    [Parameter]
    public TnTColor TintColor { get; set; } = TnTColor.Primary;

    /// <summary>
    ///     The content to display as a tooltip for the component.
    /// </summary>
    [Parameter]
    public RenderFragment? Tooltip { get; set; }

    /// <summary>
    ///     The icon displayed alongside the tooltip text.
    /// </summary>
    [Parameter]
    public NTIcon TooltipIcon { get; set; } = MaterialIcon.Help;

    /// <summary>
    ///     The resource string displayed before loading starts.
    /// </summary>
    public const string ResourceLoadingBefore = "Loading...";

    /// <summary>
    ///     The resource string displayed when loading is canceled.
    /// </summary>
    public const string ResourceLoadingCanceled = "Canceled";

    /// <summary>
    ///     The resource string displayed when loading is completed.
    /// </summary>
    public const string ResourceLoadingCompleted = "Completed";

    /// <summary>
    ///     The resource string format for loading progress.
    /// </summary>
    public const string ResourceLoadingInProgress = "Loading {0}/{1} - {2}";

    /// <inheritdoc />
    IReadOnlyDictionary<string, object>? ITnTComponentBase.AdditionalAttributes { get => base.AdditionalAttributes?.ToDictionary(); set => base.AdditionalAttributes = value is null ? null : new Dictionary<string, object>(value!); }

    /// <inheritdoc />
    ElementReference ITnTComponentBase.Element => base.Element ?? default;

    /// <summary>
    ///     Gets the JavaScript runtime for interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    [Inject]
    private IServiceProvider? Services { get; set; }

    /// <summary>
    ///     Gets or sets the progress details for the current file.
    /// </summary>
    private ProgressFileDetails _progressFileDetails { get; set; }

    /// <summary>
    ///     Gets the cascading form context, if any.
    /// </summary>
    [CascadingParameter]
    private ITnTForm? _tntForm { get; set; }

    private readonly NTDisposalState _disposalState = new();
    private IJSObjectReference? _containerInstance;

    private bool _defaultHandler;
    private ElementReference _labelElement;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TnTInputFile" /> class.
    /// </summary>
    public TnTInputFile() => DotNetObjectRef = DotNetObjectReference.Create(this);

    /// <inheritdoc />
    public void Dispose() {
        if (_disposalState.TryBegin()) {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_disposalState.TryBegin()) {
            try {
                await DisposeAsyncCore().ConfigureAwait(false);
                Dispose(disposing: false);
            }
            finally {
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the component and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    #if NET11_0_OR_GREATER
    protected override void Dispose(bool disposing) {
        DisposeCore(disposing);
        base.Dispose(disposing);
    }
    #else
    protected virtual void Dispose(bool disposing) => DisposeCore(disposing);
    #endif

    private void DisposeCore(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
            // Do not dispose IsolatedJsModule here; it should be disposed asynchronously in DisposeAsyncCore.
        }
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the component asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore() {
        var module = IsolatedJsModule;
        var containerInstance = _containerInstance;
        var dotNetObjectRef = DotNetObjectRef;
        IsolatedJsModule = null;
        _containerInstance = null;
        DotNetObjectRef = null;

        try {
            try {
                if (containerInstance is not null) {
                    try {
                        await containerInstance.InvokeVoidAsync("dispose");
                    }
                    finally {
                        await containerInstance.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally {
                if (module is not null) {
                    try {
                        await module.InvokeVoidAsync("onDispose", Element, dotNetObjectRef);
                    }
                    finally {
                        await module.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during disposal.
        }
        finally {
            dotNetObjectRef?.Dispose();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (!_disposalState.HasStarted) {
            try {
                if (firstRender) {
                    var importedModule = IsolatedJsModule ?? await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                    if (!_disposalState.HasStarted && DotNetObjectRef is not null) {
                        IsolatedJsModule = importedModule;
                        await importedModule.InvokeVoidAsync("onLoad", Element, DotNetObjectRef);
                        if (!_disposalState.HasStarted) {
                            var containerInstance = await importedModule.InvokeAsync<IJSObjectReference>("initializeFileDropZone", _labelElement, Element);
                            if (!_disposalState.HasStarted) {
                                _containerInstance = containerInstance;
                            }
                            else {
                                await containerInstance.DisposeAsync();
                            }
                        }
                    }
                    else {
                        await importedModule.DisposeAsync();
                        IsolatedJsModule = null;
                    }
                }
                if (!_disposalState.HasStarted && IsolatedJsModule is not null && DotNetObjectRef is not null) {
                    await IsolatedJsModule.InvokeVoidAsync("onUpdate", Element, DotNetObjectRef);
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();
        OnChange = EventCallback.Factory.Create(this, async (InputFileChangeEventArgs args) => await OnUploadFilesHandlerAsync(args));
        if (!OnInputFileChange.HasDelegate) {
            _defaultHandler = true;
            OnInputFileChange = EventCallback.Factory.Create<InputFileChangeEventArgs>(this, OnUploadFilesHandlerAsync);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        var dict = AdditionalAttributes?.ToDictionary() ?? [];
        dict.TryAdd("disabled", FieldDisabled);
        dict.TryAdd("readonly", FieldReadonly);
        AdditionalAttributes = dict;

        TooltipIcon.Tooltip = Tooltip;
        TooltipIcon.AdditionalClass = "tnt-tooltip-icon";
        TooltipIcon.Size = IconSize.Small;
    }

    /// <summary>
    ///     Handles the file upload process and progress updates.
    /// </summary>
    /// <param name="e">The file change event arguments.</param>
    private async Task OnUploadFilesHandlerAsync(InputFileChangeEventArgs e) {
        if (e.FileCount > MaximumFileCount) {
            if (OnFileCountExceeded.HasDelegate) {
                await OnFileCountExceeded.InvokeAsync(e.FileCount);
            }
            return;
        }

        // Start
        await UpdateProgressAsync(0, ResourceLoadingBefore);

        List<TnTInputFileEventArgs>? uploadedFiles = [];
        var allFiles = e.GetMultipleFiles(MaximumFileCount);
        var allFilesSummary = allFiles.Select(i => new UploadedFileDetails() { Name = i.Name, Size = i.Size, ContentType = i.ContentType }).ToArray();
        var totalFileSizes = allFiles.Sum(i => i.Size);
        var totalRead = 0L;
        var fileNumber = 0;

        foreach (var file in allFiles) {
            _progressFileDetails = new() {
                Index = fileNumber,
                Name = file.Name,
                Percentage = 0
            };
            // Keep a trace of this file
            TnTInputFileEventArgs? fileDetails = new() {
                AllFiles = allFilesSummary,
                Index = fileNumber,
                Name = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                LastModified = file.LastModified,
                IsCancelled = false,
            };
            uploadedFiles.Add(fileDetails);

            // Max size => ERROR
            if (file.Size > MaximumFileSize) {
                fileDetails.ErrorMessage = "The maximum size allowed is reached";
                continue;
            }

            // Progress
            var title = string.Format(ResourceLoadingInProgress, fileNumber + 1, allFiles.Count, file.Name) ?? string.Empty;
            fileDetails.ProgressTitle = title;

            switch (Mode) {
                case InputFileMode.Buffer:

                    // Read all buffers and raise the event OnProgressChange
                    await ReadFileToBufferAndRaiseProgressEventAsync(file, fileDetails, (buffer, bytesRead) => {
                        totalRead += bytesRead;

                        fileDetails.Buffer = new TnTInputFileBuffer(buffer, bytesRead);

                        return UpdateProgressAsync(totalRead, totalFileSizes, title);
                    });

                    break;

                case InputFileMode.SaveToTemporaryFolder:

                    // Save to temporary file
                    var tempFileName = Path.GetTempFileName();
                    fileDetails.LocalFile = new FileInfo(tempFileName);

                    // Create a local file and write all read buffers
                    await using (FileStream writeStream = new(tempFileName, FileMode.Create)) {
                        await ReadFileToBufferAndRaiseProgressEventAsync(file, fileDetails, async (buffer, bytesRead) => {
                            totalRead += bytesRead;

                            await writeStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                            await UpdateProgressAsync(totalRead, totalFileSizes, title);
                        });
                    }

                    break;

                case InputFileMode.Stream:

                    var fileSizePart1 = file.Size / 2;
                    var fileSizePart2 = file.Size - fileSizePart1;

                    // Get a reference to the current file Stream
                    fileDetails.Stream = file.OpenReadStream(MaximumFileSize);

                    // Progression percent (first 50%)
                    totalRead += fileSizePart1;
                    await UpdateProgressAsync(totalRead, totalFileSizes, title);

                    // Uploaded event
                    if (OnFileUploaded.HasDelegate) {
                        fileDetails.ProgressPercent = ProgressPercent;
                        await OnFileUploaded.InvokeAsync(fileDetails);
                    }

                    // Progression percent (last 50%)
                    totalRead += fileSizePart2;
                    await UpdateProgressAsync(totalRead, totalFileSizes, title);

                    break;

                default:
                    throw new ArgumentException("Invalid Mode value.");
            }

            if (fileDetails.IsCancelled) {
                break;
            }

            fileNumber++;
        }

        // Canceled or Completed
        if (uploadedFiles.Any(i => i.IsCancelled)) {
            await UpdateProgressAsync(100, ResourceLoadingCanceled);
        }
        else {
            await UpdateProgressAsync(100, ResourceLoadingCompleted);
        }

        if (OnCompleted.HasDelegate) {
            await OnCompleted.InvokeAsync([.. uploadedFiles]);
        }

        if (!_defaultHandler && OnInputFileChange.HasDelegate) {
            await OnInputFileChange.InvokeAsync(e);
        }
    }

    /// <summary>
    ///     Reads a file to buffer and raises progress events.
    /// </summary>
    /// <param name="file">       The browser file to read.</param>
    /// <param name="fileDetails">The file event details.</param>
    /// <param name="action">     The action to perform on each buffer read.</param>
    private async Task ReadFileToBufferAndRaiseProgressEventAsync(IBrowserFile file, TnTInputFileEventArgs fileDetails, Func<byte[], int, Task> action) {
        await using var readStream = file.OpenReadStream(MaximumFileSize);
        var bytesRead = 0;
        var buffer = new byte[BufferSize];

        // Read file
        while ((bytesRead = await readStream.ReadAsync(buffer)) != 0) {
            await action(buffer, bytesRead);

            if (ProgressPercent <= 0) {
                ProgressPercent = 1;
            }

            if (OnProgressChange.HasDelegate) {
                fileDetails.ProgressPercent = ProgressPercent;
                await OnProgressChange.InvokeAsync(fileDetails);

                if (fileDetails.IsCancelled) {
                    break;
                }
            }

            StateHasChanged();
        }

        // Uploaded event
        if (OnFileUploaded.HasDelegate) {
            fileDetails.ProgressPercent = ProgressPercent;
            await OnFileUploaded.InvokeAsync(fileDetails);
        }
    }

    /// <summary>
    ///     Updates the progress percent and title.
    /// </summary>
    /// <param name="current">The current progress value.</param>
    /// <param name="size">   The total size.</param>
    /// <param name="title">  The progress title.</param>
    private Task UpdateProgressAsync(long current, long size, string title) => UpdateProgressAsync(Convert.ToInt32(decimal.Divide(current, size <= 0 ? 1 : size) * 100), title);

    /// <summary>
    ///     Updates the progress percent and title.
    /// </summary>
    /// <param name="percent">The progress percent.</param>
    /// <param name="title">  The progress title.</param>
    private async Task UpdateProgressAsync(int percent, string title) {
        if (ProgressPercent != percent) {
            ProgressPercent = percent;
            if (ProgressPercentChanged.HasDelegate) {
                await ProgressPercentChanged.InvokeAsync(percent);
            }
        }
        if (ProgressTitle != title) {
            ProgressTitle = title;
        }
    }

    /// <summary>
    ///     Returns the CSS class name that corresponds to the specified form appearance style.
    /// </summary>
    /// <remarks>
    ///     Use this method to map a <see cref="FormAppearance" /> value to its corresponding CSS class for styling form controls. Compact variants include an additional class to indicate compact styling.
    /// </remarks>
    /// <param name="parentForm">The parent form implementing <see cref="ITnTForm" /> from which the appearance context may be derived.</param>
    /// <param name="appearance">The form appearance value for which to retrieve the associated CSS class. Must be a defined value of the <see cref="FormAppearance" /> enumeration.</param>
    /// <returns>A string containing the CSS class name that represents the given form appearance. The returned value reflects whether the appearance is filled, outlined, or compact.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="appearance" /> is not a supported <see cref="FormAppearance" /> value.</exception>
    protected string GetAppearanceClass(ITnTForm? parentForm, FormAppearance appearance) {
        var effectiveAppearance = FormAppearanceResolver.ResolveEffective(parentForm, appearance, Services);

        var appearanceClass = effectiveAppearance switch {
            FormAppearance.Filled => "tnt-form-filled",
            FormAppearance.FilledCompact => "tnt-form-filled",
            FormAppearance.FilledXS => "tnt-form-filled",
            FormAppearance.Outlined => "tnt-form-outlined",
            FormAppearance.OutlinedCompact => "tnt-form-outlined",
            FormAppearance.OutlinedXS => "tnt-form-outlined",
            _ => throw new NotSupportedException()
        };

        if (effectiveAppearance is FormAppearance.FilledCompact or FormAppearance.OutlinedCompact) {
            appearanceClass += " tnt-form-compact";
        }
        else if (effectiveAppearance is FormAppearance.FilledXS or FormAppearance.OutlinedXS) {
            appearanceClass += " tnt-form-xs";
        }
        return appearanceClass;
    }
}

/// <summary>
///     Represents a buffer containing bytes read from an uploaded file.
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public class TnTInputFileBuffer(byte[] data, int bytesRead) {

    /// <summary>
    ///     Gets the number of bytes read into the buffer.
    /// </summary>
    public int BytesRead { get; } = bytesRead;

    /// <summary>
    ///     Gets the byte array containing the data read from the file.
    /// </summary>
    public byte[] Data { get; } = data;

    /// <summary>
    ///     Appends the contents of this buffer to the specified file asynchronously.
    /// </summary>
    /// <param name="file">The path of the file to append to.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public async Task AppendToFileAsync(string file) {
        await using var stream = new FileStream(file, FileMode.Append);
        await stream.WriteAsync(Data.AsMemory(0, BytesRead));
    }

    /// <summary>
    ///     Appends the contents of this buffer to the specified <see cref="FileInfo" /> asynchronously.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo" /> representing the file to append to.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public Task AppendToFileAsync(FileInfo file) => AppendToFileAsync(file.FullName);
}

/// <summary>
///     Provides event data for file upload operations in <see cref="TnTInputFile" />.
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public sealed class TnTInputFileEventArgs : EventArgs {

    /// <summary>
    ///     Gets the collection of all files involved in the upload operation.
    /// </summary>
    public IReadOnlyList<UploadedFileDetails> AllFiles { get; internal set; } = default!;

    /// <summary>
    ///     Gets the buffer containing the bytes read from the file, if applicable.
    /// </summary>
    public TnTInputFileBuffer Buffer { get; internal set; } = default!;

    /// <summary>
    ///     Gets the MIME type of the file.
    /// </summary>
    public string ContentType { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the error message if an error occurred during the upload, otherwise <c>null</c>.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    ///     Gets the index of the file in the upload operation.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the upload operation was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    ///     Gets the last modified date and time of the file.
    /// </summary>
    public DateTimeOffset LastModified { get; internal set; } = default!;

    /// <summary>
    ///     Gets the local file information if the file was saved to a temporary folder.
    /// </summary>
    public FileInfo? LocalFile { get; internal set; }

    /// <summary>
    ///     Gets the name of the file.
    /// </summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the percentage of the upload completed for this file.
    /// </summary>
    public int ProgressPercent { get; internal set; }

    /// <summary>
    ///     Gets the progress title or status message for the upload operation.
    /// </summary>
    public string ProgressTitle { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets the size of the file in bytes.
    /// </summary>
    public long Size { get; internal set; }

    /// <summary>
    ///     Gets the stream for reading the file, if applicable.
    /// </summary>
    public Stream? Stream { get; internal set; }
}

/// <summary>
///     Represents the progress details of a file being uploaded.
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public readonly record struct ProgressFileDetails {
    /// <summary>
    ///     Gets the index of the file in the upload process.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    ///     Gets the name of the file being uploaded.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Gets the percentage of the upload completed for this file.
    /// </summary>
    public int Percentage { get; init; }
}

/// <summary>
///     Represents the details of a file that has been uploaded.
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public record struct UploadedFileDetails {
    /// <summary>
    ///     Gets the name of the uploaded file.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets or sets the size of the uploaded file in bytes.
    /// </summary>
    public required long Size { get; set; }

    /// <summary>
    ///     Gets the content type (MIME type) of the uploaded file.
    /// </summary>
    public required string ContentType { get; init; }
}

/// <summary>
///     Specifies the mode for handling input files.
/// </summary>
[System.Obsolete("This legacy Form element is obsolete. Use the NT form components instead.")]
public enum InputFileMode {

    /// <summary>
    ///     Uploaded files are saved to a temporary folder.
    /// </summary>
    SaveToTemporaryFolder,

    /// <summary>
    ///     Files are read into a buffer. to retrieve bytes.
    /// </summary>
    Buffer,

    /// <summary>
    ///     In Blazor Server, file data is streamed over the SignalR connection into .NET code on the server as the file is read. In Blazor WebAssembly, file data is streamed directly into the .NET
    ///     code within the browser.
    /// </summary>
    Stream
}
