using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

/// <summary>
///     Browser-level coverage for two prefixed NTDataGrids sharing one URL.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridMultiQuery_IntegrationTests : IAsyncLifetime {
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
    public async Task SortingOneGrid_KeepsOtherGridAndUnrelatedKeys(string mode, bool pushSecond) {
        var page = _fixture!.Page;
        await page.GotoAsync($"{_fixture.ServerAddress}/dataGridMultiQuery/{mode}/{pushSecond.ToString().ToLowerInvariant()}?tab=open");
        var firstGrid = page.Locator(".nt-data-grid").Nth(0);
        var secondGrid = page.Locator(".nt-data-grid").Nth(1);
        await secondGrid.Locator("tbody tr.nt-data-grid-row").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });

        await firstGrid.Locator(".pagination-next-page").ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("a-page=2"));
        await secondGrid.Locator("thead button", new LocatorLocatorOptions { HasText = "Customer" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("b-sort=Customer%3Adesc"));

        page.Url.Should().Contain("tab=open").And.Contain("a-page=2").And.Contain("a-sort=Customer%3Aasc");

        await page.ReloadAsync();
        await Expect(firstGrid.Locator("tbody tr.nt-data-grid-row").First).ToContainTextAsync("Customer 006", new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
        await Expect(secondGrid.Locator("tbody tr.nt-data-grid-row").First).ToContainTextAsync("Customer 040", new LocatorAssertionsToContainTextOptions { Timeout = 30000 });
    }
}
