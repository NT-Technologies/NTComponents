using NTComponents.Core;

namespace NTComponents.Tests.Core;

/// <summary>
///     Unit tests for <see cref="CssStyleBuilder" />.
/// </summary>
public class CssStyleBuilder_Tests {

    [Fact]
    public void Add_WithEmptyString_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Add("").Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Add_WithMultipleStyleStrings_ConcatenatesStyles() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .Add("color: red")
            .Add("background: blue")
            .Build();

        // Assert
        result.Should().Be("color: red;background: blue;");
    }

    [Fact]
    public void Add_WithNullString_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Add(null!).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Add_WithStyleStringEndingSemicolon_DoesNotAddExtraSemicolon() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Add("color: red;").Build();

        // Assert
        result.Should().Be("color: red;");
    }

    [Fact]
    public void Add_WithValidStyleString_AddsStyleString() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Add("color: red").Build();

        // Assert
        result.Should().Be("color: red;");
    }

    [Fact]
    public void Add_WithWhitespaceString_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Add("   ").Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithNonStringStyleValue_ConvertsToString() {
        // Arrange
        var builder = CssStyleBuilder.Create();
        var attributes = new Dictionary<string, object> { ["style"] = 123 };

        // Act
        var result = builder.AddFromAdditionalAttributes(attributes).Build();

        // Assert
        result.Should().Be("123;");
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithNullAttributes_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddFromAdditionalAttributes(null).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithNullStyleValue_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();
        var attributes = new Dictionary<string, object> { ["style"] = null! };

        // Act
        var result = builder.AddFromAdditionalAttributes(attributes).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithoutStyleKey_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();
        var attributes = new Dictionary<string, object> { ["class"] = "test-class" };

        // Act
        var result = builder.AddFromAdditionalAttributes(attributes).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithStyleAttribute_AddsStyleString() {
        // Arrange
        var builder = CssStyleBuilder.Create();
        var attributes = new Dictionary<string, object> { ["style"] = "color: red; background: blue;" };

        // Act
        var result = builder.AddFromAdditionalAttributes(attributes).Build();

        // Assert
        result.Should().Be("color: red; background: blue;");
    }

    [Fact]
    public void AddFromAdditionalAttributes_WithStyleAttributeMissingSemicolon_AddsSemicolon() {
        // Arrange
        var builder = CssStyleBuilder.Create();
        var attributes = new Dictionary<string, object> { ["style"] = "color: red" };

        // Act
        var result = builder.AddFromAdditionalAttributes(attributes).Build();

        // Assert
        result.Should().Be("color: red;");
    }

    [Fact]
    public void AddStyle_WithDuplicateKey_OverwritesValue() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .AddStyle("color", "red")
            .AddStyle("color", "blue")
            .Build();

        // Assert
        result.Should().Be("color:blue;");
        result.Should().NotContain("red");
    }

    [Fact]
    public void AddStyle_WithEmptyKey_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("", "red").Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddStyle_WithEmptyValue_AddsKeyWithSemicolon() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("display", "").Build();

        // Assert
        result.Should().Be("display;");
    }

    [Fact]
    public void AddStyle_WithEnabledFalse_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color", "red", false).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddStyle_WithEnabledTrue_AddsStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color", "red", true).Build();

        // Assert
        result.Should().Be("color:red;");
    }

    [Fact]
    public void AddStyle_WithKeyAndValueHavingWhitespace_TrimsWhitespace() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("  color  ", "  red  ").Build();

        // Assert
        result.Should().Be("color:red;");
    }

    [Fact]
    public void AddStyle_WithKeyContainingColon_DoesNotAddExtraColon() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color:", "red").Build();

        // Assert
        result.Should().Be("color:red;");
    }

    [Fact]
    public void AddStyle_WithMultipleStyles_AddsAllStyles() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .AddStyle("color", "red")
            .AddStyle("background-color", "blue")
            .AddStyle("font-size", "14px")
            .Build();

        // Assert
        result.Should().Contain("color:red;");
        result.Should().Contain("background-color:blue;");
        result.Should().Contain("font-size:14px;");
    }

    [Fact]
    public void AddStyle_WithNullKey_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle(null, "red").Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddStyle_WithNullValue_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color", null).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddStyle_WithValidKeyValue_AddsStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color", "red").Build();

        // Assert
        result.Should().Be("color:red;");
    }

    [Fact]
    public void AddStyle_WithValueContainingSemicolon_DoesNotAddExtraSemicolon() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("color", "red;").Build();

        // Assert
        result.Should().Be("color:red;");
    }

    [Fact]
    public void AddStyle_WithWhitespaceKey_DoesNotAddStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("   ", "red").Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddStyle_WithWhitespaceValue_AddsKeyWithSemicolon() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddStyle("display", "   ").Build();

        // Assert
        result.Should().Be("display;");
    }

    [Fact]
    public void AddVariable_WithEmptyVariableName_CreatesEmptyVariable() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("", "value").Build();

        // Assert This tests the current behavior - empty variable names are allowed
        result.Should().Be("--:value;");
    }

    [Fact]
    public void AddVariable_WithEnabledFalse_DoesNotAddVariable() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("primary-color", "#ff0000", false).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddVariable_WithEnabledTrue_AddsVariable() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("primary-color", "#ff0000", true).Build();

        // Assert
        result.Should().Be("--primary-color:#ff0000;");
    }

    [Fact]
    public void AddVariable_WithMultipleVariables_AddsAllVariables() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .AddVariable("color1", "#ff0000")
            .AddVariable("color2", NTColor.Secondary)
            .AddVariable("size", "10px")
            .Build();

        // Assert
        result.Should().Contain("--color1:#ff0000;");
        result.Should().Contain("--color2:var(--nt-color-secondary);");
        result.Should().Contain("--size:10px;");
    }

    [Fact]
    public void AddVariable_WithNTColorAndEnabledFalse_DoesNotAddVariable() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("theme-color", NTColor.Primary, false).Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddVariable_WithNTColorOverload_AddsColorVariable() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("theme-color", NTColor.Primary).Build();

        // Assert
        result.Should().Be("--theme-color:var(--nt-color-primary);");
    }

    [Fact]
    public void AddVariable_WithValidNameValue_AddsVariableStyle() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.AddVariable("primary-color", "#ff0000").Build();

        // Assert
        result.Should().Be("--primary-color:#ff0000;");
    }

    [Fact]
    public void Build_MultipleCalls_ReturnsSameResult() {
        // Arrange
        var builder = CssStyleBuilder.Create()
            .AddStyle("color", "red")
            .AddVariable("size", "10px");

        // Act
        var result1 = builder.Build();
        var result2 = builder.Build();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Build_WithMixedEnabledDisabledStyles_ReturnsOnlyEnabledStyles() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .AddStyle("color", "red", true)
            .AddStyle("background", "blue", false)
            .AddVariable("size", "10px", true)
            .Build();

        // Assert
        result.Should().Contain("color:red;");
        result.Should().Contain("--size:10px;");
        result.Should().NotContain("background");
    }

    [Fact]
    public void Build_WithNoStyles_ReturnsNull() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder.Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Build_WithOnlyDisabledStyles_ReturnsNull() {
        // Arrange
        var builder = CssStyleBuilder.Create();

        // Act
        var result = builder
            .AddStyle("color", "red", false)
            .AddVariable("size", "10px", false)
            .Build();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Builder_ChainedCalls_ReturnsBuilderInstance() {
        // Arrange & Act
        var builder = CssStyleBuilder.Create();
        var chainedBuilder = builder
            .Add("color: red")
            .AddStyle("background", "blue")
            .AddVariable("size", "10px");

        // Assert
        builder.Should().BeSameAs(chainedBuilder);
    }

    [Fact]
    public void Builder_WithAllMethodTypes_CombinesAllStyles() {
        // Arrange
        var attributes = new Dictionary<string, object> { ["style"] = "margin: 10px" };

        // Act
        var result = CssStyleBuilder.Create()
            .Add("padding: 5px")
            .AddStyle("color", "red")
            .AddVariable("theme-color", NTColor.Primary)
            .AddVariable("custom-size", "20px")
            .AddFromAdditionalAttributes(attributes)
            .Build();

        // Assert
        result.Should().Contain("padding: 5px;");
        result.Should().Contain("color:red;");
        result.Should().Contain("--theme-color:var(--nt-color-primary);");
        result.Should().Contain("--custom-size:20px;");
        result.Should().Contain("margin: 10px;");
    }

    [Fact]
    public void Builder_WithComplexStylesAndVariables_GeneratesCorrectCss() {
        // Arrange & Act
        var result = CssStyleBuilder.Create()
            .Add("display: flex; align-items: center")
            .AddStyle("padding", "10px 20px")
            .AddStyle("border-radius", "4px")
            .AddVariable("primary-color", NTColor.Primary)
            .AddVariable("spacing", "8px")
            .AddVariable("elevation", "2")
            .Build();

        // Assert
        result.Should().Contain("display: flex; align-items: center;");
        result.Should().Contain("padding:10px 20px;");
        result.Should().Contain("border-radius:4px;");
        result.Should().Contain("--primary-color:var(--nt-color-primary);");
        result.Should().Contain("--spacing:8px;");
        result.Should().Contain("--elevation:2;");
    }

    [Fact]
    public void Builder_WithConditionalStyles_AddsOnlyWhenEnabled() {
        // Arrange
        bool isHighlighted = true;
        bool isHidden = false;

        // Act
        var result = CssStyleBuilder.Create()
            .AddStyle("background-color", "yellow", isHighlighted)
            .AddStyle("display", "none", isHidden)
            .AddVariable("highlight-color", "#ffff00", isHighlighted)
            .Build();

        // Assert
        result.Should().Contain("background-color:yellow;");
        result.Should().Contain("--highlight-color:#ffff00;");
        result.Should().NotContain("display");
    }

    [Fact]
    public void Builder_WithEmptyAndNullInputs_HandlesGracefully() {
        // Arrange & Act
        var result = CssStyleBuilder.Create()
            .Add("")
            .Add(null!)
            .AddStyle(null, "red")
            .AddStyle("color", null)
            .AddStyle("", "blue")
            .AddFromAdditionalAttributes(null)
            .Build();

        // Assert Empty and null inputs are filtered out, so result should be null
        result.Should().BeNull();
    }

    [Fact]
    public void Builder_WithStyleStringAndKeyValueStyles_MergesCorrectly() {
        // Arrange & Act
        var result = CssStyleBuilder.Create()
            .Add("color: red; background: blue")
            .AddStyle("font-size", "14px")
            .AddStyle("margin", "10px")
            .Build();

        // Assert
        result.Should().StartWith("color: red; background: blue;");
        result.Should().Contain("font-size:14px;");
        result.Should().Contain("margin:10px;");
    }

    [Fact]
    public void Create_MultipleCalls_ReturnsDifferentInstances() {
        // Arrange & Act
        var builder1 = CssStyleBuilder.Create();
        var builder2 = CssStyleBuilder.Create();

        // Assert
        builder1.Should().NotBeSameAs(builder2);
    }

    [Fact]
    public void Create_ReturnsNewBuilderInstance() {
        // Arrange & Act
        var builder = CssStyleBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<CssStyleBuilder>();
    }
}