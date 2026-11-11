namespace NTComponents;

/// <summary>
///     Represents the days of the week as a flag enumeration.
/// </summary>
[Flags]
public enum NTDayOfWeekFlag {

    /// <summary>
    ///     No day selected.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Represents Sunday.
    /// </summary>
    Sunday = 1,

    /// <summary>
    ///     Represents Monday.
    /// </summary>
    Monday = 2,

    /// <summary>
    ///     Represents Tuesday.
    /// </summary>
    Tuesday = 4,

    /// <summary>
    ///     Represents Wednesday.
    /// </summary>
    Wednesday = 8,

    /// <summary>
    ///     Represents Thursday.
    /// </summary>
    Thursday = 16,

    /// <summary>
    ///     Represents Friday.
    /// </summary>
    Friday = 32,

    /// <summary>
    ///     Represents Saturday.
    /// </summary>
    Saturday = 64,

    /// <summary>
    ///     Represents all days of the week.
    /// </summary>
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}
