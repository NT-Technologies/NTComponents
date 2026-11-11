using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Buttons;

[Collection(PlaywrightE2ECollection.Name)]
public class NTFabOffsets_IntegrationTests : IAsyncLifetime {
    private static readonly string[] UpperFabs = [
        ".nt-fab-button-placement-upper-right", ".nt-fab-button-placement-upper-left",
        ".nt-fab-menu-placement-upper-right", ".nt-fab-menu-placement-upper-left"
    ];
    private static readonly string[] LowerFabs = [
        ".nt-fab-button-placement-lower-right", ".nt-fab-button-placement-lower-left",
        ".nt-fab-menu-placement-lower-right", ".nt-fab-menu-placement-lower-left"
    ];

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
    [InlineData(390, 16)]
    [InlineData(900, 24)]
    public async Task HeaderAndFooterVariants_FabsClearRegionsAndReturnToBaseOffsetsWhenRemoved(int width, int gap) {
        ArgumentNullException.ThrowIfNull(_fixture);
        var page = _fixture.Page;
        await page.SetViewportSizeAsync(width, 844);
        await page.GotoAsync($"{_fixture.ServerAddress}/fab-offsets", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(page.Locator(".nt-fab-menu")).ToHaveCountAsync(4);

        var headerBox = await page.Locator(".nt-layout > .nt-header").First.BoundingBoxAsync();
        var footerBox = await page.Locator(".nt-layout > .nt-footer").First.BoundingBoxAsync();
        ArgumentNullException.ThrowIfNull(headerBox);
        ArgumentNullException.ThrowIfNull(footerBox);

        await AssertOffsetsAsync(page, headerBox.Height + gap, footerBox.Height + gap);

        foreach (var selector in UpperFabs) {
            var box = await page.Locator(selector).BoundingBoxAsync();
            ArgumentNullException.ThrowIfNull(box);
            box.Y.Should().BeGreaterThanOrEqualTo(headerBox.Y + headerBox.Height + gap - 1, selector);
        }

        foreach (var selector in LowerFabs) {
            var box = await page.Locator(selector).BoundingBoxAsync();
            ArgumentNullException.ThrowIfNull(box);
            (box.Y + box.Height).Should().BeLessThanOrEqualTo(footerBox.Y - gap + 1, selector);
        }

        await page.EvaluateAsync("""
            () => {
                document.querySelector('.nt-layout > .nt-header').classList.remove('nt-header');
                document.querySelector('.nt-layout > .nt-footer').classList.remove('nt-footer');
            }
            """);
        await AssertOffsetsAsync(page, gap, gap);
    }

    private static async Task AssertOffsetsAsync(IPage page, double top, double bottom) {
        foreach (var selector in UpperFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).top)"))
                .Should().BeApproximately(top, 1, selector);
        }

        foreach (var selector in LowerFabs) {
            (await page.Locator(selector).EvaluateAsync<double>("element => Number.parseFloat(getComputedStyle(element).bottom)"))
                .Should().BeApproximately(bottom, 1, selector);
        }
    }
}
