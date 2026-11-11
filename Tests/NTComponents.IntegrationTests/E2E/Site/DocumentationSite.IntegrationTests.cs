using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;

namespace NTComponents.IntegrationTests.Site;

/// <summary>
///     Browser-level coverage for every NTComponents.Site component page and its examples.
/// </summary>
public sealed class DocumentationSite_IntegrationTests : IAsyncLifetime {
    private const int ExpectedComponentTypeCount = 96;
    private const int ExpectedRootRouteCount = 69;
    private const int ExpectedChartRootRouteCount = 11;
    // Chart components live in their own assembly; these are documented on the NTChart and NTRadarSeries pages.
    private static readonly string[] ExpectedChartDependentComponentNames = ["NTLegend", "NTTooltip", "NTXAxisOptions", "NTYAxisOptions", "NTRadialAxisOptions"];
    private static readonly string[] ExpectedDependentComponentNames = [
        "NTAccordionItem",
        "NTAutocompleteOption",
        "NTAutocompleteOptionGroup",
        "NTBody",
        "NTButtonGroupItem",
        "NTCarouselItem",
        "NTFabMenuAnchorItem",
        "NTFabMenuButtonItem",
        "NTFileUploadItem",
        "NTFooter",
        "NTHeader",
        "NTInputRadio",
        "NTMenuAnchorItem",
        "NTMenuButtonItem",
        "NTMenuDividerItem",
        "NTMenuLabelItem",
        "NTMenuSubMenuItem",
        "NTNavigationRailGroup",
        "NTNavigationRailItem",
        "NTNavigationRailSectionHeader",
        "NTPageScript",
        "NTPopoutTab",
        "NTPropertyColumn",
        "NTTab",
        "NTTemplateColumn",
        "NTWizardFormStep",
        "NTWizardStep"
    ];
    private readonly ConcurrentQueue<BrowserDiagnostic> _browserDiagnostics = new();
    private WebApplication? _application;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IPlaywright? _playwright;
    private string _activeRoute = "/components";
    private string _baseUrl = default!;

    public async ValueTask InitializeAsync() {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var siteRoot = Path.Combine(repositoryRoot, "NTComponents.Site");
        var targetFramework = $"net{Environment.Version.Major}.0";
        var staticWebAssetsManifest = Path.Combine(siteRoot, "bin", configuration, targetFramework, "NTComponents.Site.staticwebassets.runtime.json");
        var staticWebAssetsEndpoints = Path.Combine(siteRoot, "obj", configuration, targetFramework, "staticwebassets.build.endpoints.json");
        if (!File.Exists(staticWebAssetsManifest)) {
            throw new FileNotFoundException($"Build NTComponents.Site before running its browser tests. Expected static-web-assets manifest: {staticWebAssetsManifest}");
        }
        if (!File.Exists(staticWebAssetsEndpoints)) {
            throw new FileNotFoundException($"Build NTComponents.Site before running its browser tests. Expected static-asset endpoints: {staticWebAssetsEndpoints}");
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = siteRoot
        });
        builder.Configuration[WebHostDefaults.StaticWebAssetsKey] = staticWebAssetsManifest;
        builder.WebHost.UseStaticWebAssets();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _application = builder.Build();
        _application.UseStaticFiles();
        _application.MapGet("/_framework/dotnet-browser-tools/hot-reload-settings.json", () => Results.Json(new { hotReload = false }));
        _application.MapStaticAssets(staticWebAssetsEndpoints);
        _application.MapFallbackToFile("index.html");
        await _application.StartAsync();

        var server = _application.Services.GetRequiredService<IServer>();
        _baseUrl = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The documentation test server did not publish an address.");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions {
            ExtraHTTPHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Accept-Encoding"] = "identity" }
        });
        _page = await _context.NewPageAsync();
        _page.Console += (_, message) => {
            if (string.Equals(message.Type, "error", StringComparison.Ordinal)) {
                _browserDiagnostics.Enqueue(new(_activeRoute, "console", $"{message.Text} ({message.Location})"));
            }
        };
        _page.PageError += (_, error) => _browserDiagnostics.Enqueue(new(_activeRoute, "pageerror", error));
    }

    public async ValueTask DisposeAsync() {
        if (_context is not null) {
            await _context.CloseAsync();
        }

        if (_browser is not null) {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_application is not null) {
            await _application.StopAsync();
            await _application.DisposeAsync();
        }
    }

    [Fact]
    public async Task ComponentRoutes_RenderExamplesWithSource_AndRepresentEveryPublicComponent() {
        ArgumentNullException.ThrowIfNull(_page);

        // Arrange
        await _page.GotoAsync($"{_baseUrl}/components", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        try {
            await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "NT components", Exact = true }).WaitForAsync();
        }
        catch (TimeoutException exception) {
            var diagnostics = string.Join("\n", _browserDiagnostics.Select(diagnostic => $"{diagnostic.Kind}: {FirstLine(diagnostic.Message)}"));
            throw new InvalidOperationException($"The documentation application did not start at {_page.Url}. Browser diagnostics:\n{diagnostics}", exception);
        }
        var indexedRoutes = await _page.Locator("a[href^='/components/']").EvaluateAllAsync<string[]>(
            "links => links.map(link => link.getAttribute('href')).filter(Boolean)");
        var routes = indexedRoutes.Distinct(StringComparer.Ordinal).OrderBy(route => route, StringComparer.Ordinal).ToArray();
        routes.Should().HaveCount(ExpectedRootRouteCount + ExpectedChartRootRouteCount, "the component index should link to exactly the expected root component pages");

        var exportedComponentTypes = typeof(NTButton).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .ToArray();
        var expectedComponentNames = exportedComponentTypes
            .Where(type => type != typeof(NTBadge) && !type.IsDefined(typeof(ObsoleteAttribute), inherit: false))
            .Select(type => RemoveGenericArity(type.Name))
            .ToHashSet(StringComparer.Ordinal);
        expectedComponentNames.Should().HaveCount(ExpectedComponentTypeCount, "the browser coverage contract should be updated intentionally when the public NT component surface changes");
        var expectedDependentComponentNames = ExpectedDependentComponentNames.Intersect(expectedComponentNames, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var expectedRootComponentNames = expectedComponentNames.Except(expectedDependentComponentNames, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        expectedRootComponentNames.Should().HaveCount(ExpectedRootRouteCount, "every current exported component should be classified as either a root route or a composed dependent");
        var expectedChartRootComponentNames = typeof(NTComponents.Charts.Core.NTChart<>).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .Select(type => RemoveGenericArity(type.Name))
            .Except(ExpectedChartDependentComponentNames, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        expectedChartRootComponentNames.Should().HaveCount(ExpectedChartRootRouteCount, "every exported chart component should be classified as either a root route or a composed dependent");
        expectedRootComponentNames.UnionWith(expectedChartRootComponentNames);

        var failures = new List<string>();
        var rootComponentNames = new List<string>();

        // Act
        foreach (var route in routes) {
            _activeRoute = route;
            var diagnosticCountBeforeNavigation = _browserDiagnostics.Count;

            await _page.EvaluateAsync("href => document.querySelector(`a[href=\"${href}\"]`)?.click()", route);
            await _page.WaitForURLAsync($"**{route}");

            var heading = _page.Locator("main h1").First;
            await heading.WaitForAsync();
            var rootComponentName = RemoveGenericArity((await heading.InnerTextAsync()).Trim());
            rootComponentNames.Add(rootComponentName);
            var expectedRoute = $"/components/{rootComponentName.ToLowerInvariant()}";
            if (!string.Equals(route, expectedRoute, StringComparison.Ordinal)) {
                failures.Add($"{route}: root heading {rootComponentName} belongs at {expectedRoute}.");
            }

            var examples = _page.Locator("#examples .docs-example");
            if (!await TryWaitForAttachedAsync(examples.First)) {
                failures.Add($"{route}: no .docs-example rendered.");
                continue;
            }

            await WaitForRenderAsync(_page);
            var failedExamples = await _page.Locator(".docs-example-failure").AllInnerTextsAsync();
            if (failedExamples.Count > 0) {
                failures.Add($"{route}: failing examples: {string.Join(" | ", failedExamples)}");
            }

            var exampleCount = await examples.CountAsync();
            var sources = await _page.Locator(".docs-example-code code").EvaluateAllAsync<string[]>("codes => codes.map(code => code.textContent ?? '')");
            if (sources.Length != exampleCount || sources.Any(string.IsNullOrWhiteSpace)) {
                failures.Add($"{route}: every example needs non-empty Razor source ({sources.Length} sources for {exampleCount} examples).");
            }

            var expectedNavigation = new List<string> { "Overview", "Examples" };
            expectedNavigation.AddRange(await examples.Locator(":scope > h3").AllInnerTextsAsync());
            expectedNavigation.Add("API");
            if (await _page.Locator("#dependent-components").CountAsync() > 0) {
                expectedNavigation.Add("Dependent components");
            }

            try {
                await Assertions.Expect(_page.Locator(".nt-container-view-quick-nav-list a:not(.docs-example-preview a)")).ToHaveTextAsync([.. expectedNavigation]);
            }
            catch (PlaywrightException exception) {
                failures.Add($"{route}: On this page should list only {string.Join(", ", expectedNavigation)}. {FirstLine(exception.Message)}");
            }

            await _page.SetViewportSizeAsync(390, 844);
            await WaitForRenderAsync(_page);
            var mobileOverflow = await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth");
            if (mobileOverflow > 1) {
                failures.Add($"{route}: mobile page overflowed horizontally by {mobileOverflow}px.");
            }
            await _page.SetViewportSizeAsync(1280, 720);
            await WaitForRenderAsync(_page);
            foreach (var diagnostic in _browserDiagnostics.Skip(diagnosticCountBeforeNavigation)) {
                failures.Add($"{diagnostic.Route}: browser {diagnostic.Kind}: {FirstLine(diagnostic.Message)}");
            }
        }

        var duplicateRootComponentNames = rootComponentNames.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (duplicateRootComponentNames.Length > 0) {
            failures.Add($"Components covered by more than one root route: {string.Join(", ", duplicateRootComponentNames)}");
        }

        var unexpectedRootComponentNames = rootComponentNames.Except(expectedRootComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (unexpectedRootComponentNames.Length > 0) {
            failures.Add($"Unexpected root component pages: {string.Join(", ", unexpectedRootComponentNames)}");
        }

        var missingRootComponentNames = expectedRootComponentNames.Except(rootComponentNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (missingRootComponentNames.Length > 0) {
            failures.Add($"Expected root component pages missing from the component index: {string.Join(", ", missingRootComponentNames)}");
        }

        // Assert
        failures.Should().BeEmpty("every component page should render working examples in a real browser:\n{0}", string.Join("\n", failures.Distinct(StringComparer.Ordinal)));
    }

    [Fact]
    public async Task MaterialThemeCreator_GeneratesSixSchemes_AndRecolorsTheSiteOnlyAfterEdits() {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.GotoAsync($"{_baseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.Locator(".docs-home").WaitForAsync();
        var originalPrimary = await RootColorAsync("--tnt-color-primary");

        _activeRoute = "/tools/material-theme";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Material 3 theme creator", Exact = true }).WaitForAsync();

        var generatedOutput = _page.Locator("[aria-label='Generated theme files']");
        await generatedOutput.WaitForAsync();
        (await generatedOutput.Locator("details").CountAsync()).Should().Be(6);
        await generatedOutput.Locator("summary").First.ClickAsync();
        var lightTheme = await generatedOutput.Locator("details").First.Locator("pre code").InnerTextAsync();
        lightTheme.Should().StartWith(":root {");
        lightTheme.Should().Contain("--tnt-color-primary: rgb(101 85 143);");
        lightTheme.Should().Contain("--tnt-color-on-assert-container:");
        lightTheme.Split("--tnt-color-", StringSplitOptions.None).Should().HaveCount(66, "the file should contain the 49 Material system roles and four 4-role NTComponents colors");
        var defaultExtendedColors = new[] {
            (Id: "success", Color: "#00c853"),
            (Id: "info", Color: "#0091ea"),
            (Id: "warning", Color: "#ffab00"),
            (Id: "assert", Color: "#aa00ff")
        };
        foreach (var color in defaultExtendedColors) {
            await Assertions.Expect(_page.Locator($"#theme-{color.Id}")).ToHaveValueAsync(color.Color);
        }

        var usageGuide = _page.GetByTestId("theme-usage-guide");
        await usageGuide.WaitForAsync();
        (await usageGuide.Locator(".theme-style-card").CountAsync()).Should().Be(9);
        (await usageGuide.Locator(".theme-role-card").CountAsync()).Should().Be(7);
        (await usageGuide.InnerTextAsync()).Should().Contain("Custom palette overrides always win");

        var preview = _page.Locator("[data-testid='theme-preview']");
        var previewButton = preview.Locator(".nt-button-filled");
        var previewHeading = preview.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "See the theme in NTComponents", Exact = true });
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(101, 85, 143)");
        (await previewHeading.EvaluateAsync<string>("element => getComputedStyle(element).color")).Should().Be("rgb(101, 85, 143)");
        (await RootColorAsync("--tnt-color-primary")).Should().Be(originalPrimary, "opening the theme creator must not recolor the site");
        await _page.Locator("#theme-style").SelectOptionAsync("vibrant");
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(111, 25, 255)");
        await WaitForRootColorAsync("--nt-color-primary", "rgb(111, 25, 255)");
        await _page.Locator("#theme-style").SelectOptionAsync("tonal-spot");
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(101, 85, 143)");
        await WaitForRootColorAsync("--nt-color-primary", "rgb(101, 85, 143)");

        await _page.Locator(".theme-advanced summary").ClickAsync();
        (await _page.Locator(".theme-advanced input[type='checkbox']").CountAsync()).Should().Be(0);
        var paletteOverrides = new[] {
            (Id: "secondary", Color: "#005a80", Token: "--tnt-color-secondary"),
            (Id: "tertiary", Color: "#805200", Token: "--tnt-color-tertiary"),
            (Id: "neutral", Color: "#44505a", Token: "--tnt-color-surface"),
            (Id: "neutral-variant", Color: "#5b5f6a", Token: "--tnt-color-outline"),
            (Id: "error", Color: "#9f2030", Token: "--tnt-color-error")
        };
        // Role pickers show the rendered role color. Surface pickers show their palette's key color, because surface tones are near white or black.
        string[] surfacePalettes = ["neutral", "neutral-variant"];
        var paletteDisplays = paletteOverrides
            .Where(palette => !surfacePalettes.Contains(palette.Id))
            .Select(palette => (palette.Id, palette.Token))
            .Prepend((Id: "primary", Token: "--tnt-color-primary"))
            .ToArray();
        foreach (var palette in paletteDisplays) {
            await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
        }

        var overriddenProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var palette in paletteOverrides) {
            var previousValue = await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", palette.Token);
            var picker = _page.Locator($"#theme-palette-{palette.Id}");
            await Assertions.Expect(picker).ToBeEnabledAsync();
            // While the color dialog is open (input without change), the preview updates but never moves the picker being dragged.
            await picker.EvaluateAsync(
                """
                (element, color) => {
                    element.value = color;
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                }
                """,
                palette.Color);
            await _page.WaitForFunctionAsync(
                "args => document.querySelector('[data-testid=theme-preview]').style.getPropertyValue(args.token) !== args.previousValue",
                new { token = palette.Token, previousValue });
            await WaitForRenderAsync(_page);
            await Assertions.Expect(picker).ToHaveValueAsync(palette.Color);
            await picker.EvaluateAsync("element => element.dispatchEvent(new Event('change', { bubbles: true }))");
            if (surfacePalettes.Contains(palette.Id)) {
                // A palette seeded from a color keeps that color as its key color.
                await Assertions.Expect(picker).ToHaveValueAsync(palette.Color);
            }
            else {
                await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
            }
            overriddenProperties[palette.Token] = await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", palette.Token);
        }

        var downloadLink = _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Download generated themes", Exact = true });
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-theme-6750a4.zip");
        var primaryPicker = _page.Locator("#theme-primary");
        await primaryPicker.EvaluateAsync(
            """
            element => {
                for (const color of ['#00515c', '#005f6b', '#006874']) {
                    element.value = color;
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                }
            }
            """);
        await _page.WaitForFunctionAsync("expected => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor === expected", "rgb(0, 104, 116)");
        await WaitForPalettePickerToMatchPreviewAsync("primary", "--tnt-color-primary");
        (await previewHeading.EvaluateAsync<string>("element => getComputedStyle(element).color")).Should().Be("rgb(0, 104, 116)", "the preview heading should consume the live primary theme variable");
        await Assertions.Expect(primaryPicker.Locator("xpath=..").Locator("[data-theme-color-output]")).ToHaveTextAsync("#006874");
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-theme-6750a4.zip", "live input should not rebuild the ZIP");
        await WaitForRootColorAsync("--nt-color-primary", "rgb(0, 104, 116)");
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Restore site theme", Exact = true }).ClickAsync();
        await WaitForRootColorAsync("--tnt-color-primary", originalPrimary);

        await primaryPicker.EvaluateAsync("element => element.dispatchEvent(new Event('change', { bubbles: true }))");
        await Assertions.Expect(downloadLink).ToHaveAttributeAsync("download", "ntcomponents-theme-006874.zip");
        foreach (var property in overriddenProperties) {
            (await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", property.Key)).Should().Be(property.Value, $"the custom {property.Key} palette should survive a source-color change");
        }

        var previousPrimaryOverride = await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')");
        var primaryOverridePicker = _page.Locator("#theme-palette-primary");
        await primaryOverridePicker.EvaluateAsync(
            """
            (element, color) => {
                element.value = color;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            "#7b2cbf");
        await _page.WaitForFunctionAsync(
            "previous => document.querySelector('[data-testid=theme-preview]').style.getPropertyValue('--tnt-color-primary') !== previous",
            previousPrimaryOverride);
        await WaitForPalettePickerToMatchPreviewAsync("primary", "--tnt-color-primary");
        overriddenProperties["--tnt-color-primary"] = await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')");

        await primaryPicker.EvaluateAsync(
            """
            (element, color) => {
                element.value = color;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }
            """,
            "#b3261e");
        await Assertions.Expect(downloadLink).ToHaveAttributeAsync("download", "ntcomponents-theme-b3261e.zip");
        (await preview.EvaluateAsync<string>("element => element.style.getPropertyValue('--tnt-color-primary')")).Should().Be(overriddenProperties["--tnt-color-primary"], "the custom primary palette should survive a source-color change");

        await _page.Locator("#theme-style").SelectOptionAsync("vibrant");
        await Assertions.Expect(usageGuide.Locator(".theme-style-card.selected h4")).ToHaveTextAsync("Vibrant");
        foreach (var property in overriddenProperties) {
            (await preview.EvaluateAsync<string>("(element, token) => element.style.getPropertyValue(token)", property.Key)).Should().Be(property.Value, $"the custom {property.Key} palette should survive a style change");
        }
        await _page.Locator("#theme-style").SelectOptionAsync("tonal-spot");
        await Assertions.Expect(usageGuide.Locator(".theme-style-card.selected h4")).ToHaveTextAsync("Tonal spot");

        var lightSurface = await preview.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
        var lightPrimary = await previewButton.EvaluateAsync<string>("element => getComputedStyle(element, '::after').backgroundColor");
        await _page.Locator("#theme-preview-mode").SelectOptionAsync("dark");
        await _page.WaitForFunctionAsync("previous => getComputedStyle(document.querySelector('[data-testid=theme-preview] .nt-button-filled'), '::after').backgroundColor !== previous", lightPrimary);
        foreach (var palette in paletteDisplays) {
            await WaitForPalettePickerToMatchPreviewAsync(palette.Id, palette.Token);
        }
        (await preview.EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")).Should().NotBe(lightSurface);
        (await previewButton.EvaluateAsync<string>("element => getComputedStyle(element, '::after').backgroundColor"))
            .Should().NotBe(lightPrimary, "the representative NTButton should consume the selected dark preview token");
        // The site follows its own light theme with the edited colors, independent of the preview's mode.
        await WaitForRootColorAsync("--nt-color-primary", lightPrimary);

        var downloadUrl = await downloadLink.GetAttributeAsync("href");
        downloadUrl.Should().StartWith("data:application/zip;base64,");
        using (var archiveStream = new MemoryStream(Convert.FromBase64String(downloadUrl![(downloadUrl.IndexOf(',') + 1)..])))
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read)) {
            archive.Entries.Should().HaveCount(6);
            archive.Entries.Should().OnlyContain(entry => entry.FullName.StartsWith("Themes/", StringComparison.Ordinal));
        }

        await _page.Locator("a[href='/']").First.ClickAsync();
        await _page.WaitForURLAsync($"{_baseUrl}/");
        // The edited colors stay applied across in-app navigation until a reload or "Restore site theme".
        await WaitForRootColorAsync("--nt-color-primary", lightPrimary);
        _browserDiagnostics.Where(diagnostic => string.Equals(diagnostic.Route, _activeRoute, StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public async Task MaterialThemeConverter_ConvertsSixGeneratedSchemes_AndBuildsReadyToCopyArchive() {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = "/tools/material-theme";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Material 3 theme creator", Exact = true }).WaitForAsync();

        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"ntcomponents-theme-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDirectory);
        try {
            var files = new[] {
                ("light.css", ".light"),
                ("light-mc.css", ".light-medium-contrast"),
                ("light-hc.css", ".light-high-contrast"),
                ("dark.css", ".dark"),
                ("dark-mc.css", ".dark-medium-contrast"),
                ("dark-hc.css", ".dark-high-contrast")
            };
            foreach (var (name, selector) in files) {
                await File.WriteAllTextAsync(Path.Combine(sourceDirectory, name), CreateThemeSource(selector), Encoding.UTF8, TestContext.Current.CancellationToken);
            }

            await _page.Locator("#material-theme-files").SetInputFilesAsync(sourceDirectory);
            await _page.Locator(".theme-import .theme-message").WaitForAsync();
        }
        finally {
            Directory.Delete(sourceDirectory, recursive: true);
        }

        var conversionErrors = await _page.Locator(".theme-error").AllInnerTextsAsync();
        conversionErrors.Should().BeEmpty("the selected Material Theme Builder folder should convert successfully: {0}", string.Join(" | ", conversionErrors));
        await _page.GetByText("Six converted theme files are ready", new PageGetByTextOptions { Exact = true }).WaitForAsync();
        var output = _page.Locator("[aria-label='Converted theme files']");
        (await output.Locator("details").CountAsync()).Should().Be(6);
        await output.Locator("summary").First.ClickAsync();
        var preview = await output.Locator("details").First.Locator("pre code").InnerTextAsync();
        preview.Should().StartWith(":root {");
        preview.Should().Contain("--tnt-color-primary: rgb(1 2 3);");
        preview.Should().Contain("--tnt-color-on-success-container: rgb(4 5 6);");
        preview.Should().NotContain("--md-");

        var downloadLink = _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Download converted themes", Exact = true });
        (await downloadLink.GetAttributeAsync("download")).Should().Be("ntcomponents-themes.zip");
        var downloadUrl = await downloadLink.GetAttributeAsync("href");
        downloadUrl.Should().StartWith("data:application/zip;base64,");
        var archiveBytes = Convert.FromBase64String(downloadUrl![(downloadUrl.IndexOf(',') + 1)..]);
        using var archiveStream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        archive.Entries.Select(entry => entry.FullName).Should().Equal(
            "Themes/light.css",
            "Themes/light-mc.css",
            "Themes/light-hc.css",
            "Themes/dark.css",
            "Themes/dark-mc.css",
            "Themes/dark-hc.css");

        using var lightReader = new StreamReader(archive.GetEntry("Themes/light.css")!.Open());
        (await lightReader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(preview.ReplaceLineEndings("\r\n"));
        _browserDiagnostics.Where(diagnostic => string.Equals(diagnostic.Route, _activeRoute, StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public async Task GettingStarted_RendersSetupGuide_AndComponentsStartCollapsed() {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = "/";
        await _page.GotoAsync(_baseUrl);
        var navigation = _page.GetByRole(AriaRole.Navigation, new PageGetByRoleOptions { Name = "Documentation navigation", Exact = true });
        var components = navigation.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Components", Exact = true });
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        var panel = navigation.Locator(".nt-navigation-rail-group-panel[aria-label='Components destinations']");
        await Assertions.Expect(panel).ToHaveAttributeAsync("inert", "");
        (await panel.EvaluateAsync<double>("element => element.getBoundingClientRect().height")).Should().BeLessThanOrEqualTo(1);
        await components.ClickAsync();
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(navigation.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "All components", Exact = true })).ToBeVisibleAsync();
        await components.ClickAsync();
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(panel).ToHaveAttributeAsync("inert", "");

        _activeRoute = "/getting-started";
        await navigation.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Getting started", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync("**/getting-started");
        await Assertions.Expect(_page.Locator("main h1")).ToHaveTextAsync("Getting started");
        (await _page.Locator("main h2").CountAsync()).Should().Be(10);
        var guideCode = string.Join("\n", await _page.Locator("main pre code").AllTextContentsAsync());
        guideCode.Should().Contain("dotnet add package NTComponents").And.Contain("builder.Services.AddNTServices();")
            .And.Contain("<NTHeadDependencies />").And.Contain("<HeadContent>").And.Contain("<NTToast />")
            .And.Contain("@page \"/welcome\"").And.Contain("light-hc.css").And.Contain("dark-hc.css")
            .And.Contain("dotnet add package NTComponents.Charts").And.Contain("<WasmBuildNative>true</WasmBuildNative>");
        await Assertions.Expect(_page.Locator("main a[href='/tools/material-theme']")).ToBeVisibleAsync();
        await _page.ReloadAsync();
        await Assertions.Expect(_page.Locator("main h1")).ToHaveTextAsync("Getting started");
        await Assertions.Expect(components).ToHaveAttributeAsync("aria-expanded", "false");
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-getting-started-desktop.png") });
        await _page.SetViewportSizeAsync(390, 844);
        await WaitForRenderAsync(_page);
        await navigation.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Collapse navigation rail", Exact = true }).ClickAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Expand navigation rail", Exact = true })).ToBeVisibleAsync();
        await _page.EvaluateAsync("() => Promise.allSettled(document.getAnimations().filter(animation => animation.effect?.getTiming().iterations !== Infinity).map(animation => animation.finished))");
        (await _page.Locator("main h1").BoundingBoxAsync())!.X.Should().BeGreaterThanOrEqualTo(0);
        (await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth")).Should().BeLessThanOrEqualTo(1);
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-getting-started-mobile.png") });
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task HeaderThemeSelector_ChangesAndPersistsTheme_AndFitsMobile() {
        ArgumentNullException.ThrowIfNull(_page);
        await OpenComponentPageAsync("ntbutton");
        var selector = _page.Locator(".docs-topbar nt-theme-toggle select");
        await Assertions.Expect(selector).ToBeVisibleAsync();
        (await selector.Locator("option").AllTextContentsAsync()).Should().Equal("Light", "Dark", "System");

        var activeTheme = _page.Locator("link[data-nt-theme][data-nt-theme-loaded='true']");
        await selector.SelectOptionAsync("LIGHT-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/light\\.css$"));
        var lightSurface = await RootColorAsync("--tnt-color-surface");
        await selector.SelectOptionAsync("DARK-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/dark\\.css$"));
        (await RootColorAsync("--tnt-color-surface")).Should().NotBe(lightSurface);
        (await _page.EvaluateAsync<string>("localStorage.getItem('NTComponentsStoredThemeKey')")).Should().Be("DARK");
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-header-theme-desktop.png") });

        await _page.ReloadAsync();
        await _page.Locator(".docs-topbar nt-theme-toggle select").WaitForAsync();
        await Assertions.Expect(selector).ToHaveValueAsync("DARK-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/dark\\.css$"));
        await _page.SetViewportSizeAsync(390, 844);
        var github = _page.Locator(".docs-github-link");
        await Assertions.Expect(github).ToBeVisibleAsync();
        await Assertions.Expect(selector).ToBeVisibleAsync();
        var githubBounds = (await github.BoundingBoxAsync())!;
        var selectorBounds = (await selector.BoundingBoxAsync())!;
        selectorBounds.X.Should().BeGreaterThanOrEqualTo(githubBounds.X + githubBounds.Width);
        (await _page.EvaluateAsync<int>("Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth")).Should().BeLessThanOrEqualTo(1);
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(FindRepositoryRoot(), "artifacts", "site-header-theme-mobile.png") });

        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await selector.SelectOptionAsync("SYSTEM-DEFAULT");
        await Assertions.Expect(activeTheme).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("/Themes/light\\.css$"));
        (await _page.EvaluateAsync<string>("localStorage.getItem('NTComponentsStoredThemeKey')")).Should().Be("SYSTEM");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task AccordionDemo_LimitToOneExpanded_KeepsSingleItemOpen() {
        var example = await OpenExampleAsync("ntaccordion", "AccordionLimitToOneExample");

        var items = example.Locator("details.nt-accordion-item");
        (await items.EvaluateAllAsync<string?[]>("items => items.map(item => item.getAttribute('name'))")).Should().OnlyContain(name => name == "faq-single");
        (await items.EvaluateAllAsync<bool[]>("items => items.map(item => item.open)")).Should().Equal(true, false, false);

        await items.Nth(1).Locator("summary").ClickAsync();
        await Assertions.Expect(items.Nth(1)).ToHaveAttributeAsync("open", "");
        (await items.EvaluateAllAsync<bool[]>("items => items.map(item => item.open)")).Should().Equal(false, true, false);

        await items.Nth(2).Locator("summary").ClickAsync();
        await Assertions.Expect(items.Nth(2)).ToHaveAttributeAsync("open", "");
        (await items.EvaluateAllAsync<bool[]>("items => items.map(item => item.open)")).Should().Equal(false, false, true);
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DialogDemos_OpenAndCloseWithTheChosenResult() {
        var example = await OpenExampleAsync("ntdialog", "DialogBasicExample");
        var dialog = _page!.Locator("dialog#dialog-basic-example[open]");
        var result = example.Locator("output");
        await Assertions.Expect(result).ToHaveTextAsync("Not opened");

        await example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Review changes", Exact = true }).ClickAsync();
        await dialog.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = "Publish settings update?", Exact = true }).WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Cancel", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(result).ToHaveTextAsync("cancel");

        await example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Review changes", Exact = true }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Confirm", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await Assertions.Expect(result).ToHaveTextAsync("confirm");

        var dismiss = Example("DialogDismissExample");
        await dismiss.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Open reminder", Exact = true }).ClickAsync();
        await _page.Locator("dialog#dialog-dismiss-example[open]").WaitForAsync();
        await _page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(_page.Locator("dialog#dialog-dismiss-example[open]")).ToHaveCountAsync(0);
        await Assertions.Expect(dismiss.Locator("output")).ToHaveTextAsync("1");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MenuDemo_TriggerOpensTheMenu_AndChoosingAnItemUpdatesTheOutput() {
        var example = await OpenExampleAsync("ntmenu", "MenuBasicExample");

        await example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Document actions", Exact = true }).ClickAsync();
        await _page!.Locator("#menu-basic-example:popover-open").WaitForAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Rename", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Duplicate", Exact = true })).ToBeVisibleAsync();
        await _page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Duplicate", Exact = true }).ClickAsync();
        await Assertions.Expect(example.Locator("output")).ToHaveTextAsync("Duplicate");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task SnackbarAndToastDemos_ShowTheirMessage() {
        var snackbar = await OpenExampleAsync("ntsnackbar", "SnackbarServiceExample");
        await snackbar.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Show snackbar", Exact = true }).ClickAsync();
        await _page!.GetByText("Changes saved", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        var toast = await OpenExampleAsync("nttoast", "ToastServiceExample");
        await toast.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Show toast", Exact = true }).ClickAsync();
        await _page.GetByText("Your profile changes were saved.", new PageGetByTextOptions { Exact = true }).WaitForAsync();
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TooltipDemo_FocusingTheTargetShowsTheTooltip() {
        var example = await OpenExampleAsync("nttooltip", "TooltipBasicExample");

        await example.Locator("button").FocusAsync();
        await Assertions.Expect(example.GetByRole(AriaRole.Tooltip)).ToBeVisibleAsync();
        await Assertions.Expect(example.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Move this conversation to the archive");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DragToScrollDemo_DragFromTrackButtonScrollsTheTable() {
        var example = await OpenDragScrollDemoAsync();

        await DragAsync(example.GetByRole(AriaRole.Button, new() { Name = "Track" }).First, -150);

        (await example.Locator(".nt-data-grid-scroll").EvaluateAsync<double>("strip => strip.scrollLeft")).Should().BeGreaterThan(75, "the table follows the mouse");
        _browserDiagnostics.Should().BeEmpty();
    }

    // WebAssembly runs a click handler and its re-render inside the click dispatch, which the mouse release awaits, so a click
    // leaked by a drag would already show when the drag returns.
    [Fact]
    public async Task DragToScrollDemo_DragFromTrackButtonDoesNotTrack() {
        var example = await OpenDragScrollDemoAsync();

        await DragAsync(example.GetByRole(AriaRole.Button, new() { Name = "Track" }).First, -150);

        (await example.GetByLabel("Tracked shipment").TextContentAsync()).Should().Be("none");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DragToScrollDemo_DragFromCellDoesNotSelectTheRow() {
        var example = await OpenDragScrollDemoAsync();

        await DragAsync(example.GetByRole(AriaRole.Cell, new() { Name = "Ada Lovelace", Exact = true }), -150);

        (await example.GetByLabel("Selected row").TextContentAsync()).Should().Be("none");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DragToScrollDemo_ClickingACellStillSelectsTheRow() {
        var example = await OpenDragScrollDemoAsync();

        await example.GetByRole(AriaRole.Cell, new() { Name = "Ada Lovelace", Exact = true }).ClickAsync();

        await Assertions.Expect(example.GetByLabel("Selected row")).ToHaveTextAsync("SH-1001");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DragToScrollDemo_ClickingTrackStillTracks() {
        var example = await OpenDragScrollDemoAsync();

        await example.GetByRole(AriaRole.Button, new() { Name = "Track" }).First.ClickAsync();

        await Assertions.Expect(example.GetByLabel("Tracked shipment")).ToHaveTextAsync("SH-1001");
        _browserDiagnostics.Should().BeEmpty();
    }

    private async Task<ILocator> OpenDragScrollDemoAsync() {
        // Landscape keeps the docs rail visible, so the grid stays narrow enough to overflow.
        await _page!.SetViewportSizeAsync(700, 680);
        var example = await OpenExampleAsync("ntdatagrid", "DataGridDragScrollExample");
        // The Track buttons enable once the demo is interactive.
        await Assertions.Expect(example.GetByRole(AriaRole.Button, new() { Name = "Track" }).First).ToBeEnabledAsync();
        var overflow = await example.Locator(".nt-data-grid-scroll").EvaluateAsync<double>("strip => strip.scrollWidth - strip.clientWidth");
        overflow.Should().BeGreaterThan(150, "the demo's table must be wider than its grid for a drag to scroll it");
        return example;
    }

    private async Task DragAsync(ILocator start, int deltaX) {
        // Raw mouse input does not scroll, so bring the start point on screen first.
        await start.ScrollIntoViewIfNeededAsync();
        var box = (await start.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await _page!.Mouse.MoveAsync(x, y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(x + deltaX, y, new MouseMoveOptions { Steps = 10 });
        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task VirtualizeDemo_StartsAtTheTop_AndScrollingReachesTheLastItem() {
        var example = await OpenExampleAsync("ntvirtualize", "VirtualizeBasicExample");
        var scroller = example.Locator("div[style*='overflow']");
        await example.GetByText("Item 1", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();
        await scroller.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await example.GetByText("Item 100", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();
        _browserDiagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ntinputtext", "InputTextBasicExample", "Full name", "Grace Hopper")]
    [InlineData("nttextarea", "TextAreaBasicExample", "Notes", "An edited message")]
    [InlineData("ntinputnumeric", "InputNumericBasicExample", "Quantity", "42")]
    public async Task InputDemo_EditsUpdateTheVisibleOutput(string slug, string exampleName, string label, string value) {
        var example = await OpenExampleAsync(slug, exampleName);

        var input = example.GetByLabel(label);
        await input.FillAsync(value);
        await Assertions.Expect(example.Locator("output").Last).ToHaveTextAsync(value);
        await Assertions.Expect(input).ToHaveValueAsync(value);
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectionAndSliderDemos_UpdateTheirBoundOutput() {
        var checkbox = await OpenExampleAsync("ntinputcheckbox", "InputCheckboxBasicExample");
        await checkbox.Locator("input[type=checkbox]").CheckAsync();
        await Assertions.Expect(checkbox.Locator("output")).ToHaveTextAsync("True");

        var toggle = await OpenExampleAsync("ntinputswitch", "InputSwitchBasicExample");
        await Assertions.Expect(toggle.Locator("output")).ToHaveTextAsync("On");
        await toggle.Locator("input[type=checkbox]").UncheckAsync();
        await Assertions.Expect(toggle.Locator("output")).ToHaveTextAsync("Off");

        var radios = await OpenExampleAsync("ntinputradiogroup", "RadioGroupBasicExample");
        await radios.Locator("input[type=radio]").Nth(1).CheckAsync();
        await Assertions.Expect(radios.Locator("output")).ToHaveTextAsync("sms");

        var select = await OpenExampleAsync("ntselect", "SelectBasicExample");
        await select.Locator("select").SelectOptionAsync("de");
        await Assertions.Expect(select.Locator("output")).ToHaveTextAsync("de");

        var combobox = await OpenExampleAsync("ntcombobox", "ComboboxBasicExample");
        await Assertions.Expect(combobox.Locator("output")).ToHaveTextAsync("editor");
        await combobox.Locator("input[role=combobox]").ClickAsync();
        await combobox.GetByRole(AriaRole.Option).Filter(new LocatorFilterOptions { HasText = "Reviewer" }).ClickAsync();
        await Assertions.Expect(combobox.Locator("output")).ToHaveTextAsync("editor, reviewer");

        var slider = await OpenExampleAsync("ntinputslider", "SliderBasicExample");
        var range = slider.Locator("input[type=range]");
        await range.PressAsync("Home");
        await range.PressAsync("ArrowRight");
        await Assertions.Expect(slider.Locator("p output")).ToHaveTextAsync("5");

        var color = await OpenExampleAsync("ntinputcolor", "InputColorBasicExample");
        var colorField = color.Locator("input[data-nt-input-color-input]");
        await colorField.FillAsync("#123456");
        await colorField.PressAsync("Tab");
        await Assertions.Expect(color.Locator("output")).ToHaveTextAsync("#123456");
        await color.GetByRole(AriaRole.Button, new() { Name = "Open color picker" }).ClickAsync();
        var pickerHex = color.Locator("[data-nt-color-field=hex]");
        await pickerHex.FillAsync("#ABCDEF");
        await pickerHex.PressAsync("Enter");
        await Assertions.Expect(colorField).ToHaveValueAsync("#abcdef");
        await Assertions.Expect(color.Locator("output")).ToHaveTextAsync("#123456");
        await color.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
        await Assertions.Expect(color.Locator("output")).ToHaveTextAsync("#abcdef");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TabWizardAndButtonGroupDemos_NavigateToTheSelectedContent() {
        var tabs = await OpenExampleAsync("nttabview", "TabViewBasicExample");
        await tabs.GetByRole(AriaRole.Tab, new LocatorGetByRoleOptions { Name = "Details", Exact = true }).ClickAsync();
        await Assertions.Expect(tabs.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("Billing contact");

        var wizard = await OpenExampleAsync("ntwizard", "WizardBasicExample");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Next Step", Exact = true }).ClickAsync();
        await Assertions.Expect(wizard.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("Second step content");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Previous Step", Exact = true }).ClickAsync();
        await Assertions.Expect(wizard.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("First step content");

        var group = await OpenExampleAsync("ntbuttongroup", "ButtonGroupSelectionExample");
        var quarterly = group.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Quarterly", Exact = true });
        await quarterly.ClickAsync();
        await Assertions.Expect(quarterly).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(group.Locator("output")).ToHaveTextAsync("quarterly");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task FormDemo_ValidatesAndSubmitsEditedValues_AndAppliesDisabledAndReadOnly() {
        var example = await OpenExampleAsync("ntform", "FormValidationExample");
        var name = example.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("^Name\\b") });
        var email = example.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("^Email\\b") });
        var submit = example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit example form", Exact = true });
        var result = example.Locator("output[aria-label='Form result']");

        await name.FillAsync("");
        await submit.ClickAsync();
        await Assertions.Expect(example).ToContainTextAsync("The Name field is required.");
        await Assertions.Expect(result).ToBeEmptyAsync();

        await name.FillAsync("Grace Hopper");
        await email.FillAsync("grace@example.com");
        await submit.ClickAsync();
        await Assertions.Expect(result).ToHaveTextAsync("Submitted Grace Hopper (grace@example.com).");

        var states = Example("FormDisabledReadOnlyExample");
        // The input's title (the model property name) takes precedence over its label text in the accessible name.
        var account = states.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { Name = "Name", Exact = true });
        await Assertions.Expect(account).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Editable = false });
        await states.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Toggle read-only", Exact = true }).ClickAsync();
        await Assertions.Expect(account).ToBeEditableAsync();
        await states.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Toggle disabled", Exact = true }).ClickAsync();
        await Assertions.Expect(account).ToBeDisabledAsync();
        await states.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Toggle disabled", Exact = true }).ClickAsync();
        await Assertions.Expect(account).ToBeEnabledAsync();
        await Assertions.Expect(account).ToHaveValueAsync("Northwind Traders");
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task FileUploadDemo_ReadsTheSelectedBytes_AndRejectsOversizedFiles() {
        var read = await OpenExampleAsync("ntfileupload", "FileUploadReadExample");
        await read.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "hello.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });
        await read.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Upload", Exact = true }).ClickAsync();
        await Assertions.Expect(read.Locator("output[aria-label='Upload result']")).ToHaveTextAsync("Read hello.txt: 5 bytes.");

        var limits = Example("FileUploadLimitsExample");
        await limits.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "big.txt", MimeType = "text/plain", Buffer = new byte[2048] });
        await Assertions.Expect(limits.Locator("output[aria-label='Upload message']")).Not.ToBeEmptyAsync();
        await Assertions.Expect(limits.Locator("output[aria-label='Upload message']")).Not.ToContainTextAsync("Uploaded");
        await Assertions.Expect(limits.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Upload", Exact = true })).ToBeDisabledAsync();
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ContextMenuDemo_InvokesTheAction_ClosesOnEscapeAndOutsideClick_AndHonorsDisabled() {
        var example = await OpenExampleAsync("ntcontextmenu", "ContextMenuBasicExample");
        var trigger = example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Right-click or long-press report.pdf", Exact = true });
        var output = example.Locator("output[aria-label='Context action result']");
        var menu = example.Locator("nt-menu:popover-open");

        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await menu.WaitForAsync();
        // The surface's clip-path animates independently of the menu item's bounds.
        await menu.EvaluateAsync("menu => Promise.allSettled(menu.getAnimations({ subtree: true }).map(animation => animation.finished))");
        await example.GetByRole(AriaRole.Menuitem, new LocatorGetByRoleOptions { Name = "Archive", Exact = true }).ClickAsync();
        await Assertions.Expect(output).ToHaveTextAsync("Archived report.pdf");
        await Assertions.Expect(example.Locator(":popover-open")).ToHaveCountAsync(0);

        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await menu.WaitForAsync();
        await _page!.Keyboard.PressAsync("Escape");
        await Assertions.Expect(example.Locator(":popover-open")).ToHaveCountAsync(0);

        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await menu.WaitForAsync();
        await output.ClickAsync();
        await Assertions.Expect(example.Locator(":popover-open")).ToHaveCountAsync(0);

        var disabled = Example("ContextMenuDisabledExample");
        await Assertions.Expect(disabled.Locator("nt-context-menu")).ToHaveAttributeAsync("data-disabled", "true");
        var lockedTrigger = disabled.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Locked: menu unavailable", Exact = true });
        await lockedTrigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await Assertions.Expect(disabled.Locator(":popover-open")).ToHaveCountAsync(0);

        await disabled.GetByLabel("Lock this file").UncheckAsync();
        var unlockedTrigger = disabled.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Right-click for actions", Exact = true });
        await unlockedTrigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await disabled.Locator("nt-menu:popover-open").WaitForAsync();
        _browserDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WindowHostDemo_OpensClosesAndReopensManagedContent_AndReleasesItOnNavigation() {
        var example = await OpenExampleAsync("ntwindowhost", "WindowHostServiceExample");
        var open = example.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Open managed window", Exact = true });
        var window = example.Locator(".nt-window");
        var count = example.Locator("output");

        await open.ClickAsync();
        await Assertions.Expect(window).ToContainTextAsync("This window is rendered by NTWindowHost.");
        await Assertions.Expect(count).ToHaveTextAsync("1");
        await window.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Close Managed project notes", Exact = true }).ClickAsync();
        await Assertions.Expect(window).ToHaveCountAsync(0);
        await open.ClickAsync();
        await Assertions.Expect(window).ToHaveCountAsync(1);

        await _page!.Locator("a[href='/components/ntbutton']").First.EvaluateAsync("link => link.click()");
        await _page.WaitForURLAsync("**/components/ntbutton");
        await Assertions.Expect(_page.Locator(".nt-window")).ToHaveCountAsync(0);

        await _page.GoBackAsync();
        example = Example("WindowHostServiceExample");
        await example.WaitForAsync();
        await Assertions.Expect(example.Locator("output")).ToHaveTextAsync("0", new LocatorAssertionsToHaveTextOptions { Timeout = 5_000 });
        _browserDiagnostics.Should().BeEmpty("navigating away must release the managed windows");
    }

    [Fact]
    public async Task LayoutDemos_RenderRealPanesCardsFieldsAndDocumentNavigation() {
        var dashboard = await OpenExampleAsync("ntmultipaneview", "MultiPaneViewDashboardExample");
        await Assertions.Expect(dashboard.Locator(".nt-multi-pane-view > .nt-card")).ToHaveCountAsync(3);

        var feed = await OpenExampleAsync("ntfeedview", "FeedViewCardsExample");
        await Assertions.Expect(feed.Locator(".nt-feed-view > .nt-card")).ToHaveCountAsync(6);

        foreach (var (slug, name, expectedFields) in new[] { ("ntformfieldgridview", "FormFieldGridViewContactExample", 6), ("ntformsectionview", "FormSectionViewBasicExample", 4), ("ntformfieldlayoutspan", "FormFieldLayoutSpanShippingExample", 4) }) {
            var form = await OpenExampleAsync(slug, name);
            var fields = form.Locator("input:not([type=hidden])");
            await Assertions.Expect(fields).ToHaveCountAsync(expectedFields);
            await fields.First.FillAsync("Edited field");
            await fields.First.PressAsync("Tab");
            await Assertions.Expect(fields.First).ToHaveValueAsync("Edited field");
        }

        var document = await OpenExampleAsync("ntcontainerview", "ContainerViewDocumentExample");
        var view = document.Locator(".nt-container-view");
        await Assertions.Expect(view.Locator(".nt-container-view-content h2")).ToHaveCountAsync(3);
        await Assertions.Expect(view.Locator(".nt-container-view-quick-nav-list a")).ToHaveCountAsync(3);
        _browserDiagnostics.Should().BeEmpty();
    }

    private ILocator Example(string exampleTypeName) => _page!.Locator($"[data-docs-example='{exampleTypeName}']");

    private async Task<ILocator> OpenExampleAsync(string slug, string exampleTypeName) {
        await OpenComponentPageAsync(slug);
        var example = Example(exampleTypeName);
        await example.WaitForAsync();
        await Assertions.Expect(example.Locator(".docs-example-failure")).ToHaveCountAsync(0);
        return example;
    }

    private async Task OpenComponentPageAsync(string slug) {
        ArgumentNullException.ThrowIfNull(_page);
        _activeRoute = $"/components/{slug}";
        await _page.GotoAsync($"{_baseUrl}{_activeRoute}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.Locator(".docs-example-preview").First.WaitForAsync();
        await Assertions.Expect(_page.Locator(".docs-example-failure")).ToHaveCountAsync(0);
    }

    private static async Task<bool> TryWaitForAttachedAsync(ILocator locator) {
        try {
            await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5_000 });
            return true;
        }
        catch (TimeoutException) {
            return false;
        }
    }

    private static string FindRepositoryRoot() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            if (File.Exists(Path.Combine(directory.FullName, "NTComponents.slnx"))) {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the NTComponents repository root from the test output directory.");
    }

    private static string FirstLine(string value) {
        var lines = value.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.FirstOrDefault(line => line.StartsWith("System.", StringComparison.Ordinal) || line.StartsWith("TypeError", StringComparison.Ordinal))
            ?? lines.FirstOrDefault()
            ?? value;
    }

    private static string CreateThemeSource(string selector) => $$"""
        {{selector}} {
          --md-sys-color-primary: rgb(1 2 3);
          --md-sys-color-on-primary: rgb(255 255 255);
          --md-extended-color-success-on-color-container: rgb(4 5 6);
        }
        """;

    private Task WaitForRootColorAsync(string propertyName, string expected) => _page!.WaitForFunctionAsync(
        """
        ([name, expected]) => {
            const probe = document.createElement('span');
            probe.style.color = `var(${name})`;
            document.body.append(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color === expected;
        }
        """, new[] { propertyName, expected });

    private async Task<string> RootColorAsync(string propertyName) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync("name => getComputedStyle(document.documentElement).getPropertyValue(name).trim().length > 0", propertyName);
        return await _page.EvaluateAsync<string>(
            """
            name => {
                const probe = document.createElement('span');
                probe.style.color = `var(${name})`;
                document.body.append(probe);
                const color = getComputedStyle(probe).color;
                probe.remove();
                return color;
            }
            """,
            propertyName);
    }

    private async Task WaitForPalettePickerToMatchPreviewAsync(string paletteId, string token) {
        ArgumentNullException.ThrowIfNull(_page);
        await _page.WaitForFunctionAsync(
            """
            args => {
                const input = document.querySelector(`#theme-palette-${args.paletteId}`);
                const rgb = document.querySelector('[data-testid=theme-preview]').style.getPropertyValue(args.token).match(/\d+/g);
                return input?.value === `#${rgb?.map(channel => Number(channel).toString(16).padStart(2, '0')).join('')}`;
            }
            """,
            new { paletteId, token });
    }

    private static string RemoveGenericArity(string name) {
        var arityIndex = name.IndexOf('`');
        return arityIndex < 0 ? name : name[..arityIndex];
    }

    private static Task WaitForRenderAsync(IPage page) => page.EvaluateAsync(
        "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private sealed record BrowserDiagnostic(string Route, string Kind, string Message);
}
