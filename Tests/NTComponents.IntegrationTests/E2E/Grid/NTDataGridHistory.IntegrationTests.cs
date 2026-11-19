using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Grid;

/// <summary>
///     Browser-level coverage for NTDataGrid URL and history updates on a statically routed page with an interactive grid.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridHistory_IntegrationTests : IAsyncLifetime {
    private const int SettleMilliseconds = 1500;
    private PlaywrightFixture? _fixture;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("server", false)]
    [InlineData("server", true)]
    [InlineData("wasm", false)]
    [InlineData("wasm", true)]
    public async Task Paging_Sorting_And_Back_Each_Load_Once(string mode, bool pushHistoryEntries) {
        var page = _fixture!.Page;
        await page.GotoAsync($"{_fixture.ServerAddress}/dataGridHistory/{mode}/{pushHistoryEntries.ToString().ToLowerInvariant()}");
        (await WaitForNewRequestsAsync(page, 0)).Should().Equal("start=0;sort=Customer:Ascending");

        await page.Locator(".pagination-next-page").ClickAsync();
        (await WaitForNewRequestsAsync(page, 1)).Should().Equal("start=5;sort=Customer:Ascending");
        page.Url.Should().Contain("ntdg-page=2");
        await AssertFirstRowAsync(page, "Customer 006");

        await page.Locator("thead button", new PageLocatorOptions { HasText = "Customer" }).ClickAsync();
        (await WaitForNewRequestsAsync(page, 2)).Should().Equal("start=0;sort=Customer:Descending");
        page.Url.Should().Contain("ntdg-page=1").And.Contain("ntdg-sort=Customer%3Adesc");
        await AssertFirstRowAsync(page, "Customer 040");

        if (pushHistoryEntries) {
            await page.GoBackAsync();
            (await WaitForNewRequestsAsync(page, 3)).Should().Equal("start=5;sort=Customer:Ascending");
            page.Url.Should().Contain("ntdg-page=2").And.Contain("ntdg-sort=Customer%3Aasc");
            await AssertFirstRowAsync(page, "Customer 006");
        }
    }

    // Waits for at least one request after the first previousCount, then lets late duplicates arrive before returning all new ones.
    private static async Task<IReadOnlyList<string>> WaitForNewRequestsAsync(IPage page, int previousCount) {
        var log = page.Locator(".provider-log li");
        await log.Nth(previousCount).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 30000 });
        await page.WaitForTimeoutAsync(SettleMilliseconds);
        return (await log.AllTextContentsAsync()).Skip(previousCount).ToArray();
    }

    private static async Task AssertFirstRowAsync(IPage page, string customer) =>
        (await page.Locator("tbody tr.nt-data-grid-row").First.TextContentAsync()).Should().Contain(customer);
}
