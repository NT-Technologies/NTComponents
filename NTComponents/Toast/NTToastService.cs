using Microsoft.JSInterop;
using NTComponents.Core;

namespace NTComponents.Toast;

/// <summary>
///     Service for coordinating toast presentation through the toast JavaScript module.
/// </summary>
internal sealed class NTToastService(IJSRuntime _jsRuntime) : INTToastService, IAsyncDisposable {
    private const string _dotNetCloseMethod = nameof(NotifyClosedFromJavaScript);
    private readonly object _dotNetReferenceLock = new();
    private readonly Dictionary<string, NTToastImplementation> _toastsById = [];
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private DotNetObjectReference<NTToastService>? _dotNetReference;
    private IJSObjectReference? _module;

    internal IReadOnlyList<INTToast> ActiveToasts {
        get {
            lock (_toastsById) {
                return _toastsById.Values.ToArray();
            }
        }
    }

    public event INTToastService.OnCloseCallback? OnClose;

    public event INTToastService.OnOpenCallback? OnOpen;

    public async Task CloseAsync(INTToast toast) {
        if (toast is not NTToastImplementation implementation || !TryGetTrackedToast(implementation.Id, out var trackedToast)) {
            return;
        }

        var module = await GetModuleAsync();
        var closed = await module.InvokeAsync<bool>("closeToastFromBlazor", trackedToast.Id);
        if (!closed || !RemoveTrackedToast(trackedToast.Id, out trackedToast)) {
            return;
        }

        await (OnClose?.Invoke(trackedToast) ?? Task.CompletedTask);
    }

    public async ValueTask DisposeAsync() {
        try {
            if (_module is not null) {
                var trackedToastIds = GetTrackedToastIds();
                if (trackedToastIds.Length > 0) {
                    await _module.InvokeVoidAsync("clearToastsFromBlazor", trackedToastIds);
                }

                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) {
            // Server circuits can disconnect before scoped services are disposed.
        }

        ClearTrackedToasts();
        _module = null;
        _dotNetReference?.Dispose();
        _dotNetReference = null;
        _moduleLock.Dispose();
    }

    public async Task ShowAsync(string title, string? message = null, NTToastVariant variant = NTToastVariant.Default, int? timeout = null, bool showClose = true, string? icon = null, NTColor? backgroundColor = null, NTColor? textColor = null, NTColor? iconColor = null) {
        var defaults = NTToastDefaults.ForVariant(variant);
        var toast = new NTToastImplementation {
            Id = NTComponentIdentifier.NewId(),
            BackgroundColor = backgroundColor ?? defaults.BackgroundColor,
            Icon = string.IsNullOrWhiteSpace(icon) ? defaults.Icon : icon,
            IconColor = iconColor ?? defaults.IconColor,
            Message = message,
            ShowClose = showClose,
            TextColor = textColor ?? defaults.TextColor,
            Timeout = timeout ?? 4,
            Title = title,
            Variant = variant
        };

        TrackToast(toast);
        try {
            var module = await GetModuleAsync();
            await module.InvokeAsync<string>(
                "queueToastFromBlazor",
                toast.Id,
                toast.Title,
                toast.Message,
                toast.Variant.ToString().ToLowerInvariant(),
                toast.Timeout,
                toast.ShowClose,
                toast.Icon,
                backgroundColor is not null ? toast.BackgroundColor.ToCssNTColorVariable() : null,
                textColor is not null ? toast.TextColor.ToCssNTColorVariable() : null,
                iconColor is not null ? toast.IconColor.ToCssNTColorVariable() : null,
                DotNetReference,
                _dotNetCloseMethod);
        }
        catch {
            RemoveTrackedToast(toast.Id, out _);
            throw;
        }

        await (OnOpen?.Invoke(toast) ?? Task.CompletedTask);
    }

    [JSInvokable]
    public async Task NotifyClosedFromJavaScript(string id) {
        if (!RemoveTrackedToast(id, out var toast)) {
            return;
        }

        await (OnClose?.Invoke(toast) ?? Task.CompletedTask);
    }

    private DotNetObjectReference<NTToastService> DotNetReference {
        get {
            if (_dotNetReference is not null) {
                return _dotNetReference;
            }

            lock (_dotNetReferenceLock) {
                return _dotNetReference ??= DotNetObjectReference.Create(this);
            }
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync() {
        if (_module is not null) {
            return _module;
        }

        await _moduleLock.WaitAsync();
        try {
            try {
                _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", NTToast.JsModulePathValue);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("INTToastService requires an interactive Blazor render context. For static SSR markup or prerendered lifecycle code, trigger toasts from browser JavaScript with window.NTToast?.queueToast(...).", ex);
            }

            return _module;
        }
        finally {
            _moduleLock.Release();
        }
    }

    private bool RemoveTrackedToast(string id, out NTToastImplementation toast) {
        lock (_toastsById) {
            if (!_toastsById.Remove(id, out var trackedToast)) {
                toast = default!;
                return false;
            }

            toast = trackedToast;
            return true;
        }
    }

    private void ClearTrackedToasts() {
        lock (_toastsById) {
            _toastsById.Clear();
        }
    }

    private string[] GetTrackedToastIds() {
        lock (_toastsById) {
            return _toastsById.Keys.ToArray();
        }
    }

    private void TrackToast(NTToastImplementation toast) {
        lock (_toastsById) {
            _toastsById[toast.Id] = toast;
        }
    }

    private bool TryGetTrackedToast(string id, out NTToastImplementation toast) {
        lock (_toastsById) {
            if (!_toastsById.TryGetValue(id, out var trackedToast)) {
                toast = default!;
                return false;
            }

            toast = trackedToast;
            return true;
        }
    }

    /// <summary>
    ///     Internal toast implementation stored by the toast service.
    /// </summary>
    internal sealed class NTToastImplementation : INTToast {
        public NTColor BackgroundColor { get; set; } = NTColor.SurfaceContainerHigh;
        public string? Icon { get; set; }
        public NTColor IconColor { get; set; } = NTColor.Primary;
        public string Id { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool ShowClose { get; set; } = true;
        public NTColor TextColor { get; set; } = NTColor.OnSurface;
        public double Timeout { get; set; } = 4;
        public string Title { get; set; } = string.Empty;
        public NTToastVariant Variant { get; set; } = NTToastVariant.Default;
    }

    private readonly record struct NTToastDefaults(string? Icon, NTColor BackgroundColor, NTColor TextColor, NTColor IconColor) {
        public static NTToastDefaults ForVariant(NTToastVariant variant) {
            return variant switch {
                NTToastVariant.Success => new NTToastDefaults(MaterialIcon.CheckCircle, NTColor.SuccessContainer, NTColor.OnSuccessContainer, NTColor.Success),
                NTToastVariant.Info => new NTToastDefaults(MaterialIcon.Info, NTColor.InfoContainer, NTColor.OnInfoContainer, NTColor.Info),
                NTToastVariant.Warning => new NTToastDefaults(MaterialIcon.Warning, NTColor.WarningContainer, NTColor.OnWarningContainer, NTColor.Warning),
                NTToastVariant.Error => new NTToastDefaults(MaterialIcon.Error, NTColor.ErrorContainer, NTColor.OnErrorContainer, NTColor.Error),
                NTToastVariant.Assert => new NTToastDefaults(MaterialIcon.Rule, NTColor.AssertContainer, NTColor.OnAssertContainer, NTColor.Assert),
                _ => new NTToastDefaults(MaterialIcon.Info, NTColor.SurfaceContainerHigh, NTColor.OnSurface, NTColor.Primary)
            };
        }
    }
}
