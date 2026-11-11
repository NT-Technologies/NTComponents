using Microsoft.Extensions.DependencyInjection;

namespace NTComponents.Tests.Icons;

public class NTSVGIcon_Tests : BunitContext {
    private const string Svg = """<svg viewBox="0 0 24 24" width="48" height="48"><path d="M12 2 2 22h20z"/></svg>""";

    [Fact]
    public void Activation_WithoutConstructorArguments_CreatesComponent() {
        var factory = ActivatorUtilities.CreateFactory(typeof(NTSVGIcon), Type.EmptyTypes);

        var icon = factory(Services, null);

        icon.Should().BeOfType<NTSVGIcon>();
    }

    [Fact]
    public void Constructor_WithSvg_SetsIconAndIsNTIcon() {
        // Arrange & Act
        NTIcon icon = new NTSVGIcon(Svg);

        // Assert
        icon.Icon.Should().Be(Svg);
        icon.Color.Should().BeNull();
        icon.Size.Should().Be(IconSize.Medium);
    }

    [Fact]
    public void Render_EmitsSvgAsMarkupInsideIconSpan() {
        // Arrange & Act
        var cut = Render<NTSVGIcon>(p => p.Add(c => c.Icon, Svg));

        // Assert
        var span = cut.Find("span.nt-icon.nt-svg-icon");
        span.QuerySelector("svg > path").Should().NotBeNull();
        span.TextContent.Should().BeEmpty();
        span.GetAttribute("class")!.Should().Contain("mi-medium").And.NotContain("material-symbols");
    }

    [Fact]
    public void Render_DoesNotUseMarkupAsDefaultTitle() {
        // Arrange & Act
        var cut = Render<NTSVGIcon>(p => p.Add(c => c.Icon, Svg));

        // Assert
        cut.Find("span").HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void Render_UsesElementTitleWhenSet() {
        // Arrange & Act
        var cut = Render<NTSVGIcon>(p => p.Add(c => c.Icon, Svg).Add(c => c.ElementTitle, "Warning"));

        // Assert
        cut.Find("span").GetAttribute("title").Should().Be("Warning");
    }

    [Fact]
    public void Render_AppliesColorStyleForNonDefaultColor() {
        // Arrange & Act
        var cut = Render<NTSVGIcon>(p => p.Add(c => c.Icon, Svg).Add(c => c.Color, NTColor.Primary));

        // Assert
        cut.Find("span").GetAttribute("style")!.Should().Contain("color:var(--nt-color-primary)");
    }

    [Fact]
    public void Render_OmitsColorStyleForDefaultColor() {
        // Arrange & Act
        var cut = Render<NTSVGIcon>(p => p.Add(c => c.Icon, Svg));

        // Assert
        (cut.Find("span").GetAttribute("style") ?? string.Empty).Should().NotContain("color");
    }

    [Fact]
    public void RenderFragment_AppliesAdditionalClassAndRendersSvg() {
        // Arrange
        var icon = new NTSVGIcon(Svg);

        // Act
        var cut = Render(icon.Render("leading-icon"));

        // Assert
        var span = cut.Find("span.nt-svg-icon.leading-icon");
        span.QuerySelector("svg").Should().NotBeNull();
        span.HasAttribute("title").Should().BeFalse();
    }
}
