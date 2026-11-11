using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;

namespace NTComponents.Ext;

/// <summary>
///     Extension methods for <see cref="IJSRuntime" /> to interact with JavaScript functionality in Blazor applications.
/// </summary>
[ExcludeFromCodeCoverage]
public static class JSRuntimeExt {

    /// <summary>
    ///     Downloads a file from a stream to the client's device.
    /// </summary>
    /// <param name="jsRuntime">        The JavaScript runtime instance.</param>
    /// <param name="stream">           The stream containing the file data to download.</param>
    /// <param name="fileName">         The name for the downloaded file. Defaults to "download" if not specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    public static async ValueTask DownloadFileFromStreamAsync(this IJSRuntime jsRuntime, Stream stream, string? fileName = null, CancellationToken cancellationToken = default) {
        using var streamRef = new DotNetStreamReference(stream);
        if (string.IsNullOrWhiteSpace(fileName)) {
            fileName = "download";
        }
        await jsRuntime.InvokeVoidAsync("NTComponents.downloadFileFromStream", cancellationToken, fileName, streamRef);
    }

    /// <summary>
    ///     Downloads a file from a specified URL to the client's device.
    /// </summary>
    /// <param name="jsRuntime">        The JavaScript runtime instance.</param>
    /// <param name="url">              The URL of the file to download.</param>
    /// <param name="fileName">         The name for the downloaded file. Defaults to "download" if not specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="url" /> is null.</exception>
    public static async ValueTask DownloadFromUrlAsync(this IJSRuntime jsRuntime, string url, string? fileName = null, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        if (string.IsNullOrWhiteSpace(fileName)) {
            fileName = "download";
        }
        await jsRuntime.InvokeVoidAsync("NTComponents.downloadFromUrl", cancellationToken, fileName, url);
    }

    /// <summary>
    ///     Gets the current browser location URL.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> representing the asynchronous operation that returns the current location URL as a string.</returns>
    public static ValueTask<string> GetCurrentLocation(this IJSRuntime jsRuntime) => jsRuntime.InvokeAsync<string>("NTComponents.getCurrentLocation");

    /// <summary>
    ///     Updates the browser's URI without reloading the page.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <param name="newUri">   The new URI to display in the browser's address bar.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newUri" /> is null.</exception>
    public static async ValueTask UpdateUriAsync(this IJSRuntime jsRuntime, Uri newUri) {
        await UpdateUriAsync(jsRuntime, newUri?.ToString()!);
    }

    /// <summary>
    ///     Updates the browser's URI without reloading the page.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <param name="newUri">   The new URI to display in the browser's address bar.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newUri" /> is null.</exception>
    public static async ValueTask UpdateUriAsync(this IJSRuntime jsRuntime, string newUri) {
        ArgumentNullException.ThrowIfNull(newUri, nameof(newUri));
        await jsRuntime.InvokeVoidAsync("NTComponents.updateUri", newUri);
    }

    /// <summary>
    ///     Imports a JavaScript module from the specified path.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <param name="path">     The path to the JavaScript module to import.</param>
    /// <returns>A <see cref="Task{TResult}" /> representing the asynchronous operation that returns a reference to the imported JavaScript module.</returns>
    internal static ValueTask<IJSObjectReference> Import(this IJSRuntime jsRuntime, string path) => jsRuntime.InvokeAsync<IJSObjectReference>("import", path);

    /// <summary>
    ///     Imports an isolated JavaScript module associated with a component.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime instance.</param>
    /// <param name="obj">      The component object for which to import the JavaScript module.</param>
    /// <param name="path">     Optional custom path to the JavaScript module. If null, path is derived from the component's type.</param>
    /// <returns>A <see cref="Task{TResult}" /> representing the asynchronous operation that returns a reference to the imported JavaScript module.</returns>
    internal static async Task<IJSObjectReference> ImportIsolatedJs(this IJSRuntime jsRuntime, object obj, string? path = null) {
        if (path is null) {
            var @namespace = obj.GetType().Namespace?.Split('.') ?? [];
            var name = obj.GetType().Name;
            if (name.Contains('`')) {
                name = name[..name.IndexOf('`')];
            }
            path = $"./_content/{string.Join('/', @namespace)}/{name}.razor.js";
        }
        return await jsRuntime.Import(path);
    }
}
