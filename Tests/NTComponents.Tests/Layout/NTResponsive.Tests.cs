using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Layout;

public class NTResponsiveTests : BunitContext {

    [Theory]
    [InlineData(NTBreakpointVisibility.Show, NTBreakpointDirection.Above, "above")]
    [InlineData(NTBreakpointVisibility.Show, NTBreakpointDirection.Below, "below")]
    [InlineData(NTBreakpointVisibility.Hide, NTBreakpointDirection.Above, "below")]
    [InlineData(NTBreakpointVisibility.Hide, NTBreakpointDirection.Below, "above")]
    public void Visibility_And_Direction_Select_Visible_Side(NTBreakpointVisibility visibility, NTBreakpointDirection direction, string visibleDirection) {
        var cut = Render<NTResponsive>(parameters => parameters
            .Add(component => component.Visibility, visibility)
            .Add(component => component.Direction, direction)
            .AddChildContent("Responsive content"));

        var responsive = cut.Find(".nt-responsive");

        responsive.GetAttribute("class").Should().Contain($"nt-responsive-visible-{visibleDirection}");
        responsive.GetAttribute("data-nt-breakpoint-direction").Should().Be(direction.ToString().ToLowerInvariant());
        responsive.GetAttribute("data-nt-breakpoint-visibility").Should().Be(visibility.ToString().ToLowerInvariant());
        responsive.TextContent.Should().Be("Responsive content");
    }

    [Theory]
    [InlineData(NTBreakpoint.Small, "small", 600)]
    [InlineData(NTBreakpoint.Medium, "medium", 840)]
    [InlineData(NTBreakpoint.Large, "large", 1200)]
    [InlineData(NTBreakpoint.ExtraLarge, "extra-large", 1600)]
    public void Standard_Breakpoint_Uses_Material_Threshold_Class_Without_Inline_Media_Rule(NTBreakpoint breakpoint, string name, int width) {
        ((int)breakpoint).Should().Be(width);

        var cut = Render<NTResponsive>(parameters => parameters
            .Add(component => component.Breakpoint, breakpoint)
            .AddChildContent("Content"));

        var responsive = cut.Find(".nt-responsive");

        responsive.GetAttribute("class").Should().Contain($"nt-responsive-{name}");
        responsive.GetAttribute("data-nt-breakpoint").Should().Be(name);
        cut.FindAll("style").Should().BeEmpty();
    }

    [Fact]
    public void Custom_Breakpoint_Overrides_Standard_Breakpoint_With_Scoped_Media_Rule() {
        var cut = Render<NTResponsive>(parameters => parameters
            .Add(component => component.Breakpoint, NTBreakpoint.ExtraLarge)
            .Add(component => component.CustomBreakpoint, 721)
            .Add(component => component.Direction, NTBreakpointDirection.Below)
            .Add(component => component.Visibility, NTBreakpointVisibility.Hide)
            .AddChildContent("Content"));

        var responsive = cut.Find(".nt-responsive");
        var style = cut.Find("style");
        var identifier = responsive.GetAttribute("ntid");

        responsive.GetAttribute("class").Should().Contain("nt-responsive-custom");
        responsive.GetAttribute("class").Should().Contain("nt-responsive-visible-above");
        responsive.GetAttribute("class").Should().NotContain("nt-responsive-extra-large");
        responsive.GetAttribute("data-nt-breakpoint").Should().Be("721");
        style.GetAttribute("media").Should().Be("(min-width: 721px)");
        style.TextContent.Should().Contain($"[ntid=\"{identifier}\"]");
        style.TextContent.Should().Contain("display:contents");
    }

    [Fact]
    public void Custom_Breakpoint_Hides_Above_When_Content_Is_Visible_Below() {
        var cut = Render<NTResponsive>(parameters => parameters
            .Add(component => component.CustomBreakpoint, 1024)
            .Add(component => component.Direction, NTBreakpointDirection.Below)
            .Add(component => component.Visibility, NTBreakpointVisibility.Show));

        cut.Find("style").TextContent.Should().Contain("display:none");
    }

    [Fact]
    public void Root_Preserves_Component_And_Additional_Attributes() {
        var attributes = new Dictionary<string, object> {
            ["class"] = "consumer-class",
            ["style"] = "color:red",
            ["data-testid"] = "responsive-content"
        };

        var cut = Render<NTResponsive>(parameters => parameters
            .Add(component => component.AdditionalAttributes, attributes)
            .Add(component => component.ElementId, "responsive-id")
            .Add(component => component.ElementLang, "en")
            .Add(component => component.ElementTitle, "Responsive region")
            .AddChildContent("Content"));

        var responsive = cut.Find("#responsive-id");

        ShouldHaveScopedCssAttribute(responsive);
        responsive.GetAttribute("class").Should().Contain("consumer-class");
        responsive.GetAttribute("style").Should().Contain("color:red");
        responsive.GetAttribute("lang").Should().Be("en");
        responsive.GetAttribute("title").Should().Be("Responsive region");
        responsive.GetAttribute("data-testid").Should().Be("responsive-content");
    }

    [Fact]
    public void Updated_Parameters_Replace_Responsive_Classes_And_Media_Rule() {
        var cut = Render<ResponsiveHost>();

        cut.Find(".nt-responsive").GetAttribute("class").Should().Contain("nt-responsive-medium");
        cut.FindAll("style").Should().BeEmpty();

        cut.Instance.CustomBreakpoint = 960;
        cut.Instance.Direction = NTBreakpointDirection.Below;
        cut.Render();

        var responsive = cut.Find(".nt-responsive");

        responsive.GetAttribute("class").Should().Contain("nt-responsive-custom");
        responsive.GetAttribute("class").Should().Contain("nt-responsive-visible-below");
        responsive.GetAttribute("class").Should().NotContain("nt-responsive-medium");
        cut.Find("style").GetAttribute("media").Should().Be("(min-width: 960px)");
    }

    [Fact]
    public void Negative_Custom_Breakpoint_Is_Rejected() {
        var render = () => Render<NTResponsive>(parameters => parameters.Add(component => component.CustomBreakpoint, -1));

        render.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(NTResponsive.CustomBreakpoint));
    }

    [Fact]
    public void Undefined_Enum_Values_Are_Rejected() {
        var invalidBreakpoint = () => Render<NTResponsive>(parameters => parameters.Add(component => component.Breakpoint, (NTBreakpoint)999));
        var invalidDirection = () => Render<NTResponsive>(parameters => parameters.Add(component => component.Direction, (NTBreakpointDirection)999));
        var invalidVisibility = () => Render<NTResponsive>(parameters => parameters.Add(component => component.Visibility, (NTBreakpointVisibility)999));

        invalidBreakpoint.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(NTResponsive.Breakpoint));
        invalidDirection.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(NTResponsive.Direction));
        invalidVisibility.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(NTResponsive.Visibility));
    }

    private static void ShouldHaveScopedCssAttribute(IElement element) =>
        element.Attributes.Any(attribute => attribute.Name.StartsWith("b-", StringComparison.Ordinal)).Should().BeTrue();

    private sealed class ResponsiveHost : ComponentBase {
        public int? CustomBreakpoint { get; set; }

        public NTBreakpointDirection Direction { get; set; } = NTBreakpointDirection.Above;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTResponsive>(0);
            builder.AddAttribute(1, nameof(NTResponsive.CustomBreakpoint), CustomBreakpoint);
            builder.AddAttribute(2, nameof(NTResponsive.Direction), Direction);
            builder.AddAttribute(3, nameof(NTResponsive.ChildContent), (RenderFragment)(content => content.AddContent(4, "Content")));
            builder.CloseComponent();
        }
    }
}
