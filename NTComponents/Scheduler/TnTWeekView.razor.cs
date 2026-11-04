using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NTComponents.Core;
using NTComponents.Scheduler;

namespace NTComponents;

/// <summary>
///     Represents a week view component for displaying events in a scheduler.
/// </summary>
/// <typeparam name="TEventType">The type of the event.</typeparam>
public partial class TnTWeekView< TEventType> where TEventType : TnTEvent {

    /// <summary>
    ///     Gets or sets the default appointment time.
    /// </summary>
    [Parameter]
    public TimeSpan DefaultAppointmentTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
            .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-week-view")
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("header-height", $"{_headerHeight}px")
        .AddVariable("cell-min-width", $"{_cellMinWidth}px")
        .AddVariable("cell-height", $"{_cellHeight}px")
        .AddVariable("time-column-width", $"{_timeColumnWidth}px")
        .AddVariable("hour-offset", $"{_hourOffset}px")
        .Build();

    /// <summary>
    ///     Gets or sets a value indicating whether to hide dates.
    /// </summary>
    [Parameter]
    public bool HideDates { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to hide event dates.
    /// </summary>
    [Parameter]
    public bool HideEventDates { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to show descriptions.
    /// </summary>
    [Parameter]
    public bool ShowDescription { get; set; }

    /// <summary>
    ///     Gets or sets the day of the week to start the view on.
    /// </summary>
    [Parameter]
    public DayOfWeek StartViewOn { get; set; } = DayOfWeek.Sunday;

    private const int _cellHeight = 48;
    private const int _cellMinWidth = 80;
    private const int _headerHeight = 80;
    private const int _hourOffset = _cellHeight;
    private const int _timeColumnWidth = 36;
    private TnTEvent? _addEventPlaceholder;
    private readonly Dictionary<DateOnly, SortedSet<WeekViewTnTEvent>> _events = [];
    private bool _mouseOverEvent;
    private bool _mouseOverPicker;
    private ImmutableSortedSet<DateOnly> _visibleDates = [];

    /// <inheritdoc />
    public override DateOnly DecrementDate(DateOnly src) => src.AddDays(-7);

    /// <inheritdoc />
    public override DateOnly GetFirstVisibleDate() => _visibleDates[0];

    /// <inheritdoc />
    public override DateOnly GetLastVisibleDate() => _visibleDates[^1];

    /// <inheritdoc />
    public override DateOnly IncrementDate(DateOnly src) => src.AddDays(7);

    /// <inheritdoc />
    public override void Refresh() {
        UpdateVisibleDates();
        UpdateEventsList();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override DateTimeOffset CalculateDateTimeOffset(double pointerYOffset, DateOnly date) {
        var time = TimeOnly.FromTimeSpan(TimeSpan.FromHours(pointerYOffset / _cellHeight));
        // Use the offset in effect on the target date, not today's, so slots in weeks across a DST change land on the clicked hour.
        return Floor(new DateTimeOffset(date, time, TimeZoneInfo.Local.GetUtcOffset(date.ToDateTime(time))), TimeSpan.FromMinutes(15));
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        UpdateVisibleDates();
        UpdateEventsList();
    }

    /// <summary>
    ///     Creates the CSS class for an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>The CSS class.</returns>
    private string? CreateEventClass(TnTEvent @event) {
        return CssClassBuilder.Create()
            .AddClass("tnt-event")
            .AddClass("tnt-interactable", Scheduler.EventClickedCallback.HasDelegate)
            .AddClass("tnt-dragging", DraggingEvent == @event)
            .AddRipple(Scheduler.EventClickedCallback.HasDelegate)
            .AddTintColor(Scheduler.EventClickedCallback.HasDelegate ? @event.TintColor : null)
            .AddOnTintColor(Scheduler.EventClickedCallback.HasDelegate ? @event.OnTintColor : null)
            .Build();
    }

    /// <summary>
    ///     Creates the CSS style for an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="left"> The left position.</param>
    /// <param name="width">The width.</param>
    /// <returns>The CSS style.</returns>
    private static string? CreateEventStyle(TnTEvent @event, int left, int width) {
        return CssStyleBuilder.Create()
            .AddVariable("tnt-event-start-hour", @event.StartTime.Hour.ToString())
            .AddVariable("tnt-event-end-hour", @event.EndTime.Hour.ToString())
            .AddVariable("tnt-event-start-min", @event.StartTime.Minute.ToString())
            .AddVariable("tnt-event-end-min", @event.EndTime.Minute.ToString())
            .AddVariable("tnt-event-bg-color", @event.BackgroundColor)
            .AddVariable("tnt-event-fg-color", @event.ForegroundColor)
            .AddStyle("left", left.ToString() + "%")
            .AddStyle("width", width.ToString() + "%")
            .Build();
    }

    /// <summary>
    ///     Creates a placeholder event.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="e">   The mouse event arguments.</param>
    private void CreatePlaceholderEvent(DateOnly date, MouseEventArgs e) {
        if (_mouseOverPicker && e.OffsetY >= 0) {
            var time = CalculateDateTimeOffset(e.OffsetY, date);
            var existingEvent = _addEventPlaceholder;
            if (existingEvent is not null) {
                existingEvent.EventStart = time;
                existingEvent.EventEnd = time.Add(DefaultAppointmentTime);
                _addEventPlaceholder = existingEvent;
            }
            else {
                _addEventPlaceholder = new TnTEvent() {
                    BackgroundColor = Scheduler.PlaceholderBackgroundColor,
                    ForegroundColor = Scheduler.PlaceholderTextColor,
                    Title = "New Event",
                    EventStart = time,
                    EventEnd = time.Add(DefaultAppointmentTime),
                };
            }
        }
    }

    /// <summary>
    ///     Updates the events list.
    /// </summary>
    private void UpdateEventsList() {
        _events.Clear();
        foreach (var @event in Scheduler.Events.Where(e => e.StartDate <= _visibleDates[^1] && e.EndDate >= _visibleDates[0]).OrderBy(e => e.EventStart)) {
            var eventStart = (@event.StartDate >= _visibleDates[0] ? @event.EventStart : new DateTimeOffset(_visibleDates[0], TimeOnly.MinValue, @event.EventStart.Offset)).ToLocalTime();
            var eventEnd = (@event.EndDate <= _visibleDates[^1] ? @event.EventEnd : new DateTimeOffset(_visibleDates[^1], TimeOnly.MaxValue, @event.EventEnd.Offset)).ToLocalTime();

            do {
                var entryEnd = new DateTimeOffset(Math.Min(eventEnd.Ticks, new DateTimeOffset(DateOnly.FromDateTime(eventStart.LocalDateTime), TimeOnly.MaxValue, eventStart.Offset).Ticks), eventStart.Offset);
                var entry = new WeekViewTnTEvent {
                    WeekViewEventEnd = entryEnd,
                    WeekViewEventStart = eventStart,
                    Event = @event
                };

                if (_events.TryGetValue(entry.StartDate, out var sortedList)) {
                    var lastEvent = sortedList.Last();
                    if (lastEvent.Event.Overlaps(entry.Event)) {
                        entry.OverlapCount = lastEvent.OverlapCount + 1;

                        if (Math.Abs((entry.StartTime - lastEvent.StartTime).Ticks) <= TimeSpan.FromMinutes(30).Ticks) {
                            lastEvent.HeaderOverlapCount ??= new HeaderOverlapCount { Count = 1 };
                            lastEvent.HeaderOverlapIndex ??= 0;
                            entry.HeaderOverlapIndex = lastEvent.HeaderOverlapIndex + 1;
                            entry.HeaderOverlapCount = lastEvent.HeaderOverlapCount;
                            entry.HeaderOverlapCount.Count++;
                        }
                    }

                    sortedList.Add(entry);
                }
                else {
                    _events.Add(entry.StartDate, new SortedSet<WeekViewTnTEvent>(new WeekViewTnTEventComparer()) { entry });
                }

                eventStart = new DateTimeOffset(DateOnly.FromDateTime(eventStart.Date), TimeOnly.MinValue, eventStart.Offset).AddDays(1);
            }
            while (eventStart < eventEnd);
        }
    }

    /// <summary>
    ///     Updates the visible dates.
    /// </summary>
    private void UpdateVisibleDates() {
        var diff = (7 + (Scheduler.Date.DayOfWeek - StartViewOn)) % 7;
        var startOfWeek = Scheduler.Date.AddDays(-1 * diff);

        _visibleDates = [.. Enumerable.Range(0, 7).Select(startOfWeek.AddDays)];
    }

    /// <summary>
    ///     Represents an event in the week view.
    /// </summary>
    private sealed record WeekViewTnTEvent {
        /// <summary>
        ///     Gets or sets the start time of the event in the week view.
        /// </summary>
        public required DateTimeOffset WeekViewEventStart { get; init; }
        /// <summary>
        ///     Gets or sets the end time of the event in the week view.
        /// </summary>
        public required DateTimeOffset WeekViewEventEnd { get; init; }
        /// <summary>
        ///     Gets or sets the overlap count of the event.
        /// </summary>
        public int OverlapCount { get; set; }
        /// <summary>
        ///     Gets or sets the header overlap index of the event.
        /// </summary>
        public int? HeaderOverlapIndex { get; set; }
        /// <summary>
        ///     Gets or sets the header overlap count of the event.
        /// </summary>
        public HeaderOverlapCount? HeaderOverlapCount { get; set; }
        /// <summary>
        ///     Gets or sets the event.
        /// </summary>
        public required TEventType Event { get; init; }

        /// <summary>
        ///     Gets the start time of the event.
        /// </summary>
        public TimeOnly StartTime => TimeOnly.FromTimeSpan(WeekViewEventStart.LocalDateTime.TimeOfDay);
        /// <summary>
        ///     Gets the end time of the event.
        /// </summary>
        public TimeOnly EndTime => TimeOnly.FromTimeSpan(WeekViewEventEnd.LocalDateTime.TimeOfDay);
        /// <summary>
        ///     Gets the start date of the event.
        /// </summary>
        public DateOnly StartDate => DateOnly.FromDateTime(WeekViewEventStart.LocalDateTime.Date);
        /// <summary>
        ///     Gets the end date of the event.
        /// </summary>
        public DateOnly EndDate => DateOnly.FromDateTime(WeekViewEventEnd.LocalDateTime.Date);
    }

    /// <summary>
    ///     Represents the header overlap count.
    /// </summary>
    private sealed class HeaderOverlapCount {

        /// <summary>
        ///     Gets or sets the count.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    ///     Compares two week view events.
    /// </summary>
    private class WeekViewTnTEventComparer : IComparer<WeekViewTnTEvent> {

        /// <inheritdoc />
        public int Compare(WeekViewTnTEvent? x, WeekViewTnTEvent? y) {
            var result = x?.Event.StartTime.CompareTo(y?.Event.StartTime);
            if (result == 0) {
                result = x?.Event.Duration.CompareTo(y?.Event.Duration);
            }
            return result.GetValueOrDefault() == 0 ? 1 : result ?? 1;
        }
    }
}