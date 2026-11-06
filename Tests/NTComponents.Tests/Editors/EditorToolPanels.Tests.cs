namespace NTComponents.Tests.Editors;

public class EditorToolPanels_Tests : BunitContext {

    public EditorToolPanels_Tests() {
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Static", false));
        foreach (var path in new[] { "./_content/NTComponents/Menus/NTContextMenu.razor.js", "./_content/NTComponents/Menus/NTMenu.razor.js" }) {
            var menuModule = JSInterop.SetupModule(path);
            menuModule.SetupVoid("onLoad", _ => true).SetVoidResult();
            menuModule.SetupVoid("onUpdate", _ => true).SetVoidResult();
            menuModule.SetupVoid("onDispose", _ => true).SetVoidResult();
        }
    }

    [Fact]
    public void ImagePanel_Renders_Expected_Interactive_Fields() {
        var cut = Render<EditorToolImagePanel>(parameters => parameters.Add(x => x.Disabled, false));

        cut.Find("[data-tool-command='image']").GetAttribute("role").Should().Be("group");
        cut.Find("[data-tool-command='image']").GetAttribute("aria-label").Should().Be("Image settings");
        cut.Find("[data-role='image-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='image-file']").GetAttribute("type").Should().Be("file");
        cut.Find("[data-role='image-file']").GetAttribute("accept").Should().Be("image/*");
        cut.Find("[data-role='image-alt']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='image-width']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='image-height']").GetAttribute("type").Should().Be("number");
        cut.Find("[data-role='image-apply']").TextContent.Should().Contain("Insert image");
        cut.Find("[data-role='image-cancel']").TextContent.Should().Contain("Cancel");
        AssertToolInputsAreNotSubmitted(cut);
        cut.FindAll("[disabled]").Should().BeEmpty();
    }

    [Fact]
    public void ImagePanel_Disables_All_Controls_When_Disabled() {
        var cut = Render<EditorToolImagePanel>(parameters => parameters.Add(x => x.Disabled, true));

        cut.FindAll("input, button").Should().OnlyContain(element => element.HasAttribute("disabled"));
    }

    [Fact]
    public void TablePanel_Renders_Expected_Interactive_Fields() {
        var cut = Render<EditorToolTablePanel>(parameters => parameters.Add(x => x.Disabled, false));

        cut.Find("[data-tool-command='table']").GetAttribute("role").Should().Be("group");
        cut.Find("[data-tool-command='table']").GetAttribute("aria-label").Should().Be("Table settings");
        cut.FindAll("[data-role='table-grid'] button[data-col][data-row]").Should().HaveCount(80);
        cut.Find("[data-role='table-grid'] button[data-col='3'][data-row='2']").GetAttribute("aria-label").Should().Be("Insert 3 by 2 table");
        cut.FindAll("[data-role='table-grid'] button[tabindex='0']").Should().ContainSingle();
        cut.FindAll("[data-role='table-columns'], [data-role='table-rows']").Should().BeEmpty();
        cut.Find("[data-role='table-border-color']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='table-apply']").TextContent.Should().Contain("Apply table");
        cut.Find("[data-role='table-cancel']").TextContent.Should().Contain("Cancel");
        AssertToolInputsAreNotSubmitted(cut);
        cut.FindAll("[disabled]").Should().BeEmpty();
    }

    [Fact]
    public void TablePanel_Disables_All_Controls_When_Disabled() {
        var cut = Render<EditorToolTablePanel>(parameters => parameters.Add(x => x.Disabled, true));

        cut.FindAll("[data-tool-command='table'] input, [data-tool-command='table'] button").Should().OnlyContain(element => element.HasAttribute("disabled"));
    }

    [Fact]
    public void TablePanel_Renders_Context_Menu_Targeting_Table_Cells_Of_The_Surface() {
        var cut = Render<EditorToolTablePanel>(parameters => parameters.Add(x => x.Disabled, false).AddCascadingValue("RichTextEditorSurfaceId", "editor-surface"));

        var menu = cut.Find("nt-context-menu");
        menu.GetAttribute("data-target-id").Should().Be("editor-surface");
        menu.GetAttribute("data-target-selector").Should().Be("td, th");
        cut.FindAll("[data-table-action]").Select(item => item.GetAttribute("data-table-action")).Should().Equal("row-above", "row-below", "column-left", "column-right", "delete-row", "delete-column", "delete-table");
    }

    [Fact]
    public void TextColorPanel_Renders_Expected_Interactive_Fields() {
        var cut = Render<EditorToolTextColorPanel>(parameters => parameters.Add(x => x.Disabled, false));

        cut.Find("[data-tool-command='textColor']").GetAttribute("role").Should().Be("group");
        cut.Find("[data-tool-command='textColor']").GetAttribute("aria-label").Should().Be("Text color settings");
        cut.Find("[data-role='text-color-value']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='text-color-apply']").TextContent.Should().Contain("Apply color");
        cut.Find("[data-role='text-color-cancel']").TextContent.Should().Contain("Cancel");
        AssertToolInputsAreNotSubmitted(cut);
        cut.FindAll("[disabled]").Should().BeEmpty();
    }

    [Fact]
    public void TextColorPanel_Disables_All_Controls_When_Disabled() {
        var cut = Render<EditorToolTextColorPanel>(parameters => parameters.Add(x => x.Disabled, true));

        cut.FindAll("input, button").Should().OnlyContain(element => element.HasAttribute("disabled"));
    }

    [Fact]
    public void LinkPanel_Renders_Expected_Interactive_Fields() {
        var cut = Render<EditorToolLinkPanel>(parameters => parameters.Add(x => x.Disabled, false));

        cut.Find("[data-tool-command='link']").GetAttribute("role").Should().Be("group");
        cut.Find("[data-tool-command='link']").GetAttribute("aria-label").Should().Be("Link settings");
        cut.Find("[data-role='link-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='link-text']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='link-apply']").TextContent.Should().Contain("Apply link");
        cut.Find("[data-role='link-cancel']").TextContent.Should().Contain("Cancel");
        AssertToolInputsAreNotSubmitted(cut);
        cut.FindAll("[disabled]").Should().BeEmpty();
    }

    [Fact]
    public void LinkPanel_Disables_All_Controls_When_Disabled() {
        var cut = Render<EditorToolLinkPanel>(parameters => parameters.Add(x => x.Disabled, true));

        cut.FindAll("input, button").Should().OnlyContain(element => element.HasAttribute("disabled"));
    }

    [Fact]
    public void IframePanel_Renders_Expected_Interactive_Fields() {
        var cut = Render<EditorToolIframePanel>(parameters => parameters.Add(x => x.Disabled, false));

        cut.Find("[data-tool-command='iframe']").GetAttribute("role").Should().Be("group");
        cut.Find("[data-tool-command='iframe']").GetAttribute("aria-label").Should().Be("Iframe settings");
        cut.Find("[data-role='iframe-url']").GetAttribute("type").Should().Be("url");
        cut.Find("[data-role='iframe-title']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-width']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-height']").GetAttribute("type").Should().Be("text");
        cut.Find("[data-role='iframe-apply']").TextContent.Should().Contain("Apply iframe");
        cut.Find("[data-role='iframe-cancel']").TextContent.Should().Contain("Cancel");
        AssertToolInputsAreNotSubmitted(cut);
        cut.FindAll("[disabled]").Should().BeEmpty();
    }

    [Fact]
    public void IframePanel_Disables_All_Controls_When_Disabled() {
        var cut = Render<EditorToolIframePanel>(parameters => parameters.Add(x => x.Disabled, true));

        cut.FindAll("input, button").Should().OnlyContain(element => element.HasAttribute("disabled"));
    }

    private static void AssertToolInputsAreNotSubmitted<TComponent>(IRenderedComponent<TComponent> cut) where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        cut.FindAll("[data-tool-command] input").Should().OnlyContain(input => !input.HasAttribute("name"));
}
