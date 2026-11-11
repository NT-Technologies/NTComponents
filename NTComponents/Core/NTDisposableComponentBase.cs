using Microsoft.AspNetCore.Components;

namespace NTComponents.Core;

/// <summary>
///     Provides a base class for Blazor components that require both synchronous and asynchronous disposal. Implements the recommended disposal pattern for Blazor and WebAssembly scenarios.
/// </summary>
/// <remarks>
///     Inherit from this class to ensure proper cleanup of resources, such as JS interop objects or event handlers. Override <see cref="Dispose(bool)" /> for synchronous cleanup and <see
///     cref="DisposeAsyncCore" /> for asynchronous cleanup. In Blazor, avoid touching UI or JS interop from continuations that used <c>ConfigureAwait(false)</c>; marshal back using <c>InvokeAsync</c>
///     if needed.
/// </remarks>
public abstract class NTDisposableComponentBase : NTComponentBase, IAsyncDisposable, IDisposable {

    private readonly NTDisposalState _disposalState = new();

    /// <summary>
    ///     Gets a value indicating whether disposal has started.
    /// </summary>
    protected bool DisposalStarted => _disposalState.HasStarted;

    /// <inheritdoc />
    public void Dispose() {
        if (_disposalState.TryBegin()) {
            try {
                Dispose(disposing: true);
            }
            finally {
                GC.SuppressFinalize(this);
            }
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
    ///     Called to release resources used by the component.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> when called from <see cref="Dispose()" /> for synchronous cleanup; <see langword="false" /> when called as part of <see cref="DisposeAsync()" /> after asynchronous cleanup.
    /// </param>
    /// <remarks>Override to dispose of managed resources synchronously. Do not perform asynchronous work here. Derived classes should perform their own cleanup first, then call <c>base.Dispose(disposing)</c>.</remarks>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    ///     Asynchronously releases resources used by the component.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous dispose operation.</returns>
    /// <remarks>
    ///     Override to dispose of asynchronous resources. Prefer using <c>ConfigureAwait(false)</c> inside this method. After awaiting, do not access component state or JS interop directly; marshal
    ///     back using <c>InvokeAsync</c> if UI updates are required. Derived classes should perform their own cleanup first, then <c>await base.DisposeAsyncCore()</c> if overridden further down the hierarchy.
    /// </remarks>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}

internal sealed class NTDisposalState {
    private int _started;

    internal bool HasStarted => Volatile.Read(ref _started) != 0;

    internal bool TryBegin() => Interlocked.Exchange(ref _started, 1) == 0;
}
