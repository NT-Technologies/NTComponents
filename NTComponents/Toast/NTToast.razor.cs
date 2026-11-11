using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Toast;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Renders the toast host used by the toast JavaScript module.
/// </summary>
/// <remarks>
///     <para>
///         Place one <code>NTToast</code> near the route or layout level. The component loads the toast JavaScript module and registers
///         <code>window.NTToast</code>, so static server-rendered markup and interactive components in any render mode can trigger toasts through the same browser bridge.
///     </para>
///     <para>
///         <see cref="INTToastService" /> uses Blazor JavaScript interop and should be called after an interactive render context exists, such as from event handlers or after first interactive render.
///         Static SSR markup should use guarded browser JavaScript, for example <code>window.NTToast?.queueToast({ title: 'Saved' })</code>.
///     </para>
///     <para>
///         <code>NTToast</code> is the host for <see cref="INTToastService" />.
///     </para>
///     <para>
///         From JavaScript, prefer a guarded call so early clicks before the module loads do not throw:
///         <code>window.NTToast?.queueToast({ title: 'Saved', message: 'Your changes are stored.', variant: 'success' })</code>.
///         Supported JavaScript options include <code>title</code>, <code>message</code>, <code>variant</code>, <code>timeout</code>, <code>showClose</code>, <code>icon</code>,
///         <code>backgroundColor</code>, <code>textColor</code>, and <code>iconColor</code>.
///     </para>
///     <para>
///         Toasts stack up to five visible messages, queue overflow behind the stack, and auto-dismiss after four seconds unless <code>timeout: 0</code> is provided.
///         Prefer semantic variants and short status text. Use <code>timeout: 0</code> only when the user must explicitly dismiss the message.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders a toast host and registers the browser toast bridge.",
    CompatibilityDetails = "Static SSR emits the host container and page script. Displaying queued toasts requires the browser module or the interactive toast service after the page reaches the browser.")]
public partial class NTToast {
    /// <summary>
    ///     Controls where the toast stack is placed in the viewport.
    /// </summary>
    [Parameter]
    public NTToastPosition Position { get; set; } = NTToastPosition.BottomRightCorner;

    /// <summary>
    ///     The static web asset path for the toast JavaScript module.
    /// </summary>
    public const string JsModulePathValue = "./_content/NTComponents/Toast/NTToast.razor.js";

    private string ContainerClass => CssClassBuilder.Create()
        .AddClass("nt-toast-container")
        .AddClass(Position switch {
            NTToastPosition.TopLeftCorner => "nt-toast-top-left-corner",
            NTToastPosition.TopRightCorner => "nt-toast-top-right-corner",
            NTToastPosition.BottomLeftCorner => "nt-toast-bottom-left-corner",
            _ => "nt-toast-bottom-right-corner"
        })
        .Build() ?? string.Empty;

    private const string _queueScriptFormat = """
    (() => {{
        const toast = {{
    {0}
        }};
        if (window.NTToast?.queueToast) {{
            window.NTToast.queueToast(toast);
            return;
        }}

        (window.__ntToastPendingQueue ??= []).push(toast);
    }})();
    """;
    /// <summary>
    ///     Returns a static helper script that queues an assertive toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderAssertQueueScript(string title) {
        return RenderVariantQueueScript(title, NTToastVariant.Assert);
    }

    /// <summary>
    ///     Returns a static helper script that queues an assertive toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderAssertQueueScript(NTToastQueueScriptOptions options) {
        return RenderVariantQueueScript(options, NTToastVariant.Assert);
    }

    /// <summary>
    ///     Returns a static helper script that queues an error toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderErrorQueueScript(string title) {
        return RenderVariantQueueScript(title, NTToastVariant.Error);
    }

    /// <summary>
    ///     Returns a static helper script that queues an error toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderErrorQueueScript(NTToastQueueScriptOptions options) {
        return RenderVariantQueueScript(options, NTToastVariant.Error);
    }

    /// <summary>
    ///     Returns a static helper script that queues an informational toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderInfoQueueScript(string title) {
        return RenderVariantQueueScript(title, NTToastVariant.Info);
    }

    /// <summary>
    ///     Returns a static helper script that queues an informational toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderInfoQueueScript(NTToastQueueScriptOptions options) {
        return RenderVariantQueueScript(options, NTToastVariant.Info);
    }

    /// <summary>
    ///     Returns a static helper script that queues a toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderQueueScript(string title) {
        ArgumentNullException.ThrowIfNull(title);
        return RenderQueueScript(new NTToastQueueScriptOptions { Title = title });
    }

    /// <summary>
    ///     Returns a static helper script that queues a toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderQueueScript(NTToastQueueScriptOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        var title = options.Title;
        ArgumentNullException.ThrowIfNull(title);
        var properties = new StringBuilder();
        var hasPreviousProperty = false;
        AppendStringProperty(properties, ref hasPreviousProperty, "title", title);
        AppendStringProperty(properties, ref hasPreviousProperty, "variant", GetVariantScriptValue(options.Variant));
        AppendStringProperty(properties, ref hasPreviousProperty, "message", options.Message);
        AppendNumberProperty(properties, ref hasPreviousProperty, "timeout", options.Timeout);
        AppendBooleanProperty(properties, ref hasPreviousProperty, "showClose", options.ShowClose);
        AppendStringProperty(properties, ref hasPreviousProperty, "icon", options.Icon);
        AppendColorProperty(properties, ref hasPreviousProperty, "backgroundColor", options.BackgroundColor);
        AppendColorProperty(properties, ref hasPreviousProperty, "textColor", options.TextColor);
        AppendColorProperty(properties, ref hasPreviousProperty, "iconColor", options.IconColor);
        AppendStringProperty(properties, ref hasPreviousProperty, "id", options.Id);
        AppendStringProperty(properties, ref hasPreviousProperty, "host", options.Host);

        var script = string.Format(CultureInfo.InvariantCulture, _queueScriptFormat, properties);
        return builder => {
            builder.OpenElement(0, "script");
            builder.AddMarkupContent(1, script);
            builder.CloseElement();
        };
    }

    /// <summary>
    ///     Returns a static helper script that queues a success toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderSuccessQueueScript(string title) {
        return RenderVariantQueueScript(title, NTToastVariant.Success);
    }

    /// <summary>
    ///     Returns a static helper script that queues a success toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderSuccessQueueScript(NTToastQueueScriptOptions options) {
        return RenderVariantQueueScript(options, NTToastVariant.Success);
    }
    /// <summary>
    ///     Returns a static helper script that queues a warning toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderWarningQueueScript(string title) {
        return RenderVariantQueueScript(title, NTToastVariant.Warning);
    }

    /// <summary>
    ///     Returns a static helper script that queues a warning toast after the <c>window.NTToast</c> browser bridge is available.
    /// </summary>
    public static RenderFragment RenderWarningQueueScript(NTToastQueueScriptOptions options) {
        return RenderVariantQueueScript(options, NTToastVariant.Warning);
    }
    private static void AppendBooleanProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, bool? value) {
        if (value.HasValue) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append(value.Value ? "true" : "false");
        }
    }

    private static void AppendColorProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, NTColor? value) {
        if (value.HasValue) {
            AppendStringProperty(builder, ref hasPreviousProperty, name, value.Value.ToCssNTColorVariable());
        }
    }

    private static void AppendNumberProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, int? value) {
        if (value.HasValue) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendPropertyName(StringBuilder builder, ref bool hasPreviousProperty, string name) {
        if (hasPreviousProperty) {
            builder.AppendLine(",");
        }

        hasPreviousProperty = true;
        builder.Append("        ");
        builder.Append(name);
        builder.Append(": ");
    }

    private static void AppendStringProperty(StringBuilder builder, ref bool hasPreviousProperty, string name, string? value) {
        if (value is not null) {
            AppendPropertyName(builder, ref hasPreviousProperty, name);
            builder.Append('"');
            builder.Append(JavaScriptEncoder.Default.Encode(value));
            builder.Append('"');
        }
    }

    private static string GetVariantScriptValue(NTToastVariant variant) {
        return variant switch {
            NTToastVariant.Success => "success",
            NTToastVariant.Info => "info",
            NTToastVariant.Warning => "warning",
            NTToastVariant.Error => "error",
            NTToastVariant.Assert => "assert",
            _ => "default"
        };
    }

    private static RenderFragment RenderVariantQueueScript(string title, NTToastVariant variant) {
        ArgumentNullException.ThrowIfNull(title);
        return RenderQueueScript(new NTToastQueueScriptOptions { Title = title, Variant = variant });
    }

    private static RenderFragment RenderVariantQueueScript(NTToastQueueScriptOptions options, NTToastVariant variant) {
        ArgumentNullException.ThrowIfNull(options);
        return RenderQueueScript(new NTToastQueueScriptOptions {
            BackgroundColor = options.BackgroundColor,
            Host = options.Host,
            Icon = options.Icon,
            IconColor = options.IconColor,
            Id = options.Id,
            Message = options.Message,
            ShowClose = options.ShowClose,
            TextColor = options.TextColor,
            Timeout = options.Timeout,
            Title = options.Title,
            Variant = variant
        });
    }
}

/// <summary>
///     Options for rendering an <see cref="NTToast" /> static queue script.
/// </summary>
public sealed class NTToastQueueScriptOptions {
    /// <summary>
    ///     Gets or sets the toast container color.
    /// </summary>
    public NTColor? BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the host element id to target, when a specific host should receive the toast.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     Gets or sets the icon name rendered at the start of the toast, when present.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     Gets or sets the icon color.
    /// </summary>
    public NTColor? IconColor { get; set; }

    /// <summary>
    ///     Gets or sets a caller-provided toast id. Explicit ids are used to prevent duplicate pending, queued, or active toasts.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets the supporting message shown below the title.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the dismiss affordance should be shown.
    /// </summary>
    public bool? ShowClose { get; set; }

    /// <summary>
    ///     Gets or sets the text color.
    /// </summary>
    public NTColor? TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the timeout in seconds before auto-dismiss. Zero or less disables auto-dismiss.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    ///     Gets or sets the toast title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     Gets or sets the semantic toast variant.
    /// </summary>
    public NTToastVariant Variant { get; set; } = NTToastVariant.Default;
}
