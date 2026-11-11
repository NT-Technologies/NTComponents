using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Scheduler.Infrastructure;

/// <summary>
///     Represents a range of dates that are disabled.
/// </summary>
[ExcludeFromCodeCoverage]
internal class DisabledDateRange(DateTimeOffset disabledStart, DateTimeOffset disabledEnd) : NTDisabledDateTime {

    /// <summary>
    ///     Determines whether the specified date and time is within the disabled range.
    /// </summary>
    /// <param name="dateTimeOffset">The date and time to check.</param>
    /// <returns>
    ///     <c>true</c> if the date and time is within the disabled range; otherwise, <c>false</c>.
    /// </returns>
    internal override bool IsDateTimeDisabled(DateTimeOffset dateTimeOffset) => dateTimeOffset >= disabledStart && dateTimeOffset <= disabledEnd;

    /// <summary>
    ///     Determines whether the specified day is within the disabled range.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns><c>true</c> if the day is within the disabled range; otherwise, <c>false</c>.</returns>
    internal override bool IsDayDisabled(DateOnly date) => date >= DateOnly.FromDateTime(disabledStart.Date) && date <= DateOnly.FromDateTime(disabledEnd.Date);

    /// <summary>
    ///     Determines whether the specified time slot on a given day of the week is within the
    ///     disabled range.
    /// </summary>
    /// <param name="dayOfWeek">The day of the week to check.</param>
    /// <param name="timeSlot"> The time slot to check.</param>
    /// <returns>
    ///     <c>true</c> if the time slot on the specified day is within the disabled range;
    ///     otherwise, <c>false</c>.
    /// </returns>
    internal override bool IsTimeSlotDisabled(DayOfWeek dayOfWeek, TimeOnly timeSlot) => dayOfWeek == disabledStart.DayOfWeek && dayOfWeek == disabledEnd.DayOfWeek && timeSlot >= TimeOnly.FromTimeSpan(disabledStart.TimeOfDay) && timeSlot <= TimeOnly.FromTimeSpan(disabledEnd.TimeOfDay);
}