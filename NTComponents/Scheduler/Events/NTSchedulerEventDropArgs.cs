namespace NTComponents.Scheduler.Events;

/// <summary>
///     Provides data for an event drop in <see cref="NTScheduler{TEventType}" />.
/// </summary>
/// <typeparam name="TEventType">The event type.</typeparam>
public sealed class NTSchedulerEventDropArgs<TEventType> where TEventType : NTEvent {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NTSchedulerEventDropArgs{TEventType}" /> class.
    /// </summary>
    public NTSchedulerEventDropArgs(TEventType @event, DateTimeOffset previousStart, DateTimeOffset previousEnd, DateTimeOffset newStart, DateTimeOffset newEnd) {
        Event = @event;
        PreviousStart = previousStart;
        PreviousEnd = previousEnd;
        NewStart = newStart;
        NewEnd = newEnd;
    }

    /// <summary>
    ///     Gets the event that was dropped.
    /// </summary>
    public TEventType Event { get; }

    /// <summary>
    ///     Gets the event end before the drop.
    /// </summary>
    public DateTimeOffset PreviousEnd { get; }

    /// <summary>
    ///     Gets the event start before the drop.
    /// </summary>
    public DateTimeOffset PreviousStart { get; }

    /// <summary>
    ///     Gets the event end after the drop.
    /// </summary>
    public DateTimeOffset NewEnd { get; }

    /// <summary>
    ///     Gets the event start after the drop.
    /// </summary>
    public DateTimeOffset NewStart { get; }
}
