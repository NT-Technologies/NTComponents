namespace NTComponents;

/// <summary>
///     Extension methods for the NTDayOfWeekFlag enumeration.
/// </summary>
public static class NTDayOfWeekFlagExt {

    /// <summary>
    ///     Determines whether the specified NTDayOfWeekFlag includes the given day of the week.
    /// </summary>
    /// <param name="dayOfWeekFlag">The NTDayOfWeekFlag to check.</param>
    /// <param name="dayOfWeek">   The day of the week to check for.</param>
    /// <returns>True if the NTDayOfWeekFlag includes the specified day of the week; otherwise, false.</returns>
    public static bool HasDay(this NTDayOfWeekFlag dayOfWeekFlag, DayOfWeek dayOfWeek) => dayOfWeekFlag.HasFlag(dayOfWeek.ToNTDayOfWeekFlag());

    /// <summary>
    ///     Converts a DayOfWeek value to the corresponding NTDayOfWeekFlag value.
    /// </summary>
    /// <param name="dayOfWeek">The DayOfWeek value to convert.</param>
    /// <returns>The corresponding NTDayOfWeekFlag value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified day of the week is not valid.</exception>
    public static NTDayOfWeekFlag ToNTDayOfWeekFlag(this DayOfWeek dayOfWeek) {
        return dayOfWeek switch {
            DayOfWeek.Sunday => NTDayOfWeekFlag.Sunday,
            DayOfWeek.Monday => NTDayOfWeekFlag.Monday,
            DayOfWeek.Tuesday => NTDayOfWeekFlag.Tuesday,
            DayOfWeek.Wednesday => NTDayOfWeekFlag.Wednesday,
            DayOfWeek.Thursday => NTDayOfWeekFlag.Thursday,
            DayOfWeek.Friday => NTDayOfWeekFlag.Friday,
            DayOfWeek.Saturday => NTDayOfWeekFlag.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };
    }
}
