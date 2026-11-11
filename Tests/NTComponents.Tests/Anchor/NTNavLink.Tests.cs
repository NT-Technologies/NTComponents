using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace NTComponents.Tests.Anchor;

public class NTNavLink_Tests : BunitContext {

    [Fact]
    public void Default_Render_Uses_Filled_Button_Chrome() {
        var cut = Render<NTNavLink>(parameters => parameters.Add(x => x.Label, "Home"));
        var link = cut.Find("a");

        link.GetAttribute("class")!.Should().Contain("nt-nav-link-button-chrome");
        link.GetAttribute("class")!.Should().Contain("nt-nav-link-filled");
        link.GetAttribute("class")!.Should().Contain("nt-size-s");
        link.GetAttribute("style")!.Should().Contain("--nt-nav-link-bg:var(--nt-color-primary)");
        link.GetAttribute("style")!.Should().Contain("--nt-nav-link-fg:var(--nt-color-on-primary)");
        link.GetAttribute("style")!.Should().NotContain("--nt-nav-link-active-bg");
        link.GetAttribute("style")!.Should().NotContain("--nt-nav-link-active-fg");
    }

    [Fact]
    public void Render_Inherits_Blazor_NavLink() {
        var cut = Render<NTNavLink>(parameters => parameters.Add(x => x.Label, "Home"));

        cut.Instance.Should().BeAssignableTo<Microsoft.AspNetCore.Components.Routing.NavLink>();
    }

    [Fact]
    public void AdditionalAttributes_AreRendered() {
        var attrs = new Dictionary<string, object> { { "data-test", "nav" }, { "href", "/home" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.GetAttribute("data-test").Should().Be("nav");
        link.GetAttribute("href").Should().Be("/home");
    }

    [Fact]
    public void Disabled_Removes_Href_And_Adds_AriaDisabled() {
        var attrs = new Dictionary<string, object> { { "href", "/home" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        cut.Instance.AdditionalAttributes!.ContainsKey("href").Should().BeTrue();
        link.HasAttribute("href").Should().BeFalse();
        link.GetAttribute("aria-disabled").Should().Be("true");
        link.GetAttribute("tabindex").Should().Be("-1");
        link.GetAttribute("class")!.Should().Contain("nt-disabled");
    }

    [Fact]
    public void Disabled_Removes_Href_CaseInsensitive_Without_Mutating_AdditionalAttributes() {
        var attrs = new Dictionary<string, object> { { "HREF", "/home" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        cut.Instance.AdditionalAttributes!.ContainsKey("HREF").Should().BeTrue();
        link.HasAttribute("href").Should().BeFalse();
        link.GetAttribute("aria-disabled").Should().Be("true");
        link.GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public void Disabled_Overrides_Caller_TabIndex_To_Remove_Link_From_Tab_Order() {
        var attrs = new Dictionary<string, object> { { "href", "/home" }, { "tabindex", "0" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        cut.Instance.AdditionalAttributes!.ContainsKey("tabindex").Should().BeTrue();
        link.HasAttribute("href").Should().BeFalse();
        link.GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public void Disabled_Rebuilds_Filtered_Attributes_When_Same_Dictionary_Mutates() {
        var attrs = new Dictionary<string, object> { { "href", "/home" }, { "data-test", "before" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        attrs["data-test"] = "after";

        cut.Render(parameters => parameters
            .Add(x => x.Disabled, true)
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.HasAttribute("href").Should().BeFalse();
        link.GetAttribute("data-test").Should().Be("after");
    }

    [Fact]
    public void AdditionalAttributes_Class_Renders_Once() {
        var attrs = new Dictionary<string, object> { { "class", "custom-link" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var classTokens = cut.Find("a").GetAttribute("class")!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        classTokens.Count(x => x == "custom-link").Should().Be(1);
    }

    [Fact]
    public void Active_Route_Renders_AriaCurrent_Page() {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/home");

        var attrs = new Dictionary<string, object> { { "href", "/home" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        cut.Find("a").GetAttribute("aria-current").Should().Be("page");
    }

    [Fact]
    public void Component_Owns_AriaCurrent_And_AriaDisabled_State() {
        var attrs = new Dictionary<string, object> {
            { "href", "/other" },
            { "aria-current", "step" },
            { "aria-disabled", "true" }
        };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Other"));

        var link = cut.Find("a");

        link.HasAttribute("aria-current").Should().BeFalse();
        link.HasAttribute("aria-disabled").Should().BeFalse();
    }

    [Fact]
    public void Descriptive_Aria_Attributes_AreRendered() {
        var attrs = new Dictionary<string, object> {
            { "aria-label", "Open home" },
            { "aria-describedby", "home-help" }
        };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.GetAttribute("aria-label").Should().Be("Open home");
        link.GetAttribute("aria-describedby").Should().Be("home-help");
    }

    [Fact]
    public void Inactive_Link_Does_Not_Render_AriaCurrent_From_User_Class() {
        var attrs = new Dictionary<string, object> { { "href", "/other" }, { "class", "active" } };
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, attrs)
            .Add(x => x.Label, "Other"));

        cut.Find("a").HasAttribute("aria-current").Should().BeFalse();
    }

    [Fact]
    public void EnableRipple_True_Renders_Button_Ripple_Host_For_Button_Chrome() {
        var cut = Render<NTNavLink>(parameters => parameters.Add(x => x.Label, "Home"));

        cut.Find(".nt-button-ripple-host").Should().NotBeNull();
        cut.Markup.Should().Contain("startRippleHost");
        cut.Markup.Should().NotContain("startButtonInteraction");
    }

    [Fact]
    public void EnableRipple_False_Does_Not_Render_Ripple_Host() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.EnableRipple, false)
            .Add(x => x.Label, "Home"));

        cut.Markup.Should().NotContain("nt-button-ripple-host");
        cut.Markup.Should().NotContain("startRippleHost");
        cut.Markup.Should().NotContain("startButtonInteraction");
    }

    [Fact]
    public void Active_Colors_Render_As_Css_Variables_And_Classes() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.ActiveBackgroundColor, NTColor.Secondary)
            .Add(x => x.ActiveTextColor, NTColor.OnSecondary)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.GetAttribute("class")!.Should().Contain("active-bg-color");
        link.GetAttribute("class")!.Should().Contain("active-fg-color");
        link.GetAttribute("style")!.Should().Contain("--nt-nav-link-active-bg:var(--nt-color-secondary)");
        link.GetAttribute("style")!.Should().Contain("--nt-nav-link-active-fg:var(--nt-color-on-secondary)");
    }

    [Theory]
    [InlineData(NTNavLinkVariant.Elevated, "nt-nav-link-elevated", "surface-container-low", "primary")]
    [InlineData(NTNavLinkVariant.Filled, "nt-nav-link-filled", "primary", "on-primary")]
    [InlineData(NTNavLinkVariant.Tonal, "nt-nav-link-tonal", "secondary-container", "on-secondary-container")]
    [InlineData(NTNavLinkVariant.Outlined, "nt-nav-link-outlined", "transparent", "primary")]
    [InlineData(NTNavLinkVariant.Text, "nt-nav-link-text", "transparent", "primary")]
    public void Button_Chrome_Variants_Render_Default_Colors(NTNavLinkVariant variant, string expectedClass, string expectedBackground, string expectedText) {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, variant)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.GetAttribute("class")!.Should().Contain(expectedClass);
        link.GetAttribute("class")!.Should().Contain("nt-nav-link-button-chrome");
        link.GetAttribute("style")!.Should().Contain($"--nt-nav-link-bg:var(--nt-color-{expectedBackground})");
        link.GetAttribute("style")!.Should().Contain($"--nt-nav-link-fg:var(--nt-color-{expectedText})");
        link.GetAttribute("style")!.Should().NotContain("--nt-nav-link-active-bg");
        link.GetAttribute("style")!.Should().NotContain("--nt-nav-link-active-fg");
    }

    [Fact]
    public void DefaultAnchor_Renders_Text_Link_Without_Default_Color_Overrides_Or_Ripple() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, NTNavLinkVariant.DefaultAnchor)
            .Add(x => x.Label, "Docs"));

        var link = cut.Find("a");

        link.GetAttribute("class")!.Should().Contain("nt-nav-link-default-anchor");
        link.GetAttribute("class")!.Should().NotContain("nt-nav-link-button-chrome");
        link.GetAttribute("style").Should().BeNull();
        cut.Markup.Should().NotContain("nt-button-ripple-host");
    }

    [Fact]
    public void InlineText_Renders_Text_Link_With_No_Underline_Class_And_Color_Overrides() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, NTNavLinkVariant.InlineText)
            .Add(x => x.TextColor, NTColor.Primary)
            .Add(x => x.HoverTextColor, NTColor.Secondary)
            .Add(x => x.VisitedTextColor, NTColor.Tertiary)
            .Add(x => x.Label, "inline docs"));

        var link = cut.Find("a");
        var style = link.GetAttribute("style");

        link.GetAttribute("class")!.Should().Contain("nt-nav-link-inline-text");
        link.GetAttribute("class")!.Should().NotContain("nt-nav-link-button-chrome");
        style.Should().Contain("--nt-nav-link-fg:var(--nt-color-primary)");
        style.Should().Contain("--nt-nav-link-hover-fg:var(--nt-color-secondary)");
        style.Should().Contain("--nt-nav-link-visited-fg:var(--nt-color-tertiary)");
        cut.Markup.Should().NotContain("nt-button-ripple-host");
    }

    [Fact]
    public void Button_Chrome_Variant_Does_Not_Render_Visited_Color_Override() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, NTNavLinkVariant.Filled)
            .Add(x => x.VisitedTextColor, NTColor.Tertiary)
            .Add(x => x.Label, "Home"));

        cut.Find("a").GetAttribute("style").Should().NotContain("--nt-nav-link-visited-fg");
    }

    [Fact]
    public void LeadingIcon_Renders_Before_Content() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Home)
            .Add(x => x.Label, "Home"));

        cut.Find(".nt-nav-link-icon .nt-icon").TextContent.Should().Be(MaterialIcon.Home.Icon);
        cut.Find("a").TextContent.Should().Contain("Home");
    }

    [Fact]
    public void Text_Variant_With_Visible_BackgroundColor_Throws() {
        var render = () => Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, NTNavLinkVariant.Text)
            .Add(x => x.BackgroundColor, NTColor.Primary)
            .Add(x => x.Label, "Invalid"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Text navigation links must use a transparent BackgroundColor*");
    }

    [Fact]
    public void Empty_Label_Throws() {
        var render = () => Render<NTNavLink>();

        render.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Square_Button_Chrome_Renders_Only_The_Square_Shape_Class() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Shape, ButtonShape.Square)
            .Add(x => x.Label, "Home"));

        var link = cut.Find("a");

        link.ClassList.Should().Contain("nt-nav-link-shape-square");
        link.ClassList.Should().NotContain("nt-nav-link-shape-round");
    }

    [Fact]
    public void Exact_Route_Matching_Ignores_Query_And_Fragment() {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/home?tab=recent#summary");

        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Match, Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All)
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["href"] = "/home#top" })
            .Add(x => x.Label, "Home"));

        cut.Find("a").GetAttribute("aria-current").Should().Be("page");
    }

    [Theory]
    [InlineData("/home/settings", true)]
    [InlineData("/home-office", false)]
    public void Prefix_Route_Matching_Requires_A_Path_Segment_Boundary(string currentPath, bool expectedActive) {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(currentPath);

        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["href"] = "/home" })
            .Add(x => x.Label, "Home"));

        cut.Find("a").HasAttribute("aria-current").Should().Be(expectedActive);
    }

    [Fact]
    public void External_Route_Is_Never_Marked_Active() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> { ["href"] = "https://example.test/home" })
            .Add(x => x.Label, "External home"));

        cut.Find("a").HasAttribute("aria-current").Should().BeFalse();
    }

    [Fact]
    public void Explicit_Null_Overrides_Use_The_Variant_Defaults() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.BackgroundColor, (NTColor?)null)
            .Add(x => x.Elevation, (NTElevation?)null)
            .Add(x => x.TextColor, (NTColor?)null)
            .Add(x => x.Label, "Home"));

        var style = cut.Find("a").GetAttribute("style");

        style.Should().Contain("--nt-nav-link-bg:var(--nt-color-primary)");
        style.Should().Contain("--nt-nav-link-fg:var(--nt-color-on-primary)");
    }

    [Theory]
    [InlineData(NTNavLinkVariant.Outlined)]
    [InlineData(NTNavLinkVariant.Text)]
    public void Transparent_Button_Variants_Reject_Visible_Backgrounds(NTNavLinkVariant variant) {
        var render = () => Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, NTColor.Primary)
            .Add(x => x.Label, "Invalid"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} navigation links must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTNavLinkVariant.Filled, NTColor.Transparent)]
    public void Contained_Button_Variants_Reject_Invisible_Backgrounds(NTNavLinkVariant variant, NTColor backgroundColor) {
        var render = () => Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, backgroundColor)
            .Add(x => x.Label, "Invalid"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} navigation links must use a visible container BackgroundColor*");
    }

    [Theory]
    [InlineData("TextColor", NTColor.Transparent, NTNavLinkVariant.Filled)]
    [InlineData("HoverTextColor", NTColor.Transparent, NTNavLinkVariant.Filled)]
    [InlineData("VisitedTextColor", NTColor.Transparent, NTNavLinkVariant.InlineText)]
    [InlineData("ActiveTextColor", NTColor.Transparent, NTNavLinkVariant.Filled)]
    public void Invisible_Content_Color_Overrides_Are_Rejected(string parameterName, NTColor color, NTNavLinkVariant variant) {
        var render = () => Render<NTNavLink>(parameters => {
            parameters
                .Add(x => x.Variant, variant)
                .Add(x => x.Label, "Invalid");

            switch (parameterName) {
                case nameof(NTNavLink.TextColor):
                    parameters.Add(x => x.TextColor, color);
                    break;
                case nameof(NTNavLink.HoverTextColor):
                    parameters.Add(x => x.HoverTextColor, color);
                    break;
                case nameof(NTNavLink.VisitedTextColor):
                    parameters.Add(x => x.VisitedTextColor, color);
                    break;
                case nameof(NTNavLink.ActiveTextColor):
                    parameters.Add(x => x.ActiveTextColor, color);
                    break;
            }
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{parameterName} must be a visible content color*");
    }

    [Fact]
    public void Text_Link_Chrome_Allows_A_Consumer_Background_Without_Button_Validation() {
        var cut = Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, NTNavLinkVariant.DefaultAnchor)
            .Add(x => x.BackgroundColor, NTColor.Primary)
            .Add(x => x.Elevation, NTElevation.Low)
            .Add(x => x.Label, "Docs"));

        var link = cut.Find("a");

        link.ClassList.Should().NotContain("nt-nav-link-button-chrome");
        link.GetAttribute("style").Should().Contain("--nt-nav-link-bg:var(--nt-color-primary)");
    }

    [Theory]
    [InlineData(NTNavLinkVariant.Elevated, NTElevation.None, "Elevated navigation links must use a non-zero Elevation")]
    [InlineData(NTNavLinkVariant.Filled, NTElevation.Lowest, "Filled navigation links must use None Elevation")]
    public void Invalid_Elevation_For_Button_Chrome_Throws(NTNavLinkVariant variant, NTElevation elevation, string message) {
        var render = () => Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, variant)
            .Add(x => x.Elevation, elevation)
            .Add(x => x.Label, "Invalid"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{message}*");
    }

    [Fact]
    public void Unknown_Variant_Throws_Instead_Of_Rendering_An_Undefined_Style() {
        var render = () => Render<NTNavLink>(parameters => parameters
            .Add(x => x.Variant, (NTNavLinkVariant)999)
            .Add(x => x.Label, "Invalid"));

        render.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Variant");
    }
}
