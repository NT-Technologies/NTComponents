namespace NTComponents.Tests.Editors;

public class EditorToolButtons_Tests : BunitContext {

    [Fact]
    public void ImageButton_Renders_Accessibility_Attributes() {
        var button = new NTRichTextEditorButton("image", "Insert image", icon: MaterialIcon.Image);
        var cut = Render<EditorToolImageButton>(parameters => parameters
            .Add(x => x.Button, button)
            .Add(x => x.Disabled, false));

        var buttonElement = cut.Find("button[data-command='image']");
        buttonElement.GetAttribute("type").Should().Be("button");
        buttonElement.GetAttribute("aria-label").Should().Be(button.AriaLabel);
        buttonElement.GetAttribute("aria-haspopup").Should().Be("dialog");
        buttonElement.GetAttribute("aria-expanded").Should().Be("false");
        buttonElement.GetAttribute("aria-pressed").Should().Be("false");
        buttonElement.QuerySelector(".nt-rich-text-editor-toolbar-icon")!.HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void TableButton_Renders_Accessibility_Attributes() {
        var button = new NTRichTextEditorButton("table", "Insert table", icon: MaterialIcon.TableChart);
        var cut = Render<EditorToolTableButton>(parameters => parameters
            .Add(x => x.Button, button)
            .Add(x => x.Disabled, false));

        var buttonElement = cut.Find("button[data-command='table']");
        buttonElement.GetAttribute("type").Should().Be("button");
        buttonElement.GetAttribute("aria-label").Should().Be(button.AriaLabel);
        buttonElement.GetAttribute("aria-haspopup").Should().Be("dialog");
        buttonElement.GetAttribute("aria-expanded").Should().Be("false");
        buttonElement.GetAttribute("aria-pressed").Should().Be("false");
        buttonElement.QuerySelector(".nt-rich-text-editor-toolbar-icon")!.HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void TextColorButton_Renders_Accessibility_Attributes() {
        var button = new NTRichTextEditorButton("textColor", "Text color", icon: MaterialIcon.FormatColorText);
        var cut = Render<EditorToolTextColorButton>(parameters => parameters
            .Add(x => x.Button, button)
            .Add(x => x.Disabled, false));

        var buttonElement = cut.Find("button[data-command='textColor']");
        buttonElement.GetAttribute("type").Should().Be("button");
        buttonElement.GetAttribute("aria-label").Should().Be(button.AriaLabel);
        buttonElement.GetAttribute("aria-haspopup").Should().Be("dialog");
        buttonElement.GetAttribute("aria-expanded").Should().Be("false");
        buttonElement.GetAttribute("aria-pressed").Should().Be("false");
        buttonElement.QuerySelector(".nt-rich-text-editor-toolbar-icon")!.HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void LinkButton_Renders_Accessibility_Attributes() {
        var button = new NTRichTextEditorButton("link", "Insert link", icon: MaterialIcon.Link);
        var cut = Render<EditorToolLinkButton>(parameters => parameters
            .Add(x => x.Button, button)
            .Add(x => x.Disabled, false));

        var buttonElement = cut.Find("button[data-command='link']");
        buttonElement.GetAttribute("type").Should().Be("button");
        buttonElement.GetAttribute("aria-label").Should().Be(button.AriaLabel);
        buttonElement.GetAttribute("aria-haspopup").Should().Be("dialog");
        buttonElement.GetAttribute("aria-expanded").Should().Be("false");
        buttonElement.GetAttribute("aria-pressed").Should().Be("false");
        buttonElement.QuerySelector(".nt-rich-text-editor-toolbar-icon")!.HasAttribute("title").Should().BeFalse();
    }

    [Fact]
    public void IframeButton_Renders_Accessibility_Attributes() {
        var button = new NTRichTextEditorButton("iframe", "Insert iframe", icon: MaterialIcon.WebAsset);
        var cut = Render<EditorToolIframeButton>(parameters => parameters
            .Add(x => x.Button, button)
            .Add(x => x.Disabled, false));

        var buttonElement = cut.Find("button[data-command='iframe']");
        buttonElement.GetAttribute("type").Should().Be("button");
        buttonElement.GetAttribute("aria-label").Should().Be(button.AriaLabel);
        buttonElement.GetAttribute("aria-haspopup").Should().Be("dialog");
        buttonElement.GetAttribute("aria-expanded").Should().Be("false");
        buttonElement.GetAttribute("aria-pressed").Should().Be("false");
        buttonElement.QuerySelector(".nt-rich-text-editor-toolbar-icon")!.HasAttribute("title").Should().BeFalse();
    }
}
