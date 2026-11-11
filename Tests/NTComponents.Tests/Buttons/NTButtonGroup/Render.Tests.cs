using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NTComponents.Core;

namespace NTComponents.Tests.Buttons.NTButtonGroup;

/// <summary>
///     Verifies the rendering behavior of the <see cref="NTButtonGroup{TObjectType}" /> component.
/// </summary>
public sealed class Render_Tests : NTButtonGroupTestContext {
    /// <summary>
    ///     Ensures the component renders one button for each provided item.
    /// </summary>
    [Fact]
    public void WithMultipleItems_RendersAButtonForEachItem() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons.Count.Should().Be(items.Count);
    }

    /// <summary>
    ///     Ensures the button interaction initializer is emitted once for the group instead of once per item.
    /// </summary>
    [Fact]
    public void WithMultipleItems_RendersOneButtonInteractionInitializer() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisableRipple, true));
        var scripts = cut.FindAll("script");

        // Assert
        scripts.Should().ContainSingle();
        scripts[0].TextContent.Should().Contain("startButtonInteraction");
    }

    /// <summary>
    ///     Ensures the group-level interaction initializer is not duplicated when ripple hosts already register each button.
    /// </summary>
    [Fact]
    public void WithRippleEnabled_DoesNotRenderGroupInteractionInitializer() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));

        // Assert
        cut.FindAll("script").Should().HaveCount(items.Count);
        cut.Markup.Should().NotContain("startButtonInteraction");
    }

    /// <summary>
    ///     Validates that the disconnected display type emits the correct CSS modifier.
    /// </summary>
    [Fact]
    public void WithDisconnectedDisplayType_AddsDisconnectedModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisplayType, NTButtonGroupDisplayType.Disconnected));
        var container = cut.Find("div.nt-button-group");

        // Assert
        var classes = container.GetAttribute("class")!;
        classes.Should().Contain("nt-button-group-disconnected");
        classes.Should().NotContain("nt-button-group-connected");
    }

    /// <summary>
    ///     Validates full-width layout is opt-in rather than forced by connected groups.
    /// </summary>
    [Fact]
    public void WithConnectedDisplayType_WhenFullWidthFalse_DoesNotAddFullWidthModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.DisplayType, NTButtonGroupDisplayType.Connected));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("nt-button-group-connected");
        container.ClassList.Should().NotContain("nt-button-group-full-width");
    }

    /// <summary>
    ///     Validates full-width layout can be requested explicitly.
    /// </summary>
    [Fact]
    public void WithFullWidth_AddsFullWidthModifier() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.FullWidth, true));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("nt-button-group-full-width");
    }

    [Fact]
    public async Task SubmitInvalidSelection_ThenSelectItem_UsesNtValidationClasses() {
        var model = new RequiredSelectionModel();
        var items = CreateItems();

        var cut = Render<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.ChildContent, (EditContext _) => builder => {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<NTButtonGroup<string>>(1);
                builder.AddAttribute(2, nameof(NTButtonGroup<string>.SelectedKey), model.SelectedKey);
                builder.AddAttribute(3, nameof(NTButtonGroup<string>.SelectedKeyChanged), EventCallback.Factory.Create<string?>(this, value => model.SelectedKey = value));
                builder.AddAttribute(4, nameof(NTButtonGroup<string>.SelectedKeyExpression), (Expression<Func<string?>>)(() => model.SelectedKey));
                builder.AddAttribute(5, nameof(NTButtonGroup<string>.ChildContent), RenderItems(items));
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        var group = cut.Find("div.nt-button-group");
        group.ClassList.Should().Contain("nt-invalid");
        group.ClassList.Should().Contain("nt-modified");
        cut.Find(".nt-validation-message").TextContent.Should().Be("Choose an option");

        await cut.Find("button.nt-btn-grp-btn").ClickAsync();

        group = cut.Find("div.nt-button-group");
        group.ClassList.Should().Contain("nt-modified");
        group.ClassList.Should().Contain("nt-valid");
        group.ClassList.Should().NotContain("nt-invalid");
        model.SelectedKey.Should().Be(items.First().Key);
        cut.Instance.EditContext!.GetValidationMessages().Should().BeEmpty();
        // .NET 11 retains an empty hidden validation target for client-side validation.
        cut.FindAll(".nt-validation-message:not([hidden])").Should().BeEmpty();
    }

    /// <summary>
    ///     Ensures the group emits the shared size class consumed by the component stylesheet.
    /// </summary>
    [Fact]
    public void WithButtonSize_AddsGroupSizeClass() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.ButtonSize, NTSize.Large));
        var container = cut.Find("div.nt-button-group");

        // Assert
        container.ClassList.Should().Contain("nt-size-l");
    }

    /// <summary>
    ///     Ensures icon-only items render the spec-aligned icon button variant with an accessible name.
    /// </summary>
    [Fact]
    public void WithIconOnlyItem_RendersIconButtonWithAccessibleName() {
        // Arrange
        var items = CreateItems(iconOnlyFirstItem: true);

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(RenderItems(items)));
        var iconButton = cut.Find("button.nt-btn-grp-btn");

        // Assert
        iconButton.GetAttribute("aria-label").Should().Be(items.First().AriaLabel);
    }

    /// <summary>
    ///     Ensures whitespace accessible labels do not replace the fallback key accessible label.
    /// </summary>
    [Fact]
    public void WithWhitespaceAriaLabelAndNoLabel_RendersKeyFallback() {
        // Arrange
        RenderFragment items = builder => {
            builder.OpenComponent<NTButtonGroupItem<string>>(0);
            builder.AddAttribute(1, nameof(NTButtonGroupItem<string>.Key), "home");
            builder.AddAttribute(2, nameof(NTButtonGroupItem<string>.AriaLabel), " ");
            builder.CloseComponent();
        };

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(items));
        var button = cut.Find("button.nt-btn-grp-btn");

        // Assert
        button.GetAttribute("aria-label").Should().Be("home");
    }

    /// <summary>
    ///     Ensures icon-only items cannot render without an explicit accessible name.
    /// </summary>
    [Fact]
    public void WithIconOnlyItemWithoutAriaLabel_Throws() {
        // Arrange
        // This test proves the runtime throw that NTC1025 predicts.
#pragma warning disable NTC1025
        RenderFragment items = builder => {
            builder.OpenComponent<NTButtonGroupItem<string>>(0);
            builder.AddAttribute(1, nameof(NTButtonGroupItem<string>.Key), "search");
            builder.AddAttribute(2, nameof(NTButtonGroupItem<string>.Icon), (object)MaterialIcon.Search);
            builder.CloseComponent();
        };
#pragma warning restore NTC1025

        // Act
        var act = () => Render<NTButtonGroup<string>>(parameters => parameters.AddChildContent(items));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AriaLabel*icon-only*");
    }

    /// <summary>
    ///     Ensures selected items expose toggle state to assistive technology.
    /// </summary>
    [Fact]
    public void WithSelectedItem_RendersAriaPressed() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectedKey, items.First().Key));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons[0].GetAttribute("aria-pressed").Should().Be("true");
        buttons[1].GetAttribute("aria-pressed").Should().Be("false");
    }

    /// <summary>
    ///     Ensures action-only groups do not expose toggle state.
    /// </summary>
    [Fact]
    public async Task WithNoSelectionMode_DoesNotRenderAriaPressedOrSelectedState() {
        // Arrange
        var items = CreateItems();
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.SelectionMode, NTButtonGroupSelectionMode.None)
            .Add(p => p.SelectedKey, items.First().Key));
        var buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Act
        await buttons[0].ClickAsync();
        buttons = cut.FindAll("button.nt-btn-grp-btn");

        // Assert
        buttons.Should().AllSatisfy(button => {
            button.HasAttribute("aria-pressed").Should().BeFalse();
        });
    }

    // Behavior source: NTButtonGroup.Variant XML remarks define Elevated, Filled, Tonal, and Outlined as supported group treatments.
    [Theory]
    [InlineData(NTButtonVariant.Elevated, "nt-button-group-elevated")]
    [InlineData(NTButtonVariant.Filled, "nt-button-group-filled")]
    [InlineData(NTButtonVariant.Tonal, "nt-button-group-tonal")]
    [InlineData(NTButtonVariant.Outlined, "nt-button-group-outlined")]
    public void WithSupportedVariant_RendersItsGroupTreatment(NTButtonVariant variant, string expectedClass) {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.Variant, variant));

        // Assert
        cut.Find("div.nt-button-group").ClassList.Should().Contain(expectedClass);
        cut.FindAll("button.nt-btn-grp-btn").Should().HaveCount(items.Count);
    }

    // Behavior source: NTButtonGroup.Variant XML remarks explicitly state that Text buttons are not supported for selectable groups.
    [Fact]
    public void WithSelectableTextVariant_ThrowsContractError() {
        // Arrange
        var items = CreateItems();

        // Act
        Action render = () => Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.Variant, NTButtonVariant.Text));

        // Assert
        render.Should().Throw<InvalidOperationException>().WithMessage("*Text button groups do not support selectable behavior*");
    }

    // Behavior source: NTButtonGroup color parameter XML documentation defines explicit resting and selected color overrides.
    [Fact]
    public void WithExplicitColorOverrides_RendersAllGroupColorVariables() {
        // Arrange
        var items = CreateItems();

        // Act
        var cut = Render<NTButtonGroup<string>>(parameters => parameters
            .AddChildContent(RenderItems(items))
            .Add(p => p.BackgroundColor, NTColor.SecondaryContainer)
            .Add(p => p.TextColor, NTColor.OnSecondaryContainer)
            .Add(p => p.SelectedBackgroundColor, NTColor.Secondary)
            .Add(p => p.SelectedTextColor, NTColor.OnSecondary));

        // Assert
        var style = cut.Find("div.nt-button-group").GetAttribute("style")!;
        style.Should().Contain("--nt-button-group-bg:var(--nt-color-secondary-container)");
        style.Should().Contain("--nt-button-group-fg:var(--nt-color-on-secondary-container)");
        style.Should().Contain("--nt-button-group-selected-bg:var(--nt-color-secondary)");
        style.Should().Contain("--nt-button-group-selected-fg:var(--nt-color-on-secondary)");
    }

    private sealed class RequiredSelectionModel {
        [Required(ErrorMessage = "Choose an option")]
        public string? SelectedKey { get; set; }
    }
}
