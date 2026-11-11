using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Layout;

public class NTLayout_Tests : BunitContext {

    public NTLayout_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/NavRail/NTNavigationRail.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Layout_Renders_Shell_Div_With_Base_Class() {
        var cut = Render<NTLayout>(p => p.AddChildContent("Layout content"));

        var layout = cut.Find("div.nt-layout");

        layout.GetAttribute("class")!.Should().Contain("nt-components");
        layout.TextContent.Should().Contain("Layout content");
    }

    [Fact]
    public void Body_Renders_Main_Tag_By_Default() {
        var cut = Render<NTBody>(p => p.AddChildContent("Body content"));

        var body = cut.Find("main.nt-body");

        ShouldHaveScopedCssAttribute(body);
        body.GetAttribute("class")!.Should().Contain("nt-body-rounded");
        body.TextContent.Should().Contain("Body content");
    }

    [Fact]
    public void Body_Can_Disable_Rounded_Corners() {
        var cut = Render<NTBody>(p => p.Add(c => c.RoundedCorners, false).AddChildContent("Body content"));

        cut.Find("main.nt-body").GetAttribute("class")!.Should().NotContain("nt-body-rounded");
    }

    [Fact]
    public void Header_And_Footer_Render_Semantic_Tags_By_Default() {
        var header = Render<NTHeader>(p => p.AddChildContent("Header content"));
        var footer = Render<NTFooter>(p => p.AddChildContent("Footer content"));

        var headerElement = header.Find("header.nt-header");
        var footerElement = footer.Find("footer.nt-footer");

        ShouldHaveScopedCssAttribute(headerElement);
        headerElement.TextContent.Should().Contain("Header content");
        ShouldHaveScopedCssAttribute(footerElement);
        footerElement.TextContent.Should().Contain("Footer content");
    }

    [Fact]
    public void Layout_Regions_Can_Render_Explicit_Neutral_Tags() {
        var body = Render<NTBody>(p => p.Add(c => c.Tag, NTLayoutTag.Div).AddChildContent("Body content"));
        var header = Render<NTHeader>(p => p.Add(c => c.Tag, NTLayoutTag.Div).AddChildContent("Header content"));
        var footer = Render<NTFooter>(p => p.Add(c => c.Tag, NTLayoutTag.Div).AddChildContent("Footer content"));

        body.Find("div.nt-body").TextContent.Should().Contain("Body content");
        header.Find("div.nt-header").TextContent.Should().Contain("Header content");
        footer.Find("div.nt-footer").TextContent.Should().Contain("Footer content");
    }

    [Fact]
    public void Layout_Regions_Can_Render_Nested_Sectioning_Tags() {
        var cut = Render<NTBody>(p => p.Add(c => c.Tag, NTLayoutTag.Section).AddChildContent("Section content"));

        cut.Find("section.nt-body").TextContent.Should().Contain("Section content");
    }

    [Theory]
    [InlineData(NTLayoutTag.Article, "article")]
    [InlineData(NTLayoutTag.Aside, "aside")]
    [InlineData(NTLayoutTag.Div, "div")]
    [InlineData(NTLayoutTag.Footer, "footer")]
    [InlineData(NTLayoutTag.Header, "header")]
    [InlineData(NTLayoutTag.Main, "main")]
    [InlineData(NTLayoutTag.Section, "section")]
    public void Body_Can_Render_All_Supported_Tags(NTLayoutTag tag, string elementName) {
        var cut = Render<NTBody>(p => p.Add(c => c.Tag, tag).AddChildContent("Body content"));

        var body = cut.Find($"{elementName}.nt-body");

        ShouldHaveScopedCssAttribute(body);
        body.TextContent.Should().Contain("Body content");
    }

    [Theory]
    [InlineData(NTLayoutTag.Article, "article")]
    [InlineData(NTLayoutTag.Aside, "aside")]
    [InlineData(NTLayoutTag.Div, "div")]
    [InlineData(NTLayoutTag.Footer, "footer")]
    [InlineData(NTLayoutTag.Header, "header")]
    [InlineData(NTLayoutTag.Main, "main")]
    [InlineData(NTLayoutTag.Section, "section")]
    public void Footer_Can_Render_All_Supported_Tags(NTLayoutTag tag, string elementName) {
        var cut = Render<NTFooter>(p => p.Add(c => c.Tag, tag).AddChildContent("Footer content"));

        var footer = cut.Find($"{elementName}.nt-footer");

        ShouldHaveScopedCssAttribute(footer);
        footer.TextContent.Should().Contain("Footer content");
    }

    [Fact]
    public void Header_And_Footer_Default_To_Low_Elevation() {
        var header = Render<NTHeader>();
        var footer = Render<NTFooter>();

        header.Find("header.nt-header").GetAttribute("class")!.Should().Contain("nt-elevation-low");
        footer.Find("footer.nt-footer").GetAttribute("class")!.Should().Contain("nt-elevation-low");
    }

    [Fact]
    public void Header_And_Footer_Elevation_Can_Be_Overridden() {
        var header = Render<NTHeader>(p => p.Add(c => c.Elevation, NTElevation.None));
        var footer = Render<NTFooter>(p => p.Add(c => c.Elevation, NTElevation.High));

        header.Find("header.nt-header").GetAttribute("class")!.Should().Contain("nt-elevation-none");
        footer.Find("footer.nt-footer").GetAttribute("class")!.Should().Contain("nt-elevation-high");
    }

    [Fact]
    public void Header_And_Footer_Can_Render_Fixed_Position_State() {
        var header = Render<NTHeader>(p => p.Add(c => c.FixedPosition, true));
        var footer = Render<NTFooter>(p => p.Add(c => c.FixedPosition, true));

        header.Find("header.nt-header").GetAttribute("class")!.Should().Contain("nt-header-fixed-position");
        footer.Find("footer.nt-footer").GetAttribute("class")!.Should().Contain("nt-footer-fixed-position");
    }

    [Fact]
    public void AdditionalAttributes_Are_Merged() {
        var attrs = new Dictionary<string, object> {
            { "class", "custom-shell" },
            { "style", "min-height:42px" },
            { "data-testid", "layout" }
        };

        var cut = Render<NTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var layout = cut.Find("div.nt-layout");

        ShouldHaveScopedCssAttribute(layout);
        layout.GetAttribute("class")!.Should().Contain("custom-shell");
        layout.GetAttribute("style")!.Should().Contain("min-height:42px");
        layout.GetAttribute("data-testid")!.Should().Be("layout");
    }

    [Fact]
    public void Reserved_Attributes_Override_AdditionalAttributes() {
        var attrs = new Dictionary<string, object> {
            { "id", "attribute-id" },
            { "lang", "en" },
            { "title", "Attribute title" }
        };

        var cut = Render<NTLayout>(p => p
            .Add(c => c.AdditionalAttributes, attrs)
            .Add(c => c.ElementId, "parameter-id")
            .Add(c => c.ElementLang, "fr")
            .Add(c => c.ElementTitle, "Parameter title"));

        var layout = cut.Find("div.nt-layout");

        layout.GetAttribute("id").Should().Be("parameter-id");
        layout.GetAttribute("lang").Should().Be("fr");
        layout.GetAttribute("title").Should().Be("Parameter title");
    }

    [Fact]
    public void Null_Reserved_Attributes_Remove_AdditionalAttribute_Values() {
        var attrs = new Dictionary<string, object> {
            { "id", "attribute-id" },
            { "lang", "en" },
            { "title", "Attribute title" }
        };

        var cut = Render<NTLayout>(p => p.Add(c => c.AdditionalAttributes, attrs));
        var layout = cut.Find("div.nt-layout");

        layout.HasAttribute("id").Should().BeFalse();
        layout.HasAttribute("lang").Should().BeFalse();
        layout.HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void Explicit_Colors_Render_Component_Css_Variables() {
        var cut = Render<NTBody>(p => p
            .Add(c => c.BackgroundColor, NTColor.SurfaceContainerHigh)
            .Add(c => c.TextColor, NTColor.OnSurfaceVariant));

        var style = cut.Find("main.nt-body").GetAttribute("style")!;

        style.Should().Contain("--nt-body-background-color:var(--nt-color-surface-container-high)");
        style.Should().Contain("--nt-body-text-color:var(--nt-color-on-surface-variant)");
    }

    [Fact]
    public void Header_Explicit_Colors_Render_Header_Css_Variables() {
        var cut = Render<NTHeader>(p => p
            .Add(c => c.BackgroundColor, NTColor.PrimaryContainer)
            .Add(c => c.TextColor, NTColor.OnPrimaryContainer));

        var style = cut.Find("header.nt-header").GetAttribute("style")!;

        style.Should().Contain("--nt-header-background-color:var(--nt-color-primary-container)");
        style.Should().Contain("--nt-header-text-color:var(--nt-color-on-primary-container)");
    }

    [Fact]
    public void Footer_Explicit_Colors_Render_Footer_Css_Variables() {
        var cut = Render<NTFooter>(p => p
            .Add(c => c.BackgroundColor, NTColor.TertiaryContainer)
            .Add(c => c.TextColor, NTColor.OnTertiaryContainer));

        var style = cut.Find("footer.nt-footer").GetAttribute("style")!;

        style.Should().Contain("--nt-footer-background-color:var(--nt-color-tertiary-container)");
        style.Should().Contain("--nt-footer-text-color:var(--nt-color-on-tertiary-container)");
    }

    [Fact]
    public void Default_Colors_Do_Not_Render_Inline_Variables() {
        var cut = Render<NTHeader>();

        cut.Find("header.nt-header").GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void Nested_Layout_Composes_With_Navigation_Rail_And_Body() {
        var cut = Render<NTLayout>(p => p.AddChildContent(builder => {
            builder.OpenComponent<NTNavigationRail>(0);
            builder.AddAttribute(1, nameof(NTNavigationRail.AriaLabel), "Primary");
            builder.CloseComponent();
            builder.OpenComponent<NTLayout>(2);
            builder.AddAttribute(3, nameof(NTLayout.ChildContent), (RenderFragment)(innerBuilder => {
                innerBuilder.OpenComponent<NTHeader>(4);
                innerBuilder.AddAttribute(5, nameof(NTHeader.Tag), NTLayoutTag.Div);
                innerBuilder.AddAttribute(6, nameof(NTHeader.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(7, "App header")));
                innerBuilder.CloseComponent();
                innerBuilder.OpenComponent<NTBody>(8);
                innerBuilder.AddAttribute(9, nameof(NTBody.Tag), NTLayoutTag.Div);
                innerBuilder.AddAttribute(10, nameof(NTBody.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(11, "Page body")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }));

        cut.Find("nav.nt-navigation-rail").Should().NotBeNull();
        var layouts = cut.FindAll(".nt-layout");
        layouts[0].GetAttribute("class")!.Should().NotContain("nt-layout-nested");
        layouts[1].GetAttribute("class")!.Should().Contain("nt-layout-nested");
        cut.Find("div.nt-header").TextContent.Should().Contain("App header");
        cut.Find("div.nt-body").TextContent.Should().Contain("Page body");
    }

    private static void ShouldHaveScopedCssAttribute(IElement element) =>
        element.Attributes.Any(attribute => attribute.Name.StartsWith("b-", StringComparison.Ordinal)).Should().BeTrue();

}
