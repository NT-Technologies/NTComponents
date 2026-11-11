using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Site.Documentation;
using NTComponents.Site.Examples;
using System.Text.RegularExpressions;

namespace NTComponents.Tests.Site;

public class ComponentExamples_Tests : BunitContext {
    public ComponentExamples_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(_ => true).Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddNTServices();
        Services.AddNTDebugConsole(options => options.AllowAll());
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        // Chart pages host SkiaSharp views that need a browser canvas; stub them so the pages around them still render.
        ComponentFactories.AddStub(type => type.FullName is "SkiaSharp.Views.Blazor.SKGLView" or "SkiaSharp.Views.Blazor.SKCanvasView");
    }

    public static TheoryData<string> PageNames => [.. ComponentExamples.Pages.Keys.Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(PageNames))]
    public void Page_RendersItsExamplesWithoutErrors(string pageName) {
        var cut = Render(ComponentExamples.Pages[pageName]);

        cut.FindAll(".docs-example").Should().NotBeEmpty("every component page needs at least one hand-written example");
        cut.FindAll(".docs-example-failure").Select(alert => alert.TextContent).Should().BeEmpty();
        cut.FindAll(".docs-example-code code").Should().OnlyContain(code => !string.IsNullOrWhiteSpace(code.TextContent));
    }

    [Theory]
    [MemberData(nameof(PageNames))]
    public void Page_ExamplesUseTheComponentAndEveryDependent(string pageName) {
        var page = new DocumentationCatalog().Components.Single(component => component.Type.Name == pageName);
        var cut = Render(ComponentExamples.Pages[pageName]);
        var source = string.Join("\n", cut.FindAll(".docs-example-code code").Select(code => code.TextContent));

        page.DependentComponents.Prepend(page)
            .Select(component => component.Type.Name)
            // A tag may carry its namespace (<NTComponents.Charts.Core.NTTooltip) when two libraries share a component name.
            .Where(name => !Regex.IsMatch(source, $@"<(?:[\w.]+\.)?{Regex.Escape(name)}"))
            .Should().BeEmpty("each component documented on a page should appear in at least one of its examples");
    }

    [Fact]
    public void EveryPublicComponent_IsDocumentedOnAPage() {
        var documentedNames = new DocumentationCatalog().Components
            .SelectMany(page => page.DependentComponents.Prepend(page))
            .Select(component => component.Type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var publicComponentNames = typeof(NTButton).Assembly.ExportedTypes
            .Where(type => !type.IsAbstract && type.Name.StartsWith("NT", StringComparison.Ordinal) && typeof(IComponent).IsAssignableFrom(type))
            .Where(type => type != typeof(NTBadge) && !type.IsDefined(typeof(ObsoleteAttribute), inherit: false))
            .Select(type => type.Name.Split('`')[0]);

        publicComponentNames.Should().OnlyContain(name => documentedNames.Contains(name), "add a page to ComponentExamples or a parent to the catalog's dependent-component map");
    }
}
