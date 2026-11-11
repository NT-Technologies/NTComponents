using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NTComponents.Scheduler;
using NTComponents.Scheduler.Events;

namespace NTComponents.Tests.Scheduler;

public sealed class NTSchedulerBranch_Tests : BunitContext {
    private readonly BunitJSModuleInterop _module;

    public NTSchedulerBranch_Tests() {
        _module = JSInterop.SetupModule(NTScheduler<NTEvent>.JsModulePathValue);
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task NotifyEventDroppedAsync_Rejects_Invalid_Requests_Without_Mutation_Or_Callback() {
        var @event = CreateEvent("Protected", 9, 10);
        var callbackCount = 0;
        var cut = RenderScheduler([@event], parameters => parameters
            .Add(p => p.AllowDraggingEvents, false)
            .Add(p => p.EventDropped, _ => callbackCount++));

        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), "2024-06-20", 600);
        cut.Render(parameters => parameters.Add(p => p.AllowDraggingEvents, true));
        await cut.Instance.NotifyEventDroppedAsync(null, "2024-06-20", 600);
        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), " ", 600);
        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), "06/20/2024", 600);
        await cut.Instance.NotifyEventDroppedAsync("missing-event", "2024-06-20", 600);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
        callbackCount.Should().Be(0);
    }

    [Fact]
    public async Task NotifyEventDroppedAsync_Reports_Clamped_Target_Without_AutoUpdating_Event() {
        var @event = CreateEvent("Preview", 8, 10);
        NTSchedulerEventDropArgs<NTEvent>? dropped = null;
        var cut = RenderScheduler([@event], parameters => parameters
            .Add(p => p.AutoUpdateEventsOnDrop, false)
            .Add(p => p.EventDropped, args => dropped = args));

        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), "2024-06-20", -100);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 12, 8, 0, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
        dropped.Should().NotBeNull();
        dropped!.NewStart.Should().Be(new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero));
        dropped.NewEnd.Should().Be(new DateTimeOffset(2024, 6, 20, 2, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task NotifyEventDroppedAsync_Without_Minutes_Preserves_Display_Time() {
        var @event = CreateEvent("Keep time", 8, 10);
        var cut = RenderScheduler([@event]);

        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), "2024-06-20", null);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 20, 8, 0, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 20, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task NotifyEventResizedAsync_Rejects_Invalid_Requests_Without_Mutation_Or_Callback() {
        var @event = CreateEvent("Protected", 9, 10);
        var callbackCount = 0;
        var cut = RenderScheduler([@event], parameters => parameters
            .Add(p => p.AllowDraggingEvents, false)
            .Add(p => p.EventDropped, _ => callbackCount++));

        await cut.Instance.NotifyEventResizedAsync(@event.Id.ToString(), 500, null);
        cut.Render(parameters => parameters.Add(p => p.AllowDraggingEvents, true));
        await cut.Instance.NotifyEventResizedAsync(null, 500, null);
        await cut.Instance.NotifyEventResizedAsync(@event.Id.ToString(), null, null);
        await cut.Instance.NotifyEventResizedAsync("missing-event", 500, null);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
        callbackCount.Should().Be(0);
    }

    [Fact]
    public async Task NotifyEventResizedAsync_Clamps_CrossMidnight_End_Without_AutoUpdating_Event() {
        var @event = new NTEvent {
            Title = "Overnight",
            EventStart = new DateTimeOffset(2024, 6, 12, 23, 30, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 13, 1, 0, 0, TimeSpan.Zero)
        };
        NTSchedulerEventDropArgs<NTEvent>? resized = null;
        var cut = RenderScheduler([@event], parameters => parameters
            .Add(p => p.AutoUpdateEventsOnDrop, false)
            .Add(p => p.DragSnapMinutes, 0)
            .Add(p => p.EventDropped, args => resized = args));

        await cut.Instance.NotifyEventResizedAsync(@event.Id.ToString(), null, 20);

        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 13, 1, 0, 0, TimeSpan.Zero));
        resized.Should().NotBeNull();
        resized!.NewStart.Should().Be(new DateTimeOffset(2024, 6, 12, 23, 30, 0, TimeSpan.Zero));
        resized.NewEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 23, 45, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task NotifySlotSelectedAsync_Rejects_Incomplete_Requests_And_Clamps_Valid_Ranges() {
        var ranges = new List<NTSchedulerSlotSelectedArgs>();
        var cutWithoutCallback = RenderScheduler([]);
        await cutWithoutCallback.Instance.NotifySlotSelectedAsync("2024-06-12", 10, 20);

        var cut = RenderScheduler([], parameters => parameters.Add(p => p.SlotSelected, args => ranges.Add(args)));
        await cut.Instance.NotifySlotSelectedAsync(null, 10, 20);
        await cut.Instance.NotifySlotSelectedAsync("bad", 10, 20);
        await cut.Instance.NotifySlotSelectedAsync("2024-06-12", null, 20);
        await cut.Instance.NotifySlotSelectedAsync("2024-06-12", 10, null);
        await cut.Instance.NotifySlotSelectedAsync("2024-06-12", -10, -5);
        await cut.Instance.NotifySlotSelectedAsync("2024-06-12", 5000, 5001);

        ranges.Should().HaveCount(2);
        ranges[0].Start.Should().Be(new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
        ranges[0].End.Should().Be(new DateTimeOffset(2024, 6, 12, 0, 1, 0, TimeSpan.Zero));
        ranges[1].Start.Should().Be(new DateTimeOffset(2024, 6, 12, 23, 59, 0, TimeSpan.Zero));
        ranges[1].End.Should().Be(new DateTimeOffset(2024, 6, 13, 0, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(NTSchedulerView.Month, "2024-05-12T12:00:00+00:00")]
    [InlineData(NTSchedulerView.Week, "2024-06-05T12:00:00+00:00")]
    [InlineData(NTSchedulerView.Day, "2024-06-11T12:00:00+00:00")]
    [InlineData((NTSchedulerView)99, "2024-06-12T12:00:00+00:00")]
    public void Previous_Navigation_Uses_The_Selected_View_Interval(NTSchedulerView view, string expectedDateText) {
        DateTimeOffset? changedDate = null;
        var cut = RenderScheduler([], parameters => parameters
            .Add(p => p.View, view)
            .Add(p => p.DateChanged, date => changedDate = date));

        cut.Find("button[aria-label='Previous date range']").Click();

        changedDate.Should().Be(DateTimeOffset.Parse(expectedDateText));
    }

    [Theory]
    [InlineData(NTSchedulerView.Month, "2024-07-12T12:00:00+00:00")]
    [InlineData(NTSchedulerView.Day, "2024-06-13T12:00:00+00:00")]
    [InlineData((NTSchedulerView)99, "2024-06-12T12:00:00+00:00")]
    public void Next_Navigation_Uses_The_Selected_View_Interval(NTSchedulerView view, string expectedDateText) {
        DateTimeOffset? changedDate = null;
        var cut = RenderScheduler([], parameters => parameters
            .Add(p => p.View, view)
            .Add(p => p.DateChanged, date => changedDate = date));

        cut.Find("button[aria-label='Next date range']").Click();

        changedDate.Should().Be(DateTimeOffset.Parse(expectedDateText));
    }

    [Fact]
    public void Event_Rendering_Honors_Template_Description_Dragging_And_Continuation_Contracts() {
        var spanning = new NTEvent {
            Title = "Spanning",
            Description = "Visible description",
            IsAllDay = true,
            EventStart = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 30, 0, 0, 0, TimeSpan.Zero)
        };
        var cut = RenderScheduler([spanning], parameters => parameters
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.AllowDraggingEvents, false)
            .Add(p => p.ShowDescription, true));

        var segment = cut.Find(".event");
        segment.ClassList.Should().Contain("event-continues-before");
        segment.ClassList.Should().Contain("event-continues-after");
        segment.ClassList.Should().NotContain("event-draggable");
        segment.QuerySelector(".event-description")!.TextContent.Should().Be("Visible description");
        segment.QuerySelectorAll("[data-nt-scheduler-resize-edge]").Should().BeEmpty();

        cut.Render(parameters => parameters.Add(p => p.EventTemplate, @event => builder => builder.AddContent(0, $"Custom: {@event.Title}")));

        cut.Find(".event").TextContent.Should().Contain("Custom: Spanning");
        cut.FindAll(".event-title").Should().BeEmpty();
    }

    [Fact]
    public void Event_Ending_At_Midnight_Renders_Only_On_The_Preceding_Day() {
        var midnightEnd = new NTEvent {
            Title = "Late shift",
            EventStart = new DateTimeOffset(2024, 6, 12, 22, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 13, 0, 0, 0, TimeSpan.Zero)
        };
        var cut = RenderScheduler([midnightEnd], parameters => parameters.Add(p => p.View, NTSchedulerView.Week));

        var segment = cut.Find(".event-timed");
        segment.GetAttribute("style").Should().Contain("--event-start-minute:1320;");
        segment.TextContent.Should().Contain("Late shift");
        cut.FindAll(".event-timed").Should().HaveCount(1);
    }

    [Fact]
    public void Month_View_Ignores_Events_Outside_The_Visible_Grid() {
        var outside = CreateEvent("Outside", 9, 10, new DateOnly(2025, 1, 1));
        var cut = RenderScheduler([outside], parameters => parameters.Add(p => p.View, NTSchedulerView.Month));

        cut.FindAll(".event").Should().BeEmpty();
        cut.Markup.Should().NotContain("Outside");
    }

    [Fact]
    public async Task Disposal_Notifies_The_Loaded_JavaScript_Module() {
        var cut = RenderScheduler([]);

        await cut.Instance.DisposeAsync();

        JSInterop.VerifyInvoke("onDispose", 1);
    }

    [Fact]
    public void JavaScript_Disconnection_During_Initial_Render_Is_Handled() {
        _module.SetupVoid("onLoad", _ => true).SetException(new JSDisconnectedException("Disconnected"));

        Action render = () => RenderScheduler([]);

        render.Should().NotThrow();
    }

    private IRenderedComponent<NTScheduler<NTEvent>> RenderScheduler(ICollection<NTEvent> events, Action<ComponentParameterCollectionBuilder<NTScheduler<NTEvent>>>? configure = null) {
        return Render<NTScheduler<NTEvent>>(parameters => {
            parameters
                .Add(p => p.Events, events)
                .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
                .Add(p => p.TimeZone, TimeZoneInfo.Utc);
            configure?.Invoke(parameters);
        });
    }

    private static NTEvent CreateEvent(string title, int startHour, int endHour, DateOnly? date = null) {
        var eventDate = date ?? new DateOnly(2024, 6, 12);
        return new NTEvent {
            Title = title,
            EventStart = new DateTimeOffset(eventDate, new TimeOnly(startHour, 0), TimeSpan.Zero),
            EventEnd = new DateTimeOffset(eventDate, new TimeOnly(endHour, 0), TimeSpan.Zero)
        };
    }
}
