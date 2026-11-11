using Microsoft.JSInterop;
using NTComponents.Core;

namespace NTComponents.Snackbar;

/// <summary>
///     Service for coordinating snackbar presentation and action execution through the snackbar JavaScript module.
/// </summary>
internal sealed class NTSnackbarService(IJSRuntime _jsRuntime) : INTSnackbarService, IAsyncDisposable {
    private const string _dotNetActionMethod = nameof(InvokeActionFromJavaScript);
    private const string _dotNetCloseMethod = nameof(NotifyClosedFromJavaScript);
    private readonly Dictionary<string, NTSnackbarImplementation> _snackbarsById = [];
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private DotNetObjectReference<NTSnackbarService>? _dotNetReference;
    private IJSObjectReference? _module;

    internal INTSnackbar? ActiveSnackbar {
        get {
            lock (_snackbarsById) {
                return _snackbarsById.Values.FirstOrDefault();
            }
        }
    }

    public event INTSnackbarService.OnCloseCallback? OnClose;

    public event INTSnackbarService.OnOpenCallback? OnOpen;

    public async Task CloseAsync(INTSnackbar snackbar) {
        if (snackbar is not NTSnackbarImplementation implementation || !TryGetTrackedSnackbar(implementation.Id, out var trackedSnackbar)) {
            return;
        }

        var module = await GetModuleAsync();
        var closed = await module.InvokeAsync<bool>("closeSnackbarFromBlazor", trackedSnackbar.Id);
        if (!closed || !RemoveTrackedSnackbar(trackedSnackbar.Id, out trackedSnackbar)) {
            return;
        }

        await (OnClose?.Invoke(trackedSnackbar) ?? Task.CompletedTask);
    }

    public async ValueTask DisposeAsync() {
        if (_module is not null) {
            await _module.DisposeAsync();
        }

        _dotNetReference?.Dispose();
        _moduleLock.Dispose();
    }

    public Task InvokeActionAsync(INTSnackbar snackbar) => InvokeActionAsync(snackbar, 0);

    public async Task InvokeActionAsync(INTSnackbar snackbar, int actionIndex) {
        if (snackbar is not NTSnackbarImplementation implementation || !TryGetAction(implementation, actionIndex, out var action)) {
            return;
        }

        if (await action.Callback()) {
            await CloseAsync(snackbar);
        }
    }

    public async Task ShowAsync(
        string message,
        string? actionLabel = null,
        Func<Task>? actionCallback = null,
        int? timeout = null,
        bool? showClose = null,
        NTColor backgroundColor = NTColor.InverseSurface,
        NTColor textColor = NTColor.InverseOnSurface,
        NTColor actionColor = NTColor.InversePrimary
    ) {
        var hasAction = actionCallback is not null || !string.IsNullOrWhiteSpace(actionLabel);
        if (hasAction && (actionCallback is null || string.IsNullOrWhiteSpace(actionLabel))) {
            throw new ArgumentException("Snackbar actions require both an action label and an action callback.");
        }

        var actions = hasAction
            ? new[] { new NTSnackbarAction(actionLabel!, async () => {
                await actionCallback!();
                return true;
            }) }
            : [];

        await ShowAsync(message, actions, timeout, showClose, backgroundColor, textColor, actionColor);
    }

    public async Task ShowAsync(
        string message,
        IReadOnlyList<NTSnackbarAction> actions,
        int? timeout = null,
        bool? showClose = null,
        NTColor backgroundColor = NTColor.InverseSurface,
        NTColor textColor = NTColor.InverseOnSurface,
        NTColor actionColor = NTColor.InversePrimary
    ) {
        ArgumentNullException.ThrowIfNull(actions);
        if (actions.Any(static action => action is null)) {
            throw new ArgumentException("Snackbar actions cannot contain null values.", nameof(actions));
        }

        var materializedActions = actions.ToArray();
        var hasAction = materializedActions.Length > 0;
        var snackbar = new NTSnackbarImplementation() {
            Id = NTComponentIdentifier.NewId(),
            Message = message,
            Actions = materializedActions,
            Timeout = timeout ?? (hasAction ? 0 : 4),
            ShowClose = showClose ?? hasAction,
            BackgroundColor = backgroundColor,
            TextColor = textColor,
            ActionColor = actionColor
        };

        TrackSnackbar(snackbar);
        try {
            var module = await GetModuleAsync();
            await module.InvokeAsync<string>(
                "queueSnackbarFromBlazor",
                snackbar.Id,
                snackbar.Message,
                snackbar.Actions.Select(static action => action.Label).ToArray(),
                snackbar.Timeout,
                snackbar.ShowClose,
                snackbar.BackgroundColor.ToCssNTColorVariable(),
                snackbar.TextColor.ToCssNTColorVariable(),
                snackbar.ActionColor.ToCssNTColorVariable(),
                DotNetReference,
                _dotNetActionMethod,
                _dotNetCloseMethod);
        }
        catch {
            RemoveTrackedSnackbar(snackbar.Id, out _);
            throw;
        }

        await (OnOpen?.Invoke(snackbar) ?? Task.CompletedTask);
    }

    [JSInvokable]
    public async Task<bool> InvokeActionFromJavaScript(string id, int actionIndex) {
        if (!TryGetTrackedSnackbar(id, out var snackbar) || !TryGetAction(snackbar, actionIndex, out var action)) {
            return false;
        }

        return await action.Callback();
    }

    [JSInvokable]
    public async Task NotifyClosedFromJavaScript(string id) {
        if (!RemoveTrackedSnackbar(id, out var snackbar)) {
            return;
        }

        await (OnClose?.Invoke(snackbar) ?? Task.CompletedTask);
    }

    private DotNetObjectReference<NTSnackbarService> DotNetReference => _dotNetReference ??= DotNetObjectReference.Create(this);

    private async ValueTask<IJSObjectReference> GetModuleAsync() {
        if (_module is not null) {
            return _module;
        }

        await _moduleLock.WaitAsync();
        try {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", NTSnackbar.JsModulePathValue);
            return _module;
        }
        finally {
            _moduleLock.Release();
        }
    }

    private bool RemoveTrackedSnackbar(string id, out NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            if (!_snackbarsById.Remove(id, out var trackedSnackbar)) {
                snackbar = default!;
                return false;
            }

            snackbar = trackedSnackbar;
            return true;
        }
    }

    private void TrackSnackbar(NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            _snackbarsById[snackbar.Id] = snackbar;
        }
    }

    private bool TryGetTrackedSnackbar(string id, out NTSnackbarImplementation snackbar) {
        lock (_snackbarsById) {
            if (!_snackbarsById.TryGetValue(id, out var trackedSnackbar)) {
                snackbar = default!;
                return false;
            }

            snackbar = trackedSnackbar;
            return true;
        }
    }

    private static bool TryGetAction(NTSnackbarImplementation snackbar, int actionIndex, out NTSnackbarAction action) {
        if (actionIndex < 0 || actionIndex >= snackbar.Actions.Count) {
            action = default!;
            return false;
        }

        action = snackbar.Actions[actionIndex];
        return true;
    }

    /// <summary>
    ///     Internal snackbar implementation stored by the snackbar service.
    /// </summary>
    internal sealed class NTSnackbarImplementation : INTSnackbar {
        public string? ActionLabel => Actions.FirstOrDefault()?.Label;
        public IReadOnlyList<NTSnackbarAction> Actions { get; set; } = [];
        public NTColor ActionColor { get; set; } = NTColor.InversePrimary;
        public NTColor BackgroundColor { get; set; } = NTColor.InverseSurface;
        public bool Closing => false;
        public bool HasAction => Actions.Count > 0;
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = default!;
        public bool ShowClose { get; set; }
        public double Timeout { get; set; } = 4;
        public NTColor TextColor { get; set; } = NTColor.InverseOnSurface;
    }
}
