using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Core;

namespace NTComponents;

/// <summary>
/// MaterialIcon component for displaying Material Design icons.
/// </summary>
public sealed partial class MaterialIcon : NTIcon {

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-icon")
        .AddClass("material-symbols-outlined", Appearance is IconAppearance.Default or IconAppearance.Filled or IconAppearance.Outlined)
        .AddClass("material-symbols-sharp", Appearance == IconAppearance.Sharp)
        .AddClass("material-symbols-rounded", Appearance == IconAppearance.Round)
        .AddClass("mi-small", Size == IconSize.Small)
        .AddClass("mi-medium", Size == IconSize.Medium)
        .AddClass("mi-large", Size == IconSize.Large)
        .AddClass("mi-extra-large", Size == IconSize.ExtraLarge)
        .AddClass(AdditionalClass, !string.IsNullOrWhiteSpace(AdditionalClass))
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddStyle("font-variation-settings", "'FILL' 1", Appearance == IconAppearance.Filled)
        .AddStyle("color", Color.ToCssNTColorVariable(), Color.HasValue)
        .Build();

    /// <summary>
    /// Base constructor for MaterialIcon.
    /// </summary>
    public MaterialIcon() { }
    internal MaterialIcon(string icon) => Icon = icon;

    /// <summary>
    /// Implicit conversion from string to MaterialIcon.
    /// </summary>
    /// <param name="icon"></param>
    public static implicit operator string(MaterialIcon icon) => icon.Icon;
}
