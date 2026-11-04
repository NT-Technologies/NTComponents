using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTInputDateRange_Tests : BunitContext {

    private const string StartSelector = "input[data-nt-input-date-range-start]";
    private const string EndSelector = "input[data-nt-input-date-range-end]";
    private const string DisplaySelector = "input[data-nt-input-date-range-input]";

    private sealed class TestModel {
        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
    }

    [Fact]
    public void Renders_Hidden_Inputs_With_Derived_Names_And_Iso_Values() {
        var cut = RenderInput(new TestModel { CheckIn = new DateOnly(2026, 11, 4), CheckOut = new DateOnly(2026, 11, 9) });

        var start = cut.Find(StartSelector);
        var end = cut.Find(EndSelector);
        start.GetAttribute("type").Should().Be("hidden");
        start.GetAttribute("name").Should().Be("model.CheckIn");
        start.GetAttribute("value").Should().Be("2026-11-04");
        end.GetAttribute("name").Should().Be("model.CheckOut");
        end.GetAttribute("value").Should().Be("2026-11-09");
    }

    [Fact]
    public void Explicit_Element_Names_Win() {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.StartElementName, "from").Add(p => p.EndElementName, "to"));

        cut.Find(StartSelector).GetAttribute("name").Should().Be("from");
        cut.Find(EndSelector).GetAttribute("name").Should().Be("to");
    }

    [Fact]
    public void Empty_Values_Render_Empty_Hidden_Inputs_And_The_Placeholder() {
        var cut = RenderInput();

        cut.Find(StartSelector).GetAttribute("value").Should().BeNullOrEmpty();
        cut.Find(EndSelector).GetAttribute("value").Should().BeNullOrEmpty();
        var display = cut.Find(DisplaySelector);
        display.GetAttribute("value").Should().BeNullOrEmpty();
        display.GetAttribute("placeholder").Should().Be("Start date – End date");
    }

    [Fact]
    public void Display_Is_Read_Only_Unnamed_And_Describes_The_Popover() {
        var cut = RenderInput(new TestModel { CheckIn = new DateOnly(2026, 11, 4), CheckOut = new DateOnly(2026, 11, 9) });

        var display = cut.Find(DisplaySelector);
        var popover = cut.Find("[data-nt-input-date-range-popover]");
        display.HasAttribute("readonly").Should().BeTrue();
        display.HasAttribute("name").Should().BeFalse();
        display.GetAttribute("aria-haspopup").Should().Be("dialog");
        display.GetAttribute("aria-controls").Should().Be(popover.Id);
        popover.GetAttribute("popover").Should().Be("auto");
        popover.GetAttribute("role").Should().Be("dialog");
    }

    [Fact]
    public void Display_Uses_The_Display_Format_With_The_Current_Culture() {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try {
            var model = new TestModel { CheckIn = new DateOnly(2026, 11, 4), CheckOut = new DateOnly(2026, 11, 9) };

            RenderInput(model).Find(DisplaySelector).GetAttribute("value").Should().Be("Nov 4, 2026 – Nov 9, 2026");
            RenderInput(model, p => p.Add(x => x.DisplayFormat, "yyyy/MM/dd")).Find(DisplaySelector).GetAttribute("value").Should().Be("2026/11/04 – 2026/11/09");
            RenderInput(new TestModel { CheckIn = new DateOnly(2026, 11, 4) }).Find(DisplaySelector).GetAttribute("value").Should().Be("Nov 4, 2026 –");
        }
        finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Min_And_Max_Are_Exposed_To_The_Picker() {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.Min, new DateOnly(2026, 11, 1)).Add(p => p.Max, new DateOnly(2026, 12, 31)));

        var root = cut.Find("[data-nt-input-date-range]");
        root.GetAttribute("data-min").Should().Be("2026-11-01");
        root.GetAttribute("data-max").Should().Be("2026-12-31");
    }

    [Fact]
    public void Label_And_Supporting_Text_Are_Passed_To_The_Field() {
        var cut = RenderInput(configure: parameters => parameters.Add(p => p.Label, "Stay").Add(p => p.SupportingText, "Pick your dates"));

        cut.Find(".nt-input-label").TextContent.Should().Be("Stay");
        cut.Find(".nt-input-supporting").TextContent.Trim().Should().Be("Pick your dates");
    }

    [Fact]
    public void Start_Change_Updates_Its_Binding_And_Runs_BindAfter() {
        var model = new TestModel();
        var bindAfter = 0;
        var cut = RenderInput(model, p => p.Add(x => x.BindAfter, EventCallback.Factory.Create(this, () => bindAfter++)));

        cut.Find(StartSelector).Change("2026-11-10");

        model.CheckIn.Should().Be(new DateOnly(2026, 11, 10));
        model.CheckOut.Should().BeNull();
        bindAfter.Should().Be(1);
    }

    [Fact]
    public void Both_Changes_Update_Both_Bindings_And_Empty_Clears() {
        var model = new TestModel();
        var cut = RenderInput(model);

        cut.Find(StartSelector).Change("2026-11-10");
        cut.Find(EndSelector).Change("2026-11-14");
        model.CheckIn.Should().Be(new DateOnly(2026, 11, 10));
        model.CheckOut.Should().Be(new DateOnly(2026, 11, 14));
        cut.Find(DisplaySelector).GetAttribute("value").Should().Contain("2026").And.Contain("–");

        cut.Find(StartSelector).Change("");
        cut.Find(EndSelector).Change("");
        model.CheckIn.Should().BeNull();
        model.CheckOut.Should().BeNull();
    }

    [Fact]
    public void Unchanged_Or_Invalid_Values_Do_Not_Raise_Callbacks() {
        var model = new TestModel { CheckIn = new DateOnly(2026, 11, 10) };
        var bindAfter = 0;
        var cut = RenderInput(model, p => p.Add(x => x.BindAfter, EventCallback.Factory.Create(this, () => bindAfter++)));

        cut.Find(StartSelector).Change("2026-11-10");
        cut.Find(EndSelector).Change("not a date");

        bindAfter.Should().Be(0);
        model.CheckOut.Should().BeNull();
    }

    [Fact]
    public void Changes_Notify_The_EditContext_For_Each_Changed_Field() {
        var model = new TestModel();
        var cut = RenderInForm(model);
        var changed = new List<string>();
        cut.Instance.EditContext!.OnFieldChanged += (_, args) => changed.Add(args.FieldIdentifier.FieldName);

        cut.Find(StartSelector).Change("2026-11-10");
        cut.Find(EndSelector).Change("2026-11-14");

        changed.Should().Equal("CheckIn", "CheckOut");
    }

    [Theory]
    [InlineData("CheckIn", "Check-in is invalid.")]
    [InlineData("CheckOut", "Check-out is invalid.")]
    public void Error_Text_Comes_From_Either_Expression(string field, string message) {
        var model = new TestModel();
        var cut = RenderInForm(model);
        cut.FindAll(".nt-input-error-text").Should().BeEmpty();

        AddMessage(cut, model, field, message);

        cut.Find(".nt-input-error-text").TextContent.Trim().Should().Be(message);
    }

    [Fact]
    public void Start_Error_Wins_And_Explicit_Error_Text_Wins_Over_Both() {
        var model = new TestModel();
        var cut = RenderInForm(model);
        AddMessage(cut, model, nameof(TestModel.CheckOut), "End problem");
        AddMessage(cut, model, nameof(TestModel.CheckIn), "Start problem");
        cut.Find(".nt-input-error-text").TextContent.Trim().Should().Be("Start problem");

        var explicitCut = RenderInForm(model, "Explicit");
        AddMessage(explicitCut, model, nameof(TestModel.CheckIn), "Start problem");
        explicitCut.Find(".nt-input-error-text").TextContent.Trim().Should().Be("Explicit");
    }

    [Fact]
    public async Task Dispose_Unsubscribes_From_Validation_State_Changes() {
        var model = new TestModel();
        var cut = RenderInForm(model);
        var context = cut.Instance.EditContext!;

        cut.Dispose();

        var act = () => cut.InvokeAsync(context.NotifyValidationStateChanged);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Disabled_Disables_The_Display_And_Hidden_Inputs() {
        var cut = RenderInput(new TestModel { CheckIn = new DateOnly(2026, 11, 4) }, p => p.Add(x => x.Disabled, true));

        cut.Find(DisplaySelector).HasAttribute("disabled").Should().BeTrue();
        cut.Find(StartSelector).HasAttribute("disabled").Should().BeTrue();
        cut.Find(EndSelector).HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void ReadOnly_Marks_The_Root_And_Still_Posts_Values() {
        var cut = RenderInput(new TestModel { CheckIn = new DateOnly(2026, 11, 4) }, p => p.Add(x => x.ReadOnly, true));

        cut.Find("[data-nt-input-date-range]").GetAttribute("data-readonly").Should().Be("true");
        cut.Find(StartSelector).HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Not_ReadOnly_Does_Not_Mark_The_Root() {
        var cut = RenderInput();

        cut.Find("[data-nt-input-date-range]").HasAttribute("data-readonly").Should().BeFalse();
    }

    private IRenderedComponent<NTInputDateRange> RenderInput(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputDateRange>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputDateRange>(parameters => {
            AddBindings(parameters, model);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTForm> RenderInForm(TestModel model, string? errorText = null) {
        return Render<NTForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<NTInputDateRange>(0);
                builder.AddAttribute(1, nameof(NTInputDateRange.StartDate), model.CheckIn);
                builder.AddAttribute(2, nameof(NTInputDateRange.StartDateChanged), EventCallback.Factory.Create<DateOnly?>(this, value => model.CheckIn = value));
                builder.AddAttribute(3, nameof(NTInputDateRange.StartDateExpression), (Expression<Func<DateOnly?>>)(() => model.CheckIn));
                builder.AddAttribute(4, nameof(NTInputDateRange.EndDate), model.CheckOut);
                builder.AddAttribute(5, nameof(NTInputDateRange.EndDateChanged), EventCallback.Factory.Create<DateOnly?>(this, value => model.CheckOut = value));
                builder.AddAttribute(6, nameof(NTInputDateRange.EndDateExpression), (Expression<Func<DateOnly?>>)(() => model.CheckOut));
                builder.AddAttribute(7, nameof(NTInputDateRange.ErrorText), errorText);
                builder.CloseComponent();
            }));
    }

    private static void AddMessage(IRenderedComponent<NTForm> cut, TestModel model, string field, string message) {
        var context = cut.Instance.EditContext!;
        new ValidationMessageStore(context).Add(new FieldIdentifier(model, field), message);
        cut.InvokeAsync(context.NotifyValidationStateChanged).GetAwaiter().GetResult();
    }

    private void AddBindings(ComponentParameterCollectionBuilder<NTInputDateRange> parameters, TestModel model) => parameters
        .Add(p => p.StartDate, model.CheckIn)
        .Add(p => p.StartDateChanged, EventCallback.Factory.Create<DateOnly?>(this, value => model.CheckIn = value))
        .Add(p => p.StartDateExpression, () => model.CheckIn)
        .Add(p => p.EndDate, model.CheckOut)
        .Add(p => p.EndDateChanged, EventCallback.Factory.Create<DateOnly?>(this, value => model.CheckOut = value))
        .Add(p => p.EndDateExpression, () => model.CheckOut);
}
