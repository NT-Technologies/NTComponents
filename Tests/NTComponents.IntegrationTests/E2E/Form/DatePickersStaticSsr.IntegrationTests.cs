using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Form;

[Collection(PlaywrightE2ECollection.Name)]
public class DatePickersStaticSsr_IntegrationTests : IAsyncLifetime {

    private PlaywrightFixture? _fixture;
    private IPage? _page;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture != null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Pickers_Fill_Fields_And_Post_Values_Without_A_Blazor_Circuit() {
        var form = await OpenFormAsync();
        var month = DateTime.Today.ToString("yyyy-MM");

        await form.Locator("[data-nt-input-date-range-input]").ClickAsync();
        var popover = form.Locator("[data-nt-input-date-range-popover]");
        await Assertions.Expect(popover).ToBeVisibleAsync();
        await popover.Locator($"button.day[data-date='{month}-10']").ClickAsync();
        await popover.Locator($"button.day[data-date='{month}-20']").ClickAsync();

        await Assertions.Expect(popover).ToBeHiddenAsync();
        await Assertions.Expect(form.Locator("[data-nt-input-date-range-start]")).ToHaveValueAsync($"{month}-10");
        await Assertions.Expect(form.Locator("[data-nt-input-date-range-end]")).ToHaveValueAsync($"{month}-20");
        await Assertions.Expect(form.Locator("[data-nt-input-date-range-input]")).Not.ToHaveValueAsync("");

        await form.Locator("[data-nt-dtp-trigger][data-nt-dtp-target='date-ssr-form-departure-picker']").ClickAsync();
        var picker = form.Locator("#date-ssr-form-departure-picker");
        await picker.Locator("[data-nt-dtp-day='12']:not(.nt-dtp-outside-month)").ClickAsync();

        await Assertions.Expect(form.Locator("#date-ssr-form-departure")).ToHaveValueAsync($"{month}-12");
        await Assertions.Expect(picker).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("nt-dtp-open"));

        await form.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();

        await Assertions.Expect(_page!.GetByTestId("date-ssr-form-result")).ToHaveTextAsync($"{month}-10 to {month}-20, departure {month}-12");
    }

    [Fact]
    public async Task Server_Validation_Error_Renders_On_The_Range_Field_After_A_Static_Post() {
        // A server-only rule on a fully filled form, so framework binding and client-side validation differences across
        // .NET versions cannot intercept the post.
        var form = await OpenFormAsync();
        var month = DateTime.Today.ToString("yyyy-MM");

        await form.Locator("[data-nt-input-date-range-input]").ClickAsync();
        var popover = form.Locator("[data-nt-input-date-range-popover]");
        await popover.Locator($"button.day[data-date='{month}-01']").ClickAsync();
        await popover.Locator($"button.day[data-date='{month}-28']").ClickAsync();
        await form.Locator("[data-nt-dtp-trigger][data-nt-dtp-target='date-ssr-form-departure-picker']").ClickAsync();
        await form.Locator("#date-ssr-form-departure-picker [data-nt-dtp-day='12']:not(.nt-dtp-outside-month)").ClickAsync();

        await form.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();

        var rangeInput = _page!.GetByTestId("date-ssr-form").Locator("[data-nt-input-date-range-input]");
        await Assertions.Expect(_page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Trips can be at most 14 days.");
        await Assertions.Expect(rangeInput).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(_page.GetByTestId("date-ssr-form").Locator("[data-nt-input-date-range-end]")).ToHaveValueAsync($"{month}-28");
        await Assertions.Expect(_page.GetByTestId("date-ssr-form-result")).ToHaveTextAsync("");
    }

    private async Task<ILocator> OpenFormAsync() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/date-ssr-form", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(_page.GetByTestId("date-ssr-form-runtime")).ToHaveTextAsync("Runtime: static");
        return _page.GetByTestId("date-ssr-form");
    }
}
