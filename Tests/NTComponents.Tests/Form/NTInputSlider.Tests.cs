using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public class NTInputSlider_Tests : BunitContext {
    private sealed class TestModel {
        public int Volume { get; set; } = 50;

        public NTSliderRange<int> PriceRange { get; set; } = new(20, 80);
    }

    [Fact]
    public void Renders_Native_Range_Input_With_Label_And_Supporting_Text() {
        var cut = RenderSlider(configure: parameters => parameters
            .Add(p => p.ElementId, "volume")
            .Add(p => p.Label, "Volume")
            .Add(p => p.SupportingText, "Applies immediately")
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 10)
            .Add(p => p.Step, "2"));

        var input = cut.Find("input[type=range]");

        input.GetAttribute("id").Should().Be("volume");
        input.GetAttribute("data-nt-slider-input").Should().Be("true");
        input.GetAttribute("name").Should().Be("model.Volume");
        input.GetAttribute("min").Should().Be("0");
        input.GetAttribute("max").Should().Be("10");
        input.GetAttribute("step").Should().Be("2");
        input.GetAttribute("aria-labelledby").Should().Be("volume-label");
        input.GetAttribute("aria-describedby").Should().Be("volume-supporting");
        cut.Find("label.nt-slider-label").GetAttribute("for").Should().Be("volume");
        cut.Find("#volume-supporting").TextContent.Should().Be("Applies immediately");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Form/NTInputSlider.razor.js");
    }

    [Fact]
    public void Input_Updates_Value_And_Invokes_BindAfter_By_Default() {
        var model = new TestModel();
        var callbackValue = 0;
        var cut = RenderSlider(model, parameters => parameters.Add(p => p.BindAfter, EventCallback.Factory.Create<int>(this, value => callbackValue = value)));

        cut.Find("input[type=range]").Input("73");

        model.Volume.Should().Be(73);
        callbackValue.Should().Be(73);
    }

    [Fact]
    public void BindOnInput_False_Updates_On_Change_Not_Input() {
        var model = new TestModel();
        var cut = RenderSlider(model, parameters => parameters.Add(p => p.BindOnInput, false));
        var input = cut.Find("input[type=range]");

        input.Invoking(element => element.Input("73")).Should().Throw<Bunit.MissingEventHandlerException>();
        model.Volume.Should().Be(50);

        input.Change("73");
        model.Volume.Should().Be(73);
    }

    [Fact]
    public void Centered_Variant_Sets_Active_Track_Around_Center() {
        var model = new TestModel { Volume = -25 };
        var cut = RenderSlider(model, parameters => parameters
            .Add(p => p.Min, -100)
            .Add(p => p.Max, 100)
            .Add(p => p.Variant, NTSliderVariant.Centered));

        var root = cut.Find(".nt-slider");

        root.GetAttribute("class").Should().Contain("nt-slider-centered");
        root.GetAttribute("style").Should().Contain("--nt-slider-start-percent:37.5%;").And.Contain("--nt-slider-end-percent:50%;");
    }

    [Fact]
    public void InsetIcon_Renders_Inside_Track_For_Standard_Medium_Slider() {
        var cut = RenderSlider(configure: parameters => parameters
            .Add(p => p.Size, NTSliderSize.Medium)
            .Add(p => p.InsetIcon, MaterialIcon.VolumeDown));

        var root = cut.Find(".nt-slider");
        var icon = cut.Find(".nt-slider-track .nt-slider-inset-icon");

        root.GetAttribute("class").Should().Contain("nt-slider-with-inset-icon");
        icon.TextContent.Should().Contain(MaterialIcon.VolumeDown.Icon);
        cut.FindAll(".nt-slider-leading").Should().BeEmpty();
        cut.FindAll(".nt-slider-trailing").Should().BeEmpty();
    }

    [Fact]
    public void InsetIcon_Does_Not_Render_For_Unsupported_Slider_Configurations() {
        var tooSmall = RenderSlider(configure: parameters => parameters
            .Add(p => p.Size, NTSliderSize.Small)
            .Add(p => p.InsetIcon, MaterialIcon.VolumeDown));
        var centered = RenderSlider(configure: parameters => parameters
            .Add(p => p.Size, NTSliderSize.Medium)
            .Add(p => p.Variant, NTSliderVariant.Centered)
            .Add(p => p.InsetIcon, MaterialIcon.VolumeDown));
        var vertical = RenderSlider(configure: parameters => parameters
            .Add(p => p.Size, NTSliderSize.Medium)
            .Add(p => p.Orientation, NTSliderOrientation.Vertical)
            .Add(p => p.InsetIcon, MaterialIcon.VolumeDown));

        tooSmall.FindAll(".nt-slider-inset-icon").Should().BeEmpty();
        centered.FindAll(".nt-slider-inset-icon").Should().BeEmpty();
        vertical.FindAll(".nt-slider-inset-icon").Should().BeEmpty();
    }

    [Fact]
    public void Track_Renders_Without_Stop_Indicators_By_Default() {
        var cut = RenderSlider();

        cut.Find(".nt-slider-track").Should().NotBeNull();
        cut.Find(".nt-slider-active-track").Should().NotBeNull();
        cut.Find(".nt-slider-inactive-track-end").Should().NotBeNull();
        cut.FindAll(".nt-slider-stop").Should().BeEmpty();
        cut.Find(".nt-slider").GetAttribute("class").Should().NotContain("nt-slider-with-stops");
    }

    [Fact]
    public void Slider_Defaults_Native_Step_To_One() {
        var cut = RenderSlider();

        cut.Find("input[type=range]").GetAttribute("step").Should().Be("1");
    }

    [Fact]
    public void RangeSlider_Defaults_Native_Step_To_One() {
        var cut = RenderRangeSlider();

        cut.FindAll("input[type=range]").Should().OnlyContain(input => input.GetAttribute("step") == "1");
    }

    [Fact]
    public void ShowStops_Renders_Stop_Indicators_When_Enabled() {
        var cut = RenderSlider(configure: parameters => parameters
            .Add(p => p.ShowStops, true)
            .Add(p => p.StopCount, 5));

        cut.Find(".nt-slider-track").Should().NotBeNull();
        cut.FindAll(".nt-slider-stop").Should().HaveCount(5);
        cut.Find(".nt-slider").GetAttribute("class").Should().Contain("nt-slider-with-stops");
    }

    [Fact]
    public void Sliders_Do_Not_Expose_External_Icon_Parameters() {
        typeof(NTInputSlider<int>).GetProperty("LeadingIcon").Should().BeNull();
        typeof(NTInputSlider<int>).GetProperty("TrailingIcon").Should().BeNull();
        typeof(NTInputRangeSlider<int>).GetProperty("LeadingIcon").Should().BeNull();
        typeof(NTInputRangeSlider<int>).GetProperty("TrailingIcon").Should().BeNull();
    }

    [Fact]
    public void ReadOnly_Disables_Visible_Input_And_Posts_Current_Value() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { Volume = 64 };
        var cut = RenderSlider(model, parameters => parameters.Add(p => p.ReadOnly, true));

        var visibleInput = cut.Find("input[type=range]");
        var hiddenInput = cut.Find("input[type=hidden]");

        visibleInput.HasAttribute("disabled").Should().BeTrue();
        visibleInput.HasAttribute("name").Should().BeFalse();
        hiddenInput.GetAttribute("name").Should().Be("model.Volume");
        hiddenInput.GetAttribute("value").Should().Be("64");
        cut.Find(".nt-slider").GetAttribute("class").Should().Contain("nt-slider-readonly");
    }

    [Fact]
    public void RangeSlider_Renders_Two_Named_Range_Inputs() {
        var cut = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.ElementId, "price")
            .Add(p => p.Label, "Price")
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.ShowValueIndicator, true));

        var inputs = cut.FindAll("input[type=range]");

        inputs.Should().HaveCount(2);
        inputs[0].GetAttribute("id").Should().Be("price-start");
        inputs[0].GetAttribute("data-nt-slider-range-start").Should().Be("true");
        inputs[0].GetAttribute("name").Should().Be("model.PriceRange.Start");
        inputs[0].GetAttribute("aria-label").Should().Be("Price Minimum");
        inputs[0].HasAttribute("aria-labelledby").Should().BeFalse();
        inputs[1].GetAttribute("id").Should().Be("price-end");
        inputs[1].GetAttribute("data-nt-slider-range-end").Should().Be("true");
        inputs[1].GetAttribute("name").Should().Be("model.PriceRange.End");
        inputs[1].GetAttribute("aria-label").Should().Be("Price Maximum");
        inputs[1].HasAttribute("aria-labelledby").Should().BeFalse();
        cut.FindAll("output.nt-slider-value-indicator").Should().HaveCount(2);
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Form/NTInputRangeSlider.razor.js");
    }

    [Fact]
    public void RangeSlider_Applies_AdditionalAttributes_To_Both_Handles() {
        var cut = RenderRangeSlider(configure: parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object> {
                ["data-field"] = "budget-range",
                ["title"] = "Budget range"
            }));

        var inputs = cut.FindAll("input[type=range]");

        inputs.Should().OnlyContain(input => input.GetAttribute("data-field") == "budget-range");
        inputs.Should().OnlyContain(input => input.GetAttribute("title") == "Budget range");
    }

    [Fact]
    public void RangeSlider_Constrains_Each_Handle_To_The_Other_Handle() {
        var model = new TestModel { PriceRange = new NTSliderRange<int>(20, 80) };
        var cut = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100));

        var inputs = cut.FindAll("input[type=range]");

        inputs[0].GetAttribute("min").Should().Be("0");
        inputs[0].GetAttribute("max").Should().Be("80");
        inputs[0].GetAttribute("value").Should().Be("20");
        inputs[0].GetAttribute("style").Should().Be("left:calc(var(--nt-slider-range-handle-hit-width) / -2);right:auto;width:calc(var(--nt-slider-end-percent) + var(--nt-slider-range-handle-hit-width));");
        inputs[1].GetAttribute("min").Should().Be("20");
        inputs[1].GetAttribute("max").Should().Be("100");
        inputs[1].GetAttribute("value").Should().Be("80");
        inputs[1].GetAttribute("style").Should().Be("left:calc(var(--nt-slider-start-percent) - var(--nt-slider-range-handle-hit-width) / 2);right:calc(var(--nt-slider-range-handle-hit-width) / -2);width:auto;");
        cut.Find(".nt-slider").GetAttribute("style").Should().Contain("--nt-slider-start-gap:8px;").And.Contain("--nt-slider-end-gap:8px;");
    }

    [Fact]
    public void RangeSlider_Normalizes_Inverted_Rendered_Values() {
        var model = new TestModel { PriceRange = new NTSliderRange<int>(80, 20) };
        var cut = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.ShowValueIndicator, true));

        var inputs = cut.FindAll("input[type=range]");

        inputs[0].GetAttribute("max").Should().Be("80");
        inputs[0].GetAttribute("value").Should().Be("20");
        inputs[0].GetAttribute("style").Should().Be("left:calc(var(--nt-slider-range-handle-hit-width) / -2);right:auto;width:calc(var(--nt-slider-end-percent) + var(--nt-slider-range-handle-hit-width));");
        inputs[1].GetAttribute("min").Should().Be("20");
        inputs[1].GetAttribute("value").Should().Be("80");
        inputs[1].GetAttribute("style").Should().Be("left:calc(var(--nt-slider-start-percent) - var(--nt-slider-range-handle-hit-width) / 2);right:calc(var(--nt-slider-range-handle-hit-width) / -2);width:auto;");
        cut.Find(".nt-slider").GetAttribute("style").Should().Contain("--nt-slider-start-percent:20%;").And.Contain("--nt-slider-end-percent:80%;");
        cut.FindAll("output.nt-slider-value-indicator").Select(output => output.TextContent).Should().Equal("20", "80");
    }

    [Fact]
    public void Sliders_Clamp_Rendered_OutOfRange_Values_To_Effective_Bounds() {
        var model = new TestModel {
            Volume = 150,
            PriceRange = new NTSliderRange<int>(-20, 140)
        };
        var slider = RenderSlider(model, parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.ShowValueIndicator, true));
        var range = RenderRangeSlider(model, parameters => parameters
            .Add(p => p.Min, 0)
            .Add(p => p.Max, 100)
            .Add(p => p.ShowValueIndicator, true));

        slider.Find("input[type=range]").GetAttribute("value").Should().Be("100");
        slider.Find("output.nt-slider-value-indicator").TextContent.Should().Be("100");
        range.FindAll("input[type=range]").Select(input => input.GetAttribute("value")).Should().Equal("0", "100");
        range.FindAll("output.nt-slider-value-indicator").Select(output => output.TextContent).Should().Equal("0", "100");
    }

    [Fact]
    public void RangeSlider_Input_Clamps_Start_And_End_Order() {
        var model = new TestModel { PriceRange = new NTSliderRange<int>(20, 80) };
        var cut = RenderRangeSlider(model);
        var inputs = cut.FindAll("input[type=range]");

        inputs[0].Input("90");
        model.PriceRange.Start.Should().Be(80);
        model.PriceRange.End.Should().Be(80);

        inputs[1].Input("10");
        model.PriceRange.Start.Should().Be(80);
        model.PriceRange.End.Should().Be(80);
    }

    [Fact]
    public void RangeSlider_BindOnInput_False_Updates_On_Change_Not_Input() {
        var model = new TestModel { PriceRange = new NTSliderRange<int>(20, 80) };
        var cut = RenderRangeSlider(model, parameters => parameters.Add(p => p.BindOnInput, false));
        var inputs = cut.FindAll("input[type=range]");

        inputs[0].Invoking(element => element.Input("30")).Should().Throw<Bunit.MissingEventHandlerException>();
        inputs[1].Invoking(element => element.Input("70")).Should().Throw<Bunit.MissingEventHandlerException>();
        model.PriceRange.Start.Should().Be(20);
        model.PriceRange.End.Should().Be(80);

        inputs[0].Change("30");
        inputs[1].Change("70");
        model.PriceRange.Start.Should().Be(30);
        model.PriceRange.End.Should().Be(70);
    }

    [Fact]
    public void RangeSlider_ReadOnly_Posts_Current_Start_And_End_Values() {
        SetRendererInfo(new RendererInfo("Static", false));
        var model = new TestModel { PriceRange = new NTSliderRange<int>(15, 60) };
        var cut = RenderRangeSlider(model, parameters => parameters.Add(p => p.ReadOnly, true));

        cut.FindAll("input[type=range]").Should().OnlyContain(input => input.HasAttribute("disabled"));
        var hiddenInputs = cut.FindAll("input[type=hidden]");

        hiddenInputs.Should().HaveCount(2);
        hiddenInputs[0].GetAttribute("name").Should().Be("model.PriceRange.Start");
        hiddenInputs[0].GetAttribute("value").Should().Be("15");
        hiddenInputs[1].GetAttribute("name").Should().Be("model.PriceRange.End");
        hiddenInputs[1].GetAttribute("value").Should().Be("60");
    }

    [Fact]
    public void Color_Overrides_Emit_Component_Css_Variables() {
        var cut = RenderSlider(configure: parameters => parameters
            .Add(p => p.ActiveTrackColor, NTColor.Secondary)
            .Add(p => p.HandleColor, NTColor.Primary)
            .Add(p => p.InactiveTrackColor, NTColor.SecondaryContainer)
            .Add(p => p.SupportingTextColor, NTColor.Tertiary));

        var style = cut.Find(".nt-slider").GetAttribute("style");

        style.Should().Contain("--nt-slider-active-track-color:var(--nt-color-secondary);");
        style.Should().Contain("--nt-slider-handle-color:var(--nt-color-primary);");
        style.Should().Contain("--nt-slider-inactive-track-color:var(--nt-color-secondary-container);");
        style.Should().Contain("--nt-slider-supporting-text-color:var(--nt-color-tertiary);");
    }

    private IRenderedComponent<NTInputSlider<int>> RenderSlider(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputSlider<int>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputSlider<int>>(parameters => {
            parameters.Add(p => p.Value, model.Volume);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, value => model.Volume = value));
            parameters.Add(p => p.ValueExpression, () => model.Volume);
            configure?.Invoke(parameters);
        });
    }

    private IRenderedComponent<NTInputRangeSlider<int>> RenderRangeSlider(TestModel? model = null, Action<ComponentParameterCollectionBuilder<NTInputRangeSlider<int>>>? configure = null) {
        model ??= new TestModel();
        return Render<NTInputRangeSlider<int>>(parameters => {
            parameters.Add(p => p.Value, model.PriceRange);
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<NTSliderRange<int>>(this, value => model.PriceRange = value));
            parameters.Add(p => p.ValueExpression, () => model.PriceRange);
            configure?.Invoke(parameters);
        });
    }
}
