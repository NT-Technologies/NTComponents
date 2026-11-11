using System.Reflection;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;

namespace NTComponents.Tests.NavRail;

public class NTNavigationRail_Tests : BunitContext {

    public NTNavigationRail_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/NavRail/NTNavigationRail.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Rail_Inherits_Page_Script_Component() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary"));

        cut.Instance.Should().BeAssignableTo<NTPageScriptComponent<NTNavigationRail>>();
    }

    [Theory]
    [InlineData(typeof(NTNavigationRail), nameof(NTNavigationRail.AriaLabel))]
    [InlineData(typeof(NTNavigationRailItem), nameof(NTNavigationRailItem.Label))]
    [InlineData(typeof(NTNavigationRailGroup), nameof(NTNavigationRailGroup.Label))]
    [InlineData(typeof(NTNavigationRailSectionHeader), nameof(NTNavigationRailSectionHeader.Label))]
    public void Required_Parameters_Have_EditorRequired_Attribute(Type componentType, string propertyName) {
        var property = componentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        property.Should().NotBeNull();
        property!.GetCustomAttribute<EditorRequiredAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Section_Header_Renders_Static_Label_For_Ts_Module() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailSectionHeader>(header => header
                .Add(x => x.Label, "Workspace")
                .Add(x => x.AriaLevel, 3)));

        var header = cut.Find(".nt-navigation-rail-section-header");

        header.GetAttribute("role").Should().Be("heading");
        header.GetAttribute("aria-level").Should().Be("3");
        header.TextContent.Should().Contain("Workspace");
        header.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-section-header-expanded");
    }

    [Fact]
    public void Rail_Group_Renders_Collapsed_Popover_Surface_With_Nested_Content() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailGroup>(group => group
                .Add(x => x.Label, "Workspace")
                .Add(x => x.Icon, MaterialIcon.Folder)
                .Add(x => x.ChildContent, builder => {
                    builder.OpenComponent<NTNavigationRailItem>(0);
                    builder.AddComponentParameter(1, nameof(NTNavigationRailItem.Label), "Projects");
                    builder.AddComponentParameter(2, nameof(NTNavigationRailItem.Href), "/projects");
                    builder.CloseComponent();

                    builder.OpenComponent<NTNavigationRailGroup>(10);
                    builder.AddComponentParameter(11, nameof(NTNavigationRailGroup.Label), "Admin");
                    builder.AddComponentParameter(12, nameof(NTNavigationRailGroup.Icon), MaterialIcon.AdminPanelSettings);
                    builder.AddComponentParameter(13, nameof(NTNavigationRailGroup.ChildContent), (RenderFragment)(nestedBuilder => {
                        nestedBuilder.OpenComponent<NTNavigationRailItem>(0);
                        nestedBuilder.AddComponentParameter(1, nameof(NTNavigationRailItem.Label), "Users");
                        nestedBuilder.AddComponentParameter(2, nameof(NTNavigationRailItem.Href), "/users");
                        nestedBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                })));

        var group = cut.Find(".nt-navigation-rail-group");
        var trigger = cut.Find(".nt-navigation-rail-group-trigger");
        var panel = cut.Find(".nt-navigation-rail-group-panel");

        group.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-group-rail-expanded");
        trigger.GetAttribute("type").Should().Be("button");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
        trigger.GetAttribute("aria-controls").Should().Be(panel.Id);
        trigger.TextContent.Should().Contain("Workspace");
        trigger.TextContent.Should().Contain("expand_more");
        trigger.QuerySelector(".nt-navigation-rail-item-indicator").Should().NotBeNull();
        panel.GetAttribute("popover").Should().Be("auto");
        panel.GetAttribute("role").Should().Be("group");
        panel.QuerySelectorAll(":scope > .nt-navigation-rail-group-items > .nt-navigation-rail-item").Should().HaveCount(1);
        panel.QuerySelectorAll(":scope > .nt-navigation-rail-group-items > .nt-navigation-rail-group").Should().HaveCount(1);
        cut.Find(".nt-navigation-rail-group-trigger .nt-navigation-rail-item-body > .nt-navigation-rail-item-ripple.nt-button-ripple-host").Should().NotBeNull();
    }

    [Fact]
    public void Rail_Group_Renders_Expanded_Default_State_For_Ts_Module() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailGroup>(group => group
                .Add(x => x.Label, "Workspace")
                .Add(x => x.Icon, MaterialIcon.Folder)
                .Add(x => x.Expanded, true)
                .AddChildContent<NTNavigationRailItem>(item => item
                    .Add(x => x.Label, "Projects")
                    .Add(x => x.Href, "/projects"))));

        var group = cut.Find(".nt-navigation-rail-group");
        var trigger = cut.Find(".nt-navigation-rail-group-trigger");
        var panel = cut.Find(".nt-navigation-rail-group-panel");

        group.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-group-open");
        group.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-group-rail-expanded");
        group.GetAttribute("data-nt-navigation-rail-group-expanded").Should().Be("true");
        trigger.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-item-expanded");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
        trigger.QuerySelector(".nt-navigation-rail-group-chevron").Should().NotBeNull();
        panel.GetAttribute("popover").Should().Be("auto");
    }

    [Fact]
    public void Rail_Group_Renders_Collapsed_Default_For_Ts_Module() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailGroup>(group => group
                .Add(x => x.Label, "Workspace")
                .AddChildContent<NTNavigationRailItem>(item => item
                    .Add(x => x.Label, "Projects")
                    .Add(x => x.Href, "/projects"))));

        var group = cut.Find(".nt-navigation-rail-group");
        var trigger = cut.Find(".nt-navigation-rail-group-trigger");

        group.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-group-open");
        group.GetAttribute("data-nt-navigation-rail-group-expanded").Should().Be("false");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void Rail_Renders_Static_Nav_With_Menu_Button_And_Default_Divider() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var menu = cut.Find(".nt-navigation-rail-menu-button");

        rail.GetAttribute("aria-label").Should().Be("Primary");
        rail.GetAttribute("id").Should().Be("nt-navigation-rail-primary");
        rail.HasAttribute("data-permanent").Should().BeTrue();
        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-collapsed");
        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-with-divider");
        rail.GetAttribute("style")!.Should().Contain("--nt-navigation-rail-container-color:var(--nt-color-surface)");
        rail.GetAttribute("style")!.Should().Contain("--nt-navigation-rail-divider-color:var(--nt-color-outline-variant)");
        rail.GetAttribute("style")!.Should().Contain("--nt-navigation-rail-indicator-color:var(--nt-color-secondary-container)");
        cut.FindAll("nav.nt-navigation-rail > style").Should().BeEmpty();
        menu.GetAttribute("type").Should().Be("button");
        menu.HasAttribute("href").Should().BeFalse();
        menu.GetAttribute("aria-expanded").Should().Be("false");
        cut.Find(".nt-navigation-rail-menu-button > .nt-button-ripple-host").Should().NotBeNull();
        cut.Find(".nt-navigation-rail-item-content > .nt-navigation-rail-item-ripple.nt-button-ripple-host").Should().NotBeNull();
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/NavRail/NTNavigationRail.razor.js");
        cut.Markup.Should().NotContain("<script");
    }

    [Fact]
    public void Rail_Renders_Header_And_Footer_Regions_Around_Destinations() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Header, builder => builder.AddContent(0, "Rail header"))
            .Add(x => x.Footer, builder => builder.AddContent(1, "Rail footer"))
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var headerIndex = cut.Markup.IndexOf("nt-navigation-rail-header", StringComparison.Ordinal);
        var itemsIndex = cut.Markup.IndexOf("nt-navigation-rail-items", StringComparison.Ordinal);
        var footerIndex = cut.Markup.IndexOf("nt-navigation-rail-footer", StringComparison.Ordinal);

        cut.Find(".nt-navigation-rail-header").TextContent.Should().Contain("Rail header");
        cut.Find(".nt-navigation-rail-footer").TextContent.Should().Contain("Rail footer");
        headerIndex.Should().BeLessThan(itemsIndex);
        itemsIndex.Should().BeLessThan(footerIndex);
        cut.Find("nav.nt-navigation-rail").GetAttribute("style")!.Should().Contain("--nt-navigation-rail-divider-color:var(--nt-color-outline-variant)");
    }

    [Fact]
    public void Rail_ShowDivider_False_Removes_Divider_Treatment() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.ShowDivider, false)
            .Add(x => x.DividerColor, NTColor.Primary)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var rail = cut.Find("nav.nt-navigation-rail");

        rail.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-with-divider");
        rail.GetAttribute("style")!.Should().NotContain("--nt-navigation-rail-divider-color");
    }

    [Theory]
    [InlineData(NTNavigationRailVariant.Pill, false)]
    [InlineData(NTNavigationRailVariant.Square, true)]
    public void Rail_Variant_Toggles_Square_Indicator_Class(NTNavigationRailVariant variant, bool expectedSquareClass) {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Variant, variant));

        cut.Find("nav.nt-navigation-rail").ClassList.Contains("nt-navigation-rail-square").Should().Be(expectedSquareClass);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Rail_Compact_Toggles_Compact_Class(bool compact, bool expectedCompactClass) {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Compact, compact));

        cut.Find("nav.nt-navigation-rail").ClassList.Contains("nt-navigation-rail-compact").Should().Be(expectedCompactClass);
    }

    [Fact]
    public void Rail_Elevation_Parameter_Adds_Elevation_Class() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Elevation, NTElevation.Medium)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        cut.Find("nav.nt-navigation-rail").GetAttribute("class")!.Should().Contain("nt-elevation-medium");
    }

    [Fact]
    public void Rail_OpenByDefault_Renders_Progressive_Enhancement_Flag() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.OpenByDefault, true)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var menuButton = cut.Find(".nt-navigation-rail-menu-button");

        rail.GetAttribute("data-nt-navigation-rail-open-by-default").Should().Be("true");
        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-expanded");
        rail.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-collapsed");
        menuButton.GetAttribute("aria-expanded").Should().Be("true");
    }

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void Rail_LimitToOneExpanded_Renders_Browser_Contract(bool limitToOneExpanded, string expectedValue) {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.LimitToOneExpanded, limitToOneExpanded));

        cut.Find("nav.nt-navigation-rail").GetAttribute("data-nt-navigation-rail-limit-to-one-expanded").Should().Be(expectedValue);
    }

    [Fact]
    public void Rail_CloseOnNavigation_Defaults_To_Enabled() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary"));

        cut.Find("nav.nt-navigation-rail").GetAttribute("data-nt-close-on-navigation").Should().Be("true");
    }

    [Fact]
    public void Rail_CloseOnNavigation_False_Removes_Browser_OptIn() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.CloseOnNavigation, false));

        cut.Find("nav.nt-navigation-rail").HasAttribute("data-nt-close-on-navigation").Should().BeFalse();
    }

    [Fact]
    public void Rail_HideRailOnXSScreens_Renders_External_Menu_Trigger() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.HideRailOnXSScreens, true)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var xsButton = cut.Find(".nt-navigation-rail-xs-menu-button");

        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-hide-on-xs");
        rail.HasAttribute("data-permanent").Should().BeTrue();
        xsButton.GetAttribute("id").Should().Be($"{rail.Id}-xs-menu-button");
        xsButton.GetAttribute("type").Should().Be("button");
        xsButton.GetAttribute("aria-controls").Should().Be(rail.Id);
        xsButton.HasAttribute("data-permanent").Should().BeTrue();
        xsButton.GetAttribute("data-nt-navigation-rail-external-trigger").Should().Be("true");
        xsButton.GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void Rail_CollapseBehavior_Hide_Renders_External_Menu_Trigger_And_Hidden_Collapsed_Rail() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.HideRailOnXSScreens, false)
            .Add(x => x.CollapseBehavior, RailCollapseBehavior.Hide)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var menuButton = cut.Find(".nt-navigation-rail-hide-menu-button");

        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-hide-when-collapsed");
        rail.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-hide-on-xs");
        menuButton.GetAttribute("id").Should().Be($"{rail.Id}-xs-menu-button");
        menuButton.GetAttribute("aria-controls").Should().Be(rail.Id);
        menuButton.GetAttribute("data-nt-navigation-rail-external-trigger").Should().Be("true");
        menuButton.GetAttribute("aria-expanded").Should().Be("false");
    }

    [Fact]
    public void Rail_Color_Parameters_Render_Override_Variables() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.ShowDivider, true)
            .Add(x => x.ContainerColor, NTColor.SurfaceContainerHigh)
            .Add(x => x.DividerColor, NTColor.Primary)
            .Add(x => x.IndicatorColor, NTColor.TertiaryContainer)
            .Add(x => x.ActiveColor, NTColor.OnTertiaryContainer)
            .Add(x => x.ScrimColor, NTColor.Primary)
            .Add(x => x.StateLayerColor, NTColor.Tertiary)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/home")));

        var style = cut.Find("nav.nt-navigation-rail").GetAttribute("style");

        style.Should().Contain("--nt-navigation-rail-container-color:var(--nt-color-surface-container-high)");
        style.Should().Contain("--nt-navigation-rail-divider-color:var(--nt-color-primary)");
        style.Should().Contain("--nt-navigation-rail-indicator-color:var(--nt-color-tertiary-container)");
        style.Should().Contain("--nt-navigation-rail-active-color:var(--nt-color-on-tertiary-container)");
        style.Should().Contain("--nt-navigation-rail-scrim-color:var(--nt-color-primary)");
        style.Should().Contain("--nt-navigation-rail-state-layer-color:var(--nt-color-tertiary)");
    }

    [Fact]
    public void Rail_Renders_Item_State() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.IndicatorStyle, ActiveLinkIndicatorStyle.FullWidth)
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Reports")
                .Add(x => x.Href, "/reports")
                .Add(x => x.Selected, true)));

        var rail = cut.Find("nav");

        rail.GetAttribute("style")!.Should().Contain("--nt-navigation-rail-scrim-color:var(--nt-color-scrim)");
        cut.FindAll(".nt-navigation-rail-scrim").Should().BeEmpty();

        var item = cut.Find(".nt-navigation-rail-item");
        item.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-item-expanded");
        item.GetAttribute("class")!.Should().NotContain("nt-navigation-rail-item-indicator-full");
        item.GetAttribute("aria-current").Should().Be("page");
    }

    [Fact]
    public void Rail_Renders_Fab_Before_Destinations_And_Adds_Fab_State_Class() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Fab, builder => {
                builder.OpenComponent<NTFabButton>(0);
                builder.AddComponentParameter(1, nameof(NTFabButton.Icon), MaterialIcon.Add);
                builder.AddComponentParameter(2, nameof(NTFabButton.AriaLabel), "Create item");
                builder.AddComponentParameter(3, nameof(NTFabButton.EnableRipple), false);
                builder.CloseComponent();
            })
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var fab = cut.Find(".nt-navigation-rail-fab .nt-fab-button");
        var fabWrapperIndex = cut.Markup.IndexOf("nt-navigation-rail-fab", StringComparison.Ordinal);
        var itemsIndex = cut.Markup.IndexOf("nt-navigation-rail-items", StringComparison.Ordinal);

        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-with-fab");
        fab.GetAttribute("aria-label").Should().Be("Create item");
        fabWrapperIndex.Should().BeLessThan(itemsIndex);
        cut.Markup.Should().NotContain("<script");
    }

    [Fact]
    public void Rail_Allows_Extended_Fab_With_Caller_Placement() {
        var cut = Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .Add(x => x.Fab, builder => {
                builder.OpenComponent<NTFabButton>(0);
                builder.AddComponentParameter(1, nameof(NTFabButton.Icon), MaterialIcon.Add);
                builder.AddComponentParameter(2, nameof(NTFabButton.Label), "Create");
                builder.AddComponentParameter(3, nameof(NTFabButton.Placement), NTFabButtonPlacement.LowerRight);
                builder.AddComponentParameter(4, nameof(NTFabButton.EnableRipple), false);
                builder.CloseComponent();
            })
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "Home")
                .Add(x => x.Href, "/")));

        var rail = cut.Find("nav.nt-navigation-rail");
        var fab = cut.Find(".nt-navigation-rail-fab .nt-fab-button");

        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-collapsed");
        rail.GetAttribute("class")!.Should().Contain("nt-navigation-rail-with-fab");
        fab.GetAttribute("class")!.Should().Contain("nt-fab-button-extended");
        fab.GetAttribute("class")!.Should().Contain("nt-fab-button-placement-lower-right");
        fab.TextContent.Should().Contain("Create");
        cut.Markup.Should().NotContain("<script");
    }

    [Fact]
    public void External_Href_Does_Not_Throw_When_Matching_Routes() {
        var render = () => Render<NTNavigationRail>(parameters => parameters
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent<NTNavigationRailItem>(item => item
                .Add(x => x.Label, "External")
                .Add(x => x.Href, "https://example.com/docs")));

        render.Should().NotThrow();
    }

    [Fact]
    public void Disabled_Link_Removes_Href_And_Leaves_Item_Out_Of_Tab_Order() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Disabled")
            .Add(x => x.Href, "/disabled")
            .Add(x => x.Disabled, true));

        var item = cut.Find("a.nt-navigation-rail-item");

        item.HasAttribute("href").Should().BeFalse();
        item.GetAttribute("aria-disabled").Should().Be("true");
        item.GetAttribute("tabindex").Should().Be("-1");
        item.GetAttribute("class")!.Should().Contain("nt-disabled");
    }

    [Fact]
    public void Item_Preserves_Standard_AdditionalAttributes() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Reports")
            .Add(x => x.Href, "/reports")
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> {
                ["id"] = "reports-item",
                ["lang"] = "en",
                ["title"] = "Open reports",
                ["class"] = "custom-item",
                ["data-test"] = "reports"
            }));

        var item = cut.Find("a.nt-navigation-rail-item");

        item.GetAttribute("id").Should().Be("reports-item");
        item.GetAttribute("lang").Should().Be("en");
        item.GetAttribute("title").Should().Be("Open reports");
        item.GetAttribute("class")!.Should().Contain("custom-item");
        item.GetAttribute("data-test").Should().Be("reports");
    }

    [Fact]
    public void Item_Filters_Reserved_AdditionalAttributes_Case_Insensitively() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Reports")
            .Add(x => x.Href, "/reports")
            .Add(x => x.Selected, true)
            .Add(x => x.AdditionalAttributes, new Dictionary<string, object> {
                ["Href"] = "/reserved",
                ["ARIA-CURRENT"] = "step",
                ["CLASS"] = "reserved-class",
                ["Style"] = "color:red",
                ["data-test"] = "reports"
            }));

        var item = cut.Find("a.nt-navigation-rail-item");

        item.GetAttribute("href").Should().Be("/reports");
        item.GetAttribute("aria-current").Should().Be("page");
        item.GetAttribute("class")!.Should().Contain("nt-navigation-rail-item");
        item.GetAttribute("class")!.Should().NotContain("reserved-class");
        item.GetAttribute("style").Should().BeNull();
        item.GetAttribute("data-test").Should().Be("reports");
    }

    [Fact]
    public void ActiveIconContent_Takes_Precedence_Over_Default_Icon_When_Selected() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Home")
            .Add(x => x.Icon, MaterialIcon.Home)
            .Add(x => x.ActiveIconContent, builder => builder.AddContent(0, "active-icon"))
            .Add(x => x.Selected, true));

        cut.Find(".nt-navigation-rail-item-icon").TextContent.Should().Be("active-icon");
    }

    [Fact]
    public void Selected_Default_Material_Icon_Renders_Filled_Material_Symbol_Appearance() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Home")
            .Add(x => x.Icon, MaterialIcon.Home)
            .Add(x => x.Selected, true));

        var iconContainer = cut.Find(".nt-navigation-rail-item-icon");
        var icon = cut.Find(".nt-navigation-rail-item-icon .nt-icon");

        iconContainer.TextContent.Should().Contain("home");
        icon.GetAttribute("class")!.Should().Contain("nt-nav-rail-selected-icon");
        icon.GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void Unselected_Default_Material_Icon_Renders_Base_Symbol() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Home")
            .Add(x => x.Icon, MaterialIcon.Home));

        cut.Find(".nt-navigation-rail-item-icon").TextContent.Should().Contain("home");
        cut.Find(".nt-navigation-rail-item-icon .nt-icon").GetAttribute("class")!.Should().NotContain("nt-nav-rail-selected-icon");
        cut.Find(".nt-navigation-rail-item-icon .nt-icon").GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void Button_Item_Renders_Native_Button_Without_Blazor_Interactivity() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Submit")
            .Add(x => x.Value, "submit")
            .Add(x => x.ElementName, "destination"));

        var item = cut.Find("button.nt-navigation-rail-item");

        item.GetAttribute("type").Should().Be("button");
        item.GetAttribute("name").Should().Be("destination");
        item.GetAttribute("value").Should().Be("submit");
        cut.Markup.Should().NotContain("<script");
    }

    [Fact]
    public void Selected_Button_Item_Exposes_Current_Page_State() {
        var cut = Render<NTNavigationRailItem>(parameters => parameters
            .Add(x => x.Label, "Inbox")
            .Add(x => x.Selected, true));

        var item = cut.Find("button.nt-navigation-rail-item");

        item.GetAttribute("aria-current").Should().Be("page");
    }

    [Fact]
    public void Empty_Rail_AriaLabel_Throws() {
        var render = () => Render<NTNavigationRail>();

        render.Should().Throw<ArgumentException>()
            .WithMessage("*NTNavigationRail requires a non-empty AriaLabel*");
    }

    [Fact]
    public void Empty_Group_Label_Throws() {
        var render = () => Render<NTNavigationRailGroup>();

        render.Should().Throw<ArgumentException>()
            .WithMessage("*NTNavigationRailGroup requires a non-empty Label*");
    }

    [Fact]
    public void Empty_Section_Header_Label_Throws() {
        var render = () => Render<NTNavigationRailSectionHeader>();

        render.Should().Throw<ArgumentException>()
            .WithMessage("*NTNavigationRailSectionHeader requires a non-empty Label*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Section_Header_Aria_Level_Must_Be_Heading_Level(int ariaLevel) {
        var render = () => Render<NTNavigationRailSectionHeader>(parameters => parameters
            .Add(x => x.Label, "Workspace")
            .Add(x => x.AriaLevel, ariaLevel));

        render.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*AriaLevel must be between 1 and 6*");
    }

}
