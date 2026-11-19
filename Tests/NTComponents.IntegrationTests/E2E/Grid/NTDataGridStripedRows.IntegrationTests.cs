using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Grid;

/// <summary>
///     Browser-level coverage for striped NTDataGrid row backgrounds and their interaction states.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTDataGridStripedRows_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Hover_Changes_Even_And_Odd_Striped_Rows_To_The_Same_State_Color() {
        ArgumentNullException.ThrowIfNull(_page);

        // Wait for WebAssembly hydration so the prerendered rows are not replaced mid-test.
        await _page.GotoAsync($"{_fixture!.ServerAddress}/datagrid", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var grid = _page.Locator(".nt-data-grid", new PageLocatorOptions { Has = _page.Locator("caption", new PageLocatorOptions { HasText = "Provider first window" }) });
        var evenRow = grid.Locator(".nt-data-grid-row-striped-even.nt-data-grid-row-clickable").First;
        var oddRow = grid.Locator(".nt-data-grid-row-striped-odd.nt-data-grid-row-clickable").First;
        var evenCell = evenRow.Locator(".nt-data-grid-cell").First;
        var oddCell = oddRow.Locator(".nt-data-grid-cell").First;
        await evenRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        // Cells animate background-color; read final state colors, not mid-transition values.
        await _page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = ".nt-data-grid-cell { transition: none !important; }" });

        var evenRest = await GetBackgroundAsync(evenCell);
        var oddRest = await GetBackgroundAsync(oddCell);
        evenRest.Should().NotBe(oddRest);

        await oddRow.HoverAsync();
        await Expect(oddCell).Not.ToHaveCSSAsync("background-color", oddRest);
        var hoverColor = await GetBackgroundAsync(oddCell);
        hoverColor.Should().NotBe(evenRest);

        await evenRow.HoverAsync();
        await Expect(evenCell).ToHaveCSSAsync("background-color", hoverColor);
    }

    private static async Task<string> GetBackgroundAsync(ILocator cell) {
        // Retry until the cell is attached and styled, so a re-render between resolve and read cannot return an empty value.
        await Expect(cell).ToHaveCSSAsync("background-color", new Regex("."));
        return await cell.EvaluateAsync<string>("cell => getComputedStyle(cell).backgroundColor");
    }
}
