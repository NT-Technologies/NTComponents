namespace NTComponents.Scheduler;

/// <summary>
///     Represents an event in the <see cref="NTScheduler{TEventType}" />.
/// </summary>
public record NTEvent {
    private static int _id = 0;

    /// <summary>
    ///     Gets the unique identifier for the event.
    /// </summary>
    public int Id { get; internal set; } = Interlocked.Increment(ref _id);

    /// <summary>
    ///     Gets the title of the event.
    /// </summary>
    public string Title { get; init; } = default!;

    /// <summary>
    ///     Gets or sets the description of the event.
    /// </summary>
    public virtual string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the start date and time of the event.
    /// </summary>
    public DateTimeOffset EventStart { get; set; }

    /// <summary>
    ///     Gets or sets the end date and time of the event.
    /// </summary>
    public DateTimeOffset EventEnd { get; set; }

    /// <summary>
    ///     Gets or sets whether the event should render in the all-day lane.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    ///     Gets the start time of the event.
    /// </summary>
    public virtual TimeOnly StartTime => TimeOnly.FromTimeSpan(EventStart.LocalDateTime.TimeOfDay);

    /// <summary>
    ///     Gets the end time of the event.
    /// </summary>
    public virtual TimeOnly EndTime => TimeOnly.FromTimeSpan(EventEnd.LocalDateTime.TimeOfDay);

    /// <summary>
    ///     Gets the start date of the event.
    /// </summary>
    public virtual DateOnly StartDate => DateOnly.FromDateTime(EventStart.LocalDateTime.Date);

    /// <summary>
    ///     Gets the end date of the event.
    /// </summary>
    public virtual DateOnly EndDate => DateOnly.FromDateTime(EventEnd.LocalDateTime.Date);

    /// <summary>
    ///     Gets the duration of the event.
    /// </summary>
    public virtual TimeSpan Duration => EventEnd - EventStart;

    /// <summary>
    ///     Gets or sets the background color of the event.
    /// </summary>
    public NTColor BackgroundColor { get; set; } = NTColor.Tertiary;

    /// <summary>
    ///     Gets or sets an optional CSS color value that overrides <see cref="BackgroundColor" />.
    /// </summary>
    public string? BackgroundColorCss { get; set; }

    /// <summary>
    ///     Gets or sets the foreground color of the event.
    /// </summary>
    public NTColor ForegroundColor { get; set; } = NTColor.OnTertiary;

    /// <summary>
    ///     Gets or sets an optional CSS color value that overrides <see cref="ForegroundColor" />.
    /// </summary>
    public string? ForegroundColorCss { get; set; }

    /// <summary>
    ///     Gets or sets the tint color of the event.
    /// </summary>
    public NTColor TintColor { get; set; } = NTColor.SurfaceTint;

    /// <summary>
    ///     Gets or sets the color used on the tint of the event.
    /// </summary>
    public NTColor OnTintColor { get; set; } = NTColor.OnTertiary;

    /// <summary>
    ///     Determines whether the current event overlaps with another event.
    /// </summary>
    /// <param name="other">The other event to compare with.</param>
    /// <returns><c>true</c> if the events overlap; otherwise, <c>false</c>.</returns>
    public bool Overlaps(NTEvent other) => EventStart < other.EventEnd && other.EventStart < EventEnd;
}
