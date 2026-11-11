namespace NTComponents;

/// <summary>
///     Precomputed option class strings shared by the combobox-style menus.
/// </summary>
internal static class NTMenuOptionClass {
    public static string ForSelectable(bool selected, bool disabled) => (selected, disabled) switch {
        (true, true) => "nt-combobox-option nt-combobox-option-selected nt-combobox-option-disabled",
        (true, false) => "nt-combobox-option nt-combobox-option-selected",
        (false, true) => "nt-combobox-option nt-combobox-option-disabled",
        _ => "nt-combobox-option"
    };

    // NTTypeahead historically built these with CssClassBuilder.Create(), which prepends nt-components.
    public static string ForActive(bool active, bool selected) => (active, selected) switch {
        (true, true) => "nt-components nt-combobox-option nt-combobox-option-active nt-combobox-option-selected",
        (true, false) => "nt-components nt-combobox-option nt-combobox-option-active",
        (false, true) => "nt-components nt-combobox-option nt-combobox-option-selected",
        _ => "nt-components nt-combobox-option"
    };
}
