using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace NTComponents.IntegrationTests.Buttons;

[Collection(PlaywrightE2ECollection.Name)]
public sealed class NTFabMenuState_IntegrationTests : IAsyncLifetime {
    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        await _page.GotoAsync($"{_fixture.ServerAddress}/fab-menu-state");
        await _page.GetByTestId("interactive-renderer").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task ItemClickThatRerendersParent_LeavesMenuClosed() {
        await OpenAsync();

        await _page!.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Save" }).ClickAsync();
        await Expect(_page.GetByTestId("saved-count")).ToHaveTextAsync("1");

        (await IsOpenAsync()).Should().BeFalse();
        await Expect(_page.GetByTestId("menu-state")).ToHaveTextAsync("closed");
    }

    [Fact]
    public async Task OutsideClickThatRerendersParent_LeavesMenuClosed() {
        await OpenAsync();

        await _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Re-render parent" }).ClickAsync();
        await Expect(_page.GetByTestId("outside-clicks")).ToHaveTextAsync("1");

        (await IsOpenAsync()).Should().BeFalse();
        await Expect(_page.GetByTestId("menu-state")).ToHaveTextAsync("closed");
    }

    [Fact]
    public async Task OnExpandedChanged_ReportsBrowserOpenAndClose() {
        await OpenAsync();
        await Expect(_page!.GetByTestId("menu-state")).ToHaveTextAsync("open");

        await _page.Keyboard.PressAsync("Escape");

        await Expect(_page.GetByTestId("menu-state")).ToHaveTextAsync("closed");
        (await IsOpenAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task OpenFromCode_OpensMenuAndReportsIt() {
        await _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open from code" }).ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Menu, new PageGetByRoleOptions { Name = "Actions" })).ToBeVisibleAsync();
        await Expect(_page.GetByTestId("menu-state")).ToHaveTextAsync("open");
    }

    private async Task OpenAsync() {
        await _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Actions" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Menu, new PageGetByRoleOptions { Name = "Actions" })).ToBeVisibleAsync();
    }

    // Waits two frames after the triggering render so a stale reopen would already have happened.
    private async Task<bool> IsOpenAsync() {
        await _page!.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        return await _page.EvaluateAsync<bool>("() => document.querySelector('[role=menu][aria-label=\"Actions\"]').matches(':popover-open')");
    }
}
