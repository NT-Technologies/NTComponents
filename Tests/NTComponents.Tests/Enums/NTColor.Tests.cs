using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Enums;

public class NTColor_Tests {
    [Theory]
    [InlineData(NTColor.Primary, "var(--nt-color-primary)")]
    [InlineData(NTColor.OnPrimaryFixedVariant, "var(--nt-color-on-primary-fixed-variant)")]
    [InlineData(NTColor.Transparent, "var(--nt-color-transparent)")]
    public void Color_UsesNTThemeVariable(NTColor color, string expected) {
        color.ToCssNTColorVariable().Should().Be(expected);
    }

    [Fact]
    public void OptionalColorWithoutValue_ProducesNoOverride() {
        NTColor? color = null;
        color.ToCssNTColorVariable().Should().BeEmpty();
    }

}
