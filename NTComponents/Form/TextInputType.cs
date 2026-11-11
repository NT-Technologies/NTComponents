namespace NTComponents;

/// <summary>
///     Specifies the type of text input.
/// </summary>
/// <remarks>
///     Prefer the most specific input type that matches the requested data. Specific native types help browsers present the
///     right keyboard, autofill, and built-in validation behavior.
/// </remarks>
public enum TextInputType {

    /// <summary>
    ///     Represents a standard text input field for general text entry.
    /// </summary>
    /// <remarks>
    ///     Use for general single-line text when no more specific native input type applies.
    /// </remarks>
    Text,

    /// <summary>
    ///     Represents an input field optimized for email address entry, typically providing email-specific validation and keyboard on mobile devices.
    /// </summary>
    /// <remarks>
    ///     Use for email addresses instead of a plain text field.
    /// </remarks>
    Email,

    /// <summary>
    ///     Represents a password input field where entered characters are obscured for security purposes.
    /// </summary>
    /// <remarks>
    ///     Use for secrets or credentials. Pair with appropriate autocomplete values such as <c>current-password</c> or
    ///     <c>new-password</c>.
    /// </remarks>
    Password,

    /// <summary>
    ///     Represents an input field optimized for telephone number entry, typically showing a numeric keypad on mobile devices.
    /// </summary>
    /// <remarks>
    ///     Use for telephone numbers instead of numeric input because phone numbers can include formatting characters and
    ///     leading zeroes.
    /// </remarks>
    Tel,

    /// <summary>
    ///     Represents an input field optimized for URL entry, typically providing URL-specific validation and keyboard features.
    /// </summary>
    /// <remarks>
    ///     Use for web addresses and pair with placeholder or supporting text only when a format example is useful.
    /// </remarks>
    Url,

    /// <summary>
    ///     Represents a search input field, often styled differently and may include features like a clear button on supported browsers.
    /// </summary>
    /// <remarks>
    ///     Use for search queries, filtering, or lookup fields where browser search-field behavior is desirable.
    /// </remarks>
    Search
}
