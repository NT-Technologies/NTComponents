using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.NavRail;

/// <summary>
///     Browser-level coverage for the navigation rail's JavaScript-enhanced expand/collapse behavior.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTNavigationRail_IntegrationTests : IAsyncLifetime {

    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Rail_InitialRender_Exposes_Navigation_Links_And_Menu_Button() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        (await GetRail().IsVisibleAsync()).Should().BeTrue();

        var button = GetExpandButton();
        (await button.GetAttributeAsync("type")).Should().Be("button");
        (await button.GetAttributeAsync("href")).Should().BeNull();
        (await button.GetAttributeAsync("aria-expanded")).Should().Be("false");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();

        (await GetHomeLink().GetAttributeAsync("aria-current")).Should().Be("page");
    }

    [Fact]
    public async Task MenuButton_Click_Toggles_Accessible_Expanded_State_In_Both_Directions() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        await GetExpandButton().ClickAsync();

        var collapseButton = GetCollapseButton();
        await WaitForMenuStateAsync(expanded: true);
        (await collapseButton.GetAttributeAsync("aria-expanded")).Should().Be("true");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();

        await collapseButton.ClickAsync();

        var expandButton = GetExpandButton();
        await WaitForMenuStateAsync(expanded: false);
        (await expandButton.GetAttributeAsync("aria-expanded")).Should().Be("false");

        (await GetHomeLink().IsVisibleAsync()).Should().BeTrue();
        (await GetComponentsLink().IsVisibleAsync()).Should().BeTrue();
        (await GetReferenceLink().IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Rail_FullWidth_Indicator_Preserves_Top_Level_And_Nested_Item_Indent() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();
        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);
        await _page.WaitForFunctionAsync("() => !document.querySelector('[data-testid=\"nav-rail-under-test\"]')?.classList.contains('nt-navigation-rail-expanding')");

        var homeHug = await GetIndicatorBoundsAsync(GetHomeLink());
        var projectsHug = await GetIndicatorBoundsAsync(GetRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Projects" }));

        await NavigateToRailTestPageAsync(fullWidth: true);
        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);
        await _page.WaitForFunctionAsync("() => !document.querySelector('[data-testid=\"nav-rail-under-test\"]')?.classList.contains('nt-navigation-rail-expanding')");

        var homeFull = await GetIndicatorBoundsAsync(GetHomeLink());
        var projectsFull = await GetIndicatorBoundsAsync(GetRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Projects" }));

        homeFull[0].Should().BeApproximately(homeHug[0], 0.5);
        projectsFull[0].Should().BeApproximately(projectsHug[0], 0.5);
        homeFull[1].Should().BeGreaterThan(homeHug[1]);
        projectsFull[1].Should().BeGreaterThan(projectsHug[1]);
    }

    [Fact]
    public async Task Rail_LimitToOneExpanded_Closes_Other_Top_Level_Group_Without_Closing_Nested_Group() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();
        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);

        var workspaceTrigger = GetRail().GetByTestId("nav-rail-workspace-group").Locator(":scope > .nt-navigation-rail-group-trigger");
        var nestedTrigger = GetRail().GetByTestId("nav-rail-workspace-tools-group").Locator(":scope > .nt-navigation-rail-group-trigger");
        var administrationTrigger = GetRail().GetByTestId("nav-rail-administration-group").Locator(":scope > .nt-navigation-rail-group-trigger");

        (await workspaceTrigger.GetAttributeAsync("aria-expanded")).Should().Be("true");
        (await nestedTrigger.GetAttributeAsync("aria-expanded")).Should().Be("true");
        (await administrationTrigger.GetAttributeAsync("aria-expanded")).Should().Be("false");

        await administrationTrigger.ClickAsync();

        (await workspaceTrigger.GetAttributeAsync("aria-expanded")).Should().Be("false");
        (await nestedTrigger.GetAttributeAsync("aria-expanded")).Should().Be("true");
        (await administrationTrigger.GetAttributeAsync("aria-expanded")).Should().Be("true");
    }

    [Fact]
    public async Task Rail_Collapsed_Item_Label_Is_Limited_To_Two_Lines() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var labelIsClamped = await GetComponentsLink().Locator(".nt-navigation-rail-item-label").EvaluateAsync<bool>(
            """
            label => {
                label.textContent = 'Components with an intentionally long navigation label';

                const style = getComputedStyle(label);
                const lineHeight = Number.parseFloat(style.lineHeight);

                return style.overflow === 'hidden'
                    && style.webkitLineClamp === '2'
                    && label.getBoundingClientRect().height <= (lineHeight * 2) + 0.5
                    && label.scrollHeight > label.clientHeight;
            }
            """);

        labelIsClamped.Should().BeTrue("collapsed navigation rail labels should never occupy more than two lines");
    }

    [Fact]
    public async Task Rail_Collapsed_Selected_Item_Changes_Icon_But_Not_Label_Color() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var selectedLabelColor = await GetHomeLink().Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).color");
        var unselectedLabelColor = await GetComponentsLink().Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).color");
        var selectedIconColor = await GetHomeLink().Locator(".nt-navigation-rail-item-icon").EvaluateAsync<string>("icon => getComputedStyle(icon).color");
        var unselectedIconColor = await GetComponentsLink().Locator(".nt-navigation-rail-item-icon").EvaluateAsync<string>("icon => getComputedStyle(icon).color");

        selectedLabelColor.Should().Be(unselectedLabelColor);
        selectedIconColor.Should().NotBe(unselectedIconColor);
    }

    [Fact]
    public async Task Rail_Square_Variant_Renders_Square_Selected_Indicators_For_Items_And_Groups() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var collapsedIndicatorRadius = await GetHomeLink().Locator(".nt-navigation-rail-item-indicator").EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius");
        var groupTrigger = GetRail().GetByTestId("nav-rail-workspace-group").Locator(":scope > .nt-navigation-rail-group-trigger");
        var collapsedGroupIndicatorRadius = await groupTrigger.Locator(".nt-navigation-rail-item-indicator").EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius");

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);

        var expandedIndicatorRadius = await GetHomeLink().Locator(".nt-navigation-rail-item-content").EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius");
        var expandedGroupIndicatorRadius = await groupTrigger.Locator(".nt-navigation-rail-item-content").EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius");

        collapsedIndicatorRadius.Should().Be("0px");
        collapsedGroupIndicatorRadius.Should().Be("0px");
        expandedIndicatorRadius.Should().Be("0px");
        expandedGroupIndicatorRadius.Should().Be("0px");
    }

    [Fact]
    public async Task Rail_Compact_Mode_Reduces_Vertical_Spacing_And_Typography() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync(compact: true);

        var rail = GetRail();
        var item = GetComponentsLink();
        var groupTrigger = rail.GetByTestId("nav-rail-workspace-group").Locator(":scope > .nt-navigation-rail-group-trigger");
        var items = rail.Locator(":scope > .nt-navigation-rail-items");
        var menu = rail.Locator(":scope > .nt-navigation-rail-menu");

        (await rail.GetAttributeAsync("class")).Should().Contain("nt-navigation-rail-compact");
        (await item.EvaluateAsync<double>("item => item.getBoundingClientRect().height")).Should().Be(48);
        (await groupTrigger.EvaluateAsync<double>("item => item.getBoundingClientRect().height")).Should().Be(48);
        (await item.Locator(".nt-navigation-rail-item-indicator").EvaluateAsync<double>("indicator => indicator.getBoundingClientRect().height")).Should().Be(28);
        (await groupTrigger.Locator(".nt-navigation-rail-item-indicator").EvaluateAsync<double>("indicator => indicator.getBoundingClientRect().height")).Should().Be(28);
        (await item.EvaluateAsync<double>("item => item.querySelector('.nt-navigation-rail-item-label').getBoundingClientRect().top - item.querySelector('.nt-navigation-rail-item-indicator').getBoundingClientRect().bottom")).Should().Be(4);
        (await groupTrigger.EvaluateAsync<double>("item => item.querySelector('.nt-navigation-rail-item-label').getBoundingClientRect().top - item.querySelector('.nt-navigation-rail-item-indicator').getBoundingClientRect().bottom")).Should().Be(4);
        (await item.Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).fontSize")).Should().Be("11px");
        (await item.Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).webkitLineClamp")).Should().Be("1");
        (await items.EvaluateAsync<string>("items => getComputedStyle(items).paddingBlock")).Should().Be("0px");
        (await menu.EvaluateAsync<string>("menu => getComputedStyle(menu).marginBlock")).Should().Be("0px");

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);
        await _page.WaitForFunctionAsync("() => document.querySelector('[data-testid=\"nav-rail-under-test\"]')?.getBoundingClientRect().width >= 220");

        (await item.EvaluateAsync<double>("item => item.getBoundingClientRect().height")).Should().Be(40);
        (await groupTrigger.EvaluateAsync<double>("item => item.getBoundingClientRect().height")).Should().Be(40);
        (await item.Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).fontSize")).Should().Be("12px");
        (await groupTrigger.Locator(".nt-navigation-rail-item-label").EvaluateAsync<string>("label => getComputedStyle(label).fontSize")).Should().Be("12px");
        (await rail.GetByTestId("nav-rail-workspace-section").EvaluateAsync<double>("header => header.getBoundingClientRect().height")).Should().Be(24);
        (await rail.GetByTestId("nav-rail-workspace-section").EvaluateAsync<string>("header => getComputedStyle(header).fontSize")).Should().Be("12px");
        (await rail.GetByTestId("nav-rail-workspace-section").EvaluateAsync<string>("header => getComputedStyle(header).paddingBlock")).Should().Be("4px 0px");
    }

    [Fact]
    public async Task Rail_Hover_Focus_And_Pressed_State_Layers_Match_Selected_Indicator_Geometry() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var item = GetComponentsLink();
        await item.EvaluateAsync("item => item.addEventListener('click', event => event.preventDefault())");

        await item.HoverAsync();
        (await StateLayerMatchesSelectedGeometryAsync(item, expanded: false, expectFocusRing: false)).Should().BeTrue("collapsed hover should use the collapsed selected indicator bounds");

        await _page.Keyboard.PressAsync("Tab");
        await item.FocusAsync();
        (await StateLayerMatchesSelectedGeometryAsync(item, expanded: false, expectFocusRing: true)).Should().BeTrue("collapsed focus should use the collapsed selected indicator bounds");

        await item.HoverAsync();
        await _page.Mouse.DownAsync();
        (await RippleMatchesSelectedGeometryAsync(item, expanded: false)).Should().BeTrue("collapsed pressed ripple should use the collapsed selected indicator bounds");
        await _page.Mouse.UpAsync();

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);

        await item.HoverAsync();
        (await StateLayerMatchesSelectedGeometryAsync(item, expanded: true, expectFocusRing: false)).Should().BeTrue("expanded hover should use the expanded selected indicator bounds");

        await _page.Keyboard.PressAsync("Tab");
        await item.FocusAsync();
        (await StateLayerMatchesSelectedGeometryAsync(item, expanded: true, expectFocusRing: true)).Should().BeTrue("expanded focus should use the expanded selected indicator bounds");

        await item.HoverAsync();
        await _page.Mouse.DownAsync();
        (await RippleMatchesSelectedGeometryAsync(item, expanded: true)).Should().BeTrue("expanded pressed ripple should use the expanded selected indicator bounds");
        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task Rail_Selected_Indicator_Animates_From_Centered_Sixty_Percent_Width() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var item = GetComponentsLink();

        (await GetIndicatorAnimationResultAsync(item, expanded: false, exiting: false)).Should().Be("passed", "the collapsed indicator should expand from its centered 60% width at full height using the medium motion token");

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);

        (await GetIndicatorAnimationResultAsync(item, expanded: true, exiting: false)).Should().Be("passed", "the expanded indicator should expand from its centered 60% width at full height using the medium motion token");
    }

    [Fact]
    public async Task Rail_Deselected_Indicator_Reverses_To_Centered_Sixty_Percent_Width() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRailTestPageAsync();

        var item = GetComponentsLink();

        (await GetIndicatorAnimationResultAsync(item, expanded: false, exiting: true)).Should().Be("passed", "the collapsed indicator should contract to its centered 60% width while fading out");

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);

        (await GetIndicatorAnimationResultAsync(item, expanded: true, exiting: true)).Should().Be("passed", "the expanded indicator should contract to its centered 60% width while fading out");
    }

    [Fact]
    public async Task LiveTest_Navigation_Animates_Previous_Selection_Out_While_New_Selection_Enters() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/accordion", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rail = GetPrimaryLiveTestRail();
        var accordion = rail.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Accordion", Exact = true });
        var chips = rail.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Chips", Exact = true });
        await accordion.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        (await accordion.GetAttributeAsync("class")).Should().Contain("nt-navigation-rail-item-selected");

        await _page.EvaluateAsync(
            """
            () => {
                window.__ntNavigationRailSelectionAnimations = [];
                document.addEventListener('animationstart', event => {
                    const item = event.target instanceof Element
                        ? event.target.closest('.nt-navigation-rail-item')
                        : null;
                    const label = item?.querySelector('.nt-navigation-rail-item-label')?.textContent?.trim();

                    if (label && event.animationName.startsWith('nt-navigation-rail-indicator-')) {
                        window.__ntNavigationRailSelectionAnimations.push(`${label}:${event.animationName}`);
                    }
                });
            }
            """);

        await chips.ClickAsync();
        await _page.WaitForURLAsync("**/chips");
        await _page.WaitForFunctionAsync(
            """
            () => window.__ntNavigationRailSelectionAnimations?.some(value => value.startsWith('Accordion:nt-navigation-rail-indicator-exit')) === true
                && window.__ntNavigationRailSelectionAnimations.some(value => value.startsWith('Chips:nt-navigation-rail-indicator-enter'))
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var selectionAnimations = await _page.EvaluateAsync<string[]>("() => window.__ntNavigationRailSelectionAnimations");

        selectionAnimations.Should().Contain(value => value.StartsWith("Accordion:nt-navigation-rail-indicator-exit", StringComparison.Ordinal));
        selectionAnimations.Should().Contain(value => value.StartsWith("Chips:nt-navigation-rail-indicator-enter", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rail_MediumScreen_Collapsed_VisibleRail_Uses_Full_Collapsed_Item_Layout() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(800, 600);
        await NavigateToRailTestPageAsync();

        await _page.WaitForFunctionAsync(
            """
            () => {
                const rail = document.querySelector('[data-testid="nav-rail-under-test"]');
                const item = document.querySelector('[data-testid="nav-rail-home-item"]');

                return rail?.classList.contains('nt-navigation-rail-responsive-modal')
                    && rail.classList.contains('nt-navigation-rail-collapsed')
                    && item !== null;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var usesFullCollapsedItemLayout = await _page.EvaluateAsync<bool>(
            """
            () => {
                const rail = document.querySelector('[data-testid="nav-rail-under-test"]');
                const item = document.querySelector('[data-testid="nav-rail-home-item"]');
                const icon = item?.querySelector('.nt-navigation-rail-item-icon');
                const label = item?.querySelector('.nt-navigation-rail-item-label');

                if (!(rail instanceof HTMLElement)
                    || !(item instanceof HTMLElement)
                    || !(icon instanceof HTMLElement)
                    || !(label instanceof HTMLElement)
                    || item.classList.contains('nt-navigation-rail-item-expanded')) {
                    return false;
                }

                const railRect = rail.getBoundingClientRect();
                const iconRect = icon.getBoundingClientRect();
                const labelRect = label.getBoundingClientRect();
                const iconCenter = iconRect.left + (iconRect.width / 2);
                const labelCenter = labelRect.left + (labelRect.width / 2);

                return Math.abs(railRect.width - 96) < 1
                    && labelRect.top >= iconRect.bottom
                    && Math.abs(labelCenter - iconCenter) < 2;
            }
            """);

        usesFullCollapsedItemLayout.Should().BeTrue(
            "a visible collapsed rail in the medium range should retain the full collapsed label geometry");
    }

    [Fact]
    public async Task Rail_Expanded_FitsEveryRowOnOneLine() {
        await NavigateToRailTestPageAsync();

        var width = await ExpandAndGetSettledRailWidthAsync();
        var overflowingRows = await GetRail().EvaluateAsync<string[]>(
            """
            rail => {
                const railRight = rail.getBoundingClientRect().right;
                return [...rail.querySelectorAll('.nt-navigation-rail-item, .nt-navigation-rail-section-header')]
                    .filter(row => {
                        const label = row.querySelector('.nt-navigation-rail-item-label, .nt-navigation-rail-section-header-label');
                        const lineHeight = Number.parseFloat(getComputedStyle(label).lineHeight);
                        const content = row.querySelector('.nt-navigation-rail-item-content') ?? label;
                        return label.getBoundingClientRect().height >= lineHeight * 1.5
                            || label.scrollWidth > label.clientWidth
                            || content.getBoundingClientRect().right > railRight;
                    })
                    .map(row => row.textContent.trim());
            }
            """);

        width.Should().BeInRange(220, 360);
        overflowingRows.Should().BeEmpty("the expanded rail is sized to its widest destination, group, or section header");
    }

    [Fact]
    public async Task Rail_ExpandedWidth_DoesNotDependOnViewportWidth() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.SetViewportSizeAsync(1280, 800);
        await NavigateToRailTestPageAsync();
        var narrowViewportWidth = await ExpandAndGetSettledRailWidthAsync();

        await _page.SetViewportSizeAsync(1800, 800);
        await NavigateToRailTestPageAsync();
        var wideViewportWidth = await ExpandAndGetSettledRailWidthAsync();

        wideViewportWidth.Should().BeApproximately(narrowViewportWidth, 0.5, "the expanded width comes from the rail content, not the viewport");
    }

    [Fact]
    public async Task Rail_ExpandedWithLongerLabel_GrowsToFitIt() {
        await NavigateToRailTestPageAsync();
        var baselineWidth = await ExpandAndGetSettledRailWidthAsync();

        await NavigateToRailTestPageAsync(extraLabel: "Quarterly revenue forecasting");
        var width = await ExpandAndGetSettledRailWidthAsync();
        var labelFits = await GetRail().GetByTestId("nav-rail-extra-item").EvaluateAsync<bool>(
            """
            item => {
                const label = item.querySelector('.nt-navigation-rail-item-label');
                return label.getBoundingClientRect().height < Number.parseFloat(getComputedStyle(label).lineHeight) * 1.5
                    && label.scrollWidth <= label.clientWidth
                    && label.getBoundingClientRect().right <= item.closest('.nt-navigation-rail').getBoundingClientRect().right;
            }
            """);

        width.Should().BeGreaterThan(baselineWidth).And.BeLessThan(360);
        labelFits.Should().BeTrue();
    }

    [Fact]
    public async Task Rail_ExpandedWithVeryLongLabel_StopsAtMaximumWidth() {
        await NavigateToRailTestPageAsync(extraLabel: "Consolidated quarterly revenue forecasting and variance reporting");

        var width = await ExpandAndGetSettledRailWidthAsync();

        width.Should().BeApproximately(360, 0.5, "Material 3 expanded rails never grow beyond 360dp");
    }

    [Fact]
    public async Task Rail_ExpandedMaxWidthBelowMaterialMaximum_CapsLongLabel() {
        await NavigateToRailTestPageAsync(extraLabel: "Consolidated quarterly revenue forecasting and variance reporting", maxWidth: "280px");

        var width = await ExpandAndGetSettledRailWidthAsync();

        width.Should().BeApproximately(280, 0.5);
    }

    [Fact]
    public async Task Rail_ExpandedMaxWidthAboveMaterialMaximum_FitsLabelBeyond360() {
        await NavigateToRailTestPageAsync(extraLabel: "Consolidated quarterly revenue forecasting", maxWidth: "30rem");

        var width = await ExpandAndGetSettledRailWidthAsync();
        var labelFits = await GetRail().GetByTestId("nav-rail-extra-item").EvaluateAsync<bool>(
            """
            item => {
                const label = item.querySelector('.nt-navigation-rail-item-label');
                return label.getBoundingClientRect().height < Number.parseFloat(getComputedStyle(label).lineHeight) * 1.5
                    && label.scrollWidth <= label.clientWidth
                    && label.getBoundingClientRect().right <= item.closest('.nt-navigation-rail').getBoundingClientRect().right;
            }
            """);

        width.Should().BeGreaterThan(360).And.BeLessThanOrEqualTo(480);
        labelFits.Should().BeTrue();
    }

    [Fact]
    public async Task Rail_Expand_AnimatesFromCollapsedToMeasuredWidth() {
        await NavigateToRailTestPageAsync(extraLabel: "Quarterly revenue forecasting");

        var widths = await GetRail().EvaluateAsync<double[]>(
            """
            async rail => {
                const widths = [];
                rail.querySelector('.nt-navigation-rail-menu-button').click();
                const start = performance.now();
                while (performance.now() - start < 800) {
                    widths.push(rail.getBoundingClientRect().width);
                    await new Promise(resolve => requestAnimationFrame(resolve));
                }
                return widths;
            }
            """);
        var finalWidth = widths[^1];

        finalWidth.Should().BeGreaterThan(220);
        widths.Should().Contain(width => width > 100 && width < finalWidth - 4, "the width should transition rather than jump");
        widths.Zip(widths.Skip(1)).Should().OnlyContain(pair => pair.Second >= pair.First - 0.5, "the rail should only grow while expanding");
    }

    [Fact]
    public async Task Rail_ModalExpansionBelowMedium_UsesMeasuredWidth() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.SetViewportSizeAsync(800, 600);
        await NavigateToRailTestPageAsync(extraLabel: "Quarterly revenue forecasting");

        var railWidth = await ExpandAndGetSettledRailWidthAsync();
        var dialogWidth = await GetRail().EvaluateAsync<double>("rail => rail.closest('dialog.nt-navigation-rail-modal-dialog')?.getBoundingClientRect().width ?? -1");

        railWidth.Should().BeGreaterThan(220);
        dialogWidth.Should().BeApproximately(railWidth, 0.5);
    }

    [Theory]
    [InlineData(700, 900, true)]
    [InlineData(900, 700, false)]
    [InlineData(800, 600, false)]
    public async Task Rail_SmallScreenOrientation_Hides_Collapsed_Rail_Only_In_Portrait(int width, int height, bool hidden) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(width, height);
        await _page.GotoAsync($"{AppBaseUrl}/nav-rail-e2e-test", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rail = _page.Locator("[data-testid='nav-rail-under-test']");
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"nav-rail-under-test\"]')?.classList.contains('nt-navigation-rail-collapsed') === true",
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        var railWidth = await rail.EvaluateAsync<double>("rail => rail.getBoundingClientRect().width");
        var railId = await rail.GetAttributeAsync("id");

        (railWidth < 1).Should().Be(hidden);
        (await _page.Locator($"#{railId}-xs-menu-button").IsVisibleAsync()).Should().Be(hidden);
    }

    [Fact]
    public async Task LiveTest_NestedLayoutNavigation_Does_Not_Dispose_Primary_Rail_Interactions() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToLiveTestHomeAsync();

        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Buttons", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/buttons");

        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Nested Layout", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/nestedLayout");

        var nestedRail = GetNestedLiveTestRail();
        await nestedRail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedRail.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Grid", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/datagrid");

        var primaryRail = GetPrimaryLiveTestRail();
        var buttonsGroup = primaryRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Buttons", Exact = true });

        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("true");

        await buttonsGroup.ClickAsync();
        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("false");

        await buttonsGroup.ClickAsync();
        (await buttonsGroup.GetAttributeAsync("aria-expanded")).Should().Be("true");

        var collapseButton = primaryRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true });
        await collapseButton.ClickAsync();

        await _page.WaitForFunctionAsync(
            """
            () => {
                const rail = document.querySelector('nav[aria-label="LiveTest primary navigation"]');
                const button = rail?.querySelector('.nt-navigation-rail-menu-button');

                return rail?.classList.contains('nt-navigation-rail-collapsed') === true
                    && button?.getAttribute('aria-expanded') === 'false';
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    [Fact]
    public async Task LiveTest_Footer_Switch_Toggles_Navigation_Rail_Variant() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToLiveTestHomeAsync();

        var rail = GetPrimaryLiveTestRail();
        var variantSwitch = rail.Locator(".nt-navigation-rail-footer").GetByRole(AriaRole.Switch, new LocatorGetByRoleOptions { Name = "Square highlight", Exact = true });
        var indicator = rail.Locator(".nt-navigation-rail-item-content").First;

        (await variantSwitch.IsCheckedAsync()).Should().BeFalse();
        (await rail.GetAttributeAsync("class")).Should().NotContain("nt-navigation-rail-square");

        await variantSwitch.CheckAsync();

        (await rail.GetAttributeAsync("class")).Should().Contain("nt-navigation-rail-square");
        (await indicator.EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius")).Should().Be("0px");

        await variantSwitch.UncheckAsync();

        (await rail.GetAttributeAsync("class")).Should().NotContain("nt-navigation-rail-square");
        (await indicator.EvaluateAsync<string>("indicator => getComputedStyle(indicator).borderRadius")).Should().NotBe("0px");
    }

    private ILocator GetRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "E2E primary navigation" });
    }

    private ILocator GetPrimaryLiveTestRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "LiveTest primary navigation" });
    }

    private ILocator GetNestedLiveTestRail() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Nested layout navigation" });
    }

    private ILocator GetExpandButton() {
        return GetRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Expand navigation rail" });
    }

    private ILocator GetCollapseButton() {
        return GetRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail" });
    }

    private ILocator GetHomeLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Home" });
    }

    private ILocator GetComponentsLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Components" });
    }

    private ILocator GetReferenceLink() {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Reference" });
    }

    private async Task<double> ExpandAndGetSettledRailWidthAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await GetExpandButton().ClickAsync();
        await WaitForMenuStateAsync(expanded: true);
        await _page.WaitForFunctionAsync(
            """
            () => {
                const rail = document.querySelector('[data-testid="nav-rail-under-test"]');
                return rail !== null
                    && !rail.classList.contains('nt-navigation-rail-expanding')
                    && rail.getAnimations().length === 0
                    && !rail.parentElement?.classList.contains('nt-navigation-rail-modal-dialog-entering');
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        return await GetRail().EvaluateAsync<double>("rail => rail.getBoundingClientRect().width");
    }

    private async Task NavigateToRailTestPageAsync(bool compact = false, bool fullWidth = false, string? extraLabel = null, string? maxWidth = null) {
        ArgumentNullException.ThrowIfNull(_page);

        string?[] parameters = [
            fullWidth ? "fullWidth=true" : null,
            extraLabel is null ? null : $"extraLabel={Uri.EscapeDataString(extraLabel)}",
            maxWidth is null ? null : $"maxWidth={Uri.EscapeDataString(maxWidth)}"
        ];
        var query = string.Join('&', parameters.OfType<string>()) is { Length: > 0 } joined ? $"?{joined}" : string.Empty;
        await _page.GotoAsync($"{AppBaseUrl}/nav-rail-e2e-test{(compact ? "/true" : string.Empty)}{query}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await GetRail().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetExpandButton().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetHomeLink().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private async Task NavigateToLiveTestHomeAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync(AppBaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await GetPrimaryLiveTestRail().WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await GetPrimaryLiveTestRail().GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true }).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
    }

    private static Task<double[]> GetIndicatorBoundsAsync(ILocator item) => item.Locator(".nt-navigation-rail-item-content").EvaluateAsync<double[]>(
        "content => { const rect = content.getBoundingClientRect(); return [rect.left, rect.width]; }");

    private static Task<bool> StateLayerMatchesSelectedGeometryAsync(ILocator item, bool expanded, bool expectFocusRing) => item.EvaluateAsync<bool>(
        """
        (item, [expanded, expectFocusRing]) => {
            const content = item.querySelector('.nt-navigation-rail-item-content');
            const indicator = item.querySelector('.nt-navigation-rail-item-indicator');

            if (!(content instanceof HTMLElement) || !(indicator instanceof HTMLElement)) {
                return false;
            }

            const referenceRect = (expanded ? content : indicator).getBoundingClientRect();
            const stateStyle = getComputedStyle(content, '::after');
            const stateWidth = Number.parseFloat(stateStyle.width);
            const stateHeight = Number.parseFloat(stateStyle.height);
            const hasBackground = stateStyle.backgroundColor !== 'rgba(0, 0, 0, 0)';
            const hasExpectedFocusRing = !expectFocusRing || stateStyle.boxShadow !== 'none';

            return Math.abs(stateWidth - referenceRect.width) < 0.5
                && Math.abs(stateHeight - referenceRect.height) < 0.5
                && hasBackground
                && hasExpectedFocusRing;
        }
        """,
        new object[] { expanded, expectFocusRing });

    private static Task<bool> RippleMatchesSelectedGeometryAsync(ILocator item, bool expanded) => item.EvaluateAsync<bool>(
        """
        (item, expanded) => {
            const content = item.querySelector('.nt-navigation-rail-item-content');
            const indicator = item.querySelector('.nt-navigation-rail-item-indicator');
            const rippleHost = item.querySelector('.nt-navigation-rail-item-ripple');

            if (!(content instanceof HTMLElement) || !(indicator instanceof HTMLElement) || !(rippleHost instanceof HTMLElement)) {
                return false;
            }

            const referenceRect = (expanded ? content : indicator).getBoundingClientRect();
            const rippleRect = rippleHost.getBoundingClientRect();

            return Math.abs(rippleRect.x - referenceRect.x) < 0.5
                && Math.abs(rippleRect.y - referenceRect.y) < 0.5
                && Math.abs(rippleRect.width - referenceRect.width) < 0.5
                && Math.abs(rippleRect.height - referenceRect.height) < 0.5
                && getComputedStyle(rippleHost).overflow === 'hidden';
        }
        """,
        expanded);

    private static Task<string> GetIndicatorAnimationResultAsync(ILocator item, bool expanded, bool exiting) => item.EvaluateAsync<string>(
        """
        (item, [expanded, exiting]) => {
            const content = item.querySelector('.nt-navigation-rail-item-content');
            const indicator = item.querySelector('.nt-navigation-rail-item-indicator');

            if (!(content instanceof HTMLElement) || !(indicator instanceof HTMLElement)) {
                return 'indicator elements were not found';
            }

            item.classList.remove('nt-navigation-rail-item-deselecting', 'nt-navigation-rail-item-selected');
            void item.offsetWidth;
            item.classList.add(exiting ? 'nt-navigation-rail-item-deselecting' : 'nt-navigation-rail-item-selected');

            const animationName = exiting ? 'nt-navigation-rail-indicator-exit' : 'nt-navigation-rail-indicator-enter';
            const animation = item.getAnimations({ subtree: true })
                .find(animation => typeof animation.animationName === 'string' && animation.animationName.startsWith(animationName));

            if (!animation) {
                return `animation not found; active animations: ${item.getAnimations({ subtree: true }).map(animation => animation.animationName ?? animation.constructor.name).join(', ')}`;
            }

            animation.pause();

            const styleAt = currentTime => {
                animation.currentTime = currentTime;
                const style = expanded ? getComputedStyle(content, '::before') : getComputedStyle(indicator);
                const matrix = new DOMMatrixReadOnly(style.transform);
                const [originX, originY] = style.transformOrigin.split(' ').map(Number.parseFloat);

                return {
                    height: Number.parseFloat(style.height),
                    opacity: Number.parseFloat(style.opacity),
                    originX,
                    originY,
                    scaleX: matrix.a,
                    scaleY: matrix.d,
                    width: Number.parseFloat(style.width)
                };
            };

            const timing = animation.effect.getTiming();
            const easing = animation.effect.getKeyframes()[0].easing;
            const start = styleAt(0);
            const end = styleAt(Number(timing.duration));
            item.classList.remove('nt-navigation-rail-item-deselecting', 'nt-navigation-rail-item-selected');

            const matches = Math.abs(start.scaleX - (exiting ? 1 : 0.6)) < 0.01
                && Math.abs(start.scaleY - 1) < 0.01
                && Math.abs(start.opacity - (exiting ? 1 : 0)) < 0.01
                && Math.abs(start.originX - (start.width / 2)) < 0.5
                && Math.abs(start.originY - (start.height / 2)) < 0.5
                && Math.abs(end.scaleX - (exiting ? 0.6 : 1)) < 0.01
                && Math.abs(end.scaleY - 1) < 0.01
                && Math.abs(end.opacity - (exiting ? 0 : 1)) < 0.01
                && timing.duration === 250
                && easing === (exiting ? 'cubic-bezier(0.3, 0, 0.8, 0.15)' : 'cubic-bezier(0.05, 0.7, 0.1, 1)');

            return matches ? 'passed' : JSON.stringify({ expanded, start, end, duration: timing.duration, easing });
        }
        """,
        new object[] { expanded, exiting });

    private async Task WaitForMenuStateAsync(bool expanded) {
        ArgumentNullException.ThrowIfNull(_page);

        var expectedName = expanded ? "Collapse navigation rail" : "Expand navigation rail";
        var expectedExpanded = expanded ? "true" : "false";

        await _page.WaitForFunctionAsync(
            """
            ([expectedName, expectedExpanded]) => {
                const button = Array.from(document.querySelectorAll('button'))
                    .find(button => button.getAttribute('aria-label') === expectedName);

                return button?.getAttribute('aria-expanded') === expectedExpanded;
            }
            """,
            new[] { expectedName, expectedExpanded },
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }
}
