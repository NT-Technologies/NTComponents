using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Core;

public class NTHeadDependencies_Tests : BunitContext {

    [Fact]
    public void Render_DefaultFontFamilies_SplitsTextAndIconStylesheets() {
        var cut = Render<NTHeadDependencies>();
        var textHref = cut.FindAll("link#nt-fonts-stylesheet").Should().ContainSingle().Subject.GetAttribute("href");
        var iconHref = cut.FindAll("link#nt-icon-fonts-stylesheet").Should().ContainSingle().Subject.GetAttribute("href");

        textHref.Should().Be("https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;600;700&display=swap");
        iconHref.Should().StartWith("https://fonts.googleapis.com/css2?");
        iconHref.Should().Contain("family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,400,0..1,0");
        iconHref.Should().Contain("family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,400,0..1,0");
        iconHref.Should().Contain("family=Material+Symbols+Sharp:opsz,wght,FILL,GRAD@20..48,400,0..1,0");
        iconHref.Should().NotContain("Roboto");
        iconHref.Should().EndWith("&display=block");
        cut.FindAll("link[href^='https://fonts.googleapis.com/css2']").Should().HaveCount(2);
    }

    [Fact]
    public void Render_EmitsThemeRuntimeConfigBeforeBundle() {
        var cut = Render<NTHeadDependencies>();
        var markup = cut.Markup;

        markup.Should().Contain("data-tnt-theme-critical");
        markup.Should().Contain("data-nt-theme-critical");
        markup.Should().Contain("data-permanent");
        markup.Should().Contain("data-nt-theme-default");
        markup.Should().Contain("id=\"nt-theme-config\"");
        markup.Should().Contain("id=\"nt-theme-state\"");
        markup.Should().Contain("_content/NTComponents/NTTheme.js");
        cut.Find("#nt-measurement-tokens").TextContent.Should().Be(":root{--tnt-header-height:64px;--tnt-footer-height:64px;--tnt-side-nav-width:256px;}");
        markup.Should().Contain("_content/NTComponents/nt-measurements.css");

        markup.IndexOf("data-nt-theme-default", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("id=\"nt-theme-config\"", StringComparison.Ordinal));
        markup.IndexOf("id=\"nt-theme-config\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/NTTheme.js", StringComparison.Ordinal));
        markup.IndexOf("id=\"nt-theme-state\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("_content/NTComponents/NTTheme.js", StringComparison.Ordinal));
        markup.IndexOf("_content/NTComponents/NTTheme.js", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("class=\"tnt-measurements\"", StringComparison.Ordinal));
        markup.IndexOf("class=\"tnt-measurements\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("id=\"nt-measurements-stylesheet\"", StringComparison.Ordinal));
        markup.IndexOf("id=\"nt-measurements-stylesheet\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("id=\"nt-ripple-stylesheet\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_DoesNotLoadLegacyThemeCompatibilityStylesheet() {
        var cut = Render<NTHeadDependencies>();

        cut.Markup.Should().NotContain("nt-theme-compat");
    }

    [Fact]
    public void Render_PreloadsMeasurementStylesheetBeforeThemeBundle() {
        var cut = Render<NTHeadDependencies>();
        var markup = cut.Markup;
        var preload = cut.FindAll("link#nt-measurements-preload").Should().ContainSingle().Subject;
        var stylesheet = cut.Find("link#nt-measurements-stylesheet");

        preload.GetAttribute("rel").Should().Be("preload");
        preload.GetAttribute("as").Should().Be("style");
        preload.GetAttribute("href").Should().Be(stylesheet.GetAttribute("href"));
        preload.HasAttribute("data-permanent").Should().BeTrue();
        markup.IndexOf("id=\"nt-measurements-preload\"", StringComparison.Ordinal).Should().BeLessThan(markup.IndexOf("id=\"nt-theme-script\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_MarksThemeHeadItemsAsPermanent() {
        var cut = Render<NTHeadDependencies>();
        var permanentElements = cut.FindAll("[data-permanent]");

        permanentElements.Should().OnlyContain(element => !string.IsNullOrWhiteSpace(element.Id));
        permanentElements.Select(element => element.Id).Should().OnlyHaveUniqueItems();
        permanentElements.Should().OnlyContain(element => element.GetAttribute("data-permanent") == element.Id);
        cut.Find("#nt-theme-critical").HasAttribute("data-nt-theme-critical").Should().BeTrue();
        cut.FindAll("link[data-nt-theme-default]").Should().OnlyContain(link => link.HasAttribute("data-permanent"));
        cut.Find("#nt-theme-active-slot").HasAttribute("href").Should().BeFalse();
        cut.Find("#nt-theme-pending-slot").HasAttribute("href").Should().BeFalse();
        cut.Find("#nt-theme-script").GetAttribute("src").Should().Be("_content/NTComponents/NTTheme.js");
        cut.Find("#nt-measurements-stylesheet").GetAttribute("href").Should().Be("_content/NTComponents/nt-measurements.css");
        cut.Find("#nt-measurement-tokens").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-ripple-stylesheet").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-fonts-api-preconnect").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-fonts-static-preconnect").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-fonts-stylesheet").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-icon-fonts-stylesheet").HasAttribute("data-permanent").Should().BeTrue();
        cut.Find("#nt-anchor-positioning-loader").HasAttribute("data-permanent").Should().BeTrue();
    }

    [Fact]
    public void Render_UsesLocalAnchorPositioningPolyfill() {
        var cut = Render<NTHeadDependencies>();
        var loader = cut.Find("#nt-anchor-positioning-loader");

        loader.TextContent.Should().Contain("_content/NTComponents/css-anchor-positioning.js");
        loader.TextContent.Should().NotContain("unpkg.com");
    }

    [Fact]
    public void CreateIconFontStylesheetHref_NoIconFamilies_ReturnsNull() {
        NTHeadDependencies.CreateIconFontStylesheetHref(NTFontFamily.None).Should().BeNull();
        NTHeadDependencies.CreateIconFontStylesheetHref(NTFontFamily.Roboto).Should().BeNull();
    }

    [Fact]
    public void Render_SelectedFontFamilies_OnlyIncludesRequestedFamilies() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.FontFamilies, NTFontFamily.Roboto | NTFontFamily.MaterialSymbolsOutlined));
        var iconHref = cut.Find("#nt-icon-fonts-stylesheet").GetAttribute("href");

        cut.Find("#nt-fonts-stylesheet").GetAttribute("href").Should().Contain("family=Roboto:wght@400;500;600;700");
        iconHref.Should().Contain("family=Material+Symbols+Outlined");
        iconHref.Should().NotContain("Material+Symbols+Rounded");
        iconHref.Should().NotContain("Material+Symbols+Sharp");
    }

    [Fact]
    public void Render_IconFamiliesOnly_OmitsTextStylesheet() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.FontFamilies, NTFontFamily.MaterialSymbolsOutlined));

        cut.FindAll("#nt-fonts-stylesheet").Should().BeEmpty();
        cut.Find("#nt-icon-fonts-stylesheet").GetAttribute("href").Should().Contain("family=Material+Symbols+Outlined");
        cut.FindAll("#nt-fonts-api-preconnect,#nt-fonts-static-preconnect").Should().HaveCount(2);
    }

    [Fact]
    public void Render_RobotoOnly_OmitsIconStylesheet() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.FontFamilies, NTFontFamily.Roboto));

        cut.Find("#nt-fonts-stylesheet").GetAttribute("href").Should().EndWith("&display=swap");
        cut.FindAll("#nt-icon-fonts-stylesheet").Should().BeEmpty();
    }

    [Fact]
    public void Render_CustomFontStylesheet_ReplacesGoogleFontsDependencies() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.FontStylesheetHref, "fonts/app-fonts.css"));

        cut.Find("#nt-fonts-stylesheet").GetAttribute("href").Should().Be("fonts/app-fonts.css");
        cut.FindAll("link[href*='fonts.googleapis.com'],link[href*='fonts.gstatic.com']").Should().BeEmpty();
    }

    [Fact]
    public void Render_NoFontFamilies_OmitsFontDependencies() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.FontFamilies, NTFontFamily.None));

        cut.FindAll("#nt-fonts-stylesheet,#nt-icon-fonts-stylesheet,#nt-fonts-api-preconnect,#nt-fonts-static-preconnect").Should().BeEmpty();
    }

    [Fact]
    public void ResolveAssetPath_UsesContentSpecificUrl() {
        var assets = new ResourceAssetCollection([
            new ResourceAsset("_content/NTComponents/NTTheme.fingerprint.js", [new ResourceAssetProperty("label", "_content/NTComponents/NTTheme.js")])
        ]);

        NTHeadDependencies.ResolveAssetPath(assets, "_content/NTComponents/NTTheme.js").Should().Be("_content/NTComponents/NTTheme.fingerprint.js");
    }

    [Fact]
    public void Render_DefaultSystemTheme_EmitsMediaQualifiedFirstPaintThemeLinks() {
        var cut = Render<NTHeadDependencies>();
        var defaultLinks = cut.FindAll("link[data-nt-theme-default]");

        defaultLinks.Should().HaveCount(2);
        defaultLinks[0].GetAttribute("rel").Should().Be("stylesheet");
        defaultLinks[0].GetAttribute("href").Should().Be("/Themes/light.css");
        defaultLinks[0].GetAttribute("media").Should().Be("(prefers-color-scheme: light)");
        defaultLinks[1].GetAttribute("rel").Should().Be("stylesheet");
        defaultLinks[1].GetAttribute("href").Should().Be("/Themes/dark.css");
        defaultLinks[1].GetAttribute("media").Should().Be("(prefers-color-scheme: dark)");
    }

    [Fact]
    public void Render_CustomThemeConfiguration_IsSerialized() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.DefaultTheme, NTTheme.Dark)
            .Add(p => p.DefaultContrast, NTThemeContrast.High)
            .Add(p => p.ThemesRoot, "/brand/themes")
            .Add(p => p.LightDefaultCss, "brand-light.css")
            .Add(p => p.LightMediumCss, "brand-light-mc.css")
            .Add(p => p.LightHighCss, "brand-light-hc.css")
            .Add(p => p.DarkDefaultCss, "brand-dark.css")
            .Add(p => p.DarkMediumCss, "brand-dark-mc.css")
            .Add(p => p.DarkHighCss, "brand-dark-hc.css"));

        cut.Markup.Should().Contain("\"defaultTheme\":\"DARK\"");
        cut.Markup.Should().Contain("\"defaultContrast\":\"HIGH\"");
        cut.Markup.Should().Contain("\"themesRoot\":\"/brand/themes\"");
        cut.Markup.Should().Contain("\"lightDefaultCss\":\"brand-light.css\"");
        cut.Markup.Should().Contain("\"darkHighCss\":\"brand-dark-hc.css\"");
    }

    [Fact]
    public void Render_ExplicitDefaultTheme_EmitsSingleFirstPaintThemeLink() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.DefaultTheme, NTTheme.Dark)
            .Add(p => p.DefaultContrast, NTThemeContrast.High)
            .Add(p => p.ThemesRoot, "/brand/themes")
            .Add(p => p.DarkHighCss, "brand-dark-hc.css"));
        var defaultLinks = cut.FindAll("link[data-nt-theme-default]");

        defaultLinks.Should().HaveCount(1);
        defaultLinks[0].GetAttribute("href").Should().Be("/brand/themes/brand-dark-hc.css");
        defaultLinks[0].HasAttribute("media").Should().BeFalse();
    }

    [Fact]
    public void Render_DefaultThemeChanges_ReconcileIndependentThemeLinkRegions() {
        var cut = Render<NTHeadDependencies>(parameters => parameters.Add(p => p.DefaultTheme, NTTheme.Light));

        cut.FindAll("link[data-nt-theme-default]").Should().ContainSingle(link => link.Id == "nt-theme-default-light");

        cut.Render(parameters => parameters.Add(p => p.DefaultTheme, NTTheme.System));
        cut.FindAll("link[data-nt-theme-default]").Select(link => link.Id).Should().BeEquivalentTo("nt-theme-default-light", "nt-theme-default-dark");

        cut.Render(parameters => parameters.Add(p => p.DefaultTheme, NTTheme.Dark));
        cut.FindAll("link[data-nt-theme-default]").Should().ContainSingle(link => link.Id == "nt-theme-default-dark");
    }

    [Fact]
    public void Render_CustomMeasurementConfiguration_IsEmittedInline() {
        var cut = Render<NTHeadDependencies>(parameters => parameters
            .Add(p => p.TokenScopeSelector, ".app-shell")
            .Add(p => p.HeaderHeight, 72.5)
            .Add(p => p.FooterHeight, 48)
            .Add(p => p.SideNavWidth, 300));

        cut.Find("#nt-measurement-tokens").TextContent.Should().Be(".app-shell{--tnt-header-height:72.5px;--tnt-footer-height:48px;--tnt-side-nav-width:300px;}");
    }

}
