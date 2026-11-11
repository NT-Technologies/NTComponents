using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Buttons;

public class NTFabMenu_Tests : BunitContext {
    private static NTIcon SampleIcon => MaterialIcon.Add;

    public NTFabMenu_Tests() {
        RippleTestingUtility.SetupRippleEffectModule(this);

        var fabMenuModule = JSInterop.SetupModule("./_content/NTComponents/Buttons/NTFabMenu.razor.js");
        fabMenuModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        fabMenuModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        fabMenuModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Default_Render_Uses_Native_Popover_And_Ssr_Page_Script() {
        var cut = Render<ValidFabMenu>();

        var host = cut.Find("nt-fab-menu");
        var button = cut.Find(".nt-fab-menu-button");
        var panel = cut.Find(".nt-fab-menu-panel");

        host.GetAttribute("class")!.Should().Contain("nt-fab-menu-placement-lower-right");
        host.GetAttribute("class")!.Should().Contain("nt-size-s");
        host.GetAttribute("class")!.Should().NotContain("nt-fab-menu-color-");
        button.GetAttribute("type")!.Should().Be("button");
        button.GetAttribute("aria-label")!.Should().Be("Create options");
        button.GetAttribute("aria-haspopup")!.Should().Be("menu");
        button.GetAttribute("aria-expanded")!.Should().Be("false");
        button.GetAttribute("popovertarget").Should().Be(panel.GetAttribute("id"));
        button.GetAttribute("popovertargetaction")!.Should().Be("toggle");
        panel.GetAttribute("popover")!.Should().Be("auto");
        panel.GetAttribute("role")!.Should().Be("menu");
        cut.Find("nt-page-script").GetAttribute("src")!.Should().Be("./_content/NTComponents/Buttons/NTFabMenu.razor.js");
    }

    [Fact]
    public void Requires_Icon_Parameter() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        render.Should().Throw<ArgumentNullException>()
            .WithMessage("*NTFabMenu requires a non-null Icon parameter*");
    }

    [Fact]
    public void Requires_AriaLabel() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*requires a non-empty AriaLabel*");
    }

    [Fact]
    public void Requires_At_Least_Two_Items() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children*");
    }

    [Fact]
    public void Allows_Up_To_Six_Items() {
        var cut = Render<SixItemFabMenu>();

        cut.FindAll(".nt-fab-menu-item").Should().HaveCount(6);
    }

    [Fact]
    public void More_Than_Six_Items_Throws() {
        var render = () => Render<SevenItemFabMenu>();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 2 to 6 NTFabMenuButtonItem or NTFabMenuAnchorItem children*");
    }

    [Fact]
    public void Button_Item_Requires_Label() {
        var item = new NTFabMenuButtonItem {
            Parent = new NTFabMenu()
        };
        var parameters = ParameterView.Empty;

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTFabMenuButtonItem requires a non-empty Label*");
    }

    [Fact]
    public void Anchor_Item_Requires_Href() {
        var item = new NTFabMenuAnchorItem {
            Parent = new NTFabMenu()
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(NTFabMenuAnchorItem.Label)] = "Import"
        });

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTFabMenuAnchorItem requires a non-empty Href*");
    }

    [Fact]
    public void Button_Item_With_Native_Onclick_Preserves_Ssr_Attribute() {
        var cut = Render<NativeOnClickFabMenu>();

        var item = cut.Find(".nt-fab-menu-item");

        item.GetAttribute("onclick")!.Should().Be("window.createDraft()");
    }

    [Fact]
    public void Button_Item_Without_Callback_Does_Not_Render_Onclick_Attribute() {
        var cut = Render<ValidFabMenu>();

        var firstItem = cut.Find(".nt-fab-menu-item");

        firstItem.HasAttribute("onclick").Should().BeFalse();
    }

    [Fact]
    public void Button_Item_With_Callback_Invokes_Callback() {
        var clicked = 0;
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Edit)
                .Add(x => x.Label, "Draft")
                .Add(x => x.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++)))
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Upload)
                .Add(x => x.Label, "Import")));

        cut.Find(".nt-fab-menu-item").Click();

        clicked.Should().Be(1);
    }

    [Fact]
    public void Anchor_Item_Renders_Href_Target_And_Label() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuAnchorItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Upload)
                .Add(x => x.Label, "Import")
                .Add(x => x.Href, "/imports")
                .Add(x => x.Target, "_blank"))
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Edit)
                .Add(x => x.Label, "Draft")));

        var anchor = cut.Find("a.nt-fab-menu-item");

        anchor.GetAttribute("href")!.Should().Be("/imports");
        anchor.GetAttribute("target")!.Should().Be("_blank");
        anchor.GetAttribute("aria-label")!.Should().Be("Import");
    }

    [Fact]
    public void Custom_Colors_Render_As_Css_Variables() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.BackgroundColor, NTColor.SecondaryContainer)
            .Add(x => x.TextColor, NTColor.OnSecondaryContainer)
            .Add(x => x.SelectedFabBackgroundColor, NTColor.Secondary)
            .Add(x => x.SelectedFabTextColor, NTColor.OnSecondary)
            .Add(x => x.MenuItemBackgroundColor, NTColor.TertiaryContainer)
            .Add(x => x.MenuItemTextColor, NTColor.OnTertiaryContainer)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        var style = cut.Find("nt-fab-menu").GetAttribute("style");

        style.Should().Contain("--nt-fab-menu-fab-bg:var(--nt-color-secondary-container)");
        style.Should().Contain("--nt-fab-menu-fab-fg:var(--nt-color-on-secondary-container)");
        style.Should().Contain("--nt-fab-menu-selected-fab-bg:var(--nt-color-secondary)");
        style.Should().Contain("--nt-fab-menu-selected-fab-fg:var(--nt-color-on-secondary)");
        style.Should().Contain("--nt-fab-menu-item-bg:var(--nt-color-tertiary-container)");
        style.Should().Contain("--nt-fab-menu-item-fg:var(--nt-color-on-tertiary-container)");
    }

    [Theory]
    [InlineData(NTSize.Smallest, "nt-size-s")]
    [InlineData(NTSize.Small, "nt-size-s")]
    [InlineData(NTSize.Medium, "nt-size-m")]
    [InlineData(NTSize.Large, "nt-size-l")]
    [InlineData(NTSize.Largest, "nt-size-l")]
    public void ButtonSize_Uses_Supported_Fab_Size(NTSize suppliedSize, string expectedClass) {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.ButtonSize, suppliedSize)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        var classes = cut.Find("nt-fab-menu").GetAttribute("class")!;

        classes.Should().Contain(expectedClass);
        classes.Should().NotContain("nt-size-xs");
        classes.Should().NotContain("nt-size-xl");
    }

    [Fact]
    public async Task RefreshAsync_Rerenders_Registered_Item_Label() {
        var cut = Render<RefreshableFabMenu>();

        cut.Find(".nt-fab-menu-item-label").TextContent.Should().Be("Draft");

        await cut.Instance.UpdateDraftLabelAsync("Draft saved");

        cut.Find(".nt-fab-menu-item-label").TextContent.Should().Be("Draft saved");
    }

    [Theory]
    [InlineData(NTFabButtonPlacement.Inline, "nt-fab-menu-placement-inline")]
    [InlineData(NTFabButtonPlacement.LowerRight, "nt-fab-menu-placement-lower-right")]
    [InlineData(NTFabButtonPlacement.LowerLeft, "nt-fab-menu-placement-lower-left")]
    [InlineData(NTFabButtonPlacement.UpperRight, "nt-fab-menu-placement-upper-right")]
    [InlineData(NTFabButtonPlacement.UpperLeft, "nt-fab-menu-placement-upper-left")]
    public void Placement_Renders_The_Public_Positioning_Class(NTFabButtonPlacement placement, string expectedClass) {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.Placement, placement)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        cut.Find("nt-fab-menu").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Unknown_Placement_Throws_Instead_Of_Rendering_An_Undefined_Position() {
        var render = () => Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.Placement, (NTFabButtonPlacement)999)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        render.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Placement");
    }

    [Fact]
    public void Expanded_Disabled_Menu_With_Custom_Id_Renders_Its_State_Contract() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.ElementId, "create-menu")
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "  Create options  ")
            .Add(x => x.Expanded, true)
            .Add(x => x.Disabled, true)
            .Add(x => x.CloseOnMenuContentClick, false)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        var button = cut.Find(".nt-fab-menu-button");
        var panel = cut.Find(".nt-fab-menu-panel");

        cut.Find("nt-fab-menu").ClassList.Should().Contain("nt-fab-menu-expanded");
        button.GetAttribute("aria-label").Should().Be("Create options");
        button.GetAttribute("aria-expanded").Should().Be("true");
        button.HasAttribute("popovertarget").Should().BeFalse();
        panel.Id.Should().Be("create-menu-menu");
        panel.GetAttribute("data-close-on-item-click").Should().Be("false");
    }

    [Theory]
    [InlineData("BackgroundColor", NTColor.Transparent)]
    [InlineData("TextColor", NTColor.Transparent)]
    [InlineData("SelectedFabBackgroundColor", NTColor.Transparent)]
    [InlineData("SelectedFabTextColor", NTColor.Transparent)]
    [InlineData("MenuItemBackgroundColor", NTColor.Transparent)]
    [InlineData("MenuItemTextColor", NTColor.Transparent)]
    public void Invisible_Color_Overrides_Are_Rejected(string parameterName, NTColor color) {
        var render = () => Render<NTFabMenu>(parameters => {
            parameters
                .Add(x => x.Icon, SampleIcon)
                .Add(x => x.AriaLabel, "Create options")
                .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
                .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import"));

            switch (parameterName) {
                case nameof(NTFabMenu.BackgroundColor):
                    parameters.Add(x => x.BackgroundColor, color);
                    break;
                case nameof(NTFabMenu.TextColor):
                    parameters.Add(x => x.TextColor, color);
                    break;
                case nameof(NTFabMenu.SelectedFabBackgroundColor):
                    parameters.Add(x => x.SelectedFabBackgroundColor, color);
                    break;
                case nameof(NTFabMenu.SelectedFabTextColor):
                    parameters.Add(x => x.SelectedFabTextColor, color);
                    break;
                case nameof(NTFabMenu.MenuItemBackgroundColor):
                    parameters.Add(x => x.MenuItemBackgroundColor, color);
                    break;
                case nameof(NTFabMenu.MenuItemTextColor):
                    parameters.Add(x => x.MenuItemTextColor, color);
                    break;
            }
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{parameterName} must be a visible color*");
    }

    [Fact]
    public void Item_Accessibility_Disabled_And_Additional_Attribute_Contracts_Are_Rendered() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Label, "Draft")
                .Add(x => x.AriaLabel, "Create draft")
                .Add(x => x.Disabled, true)
                .Add(x => x.AdditionalAttributes, new Dictionary<string, object?> {
                    ["class"] = "consumer-item",
                    ["data-origin"] = "toolbar"
                }))
            .AddChildContent<NTFabMenuButtonItem>(item => item
                .Add(x => x.Icon, MaterialIcon.Upload)
                .Add(x => x.Label, "Import")));

        var item = cut.Find(".consumer-item");

        item.ClassList.Should().Contain("nt-fab-menu-item-disabled");
        item.ClassList.Should().Contain("nt-fab-menu-item-without-icon");
        item.GetAttribute("aria-label").Should().Be("Create draft");
        item.GetAttribute("data-origin").Should().Be("toolbar");
        item.HasAttribute("disabled").Should().BeTrue();
        item.HasAttribute("popovertarget").Should().BeFalse();
    }

    [Fact]
    public void Unsupported_Enum_ButtonSize_Falls_Back_To_Small_Rendering() {
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.ButtonSize, (NTSize)999)
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        cut.Find("nt-fab-menu").ClassList.Should().Contain("nt-size-s");
    }

    [Fact]
    public async Task NotifyFabMenuExpandedChanged_Only_Notifies_When_State_Changes() {
        var changes = new List<bool>();
        var cut = Render<NTFabMenu>(parameters => parameters
            .Add(x => x.Icon, SampleIcon)
            .Add(x => x.AriaLabel, "Create options")
            .Add(x => x.ExpandedChanged, EventCallback.Factory.Create<bool>(this, changes.Add))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Draft"))
            .AddChildContent<NTFabMenuButtonItem>(item => item.Add(x => x.Label, "Import")));

        await cut.InvokeAsync(() => cut.Instance.NotifyFabMenuExpandedChanged(false));
        await cut.InvokeAsync(() => cut.Instance.NotifyFabMenuExpandedChanged(true));

        changes.Should().Equal(true);
        cut.Instance.Expanded.Should().BeTrue();
    }

    private sealed class RefreshableFabMenu : ComponentBase {
        private NTFabMenu? _menu;
        private string _draftLabel = "Draft";

        public async Task UpdateDraftLabelAsync(string label) {
            _draftLabel = label;
            await (_menu?.RefreshAsync() ?? Task.CompletedTask);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(NTIcon)MaterialIcon.Add);
            builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
            builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTFabMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Edit);
                childBuilder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), _draftLabel);
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<NTFabMenuButtonItem>(10);
                childBuilder.AddAttribute(11, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Upload);
                childBuilder.AddAttribute(12, nameof(NTFabMenuButtonItem.Label), "Import");
                childBuilder.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(4, component => _menu = (NTFabMenu)component);
            builder.CloseComponent();
        }
    }

    private sealed class ValidFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(NTIcon)MaterialIcon.Add);
            builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
            builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTFabMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Edit);
                childBuilder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), "Draft");
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<NTFabMenuButtonItem>(10);
                childBuilder.AddAttribute(11, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Upload);
                childBuilder.AddAttribute(12, nameof(NTFabMenuButtonItem.Label), "Import");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class NativeOnClickFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTFabMenu>(0);
            builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(NTIcon)MaterialIcon.Add);
            builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
            builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTFabMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Edit);
                childBuilder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), "Draft");
                childBuilder.AddAttribute(3, "onclick", "window.createDraft()");
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<NTFabMenuButtonItem>(10);
                childBuilder.AddAttribute(11, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Upload);
                childBuilder.AddAttribute(12, nameof(NTFabMenuButtonItem.Label), "Import");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class SixItemFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderItemCount(builder, 6);
    }

    private sealed class SevenItemFabMenu : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderItemCount(builder, 7);
    }

    private static void RenderItemCount(RenderTreeBuilder builder, int count) {
        builder.OpenComponent<NTFabMenu>(0);
        builder.AddAttribute(1, nameof(NTFabMenu.Icon), (object)(NTIcon)MaterialIcon.Add);
        builder.AddAttribute(2, nameof(NTFabMenu.AriaLabel), "Create options");
        builder.AddAttribute(3, nameof(NTFabMenu.ChildContent), (RenderFragment)(childBuilder => {
            for (var i = 0; i < count; i++) {
                RenderItem(childBuilder, $"Item {i + 1}");
            }
        }));
        builder.CloseComponent();
    }

    private static void RenderItem(RenderTreeBuilder builder, string label) {
        builder.OpenComponent<NTFabMenuButtonItem>(0);
        builder.AddAttribute(1, nameof(NTFabMenuButtonItem.Icon), (object)(NTIcon)MaterialIcon.Add);
        builder.AddAttribute(2, nameof(NTFabMenuButtonItem.Label), label);
        builder.CloseComponent();
    }
}
