using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace NTComponents.IntegrationTests.Editors;

/// <summary>
///     Browser coverage for the rich text editor LiveTest surface across render modes and toolbar controls.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTRichTextEditor_IntegrationTests : IAsyncLifetime {
    private const int ExpectedDefaultToolbarButtonCount = 28;
    private const string ServerEditorTestId = "rich-text-editor-server";
    private readonly List<string> _browserDiagnostics = [];
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string AppBaseUrl = default!;

    public static IEnumerable<object[]> ToolbarControlCases() {
        yield return [new ToolbarControlCase("undo", null, "<p>Undo text</p>", "Undo text", "<p>Undo text</p>", PreparedValue: "Undo text</b>", PrepareUndoRedo: true)];
        yield return [new ToolbarControlCase("redo", null, "<p>Redo text</p>", "Redo text", "Redo text</b>", PrepareRedo: true)];
        yield return [new ToolbarControlCase("paragraph", null, "<h2>Paragraph text</h2>", "Paragraph text", "<p>Paragraph text</p>")];
        yield return [new ToolbarControlCase("heading", "1", "<p>Heading one</p>", "Heading one", "<h1>Heading one</h1>")];
        yield return [new ToolbarControlCase("heading", "2", "<p>Heading two</p>", "Heading two", "<h2>Heading two</h2>")];
        yield return [new ToolbarControlCase("heading", "3", "<p>Heading three</p>", "Heading three", "<h3>Heading three</h3>")];
        yield return [new ToolbarControlCase("heading", "4", "<p>Heading four</p>", "Heading four", "<h4>Heading four</h4>")];
        yield return [new ToolbarControlCase("heading", "5", "<p>Heading five</p>", "Heading five", "<h5>Heading five</h5>")];
        yield return [new ToolbarControlCase("heading", "6", "<p>Heading six</p>", "Heading six", "<h6>Heading six</h6>")];
        yield return [new ToolbarControlCase("alignLeft", null, "<p style=\"text-align:center;\">Left text</p>", "Left text", "Left text")];
        yield return [new ToolbarControlCase("alignCenter", null, "<p>Center text</p>", "Center text", "text-align: center")];
        yield return [new ToolbarControlCase("alignRight", null, "<p>Right text</p>", "Right text", "text-align: right")];
        yield return [new ToolbarControlCase("alignJustify", null, "<p>Justified text</p>", "Justified text", "text-align: justify")];
        yield return [new ToolbarControlCase("unorderedList", null, "<p>Bullet text</p>", "Bullet text", "<ul><li>Bullet text</li></ul>")];
        yield return [new ToolbarControlCase("orderedList", null, "<p>Numbered text</p>", "Numbered text", "<ol><li>Numbered text</li></ol>")];
        yield return [new ToolbarControlCase("blockquote", null, "<p>Quote text</p>", "Quote text", "<blockquote>")];
        yield return [new ToolbarControlCase("codeBlock", null, "<p>const value = 1;</p>", "const value = 1;", "<pre")];
        yield return [new ToolbarControlCase("table", null, "<p>Table anchor</p>", "Table anchor", null, ExpectedPanelRole: "table-editor")];
        yield return [new ToolbarControlCase("bold", null, "<p>Bold text</p>", "Bold text", "Bold text</b>")];
        yield return [new ToolbarControlCase("italic", null, "<p>Italic text</p>", "Italic text", "Italic text</i>")];
        yield return [new ToolbarControlCase("underline", null, "<p>Underline text</p>", "Underline text", "<u>Underline text</u>")];
        yield return [new ToolbarControlCase("strikeThrough", null, "<p>Strike text</p>", "Strike text", "Strike text</strike>")];
        yield return [new ToolbarControlCase("textColor", null, "<p>Color text</p>", "Color text", null, ExpectedPanelRole: "text-color-editor")];
        yield return [new ToolbarControlCase("link", null, "<p>Link text</p>", "Link text", null, ExpectedPanelRole: "link-editor")];
        yield return [new ToolbarControlCase("image", null, "<p>Image anchor</p>", "Image anchor", null, ExpectedPanelRole: "image-editor")];
        yield return [new ToolbarControlCase("iframe", null, "<p>Iframe anchor</p>", "Iframe anchor", null, ExpectedPanelRole: "iframe-editor")];
    }

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
        AppBaseUrl = _fixture.ServerAddress;

        _page.Console += (_, message) => _browserDiagnostics.Add($"console:{message.Type}:{message.Text}");
        _page.PageError += (_, error) => _browserDiagnostics.Add($"pageerror:{error}");
        _page.RequestFailed += (_, request) => _browserDiagnostics.Add($"requestfailed:{request.Url}:{request.Failure}");
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task RichTextEditor_LiveTest_Renders_FormV2_Shell_And_Syncs_Value_In_All_Render_Modes() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();

        foreach (var testId in new[] { "rich-text-editor-ssr", ServerEditorTestId, "rich-text-editor-wasm" }) {
            await WaitForEditorReadyAsync(testId);

            var root = EditorRoot(testId);
            var field = FieldRoot(testId);
            (await field.GetAttributeAsync("class")).Should().Contain("nt-input");
            (await field.GetAttributeAsync("class")).Should().Contain("nt-rich-text-editor");
            (await field.GetAttributeAsync("class")).Should().Contain("nt-input-outlined");
            (await root.Locator(".nt-rich-text-editor-toolbar-button").CountAsync()).Should().BeGreaterThanOrEqualTo(ExpectedDefaultToolbarButtonCount);
            (await root.Locator(".nt-input-outline").First.IsVisibleAsync()).Should().BeTrue();

            await SetEditorHtmlAndSelectTextAsync(testId, "<p>mode sync</p>", "mode sync");
            await WaitForHiddenValueAsync(testId, "mode sync");
        }
    }

    [Fact]
    public async Task ToolbarControls_Activate_Expected_Editor_Behavior() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);

        foreach (var values in ToolbarControlCases()) {
            var controlCase = (ToolbarControlCase)values[0];

            await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, controlCase.InitialHtml, controlCase.SelectionText);

            if (controlCase.PrepareUndoRedo) {
                await ClickToolbarButtonAsync(new("bold"));
                await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.PreparedValue!);
            }

            if (controlCase.PrepareRedo) {
                await ClickToolbarButtonAsync(new("bold"));
                await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.ExpectedValue!);
                await ClickToolbarButtonAsync(new("undo"));
                await WaitForHiddenValueAsync(ServerEditorTestId, "Redo text");
            }

            await ClickToolbarButtonAsync(controlCase);

            if (controlCase.ExpectedPanelRole is not null) {
                var panel = EditorRoot(ServerEditorTestId).Locator($"[data-role='{controlCase.ExpectedPanelRole}']");
                await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                (await panel.GetAttributeAsync("aria-hidden")).Should().Be("false");
                await CloseOpenPanelAsync(controlCase.ExpectedPanelRole);
                continue;
            }

            await WaitForHiddenValueAsync(ServerEditorTestId, controlCase.ExpectedValue!);
        }
    }

    [Theory]
    [InlineData("ul")]
    [InlineData("ol")]
    public async Task EnterInListItem_Inserts_New_List_Item(string listTag) {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, $"<{listTag}><li>First item</li></{listTag}>", "First item");

        await _page.Keyboard.PressAsync("End");
        await _page.Keyboard.PressAsync("Enter");
        await _page.Keyboard.TypeAsync("Second item");

        await WaitForHiddenValueAsync(ServerEditorTestId, $"<{listTag}><li>First item</li><li>Second item</li></{listTag}>");
    }

    [Fact]
    public async Task EnterAfterHeading_Creates_Paragraph() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<h2>Title|</h2>");

        await _page.Keyboard.PressAsync("Enter");
        await _page.Keyboard.TypeAsync("body");

        await WaitForHiddenValueAsync(ServerEditorTestId, "<h2>Title</h2><p>body</p>");
    }

    [Theory]
    [InlineData("blockquote", "<blockquote>quote|</blockquote>", "<p>quote</p>")]
    [InlineData("unorderedList", "<ul><li>item|</li></ul>", "<p>item</p>")]
    public async Task ToggleBlockOff_Yields_Paragraph(string command, string html, string expectedValue) {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, html);

        await ClickToolbarButtonAsync(new(command));

        await WaitForHiddenValueAsync(ServerEditorTestId, expectedValue);
        (await EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-hidden-input").InputValueAsync()).Should().Be(expectedValue);
    }

    [Fact]
    public async Task CodeBlock_Toggled_Off_After_Enter_Yields_Paragraphs() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>code|</p>");

        await ClickToolbarButtonAsync(new("codeBlock"));
        await _page.Keyboard.PressAsync("Enter");
        await _page.Keyboard.TypeAsync("line2");
        await ClickToolbarButtonAsync(new("codeBlock"));

        await WaitForHiddenValueAsync(ServerEditorTestId, "<p>code</p><p>line2</p>");
        (await EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-hidden-input").InputValueAsync()).Should().Be("<p>code</p><p>line2</p>");
    }

    [Fact]
    public async Task ListCreated_From_Paragraph_Keeps_Caret_For_Enter() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>item|</p>");

        await ClickToolbarButtonAsync(new("unorderedList"));
        await _page.Keyboard.PressAsync("Enter");
        await _page.Keyboard.TypeAsync("next");

        await WaitForHiddenValueAsync(ServerEditorTestId, "<ul><li>item</li><li>next</li></ul>");
    }

    [Fact]
    public async Task BackspaceMergingParagraphs_Keeps_Caret_At_Join() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>ab</p><p>|cd</p>");

        await _page.Keyboard.PressAsync("Backspace");
        await _page.Keyboard.TypeAsync("X");

        await WaitForHiddenValueAsync(ServerEditorTestId, "<p>abXcd</p>");
        (await EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-hidden-input").InputValueAsync()).Should().Be("<p>abXcd</p>");
    }

    [Fact]
    public async Task ClearedEditor_Reports_Empty_Value_And_Data_Empty() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>[gone]</p>");

        await _page.Keyboard.PressAsync("Backspace");

        await _page.WaitForFunctionAsync(
            """
            testId => {
                const section = document.querySelector(`[data-testid="${testId}"]`);
                const surface = section?.querySelector('.nt-rich-text-editor-surface');
                return section?.querySelector('.nt-rich-text-editor-hidden-input')?.value === ''
                    && surface?.dataset.empty === 'true'
                    && getComputedStyle(surface, '::before').content === `"${surface.dataset.placeholder}"`;
            }
            """,
            ServerEditorTestId,
            new PageWaitForFunctionOptions { Timeout = 5000 });
    }

    [Fact]
    public async Task TextColor_Across_Paragraphs_Keeps_Both_Paragraphs() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>on[e</p><p>tw]o</p>");

        await ClickToolbarButtonAsync(new("textColor"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-value']").FillAsync("#00ff00");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<p>on<span style=\"color: rgb(0, 255, 0);\">e</span></p><p><span style=\"color: rgb(0, 255, 0);\">tw</span>o</p>");
    }

    [Fact]
    public async Task PartialRecolor_Only_Changes_Selected_Text() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p><span style=\"color:#ff0000\">[hello] world</span></p>");

        await ClickToolbarButtonAsync(new("textColor"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-value']").FillAsync("#0000ff");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<span style=\"color: rgb(0, 0, 255);\">hello</span><span style=\"color: rgb(255, 0, 0);\"> world</span>");
    }

    [Fact]
    public async Task LinkPanel_Closes_After_Apply_And_Stays_Closed() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>go [here] now</p>");

        await ClickToolbarButtonAsync(new("link"));
        var root = EditorRoot(ServerEditorTestId);
        await root.Locator("[data-role='link-url']").FillAsync("https://a.example");
        await _page.Keyboard.PressAsync("Enter");

        await WaitForHiddenValueAsync(ServerEditorTestId, "<a href=\"https://a.example\">here</a>");
        (await root.Locator("[data-role='link-editor']").IsHiddenAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ClearingLinkUrl_Unlinks_Text() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p><a href=\"https://a.example\">li|nk</a></p>");

        await ClickToolbarButtonAsync(new("link"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-url']").FillAsync(string.Empty);
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<p>link</p>");
        (await EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-hidden-input").InputValueAsync()).Should().Be("<p>link</p>");
    }

    [Fact]
    public async Task EditingLinkUrl_Keeps_Inline_Formatting() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p><a href=\"https://a.com\"><strong>x|y</strong></a></p>");

        await ClickToolbarButtonAsync(new("link"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-url']").FillAsync("https://b.com");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<a href=\"https://b.com\"><strong>xy</strong></a>");
    }

    [Fact]
    public async Task BareDomainLink_Gets_Https_Scheme() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>[rel]</p>");

        await ClickToolbarButtonAsync(new("link"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-url']").FillAsync("example.com/page");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "href=\"https://example.com/page\"");
    }

    [Fact]
    public async Task Tab_Moves_Between_Table_Cells_Without_Opening_Table_Panel() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<table><tbody><tr><td>a|</td><td>b</td></tr></tbody></table>");

        await _page.Keyboard.PressAsync("Tab");
        await _page.Keyboard.TypeAsync("Z");

        await WaitForHiddenValueAsync(ServerEditorTestId, "<td>a</td><td>Z</td>");
        (await EditorRoot(ServerEditorTestId).Locator("[data-role='table-editor']").IsHiddenAsync()).Should().BeTrue();
    }

    public static TheoryData<ClickToEditCase> ClickToEditCases() => [
        new ClickToEditCase("image", "<p>pic <img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==\" alt=\"A\" width=\"40\" height=\"40\"> after</p>", "img", "image-alt", "A", "B", "alt=\"B\""),
        new ClickToEditCase("iframe", "<p>before</p><iframe src=\"https://example.com/embed\" title=\"Old\" width=\"100%\" height=\"200\" loading=\"lazy\"></iframe><p>after</p>", "iframe", "iframe-title", "Old", "New", "title=\"New\""),
        new ClickToEditCase("link", "<p>go <a href=\"https://a.example\">here</a> now</p>", "a", "link-url", "https://a.example", "https://b.example", "<a href=\"https://b.example\">here</a>"),
        new ClickToEditCase("table", "<table><thead><tr><th>Head</th></tr></thead><tbody><tr><td>cell</td></tr></tbody></table>", "td", "table-caption", "", "Cap", "<caption>Cap</caption>"),
        new ClickToEditCase("text-color", "<p>a <span style=\"color: rgb(255, 0, 0);\">red</span> b</p>", "span", "text-color-value", "#ff0000", "#0000ff", "color: rgb(0, 0, 255);\">red</span>"),
        new ClickToEditCase("aria-label", "<p>a <span role=\"group\" aria-label=\"L1\">phrase</span> b</p>", "span", "aria-label-value", "L1", "L2", "aria-label=\"L2\"")
    ];

    [Theory]
    [MemberData(nameof(ClickToEditCases))]
    public async Task ClickingEditableElement_Opens_Its_Settings_And_Edits_It(ClickToEditCase testCase) {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, testCase.Html);
        var root = EditorRoot(ServerEditorTestId);
        var target = root.Locator($".nt-rich-text-editor-surface {testCase.TargetSelector}").First;

        // Click by coordinates: an editable iframe ignores pointer events, which Playwright's element click refuses.
        var box = await target.BoundingBoxAsync();
        await _page.Mouse.ClickAsync(box!.X + (box.Width / 2), box.Y + (box.Height / 2));

        var input = root.Locator($"[data-role='{testCase.InputRole}']");
        await Assertions.Expect(input).ToBeVisibleAsync();
        await Assertions.Expect(input).ToHaveValueAsync(testCase.PrefilledValue);
        (await _page.EvaluateAsync<bool>("() => document.activeElement?.classList.contains('nt-rich-text-editor-surface') === true")).Should().BeTrue();

        await input.FillAsync(testCase.NewValue);
        await input.PressAsync("Enter");

        await WaitForHiddenValueAsync(ServerEditorTestId, testCase.ExpectedValue);
        (await root.Locator($".nt-rich-text-editor-surface {testCase.TargetSelector}").CountAsync()).Should().Be(1);
        await Assertions.Expect(input).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ClickingPlainText_Closes_Settings_Opened_By_A_Click() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>go <a href=\"https://a.example\">here</a></p><p>plain text</p>");
        var root = EditorRoot(ServerEditorTestId);
        var linkUrl = root.Locator("[data-role='link-url']");

        await root.Locator(".nt-rich-text-editor-surface a").ClickAsync();
        await Assertions.Expect(linkUrl).ToBeVisibleAsync();
        await root.Locator(".nt-rich-text-editor-surface p").Nth(1).ClickAsync();

        await Assertions.Expect(linkUrl).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ChosenImageFile_Is_Inserted_Only_When_Insert_Is_Clicked() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>picture here|</p>");
        var root = EditorRoot(ServerEditorTestId);
        await ClickToolbarButtonAsync(new("image"));

        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        await root.Locator("[data-role='image-file']").SetInputFilesAsync(new FilePayload { Name = "dot.png", MimeType = "image/png", Buffer = png });

        await Assertions.Expect(root.Locator("[data-role='image-alt']")).ToHaveValueAsync("dot");
        await Assertions.Expect(root.Locator("[data-role='image-url']")).ToHaveValueAsync(string.Empty);
        (await root.Locator(".nt-rich-text-editor-surface img").CountAsync()).Should().Be(0);

        await root.Locator("[data-role='image-apply']").ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<img src=\"data:image/png;base64,iVBORw0KGgo");
    }

    [Fact]
    public async Task StaticSsrImagePanel_Shows_Chosen_File_And_Resets_When_Reopened() {
        ArgumentNullException.ThrowIfNull(_page);

        const string ssrEditorTestId = "rich-text-editor-ssr";
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ssrEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ssrEditorTestId, "<p>picture here|</p>");
        var root = EditorRoot(ssrEditorTestId);
        var imageButton = root.Locator(".nt-rich-text-editor-toolbar-button[data-command='image']");
        var chosenFile = root.Locator("[data-tool-command='image'] .nt-file-upload-value");

        await imageButton.ClickAsync();
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        await root.Locator("[data-role='image-file']").SetInputFilesAsync(new FilePayload { Name = "dot.png", MimeType = "image/png", Buffer = png });
        await Assertions.Expect(chosenFile).ToHaveTextAsync("dot.png");

        await root.Locator("[data-role='image-cancel']").ClickAsync();
        await imageButton.ClickAsync();
        await Assertions.Expect(chosenFile).ToHaveTextAsync("No image selected");
    }

    [Fact]
    public async Task HtmlSource_Collapses_Embedded_Image_Data_But_Saves_It() {
        ArgumentNullException.ThrowIfNull(_page);

        var data = new string('A', 4096);
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, $"<p>Pic| <img src=\"data:image/png;base64,{data}\" alt=\"Dot\"></p>");
        var source = EditorRoot(ServerEditorTestId).Locator("[data-role='html-source']");

        await ClickToolbarButtonAsync(new("editHtml"));
        var shown = await source.InputValueAsync();
        await source.FillAsync(shown.Replace("Pic", "Edited", StringComparison.Ordinal));
        await ClickToolbarButtonAsync(new("editHtml"));

        shown.Should().Contain("data:image/png;base64,[image-data-1, 3 KB]").And.NotContain(data);
        await WaitForHiddenValueAsync(ServerEditorTestId, $"<p>Edited <img src=\"data:image/png;base64,{data}\" alt=\"Dot\"></p>");
    }

    [Fact]
    public async Task EscapeOnPanelButton_Closes_Panel() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>[x]</p>");

        await ClickToolbarButtonAsync(new("link"));
        var linkPanel = EditorRoot(ServerEditorTestId).Locator("[data-role='link-editor']");
        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").FocusAsync();
        await _page.Keyboard.PressAsync("Escape");

        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    [Fact]
    public async Task ToolPanelControls_Insert_Expected_Rich_Content_And_Cancel_Closes_Panels() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);

        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, "<p>Link text</p>", "Link text");
        await ClickToolbarButtonAsync(new("link"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-url']").FillAsync("https://example.com/docs");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<a href=\"https://example.com/docs\">Link text</a>");

        await ClickToolbarButtonAsync(new("image"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-url']").FillAsync("https://example.com/image.png");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-alt']").FillAsync("Architecture diagram");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='image-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<img src=\"https://example.com/image.png\" alt=\"Architecture diagram\"");

        await ClickToolbarButtonAsync(new("table"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='table-grid'] button[data-col='2'][data-row='2']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<table data-border-color=\"#94a3b8\"");

        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, "<p>Color text</p>", "Color text");
        await ClickToolbarButtonAsync(new("textColor"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-value']").FillAsync("#123456");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='text-color-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "color: rgb(18, 52, 86);\">Color text</span>");

        await ClickToolbarButtonAsync(new("iframe"));
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-url']").FillAsync("https://example.com/embed");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-title']").FillAsync("Embedded content");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-width']").FillAsync("100%");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-height']").FillAsync("315");
        await EditorRoot(ServerEditorTestId).Locator("[data-role='iframe-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "<iframe src=\"https://example.com/embed\"");

        await ClickToolbarButtonAsync(new("link"));
        var linkPanel = EditorRoot(ServerEditorTestId).Locator("[data-role='link-editor']");
        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await EditorRoot(ServerEditorTestId).Locator("[data-role='link-cancel']").ClickAsync();
        await linkPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    private const string TableSeedHtml = "<table><thead><tr><th>H1</th><th>H2</th></tr></thead><tbody><tr><td>A1</td><td>A2</td></tr></tbody></table><p>After</p>";

    private ILocator TableMenuItem(string action) => EditorRoot(ServerEditorTestId).Locator($"[data-table-action='{action}']");

    private async Task RightClickTableCellAndChooseAsync(string cellText, string action) {
        var cell = EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface td", new LocatorLocatorOptions { HasTextString = cellText });
        await cell.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        var item = TableMenuItem(action);
        await item.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await item.ClickAsync();
    }

    [Fact]
    public async Task TableGrid_Inserts_Table_Of_The_Hovered_Size() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>Text|</p>");

        await ClickToolbarButtonAsync(new("table"));
        var cell = EditorRoot(ServerEditorTestId).Locator("[data-role='table-grid'] button[data-col='3'][data-row='2']");
        await cell.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        await cell.HoverAsync();
        await Assertions.Expect(EditorRoot(ServerEditorTestId).Locator("[data-role='table-grid-label']")).ToHaveTextAsync("3 × 2");
        await cell.ClickAsync();

        await WaitForHiddenValueAsync(ServerEditorTestId, "<table data-border-color=\"#94a3b8\"");
        var surface = EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface");
        await Assertions.Expect(surface.Locator("thead th")).ToHaveCountAsync(3);
        await Assertions.Expect(surface.Locator("tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(surface.Locator("tbody td")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task TableContextMenu_Inserts_Row_Below() {
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, TableSeedHtml);

        await RightClickTableCellAndChooseAsync("A1", "row-below");

        var surface = EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface");
        await Assertions.Expect(surface.Locator("tr")).ToHaveCountAsync(3);
        await Assertions.Expect(surface.Locator("tbody tr")).ToHaveCountAsync(2);
        await WaitForHiddenValueAsync(ServerEditorTestId, "<td><br></td>");
    }

    [Fact]
    public async Task TableContextMenu_Inserts_Column_Right_In_Every_Row() {
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, TableSeedHtml);

        await RightClickTableCellAndChooseAsync("A1", "column-right");

        var surface = EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface");
        await Assertions.Expect(surface.Locator("thead th")).ToHaveCountAsync(3);
        await Assertions.Expect(surface.Locator("tbody td")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task TableContextMenu_Deletes_Table() {
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, TableSeedHtml);

        await RightClickTableCellAndChooseAsync("A2", "delete-table");

        await Assertions.Expect(EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface table")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RightClick_Outside_A_Table_Does_Not_Open_The_Table_Menu() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlWithSelectionAsync(ServerEditorTestId, "<p>Plain paragraph|</p>");

        var prevented = await _page.EvaluateAsync<bool>(
            """
            (testId) => {
                const paragraph = document.querySelector(`[data-testid="${testId}"] .nt-rich-text-editor-surface p`);
                const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
                paragraph.dispatchEvent(event);
                return event.defaultPrevented;
            }
            """,
            ServerEditorTestId);

        prevented.Should().BeFalse();
        await EditorRoot(ServerEditorTestId).Locator(".nt-rich-text-editor-surface p").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        (await TableMenuItem("row-below").IsVisibleAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Accessible_Label_And_Html_Source_Controls_Edit_And_Save_Html() {
        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await SetEditorHtmlAndSelectTextAsync(ServerEditorTestId, "<p>Tagged text</p>", "Tagged");

        await ClickToolbarButtonAsync(new("ariaLabel"));
        var root = EditorRoot(ServerEditorTestId);
        await root.Locator("[data-role='aria-label-value']").FillAsync("Important phrase");
        await root.Locator("[data-role='aria-label-apply']").ClickAsync();
        await WaitForHiddenValueAsync(ServerEditorTestId, "aria-label=\"Important phrase\"");
        (await root.Locator(".nt-rich-text-editor-surface span[aria-label]").GetAttributeAsync("role")).Should().Be("group");

        await ClickToolbarButtonAsync(new("editHtml"));
        (await root.Locator(".nt-rich-text-editor-surface").IsVisibleAsync()).Should().BeFalse();
        (await root.Locator("[data-role='html-source']").IsVisibleAsync()).Should().BeTrue();
        (await root.Locator("[data-command='bold']").IsEnabledAsync()).Should().BeFalse();
        await root.Locator("[data-role='html-source']").FillAsync("<div>\n  <p>Edited <strong>HTML</strong></p>\n  <p>Second line</p>\n</div><script>bad()</script>");
        await ClickToolbarButtonAsync(new("editHtml"));
        const string savedHtml = "<div><p>Edited <strong>HTML</strong></p><p>Second line</p></div>";
        await WaitForHiddenValueAsync(ServerEditorTestId, savedHtml);
        (await root.Locator(".nt-rich-text-editor-surface").IsVisibleAsync()).Should().BeTrue();
        (await root.Locator(".nt-rich-text-editor-hidden-input").InputValueAsync()).Should().Be(savedHtml);
        await ClickToolbarButtonAsync(new("editHtml"));
        (await root.Locator("[data-role='html-source']").InputValueAsync()).Should().Contain("\n  <p>Edited <strong>HTML</strong></p>\n");
    }

    [Fact]
    public async Task Html_Source_Grows_With_Content_Up_To_Eighty_Percent_Of_Viewport() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(ServerEditorTestId);
        await ClickToolbarButtonAsync(new("editHtml"));

        var source = EditorRoot(ServerEditorTestId).Locator("[data-role='html-source']");
        await source.FillAsync("<p>Short</p>");
        var highlightingIsAligned = await source.EvaluateAsync<bool>(
            """
            source => {
                const highlight = source.parentElement.querySelector('.nt-rich-text-editor-source-highlight');
                const code = highlight.querySelector('code');
                const sourceStyle = getComputedStyle(source);
                const highlightStyle = getComputedStyle(highlight);
                return highlight.getAttribute('aria-hidden') === 'true'
                    && code.textContent === source.value
                    && code.clientWidth === source.clientWidth
                    && sourceStyle.fontFamily === highlightStyle.fontFamily
                    && sourceStyle.fontSize === highlightStyle.fontSize
                    && sourceStyle.lineHeight === highlightStyle.lineHeight
                    && sourceStyle.letterSpacing === highlightStyle.letterSpacing
                    && getComputedStyle(code.querySelector('.syntax-tag')).color !== highlightStyle.color;
            }
            """);
        highlightingIsAligned.Should().BeTrue();
        var shortHeight = await source.EvaluateAsync<double>("element => element.getBoundingClientRect().height");

        await source.FillAsync(string.Join('\n', Enumerable.Repeat("<p>Some HTML content</p>", 12)));
        var mediumHeight = await source.EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        mediumHeight.Should().BeGreaterThan(shortHeight);

        await source.FillAsync(string.Join('\n', Enumerable.Repeat("<p>Some HTML content</p>", 100)));
        var cappedHeight = await source.EvaluateAsync<double>("element => element.getBoundingClientRect().height");
        var viewportCap = await _page.EvaluateAsync<double>("() => window.innerHeight * 0.8");
        var scrollHeight = await source.EvaluateAsync<int>("element => element.scrollHeight");
        cappedHeight.Should().BeApproximately(viewportCap, 1);
        scrollHeight.Should().BeGreaterThan((int)cappedHeight);
        await source.EvaluateAsync("element => { element.scrollTop = 150; element.dispatchEvent(new Event('scroll')); }");
        var highlightScrollsWithSource = await source.EvaluateAsync<bool>(
            """
            source => {
                const highlight = source.parentElement.querySelector('.nt-rich-text-editor-source-highlight');
                const code = highlight.querySelector('code');
                return Math.abs(code.getBoundingClientRect().top - highlight.getBoundingClientRect().top + source.scrollTop) < 1;
            }
            """);
        highlightScrollsWithSource.Should().BeTrue();

        await source.FillAsync("<p>Short</p>");
        (await source.EvaluateAsync<double>("element => element.getBoundingClientRect().height")).Should().BeApproximately(shortHeight, 1);
    }

    [Fact]
    public async Task Interactive_Render_Modes_Submit_Updated_Html() {
        ArgumentNullException.ThrowIfNull(_page);

        await NavigateToRichTextEditorAsync();

        foreach (var testId in new[] { ServerEditorTestId, "rich-text-editor-wasm" }) {
            await WaitForEditorReadyAsync(testId);
            await SetEditorHtmlAndSelectTextAsync(testId, "<p>Submitted HTML</p>", "Submitted HTML");
            await WaitForHiddenValueAsync(testId, "<p>Submitted HTML</p>");

            await Section(testId).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).ClickAsync();
            await ExpectStatusAsync(testId, "Form is valid.");
        }
    }

    [Theory]
    [InlineData(ServerEditorTestId)]
    [InlineData("rich-text-editor-wasm")]
    public async Task Value_Larger_Than_An_Interop_Message_Commits_And_Submits_Without_Disconnecting(string testId) {
        ArgumentNullException.ThrowIfNull(_page);
        var mode = testId.Replace("rich-text-editor-", "", StringComparison.Ordinal);
        var html = string.Concat(Enumerable.Range(0, 600).Select(i => $"<p>Paragraph {i}: a long line of text that makes the editor value large.</p>"));

        await NavigateToRichTextEditorAsync();
        await WaitForEditorReadyAsync(testId);
        await SetEditorHtmlWithSelectionAsync(testId, html);
        await Section(testId).Locator("header h2").ClickAsync();
        await Assertions.Expect(_page.GetByTestId($"{mode}-blur-status")).ToContainTextAsync("Editor blurred.", new LocatorAssertionsToContainTextOptions { Timeout = 10000 });
        await Assertions.Expect(_page.GetByTestId($"{mode}-html")).ToContainTextAsync("Paragraph 599:", new LocatorAssertionsToContainTextOptions { Timeout = 10000 });
        await Section(testId).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Submit" }).ClickAsync();

        await ExpectStatusAsync(testId, "Form is valid.");
        _browserDiagnostics.Should().NotContain(diagnostic => diagnostic.Contains("Connection disconnected", StringComparison.Ordinal));
    }

    private async Task NavigateToRichTextEditorAsync() {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.GotoAsync($"{AppBaseUrl}/rich-text-editor", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private ILocator Section(string testId) {
        ArgumentNullException.ThrowIfNull(_page);
        return _page.Locator($"[data-testid='{testId}']");
    }

    private ILocator EditorRoot(string testId) => Section(testId).Locator("nt-rich-text-editor");

    private ILocator FieldRoot(string testId) => Section(testId).Locator(".nt-input.nt-rich-text-editor");

    private async Task WaitForEditorReadyAsync(string testId) {
        ArgumentNullException.ThrowIfNull(_page);

        try {
            await _page.WaitForFunctionAsync(
                """
                ([testId, expectedToolbarButtonCount]) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                    return editor?.querySelectorAll('.nt-rich-text-editor-toolbar-button').length >= expectedToolbarButtonCount
                        && surface?.textContent?.includes('Meeting Notes');
                }
                """,
                new object[] { testId, ExpectedDefaultToolbarButtonCount },
                new PageWaitForFunctionOptions { Timeout = 30000 });
        }
        catch (TimeoutException ex) {
            var state = await _page.EvaluateAsync<string>(
                """
                (testId) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                    const pageScripts = Array.from(document.querySelectorAll('nt-page-script')).map(script => script.getAttribute('src'));
                    return JSON.stringify({
                        testId,
                        hasSection: Boolean(section),
                        hasEditor: Boolean(editor),
                        toolbarCount: editor?.querySelectorAll('.nt-rich-text-editor-toolbar-button').length ?? 0,
                        surfaceText: surface?.textContent?.slice(0, 160) ?? null,
                        surfaceHtml: surface?.innerHTML?.slice(0, 240) ?? null,
                        hiddenValue: editor?.querySelector('.nt-rich-text-editor-hidden-input')?.value?.slice(0, 160) ?? null,
                        pageScriptDefined: Boolean(customElements.get('nt-page-script')),
                        pageScripts
                    });
                }
                """,
                testId);
            throw new TimeoutException($"{ex.Message}{Environment.NewLine}{state}{Environment.NewLine}{string.Join(Environment.NewLine, _browserDiagnostics)}", ex);
        }
    }

    private async Task SetEditorHtmlAndSelectTextAsync(string testId, string html, string textToSelect) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync(
            """
            ([testId, html, textToSelect]) => {
                const section = document.querySelector(`[data-testid="${testId}"]`);
                const editor = section?.querySelector('nt-rich-text-editor');
                const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                if (!(surface instanceof HTMLElement)) {
                    throw new Error(`Editor surface not found for ${testId}`);
                }

                surface.innerHTML = html;
                surface.dataset.empty = surface.textContent?.trim() ? 'false' : 'true';
                surface.focus();

                const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT);
                let selected = false;
                while (walker.nextNode()) {
                    const node = walker.currentNode;
                    const index = node.textContent?.indexOf(textToSelect) ?? -1;
                    if (index < 0) {
                        continue;
                    }

                    const range = document.createRange();
                    range.setStart(node, index);
                    range.setEnd(node, index + textToSelect.length);
                    const selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(range);
                    selected = true;
                    break;
                }

                if (!selected) {
                    throw new Error(`Text '${textToSelect}' was not found for ${testId}`);
                }

                surface.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: null }));
            }
            """,
            new[] { testId, html, textToSelect });

        await WaitForHiddenValueAsync(testId, textToSelect);
    }

    // Markers in html: '|' places a collapsed caret, '[' and ']' select between them. Without markers the caret goes to the end.
    private async Task SetEditorHtmlWithSelectionAsync(string testId, string html) {
        ArgumentNullException.ThrowIfNull(_page);

        await _page.EvaluateAsync(
            """
            ([testId, html]) => {
                const section = document.querySelector(`[data-testid="${testId}"]`);
                const editor = section?.querySelector('nt-rich-text-editor');
                const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                if (!(surface instanceof HTMLElement)) {
                    throw new Error(`Editor surface not found for ${testId}`);
                }

                surface.innerHTML = html;
                surface.focus();

                const marks = [];
                const walker = document.createTreeWalker(surface, NodeFilter.SHOW_TEXT);
                const nodes = [];
                while (walker.nextNode()) {
                    nodes.push(walker.currentNode);
                }
                for (const node of nodes) {
                    let text = node.textContent;
                    for (let i = 0; i < text.length; i++) {
                        if ('|[]'.includes(text[i])) {
                            marks.push({ ch: text[i], node, offset: i });
                            text = text.slice(0, i) + text.slice(i + 1);
                            i--;
                        }
                    }
                    node.textContent = text;
                }

                const range = document.createRange();
                const caret = marks.find(m => m.ch === '|');
                const start = marks.find(m => m.ch === '[');
                const end = marks.find(m => m.ch === ']');
                if (caret) {
                    range.setStart(caret.node, caret.offset);
                    range.collapse(true);
                } else if (start && end) {
                    range.setStart(start.node, start.offset);
                    range.setEnd(end.node, end.offset);
                } else {
                    range.selectNodeContents(surface);
                    range.collapse(false);
                }

                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
                surface.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: null }));
                document.dispatchEvent(new Event('selectionchange'));
            }
            """,
            new[] { testId, html });

        var expectedText = Regex.Match(html.Replace("|", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty), ">([^<]+)<").Groups[1].Value;
        await WaitForHiddenValueAsync(testId, expectedText);
    }

    private async Task ClickToolbarButtonAsync(ToolbarControlCase controlCase) {
        var valueSelector = controlCase.Value is null ? string.Empty : $"[data-value='{controlCase.Value}']";
        var button = EditorRoot(ServerEditorTestId).Locator($".nt-rich-text-editor-toolbar-button[data-command='{controlCase.Command}']{valueSelector}").First;
        await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
        (await button.IsEnabledAsync()).Should().BeTrue();
        await button.ClickAsync();
    }

    private async Task CloseOpenPanelAsync(string panelRole) {
        var cancelButtonRole = panelRole.Replace("-editor", "-cancel", StringComparison.Ordinal);
        var panel = EditorRoot(ServerEditorTestId).Locator($"[data-role='{panelRole}']");
        var cancelButton = EditorRoot(ServerEditorTestId).Locator($"[data-role='{cancelButtonRole}']");
        if (await cancelButton.IsVisibleAsync()) {
            await cancelButton.ClickAsync();
        } else {
            // The table size picker has no Cancel button; Escape closes it.
            await _page!.Keyboard.PressAsync("Escape");
        }

        await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    private async Task WaitForHiddenValueAsync(string testId, string expectedValue) {
        ArgumentNullException.ThrowIfNull(_page);

        try {
            await _page.WaitForFunctionAsync(
                """
                ([testId, expectedValue]) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const value = section?.querySelector('.nt-rich-text-editor-hidden-input')?.value ?? '';
                    return value.includes(expectedValue);
                }
                """,
                new[] { testId, expectedValue },
                new PageWaitForFunctionOptions { Timeout = 5000 });
        }
        catch (TimeoutException ex) {
            var state = await _page.EvaluateAsync<string>(
                """
                ([testId, expectedValue]) => {
                    const section = document.querySelector(`[data-testid="${testId}"]`);
                    const editor = section?.querySelector('nt-rich-text-editor');
                    const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                    return JSON.stringify({
                        testId,
                        expectedValue,
                        hiddenValue: editor?.querySelector('.nt-rich-text-editor-hidden-input')?.value ?? null,
                        sourceValue: editor?.querySelector('.nt-rich-text-editor-value')?.textContent ?? null,
                        surfaceHtml: surface?.innerHTML ?? null,
                        activeElement: document.activeElement?.outerHTML?.slice(0, 240) ?? null,
                        openPanels: Array.from(editor?.querySelectorAll('[data-tool-command][aria-hidden="false"]') ?? []).map(panel => panel.getAttribute('data-role'))
                    });
                }
                """,
                new[] { testId, expectedValue });
            throw new TimeoutException($"{ex.Message}{Environment.NewLine}{state}{Environment.NewLine}{string.Join(Environment.NewLine, _browserDiagnostics)}", ex);
        }
    }

    private async Task ExpectStatusAsync(string testId, string expectedText) {
        ArgumentNullException.ThrowIfNull(_page);

        var mode = testId.Replace("rich-text-editor-", "", StringComparison.Ordinal);
        await Assertions.Expect(_page.GetByTestId($"{mode}-status")).ToContainTextAsync(expectedText, new LocatorAssertionsToContainTextOptions { Timeout = 10000 });
    }

    public sealed record ClickToEditCase(string Tool, string Html, string TargetSelector, string InputRole, string PrefilledValue, string NewValue, string ExpectedValue) {
        public override string ToString() => Tool;
    }

    public sealed record ToolbarControlCase(string Command, string? Value = null, string InitialHtml = "<p>Text</p>", string SelectionText = "Text", string? ExpectedValue = null, string? ExpectedPanelRole = null, string? PreparedValue = null, bool PrepareUndoRedo = false, bool PrepareRedo = false);
}
