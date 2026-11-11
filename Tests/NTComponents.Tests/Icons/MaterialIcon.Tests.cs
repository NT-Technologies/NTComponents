namespace NTComponents.Tests.Icons;

public class MaterialIcon_Tests : BunitContext {

    [Fact]
    public void Constructor_Parameterless_InitializesWithDefaults() {
        // Arrange & Act
        var icon = new MaterialIcon();

        // Assert
        icon.Appearance.Should().Be(IconAppearance.Default);
        icon.Color.Should().BeNull();
        icon.Size.Should().Be(IconSize.Medium);
    }

    [Fact]
    public void Constructor_WithIcon_SetsIconProperty() {
        // Arrange & Act
        var icon = new MaterialIcon("home");

        // Assert
        icon.Icon.Should().Be("home");
    }

    [Fact]
    public void ElementStyle_ReturnsStyleFromAdditionalAttributes() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "opacity: 0.8;" } };

        // Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.AdditionalAttributes, attrs).Add(c => c.Icon, "visibility"));

        // Assert
        var style = cut.Find("span").GetAttribute("style");
        if (style != null) {
            style.Should().Contain("opacity: 0.8;");
        }
    }

    [Fact]
    public void HasNtIdAttribute() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Icon, "notifications"));
        var root = cut.Find("span.nt-icon");

        // Assert
        root.HasAttribute("ntid").Should().BeTrue();
        root.GetAttribute("ntid").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ImplicitOperator_ReturnsIconString() {
        // Arrange
        var icon = new MaterialIcon("dashboard");

        // Act
        string iconString = icon;

        // Assert
        iconString.Should().Be("dashboard");
    }

    [Fact]
    public void Render_CombinesAllClassesCorrectly() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p =>
            p.Add(c => c.Icon, "build")
             .Add(c => c.Size, IconSize.Large)
             .Add(c => c.Appearance, IconAppearance.Sharp)
             .Add(c => c.ElementId, "combined-test"));

        var cls = cut.Find("span").GetAttribute("class")!;

        // Assert
        cls.Should().Contain("nt-icon");
        cls.Should().Contain("nt-components");
        cls.Should().Contain("material-symbols-sharp");
        cls.Should().Contain("mi-large");
        cut.Find("span").TextContent.Should().Be("build");
        cut.Find("span").GetAttribute("id")!.Should().Be("combined-test");
    }

    [Fact]
    public void Render_MultipleTimesWithDifferentAdditionalClass_UpdatesAdditionalClass() {
        // Arrange
        var icon = new MaterialIcon("refresh");

        // Act
        var cut1 = Render(icon.Render("first-material-class"));
        var cut2 = Render(icon.Render("second-material-class"));

        // Assert
        cut1.Find("span").GetAttribute("class")!.Should().Contain("first-material-class");
        cut2.Find("span").GetAttribute("class")!.Should().Contain("second-material-class");
    }

    [Fact]
    public void Render_WithAdditionalAttributes_MergesClassAttribute() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "class", "material-extra" } };

        // Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.AdditionalAttributes, attrs).Add(c => c.Icon, "edit"));

        // Assert
        var cls = cut.Find("span").GetAttribute("class")!;
        cls.Should().Contain("material-extra");
        cls.Should().Contain("nt-icon");
    }

    [Fact]
    public void Render_WithAdditionalAttributes_MergesStyleAttribute() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "style", "font-size: 24px;" } };

        // Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.AdditionalAttributes, attrs).Add(c => c.Icon, "account"));

        // Assert
        var style = cut.Find("span").GetAttribute("style");
        if (style != null) {
            style.Should().Contain("font-size: 24px;");
        }
    }

    [Fact]
    public void Render_WithAdditionalClass_IncludesAdditionalClass() {
        // Arrange
        var icon = new MaterialIcon("close");

        // Act
        var cut = Render(icon.Render("custom-material-class"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("custom-material-class");
    }

    [Theory]
    [InlineData(IconAppearance.Default, "material-symbols-outlined")]
    [InlineData(IconAppearance.Filled, "material-symbols-outlined")]
    [InlineData(IconAppearance.Outlined, "material-symbols-outlined")]
    [InlineData(IconAppearance.Round, "material-symbols-rounded")]
    [InlineData(IconAppearance.Sharp, "material-symbols-sharp")]
    public void Render_WithAllAppearances_RendersCorrectClasses(IconAppearance appearance, string expectedClass) {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, appearance).Add(c => c.Icon, "test"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Theory]
    [InlineData(IconSize.Small, "mi-small")]
    [InlineData(IconSize.Medium, "mi-medium")]
    [InlineData(IconSize.Large, "mi-large")]
    [InlineData(IconSize.ExtraLarge, "mi-extra-large")]
    public void Render_WithAllSizes_RendersCorrectClasses(IconSize size, string expectedClass) {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Size, size).Add(c => c.Icon, "test"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Render_WithAppearance_Default_AddsMaterialSymbolsOutlined() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, IconAppearance.Default).Add(c => c.Icon, "star"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("material-symbols-outlined");
    }

    [Fact]
    public void Render_WithAppearance_Outlined_AddsMaterialSymbolsOutlined() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, IconAppearance.Outlined).Add(c => c.Icon, "favorite"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("material-symbols-outlined");
    }

    [Fact]
    public void Render_WithAppearance_Filled_AddsMaterialSymbolsFillAxis() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, IconAppearance.Filled).Add(c => c.Icon, "favorite"));

        // Assert
        var icon = cut.Find("span");
        icon.GetAttribute("class")!.Should().Contain("material-symbols-outlined");
        icon.GetAttribute("style").Should().Contain("font-variation-settings:'FILL' 1");
    }

    [Fact]
    public void Render_WithAppearance_Round_AddsMaterialSymbolsRounded() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, IconAppearance.Round).Add(c => c.Icon, "person"));

        // Assert
        var cls = cut.Find("span").GetAttribute("class")!;
        cls.Should().Contain("material-symbols-rounded");
        cls.Should().NotContain("material-symbols-outlined");
        cls.Should().NotContain("material-symbols-sharp");
    }

    [Fact]
    public void Render_WithAppearance_Sharp_AddsMaterialSymbolsSharp() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Appearance, IconAppearance.Sharp).Add(c => c.Icon, "settings"));

        // Assert
        var cls = cut.Find("span").GetAttribute("class")!;
        cls.Should().Contain("material-symbols-sharp");
        cls.Should().NotContain("material-symbols-outlined");
        cls.Should().NotContain("material-symbols-rounded");
    }

    [Fact]
    public void Render_WithColor_AppliesForegroundColorStyle() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Color, NTColor.Primary).Add(c => c.Appearance, IconAppearance.Filled).Add(c => c.Icon, "heart"));

        // Assert
        var style = cut.Find("span").GetAttribute("style")!;
        style.Should().Contain("color:var(--nt-color-primary)");
        style.Should().Contain("font-variation-settings:'FILL' 1");
    }

    [Fact]
    public void Render_WithoutColor_InheritsTextColor() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Icon, "heart"));

        // Assert
        cut.Find("span").GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void Render_WithOnSurfaceColor_AppliesExplicitColor() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Icon, "heart").Add(c => c.Color, NTColor.OnSurface));

        // Assert
        cut.Find("span").GetAttribute("style")!.Should().Contain("color:var(--nt-color-on-surface)");
    }

    [Fact]
    public void Render_WithDefaultParameters_RendersSpanWithMaterialIconClasses() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Icon, "search"));
        var root = cut.Find("span.nt-icon");

        // Assert
        root.Should().NotBeNull();
        root.GetAttribute("class")!.Should().Contain("nt-icon");
        root.GetAttribute("class")!.Should().Contain("nt-components");
        root.GetAttribute("class")!.Should().Contain("material-symbols-outlined"); // default appearance
        root.GetAttribute("class")!.Should().Contain("mi-medium"); // default size
        root.TextContent.Should().Be("search");
    }

    [Fact]
    public void Render_WithElementId_SetsIdAttribute() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.ElementId, "material-icon").Add(c => c.Icon, "check"));

        // Assert
        cut.Find("span").GetAttribute("id")!.Should().Be("material-icon");
    }

    [Fact]
    public void Render_WithElementTitle_SetsTitleAttribute() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.ElementTitle, "Material icon tooltip").Add(c => c.Icon, "info"));

        // Assert
        cut.Find("span").GetAttribute("title")!.Should().Be("Material icon tooltip");
    }

    [Fact]
    public void Render_WithNoAdditionalClass_DoesNotIncludeEmptyAdditionalClass() {
        // Arrange
        var icon = new MaterialIcon("home");

        // Act
        var cut = Render(icon.Render());

        // Assert Should not have empty additional class in the class string
        var cls = cut.Find("span").GetAttribute("class")!;
        cls.Should().NotContain("  "); // No double spaces indicating empty class
        cls.Should().EndWith("mi-medium"); // Should end properly
    }

    [Fact]
    public void Render_WithoutElementTitle_UsesIconAsTitle() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Icon, "delete"));

        // Assert
        cut.Find("span").GetAttribute("title")!.Should().Be("delete");
    }

    [Fact]
    public void Render_WithSize_ExtraLarge_AddsExtraLargeSizeClass() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Size, IconSize.ExtraLarge).Add(c => c.Icon, "warning"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("mi-extra-large");
    }

    [Fact]
    public void Render_WithSize_Large_AddsLargeSizeClass() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Size, IconSize.Large).Add(c => c.Icon, "location"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("mi-large");
    }

    [Fact]
    public void Render_WithSize_Medium_AddsMediumSizeClass() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Size, IconSize.Medium).Add(c => c.Icon, "phone"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("mi-medium");
    }

    [Fact]
    public void Render_WithSize_Small_AddsSmallSizeClass() {
        // Arrange & Act
        var cut = Render<MaterialIcon>(p => p.Add(c => c.Size, IconSize.Small).Add(c => c.Icon, "mail"));

        // Assert
        cut.Find("span").GetAttribute("class")!.Should().Contain("mi-small");
    }

    [Fact]
    public void Static_MaterialIcon_Properties_ExistAndHaveValues() {
        // Assert - Test a few of the static properties to ensure they exist and have values
        MaterialIcon.Add.Should().NotBeNull();
        MaterialIcon.Add.Icon.Should().Be("add");

        MaterialIcon.Home.Should().NotBeNull();
        MaterialIcon.Home.Icon.Should().Be("home");

        MaterialIcon.Settings.Should().NotBeNull();
        MaterialIcon.Settings.Icon.Should().Be("settings");
    }
}
