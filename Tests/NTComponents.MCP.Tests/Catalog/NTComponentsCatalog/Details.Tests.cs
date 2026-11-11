using NTComponents.MCP.Catalog;

namespace NTComponents.MCP.Tests.Catalog;

public class Details_Tests {
    [Fact]
    public void GetComponent_ExcludesFrameworkInfrastructureMethods() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButtonGroup");

        component.Should().NotBeNull();
        component!.Methods.Should().NotContain(method => method.Name == "OnParametersSet" || method.Name == "SetParametersAsync" || method.Name == "Dispose");
    }

    [Fact]
    public void GetComponent_PreservesConsumerMethodsAndAccessibility() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTDialog");

        component.Should().NotBeNull();
        component!.Methods.Should().Contain(method => method.Name == "OpenAsync" && method.Accessibility == "Public");
        component.Methods.Should().NotContain(method => method.Name == "OnParametersSet" || method.Name == "OnAfterRenderAsync");
    }

    [Fact]
    public void GetComponent_IncludesParameterAccessibilityAndDeclaredDefaultExpression() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButton");

        component.Should().NotBeNull();
        component!.Parameters.Should().Contain(parameter => parameter.Name == "Label" && parameter.IsRequired && parameter.Accessibility == "Public");
        component.Parameters.Should().Contain(parameter => parameter.Name == "Variant" && parameter.Accessibility == "Public" && parameter.DefaultValueExpression == "NTButtonVariant.Filled");
        component.Parameters.Should().Contain(parameter => parameter.Name == "Elevation" && parameter.DefaultValueExpression == null);
    }

    [Fact]
    public void GetComponent_UsesDocumentedRazorExampleWhenAvailable() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTAutocomplete");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().Contain("<NTAutocomplete");
        component.RazorUsage.Should().Contain("@bind-Value=\"_city\"");
        component.RazorUsage.Should().Contain("<NTAutocompleteOptionGroup Label=\"Texas\">");
        component.RazorUsage.Should().NotContain("&lt;");
    }

    [Fact]
    public void GetComponent_GeneratesMinimalRazorUsageFromRequiredParameters() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTButton");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().StartWith("<NTButton");
        component.RazorUsage.Should().Contain("Label=\"TODO\"");
        component.RazorUsage.Should().EndWith("/>");
    }

    [Fact]
    public void GetComponent_OnlyExpandsRelatedEnumValuesWhenRequested() {
        var catalog = new NTComponentsCatalog();

        var summary = catalog.GetComponent("NTButton");
        var expanded = catalog.GetComponent("NTButton", includeRelatedEnumValues: true);

        summary.Should().NotBeNull();
        summary!.RelatedEnums.Should().BeEmpty();
        expanded.Should().NotBeNull();
        expanded!.RelatedEnums.Should().Contain(enumDetails => enumDetails.Name == "NTButtonVariant" && enumDetails.Values.Count > 0 && !enumDetails.IsTruncated);
        expanded.RelatedEnums.Should().Contain(enumDetails => enumDetails.Name == "NTColor" && enumDetails.Values.Count == 20 && enumDetails.TotalValueCount > 20 && enumDetails.IsTruncated);
    }

    [Fact]
    public void GetAccordion_IncludesCompositionAwareUsage() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTAccordion");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().Contain("<NTAccordionItem");
        component.RelatedComponents.Should().Contain(related => related.Name == "NTAccordionItem");
        component.UsageExamples.Should().Contain(example => example.Razor.Contains("<NTAccordionItem", StringComparison.Ordinal));
        component.DocumentationUrl.Should().Be("https://ntcomponents.nttechnologies.dev/components/ntaccordion");
    }

    [Fact]
    public void GetComponent_OrdersAndCategorizesParametersForConsumers() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTDataGrid");

        component.Should().NotBeNull();
        component!.Parameters.Should().BeInAscendingOrder(parameter => parameter.CategoryOrder);
        component.Parameters.Should().Contain(parameter => parameter.Name == "Items" && parameter.Category == "Data");
        component.Parameters.Should().Contain(parameter => parameter.Name == "ChildContent" && parameter.Category == "Content");
        component.Parameters.Should().Contain(parameter => parameter.Name.StartsWith("On", StringComparison.Ordinal) && parameter.Category == "Events");
        component.Parameters.Where(parameter => parameter.IsInherited).Should().OnlyContain(parameter => parameter.Category == "Inherited");
    }

    /// <summary>Behavior source: NTDataGrid documents FullHeight so consumers can fill a page layout without restyling the grid.</summary>
    [Fact]
    public void GetComponent_DataGrid_DocumentsFullHeightLayout() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTDataGrid");

        component.Should().NotBeNull();
        component!.Parameters.Should().ContainSingle(parameter => parameter.Name == "FullHeight" && parameter.Type == "bool" && parameter.Category == "Appearance"
            && parameter.Summary.Contains("fills the block size of its parent", StringComparison.Ordinal));
        component.Remarks.Should().Contain("FullHeight");
    }

    /// <summary>Behavior source: generated basic usage promises composition-aware required inputs, including Razor expressions for non-text content parameters.</summary>
    [Fact]
    public void GetComponent_WithRequiredContent_GeneratesRazorExpressionPlaceholder() {
        var catalog = new NTComponentsCatalog();

        var component = catalog.GetComponent("NTMenu");

        component.Should().NotBeNull();
        component!.RazorUsage.Should().Contain("AriaLabel=\"TODO\"");
        component.RazorUsage.Should().Contain("ChildContent=\"@childContent\"");
    }

    /// <summary>Behavior source: reference details promise enum values and the components that consume the reference type.</summary>
    [Fact]
    public void GetEnumReference_ReturnsPublicValuesAndComponentUsage() {
        var catalog = new NTComponentsCatalog();

        var reference = catalog.GetReference("NTButtonVariant");

        reference.Should().NotBeNull();
        reference!.Kind.Should().Be("Enum");
        reference.Fields.Should().Contain(field => field.Name == "Filled" && field.Value == "1");
        reference.Properties.Should().BeEmpty();
        reference.Methods.Should().BeEmpty();
        reference.UsedByComponents.Should().Contain("NTButton");
    }

    /// <summary>Behavior source: reference details promise public helper members, including callable methods, without presenting them as enum values.</summary>
    [Fact]
    public void GetHelperReference_ReturnsPublicMethods() {
        var catalog = new NTComponentsCatalog();

        var reference = catalog.GetReference("NTElevationExt");

        reference.Should().NotBeNull();
        reference!.Kind.Should().Be("Helper");
        reference.Fields.Should().BeEmpty();
        reference.Properties.Should().BeEmpty();
        reference.Methods.Should().Contain(method => method.Name == "ToCssClass" && method.Accessibility == "Public");
    }

    /// <summary>Behavior source: reference details promise public properties for broader library helper types and identify their LibraryApi scope.</summary>
    [Fact]
    public void GetClassReference_ReturnsPublicPropertiesAndLibraryScope() {
        var catalog = new NTComponentsCatalog();

        var reference = catalog.GetReference("NTComponentsDefaultOptions");

        reference.Should().NotBeNull();
        reference!.Kind.Should().Be("Helper");
        reference.Scope.Should().Be("LibraryApi");
        reference.Properties.Should().Contain(property => property.Name == "RichTextEditorMaxValueBytes" && property.Accessibility == "Public");
    }

    /// <summary>Behavior source: get_nt_reference_type promises public enum/helper members, so catalog projection must never expose non-consumer accessibility from generated metadata.</summary>
    [Fact]
    public void GetAllReferenceDetails_ExposeOnlyConsumerAccessibleMembers() {
        var catalog = new NTComponentsCatalog();
        var references = catalog.ListReferencePage(limit: 200).Items;

        var details = references.Select(reference => catalog.GetReference(reference.FullName)).ToArray();

        details.Should().NotContainNulls();
        details.SelectMany(reference => reference!.Properties).Should().OnlyContain(property => property.Accessibility == "Public" || property.Accessibility == "Protected" || property.Accessibility == "ProtectedOrInternal");
        details.SelectMany(reference => reference!.Methods).Should().OnlyContain(method => method.Accessibility == "Public" || method.Accessibility == "Protected" || method.Accessibility == "ProtectedOrInternal");
    }
}
