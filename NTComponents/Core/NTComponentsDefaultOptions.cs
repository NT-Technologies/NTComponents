using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Core;

/// <summary>
/// Provides global default options for NTComponents, allowing host applications to configure baseline behavior across
/// all components.
/// </summary>
public class NTComponentsDefaultOptions {
    /// <summary>
    /// Gets the built-in fallback defaults used when no configured options instance has been registered in DI.
    /// </summary>
    public static readonly NTComponentsDefaultOptions Default = new();

    /// <summary>
    /// Gets or sets the default <see cref="FormAppearance"/> applied to form components when no explicit appearance is
    /// specified. Defaults to <see cref="FormAppearance.OutlinedCompact"/>.
    /// </summary>
    public FormAppearance DefaultFormAppearance { get; set; } = FormAppearance.OutlinedCompact;

    /// <summary>
    /// Gets or sets the largest value, in bytes, that the server accepts from an <see cref="NTRichTextEditor"/> as one
    /// streamed update. The server rejects a larger stream before reading it, so the value is not applied. Values short
    /// enough to send inline are limited by the hub's <c>MaximumReceiveMessageSize</c> instead. Defaults to 50 MB.
    /// </summary>
    public long RichTextEditorMaxValueBytes { get; set; } = 50 * 1024 * 1024;
}
