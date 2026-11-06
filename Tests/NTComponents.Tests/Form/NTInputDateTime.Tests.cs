using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTInputDateTime_Tests : BunitContext {

    private sealed class TestIcon : NTIcon {
        public TestIcon(string icon) => Icon = icon;

        public override string? ElementClass => $"test-picker-icon {AdditionalClass}";
        public override string? ElementStyle => null;
    }

    private sealed class DateOnlyModel {
        public DateOnly? Value { get; set; }
    }

    private sealed class DateTimeModel {
        public DateTime? Value { get; set; }
    }

    private sealed class DateTimeOffsetModel {
        public DateTimeOffset? Value { get; set; }
    }

    private sealed class TimeOnlyModel {
        public TimeOnly? Value { get; set; }
    }

    [Fact]
    public void DateTime_Renders_DateTimeLocal_Input_And_Default_Format() {
        var cut = RenderDateTime();

        cut.Find("input").GetAttribute("type").Should().Be("datetime-local");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
        cut.Instance.Type.Should().Be(InputType.DateTime);
        cut.Instance.Format.Should().BeNull();
    }

    [Fact]
    public void DateTime_Uses_Custom_Format_Without_Mutating_Parameter() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 10, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters.Add(p => p.Format, "yyyy-MM-ddTHH:mm"));

        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm");
        cut.Find("input").GetAttribute("value").Should().Be("2026-05-19T10:30");
        cut.Instance.Format.Should().Be("yyyy-MM-ddTHH:mm");
    }

    [Fact]
    public void DateTime_Custom_Picker_Uses_Effective_Format_Attribute() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 10, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Format, "yyyy-MM-ddTHH:mm"));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm");
    }

    [Fact]
    public void DateTime_Custom_Picker_Uses_Text_Input_For_NonNative_Format() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 14, 30, 0)
        };

        var cut = RenderDateTime(model, parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Format, "MM/dd/yyyy hh:mm tt"));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("text");
        input.GetAttribute("value").Should().Be("05/19/2026 02:30 PM");
        input.GetAttribute("placeholder").Should().Be("MM/dd/yyyy hh:mm tt");
        cut.Find(".nt-input").GetAttribute("class").Should().Contain("nt-input-has-placeholder");
    }

    [Fact]
    public void Custom_Picker_Explicit_Placeholder_Overrides_Format_Mask() {
        var cut = RenderDateOnly(configure: parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Format, "MM/dd/yyyy")
            .Add(p => p.Placeholder, "Choose a date"));

        cut.Find("input").GetAttribute("placeholder").Should().Be("Choose a date");
    }

    [Fact]
    public void DateOnly_NonNative_Format_Without_Custom_Picker_Preserves_Native_Input_Contract() {
        var model = new DateOnlyModel {
            Value = new DateOnly(2026, 5, 19)
        };

        var cut = RenderDateOnly(model, parameters => parameters.Add(p => p.Format, "MM/dd/yyyy"));

        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("date");
        input.GetAttribute("format").Should().Be("MM/dd/yyyy");
        input.GetAttribute("value").Should().Be("2026-05-19");
        input.GetAttribute("placeholder").Should().Be(" ");
        cut.Find(".nt-input").GetAttribute("class").Should().NotContain("nt-input-has-placeholder");

        input.Change("2000-01-01");

        model.Value.Should().Be(new DateOnly(2000, 1, 1));
        cut.Find("input").GetAttribute("value").Should().Be("2000-01-01");
    }

    [Fact]
    public void DateTimeOffset_Renders_DateTimeLocal_Input_And_Default_Format() {
        var model = new DateTimeOffsetModel {
            Value = new DateTimeOffset(2026, 5, 19, 10, 30, 0, TimeSpan.Zero)
        };

        var cut = RenderDateTimeOffset(model);

        cut.Find("input").GetAttribute("type").Should().Be("datetime-local");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-ddTHH:mm:ss");
        cut.Find("input").GetAttribute("value").Should().Be("2026-05-19T10:30:00");
        cut.Instance.Type.Should().Be(InputType.DateTime);
    }

    [Fact]
    public void DateOnly_Renders_Date_Input() {
        var cut = RenderDateOnly();

        cut.Find("input").GetAttribute("type").Should().Be("date");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM-dd");
        cut.Instance.Type.Should().Be(InputType.Date);
    }

    [Fact]
    public void DateOnly_MonthOnly_Renders_Month_Input() {
        var cut = RenderDateOnly(configure: parameters => parameters.Add(p => p.MonthOnly, true));

        cut.Find("input").GetAttribute("type").Should().Be("month");
        cut.Find("input").GetAttribute("format").Should().Be("yyyy-MM");
        cut.Instance.Type.Should().Be(InputType.Month);
    }

    [Fact]
    public void TimeOnly_Renders_Time_Input() {
        var cut = RenderTimeOnly();

        cut.Find("input").GetAttribute("type").Should().Be("time");
        cut.Find("input").GetAttribute("format").Should().Be("HH:mm:ss");
        cut.Instance.Type.Should().Be(InputType.Time);
    }

    [Fact]
    public void DateTime_Change_Updates_Value() {
        var model = new DateTimeModel();
        var cut = RenderDateTime(model);

        cut.Find("input").Change("2026-05-19T10:30:00");

        model.Value.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
    }

    [Fact]
    public void Native_Segment_Changes_Fire_BindAfter_Once_At_Blur_With_The_Settled_Value() {
        var bindAfterValues = new List<DateTime?>();
        var cut = RenderDateTime(configure: parameters => parameters
            .Add(p => p.BindAfter, EventCallback.Factory.Create<DateTime?>(this, value => bindAfterValues.Add(value))));

        cut.Find("input").Change("2026-05-19T10:30:00");
        cut.Find("input").Change("2026-05-20T10:30:00");
        cut.Find("input").Blur();

        bindAfterValues.Should().Equal(new DateTime(2026, 5, 20, 10, 30, 0));
    }

    [Fact]
    public async Task Native_Date_Awaits_Value_Callback_Before_Notifying_EditContext() {
        var model = new DateOnlyModel();
        var editContext = new EditContext(model);
        DateOnly? valueAtFieldNotification = null;
        editContext.OnFieldChanged += (_, _) => valueAtFieldNotification = model.Value;
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(p => p.Value, editContext)
            .AddChildContent<NTInputDateTime<DateOnly?>>(child => child
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, async value => {
                    await Task.Yield();
                    model.Value = value;
                }))
                .Add(p => p.ValueExpression, () => model.Value)));

        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "0002-01-01" });

        valueAtFieldNotification.Should().Be(new DateOnly(2, 1, 1));
    }

    // Behavior source: NTInputDateTime's public generic contract supports DateTime, DateTimeOffset, DateOnly, and TimeOnly values, including their non-nullable forms.
    [Fact]
    public void NonNullable_Supported_Types_Parse_Their_Native_Values() {
        var dateTime = default(DateTime);
        var dateTimeOffset = default(DateTimeOffset);
        var dateOnly = default(DateOnly);
        var timeOnly = default(TimeOnly);
        var dateTimeCut = Render<NTInputDateTime<DateTime>>(parameters => parameters
            .Add(p => p.Value, dateTime)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime>(this, value => dateTime = value))
            .Add(p => p.ValueExpression, () => dateTime));
        var dateTimeOffsetCut = Render<NTInputDateTime<DateTimeOffset>>(parameters => parameters
            .Add(p => p.Value, dateTimeOffset)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTimeOffset>(this, value => dateTimeOffset = value))
            .Add(p => p.ValueExpression, () => dateTimeOffset));
        var dateOnlyCut = Render<NTInputDateTime<DateOnly>>(parameters => parameters
            .Add(p => p.Value, dateOnly)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly>(this, value => dateOnly = value))
            .Add(p => p.ValueExpression, () => dateOnly));
        var timeOnlyCut = Render<NTInputDateTime<TimeOnly>>(parameters => parameters
            .Add(p => p.Value, timeOnly)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<TimeOnly>(this, value => timeOnly = value))
            .Add(p => p.ValueExpression, () => timeOnly));

        dateTimeCut.Find("input").Change("2026-05-19T10:30:00");
        dateTimeOffsetCut.Find("input").Change("2026-05-19T10:30:00");
        dateOnlyCut.Find("input").Change("2026-05-19");
        timeOnlyCut.Find("input").Change("10:30:00");

        dateTime.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
        dateTimeOffset.DateTime.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
        dateOnly.Should().Be(new DateOnly(2026, 5, 19));
        timeOnly.Should().Be(new TimeOnly(10, 30));
    }

    // Behavior source: NTInputDateTime supports nullable DateTimeOffset, DateOnly, and TimeOnly values and updates them from their native formats.
    [Fact]
    public void Nullable_Supported_Types_Parse_Their_Native_Values() {
        var dateTimeOffsetModel = new DateTimeOffsetModel();
        var dateOnlyModel = new DateOnlyModel();
        var timeOnlyModel = new TimeOnlyModel();
        var dateTimeOffsetCut = RenderDateTimeOffset(dateTimeOffsetModel);
        var dateOnlyCut = RenderDateOnly(dateOnlyModel);
        var timeOnlyCut = RenderTimeOnly(timeOnlyModel);

        dateTimeOffsetCut.Find("input").Change("2026-05-19T10:30:00");
        dateOnlyCut.Find("input").Change("2026-05-19");
        timeOnlyCut.Find("input").Change("10:30:00");

        dateTimeOffsetModel.Value!.Value.DateTime.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
        dateOnlyModel.Value.Should().Be(new DateOnly(2026, 5, 19));
        timeOnlyModel.Value.Should().Be(new TimeOnly(10, 30));
    }

    // Behavior source: The progressive-enhancement contract says invalid live input is reported as validation failure and must not replace the bound value.
    [Fact]
    public void Invalid_Native_Value_Does_Not_Replace_Bound_Value() {
        var model = new DateTimeModel {
            Value = new DateTime(2026, 5, 19, 10, 30, 0)
        };
        var cut = RenderDateTime(model);

        cut.Find("input").Change("not-a-date");

        model.Value.Should().Be(new DateTime(2026, 5, 19, 10, 30, 0));
    }

    [Fact]
    public void Invalid_Custom_Value_Renders_Format_Error_In_EditContext() {
        var model = new DateOnlyModel();
        var editContext = new EditContext(model);
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(p => p.Value, editContext)
            .AddChildContent<NTInputDateTime<DateOnly?>>(child => child
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value)
                .Add(p => p.EnableCustomPicker, true)
                .Add(p => p.Format, "MM/dd/yyyy")));

        cut.Find("input").Change("not-a-date");

        cut.Find(".nt-input").ClassList.Should().Contain(["nt-modified", "nt-invalid"]);
        cut.Find("input").GetAttribute("aria-invalid").Should().Be("true");
        cut.Find(".nt-input-error-text").TextContent.Should().Be("Enter a value in the format MM/dd/yyyy.");
    }

    // Behavior source: NTFormDensity documents Comfortable, Standard, and Dense as supported field-density modes.
    [Theory]
    [InlineData(NTFormDensity.Comfortable, "nt-input-date-time-comfortable")]
    [InlineData(NTFormDensity.Standard, "nt-input-date-time-standard")]
    [InlineData(NTFormDensity.Dense, "nt-input-date-time-dense")]
    public void Density_Renders_Its_Documented_Root_Class(NTFormDensity density, string expectedClass) {
        var cut = RenderDateOnly(configure: parameters => parameters.Add(p => p.Density, density));

        cut.Find(".nt-input-date-time").GetAttribute("class").Should().Contain(expectedClass);
    }

    // Behavior source: Component parameters are stable across equivalent rerenders; native attributes and the filled Material trigger icon remain equivalent.
    [Fact]
    public void Equivalent_Rerender_Preserves_Custom_Picker_Contract() {
        var cut = RenderDateTime(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));
        var originalTarget = cut.Find("input").GetAttribute("data-tnt-dtp-target");

        cut.Render();

        cut.Find("input").GetAttribute("data-tnt-dtp-target").Should().Be(originalTarget);
        cut.Find(".tnt-dtp-trigger-icon").GetAttribute("style").Should().Contain("'FILL' 1");
    }

    // Behavior source: PickerTriggerIcon accepts any NTIcon and is rendered as the custom picker's trigger icon.
    [Fact]
    public void Custom_NonMaterial_Picker_Icon_Is_Rendered() {
        var cut = RenderDateTime(configure: parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.PickerTriggerIcon, new TestIcon("custom-clock")));

        var icon = cut.Find(".test-picker-icon");
        icon.TextContent.Should().Be("custom-clock");
        icon.GetAttribute("class").Should().Contain("tnt-dtp-trigger-icon");
    }

    // Behavior source: Disabled prevents interaction with both the native field and its custom picker trigger.
    [Fact]
    public void Disabled_Custom_Picker_Disables_Trigger() {
        var cut = RenderDateTime(configure: parameters => parameters
            .Add(p => p.EnableCustomPicker, true)
            .Add(p => p.Disabled, true));

        cut.Find("button[data-tnt-dtp-trigger='true']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Unsupported_Type_Throws() {
        var model = new { Value = "" };

        var act = () => Render<NTInputDateTime<string>>(parameters => parameters
            .Add(p => p.Value, model.Value)
            .Add(p => p.ValueExpression, () => model.Value));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The type 'System.String' is not a supported DateTime type.");
    }

    [Fact]
    public void Custom_Picker_Markup_Is_Not_Rendered_By_Default() {
        var cut = RenderDateTime();

        cut.FindAll("[data-tnt-dtp-picker='true']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-trigger='true']").Should().BeEmpty();
    }

    [Fact]
    public void Native_Input_Type_Is_Used_By_Default() {
        var cut = RenderDateOnly();

        cut.Find("input").GetAttribute("type").Should().Be("date");
    }

    // Behavior source: The enhanced native picker icon remains clickable but is excluded from sequential keyboard navigation.
    [Fact]
    public void Native_Picker_Renders_NonTabbable_OnSurface_Trigger() {
        var cut = RenderDateOnly();

        cut.Find("input").GetAttribute("data-tnt-dtp-native-input").Should().Be("true");
        var trigger = cut.Find("button[data-tnt-dtp-native-trigger='true']");
        trigger.GetAttribute("tabindex").Should().Be("-1");
        trigger.GetAttribute("aria-label").Should().Be("Open date picker");
        trigger.GetAttribute("class").Should().Contain("tnt-dtp-native-trigger");
        trigger.QuerySelector(".tnt-dtp-trigger-icon").Should().NotBeNull();
    }

    [Fact]
    public void DateTime_Custom_Picker_Renders_DateTime_Mode() {
        var cut = RenderDateTime(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("datetime-local");
        input.GetAttribute("data-tnt-dtp-mode").Should().Be("datetime");
        var pickerId = input.GetAttribute("data-tnt-dtp-target");
        pickerId.Should().NotBeNullOrWhiteSpace();

        var trigger = cut.Find($"button[data-tnt-dtp-trigger='true'][data-tnt-dtp-target='{pickerId}']");
        trigger.GetAttribute("aria-label").Should().Be("Open date and time picker");
        trigger.GetAttribute("tabindex").Should().Be("-1");
        cut.Find($"div#{pickerId}[data-tnt-dtp-picker='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("datetime");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Date and time");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-label]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-calendar-year-label]").Should().BeEmpty();
        cut.FindAll(".tnt-dtp-menu-button-icon").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-second]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='today']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='now']").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-action='clear']").Should().BeEmpty();
        cut.Find("[data-tnt-dtp-action='cancel']").TextContent.Should().Be("Cancel");
        cut.Find("[data-tnt-dtp-action='confirm']").TextContent.Should().Be("OK");
    }

    [Fact]
    public void Date_Custom_Picker_Renders_Date_Mode() {
        var cut = RenderDateOnly(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        var input = cut.Find("input[data-tnt-dtp-input='true']");
        input.GetAttribute("type").Should().Be("date");
        input.GetAttribute("data-tnt-dtp-mode").Should().Be("date");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-date");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Date");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
    }

    [Fact]
    public void Month_Custom_Picker_Renders_Month_Mode_Only() {
        var cut = RenderDateOnly(configure: parameters => parameters
            .Add(p => p.MonthOnly, true)
            .Add(p => p.EnableCustomPicker, true));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("month");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-month");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Month");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
    }

    [Fact]
    public void Time_Custom_Picker_Renders_Time_Mode_And_Clock_Trigger() {
        var cut = RenderTimeOnly(configure: parameters => parameters.Add(p => p.EnableCustomPicker, true));

        cut.Find("input[data-tnt-dtp-input='true']").GetAttribute("data-tnt-dtp-mode").Should().Be("time");
        cut.Find("[data-tnt-dtp-picker='true']").GetAttribute("class").Should().Contain("tnt-dtp-mode-time");
        cut.Find("[data-tnt-dtp-headline]").TextContent.Should().Be("Time");
        cut.Find("[data-tnt-dtp-content]").Children.Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-day-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-month-index]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-year-list]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-hour]").Should().BeEmpty();
        cut.FindAll("[data-tnt-dtp-second]").Should().BeEmpty();
        cut.Markup.Should().Contain("schedule");
    }

    private IRenderedComponent<NTInputDateTime<DateOnly?>> RenderDateOnly(DateOnlyModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateOnly?>>>? configure = null) {
        model ??= new DateOnlyModel();
        return Render<NTInputDateTime<DateOnly?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<DateTime?>> RenderDateTime(DateTimeModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateTime?>>>? configure = null) {
        model ??= new DateTimeModel();
        return Render<NTInputDateTime<DateTime?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<DateTimeOffset?>> RenderDateTimeOffset(DateTimeOffsetModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<DateTimeOffset?>>>? configure = null) {
        model ??= new DateTimeOffsetModel();
        return Render<NTInputDateTime<DateTimeOffset?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputDateTime<TimeOnly?>> RenderTimeOnly(TimeOnlyModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateTime<TimeOnly?>>>? configure = null) {
        model ??= new TimeOnlyModel();
        return Render<NTInputDateTime<TimeOnly?>>(parameters => {
            parameters
                .Add(p => p.Value, model.Value)
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<TimeOnly?>(this, value => model.Value = value))
                .Add(p => p.ValueExpression, () => model.Value);
            configure?.Invoke(parameters);
        });
    }
}
