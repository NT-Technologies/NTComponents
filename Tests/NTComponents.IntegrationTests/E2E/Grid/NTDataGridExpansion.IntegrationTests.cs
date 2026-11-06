using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridExpansion_IntegrationTests : IAsyncLifetime {
    private readonly PlaywrightFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    private async Task OpenClaimsAsync() {
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(1440, 1050);
        await page.GotoAsync($"{_fixture.ServerAddress}/expandableGrid");
        await Expect(page.Locator("main > .nt-data-grid").GetByRole(AriaRole.Button, new() { Name = "Expand all", Exact = true })).ToBeEnabledAsync();
    }

    private async Task EnableVirtualizationAsync() {
        await _fixture.Page.GetByLabel("Virtualize claims (otherwise paginate)").CheckAsync();
        await _fixture.Page.WaitForFunctionAsync("() => document.querySelector('main > .nt-data-grid > .nt-data-grid-scroll > table > tbody > .nt-virtualize-spacer:last-child')?.getBoundingClientRect().height > 1");
    }

    [Fact]
    public async Task PaginatedClaims_ExpandByRowOrKeyboard_AndScrollCompletePaymentTables() {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        var grid = page.Locator("main > .nt-data-grid");
        var parentRows = grid.Locator(":scope > .nt-data-grid-scroll > table > tbody > .nt-data-grid-row");
        await Expect(parentRows).ToHaveCountAsync(5);
        await parentRows.Filter(new() { HasText = "CLM-0003" }).ClickAsync();
        var details = grid.Locator(".nt-data-grid-detail-scroll");
        await Expect(details).ToHaveCountAsync(1);
        await Expect(details.Locator("tbody > tr")).ToHaveCountAsync(20);
        (await details.EvaluateAsync<bool>("element => element.clientHeight <= 240 && element.scrollHeight > element.clientHeight && getComputedStyle(element).overflowY === 'auto'")).Should().BeTrue();
        await details.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        (await details.EvaluateAsync<double>("element => element.scrollTop")).Should().BeGreaterThan(0);
        await Expect(parentRows).ToHaveCountAsync(5);
        await grid.GetByRole(AriaRole.Button, new() { Name = "Collapse row", Exact = true }).ClickAsync();
        await Expect(details).ToHaveCountAsync(0);
        await grid.GetByRole(AriaRole.Button, new() { Name = "Expand row", Exact = true }).First.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(details).ToHaveCountAsync(1);
        (await details.EvaluateAsync<int>("element => element.clientHeight")).Should().BeLessThan(240);
        await grid.GetByRole(AriaRole.Button, new() { Name = "Expand all", Exact = true }).ClickAsync();
        await Expect(details).ToHaveCountAsync(5);
        await grid.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true }).ClickAsync();
        await Expect(parentRows.First).ToContainTextAsync("CLM-0006");
        await Expect(details).ToHaveCountAsync(5);
        await grid.GetByRole(AriaRole.Button, new() { Name = "Collapse all", Exact = true }).ClickAsync();
        await Expect(details).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TogglingScrolledRow_KeepsItsViewportPosition(bool initiallyExpanded) {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        if (initiallyExpanded) {
            await page.GetByLabel("Start all claims expanded").CheckAsync();
        }
        await EnableVirtualizationAsync();
        var scroll = page.Locator("main > .nt-data-grid > .nt-data-grid-scroll");
        await Expect(scroll.Locator(":scope > table > tbody > .nt-data-grid-row").First).ToContainTextAsync("CLM-0001");
        await scroll.EvaluateAsync("element => { element.scrollTop = 3000; element.dispatchEvent(new Event('scroll')); }");
        // Placeholder rows also carry nt-data-grid-row; wait for the debounced load to replace them.
        await Expect(scroll.Locator(":scope > table > tbody > .nt-data-grid-row:not(.nt-data-grid-row-placeholder)").First).ToContainTextAsync(new System.Text.RegularExpressions.Regex("CLM-(?!0001)"));
        // Allow the measured window and its spacer update to reach the next paint.
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var claim = await scroll.EvaluateAsync<string>("""
            element => {
                const top = element.getBoundingClientRect().top;
                const row = Array.from(element.querySelectorAll(':scope > table > tbody > .nt-data-grid-row'))
                    .find(row => row.getBoundingClientRect().top > top + 120 && row.getBoundingClientRect().top < top + 500);
                return row?.querySelector('.nt-data-grid-cell-content')?.textContent ?? '';
            }
            """);
        claim.Should().NotBeNullOrEmpty();
        var target = scroll.Locator(":scope > table > tbody > .nt-data-grid-row").Filter(new() { HasText = claim });
        for (var toggle = 0; toggle < 2; toggle++) {
            var before = await target.EvaluateAsync<double>("element => element.getBoundingClientRect().top");
            await target.EvaluateAsync("""
                element => {
                    const parent = element.parentElement;
                    const claim = element.querySelector('.nt-data-grid-cell-content').textContent;
                    element.addEventListener('click', () => {
                        const sample = window.gridAnchorSample = { positions: [], done: false };
                        const record = () => {
                            const row = Array.from(parent.querySelectorAll(':scope > .nt-data-grid-row'))
                                .find(row => row.querySelector('.nt-data-grid-cell-content')?.textContent === claim);
                            sample.positions.push(row?.getBoundingClientRect().top ?? -10000);
                            if (sample.positions.length < 20) requestAnimationFrame(record);
                            else sample.done = true;
                        };
                        requestAnimationFrame(record);
                    }, { capture: true, once: true });
                }
                """);
            await target.GetByRole(AriaRole.Button).ClickAsync();
            await Expect(target.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", (initiallyExpanded == (toggle == 1)) ? "true" : "false");
            await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
            await page.WaitForFunctionAsync("() => window.gridAnchorSample?.done === true");
            var positions = await page.EvaluateAsync<double[]>("() => window.gridAnchorSample.positions");
            positions.Should().OnlyContain(position => Math.Abs(position - before) <= 2, "expansion must preserve the clicked row's screen position throughout measurement and rendering");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(18)]
    public async Task WheelOverSubTable_AtBottom_ScrollsParent(int claimNumber) {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        await EnableVirtualizationAsync();
        var grid = page.Locator("main > .nt-data-grid");
        var parentScroll = grid.Locator(":scope > .nt-data-grid-scroll");
        if (claimNumber == 18) {
            await parentScroll.EvaluateAsync("element => element.scrollTop = 400");
            await Expect(parentScroll.Locator(":scope > table > tbody > .nt-data-grid-row").First).Not.ToContainTextAsync("CLM-0001");
        }
        var row = parentScroll.Locator(":scope > table > tbody > .nt-data-grid-row").Filter(new() { HasText = $"CLM-{claimNumber:D4}" });
        await row.GetByRole(AriaRole.Button, new() { Name = "Expand row", Exact = true }).ClickAsync();
        var details = grid.Locator(".nt-data-grid-detail-scroll");
        await Expect(details).ToBeVisibleAsync();

        await details.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        var detailPosition = await details.EvaluateAsync<double>("element => element.scrollTop");
        await details.EvaluateAsync("element => window.scrolledGridDetail = element");
        await details.HoverAsync();
        var firstClaim = await parentScroll.Locator(":scope > table > tbody > .nt-data-grid-row").First.InnerTextAsync();
        var parentPosition = await parentScroll.EvaluateAsync<double>("element => element.scrollTop");
        await page.Mouse.WheelAsync(0, 240);
        await page.WaitForFunctionAsync(
            "previous => document.querySelector('main > .nt-data-grid > .nt-data-grid-scroll').scrollTop > previous",
            parentPosition,
            new() { Timeout = 5000 });
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        if (claimNumber == 18) {
            await Expect(parentScroll.Locator(":scope > table > tbody > .nt-data-grid-row").First).Not.ToHaveTextAsync(firstClaim);
        }
        (await details.EvaluateAsync<double>("element => element.scrollTop")).Should().Be(detailPosition);
        (await details.EvaluateAsync<bool>("element => element === window.scrolledGridDetail")).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubTableHeaders_StayAtTopOfDetailViewport(bool virtualize) {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        if (virtualize) {
            await EnableVirtualizationAsync();
        }
        var grid = page.Locator("main > .nt-data-grid");
        await grid.Locator(":scope > .nt-data-grid-scroll > table > tbody > .nt-data-grid-row").Filter(new() { HasText = "CLM-0003" }).GetByRole(AriaRole.Button).ClickAsync();
        var details = grid.Locator(".nt-data-grid-detail-scroll");
        await Expect(details.Locator("tbody > tr")).ToHaveCountAsync(20);
        foreach (var offset in new[] { 150, 10000 }) {
            await details.EvaluateAsync("(element, offset) => element.scrollTop = offset", offset);
            var headerOffset = await details.EvaluateAsync<double>("element => element.querySelector('th').getBoundingClientRect().top - element.getBoundingClientRect().top");
            headerOffset.Should().BeInRange(-2, 2, "headers should stay flush with the top of the detail viewport");
        }
    }

    [Fact]
    public async Task WheelInsideOverflowingSubTable_ScrollsOnlyDetails() {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        await EnableVirtualizationAsync();
        var scroll = page.Locator("main > .nt-data-grid > .nt-data-grid-scroll");
        await scroll.Locator(":scope > table > tbody > .nt-data-grid-row").Filter(new() { HasText = "CLM-0003" }).GetByRole(AriaRole.Button).ClickAsync();
        var details = scroll.Locator(".nt-data-grid-detail-scroll");
        await Expect(details.Locator("tbody > tr")).ToHaveCountAsync(20);
        await details.HoverAsync();
        var parentPosition = await scroll.EvaluateAsync<double>("element => element.scrollTop");
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForFunctionAsync("() => document.querySelector('.nt-data-grid-detail-scroll').scrollTop > 0");
        (await scroll.EvaluateAsync<double>("element => element.scrollTop")).Should().Be(parentPosition);
    }

    [Fact]
    public async Task WheelOverSubTable_AtTop_ScrollsParentUpWithoutResettingDetails() {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        await EnableVirtualizationAsync();
        var scroll = page.Locator("main > .nt-data-grid > .nt-data-grid-scroll");
        await scroll.EvaluateAsync("element => element.scrollTop = 400");
        var row = scroll.Locator(":scope > table > tbody > .nt-data-grid-row").Filter(new() { HasText = "CLM-0018" });
        await row.GetByRole(AriaRole.Button, new() { Name = "Expand row", Exact = true }).ClickAsync();
        var details = scroll.Locator(".nt-data-grid-detail-scroll");
        await Expect(details.Locator("tbody > tr")).ToHaveCountAsync(20);
        await details.HoverAsync();
        var parentPosition = await scroll.EvaluateAsync<double>("element => element.scrollTop");
        parentPosition.Should().BeGreaterThan(0);
        (await details.EvaluateAsync<double>("element => element.scrollTop")).Should().Be(0);
        await page.Mouse.WheelAsync(0, -120);
        await page.WaitForFunctionAsync("previous => document.querySelector('main > .nt-data-grid > .nt-data-grid-scroll').scrollTop < previous", parentPosition);
        await Expect(row.GetByRole(AriaRole.Button)).ToHaveAttributeAsync("aria-expanded", "true");
        (await details.EvaluateAsync<double>("element => element.scrollTop")).Should().Be(0);
    }

    [Fact]
    public async Task DetailHeightChangesAboveViewport_KeepVisibleClaimInPlace() {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        await EnableVirtualizationAsync();
        var scroll = page.Locator("main > .nt-data-grid > .nt-data-grid-scroll");
        var rows = scroll.Locator(":scope > table > tbody > .nt-data-grid-row");
        await rows.Filter(new() { HasText = "CLM-0003" }).GetByRole(AriaRole.Button).ClickAsync();
        var details = scroll.Locator(".nt-data-grid-detail-scroll");
        await Expect(details.Locator("tbody > tr")).ToHaveCountAsync(20);
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        await scroll.EvaluateAsync("element => element.scrollTop += element.querySelector('.nt-data-grid-detail-scroll').getBoundingClientRect().bottom - element.getBoundingClientRect().top + 10");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        var target = rows.Filter(new() { HasText = "CLM-0005" });
        await Expect(target).ToBeVisibleAsync();
        (await details.EvaluateAsync<double>("element => element.getBoundingClientRect().bottom")).Should().BeLessThan(await scroll.EvaluateAsync<double>("element => element.getBoundingClientRect().top"));
        var position = await target.EvaluateAsync<double>("element => element.getBoundingClientRect().top");
        var parentPosition = await scroll.EvaluateAsync<double>("element => element.scrollTop");

        // Exercise real ResizeObserver delivery when arbitrary detail content changes height.
        await details.EvaluateAsync("element => element.style.maxHeight = '360px'");
        await page.WaitForFunctionAsync("previous => document.querySelector('main > .nt-data-grid > .nt-data-grid-scroll').scrollTop > previous + 100", parentPosition);
        (await target.EvaluateAsync<double>("element => element.getBoundingClientRect().top")).Should().BeApproximately(position, 2);
    }

    [Fact]
    public async Task VirtualizedClaims_KeepExpandedParentsTogether_WhileScrollingToLaterClaims() {
        var page = _fixture.Page;
        await OpenClaimsAsync();
        await page.GetByLabel("Start all claims expanded").CheckAsync();
        await EnableVirtualizationAsync();
        var grid = page.Locator("main > .nt-data-grid");
        var scroll = grid.Locator(":scope > .nt-data-grid-scroll");
        var parentRows = scroll.Locator(":scope > table > tbody > .nt-data-grid-row");
        await Expect(parentRows.First).ToContainTextAsync("CLM-0001");
        await Expect(grid.Locator(".nt-data-grid-detail-row").First).ToBeVisibleAsync();
        (await parentRows.CountAsync()).Should().BeLessThan(200);
        await scroll.EvaluateAsync("element => { element.scrollTop = 3000; element.dispatchEvent(new Event('scroll')); }");
        await Expect(parentRows.First).Not.ToContainTextAsync("CLM-0001");
        await page.WaitForFunctionAsync("""
            () => {
                const rows = Array.from(document.querySelectorAll('main > .nt-data-grid > .nt-data-grid-scroll > table > tbody > .nt-data-grid-row'));
                return rows.length > 0 && rows.every(row => row.nextElementSibling?.classList.contains('nt-data-grid-detail-row') && row.dataset.ntVirtualizeIndex === row.nextElementSibling.dataset.ntVirtualizeIndex);
            }
            """);
        await grid.GetByRole(AriaRole.Button, new() { Name = "Collapse all", Exact = true }).ClickAsync();
        await Expect(grid.Locator(".nt-data-grid-detail-row")).ToHaveCountAsync(0);
        await scroll.EvaluateAsync("element => { element.scrollTop = 0; element.dispatchEvent(new Event('scroll')); }");
        await Expect(parentRows.First).ToContainTextAsync("CLM-0001");
        await grid.GetByRole(AriaRole.Button, new() { Name = "Expand row", Exact = true }).First.ClickAsync();
        await Expect(grid.Locator(".nt-data-grid-detail-row")).ToHaveCountAsync(1);
    }
}
