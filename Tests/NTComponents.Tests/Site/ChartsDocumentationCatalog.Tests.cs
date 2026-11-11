using NTComponents.Site.Documentation;

namespace NTComponents.Tests.Site;

public class ChartsDocumentationCatalog_Tests {
    private readonly DocumentationCatalog _catalog = new();

    private static readonly string[] _pageNames = ["NTChart", "NTLineSeries", "NTAreaSeries", "NTBarSeries", "NTPointSeries", "NTPieSeries", "NTRadarSeries", "NTTreeMapSeries", "NTBubblePackSeries", "NTHeatMapSeries", "NTBoxPlotSeries"];

    [Fact]
    public void ChartsGroup_HoldsTheChartAndEverySeriesPage() {
        var group = _catalog.ComponentGroups.Single(group => group.Key == "Charts");

        group.Select(page => page.Type.Name).Should().BeEquivalentTo(_pageNames);
        group.Should().OnlyContain(page => page.Type.FullName.StartsWith("NTComponents.Charts.", StringComparison.Ordinal));
    }

    [Fact]
    public void CoreComponents_StayOutOfTheChartsGroup() {
        _catalog.Components.Where(page => page.GroupName == "Charts").Should().OnlyContain(page => page.Type.FullName.StartsWith("NTComponents.Charts.", StringComparison.Ordinal));
        _catalog.Components.Single(page => page.Type.FullName == "NTComponents.NTTooltip").GroupName.Should().NotBe("Charts");
    }

    [Theory]
    [InlineData("NTChart", "NTLegend")]
    [InlineData("NTChart", "NTTooltip")]
    [InlineData("NTChart", "NTXAxisOptions")]
    [InlineData("NTChart", "NTYAxisOptions")]
    [InlineData("NTRadarSeries", "NTRadialAxisOptions")]
    public void Dependents_ResolveToTheirChartParent(string parentName, string dependentName) {
        var parent = _catalog.Components.Single(page => page.Type.Name == parentName);
        var dependent = parent.DependentComponents.Single(component => component.Type.Name == dependentName);

        dependent.Type.FullName.Should().StartWith("NTComponents.Charts.");
        dependent.GroupName.Should().Be("Charts");
        _catalog.GetComponent(dependent.Slug).Should().BeSameAs(parent);
    }

    [Fact]
    public void ChartsTooltip_DoesNotReplaceTheCoreTooltipPage() {
        var tooltip = _catalog.Components.Single(page => page.Type.FullName == "NTComponents.NTTooltip");

        _catalog.GetComponent(tooltip.Slug).Should().BeSameAs(tooltip);
        _catalog.Components.Where(page => page.Type.Name == "NTTooltip").Should().ContainSingle();
    }

    [Fact]
    public void ReferenceTypes_IncludeChartsEnumsUnderTheChartsFamily() {
        var charts = _catalog.EnumGroups.Single(group => group.Key == "Charts");

        charts.Select(reference => reference.Type.Name).Should().Contain(["LineStyle", "LegendPosition"]);
        charts.Should().OnlyContain(reference => reference.Type.FullName.StartsWith("NTComponents.Charts.", StringComparison.Ordinal));
    }
}
