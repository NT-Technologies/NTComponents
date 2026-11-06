using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Layout;

/// <summary>
///     Browser-level coverage for nested layout sizing within a parent shell.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTLayout_IntegrationTests : IAsyncLifetime {
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

    // Behavior source: NTLayout documents nested shell composition. The nested-layout contract is container-sized.
    [Fact]
    public async Task NestedLayout_Stays_Within_Parent_Body() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.Locator(".nt-layout-nested").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var geometry = await _page.EvaluateAsync<double[]>(
            """
            () => {
                const nested = document.querySelector('.nt-layout-nested');
                const parentBody = nested?.closest('.nt-body');
                if (!(nested instanceof HTMLElement) || !(parentBody instanceof HTMLElement)) {
                    return [nested instanceof HTMLElement ? 1 : 0, parentBody instanceof HTMLElement ? 1 : 0];
                }

                parentBody.style.blockSize = '360px';
                parentBody.style.maxBlockSize = '360px';
                nested.querySelector(':scope > .nt-navigation-rail')?.remove();
                nested.querySelector(':scope > .nt-navigation-rail-modal-placeholder')?.remove();

                const nestedRect = nested.getBoundingClientRect();
                const parentRect = parentBody.getBoundingClientRect();
                return [
                    1,
                    1,
                    nestedRect.top,
                    nestedRect.bottom,
                    parentRect.top,
                    parentRect.bottom
                ];
            }
            """);

        geometry[0].Should().Be(1, "the nested layout should render");
        geometry[1].Should().Be(1, "the nested layout should have a parent body");
        geometry[2].Should().BeGreaterThanOrEqualTo(geometry[4] - 1, "the nested layout should start within its parent body");
        geometry[3].Should().BeLessThanOrEqualTo(geometry[5] + 1, "the nested layout should end within its parent body");
    }

    [Fact]
    public async Task NestedLayout_Body_Uses_Only_Present_Header_And_Footer_Rows() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(1280, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nested = _page.Locator(".nt-layout-nested");
        await nested.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nested.EvaluateAsync("element => { element.closest('.nt-body').style.blockSize = '360px'; element.closest('.nt-body').style.maxBlockSize = '360px'; }");

        var empty = await MeasureBodyGapsAsync(nested);
        empty[0].Should().BeApproximately(0, 1, "a nested layout without a header must start its body at the top");
        empty[1].Should().BeApproximately(0, 1, "a nested layout without a footer must end its body at the bottom");

        await nested.EvaluateAsync("element => { const header = document.createElement('header'); header.className = 'nt-header'; header.style.blockSize = '64px'; element.appendChild(header); }");
        var withHeader = await MeasureBodyGapsAsync(nested);
        withHeader[0].Should().BeApproximately(64, 1, "adding a header must reserve only its height above the body");
        withHeader[1].Should().BeApproximately(0, 1);

        await nested.EvaluateAsync("element => { const footer = document.createElement('footer'); footer.className = 'nt-footer'; footer.style.blockSize = '48px'; element.appendChild(footer); }");
        var withBoth = await MeasureBodyGapsAsync(nested);
        withBoth[0].Should().BeApproximately(64, 1);
        withBoth[1].Should().BeApproximately(48, 1, "adding a footer must reserve only its height below the body");

        await nested.EvaluateAsync("element => element.querySelector(':scope > .nt-header').remove()");
        var withFooter = await MeasureBodyGapsAsync(nested);
        withFooter[0].Should().BeApproximately(0, 1, "removing the header must let the body reach the top again");
        withFooter[1].Should().BeApproximately(48, 1);
    }

    [Fact]
    public async Task NestedLayout_On_Small_Screen_Expands_Rail_Within_Its_Own_Bounds() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(800, 600);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedLayout.EvaluateAsync(
            """
            nested => {
                const parentBody = nested.closest('.nt-body');
                if (parentBody instanceof HTMLElement) {
                    parentBody.style.blockSize = '360px';
                    parentBody.style.maxBlockSize = '360px';
                }
            }
            """);

        var nestedRail = nestedLayout.Locator(":scope > .nt-navigation-rail");
        var railId = await nestedRail.GetAttributeAsync("id");
        railId.Should().NotBeNullOrWhiteSpace();

        var menuButton = nestedRail.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Expand nested navigation rail", Exact = true });
        await menuButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var collapsedGeometryIsLocal = await RailFitsNestedLayoutAsync(railId!);
        collapsedGeometryIsLocal.Should().BeTrue("the visible collapsed rail should match the nested shell height");

        var nestedBodyLeft = await nestedLayout.Locator(":scope > .nt-body").EvaluateAsync<float>("element => element.getBoundingClientRect().left");
        await menuButton.ClickAsync();

        var rail = _page.Locator($"#{railId}");
        await rail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await _page.WaitForFunctionAsync(
            "id => document.getElementById(id)?.classList.contains('nt-navigation-rail-expanded') === true",
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var expandedGeometryIsLocal = await RailFitsNestedLayoutAsync(railId!);
        expandedGeometryIsLocal.Should().BeTrue("expanding a nested rail should overlay its own content instead of opening a viewport-height dialog");
        (await nestedLayout.Locator(":scope > .nt-body").EvaluateAsync<float>("element => element.getBoundingClientRect().left"))
            .Should().BeApproximately(nestedBodyLeft, 1, "the expanded rail should overlay rather than squeeze nested content");
    }

    [Fact]
    public async Task NestedLayout_On_Extra_Small_Screen_Provides_A_Local_Trigger_And_Opens_A_Modal_Rail_Without_A_Header() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.SetViewportSizeAsync(500, 900);
        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await nestedLayout.EvaluateAsync(
            """
            nested => {
                const parentBody = nested.closest('.nt-body');
                if (parentBody instanceof HTMLElement) {
                    parentBody.style.blockSize = '360px';
                    parentBody.style.maxBlockSize = '360px';
                }
            }
            """);
        _ = await nestedLayout.Locator(":scope > .nt-header").EvaluateAllAsync<int>("headers => { headers.forEach(header => header.remove()); return headers.length; }");

        var nestedRail = nestedLayout.Locator(":scope > .nt-navigation-rail");
        var railId = await nestedRail.GetAttributeAsync("id");
        railId.Should().NotBeNullOrWhiteSpace();

        var externalMenuButton = nestedLayout.Locator($":scope > .nt-navigation-rail-xs-menu-button[aria-controls='{railId}']");
        await externalMenuButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var triggerIsLocal = await externalMenuButton.EvaluateAsync<bool>(
            """
            button => {
                const nested = button.closest('.nt-layout-nested');
                if (!(nested instanceof HTMLElement)) {
                    return false;
                }

                const buttonRect = button.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                return getComputedStyle(button).position !== 'fixed'
                    && buttonRect.top >= nestedRect.top - 1
                    && buttonRect.bottom <= nestedRect.bottom + 1;
            }
            """);
        triggerIsLocal.Should().BeTrue("a headerless nested shell needs a trigger anchored within its own layout");

        var bodyGaps = await externalMenuButton.EvaluateAsync<double[]>(
            """
            button => {
                const nested = button.closest('.nt-layout-nested');
                const body = nested.querySelector(':scope > .nt-body');
                return [body.getBoundingClientRect().top - button.getBoundingClientRect().bottom,
                    nested.getBoundingClientRect().bottom - body.getBoundingClientRect().bottom];
            }
            """);
        bodyGaps[0].Should().BeApproximately(0, 1, "the headerless body should start directly below its local rail trigger");
        bodyGaps[1].Should().BeApproximately(0, 1, "the footerless body should reach the nested layout bottom");

        await externalMenuButton.ClickAsync();
        await _page.WaitForFunctionAsync(
            "id => document.getElementById(id)?.classList.contains('nt-navigation-rail-expanded') === true",
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        var modalRailRetainsNestedBounds = await _page.EvaluateAsync<bool>(
            """
            id => {
                const rail = document.getElementById(id);
                const dialog = rail?.closest('.nt-navigation-rail-modal-dialog');
                const nested = document.querySelector('.nt-layout-nested');
                const nestedContent = document.querySelector('.nt-layout-nested > .nt-body');
                if (!(rail instanceof HTMLElement)
                    || !(dialog instanceof HTMLDialogElement)
                    || !(nested instanceof HTMLElement)
                    || !(nestedContent instanceof HTMLElement)) {
                    return false;
                }

                const dialogRect = dialog.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                const railRect = rail.getBoundingClientRect();
                return dialog.open
                    && rail.parentElement === dialog
                    && nestedContent.inert
                    && Math.abs(dialogRect.top - nestedRect.top) <= 1
                    && Math.abs(dialogRect.height - nestedRect.height) <= 1
                    && Math.abs(railRect.top - nestedRect.top) <= 1
                    && Math.abs(railRect.height - nestedRect.height) <= 1;
            }
            """,
            railId);
        modalRailRetainsNestedBounds.Should().BeTrue("the extra-small nested modal should retain the nested layout's position and height");

        await _page.SetViewportSizeAsync(800, 600);
        await _page.WaitForFunctionAsync(
            """
            id => {
                const rail = document.getElementById(id);
                return rail?.parentElement?.classList.contains('nt-layout-nested') === true
                    && rail.closest('dialog') === null
                    && rail.classList.contains('nt-navigation-rail-expanded');
            }
            """,
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.SetViewportSizeAsync(500, 900);
        await _page.WaitForFunctionAsync(
            """
            id => {
                const rail = document.getElementById(id);
                const dialog = rail?.closest('.nt-navigation-rail-modal-dialog');
                return dialog instanceof HTMLDialogElement && dialog.open;
            }
            """,
            railId,
            new PageWaitForFunctionOptions { Timeout = 5000 });

        await _page.Locator($"#{railId}").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Accordion", Exact = true }).PressAsync("Escape");
        await _page.Locator(".nt-navigation-rail-modal-dialog").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 5000 });

        (await RailFitsNestedLayoutAsync(railId!)).Should().BeTrue("closing the modal should restore the rail to its nested layout");
        (await externalMenuButton.GetAttributeAsync("aria-expanded")).Should().Be("false");
    }

    [Fact]
    public async Task NestedLayout_Body_Does_Not_Have_Rounded_Corners() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/nestedLayout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var nestedLayout = _page.Locator(".nt-layout-nested");
        await nestedLayout.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        var bodyCornerRadii = await nestedLayout.EvaluateAsync<string[]>(
            """
            nested => {
                const nestedBody = nested.querySelector(':scope > .nt-body');
                const parentBody = nested.closest('.nt-body');
                return [
                    nestedBody ? getComputedStyle(nestedBody).borderStartStartRadius : '',
                    parentBody ? getComputedStyle(parentBody).borderStartStartRadius : ''
                ];
            }
            """);

        bodyCornerRadii[0].Should().Be("0px", "nested layout bodies should have square corners");
        bodyCornerRadii[1].Should().NotBe("0px", "top-level layout body rounding should remain unchanged");
    }

    private async Task<bool> RailFitsNestedLayoutAsync(string railId) {
        ArgumentNullException.ThrowIfNull(_page);

        return await _page.EvaluateAsync<bool>(
            """
            id => {
                const rail = document.getElementById(id);
                const nested = document.querySelector('.nt-layout-nested');
                if (!(rail instanceof HTMLElement) || !(nested instanceof HTMLElement)) {
                    return false;
                }

                const railRect = rail.getBoundingClientRect();
                const nestedRect = nested.getBoundingClientRect();
                return rail.parentElement === nested
                    && rail.closest('dialog') === null
                    && railRect.top >= nestedRect.top - 1
                    && railRect.bottom <= nestedRect.bottom + 1
                    && Math.abs(railRect.height - nestedRect.height) <= 1;
            }
            """,
            railId);
    }

    private static Task<double[]> MeasureBodyGapsAsync(ILocator nested) => nested.EvaluateAsync<double[]>(
        """
        element => {
            const layout = element.getBoundingClientRect();
            const body = element.querySelector(':scope > .nt-body').getBoundingClientRect();
            return [body.top - layout.top, layout.bottom - body.bottom];
        }
        """);
}
