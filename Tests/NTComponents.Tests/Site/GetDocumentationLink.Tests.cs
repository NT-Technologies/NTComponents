using NTComponents.Site.Documentation;

namespace NTComponents.Tests.Site;

public class GetDocumentationLink_Tests {
    private static readonly DocumentationCatalog _catalog = new();

    // Reference pages list only fields, so a link to any other member of a reference type cannot use a member anchor.
    [Theory]
    [InlineData("P:NTComponents.NTDialogEventArgs.Cancel", "/reference/constants/ntcomponents-ntdialogeventargs", "Cancel")]
    [InlineData("P:NTComponents.NTFileUploadEventArgs.Stream", "/reference/constants/ntcomponents-ntfileuploadeventargs", "Stream")]
    [InlineData("P:NTComponents.NTIcon.Icon", "/reference/constants/ntcomponents-nticon", "Icon")]
    [InlineData("P:NTComponents.Charts.Core.NTChartAnnotation.CustomRenderer", "/reference/constants/ntcomponents-charts-core-ntchartannotation", "CustomRenderer")]
    public void NonFieldMemberOfReferenceType_LinksToTheTypePageWithoutAnchor(string cref, string expectedHref, string expectedLabel) {
        var link = _catalog.GetDocumentationLink(cref);

        link.Should().Be(new DocumentationLink(expectedHref, expectedLabel));
    }

    [Fact]
    public void FieldOfReferenceType_LinksToItsMemberAnchor() {
        var link = _catalog.GetDocumentationLink("F:NTComponents.NTColor.Primary");

        link.Should().Be(new DocumentationLink("/reference/enums/ntcomponents-ntcolor#primary", "Primary"));
    }
}
