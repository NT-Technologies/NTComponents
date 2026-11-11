using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents;

/// <summary>
/// Service contract for showing and dismissing toast notifications.
/// </summary>
/// <remarks>
/// This service uses Blazor JavaScript interop and requires an interactive render context. For static SSR-only markup,
/// place <see cref="NTToast" /> at the route or layout level and call <c> window.NTToast?.queueToast({ title: 'Saved'
/// })</c> from browser JavaScript. Use this service with <see cref="NTToast" />. Prefer the semantic extension methods for normal app feedback. Use
/// direct <see cref="ShowAsync" /> only when the toast needs a custom variant, icon, timeout, or color override.
/// </remarks>
public interface INTToastService {
    /// <summary>
    /// Event triggered when a toast closes.
    /// </summary>
    public event OnCloseCallback? OnClose;

    /// <summary>
    /// Delegate for the close event.
    /// </summary>
    /// <param name="toast">The toast being closed.</param>
    public delegate Task OnCloseCallback(INTToast toast);

    /// <summary>
    /// Event triggered when a toast opens.
    /// </summary>
    public event OnOpenCallback? OnOpen;

    /// <summary>
    /// Delegate for the open event.
    /// </summary>
    /// <param name="toast">The toast being opened.</param>
    public delegate Task OnOpenCallback(INTToast toast);

    /// <summary>
    /// Closes the supplied toast.
    /// </summary>
    /// <param name="toast">The toast to close.</param>
    Task CloseAsync(INTToast toast);

    /// <summary>
    /// Shows a toast notification through the <see cref="NTToast" /> JavaScript host.
    /// </summary>
    /// <remarks>
    /// Call this from an interactive event handler or after first interactive render. Calls during static SSR or
    /// prerendered initialization cannot use JavaScript interop. Toasts stack up to five visible messages and
    /// auto-dismiss after four seconds by default. Set <paramref name="timeout" /> to <c> 0</c> only for messages that
    /// require explicit dismissal.
    /// </remarks>
    /// <param name="title">The toast title.</param>
    /// <param name="message">Optional supporting text.</param>
    /// <param name="variant">Semantic toast variant.</param>
    /// <param name="timeout">Optional timeout in seconds. Defaults to four seconds.</param>
    /// <param name="showClose">Whether to render the close affordance.</param>
    /// <param name="icon">Optional Material Symbols icon name. When null, the variant icon is used.</param>
    /// <param name="backgroundColor">
    /// Optional toast container color. When null, the variant container color is used.
    /// </param>
    /// <param name="textColor">Optional toast text color. When null, the variant text color is used.</param>
    /// <param name="iconColor">Optional icon color. When null, the variant icon color is used.</param>
    Task ShowAsync(string title, string? message = null, NTToastVariant variant = NTToastVariant.Default, int? timeout = null, bool showClose = true, string? icon = null, NTColor? backgroundColor = null, NTColor? textColor = null, NTColor? iconColor = null);
}

/// <summary>
/// Convenience methods for semantic toast variants.
/// </summary>
public static class INTToastServiceExtensions {
    /// <summary>
    /// Shows a success toast.
    /// </summary>
    public static Task ShowSuccessAsync(this INTToastService service, string title, string? message = null, int? timeout = null, bool showClose = true) =>
        service.ShowAsync(title, message, NTToastVariant.Success, timeout, showClose);

    /// <summary>
    /// Shows an informational toast.
    /// </summary>
    public static Task ShowInfoAsync(this INTToastService service, string title, string? message = null, int? timeout = null, bool showClose = true) =>
        service.ShowAsync(title, message, NTToastVariant.Info, timeout, showClose);

    /// <summary>
    /// Shows a warning toast.
    /// </summary>
    public static Task ShowWarningAsync(this INTToastService service, string title, string? message = null, int? timeout = null, bool showClose = true) =>
        service.ShowAsync(title, message, NTToastVariant.Warning, timeout, showClose);

    /// <summary>
    /// Shows an error toast.
    /// </summary>
    public static Task ShowErrorAsync(this INTToastService service, string title, string? message = null, int? timeout = null, bool showClose = true) =>
        service.ShowAsync(title, message, NTToastVariant.Error, timeout, showClose);

    /// <summary>
    /// Shows an error toast.
    /// </summary>
    public static Task ShowErrorAsync(this INTToastService service, string title, Exception exception, int? timeout = null, bool showClose = true) => service.ShowAsync(title, exception?.Message, NTToastVariant.Error, timeout, showClose);

    /// <summary>
    /// Shows an assertion toast.
    /// </summary>
    public static Task ShowAssertAsync(this INTToastService service, string title, string? message = null, int? timeout = null, bool showClose = true) =>
        service.ShowAsync(title, message, NTToastVariant.Assert, timeout, showClose);
}
