using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NTComponents.Tests.Breadcrumb;

public class NTBreadcrumb_Tests : BunitContext {

    [Theory]
    [InlineData("/", 0)]
    [InlineData("/catalog/product", 2)]
    public void DividerTemplate_ReplacesChevronOnlyBetweenItems(string url, int expectedDividers) {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var cut = Render<NTBreadcrumb>(p => p.Add(c => c.DividerTemplate, "<span class='custom-divider'>/</span>"));

        cut.FindAll(".separator[aria-hidden='true'] > .custom-divider").Should().HaveCount(expectedDividers);
        cut.FindAll(".chevron").Should().BeEmpty();
        cut.Find("li").QuerySelector(".separator").Should().BeNull();
    }

    [Fact]
    public void DefaultDivider_RendersChevron() {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/catalog/product");
        var cut = Render<NTBreadcrumb>();

        cut.FindAll(".separator.chevron[aria-hidden='true']").Should().HaveCount(2);
    }

    [Theory]
    [InlineData(NTSize.Smallest, "xs")]
    [InlineData(NTSize.Small, "s")]
    [InlineData(NTSize.Medium, "m")]
    [InlineData(NTSize.Large, "l")]
    [InlineData(NTSize.Largest, "xl")]
    public void Size_AppliesToSharedNavigationLinksAndCurrentPage(NTSize size, string suffix) {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/catalog/product");
        var cut = Render<NTBreadcrumb>(p => p.Add(c => c.Size, size));

        cut.Find("nav").ClassList.Should().Contain($"nt-size-{suffix}");
        cut.FindAll("a.nt-nav-link").Should().HaveCount(2);
        foreach (var link in cut.FindAll("a")) {
            link.ClassList.Should().Contain($"nt-size-{suffix}").And.Contain("nt-nav-link-text");
            link.HasAttribute("aria-current").Should().BeFalse();
        }
        cut.Find("span[aria-current='page']").TextContent.Should().Be("Product");
    }

    [Fact]
    public void Colors_CustomizeLinksCurrentPageAndSeparators() {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/catalog");
        var cut = Render<NTBreadcrumb>(p => p
            .Add(c => c.TextColor, NTColor.Tertiary)
            .Add(c => c.CurrentTextColor, NTColor.OnTertiaryContainer)
            .Add(c => c.CurrentBackgroundColor, NTColor.TertiaryContainer)
            .Add(c => c.SeparatorColor, NTColor.Outline));

        cut.Find("a").GetAttribute("style").Should().Contain("--nt-nav-link-fg:var(--nt-color-tertiary)");
        cut.Find("nav").GetAttribute("style").Should()
            .Contain("--nt-breadcrumb-current-fg:var(--nt-color-on-tertiary-container)")
            .And.Contain("--nt-breadcrumb-current-bg:var(--nt-color-tertiary-container)")
            .And.Contain("--nt-breadcrumb-separator:var(--nt-color-outline)");
    }

    [Fact]
    public void Root_RendersHomeAsCurrentPageWithoutLinks() {
        var cut = Render<NTBreadcrumb>();

        cut.Find("[aria-current='page']").TextContent.Should().Be("Home");
        cut.FindAll("a, .separator").Should().BeEmpty();
    }

    [Theory]
    [InlineData("/catalog/product-details", "Product Details")]
    [InlineData("/catalog/product_details/?sort=name#description", "Product Details")]
    [InlineData("/catalog/red%20shoes", "Red Shoes")]
    public void NestedUrl_BuildsAncestorLinksAndCurrentLabel(string url, string currentLabel) {
        Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var cut = Render<NTBreadcrumb>();

        cut.FindAll("a").Select(a => a.GetAttribute("href")).Should().Equal("http://localhost/", "http://localhost/catalog");
        cut.FindAll(".nt-nav-link-label, li > .label").Select(item => item.TextContent).Should().Equal("Home", "Catalog", currentLabel);
        cut.Find("[aria-current='page']").TextContent.Should().Be(currentLabel);
        cut.FindAll("[aria-current]").Should().ContainSingle();
        cut.FindAll(".separator[aria-hidden='true']").Should().HaveCount(2);
    }

    [Fact]
    public async Task Navigation_ReplacesTrailWithoutRetainingPreviousPages() {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/catalog/product");
        var cut = Render<NTBreadcrumb>();

        await cut.InvokeAsync(() => navigation.NavigateTo("/account"));

        cut.FindAll(".nt-nav-link-label, li > .label").Select(item => item.TextContent).Should().Equal("Home", "Account");
        cut.Find("[aria-current='page']").TextContent.Should().Be("Account");
    }

    [Fact]
    public void CustomAttributes_PreserveLabelsDirectionAndStyling() {
        var cut = Render<NTBreadcrumb>(p => p
            .Add(c => c.HomeLabel, "Start")
            .Add(c => c.AccessibleLabel, "Location")
            .Add(c => c.ElementId, "trail")
            .AddUnmatched("dir", "rtl")
            .AddUnmatched("class", "custom")
            .AddUnmatched("style", "margin:8px"));

        var nav = cut.Find("nav#trail");
        nav.GetAttribute("aria-label").Should().Be("Location");
        nav.GetAttribute("dir").Should().Be("rtl");
        nav.ClassList.Should().Contain("custom").And.Contain("nt-breadcrumb");
        nav.GetAttribute("style").Should().Contain("margin:8px");
        cut.Find("[aria-current='page']").TextContent.Should().Be("Start");
    }

    [Fact]
    public async Task StaticRendering_UsesBasePathAndEncodesUrlLabelsWithoutInteractiveServices() {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager>(new BreadcrumbNavigationManager("https://example.test/app/", "https://example.test/app/red%20shoes/%3Cscript%3E"))
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () => {
            var output = await renderer.RenderComponentAsync<NTBreadcrumb>();
            return output.ToHtmlString();
        });

        using var document = new HtmlParser().ParseDocument(html);
        document.QuerySelector("nav")!.GetAttribute("aria-label").Should().Be("Breadcrumb");
        document.QuerySelectorAll("a").Select(a => a.GetAttribute("href")).Should().Equal("https://example.test/app/", "https://example.test/app/red%20shoes");
        document.QuerySelector("[aria-current='page']")!.TextContent.Should().Be("<Script>");
        document.QuerySelector("[aria-current='page'] script").Should().BeNull();
        document.QuerySelectorAll("button").Should().BeEmpty();
    }
}
