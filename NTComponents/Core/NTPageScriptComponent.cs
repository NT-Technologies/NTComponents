using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Ext;
using NTComponents.Interfaces;

namespace NTComponents.Core;

/// <summary>
///     Represents a base class for components that have an isolated JavaScript module.
/// </summary>
/// <typeparam name="TDerived">The type of the component. Must match the derived class type (CRTP pattern).</typeparam>
public abstract class NTPageScriptComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TDerived> : NTDisposableComponentBase, INTPageScriptComponent<TDerived> where TDerived : ComponentBase {

    /// <inheritdoc />
    public DotNetObjectReference<TDerived>? DotNetObjectRef { get; set; }

    /// <inheritdoc />
    public IJSObjectReference? IsolatedJsModule { get; private set; }

    /// <inheritdoc />
    public abstract string? JsModulePath { get; }

    /// <summary>
    ///     The JSRuntime instance used for JavaScript interop.
    /// </summary>
    [Inject]
    protected IJSRuntime JSRuntime { get; private set; } = default!;

    private RenderFragment? _pageScript;

    /// <summary>
    ///     Gets the render fragment for the page script. Always uses the latest JsModulePath.
    /// </summary>
    protected RenderFragment PageScript => _pageScript ??= builder => {
        builder.OpenComponent<NTPageScript>(0);
        builder.AddAttribute(1, "Src", JsModulePath);
        builder.CloseComponent();
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="NTPageScriptComponent{TDerived}" /> class. The type parameter TDerived must match the actual derived class (CRTP pattern).
    /// </summary>
    protected NTPageScriptComponent() {
        if (this is not TDerived derived) {
            throw new InvalidCastException($"NTPageScriptComponent: TDerived must match the actual derived class type. Got {GetType().Name} but expected {typeof(TDerived).Name}.");
        }
        DotNetObjectRef = DotNetObjectReference.Create(derived);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            DotNetObjectRef?.Dispose();
            DotNetObjectRef = null;
            // Do not dispose IsolatedJsModule here; it should be disposed asynchronously in DisposeAsyncCore.
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        if (IsolatedJsModule is not null) {
            try {
                await IsolatedJsModule.InvokeVoidAsync("onDispose", Element, DotNetObjectRef);
                await IsolatedJsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during disposal.
            }
            IsolatedJsModule = null;
        }

        if (DotNetObjectRef is IAsyncDisposable asyncDisposable) {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else {
            DotNetObjectRef?.Dispose();
        }
        DotNetObjectRef = null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);

        if (!DisposalStarted) {
            try {
                if (firstRender) {
                    var importedModule = await JSRuntime.ImportIsolatedJs(this, JsModulePath);
                    if (!DisposalStarted) {
                        IsolatedJsModule = importedModule;
                        if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                            await module.InvokeVoidAsync("onLoad", Element, dotNetRef);
                        }
                    }
                    else {
                        await importedModule.DisposeAsync().ConfigureAwait(false);
                    }
                }

                if (TryGetInteropReferences(out var currentModule, out var currentDotNetRef)) {
                    await currentModule.InvokeVoidAsync("onUpdate", Element, currentDotNetRef);
                }
            }
            catch (JSDisconnectedException) {
                // JS runtime was disconnected, safe to ignore during render.
            }
        }
    }

    /// <summary>
    ///     Attempts to get the JavaScript module and .NET object reference when the component is still available for interop.
    /// </summary>
    /// <param name="module">The loaded JavaScript module.</param>
    /// <param name="dotNetRef">The .NET object reference passed to JavaScript.</param>
    /// <returns><see langword="true" /> when both references are available and disposal has not started; otherwise, <see langword="false" />.</returns>
    protected bool TryGetInteropReferences([NotNullWhen(true)] out IJSObjectReference? module, [NotNullWhen(true)] out DotNetObjectReference<TDerived>? dotNetRef) {
        module = IsolatedJsModule;
        dotNetRef = DotNetObjectRef;
        return !DisposalStarted && module is not null && dotNetRef is not null;
    }
}
