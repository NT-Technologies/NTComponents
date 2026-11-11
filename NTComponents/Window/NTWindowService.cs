using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents.Windowing;

/// <summary>
///     Scoped state container for windows rendered by <see cref="NTWindowHost" />.
/// </summary>
internal sealed class NTWindowService : INTWindowService {
    private readonly List<NTWindowImplementation> _windows = [];
    private INTWindow[]? _snapshot;

    public event Action? Changed;

    // Public callers get their own copy, as before; arrays are mutable through IList, so the cached snapshot must not escape.
    public IReadOnlyList<INTWindow> Windows {
        get {
            lock (_windows) {
                return [.. _windows];
            }
        }
    }

    /// <summary>
    ///     A snapshot shared by the window host between open/close changes so host renders do not copy the list each time. Never mutate it.
    /// </summary>
    internal IReadOnlyList<INTWindow> RenderSnapshot {
        get {
            lock (_windows) {
                return _snapshot ??= [.. _windows];
            }
        }
    }

    public void Close(INTWindow window) {
        ArgumentNullException.ThrowIfNull(window);

        var changed = false;
        lock (_windows) {
            if (window is NTWindowImplementation implementation) {
                changed = _windows.Remove(implementation);
                _snapshot = null;
            }
        }

        if (changed) {
            Changed?.Invoke();
        }
    }

    public INTWindow Open(string title, RenderFragment content, NTWindowState initialState = NTWindowState.Normal, NTWindowDockPosition dockPosition = NTWindowDockPosition.BottomRight) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(content);
        ValidateState(initialState);
        ValidateDockPosition(dockPosition);

        var window = new NTWindowImplementation(NTComponentIdentifier.NewId(), title, content, initialState, dockPosition);
        lock (_windows) {
            _windows.Add(window);
            _snapshot = null;
        }

        Changed?.Invoke();
        return window;
    }

    public void SetState(INTWindow window, NTWindowState state) {
        ArgumentNullException.ThrowIfNull(window);
        ValidateState(state);

        var changed = false;
        lock (_windows) {
            if (window is NTWindowImplementation implementation && _windows.Contains(implementation) && implementation.State != state) {
                implementation.State = state;
                changed = true;
            }
        }

        if (changed) {
            Changed?.Invoke();
        }
    }

    private static void ValidateState(NTWindowState state) {
        if (state is not (NTWindowState.Normal or NTWindowState.Minimized or NTWindowState.Fullscreen)) {
            throw new ArgumentOutOfRangeException(nameof(state), state, "The window state is not supported.");
        }
    }

    private static void ValidateDockPosition(NTWindowDockPosition dockPosition) {
        if (!Enum.IsDefined(dockPosition)) {
            throw new ArgumentOutOfRangeException(nameof(dockPosition), dockPosition, "The window dock position is not supported.");
        }
    }

    private sealed class NTWindowImplementation(string id, string title, RenderFragment content, NTWindowState state, NTWindowDockPosition dockPosition) : INTWindow {
        public RenderFragment Content { get; } = content;
        public NTWindowDockPosition DockPosition { get; } = dockPosition;
        public string Id { get; } = id;
        public NTWindowState State { get; set; } = state;
        public string Title { get; } = title;
    }
}
