namespace NTComponents;

/// <summary>
///     Provides extension methods for the <see cref="InputType" /> enum.
/// </summary>
public static class InputTypeExt {

    /// <summary>
    ///     Converts the <see cref="InputType" /> to its corresponding string representation.
    /// </summary>
    /// <param name="inputType">The input type to convert.</param>
    /// <returns>The string representation of the input type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input type is not valid.</exception>
    public static string ToInputTypeString(this InputType inputType) {
        return inputType switch {
            InputType.Button => "button",
            InputType.Checkbox => "checkbox",
            InputType.Color => "color",
            InputType.Date => "date",
            InputType.DateTime => "datetime-local",
            InputType.Email => "email",
            InputType.File => "file",
            InputType.Hidden => "hidden",
            InputType.Image => "image",
            InputType.Month => "month",
            InputType.Number => "number",
            InputType.Password => "password",
            InputType.Radio => "radio",
            InputType.Range => "range",
            InputType.Search => "search",
            InputType.Tel => "tel",
            InputType.Text => "text",
            InputType.Time => "time",
            InputType.Url => "url",
            InputType.Week => "week",
            InputType.Currency => "text",
            _ => throw new InvalidOperationException($"{inputType} is not a valid value of {nameof(InputType)}")
        };
    }
}
