namespace NTComponents.Tests.Divider;

public class NTDivider_Tests : BunitContext {

    [Fact]
    public void AdditionalAttributes_Class_And_Style_Are_Merged() {
        // Arrange
        var attrs = new Dictionary<string, object> {
            { "class", "custom-divider" },
            { "style", "margin-block:8px" }
        };

        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var divider = cut.Find("div.nt-divider");

        // Assert
        divider.GetAttribute("class")!.Should().Contain("custom-divider");
        divider.GetAttribute("style")!.Should().Contain("margin-block:8px");
        divider.GetAttribute("style")!.Should().Contain("--nt-divider-color:var(--nt-color-outline-variant)");
    }

    [Fact]
    public void Default_Render_Uses_FullWidth_Horizontal_OutlineVariant_Separator() {
        // Act
        var cut = Render<NTDivider>();
        var divider = cut.Find("div.nt-divider");

        // Assert
        var cls = divider.GetAttribute("class")!;
        cls.Should().Contain("nt-divider-horizontal");
        cls.Should().Contain("nt-divider-full-width");
        divider.GetAttribute("role").Should().Be("separator");
        divider.GetAttribute("aria-orientation").Should().Be("horizontal");
        divider.GetAttribute("style")!.Should().Contain("--nt-divider-color:var(--nt-color-outline-variant)");
    }

    [Fact]
    public void Direction_Vertical_Adds_Vertical_Class_And_Aria_Orientation() {
        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.Direction, NTLayoutDirection.Vertical));
        var divider = cut.Find("div.nt-divider");

        // Assert
        var cls = divider.GetAttribute("class")!;
        cls.Should().Contain("nt-divider-vertical");
        cls.Should().NotContain("nt-divider-horizontal");
        cls.Should().NotContain("nt-divider-full-width");
        divider.GetAttribute("aria-orientation").Should().Be("vertical");
    }

    [Theory]
    [InlineData(NTDividerVariant.FullWidth, "nt-divider-full-width")]
    [InlineData(NTDividerVariant.Inset, "nt-divider-inset")]
    [InlineData(NTDividerVariant.MiddleInset, "nt-divider-middle-inset")]
    public void Variant_Adds_Horizontal_Layout_Class(NTDividerVariant variant, string expectedClass) {
        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.Variant, variant));

        // Assert
        cut.Find("div.nt-divider").GetAttribute("class")!.Should().Contain(expectedClass);
    }

    [Fact]
    public void Color_Can_Be_Overridden() {
        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.Color, NTColor.Primary));

        // Assert
        cut.Find("div.nt-divider").GetAttribute("style")!.Should().Contain("--nt-divider-color:var(--nt-color-primary)");
    }

    [Fact]
    public void Null_Color_Does_Not_Emit_Color_Variable() {
        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.Color, null));

        // Assert
        cut.Find("div.nt-divider").GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void AutoFocus_Is_Not_Rendered() {
        // Act
        var cut = Render<NTDivider>(p => p.Add(c => c.AutoFocus, true));

        // Assert
        cut.Find("div.nt-divider").HasAttribute("autofocus").Should().BeFalse();
    }

    [Fact]
    public void Base_Metadata_Attributes_Are_Rendered() {
        // Act
        var cut = Render<NTDivider>(p => p
            .Add(c => c.ElementId, "divider-id")
            .Add(c => c.ElementTitle, "Section separator")
            .Add(c => c.ElementLang, "en"));
        var divider = cut.Find("div.nt-divider");

        // Assert
        divider.GetAttribute("id").Should().Be("divider-id");
        divider.GetAttribute("title").Should().Be("Section separator");
        divider.GetAttribute("lang").Should().Be("en");
        divider.HasAttribute("tntid").Should().BeTrue();
    }
}
