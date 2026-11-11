using NTComponents.Charts.GeneratedDocumentation;

namespace NTComponents.Tests.Documentation;

public class ChartsCodeDocumentation_Tests {

    [Fact]
    public void Model_Documents_The_Charts_Assembly() {
        var model = GeneratedCodeDocumentation.Model;

        model.AssemblyName.Should().Be("NTComponents.Charts");
        model.Types.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("NTChart")]
    [InlineData("NTLineSeries")]
    [InlineData("NTAreaSeries")]
    [InlineData("NTBarSeries")]
    [InlineData("NTPointSeries")]
    [InlineData("NTPieSeries")]
    [InlineData("NTRadarSeries")]
    [InlineData("NTTreeMapSeries")]
    [InlineData("NTBubblePackSeries")]
    [InlineData("NTHeatMapSeries")]
    [InlineData("NTBoxPlotSeries")]
    public void Model_Contains_Generic_Chart_Components(string name) {
        var type = GeneratedCodeDocumentation.Model.Types.SingleOrDefault(x => x.Name == name);

        type.Should().NotBeNull();
        type!.FullName.Should().StartWith($"NTComponents.Charts.").And.EndWith("<TData>");
        type.Properties.Should().Contain(property => property.IsParameter);
    }

    [Fact]
    public void Model_Does_Not_Contain_Core_Components() {
        GeneratedCodeDocumentation.Model.Types.Should().NotContain(type => type.FullName.StartsWith("NTComponents.NT", StringComparison.Ordinal));
    }
}
