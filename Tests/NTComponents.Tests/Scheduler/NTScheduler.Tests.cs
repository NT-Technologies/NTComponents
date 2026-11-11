using NTComponents.Scheduler;
using NTComponents.Scheduler.Events;

namespace NTComponents.Tests.Scheduler;

public class NTScheduler_Tests : BunitContext {
    public NTScheduler_Tests() {
        var module = JSInterop.SetupModule(NTScheduler<NTEvent>.JsModulePathValue);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Toolbar_Renders_Month_Picker_For_Month_View() {
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.TimeZone, TimeZoneInfo.Utc));

        var picker = cut.FindComponent<NTInputDateTime<DateOnly>>();
        picker.Instance.Value.Should().Be(new DateOnly(2024, 6, 15));
        picker.Instance.EnableCustomPicker.Should().BeTrue();
        picker.Instance.MonthOnly.Should().BeTrue();
        picker.Instance.Format.Should().BeNull();
        cut.Find("input[data-tnt-dtp-mode='month']").GetAttribute("aria-label").Should().Be("Displayed date");
        cut.FindAll(".range-title").Should().BeEmpty();
    }

    [Fact]
    public void Toolbar_Renders_Current_Date_In_Day_Picker() {
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.View, NTSchedulerView.Day));

        var picker = cut.FindComponent<NTInputDateTime<DateOnly>>();
        picker.Instance.Value.Should().Be(new DateOnly(2024, 6, 15));
        picker.Instance.MonthOnly.Should().BeFalse();
        picker.Instance.Format.Should().BeNull();
        var input = cut.Find("input[data-tnt-dtp-mode='date']");
        input.GetAttribute("aria-label").Should().Be("Displayed date");
        input.GetAttribute("value").Should().Be("2024-06-15");
    }

    [Fact]
    public void Toolbar_Renders_First_Day_Of_Week_In_Week_Picker() {
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.View, NTSchedulerView.Week));

        var picker = cut.FindComponent<NTInputDateTime<DateOnly>>();
        picker.Instance.Value.Should().Be(new DateOnly(2024, 6, 10));
        picker.Instance.MonthOnly.Should().BeFalse();
        picker.Instance.Format.Should().BeNull();
        cut.Find("input[data-tnt-dtp-mode='date']").GetAttribute("value").Should().Be("2024-06-10");
    }

    [Fact]
    public void Toolbar_Week_Picker_Updates_To_First_Day_When_Week_Changes() {
        DateTimeOffset? changedDate = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.DateChanged, value => changedDate = value)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.View, NTSchedulerView.Week));

        cut.Find("button[aria-label='Next date range']").Click();

        changedDate.Should().Be(new DateTimeOffset(2024, 6, 19, 12, 0, 0, TimeSpan.Zero));
        cut.FindComponent<NTInputDateTime<DateOnly>>().Instance.Value.Should().Be(new DateOnly(2024, 6, 17));
        cut.Find("input[data-tnt-dtp-mode='date']").GetAttribute("value").Should().Be("2024-06-17");
    }

    [Fact]
    public void Toolbar_Date_Picker_Updates_Date() {
        DateTimeOffset? changedDate = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.DateChanged, value => changedDate = value)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.View, NTSchedulerView.Week));

        cut.Find("input").Change("2024-07-03");

        changedDate.Should().Be(new DateTimeOffset(2024, 7, 3, 0, 0, 0, TimeSpan.Zero));
        cut.FindComponent<NTInputDateTime<DateOnly>>().Instance.Value.Should().Be(new DateOnly(2024, 7, 1));
    }

    [Fact]
    public void MonthView_Renders_Segments_For_MultiWeek_Event() {
        var events = new List<NTEvent> {
            new() { Title = "Month boundary", EventStart = new DateTimeOffset(2024, 6, 14, 9, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 18, 17, 0, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Month)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true));

        cut.Find(".month-view").Should().NotBeNull();
        cut.FindAll(".month-week").Should().HaveCount(6);
        cut.FindAll(".event-month").Should().HaveCount(2);
        cut.Markup.Should().Contain("Month boundary");
    }

    [Fact]
    public void Events_Use_Custom_Css_Colors_When_Provided() {
        var events = new List<NTEvent> {
            new() {
                Title = "Colored event",
                EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero),
                EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero),
                BackgroundColorCss = "#D8E2FF",
                ForegroundColorCss = "#001A41"
            }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Day)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc));

        var style = cut.Find(".event").GetAttribute("style");
        style.Should().Contain("--event-bg:#D8E2FF;");
        style.Should().Contain("--event-fg:#001A41;");
    }

    [Fact]
    public async Task MonthView_Moving_Event_Preserves_Reordered_Sibling_Events() {
        var standup = new NTEvent { Title = "Standup", EventStart = new DateTimeOffset(2024, 6, 13, 8, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 13, 9, 0, 0, TimeSpan.Zero) };
        var events = new List<NTEvent> {
            new() { Title = "Multi-day launch", EventStart = new DateTimeOffset(2024, 6, 10, 10, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 15, 15, 0, 0, TimeSpan.Zero) },
            standup,
            new() { Title = "Implementation block", EventStart = new DateTimeOffset(2024, 6, 14, 14, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 14, 16, 0, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Month)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc));

        await cut.Instance.NotifyEventDroppedAsync(standup.Id.ToString(), "2024-06-15", null);

        cut.FindAll(".event-month").Select(element => element.TextContent).Should().BeEquivalentTo("Multi-day launch", "Implementation block", "Standup");
    }

    [Fact]
    public void WeekView_Renders_AllDay_And_Timed_Events() {
        var events = new List<NTEvent> {
            new() { Title = "Timed event", EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero) },
            new() { Title = "All-day event", EventStart = new DateTimeOffset(2024, 6, 11, 8, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 13, 17, 0, 0, TimeSpan.Zero), IsAllDay = true }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true));

        cut.FindAll(".all-day-grid .event").Should().Contain(e => e.TextContent.Contains("All-day event"));
        cut.FindAll(".timed-day .event-timed").Should().Contain(e => e.TextContent.Contains("Timed event"));
        cut.FindAll(".event-time").Should().BeEmpty();
    }

    [Fact]
    public void WeekView_Renders_MultiDay_Timed_Event_In_Each_Day_Time_Grid() {
        var events = new List<NTEvent> {
            new() { Title = "Timed trip", EventStart = new DateTimeOffset(2024, 6, 11, 8, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 13, 17, 0, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc));

        cut.FindAll(".all-day-grid .event").Should().BeEmpty();
        var segments = cut.FindAll(".timed-day .event-timed");
        segments.Should().HaveCount(3);
        segments.Select(segment => segment.GetAttribute("style")).Should().Contain(style => style!.Contains("--event-start-minute:480;") && style.Contains("--event-end-minute:1440;"));
        segments.Select(segment => segment.GetAttribute("style")).Should().Contain(style => style!.Contains("--event-start-minute:0;") && style.Contains("--event-end-minute:1440;"));
        segments.Select(segment => segment.GetAttribute("style")).Should().Contain(style => style!.Contains("--event-start-minute:0;") && style.Contains("--event-end-minute:1020;"));
    }

    [Fact]
    public void WeekView_AllDay_DropZones_Pin_To_Day_Columns() {
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true));

        var dropZones = cut.FindAll(".all-day-drop-zone");
        dropZones.Should().HaveCount(7);
        for (var i = 0; i < dropZones.Count; i++) {
            dropZones[i].GetAttribute("style").Should().Contain($"grid-column: {i + 1}");
        }
    }

    [Fact]
    public void WeekView_OverlappingTimedEvents_Use_Separate_Lanes() {
        var events = new List<NTEvent> {
            new() { Title = "Nine", EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero) },
            new() { Title = "Nine fifteen", EventStart = new DateTimeOffset(2024, 6, 12, 9, 15, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 10, 15, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true));

        var styles = cut.FindAll(".event-timed").Select(e => e.GetAttribute("style")).ToArray();
        styles.Should().Contain(style => style!.Contains("--event-lane:1;") && style.Contains("--event-lane-count:2;"));
        styles.Should().Contain(style => style!.Contains("--event-lane:2;") && style.Contains("--event-lane-count:2;"));
        styles.Should().OnlyContain(style => !style!.Contains("--event-stack-offset:"));
    }

    [Fact]
    public void WeekView_NonOverlappingTimedEvents_Reuse_Lane() {
        var events = new List<NTEvent> {
            new() { Title = "Nine", EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero) },
            new() { Title = "Ten", EventStart = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 11, 0, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.StartViewOn, DayOfWeek.Monday)
            .Add(p => p.HideDateControls, true));

        var timedEvents = cut.FindAll(".event-timed");
        timedEvents.Should().HaveCount(2);
        timedEvents.Should().OnlyContain(e => e.GetAttribute("style")!.Contains("--event-lane:1;"));
        timedEvents.Should().OnlyContain(e => e.GetAttribute("style")!.Contains("--event-lane-count:1;"));
        timedEvents.Should().OnlyContain(e => !e.GetAttribute("style")!.Contains("--event-stack-offset:"));
    }

    [Theory]
    [InlineData(NTSchedulerView.Week)]
    [InlineData(NTSchedulerView.Day)]
    public void TimedEvents_Render_Start_And_End_Resize_Handles(NTSchedulerView view) {
        var events = new List<NTEvent> {
            new() { Title = "Resizable", EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero), EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero) }
        };

        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, view)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc));

        var timedEvent = cut.Find(".event-timed");
        timedEvent.QuerySelectorAll("[data-nt-scheduler-resize-edge]").Should().HaveCount(2);
        timedEvent.QuerySelector("[data-nt-scheduler-resize-edge='start']").Should().NotBeNull();
        timedEvent.QuerySelector("[data-nt-scheduler-resize-edge='end']").Should().NotBeNull();
    }

    [Fact]
    public async Task NotifyEventDroppedAsync_Moves_Event_With_DateTimeOffset_And_Raises_Callback() {
        var @event = new NTEvent {
            Title = "Move me",
            EventStart = new DateTimeOffset(2024, 6, 12, 8, 15, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 12, 9, 45, 0, TimeSpan.Zero)
        };
        var events = new List<NTEvent> { @event };
        NTSchedulerEventDropArgs<NTEvent>? droppedArgs = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.EventDropped, args => droppedArgs = args));

        await cut.Instance.NotifyEventDroppedAsync(@event.Id.ToString(), "2024-06-20", 570);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 20, 9, 30, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 20, 11, 0, 0, TimeSpan.Zero));
        droppedArgs.Should().NotBeNull();
        droppedArgs!.PreviousStart.Should().Be(new DateTimeOffset(2024, 6, 12, 8, 15, 0, TimeSpan.Zero));
        droppedArgs.NewStart.Should().Be(new DateTimeOffset(2024, 6, 20, 9, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task NotifyEventResizedAsync_Clamps_To_Fifteen_Minute_Minimum() {
        var @event = new NTEvent {
            Title = "Resize me",
            EventStart = new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero),
            EventEnd = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero)
        };
        var events = new List<NTEvent> { @event };
        NTSchedulerEventDropArgs<NTEvent>? resizeArgs = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, events)
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.EventDropped, args => resizeArgs = args));

        await cut.Instance.NotifyEventResizedAsync(@event.Id.ToString(), 595, null);

        @event.EventStart.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 45, 0, TimeSpan.Zero));
        @event.EventEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
        resizeArgs.Should().NotBeNull();
        resizeArgs!.PreviousStart.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 0, 0, TimeSpan.Zero));
        resizeArgs.NewStart.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 45, 0, TimeSpan.Zero));
        resizeArgs.NewEnd.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task NotifySlotSelectedAsync_Raises_Selected_Time_Range() {
        NTSchedulerSlotSelectedArgs? selectedArgs = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, NTSchedulerView.Week)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.SlotSelected, args => selectedArgs = args));

        await cut.Instance.NotifySlotSelectedAsync("2024-06-12", 555, 645);

        cut.Find("section").GetAttribute("data-nt-scheduler-range-select-enabled").Should().Be("true");
        selectedArgs.Should().NotBeNull();
        selectedArgs!.Start.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 15, 0, TimeSpan.Zero));
        selectedArgs.End.Should().Be(new DateTimeOffset(2024, 6, 12, 10, 45, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(NTSchedulerView.Week)]
    [InlineData(NTSchedulerView.Day)]
    public void TimedView_Clicking_Empty_Slot_Uses_Nearest_Snap_Interval(NTSchedulerView view) {
        DateTimeOffset? clickedSlot = null;
        var cut = Render<NTScheduler<NTEvent>>(parameters => parameters
            .Add(p => p.Events, new List<NTEvent>())
            .Add(p => p.Date, new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero))
            .Add(p => p.View, view)
            .Add(p => p.HideDateControls, true)
            .Add(p => p.TimeZone, TimeZoneInfo.Utc)
            .Add(p => p.SlotClicked, slot => clickedSlot = slot));

        cut.Find("[data-nt-scheduler-drop-date='2024-06-12'] [data-nt-scheduler-drop-slot='540']")
            .Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { OffsetY = 14 });

        clickedSlot.Should().Be(new DateTimeOffset(2024, 6, 12, 9, 15, 0, TimeSpan.Zero));
    }
}
