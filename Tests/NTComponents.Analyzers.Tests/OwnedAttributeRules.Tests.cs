namespace NTComponents.Analyzers.Tests;

public sealed class OwnedAttributeRules_Tests {

    [Theory]
    [InlineData("NTTooltip", "role=\"note\"")]
    [InlineData("NTTooltip", "aria-hidden=\"false\"")]
    [InlineData("NTWindow", "role=\"alertdialog\"")]
    [InlineData("NTWindow", "aria-modal=\"true\"")]
    [InlineData("NTWindow", "aria-labelledby=\"x\"")]
    [InlineData("NTWindow", "tabindex=\"0\"")]
    [InlineData("NTWindowHost", "role=\"list\"")]
    [InlineData("NTWindowHost", "aria-label=\"Windows\"")]
    [InlineData("NTDivider", "role=\"presentation\"")]
    [InlineData("NTDivider", "aria-orientation=\"vertical\"")]
    [InlineData("NTTab", "role=\"region\"")]
    [InlineData("NTTab", "aria-labelledby=\"x\"")]
    [InlineData("NTTab", "tabindex=\"-1\"")]
    [InlineData("NTTab", "hidden=true")]
    [InlineData("NTBreadcrumb", "aria-label=\"Trail\"")]
    [InlineData("NTNavigationRail", "aria-label=\"Main\"")]
    [InlineData("NTSkeleton", "aria-hidden=\"false\"")]
    [InlineData("NTDialog", "aria-labelledby=\"x\"")]
    [InlineData("NTDialog", "aria-describedby=\"x\"")]
    [InlineData("NTNavigationRailItem", "aria-current=\"page\"")]
    [InlineData("NTNavigationRailItem", "aria-disabled=\"true\"")]
    [InlineData("NTNavigationRailItem", "aria-label=\"Home\"")]
    [InlineData("NTNavigationRailItem", "data-nt-navigation-rail-match=\"All\"")]
    [InlineData("NTNavigationRailItem", "data-nt-navigation-rail-selected=\"true\"")]
    [InlineData("NTNavigationRailItem", "name=\"n\"")]
    [InlineData("NTNavigationRailItem", "tabindex=\"3\"")]
    [InlineData("NTLayout", "id=\"x\"")]
    [InlineData("NTBody", "title=\"x\"")]
    [InlineData("NTHeader", "lang=\"fr\"")]
    [InlineData("NTFooter", "id=\"x\" | ElementId=\"y\"")]
    public async Task Owned_Attribute_Is_Reported(string component, string parameters) => Assert.Equal("NTC1109", await ComponentSnippet.IdsAsync(component, parameters, "NTC1109"));

    [Theory]
    [InlineData("NTTooltip", "class=\"x\" | data-x=\"1\" | style=\"color: red\"")]
    [InlineData("NTWindow", "data-x=\"1\" | Title=\"x\"")]
    [InlineData("NTDivider", "class=\"x\" | Direction=NTLayoutDirection.Vertical")]
    [InlineData("NTNavigationRail", "AriaLabel=\"Main\" | id=\"rail\"")]
    [InlineData("NTNavigationRailItem", "Label=\"Home\" | id=\"home\" | title=\"t\" | lang=\"en\" | class=\"x\" | data-x=\"1\"")]
    [InlineData("NTLayout", "ElementId=\"x\" | class=\"x\" | data-x=\"1\"")]
    [InlineData("NTDialog", "aria-label=\"x\" | data-x=\"1\"")]
    public async Task Other_Attributes_Are_Not_Reported(string component, string parameters) => Assert.Equal("", await ComponentSnippet.IdsAsync(component, parameters, "NTC1109"));

    [Fact]
    public async Task Each_Owned_Attribute_Is_Reported_Once() =>
        Assert.Equal("NTC1109 NTC1109", await ComponentSnippet.IdsAsync("NTTooltip", "role=\"note\" | aria-hidden=\"false\" | @", "NTC1109"));
}
