using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Buttons;

public class NTSplitButton_Tests : BunitContext {

    public NTSplitButton_Tests() {
        var splitButtonModule = JSInterop.SetupModule("./_content/NTComponents/Buttons/NTSplitButton.razor.js");
        splitButtonModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        splitButtonModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        splitButtonModule.SetupVoid("onDispose", _ => true).SetVoidResult();

        var menuModule = JSInterop.SetupModule("./_content/NTComponents/Menus/NTMenu.razor.js");
        menuModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        menuModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        menuModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Default_Render_Uses_Filled_Variant_And_Button_Type() {
        var cut = Render<ValidSplitButton>();

        var actionButton = cut.Find(".nt-split-button-leading");

        cut.Find("nt-split-button").GetAttribute("class")!.Should().Contain("nt-split-button-filled");
        actionButton.GetAttribute("type")!.Should().Be("button");
        actionButton.TextContent.Should().Contain("Save");
    }

    [Fact]
    public void Empty_Label_Without_LeadingIcon_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, string.Empty)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*requires a non-empty Label unless a LeadingIcon is supplied*");
    }

    [Fact]
    public void Icon_Only_Action_Without_ActionAriaLabel_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentException>()
            .WithMessage("*Icon-only NTSplitButton actions require a non-empty ActionAriaLabel*");
    }

    [Fact]
    public void Icon_Only_Action_With_ActionAriaLabel_Renders_Accessible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.LeadingIcon, MaterialIcon.Save)
            .Add(x => x.ActionAriaLabel, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var actionButton = cut.Find(".nt-split-button-leading");

        actionButton.GetAttribute("aria-label")!.Should().Be("Save");
        actionButton.QuerySelector(".nt-split-button-label").Should().BeNull();
    }

    [Theory]
    [InlineData(NTButtonVariant.Text)]
    [InlineData(NTButtonVariant.Outlined)]
    public void Transparent_Variants_With_Visible_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, NTColor.Primary)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a transparent BackgroundColor*");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated)]
    [InlineData(NTButtonVariant.Filled)]
    [InlineData(NTButtonVariant.Tonal)]
    public void Contained_Variants_With_Transparent_BackgroundColor_Throw(NTButtonVariant variant) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, variant)
            .Add(x => x.BackgroundColor, NTColor.Transparent)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{variant} split buttons must use a visible container BackgroundColor*");
    }

    [Fact]
    public void Filled_Button_With_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Filled)
            .Add(x => x.Elevation, NTElevation.Lowest)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filled split buttons must use None Elevation*");
    }

    [Fact]
    public void Elevated_Button_With_No_Elevation_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Invalid")
            .Add(x => x.Variant, NTButtonVariant.Elevated)
            .Add(x => x.Elevation, NTElevation.None)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*Elevated split buttons must use a non-zero Elevation*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Menu_Colors_Must_Be_Visible(bool validateBackground) {
        var render = () => Render<NTSplitButton>(parameters => {
            parameters.Add(x => x.Label, "Invalid");

            if (validateBackground) {
                parameters.Add(x => x.MenuBackgroundColor, NTColor.Transparent);
            }
            else {
                parameters.Add(x => x.MenuTextColor, NTColor.Transparent);
            }

            parameters.AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft"));
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage(validateBackground
                ? "*MenuBackgroundColor must be a visible menu container color*"
                : "*MenuTextColor must be a visible menu content color*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Selected_Menu_Colors_Must_Be_Visible(bool validateBackground) {
        var render = () => Render<NTSplitButton>(parameters => {
            parameters.Add(x => x.Label, "Invalid");

            if (validateBackground) {
                parameters.Add(x => x.MenuSelectedBackgroundColor, NTColor.Transparent);
            }
            else {
                parameters.Add(x => x.MenuSelectedTextColor, NTColor.Transparent);
            }

            parameters.AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft"));
        });

        render.Should().Throw<InvalidOperationException>()
            .WithMessage(validateBackground
                ? "*MenuSelectedBackgroundColor must be a visible selected menu item container color*"
                : "*MenuSelectedTextColor must be a visible selected menu item content color*");
    }

    [Fact]
    public void Requires_At_Least_One_Actionable_Menu_Item() {
        var render = () => Render<NTSplitButton>(parameters => parameters.Add(x => x.Label, "Save"));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child*");
    }

    [Fact]
    public void Divider_Only_Menu_Throws() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuDividerItem>());

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one NTMenuButtonItem, NTMenuAnchorItem, or NTMenuSubMenuItem child*");
    }

    [Fact]
    public void Button_Item_Requires_Label() {
        var item = new NTMenuButtonItem {
            Parent = new NTMenu()
        };
        var parameters = ParameterView.Empty;

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuButtonItem requires a non-empty Label*");
    }

    [Fact]
    public void Anchor_Item_Requires_Href() {
        var item = new NTMenuAnchorItem {
            Parent = new NTMenu()
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(NTMenuAnchorItem.Label)] = "Export"
        });

        var render = () => item.SetParametersAsync(parameters).GetAwaiter().GetResult();

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*NTMenuAnchorItem requires a non-empty Href*");
    }

    [Fact]
    public void Menu_Item_Whitespace_AriaLabel_Falls_Back_To_Visible_Label() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.AriaLabel, "   ")));

        cut.Find(".nt-menu-item").GetAttribute("aria-label")!.Should().Be("Save draft");
    }

    [Fact]
    public void Menu_Renders_Material_Menu_Classes_And_Ripple_Host() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.Icon, MaterialIcon.Save)));

        cut.Find(".nt-split-button-menu-panel").ClassList.Should().Contain("nt-menu");
        cut.Find(".nt-split-button-menu-panel").ClassList.Should().Contain("nt-menu-placement-auto");
        cut.Find(".nt-menu-item").ClassList.Should().Contain("nt-menu-item");
        cut.Find(".nt-menu-item .nt-button-ripple-host").Should().NotBeNull();
    }

    [Fact]
    public void Selected_Menu_Item_Renders_Selected_State() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Save draft")
                .Add(x => x.Selected, true)));

        var item = cut.Find(".nt-menu-item");

        item.ClassList.Should().Contain("nt-menu-item-selected");
        item.GetAttribute("aria-selected")!.Should().Be("true");
    }

    [Fact]
    public void Menu_SubMenu_Item_Renders_Nested_Anchored_Menu() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .AddChildContent<NTMenuSubMenuItem>(subMenu => subMenu
                .Add(x => x.Label, "More actions")
                .Add(x => x.ChildContent, (RenderFragment)(builder => {
                    builder.OpenComponent<NTMenuButtonItem>(0);
                    builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Schedule send");
                    builder.CloseComponent();
                }))));

        var trigger = cut.Find("button[data-nt-menu-submenu-trigger]");
        var nestedMenu = cut.Find("nt-menu.nt-menu-submenu");

        trigger.GetAttribute("aria-haspopup")!.Should().Be("menu");
        trigger.GetAttribute("popovertarget").Should().Be(nestedMenu.Id);
        nestedMenu.GetAttribute("data-submenu")!.Should().Be("true");
        nestedMenu.TextContent.Should().Contain("Schedule send");
    }

    [Theory]
    [InlineData(NTButtonVariant.Elevated, "nt-split-button-elevated", "surface-container-low", "primary")]
    [InlineData(NTButtonVariant.Filled, "nt-split-button-filled", "primary", "on-primary")]
    [InlineData(NTButtonVariant.Tonal, "nt-split-button-tonal", "secondary-container", "on-secondary-container")]
    [InlineData(NTButtonVariant.Outlined, "nt-split-button-outlined", "transparent", "primary")]
    [InlineData(NTButtonVariant.Text, "nt-split-button-text", "transparent", "primary")]
    public void Variants_Render_Their_Default_Color_Contracts(NTButtonVariant variant, string expectedClass, string expectedBackground, string expectedText) {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.Variant, variant)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var host = cut.Find("nt-split-button");

        host.ClassList.Should().Contain(expectedClass);
        host.GetAttribute("style").Should().Contain($"--nt-split-button-bg:var(--nt-color-{expectedBackground})");
        host.GetAttribute("style").Should().Contain($"--nt-split-button-fg:var(--nt-color-{expectedText})");
    }

    [Fact]
    public void Explicit_Null_Overrides_Fall_Back_To_Default_Colors() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.BackgroundColor, (NTColor?)null)
            .Add(x => x.Elevation, (NTElevation?)null)
            .Add(x => x.TextColor, (NTColor?)null)
            .Add(x => x.MenuBackgroundColor, (NTColor?)null)
            .Add(x => x.MenuSelectedBackgroundColor, (NTColor?)null)
            .Add(x => x.MenuSelectedTextColor, (NTColor?)null)
            .Add(x => x.MenuTextColor, (NTColor?)null)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var hostStyle = cut.Find("nt-split-button").GetAttribute("style");
        var menuStyle = cut.Find(".nt-split-button-menu-panel").GetAttribute("style");

        hostStyle.Should().Contain("--nt-split-button-bg:var(--nt-color-primary)");
        hostStyle.Should().Contain("--nt-split-button-fg:var(--nt-color-on-primary)");
        menuStyle.Should().Contain("--nt-menu-container-color:var(--nt-color-surface-container-low)");
    }

    [Fact]
    public void Custom_Labels_And_ElementId_Drive_Menu_Accessibility_And_Anchoring() {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.ElementId, "save-options")
            .Add(x => x.Label, "Save")
            .Add(x => x.MenuButtonLabel, "Choose save format")
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var menuButton = cut.Find(".nt-split-button-trailing");
        var menu = cut.Find(".nt-split-button-menu-panel");

        menuButton.GetAttribute("aria-label").Should().Be("Choose save format");
        menuButton.GetAttribute("popovertarget").Should().Be("save-options-menu");
        menu.Id.Should().Be("save-options-menu");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Disabled_State_Removes_The_Menu_Popover_Target(bool disabled, bool menuButtonDisabled) {
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.Disabled, disabled)
            .Add(x => x.MenuButtonDisabled, menuButtonDisabled)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        var menuButton = cut.Find(".nt-split-button-trailing");

        menuButton.HasAttribute("disabled").Should().BeTrue();
        menuButton.HasAttribute("popovertarget").Should().BeFalse();
    }

    [Fact]
    public async Task NotifySplitButtonExpandedChanged_Updates_State_And_Only_Notifies_On_Change() {
        var changes = new List<bool>();
        var cut = Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.ExpandedChanged, EventCallback.Factory.Create<bool>(this, changes.Add))
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        await cut.InvokeAsync(() => cut.Instance.NotifySplitButtonExpandedChanged(false));
        await cut.InvokeAsync(() => cut.Instance.NotifySplitButtonExpandedChanged(true));

        changes.Should().Equal(true);
        cut.Instance.Expanded.Should().BeTrue();
    }

    [Theory]
    [InlineData(NTColor.Transparent)]
    public void Invisible_TextColor_Throws(NTColor textColor) {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.TextColor, textColor)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<InvalidOperationException>()
            .WithMessage("*TextColor must be a visible split button content color*");
    }

    [Fact]
    public void Unknown_Variant_Throws_Instead_Of_Rendering_An_Undefined_Style() {
        var render = () => Render<NTSplitButton>(parameters => parameters
            .Add(x => x.Label, "Save")
            .Add(x => x.Variant, (NTButtonVariant)999)
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Save draft")));

        render.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Variant");
    }

    private sealed class ValidSplitButton : ComponentBase {

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTSplitButton>(0);
            builder.AddAttribute(1, nameof(NTSplitButton.Label), "Save");
            builder.AddAttribute(2, nameof(NTSplitButton.ChildContent), (RenderFragment)(childBuilder => {
                childBuilder.OpenComponent<NTMenuButtonItem>(0);
                childBuilder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Save draft");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
