using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace NTComponents.Tests.Chips;

public class NTChip_Tests : BunitContext {
    public NTChip_Tests() {
        var menuModule = JSInterop.SetupModule("./_content/NTComponents/Menus/NTMenu.razor.js");
        menuModule.SetupVoid("onLoad", _ => true).SetVoidResult();
        menuModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
        menuModule.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Assist_Chip_Renders_Button_With_M3_Defaults() {
        var cut = Render<NTChip>(parameters => parameters.Add(chip => chip.Label, "Add to calendar"));

        var root = cut.Find("span.nt-chip");
        var button = cut.Find("button.nt-chip-control");

        root.GetAttribute("class")!.Should().Contain("nt-chip-assist");
        root.GetAttribute("class")!.Should().Contain("nt-chip-outlined");
        root.GetAttribute("class")!.Should().Contain("nt-corner-radius-small");
        root.GetAttribute("class")!.Should().NotContain("nt-elevation-none");
        cut.Find(".nt-chip-container").GetAttribute("class")!.Should().Contain("nt-elevation-none");
        button.GetAttribute("type").Should().Be("button");
        cut.Find(".nt-chip-label").TextContent.Should().Be("Add to calendar");
        root.GetAttribute("style").Should().NotContain("--nt-chip-");
    }

    [Fact]
    public void Filter_Chip_Renders_Native_Checkbox_By_Default() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Favorites")
            .Add(chip => chip.Variant, NTChipVariant.Filter)
            .Add(chip => chip.Selected, true)
            .Add(chip => chip.ElementName, "filters"));

        var rootClass = cut.Find("span.nt-chip").GetAttribute("class")!;
        var input = cut.Find("input.nt-chip-selection-input");

        rootClass.Should().Contain("nt-chip-filter");
        rootClass.Should().Contain("nt-chip-selectable");
        rootClass.Should().Contain("nt-chip-selected");
        input.GetAttribute("type").Should().Be("checkbox");
        input.GetAttribute("name").Should().Be("filters");
        input.HasAttribute("checked").Should().BeTrue();
        cut.Find(".nt-chip-selected-leading .nt-icon").TextContent.Should().Be(MaterialIcon.Check.Icon);
    }

    [Fact]
    public void Filter_Chip_With_Menu_Renders_Native_Popover_Trigger() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Category")
            .Add(chip => chip.Variant, NTChipVariant.Filter)
            .Add(chip => chip.MenuContent, builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Restaurants");
                builder.CloseComponent();
            }));

        var trigger = cut.Find("button.nt-chip-control");
        var target = trigger.GetAttribute("popovertarget");

        trigger.GetAttribute("aria-haspopup").Should().Be("menu");
        trigger.GetAttribute("aria-expanded").Should().Be("false");
        trigger.GetAttribute("aria-pressed").Should().Be("false");
        target.Should().NotBeNullOrWhiteSpace();
        cut.Find("nt-menu").GetAttribute("id").Should().Be(target);
        cut.Find(".nt-chip-trailing .nt-icon").TextContent.Should().Be(MaterialIcon.ArrowDropDown.Icon);
        cut.FindAll("input.nt-chip-selection-input").Should().BeEmpty();
    }

    [Fact]
    public void Filter_Chip_Change_Updates_Selected_And_Invokes_Callback() {
        var selected = false;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Favorites")
            .Add(chip => chip.Variant, NTChipVariant.Filter)
            .Add(chip => chip.SelectedChanged, EventCallback.Factory.Create<bool>(this, value => selected = value)));

        cut.Find("input.nt-chip-selection-input").Change(true);

        selected.Should().BeTrue();
        cut.Instance.Selected.Should().BeFalse();
        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain("nt-chip-selected");
        cut.Find(".nt-chip-selected-leading .nt-icon").TextContent.Should().Be(MaterialIcon.Check.Icon);
    }

    [Fact]
    public void Uncontrolled_Filter_Chip_Change_Updates_Internal_Selected_State_Without_Mutating_Parameter() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Favorites")
            .Add(chip => chip.Variant, NTChipVariant.Filter));

        cut.Find("input.nt-chip-selection-input").Change(true);

        cut.Instance.Selected.Should().BeFalse();
        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain("nt-chip-selected");
        cut.Find(".nt-chip-selected-leading .nt-icon").TextContent.Should().Be(MaterialIcon.Check.Icon);
    }

    [Fact]
    public void Href_Renders_Anchor_For_Static_Navigation() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Search")
            .Add(chip => chip.Variant, NTChipVariant.Suggestion)
            .Add(chip => chip.Href, "/search")
            .Add(chip => chip.Target, "_blank")
            .Add(chip => chip.Rel, "noopener"));

        var anchor = cut.Find("a.nt-chip-control");

        anchor.GetAttribute("href").Should().Be("/search");
        anchor.GetAttribute("target").Should().Be("_blank");
        anchor.GetAttribute("rel").Should().Be("noopener");
        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain("nt-chip-elevated");
        cut.Find(".nt-chip-container").GetAttribute("class")!.Should().Contain("nt-elevation-lowest");
    }

    [Fact]
    public void Disabled_Href_Omits_Href_For_Static_Ssr_Safety() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Search")
            .Add(chip => chip.Href, "/search")
            .Add(chip => chip.Disabled, true));

        var anchor = cut.Find("a.nt-chip-control");

        anchor.HasAttribute("href").Should().BeFalse();
        anchor.GetAttribute("aria-disabled").Should().Be("true");
        anchor.GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public void Input_Chip_Renders_Static_Content_And_Close_Button_By_Default() {
        var removed = false;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "nate@example.com")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => removed = true)));

        var chipBody = cut.Find(".nt-chip-static-control");
        var removeButton = cut.Find("button.nt-chip-remove");

        chipBody.TextContent.Should().Contain("nate@example.com");
        removeButton.GetAttribute("aria-label").Should().Be("Remove nate@example.com");
        removeButton.ParentElement!.ClassList.Should().Contain("nt-chip-container");
        cut.FindAll("button.nt-chip-remove-control").Should().BeEmpty();
        removeButton.Click();
        removed.Should().BeTrue();
    }

    [Fact]
    public void Remove_Key_Removes_Input_Chip() {
        var removed = false;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "nate@example.com")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => removed = true)));

        cut.Find("button.nt-chip-remove").KeyDown("Delete");

        removed.Should().BeTrue();
    }

    [Fact]
    public void Input_Chip_With_Primary_Action_Renders_Separate_Remove_Button() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "nate@example.com")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));

        cut.Find(".nt-chip-composite-control").Should().NotBeNull();
        cut.Find("button.nt-chip-primary-action").GetAttribute("aria-label").Should().BeNull();
        cut.Find("button.nt-chip-remove").GetAttribute("aria-label").Should().Be("Remove nate@example.com");
        cut.Find("button.nt-chip-remove").ParentElement!.ClassList.Should().Contain("nt-chip-container");
        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain("nt-chip-two-action");
    }

    // Behavior source: NTChip Href, OnRemoveCallback, and NTDocumentation contracts require link navigation and removal to remain distinct native actions.
    [Fact]
    public void Removable_Input_Link_Renders_Separate_Anchor_And_Remove_Button() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "nate@example.com")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.Href, "/contacts/nate")
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));

        var primaryAction = cut.Find("a.nt-chip-primary-action");
        var removeButton = cut.Find("button.nt-chip-remove");

        primaryAction.GetAttribute("href").Should().Be("/contacts/nate");
        removeButton.GetAttribute("aria-label").Should().Be("Remove nate@example.com");
        cut.Find("span.nt-chip").ClassList.Should().Contain("nt-chip-two-action");
    }

    // Behavior source: NTChip MenuContent and OnRemoveCallback XML documentation define dropdown choice and removal as separate affordances.
    [Fact]
    public void Removable_Input_Menu_Renders_Separate_Popover_Trigger_And_Remove_Button() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Category")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.MenuContent, builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Restaurants");
                builder.CloseComponent();
            })
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));

        var primaryAction = cut.Find("button.nt-chip-primary-action");
        var menu = cut.Find("nt-menu");

        primaryAction.GetAttribute("aria-haspopup").Should().Be("menu");
        primaryAction.GetAttribute("popovertarget").Should().Be(menu.Id);
        cut.Find("button.nt-chip-remove").Should().NotBeNull();
        cut.Find("span.nt-chip").ClassList.Should().Contain("nt-chip-two-action");
    }

    // Behavior source: NTChip.Disabled XML documentation plus the native disabled-control contract prohibit activating chip callbacks.
    [Fact]
    public void Disabled_Action_And_Remove_Controls_Do_Not_Invoke_Callbacks() {
        var clickCount = 0;
        var removeCount = 0;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Disabled input")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.Disabled, true)
            .Add(chip => chip.OnClickCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++))
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => removeCount++)));

        cut.Find("button.nt-chip-primary-action").Click();
        cut.Find("button.nt-chip-remove").Click();
        cut.Find("button.nt-chip-remove").KeyDown("Delete");

        clickCount.Should().Be(0);
        removeCount.Should().Be(0);
        cut.FindAll("button").Should().AllSatisfy(button => button.HasAttribute("disabled").Should().BeTrue());
    }

    // Behavior source: Existing Remove_Key_Removes_Input_Chip contract test establishes Delete activation; standard keyboard semantics reserve other keys for their normal behavior.
    [Fact]
    public void NonRemoval_Key_Does_Not_Remove_Input_Chip() {
        var removeCount = 0;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "nate@example.com")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => removeCount++)));

        cut.Find("button.nt-chip-remove").KeyDown("Enter");

        removeCount.Should().Be(0);
    }

    // Behavior source: NTChip MenuAriaLabel, RemoveAriaLabel, Elevation, ElementId, and Selected XML documentation define these explicit render overrides.
    [Fact]
    public void Removable_Selected_Menu_Uses_Explicit_Accessibility_And_Elevation_Overrides() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.ElementId, "category-chip")
            .Add(chip => chip.Label, "Category")
            .Add(chip => chip.Variant, NTChipVariant.Input)
            .Add(chip => chip.Selected, true)
            .Add(chip => chip.Elevation, NTElevation.High)
            .Add(chip => chip.MenuAriaLabel, "Choose category")
            .Add(chip => chip.RemoveAriaLabel, "Clear category")
            .Add(chip => chip.MenuContent, builder => {
                builder.OpenComponent<NTMenuButtonItem>(0);
                builder.AddAttribute(1, nameof(NTMenuButtonItem.Label), "Restaurants");
                builder.CloseComponent();
            })
            .Add(chip => chip.OnRemoveCallback, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));

        var trigger = cut.Find("button.nt-chip-primary-action");
        var menu = cut.Find("nt-menu");

        trigger.Id.Should().Be("category-chip-menu-button");
        trigger.GetAttribute("aria-pressed").Should().Be("true");
        menu.GetAttribute("aria-label").Should().Be("Choose category");
        cut.Find("button.nt-chip-remove").GetAttribute("aria-label").Should().Be("Clear category");
        cut.Find(".nt-chip-container").ClassList.Should().Contain("nt-elevation-high");
    }

    // Behavior source: NTChip.Disabled and SelectedChanged XML documentation require disabled selectable chips to preserve selection and suppress change callbacks.
    [Fact]
    public void Disabled_Filter_Change_Does_Not_Update_Selection_Or_Invoke_Callback() {
        var changedCount = 0;
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Favorites")
            .Add(chip => chip.Variant, NTChipVariant.Filter)
            .Add(chip => chip.Disabled, true)
            .Add(chip => chip.SelectedChanged, EventCallback.Factory.Create<bool>(this, _ => changedCount++)));

        cut.Find("input.nt-chip-selection-input").Change(true);

        changedCount.Should().Be(0);
        cut.Find("span.nt-chip").ClassList.Should().NotContain("nt-chip-selected");
        cut.Find("input.nt-chip-selection-input").HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Leading_And_Trailing_Icons_Render_As_Decorative_Content() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Directions")
            .Add(chip => chip.LeadingIcon, MaterialIcon.Map)
            .Add(chip => chip.TrailingIcon, MaterialIcon.ArrowDropDown));

        cut.Find(".nt-chip-leading").GetAttribute("aria-hidden").Should().Be("true");
        cut.Find(".nt-chip-trailing").GetAttribute("aria-hidden").Should().Be("true");
        cut.Markup.Should().Contain(MaterialIcon.Map.Icon);
        cut.Markup.Should().Contain(MaterialIcon.ArrowDropDown.Icon);
    }

    [Fact]
    public void Additional_Attributes_Are_Merged_On_Root() {
        var attrs = new Dictionary<string, object> {
            ["class"] = "custom-chip",
            ["style"] = "inline-size:120px",
            ["data-test"] = "chip"
        };

        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Custom")
            .Add(chip => chip.AdditionalAttributes, attrs));

        var root = cut.Find("span.nt-chip");
        root.GetAttribute("class")!.Should().Contain("custom-chip");
        root.GetAttribute("style")!.Should().Contain("inline-size:120px");
        root.GetAttribute("data-test").Should().Be("chip");
    }

    [Fact]
    public void Color_Overrides_Render_As_Css_Variables() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Custom")
            .Add(chip => chip.BackgroundColor, NTColor.TertiaryContainer)
            .Add(chip => chip.TextColor, NTColor.OnTertiaryContainer)
            .Add(chip => chip.IconColor, NTColor.Tertiary)
            .Add(chip => chip.OutlineColor, NTColor.Tertiary)
            .Add(chip => chip.SelectedBackgroundColor, NTColor.PrimaryContainer)
            .Add(chip => chip.SelectedTextColor, NTColor.OnPrimaryContainer)
            .Add(chip => chip.SelectedIconColor, NTColor.Primary)
            .Add(chip => chip.SelectedOutlineColor, NTColor.Primary)
            .Add(chip => chip.StateLayerColor, NTColor.Tertiary)
            .Add(chip => chip.SelectedStateLayerColor, NTColor.Primary)
            .Add(chip => chip.FocusOutlineColor, NTColor.Primary)
            .Add(chip => chip.DisabledBackgroundColor, NTColor.SurfaceContainerHighest)
            .Add(chip => chip.DisabledTextColor, NTColor.OnSurfaceVariant)
            .Add(chip => chip.DisabledIconColor, NTColor.OnSurfaceVariant)
            .Add(chip => chip.DisabledOutlineColor, NTColor.OutlineVariant));

        var style = cut.Find("span.nt-chip").GetAttribute("style")!;

        style.Should().Contain("--nt-chip-bg:var(--nt-color-tertiary-container)");
        style.Should().Contain("--nt-chip-fg:var(--nt-color-on-tertiary-container)");
        style.Should().Contain("--nt-chip-icon:var(--nt-color-tertiary)");
        style.Should().Contain("--nt-chip-outline:var(--nt-color-tertiary)");
        style.Should().Contain("--nt-chip-selected-bg:var(--nt-color-primary-container)");
        style.Should().Contain("--nt-chip-selected-fg:var(--nt-color-on-primary-container)");
        style.Should().Contain("--nt-chip-selected-icon:var(--nt-color-primary)");
        style.Should().Contain("--nt-chip-selected-outline:var(--nt-color-primary)");
        style.Should().Contain("--nt-chip-state-layer:var(--nt-color-tertiary)");
        style.Should().Contain("--nt-chip-selected-state-layer:var(--nt-color-primary)");
        style.Should().Contain("--nt-chip-focus-outline:var(--nt-color-primary)");
        style.Should().Contain("--nt-chip-disabled-bg:var(--nt-color-surface-container-highest)");
        style.Should().Contain("--nt-chip-disabled-fg:var(--nt-color-on-surface-variant)");
        style.Should().Contain("--nt-chip-disabled-icon:var(--nt-color-on-surface-variant)");
        style.Should().Contain("--nt-chip-disabled-outline:var(--nt-color-outline-variant)");
    }

    [Theory]
    [InlineData(NTChipVariant.Assist, "nt-chip-assist")]
    [InlineData(NTChipVariant.Filter, "nt-chip-filter")]
    [InlineData(NTChipVariant.Input, "nt-chip-input")]
    [InlineData(NTChipVariant.Suggestion, "nt-chip-suggestion")]
    public void Supports_All_Material_Chip_Types(NTChipVariant variant, string expectedClass) {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, variant.ToString())
            .Add(chip => chip.Variant, variant));

        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Unprovided_Appearance_Does_Not_Stick_When_Variant_Changes() {
        var cut = Render<ChipVariantHost>();

        cut.Find("span.nt-chip").GetAttribute("class")!.Should().Contain("nt-chip-elevated");

        cut.Instance.SetVariant(NTChipVariant.Assist);

        var rootClass = cut.Find("span.nt-chip").GetAttribute("class")!;
        rootClass.Should().Contain("nt-chip-outlined");
        rootClass.Should().NotContain("nt-chip-elevated");
    }

    [Fact]
    public void Filter_Chip_Preserves_Leading_Icon_When_Unselected() {
        var cut = Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Maps")
            .Add(chip => chip.Variant, NTChipVariant.Filter)
            .Add(chip => chip.LeadingIcon, MaterialIcon.Map));

        cut.Markup.Should().Contain(MaterialIcon.Map.Icon);
        cut.FindAll(".nt-chip-selected-leading").Should().BeEmpty();
    }

    [Fact]
    public void Empty_Label_Throws() {
        Action render = () => Render<NTChip>(parameters => parameters.Add(chip => chip.Label, " "));

        render.Should().Throw<ArgumentException>().WithMessage("*requires a non-empty Label*");
    }

    [Fact]
    public void Selectable_Link_Throws() {
        Action render = () => Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Bad chip")
            .Add(chip => chip.Selectable, true)
            .Add(chip => chip.Href, "/bad"));

        render.Should().Throw<InvalidOperationException>().WithMessage("*Selectable chips cannot also render as links*");
    }

    [Fact]
    public void Menu_Link_Throws() {
        Action render = () => Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Bad chip")
            .Add(chip => chip.Href, "/bad")
            .Add(chip => chip.MenuContent, builder => builder.AddContent(0, "Menu")));

        render.Should().Throw<InvalidOperationException>().WithMessage("*Menu chips cannot also render as links*");
    }

    [Fact]
    public void Menu_Selectable_Throws() {
        Action render = () => Render<NTChip>(parameters => parameters
            .Add(chip => chip.Label, "Bad chip")
            .Add(chip => chip.Selectable, true)
            .Add(chip => chip.MenuContent, builder => builder.AddContent(0, "Menu")));

        render.Should().Throw<InvalidOperationException>().WithMessage("*Menu chips cannot also render as selectable checkboxes*");
    }

    private sealed class ChipVariantHost : ComponentBase {
        private NTChipVariant _variant = NTChipVariant.Suggestion;

        public void SetVariant(NTChipVariant variant) {
            _variant = variant;
            InvokeAsync(StateHasChanged).GetAwaiter().GetResult();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTChip>(0);
            builder.AddAttribute(1, nameof(NTChip.Label), _variant.ToString());
            builder.AddAttribute(2, nameof(NTChip.Variant), _variant);
            builder.CloseComponent();
        }
    }
}
