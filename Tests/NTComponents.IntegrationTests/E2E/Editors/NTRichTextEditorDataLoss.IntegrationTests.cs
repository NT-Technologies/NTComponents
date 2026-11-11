using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace NTComponents.IntegrationTests.Editors;

/// <summary>
///     Browser regressions for text that used to be lost around the rich text editor's script lifecycle.
/// </summary>
[Collection(PlaywrightE2ECollection.Name)]
public class NTRichTextEditorDataLoss_IntegrationTests : IAsyncLifetime {
    private const string ModuleRoute = "**/NTRichTextEditor*.razor.js";
    private static readonly TimeSpan ServerLatency = TimeSpan.FromMilliseconds(200);
    private PlaywrightFixture? _fixture;
    private IPage _page = default!;

    public async ValueTask InitializeAsync() {
        _fixture = new PlaywrightFixture();
        await _fixture.InitializeAsync();
        _page = _fixture.Page;
    }

    public async ValueTask DisposeAsync() {
        if (_fixture is not null) {
            await _fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    [InlineData("lifecycle-server-no-prerender")]
    [InlineData("lifecycle-wasm-no-prerender")]
    public async Task ModuleStillLoading_SurfaceRejectsTypingUntilItIsInitialized(string testId) {
        var moduleGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _page.RouteAsync(ModuleRoute, async route => {
            await moduleGate.Task;
            await route.ContinueAsync();
        });
        await _page.GotoAsync(LifecycleUrl, new() { WaitUntil = WaitUntilState.Commit });

        var surface = Surface(testId);
        await surface.WaitForAsync();
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "false");
        await Assertions.Expect(surface).ToHaveAttributeAsync("aria-readonly", "true");
        await surface.ClickAsync();
        await _page.Keyboard.TypeAsync("lost");
        await Assertions.Expect(surface).Not.ToContainTextAsync("lost");

        moduleGate.SetResult();
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "true", new() { Timeout = 30000 });
        await Assertions.Expect(surface).ToContainTextAsync("Initial");
        await Assertions.Expect(surface).ToHaveAttributeAsync("aria-readonly", "false");

        await surface.ClickAsync();
        await _page.Keyboard.TypeAsync("kept");
        await RemoveEditorAsync(testId);
        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync("<p>Initialkept</p>");
    }

    [Theory]
    [InlineData("lifecycle-server", "**/_blazor/negotiate*")]
    [InlineData("lifecycle-wasm", "**/_framework/dotnet.*js")]
    public async Task PrerenderedEditorWaitingForInteractivity_RejectsTypingAndKeepsFocus(string testId, string interactivityRoute) {
        var interactivityGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _page.RouteAsync(interactivityRoute, async route => {
            await interactivityGate.Task;
            await route.ContinueAsync();
        });
        await _page.GotoAsync(LifecycleUrl, new() { WaitUntil = WaitUntilState.Commit });

        var surface = Surface(testId);
        await Assertions.Expect(surface).ToContainTextAsync("Initial", new() { Timeout = 30000 });
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "false");
        await surface.ClickAsync();

        interactivityGate.SetResult();
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "true", new() { Timeout = 30000 });
        await _page.Keyboard.TypeAsync("kept");

        await Assertions.Expect(surface).ToHaveTextAsync("Initialkept");
    }

    [Theory]
    [InlineData("lifecycle-server-no-prerender")]
    [InlineData("lifecycle-wasm-no-prerender")]
    public async Task ModuleFailsToLoad_ExplainsWhyEditorIsReadOnly(string testId) {
        await _page.RouteAsync(ModuleRoute, route => route.AbortAsync());
        await _page.GotoAsync(LifecycleUrl, new() { WaitUntil = WaitUntilState.Commit });

        await Assertions.Expect(Section(testId).GetByRole(AriaRole.Alert)).ToContainTextAsync("could not load", new() { Timeout = 30000 });
        await Assertions.Expect(Surface(testId)).ToHaveAttributeAsync("contenteditable", "false");
        await Assertions.Expect(Surface(testId)).ToHaveAttributeAsync("aria-readonly", "true");
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    public async Task ScriptClickSwitchingRecords_CommitsToOriginalRecord(string testId) {
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");

        await Section(testId).GetByRole(AriaRole.Button, new() { Name = "Open second note" }).EvaluateAsync("button => button.click()");

        await Assertions.Expect(Section(testId).GetByTestId("first-note")).ToHaveTextAsync("<p>Initialtyped</p>");
        await Assertions.Expect(Section(testId).GetByTestId("second-note")).ToHaveTextAsync("<p>Second</p>");
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    public async Task ShortcutSwitchingRecords_NeverWritesToNewRecord(string testId) {
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");

        await _page.Keyboard.PressAsync("F8");

        await Assertions.Expect(Surface(testId)).ToHaveCountAsync(0);
        await Assertions.Expect(Section(testId).GetByTestId("second-note")).ToHaveTextAsync("<p>Second</p>");
    }

    [Fact]
    public async Task PageHide_CommitsUncommittedText() {
        // WebAssembly only: on Server a synthetic pagehide also starts the circuit teardown, which races the commit.
        const string testId = "lifecycle-wasm";
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");

        await _page.EvaluateAsync("() => window.dispatchEvent(new PageTransitionEvent('pagehide'))");

        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync("<p>Initialtyped</p>");
    }

    [Fact]
    public async Task ReloadingPage_DeliversUncommittedTextToServer() {
        const string testId = "lifecycle-server";
        var text = $"unload{Guid.NewGuid():N}";
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync(text);

        await _page.ReloadAsync();
        // The commit races the prerender of the first reload, so the second one proves it arrived.
        await _page.ReloadAsync();

        await Assertions.Expect(Section(testId).GetByTestId("commit-log")).ToContainTextAsync(text);
    }

    [Theory]
    [InlineData("lifecycle-server-no-prerender")]
    [InlineData("lifecycle-wasm-no-prerender")]
    public async Task SurfaceFocusedWhileReadOnly_AcceptsTypingOnceInitialized(string testId) {
        var moduleGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _page.RouteAsync(ModuleRoute, async route => {
            await moduleGate.Task;
            await route.ContinueAsync();
        });
        await _page.GotoAsync(LifecycleUrl, new() { WaitUntil = WaitUntilState.Commit });
        var surface = Surface(testId);
        await surface.ClickAsync();

        moduleGate.SetResult();
        await Assertions.Expect(surface).ToHaveAttributeAsync("contenteditable", "true", new() { Timeout = 30000 });
        await _page.Keyboard.TypeAsync("kept");

        await Assertions.Expect(surface).ToHaveTextAsync("Initialkept");
    }

    [Theory]
    [InlineData("lifecycle-server", "BlurThenClick")]
    [InlineData("lifecycle-wasm", "BlurThenClick")]
    [InlineData("lifecycle-server", "ClickWithoutBlur")]
    [InlineData("lifecycle-wasm", "ClickWithoutBlur")]
    [InlineData("lifecycle-server", "Mouse")]
    [InlineData("lifecycle-wasm", "Mouse")]
    public async Task EditorRemovedRightAfterTyping_KeepsPendingText(string testId, string activation) {
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("pending");

        var button = Section(testId).GetByRole(AriaRole.Button, new() { Name = "Select another item" });
        switch (activation) {
            case "BlurThenClick":
                await button.EvaluateAsync("button => { document.activeElement.blur(); button.click(); }");
                break;
            case "ClickWithoutBlur":
                await button.EvaluateAsync("button => button.click()");
                break;
            default:
                await button.ClickAsync();
                break;
        }

        await Assertions.Expect(Surface(testId)).ToHaveCountAsync(0);
        await Assertions.Expect(BoundValue(testId)).ToContainTextAsync("pending");
    }

    [Theory]
    [InlineData("lifecycle-server", false)]
    [InlineData("lifecycle-wasm", false)]
    [InlineData("lifecycle-server", true)]
    [InlineData("lifecycle-wasm", true)]
    public async Task ValueSetFromDotNetAfterTyping_ReplacesSurface(string testId, bool bindOnInput) {
        await OpenEditableEditorAsync(testId);
        if (bindOnInput) {
            await SwitchToBindOnInputAsync(testId);
        }

        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");
        await Section(testId).GetByRole(AriaRole.Button, new() { Name = "Set value" }).ClickAsync();

        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync("<p>Set from .NET</p>");
        await Assertions.Expect(Surface(testId)).ToHaveTextAsync("Set from .NET");
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    public async Task ValueSetFromDotNetWithoutTyping_ReplacesSurface(string testId) {
        await OpenEditableEditorAsync(testId);

        await Section(testId).GetByRole(AriaRole.Button, new() { Name = "Set value" }).ClickAsync();

        await Assertions.Expect(Surface(testId)).ToHaveTextAsync("Set from .NET");
    }

    [Theory]
    [InlineData("lifecycle-server", false)]
    [InlineData("lifecycle-wasm", false)]
    [InlineData("lifecycle-server", true)]
    [InlineData("lifecycle-wasm", true)]
    public async Task ValueSetFromDotNetWhileEditing_KeepsTypedTextAndCommitsIt(string testId, bool bindOnInput) {
        await OpenEditableEditorAsync(testId);
        if (bindOnInput) {
            await SwitchToBindOnInputAsync(testId);
        }

        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");
        await _page.Keyboard.PressAsync("F9");
        await Assertions.Expect(BoundValue(testId)).ToContainTextAsync("Set from .NET");
        await _page.Keyboard.TypeAsync("more");
        await Assertions.Expect(Surface(testId)).ToHaveTextAsync("Initialtypedmore");

        await Section(testId).GetByRole(AriaRole.Heading).ClickAsync();

        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync("<p>Initialtypedmore</p>");
        await Assertions.Expect(Surface(testId)).ToHaveTextAsync("Initialtypedmore");
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    public async Task FocusedEditorWithoutEdits_AcceptsValueSetFromDotNet(string testId) {
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();

        await _page.Keyboard.PressAsync("F9");

        await Assertions.Expect(Surface(testId)).ToHaveTextAsync("Set from .NET");
    }

    [Theory]
    [InlineData("lifecycle-server")]
    [InlineData("lifecycle-wasm")]
    public async Task FocusLeavingThroughToolPanel_CommitsTypedText(string testId) {
        await OpenEditableEditorAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync("typed");

        await Section(testId).Locator("[data-command='link']").ClickAsync();
        await Section(testId).Locator("[data-role='link-url']").FocusAsync();
        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync("<p>Initial</p>");
        await Section(testId).GetByRole(AriaRole.Heading).ClickAsync();

        await Assertions.Expect(BoundValue(testId)).ToContainTextAsync("Initialtyped");
    }

    [Fact]
    public async Task FastTypingOverSlowConnection_KeepsEveryCharacter() {
        const string testId = "lifecycle-server";
        const string text = "the quick brown fox jumps over the lazy dog";
        await DelayServerMessagesAsync();
        await OpenEditableEditorAsync(testId);
        await SwitchToBindOnInputAsync(testId);
        await Surface(testId).ClickAsync();
        await _page.Keyboard.TypeAsync(text, new() { Delay = 40 });

        await Assertions.Expect(BoundValue(testId)).ToHaveTextAsync($"<p>Initial{text}</p>", new() { Timeout = 15000 });
        await Assertions.Expect(Surface(testId)).ToHaveTextAsync($"Initial{text}");
    }

    private string LifecycleUrl => $"{_fixture!.ServerAddress}/rich-text-editor-lifecycle";

    private ILocator Section(string testId) => _page.GetByTestId(testId);

    private ILocator Surface(string testId) => Section(testId).Locator(".nt-rich-text-editor-surface");

    private ILocator BoundValue(string testId) => Section(testId).GetByTestId("bound-value");

    private async Task SwitchToBindOnInputAsync(string testId) {
        await Section(testId).GetByRole(AriaRole.Button, new() { Name = "Bind on input" }).ClickAsync();
        await Section(testId).GetByRole(AriaRole.Button, new() { Name = "Commit on blur" }).WaitForAsync();
    }

    private async Task OpenEditableEditorAsync(string testId) {
        await _page.GotoAsync(LifecycleUrl);
        await _page.WaitForFunctionAsync(
            """
            testId => {
                const editor = document.querySelector(`[data-testid="${testId}"] nt-rich-text-editor`);
                const surface = editor?.querySelector('.nt-rich-text-editor-surface');
                return surface?.getAttribute('contenteditable') === 'true' && surface.textContent.includes('Initial');
            }
            """,
            testId,
            new() { Timeout = 30000 });
    }

    private async Task DelayServerMessagesAsync() {
        var responses = Channel.CreateUnbounded<(DateTime Due, Action Send)>();
        await _page.RouteWebSocketAsync(new Regex("/_blazor"), webSocket => {
            var server = webSocket.ConnectToServer();
            webSocket.OnMessage(frame => {
                if (frame.Binary is { } binary) {
                    server.Send(binary);
                }
                else {
                    server.Send(frame.Text!);
                }
            });
            server.OnMessage(frame => responses.Writer.TryWrite((DateTime.UtcNow + ServerLatency, () => {
                if (frame.Binary is { } binary) {
                    webSocket.Send(binary);
                }
                else {
                    webSocket.Send(frame.Text!);
                }
            })));
        });

        var cancellationToken = TestContext.Current.CancellationToken;
        _ = Task.Run(async () => {
            await foreach (var (due, send) in responses.Reader.ReadAllAsync(cancellationToken)) {
                var wait = due - DateTime.UtcNow;
                if (wait > TimeSpan.Zero) {
                    await Task.Delay(wait, cancellationToken);
                }

                send();
            }
        }, cancellationToken);
    }

    private Task RemoveEditorAsync(string testId) =>
        Section(testId).GetByRole(AriaRole.Button, new() { Name = "Select another item" }).ClickAsync();
}
