using NTComponents;
using NTComponents.Scheduler;
using NTComponents.Scheduler.Events;

namespace LiveTest.Client.Pages;

public partial class Scheduler {
    private static readonly TimeSpan _defaultDuration = TimeSpan.FromHours(1);
    private static readonly EventColorOption[] _eventColorOptions = [
        new("Red", "#FFDAD6", "#410002"),
        new("Orange", "#FFDBCA", "#351000"),
        new("Yellow", "#FFE16B", "#221B00"),
        new("Green", "#B7F2B2", "#002204"),
        new("Blue", "#D8E2FF", "#001A41"),
        new("Indigo", "#E0E0FF", "#19164A"),
        new("Purple", "#F1DAFF", "#2B0052"),
        new("Brown", "#F2DFD4", "#2A170F"),
        new("Gray", "#E2E2E2", "#1B1B1B")
    ];
    private static readonly EventColorOption _defaultEventColor = _eventColorOptions[4];
    private DateTimeOffset _displayDate = DateTimeOffset.Now;
    private NTDialog? _eventDialog;
    private EventEditor? _eventEditor;
    private NTEvent? _eventBeingEdited;
    private NTSchedulerView _selectedView = NTSchedulerView.Month;
    private List<NTEvent> _tasksList = [];
    private string? _validationMessage;

    private string EventDialogTitle => _eventBeingEdited is null ? "Add event" : "Edit event";

    protected override void OnInitialized() {
        var today = DateTimeOffset.Now.Date;
        _tasksList = [
            new NTEvent { EventStart = today.AddHours(9), EventEnd = today.AddHours(10.5), Title = "Design review", Description = "Month, week, and day scheduler review" },
            new NTEvent { EventStart = today.AddHours(11), EventEnd = today.AddHours(12), Title = "Customer call" },
            new NTEvent { EventStart = today.AddHours(11.5), EventEnd = today.AddHours(13), Title = "Overlap test" },
            new NTEvent { EventStart = today.AddDays(1).AddHours(8), EventEnd = today.AddDays(1).AddHours(9), Title = "Standup" },
            new NTEvent { EventStart = today.AddDays(2).AddHours(14), EventEnd = today.AddDays(2).AddHours(16), Title = "Implementation block" },
            new NTEvent { EventStart = today.AddDays(-2).AddHours(10), EventEnd = today.AddDays(3).AddHours(15), Title = "Multi-day launch", Description = "Spans across the active week" },
            new NTEvent { EventStart = today.AddDays(10), EventEnd = today.AddDays(14).AddHours(18), Title = "Conference", IsAllDay = true },
            new NTEvent { EventStart = today.AddMonths(1).AddDays(-3).AddHours(9), EventEnd = today.AddMonths(1).AddDays(2).AddHours(17), Title = "Month boundary", IsAllDay = true }
        ];

        for (var i = 0; i < _tasksList.Count; i++) {
            var color = _eventColorOptions[i % _eventColorOptions.Length];
            _tasksList[i].BackgroundColorCss = color.Background;
            _tasksList[i].ForegroundColorCss = color.Foreground;
        }
    }

    private async Task OpenEventEditorAsync(NTEvent @event) {
        _eventBeingEdited = @event;
        _eventEditor = EventEditor.From(@event, GetEventColor(@event));
        _validationMessage = null;
        await OpenEventDialogAsync();
    }

    private void OnEventDropped(NTSchedulerEventDropArgs<NTEvent> args) {
        Console.WriteLine($"Event dropped: {args.Event.Title} from {args.PreviousStart} to {args.NewStart}");
    }

    private async Task OpenNewEventEditorAsync(DateTimeOffset dateTimeOffset) {
        await OpenNewEventEditorAsync(dateTimeOffset, dateTimeOffset.Add(_defaultDuration));
    }

    private async Task OpenNewEventEditorAsync(NTSchedulerSlotSelectedArgs selection) {
        await OpenNewEventEditorAsync(selection.Start, selection.End);
    }

    private async Task OpenNewEventEditorAsync(DateTimeOffset eventStart, DateTimeOffset eventEnd) {
        _eventBeingEdited = null;
        _eventEditor = new EventEditor { EventStart = eventStart, EventEnd = eventEnd };
        _validationMessage = null;
        await OpenEventDialogAsync();
    }

    private async Task OpenEventDialogAsync() {
        if (_eventDialog is not null) {
            await _eventDialog.OpenAsync();
        }
    }

    private async Task CloseEventEditorAsync() {
        if (_eventDialog is not null) {
            await _eventDialog.CloseAsync("cancel");
        }
    }

    private static EventColorOption? GetEventColor(NTEvent @event) => _eventColorOptions.FirstOrDefault(color =>
        string.Equals(color.Background, @event.BackgroundColorCss, StringComparison.OrdinalIgnoreCase)
        && string.Equals(color.Foreground, @event.ForegroundColorCss, StringComparison.OrdinalIgnoreCase));

    private void SelectEventColor(EventColorOption color) {
        if (_eventEditor is not null) {
            _eventEditor.Color = color;
        }
    }

    private async Task SaveEventAsync() {
        if (_eventEditor is null) {
            return;
        }

        if (string.IsNullOrWhiteSpace(_eventEditor.Title)) {
            _validationMessage = "Enter a title.";
            return;
        }

        if (_eventEditor.EventEnd <= _eventEditor.EventStart) {
            _validationMessage = "The end must be after the start.";
            return;
        }

        var title = _eventEditor.Title.Trim();
        var description = string.IsNullOrWhiteSpace(_eventEditor.Description) ? null : _eventEditor.Description.Trim();
        var backgroundColorCss = _eventEditor.Color?.Background ?? _eventEditor.BackgroundColorCss;
        var foregroundColorCss = _eventEditor.Color?.Foreground ?? _eventEditor.ForegroundColorCss;
        if (_eventBeingEdited is null) {
            _tasksList.Add(new NTEvent {
                Title = title,
                Description = description,
                EventStart = _eventEditor.EventStart,
                EventEnd = _eventEditor.EventEnd,
                IsAllDay = _eventEditor.IsAllDay,
                BackgroundColorCss = backgroundColorCss,
                ForegroundColorCss = foregroundColorCss
            });
        }
        else {
            var index = _tasksList.FindIndex(@event => @event.Id == _eventBeingEdited.Id);
            if (index >= 0) {
                _tasksList[index] = _eventBeingEdited with {
                    Title = title,
                    Description = description,
                    EventStart = _eventEditor.EventStart,
                    EventEnd = _eventEditor.EventEnd,
                    IsAllDay = _eventEditor.IsAllDay,
                    BackgroundColorCss = backgroundColorCss,
                    ForegroundColorCss = foregroundColorCss
                };
            }
        }

        if (_eventDialog is not null) {
            await _eventDialog.CloseAsync("save");
        }
    }

    private sealed class EventEditor {
        public string? BackgroundColorCss { get; set; }

        public EventColorOption? Color { get; set; } = _defaultEventColor;

        public string? Description { get; set; }

        public DateTimeOffset EventEnd { get; set; }

        public DateTimeOffset EventStart { get; set; }

        public string? ForegroundColorCss { get; set; }

        public bool IsAllDay { get; set; }

        public string Title { get; set; } = string.Empty;

        public static EventEditor From(NTEvent @event, EventColorOption? color) => new() {
            BackgroundColorCss = @event.BackgroundColorCss,
            Color = color,
            Title = @event.Title,
            Description = @event.Description,
            EventStart = @event.EventStart,
            EventEnd = @event.EventEnd,
            ForegroundColorCss = @event.ForegroundColorCss,
            IsAllDay = @event.IsAllDay
        };
    }

    private sealed record EventColorOption(string Name, string Background, string Foreground) {
        public string Style => $"--event-color-background:{Background};--event-color-foreground:{Foreground};";
    }
}
