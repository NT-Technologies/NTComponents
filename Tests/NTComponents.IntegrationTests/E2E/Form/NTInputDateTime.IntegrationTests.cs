using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Form;

[Collection(PlaywrightE2ECollection.Name)]
public class NTInputDateTime_IntegrationTests : IAsyncLifetime {

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
    public async Task Native_Date_Preserves_Segmented_Year_Entry_Inside_NTForm() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/date-time-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var runtime = _page.GetByTestId("intersect-restriction-runtime");
        await Assertions.Expect(runtime).ToContainTextAsync("interactive", new LocatorAssertionsToContainTextOptions { Timeout = 15000 });

        var input = _page.Locator("#intersect-new-restriction-form").GetByLabel("Start Date");
        await input.PressSequentiallyAsync("01012000", new LocatorPressSequentiallyOptions { Delay = 25 });

        await Assertions.Expect(input).ToHaveValueAsync("2000-01-01");
        await Assertions.Expect(_page.GetByTestId("intersect-new-restriction-form-bound-start-date")).ToContainTextAsync("01/01/2000");

        await input.BlurAsync();

        await Assertions.Expect(input).ToHaveValueAsync("2000-01-01");
        var rootClass = await input.Locator("xpath=ancestor::div[contains(@class, 'nt-input-date-time')][1]").GetAttributeAsync("class");
        rootClass.Should().Contain("nt-modified");
        rootClass.Should().NotContain("nt-invalid");
    }

    [Fact]
    public async Task Native_Date_Markup_Remains_Available_In_Static_Ssr() {
        ArgumentNullException.ThrowIfNull(_fixture);

        using var response = await _fixture.HttpClient.GetAsync("/date-time-render-modes", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        html.Should().Contain("SSR runtime: static");
        html.Should().Contain("id=\"ssr-native-date\"");
        html.Should().Contain("type=\"date\"");
        html.Should().Contain("format=\"MM/dd/yyyy\"");
        html.Should().Contain("data-nt-dtp-native-input=\"true\"");
    }

    [Fact]
    public async Task Invalid_Empty_Field_Prompts_Use_The_Error_Color() {
        ArgumentNullException.ThrowIfNull(_fixture);
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{_fixture.ServerAddress}/validation-render-modes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var form = _page.GetByTestId("server-form");
        await Assertions.Expect(_page.GetByTestId("server-runtime")).ToContainTextAsync("interactive", new LocatorAssertionsToContainTextOptions { Timeout = 15000 });
        await form.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();

        var placeholderField = _page.GetByTestId("server-placeholder-field");
        var dateField = _page.GetByTestId("server-date-mask-field");
        var selectField = _page.GetByTestId("server-select-placeholder-field");
        await Assertions.Expect(placeholderField).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(dateField).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(selectField).ToHaveAttributeAsync("aria-invalid", "true");

        var placeholderColors = await placeholderField.EvaluateAsync<string[]>("""
            element => {
                const root = element.closest('.nt-input');
                const errorText = root?.querySelector('.nt-input-error-text');
                return [getComputedStyle(element, '::placeholder').color, errorText ? getComputedStyle(errorText).color : ''];
            }
            """);
        var dateMaskColors = await dateField.EvaluateAsync<string[]>("""
            element => {
                const root = element.closest('.nt-input');
                const errorText = root?.querySelector('.nt-input-error-text');
                return [
                    getComputedStyle(element, '::-webkit-datetime-edit').webkitTextFillColor,
                    errorText ? getComputedStyle(errorText).color : ''
                ];
            }
            """);
        var selectPlaceholderColors = await selectField.EvaluateAsync<string[]>("""
            element => {
                const root = element.closest('.nt-input');
                const errorText = root?.querySelector('.nt-input-error-text');
                return [getComputedStyle(element).webkitTextFillColor, errorText ? getComputedStyle(errorText).color : ''];
            }
            """);

        placeholderColors[0].Should().Be(placeholderColors[1]);
        dateMaskColors[0].Should().Be(dateMaskColors[1]);
        selectPlaceholderColors[0].Should().Be(selectPlaceholderColors[1]);
    }
}
