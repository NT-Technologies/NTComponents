using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Scheduler.Infrastructure;

/// <summary>
///     Represents a disabled date.
/// </summary>
/// <param name="_date">The date to be disabled.</param>
[ExcludeFromCodeCoverage]
internal class DisabledDateOnly(DateOnly _date) : NTDisabledDateTime {

    /// <summary>
    ///     Determines if the specified DateTimeOffset is disabled.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to check.</param>
    /// <returns>True if the DateTimeOffset is disabled; otherwise, false.</returns>
    internal override bool IsDateTimeDisabled(DateTimeOffset dateTimeOffset) => DateOnly.FromDateTime(dateTimeOffset.Date) == _date;

    /// <summary>
    ///     Determines if the specified date is disabled.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is disabled; otherwise, false.</returns>
    internal override bool IsDayDisabled(DateOnly date) => date == _date;

    /// <summary>
    ///     Determines if the specified time slot on a given day of the week is disabled.
    /// </summary>
    /// <param name="dayOfWeek">The day of the week to check.</param>
    /// <param name="timeSlot"> The time slot to check.</param>
    /// <returns>
    ///     True if the time slot on the specified day of the week is disabled; otherwise, false.
    /// </returns>
    internal override bool IsTimeSlotDisabled(DayOfWeek dayOfWeek, TimeOnly timeSlot) => _date.DayOfWeek == dayOfWeek;
}