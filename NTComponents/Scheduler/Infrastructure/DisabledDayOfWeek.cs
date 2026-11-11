using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Scheduler.Infrastructure;

/// <summary>
///     Represents a disabled day of the week with specific start and end times.
/// </summary>
/// <param name="dayOfWeekFlag">    The flag indicating the days of the week that are disabled.</param>
/// <param name="disabledStartTime">The start time when the day is considered disabled.</param>
/// <param name="disabledEndTime">  The end time when the day is considered disabled.</param>
[ExcludeFromCodeCoverage]
internal class DisabledDayOfWeek(NTDayOfWeekFlag dayOfWeekFlag, TimeOnly disabledStartTime, TimeOnly disabledEndTime) : NTDisabledDateTime {

    /// <summary>
    ///     Determines if a specific date and time is disabled.
    /// </summary>
    /// <param name="dateTimeOffset">The date and time to check.</param>
    /// <returns>True if the date and time is disabled; otherwise, false.</returns>
    internal override bool IsDateTimeDisabled(DateTimeOffset dateTimeOffset) =>
        dayOfWeekFlag.HasDay(dateTimeOffset.DayOfWeek) && TimeOnly.FromTimeSpan(dateTimeOffset.TimeOfDay) >= disabledStartTime && TimeOnly.FromTimeSpan(dateTimeOffset.TimeOfDay) < disabledEndTime;

    /// <summary>
    ///     Determines if a specific day is disabled.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the day is disabled; otherwise, false.</returns>
    internal override bool IsDayDisabled(DateOnly date) => dayOfWeekFlag.HasDay(date.DayOfWeek);

    /// <summary>
    ///     Determines if a specific time slot on a given day of the week is disabled.
    /// </summary>
    /// <param name="dayOfWeek">The day of the week to check.</param>
    /// <param name="timeSlot"> The time slot to check.</param>
    /// <returns>True if the time slot is disabled; otherwise, false.</returns>
    internal override bool IsTimeSlotDisabled(DayOfWeek dayOfWeek, TimeOnly timeSlot) => dayOfWeekFlag.HasDay(dayOfWeek) && timeSlot >= disabledStartTime && timeSlot < disabledEndTime;
}