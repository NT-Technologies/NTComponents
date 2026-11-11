using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using NTComponents.Virtualization;
using System.Globalization;
using Bunit;

namespace NTComponents.Tests.Virtualization;

public class NTVirtualize_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Virtualization/NTVirtualize.razor.js";
    private readonly BunitJSModuleInterop _module;

    public NTVirtualize_Tests() {
        _module = JSInterop.SetupModule(JsModulePath);
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
        _module.SetupVoid("init", _ => true).SetVoidResult();
        _module.SetupVoid("updateRenderState", _ => true).SetVoidResult();
        _module.SetupVoid("updateOptions", _ => true).SetVoidResult();
        _module.SetupVoid("cancelScroll", _ => true).SetVoidResult();
    }

    [Fact]
    public void DisablingMeasurement_NotifiesBrowserEvenWhenItemCountsAreUnchanged() {
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0)))
            .Add(component => component.MeasureItemSize, true));
        cut.Render(parameters => parameters.Add(component => component.MeasureItemSize, false));

        var update = _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState");
        update.Arguments[4].Should().Be(false);
    }

    [Fact]
    public void OnParametersSet_Throws_If_ItemSize_Zero_Or_Negative() {
        // Arrange
        var items = new List<string> { "Item 1" };
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(items, items.Count));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, 0)));

        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, -1)));
    }

    [Fact]
    public void OnParametersSet_Throws_If_ItemsProvider_Null() {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemSize, 50)));
    }

    [Fact]
    public void InitialRender_Shows_Spacers_And_Calls_Init_Exactly_Once_With_A_NonNull_Reference() {
        // Arrange
        var items = new List<string>();
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(items, items.Count));

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.SpacerElement, "div"));

        // Assert
        var spacers = cut.FindAll("div[aria-hidden='true']");
        spacers.Should().HaveCount(2);

        // One before, one after
        spacers[0].GetAttribute("data-nt-virtualize-size").Should().Be("0");
        spacers[1].GetAttribute("data-nt-virtualize-size").Should().Be("0");
        spacers.Should().OnlyContain(spacer => !spacer.HasAttribute("style"));
        spacers[0].ClassList.Should().Contain("nt-virtualize-spacer");
        spacers[1].ClassList.Should().Contain("nt-virtualize-spacer");
        var initInvocation = JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "init").Subject;
        initInvocation.Arguments[0].Should().NotBeNull();
    }

    [Fact]
    public async Task Disposal_While_First_Render_Initialization_Is_Awaiting_Does_Not_Invoke_Virtualizer_Interop() {
        NTVirtualizeItemsProvider<string> provider = _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0));
        var cut = Render<TestVirtualize<string>>(parameters => parameters.Add(component => component.ItemsProvider, provider));
        var initialInitCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "init");
        var initialUpdateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "updateRenderState");
        var pendingOnUpdate = _module.SetupVoid("onUpdate", _ => true);

        var renderTask = cut.InvokeAsync(() => cut.Instance.InvokeOnAfterRenderAsync(firstRender: true));
        await Task.Yield();
        pendingOnUpdate.Invocations.Should().ContainSingle();
        cut.Instance.Dispose();
        pendingOnUpdate.SetVoidResult();
        await renderTask;

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "init").Should().Be(initialInitCount);
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "updateRenderState").Should().Be(initialUpdateCount);
        JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "init" || invocation.Identifier == "updateRenderState")
            .Should().OnlyContain(invocation => invocation.Arguments[0] != null);
    }

    [Fact]
    public void JavaScript_Disconnection_During_Init_Does_Not_Fail_Rendering() {
        _module.SetupVoid("init", _ => true).SetException(new JSDisconnectedException("Disconnected"));
        NTVirtualizeItemsProvider<string> provider = _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0));

        var act = () => Render<NTVirtualize<string>>(parameters => parameters.Add(component => component.ItemsProvider, provider));

        act.Should().NotThrow();
    }

    [Fact]
    public void LoadItems_Triggers_RefreshData() {
        // Arrange
        var items = new List<string> { "Item 1", "Item 2", "Item 3" };
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act - Simulate JS call to LoadItems
        cut.Instance.LoadItems(0, 0, 0, 2);

        // Assert
        callCount.Should().BeGreaterThan(0);
        cut.FindAll("div.nt-virtualize-spacer").Should().HaveCount(2);
    }

    [Fact]
    public void Render_Items_When_Loaded() {
        // Arrange
        var items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request => {
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        // Simulate JS telling us what to load
        cut.Instance.LoadItems(0, 500, 0, 5);
        cut.WaitForState(() => cut.Markup.Contains("-Item 1-"));

        // Assert
        cut.Markup.Should().Contain("-Item 1-");
        cut.Markup.Should().Contain("-Item 5-");
        cut.Markup.Should().NotContain("-Item 6-");
    }

    [Fact]
    public async Task Render_Items_Preloads_Next_Visible_Window_As_Placeholders() {
        var items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request => {
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, 50)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-"))
            .Add(c => c.LoadingTemplate, context => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "preloaded-placeholder");
                builder.AddAttribute(2, "data-index", context.Index.ToString(CultureInfo.InvariantCulture));
                builder.AddAttribute(3, "style", $"height: {context.Size.ToString(CultureInfo.InvariantCulture)}px");
                builder.CloseElement();
            }));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));

        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("-Item 5-");
            cut.FindAll(".preloaded-placeholder").Should().HaveCount(5);
            cut.FindAll(".preloaded-placeholder")[0].GetAttribute("data-index").Should().Be("7");
            cut.FindAll(".nt-virtualize-spacer")[1].GetAttribute("data-nt-virtualize-size").Should().Be("500");
        });
    }

    [Fact]
    public async Task LoadItems_Renders_Cached_Items_And_Placeholders_For_Missing_Differences() {
        var items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();
        var pending = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        var captured = new List<NTVirtualizeItemsProviderRequest<string>>();
        NTVirtualizeItemsProvider<string> provider = request => {
            captured.Add(request);
            if (request.StartIndex == 5) {
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(pending.Task);
            }

            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ScrollDebounceMilliseconds, 0)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-"))
            .Add(c => c.LoadingTemplate, context => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "missing-placeholder");
                builder.CloseElement();
            }));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(150, 600, 3, 5));

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 5 && request.Count == 3);
            cut.Markup.Should().Contain("-Item 4-");
            cut.Markup.Should().Contain("-Item 5-");
            cut.FindAll(".missing-placeholder").Should().HaveCount(3);
        });

        pending.SetResult(new NTVirtualizeItemsProviderResult<string>(items.Skip(5).Take(3).ToList(), items.Count));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("-Item 8-");
            cut.FindAll(".missing-placeholder").Should().BeEmpty();
        });
    }

    [Fact]
    public async Task LoadItems_Uses_Cache_When_Requested_Range_Is_Already_Loaded() {
        var items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.BackgroundPreloadWindowCount, 0)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(100, 650, 2, 2));

        cut.WaitForAssertion(() => {
            callCount.Should().Be(1);
            cut.Markup.Should().Contain("-Item 3-");
            cut.Markup.Should().Contain("-Item 4-");
        });
    }

    [Fact]
    public async Task LoadItems_Waits_For_Scroll_To_Settle_And_Requests_Only_The_Final_Window() {
        var items = Enumerable.Range(1, 100).Select(i => $"Item {i}").ToList();
        var captured = new List<NTVirtualizeItemsProviderRequest<string>>();
        NTVirtualizeItemsProvider<string> provider = request => {
            captured.Add(request);
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ScrollDebounceMilliseconds, 300)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.BackgroundPreloadWindowCount, 0)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-"))
            .Add(c => c.LoadingTemplate, context => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "missing-placeholder");
                builder.CloseElement();
            }));

        // The first window loads immediately; only later scroll-driven windows wait.
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));
        captured.Should().ContainSingle();

        await cut.InvokeAsync(() => cut.Instance.LoadItems(1000, 3750, 20, 5));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(2000, 2750, 40, 5));

        captured.Should().ContainSingle();
        cut.FindAll(".missing-placeholder").Should().HaveCount(5);

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 41-"));
        captured.Should().HaveCount(2);
        captured[1].StartIndex.Should().Be(40);
    }

    [Fact]
    public async Task BackgroundPreload_While_Scrolling_Cached_Rows_Waits_For_Scroll_To_Settle() {
        var items = Enumerable.Range(1, 100).Select(i => $"Item {i}").ToList();
        var captured = new List<NTVirtualizeItemsProviderRequest<string>>();
        NTVirtualizeItemsProvider<string> provider = request => {
            captured.Add(request);
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ScrollDebounceMilliseconds, 300)
            .Add(c => c.PlaceholderPreloadWindowCount, 1)
            .Add(c => c.BackgroundPreloadWindowCount, 1)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        // The preload after a completed load is not delayed.
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 10-"));
        captured.Select(request => request.StartIndex).Should().Equal(0, 5);

        // Rows 3-7 are cached, so only the preload of rows 10-12 remains and it waits for the scroll to settle.
        await cut.InvokeAsync(() => cut.Instance.LoadItems(150, 4600, 3, 5));
        captured.Should().HaveCount(2);
        cut.Markup.Should().NotContain("-Item 11-");

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 13-"));
        captured.Select(request => request.StartIndex).Should().Equal(0, 5, 10);
    }

    [Fact]
    public async Task Unchanged_Render_State_Is_Not_Reposted_To_JavaScript() {
        var items = Enumerable.Range(1, 5).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request => {
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));
        var updateRenderStateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "updateRenderState");

        await cut.InvokeAsync(() => cut.Instance.LoadItems(1, 0, 0, 5));

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "updateRenderState").Should().Be(updateRenderStateCount);
    }

    [Fact]
    public async Task Data_Load_Render_Does_Not_Reinitialize_JavaScript_Virtualizer() {
        var items = Enumerable.Range(1, 5).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request => {
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "init").Should().Be(1);

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "init").Should().Be(1);
    }

    [Fact]
    public async Task LoadItems_Revalidates_Cached_Range_In_Background_When_Enabled() {
        var captured = new List<NTVirtualizeItemsProviderRequest<string>>();
        var revalidation = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        NTVirtualizeItemsProvider<string> provider = request => {
            captured.Add(request);
            if (request.StartIndex == 0 && captured.Count > 2) {
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(revalidation.Task);
            }

            var prefix = "Old";
            var resultItems = Enumerable.Range(request.StartIndex, request.Count ?? 0).Select(index => $"{prefix} {index}").ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, 20));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ScrollDebounceMilliseconds, 0)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.BackgroundPreloadWindowCount, 0)
            .Add(c => c.RevalidateCachedItems, true)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Old 4-"));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(250, 500, 5, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Old 9-"));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 0 && request.Count == 5);
            captured.Count(request => request.StartIndex == 0 && request.Count == 5).Should().Be(2);
            cut.Markup.Should().Contain("-Old 4-");
        });

        revalidation.SetResult(new NTVirtualizeItemsProviderResult<string>(Enumerable.Range(0, 5).Select(index => $"New {index}").ToList(), 20));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("-New 4-");
            cut.Markup.Should().NotContain("-Old 4-");
        });
    }

    [Fact]
    public async Task LoadItems_Does_Not_Revalidate_Cached_Range_When_Disabled() {
        var items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.BackgroundPreloadWindowCount, 0)
            .Add(c => c.RevalidateCachedItems, false)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-"));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));

        cut.WaitForAssertion(() => {
            callCount.Should().Be(1);
            cut.Markup.Should().Contain("-Item 5-");
        });
    }

    [Fact]
    public async Task BackgroundPreload_Replaces_Preloaded_Placeholders_With_Cached_Items() {
        var items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();
        var prefetch = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        var captured = new List<NTVirtualizeItemsProviderRequest<string>>();
        NTVirtualizeItemsProvider<string> provider = request => {
            captured.Add(request);
            if (request.StartIndex == 5) {
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(prefetch.Task);
            }

            var resultItems = items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(resultItems, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.PlaceholderPreloadWindowCount, 1)
            .Add(c => c.BackgroundPreloadWindowCount, 1)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-"))
            .Add(c => c.LoadingTemplate, context => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "preload-placeholder");
                builder.CloseElement();
            }));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 750, 0, 5));

        cut.WaitForAssertion(() => {
            captured.Should().Contain(request => request.StartIndex == 5 && request.Count == 5);
            cut.FindAll(".preload-placeholder").Should().HaveCount(5);
        });

        prefetch.SetResult(new NTVirtualizeItemsProviderResult<string>(items.Skip(5).Take(5).ToList(), items.Count));
        cut.WaitForAssertion(() => {
            cut.Markup.Should().Contain("-Item 10-");
            cut.FindAll(".preload-placeholder").Should().BeEmpty();
        });
    }

    [Fact]
    public void EmptyTemplate_Shows_When_No_Items() {
        // Arrange
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(new List<string>(), 0));

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(c => c.EmptyTemplate, builder => builder.AddContent(0, "No items found")));

        cut.Instance.LoadItems(0, 0, 0, 10);
        cut.WaitForState(() => cut.Markup.Contains("No items found"));

        // Assert
        cut.Markup.Should().Contain("No items found");
    }

    [Fact]
    public async Task RefreshDataAsync_Forces_Reload() {
        // Arrange
        var items = new List<string> { "Old Item" };
        var providerCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            providerCount++;
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(items, items.Count));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));
        cut.WaitForState(() => providerCount > 0);
        var initialCount = providerCount;

        // Act
        items = new List<string> { "New Item" };
        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("New Item"));
        providerCount.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public void ItemsProvider_Exception_Is_Thrown_In_Renderer() {
        // Arrange
        NTVirtualizeItemsProvider<string> provider = request => throw new Exception("Data fetch failed");

        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // LoadItems will trigger RefreshDataCoreAsync which will catch the exception
        cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));

        // Now rendering should throw
        Assert.Throws<Exception>(() => cut.WaitForAssertion(() => cut.Markup.Should().NotBeNull()));
    }

    [Fact]
    public async Task LoadingTemplate_Is_Used_While_Loading() {
        // Arrange
        var tcs = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        var firstCall = true;
        NTVirtualizeItemsProvider<string> provider = request => {
            if (firstCall) {
                firstCall = false;
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(new List<string>(), 100));
            }
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(tcs.Task);
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.PlaceholderPreloadWindowCount, 0)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(c => c.LoadingTemplate, context => builder => builder.AddContent(0, "Loading...")));

        // Initial load to set total count
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));
        cut.WaitForState(() => !firstCall);

        // Act - Trigger another load which will be slow
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Loading..."));

        // Finish loading - provide enough items to fill the requested range (10-15)
        tcs.SetResult(new NTVirtualizeItemsProviderResult<string>(Enumerable.Range(10, 5).Select(i => $"Item {i}").ToList(), 100));
        cut.WaitForState(() => cut.Markup.Contains("Item 14"));
        cut.Markup.Should().NotContain("Loading...");
    }

    [Fact]
    public async Task LoadingTemplate_Is_Used_During_Initial_Load_Before_Total_Count_Is_Known() {
        var tcs = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        NTVirtualizeItemsProvider<string> provider = _ => new ValueTask<NTVirtualizeItemsProviderResult<string>>(tcs.Task);

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemSize, 64)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(c => c.LoadingTemplate, context => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "initial-loading-placeholder");
                builder.AddAttribute(2, "data-index", context.Index.ToString(CultureInfo.InvariantCulture));
                builder.AddAttribute(3, "style", $"height: {context.Size.ToString(CultureInfo.InvariantCulture)}px");
                builder.CloseElement();
            }));

        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 3));

        cut.WaitForAssertion(() => {
            var placeholders = cut.FindAll(".initial-loading-placeholder");
            placeholders.Should().HaveCount(3);
            placeholders[0].GetAttribute("data-index").Should().Be("2");
            placeholders[0].GetAttribute("style").Should().Contain("height: 64px");
        });

        tcs.SetResult(new NTVirtualizeItemsProviderResult<string>(["Item 1", "Item 2", "Item 3"], 3));
        cut.WaitForState(() => cut.Markup.Contains("Item 3"));
        cut.FindAll(".initial-loading-placeholder").Should().BeEmpty();
    }

    [Fact]
    public async Task Multiple_LoadItems_Calls_Respect_Last_One() {
        // Arrange
        var callCount = 0;
        NTVirtualizeItemsProvider<string> provider = request => {
            callCount++;
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(new List<string> { $"Item {request.StartIndex}" }, 100));
        };

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 20, 5));

        cut.WaitForState(() => cut.Markup.Contains("Item 20"));

        // Assert
        cut.Markup.Should().Contain("Item 20");
        cut.Markup.Should().NotContain("Item 10");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ItemSize_Is_Respected_In_Initial_Render() {
        // Arrange
        var items = new List<string>() { "item1" };
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(items, items.Count));
        const float itemSize = 123f;
        // Act
        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => {
                builder.OpenElement(0, "div");
                builder.AddAttribute(10, "style", $"height: {itemSize}px");
                builder.AddContent(20, item);
                builder.CloseElement();
            })
            .Add(c => c.ItemSize, itemSize));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 5));
        cut.WaitForState(() => cut.Markup.Contains("item1"));

        // Assert
        cut.Markup.Should().Contain($"height: {itemSize}px");
    }

    [Fact]
    public async Task LoadItems_Handles_OutOfBounds_Indices() {
        // Arrange
        var items = Enumerable.Range(0, 5).Select(i => $"Item {i}").ToList();
        NTVirtualizeItemsProvider<string> provider = request =>
            new ValueTask<NTVirtualizeItemsProviderResult<string>>(new NTVirtualizeItemsProviderResult<string>(items.Skip(request.StartIndex).Take(request.Count ?? 0).ToList(), items.Count));

        var cut = Render<NTVirtualize<string>>(p => p
            .Add(c => c.ItemsProvider, provider)
            .Add(c => c.ItemTemplate, item => builder => builder.AddContent(0, item)));

        // Act - Request starting beyond total count
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 10, 5));

        // Assert - Should adjusted to last possible items
        cut.WaitForState(() => cut.Markup.Contains("Item 4"));
    }

    [Fact]
    public async Task RepeatedMeasurementOfEmptyOrShortWindows_DoesNotRefetchPastTheEnd() {
        foreach (var total in new[] { 0, 2 }) {
            var calls = 0;
            var cut = Render<NTVirtualize<int>>(parameters => parameters
                .Add(component => component.MeasureItemSize, true)
                .Add(component => component.ItemsProvider, request => {
                    calls++;
                    return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<int>(Enumerable.Range(0, total).ToArray(), total));
                })
                .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));

            await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));
            await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 10));

            calls.Should().Be(1, "items beyond the known end are not missing cache entries");
        }
    }

    [Fact]
    public void ChangedRuntimeOptions_UpdateBrowserWithoutReinitializing() {
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0))));

        cut.Render(parameters => parameters
            .Add(component => component.ItemSize, 72)
            .Add(component => component.OverscanCount, 6)
            .Add(component => component.MaxItemCount, 150)
            .Add(component => component.ScrollRestorationKey, "updated-list"));

        var update = _module.Invocations.Last(invocation => invocation.Identifier == "updateOptions");
        update.Arguments.Skip(1).Should().Equal(72f, 6, 150, "updated-list");
        _module.Invocations.Count(invocation => invocation.Identifier == "init").Should().Be(1);
    }

    [Fact]
    public void InitialItemIndex_IsPassedToBrowserInitialization() {
        Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0)))
            .Add(component => component.InitialItemIndex, 42));

        var init = _module.Invocations.Single(invocation => invocation.Identifier == "init");
        init.Arguments.Last().Should().Be(42);
    }

    [Fact]
    public async Task ScrollBeforeInitialization_ReportsInvalidOperation() {
        using var component = new NTVirtualize<string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => component.ScrollToItemAsync(4, Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CanceledScroll_DoesNotSendBrowserNavigation() {
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([], 0))));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cut.Instance.ScrollToItemAsync(4, cancellation.Token));

        _module.Invocations.Should().NotContain(invocation => invocation.Identifier == "scrollToItem");
    }

    [Fact]
    public async Task RefreshDataAsync_WaitsForProviderAndRendersReplacementItems() {
        var replacement = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshing = false;
        NTVirtualizeItemsProvider<string> provider = _ => {
            if (!refreshing) {
                return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>(["old record"], 1));
            }
            providerStarted.TrySetResult();
            return new ValueTask<NTVirtualizeItemsProviderResult<string>>(replacement.Task);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 1));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("old record"));
        refreshing = true;

        var refresh = cut.InvokeAsync(() => cut.Instance.RefreshDataAsync(CancellationToken.None));
        await providerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        refresh.IsCompleted.Should().BeFalse();
        replacement.SetResult(new NTVirtualizeItemsProviderResult<string>(["replacement record"], 1));
        await refresh;

        cut.Markup.Should().Contain("replacement record").And.NotContain("old record");
    }

    [Fact]
    public async Task CanceledRefresh_DoesNotRequestOrDiscardCurrentItems() {
        var requests = 0;
        NTVirtualizeItemsProvider<string> provider = _ => {
            requests++;
            return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>(["current record"], 1));
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cut.Instance.RefreshDataAsync(cancellation.Token));

        requests.Should().Be(1);
        cut.Markup.Should().Contain("current record");
    }

    [Fact]
    public async Task SupersededRefresh_CannotOverwriteNewerProviderResults() {
        var stale = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var staleStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        NTVirtualizeItemsProvider<string> provider = _ => {
            calls++;
            if (calls == 2) {
                staleStarted.SetResult();
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(stale.Task);
            }
            return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([calls == 1 ? "initial record" : "current record"], 1));
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 1));
        var olderRefresh = cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());
        await staleStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());
        stale.SetResult(new NTVirtualizeItemsProviderResult<string>(["obsolete record"], 1));
        await olderRefresh;

        cut.Markup.Should().Contain("current record").And.NotContain("obsolete record");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshCanceledDuringUncooperativeProvider_PreservesRowsAndIgnoresLateCompletion(bool failsLate) {
        var pending = new TaskCompletionSource<NTVirtualizeItemsProviderResult<string>>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        NTVirtualizeItemsProvider<string> provider = _ => {
            requests++;
            if (requests == 2) {
                started.SetResult();
                return new ValueTask<NTVirtualizeItemsProviderResult<string>>(pending.Task);
            }
            return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>(["current record"], 1));
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 1));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Xunit.TestContext.Current.CancellationToken);
        var refresh = cut.InvokeAsync(() => cut.Instance.RefreshDataAsync(cancellation.Token));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken));
        pending.Task.IsCompleted.Should().BeFalse("caller cancellation must not wait for an uncooperative provider");
        cut.Markup.Should().Contain("current record");

        if (failsLate) {
            pending.SetException(new InvalidOperationException("obsolete provider failure"));
        }
        else {
            pending.SetResult(new NTVirtualizeItemsProviderResult<string>(["obsolete record"], 1));
        }
        await cut.InvokeAsync(() => cut.Render());

        cut.Markup.Should().Contain("current record").And.NotContain("obsolete record");
        var update = _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState");
        update.Arguments.Last().Should().Be(false, "canceled refreshes must leave the browser out of its loading state");
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task RefreshWithFreshInstances_RestoresAnchorByStableItemKey(bool prepend, int expectedAnchorIndex) {
        var items = new[] { Tuple.Create(1, "first"), Tuple.Create(2, "second"), Tuple.Create(3, "third") };
        NTVirtualizeItemsProvider<Tuple<int, string>> provider = request => ValueTask.FromResult(
            new NTVirtualizeItemsProviderResult<Tuple<int, string>>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToList(), items.Length));
        var snapshot = new NTVirtualizeAnchorSnapshot { Index = 1, Offset = 12, AtStart = false, AtEnd = false, Version = 7 };
        _module.Setup<NTVirtualizeAnchorSnapshot>("captureAnchor", _ => true).SetResult(snapshot);
        _module.SetupVoid("restoreAnchor", _ => true).SetVoidResult();
        var cut = Render<NTVirtualize<Tuple<int, string>>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.AnchorMode, NTVirtualizeAnchorMode.Start)
            .Add(component => component.ItemKey, item => item.Item1)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item.Item2)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 3));
        items = prepend
            ? [Tuple.Create(0, "inserted"), Tuple.Create(1, "first updated"), Tuple.Create(2, "second updated"), Tuple.Create(3, "third updated")]
            : [Tuple.Create(1, "first updated"), Tuple.Create(2, "second updated"), Tuple.Create(3, "third updated"), Tuple.Create(4, "appended")];

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        var restore = _module.Invocations.Last(invocation => invocation.Identifier == "restoreAnchor");
        restore.Arguments[1].Should().BeSameAs(snapshot);
        restore.Arguments[2].Should().Be(expectedAnchorIndex);
        restore.Arguments[3].Should().Be((int)NTVirtualizeAnchorMode.Start);
        cut.Markup.Should().Contain("second updated");
    }

    [Fact]
    public async Task RefreshWithSameItemCount_InvalidatesBrowserMeasurementsWithoutAnchoring() {
        var item = "original short record";
        NTVirtualizeItemsProvider<string> provider = _ => ValueTask.FromResult(new NTVirtualizeItemsProviderResult<string>([item], 1));
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.MeasureItemSize, true)
            .Add(component => component.AnchorMode, NTVirtualizeAnchorMode.None)
            .Add(component => component.ItemTemplate, value => builder => builder.AddContent(0, value)));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 0, 0, 1));
        var previousUpdate = _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState");
        var previousRevision = (int)previousUpdate.Arguments[5]!;
        item = "replacement record with longer content requiring remeasurement";

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        var updated = _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState");
        ((int)updated.Arguments[5]!).Should().BeGreaterThan(previousRevision);
        updated.Arguments[1].Should().Be(1);
        updated.Arguments[2].Should().Be(1);
        cut.Markup.Should().Contain(item).And.NotContain("original short record");
        _module.Invocations.Should().NotContain(invocation => invocation.Identifier == "captureAnchor" || invocation.Identifier == "restoreAnchor");
    }

    [Fact]
    public async Task PrependLargerThanWindow_ProbesShiftedWindowAndPreservesStableAnchor() {
        var items = Enumerable.Range(0, 20).Select(index => Tuple.Create(index, $"original {index}")).ToArray();
        var requests = new List<NTVirtualizeItemsProviderRequest<Tuple<int, string>>>();
        NTVirtualizeItemsProvider<Tuple<int, string>> provider = request => {
            requests.Add(request);
            return ValueTask.FromResult(new NTVirtualizeItemsProviderResult<Tuple<int, string>>(
                items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToList(), items.Length));
        };
        _module.Setup<NTVirtualizeAnchorSnapshot>("captureAnchor", _ => true).SetResult(new NTVirtualizeAnchorSnapshot { Index = 11, Offset = 9, Version = 2 });
        _module.SetupVoid("restoreAnchor", _ => true).SetVoidResult();
        var cut = Render<NTVirtualize<Tuple<int, string>>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ScrollDebounceMilliseconds, 0)
            .Add(component => component.AnchorMode, NTVirtualizeAnchorMode.Start)
            .Add(component => component.ItemKey, value => value.Item1)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.ItemTemplate, value => builder => builder.AddContent(0, $"[{value.Item2}]")));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 850, 0, 3));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(500, 350, 10, 3));
        cut.Markup.Should().Contain("[original 11]");
        requests.Clear();
        items = Enumerable.Range(-10, 30).Select(index => Tuple.Create(index, $"updated {index}")).ToArray();

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        requests.Select(request => request.StartIndex).Should().Equal(10, 20);
        requests.Should().OnlyContain(request => request.Count == 3);
        var restore = _module.Invocations.Last(invocation => invocation.Identifier == "restoreAnchor");
        restore.Arguments[2].Should().Be(21);
        cut.Markup.Should().Contain("[updated 10]").And.Contain("[updated 11]").And.Contain("[updated 12]");
        cut.Markup.Should().NotContain("[updated 0]").And.NotContain("[original 11]");
    }

    [Fact]
    public async Task BackgroundPreloadFromDifferentDataSet_ReplacesStaleCachedRows() {
        // Arrange: row 0 is cached from data set A while the preload of rows 5+ waits.
        var filtered = false;
        var preloadGate = new TaskCompletionSource();
        NTVirtualizeItemsProvider<string> provider = async request => {
            if (request.StartIndex >= 5) {
                await preloadGate.Task;
            }

            var items = filtered ? ["B 0"] : Enumerable.Range(0, 100).Select(index => $"A {index}").ToArray();
            return new NTVirtualizeItemsProviderResult<string>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToList(), items.Length);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.BackgroundPreloadWindowCount, 1)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-A 0-"));

        // Act: the data set changes under the cache, then the preload returns its count of 1.
        filtered = true;
        preloadGate.SetResult();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-B 0-").And.NotContain("-A 0-"));
    }

    [Fact]
    public async Task BackgroundRevalidationFromDifferentDataSet_ReplacesStaleCachedRows() {
        // Arrange: rows 0-9 are cached from data set A, and revalidating the window at row 5 waits.
        var filtered = false;
        var revalidationGate = new TaskCompletionSource();
        var firstWindowGate = new TaskCompletionSource();
        var secondWindowRequests = 0;
        NTVirtualizeItemsProvider<string> provider = async request => {
            if (request.StartIndex == 5 && ++secondWindowRequests == 2) {
                await revalidationGate.Task;
            }

            if (request.StartIndex == 0 && filtered) {
                await firstWindowGate.Task;
            }

            var items = filtered ? Enumerable.Range(0, 8).Select(index => $"B {index}").ToArray() : Enumerable.Range(0, 100).Select(index => $"A {index}").ToArray();
            return new NTVirtualizeItemsProviderResult<string>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToList(), items.Length);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.ScrollDebounceMilliseconds, 0)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.BackgroundPreloadWindowCount, 0)
            .Add(component => component.RevalidateCachedItems, true)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(250, 4500, 5, 5));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(250, 4500, 5, 5));
        cut.WaitForAssertion(() => secondWindowRequests.Should().Be(2));

        // Act: the data set changes to 8 rows, the revalidation returns, and the user scrolls back to row 0 while it loads.
        filtered = true;
        revalidationGate.SetResult();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-B 5-"));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 400, 0, 5));

        // Assert
        cut.WaitForAssertion(() => {
            cut.FindAll(".nt-virtualize-spacer")[0].GetAttribute("data-nt-virtualize-start").Should().Be("0");
            cut.Markup.Should().NotContain("-A ", "rows from the previous data set must not render with the new count");
        });
        firstWindowGate.SetResult();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-B 0-"));
    }

    [Fact]
    public async Task BackgroundPreloadWithAppendedItems_KeepsWindowAndAdoptsNewCount() {
        // Arrange: the window starts at row 5 and the preload of rows 10+ waits.
        var items = Enumerable.Range(0, 20).Select(index => $"Item {index}").ToList();
        var preloadGate = new TaskCompletionSource();
        NTVirtualizeItemsProvider<string> provider = async request => {
            if (request.StartIndex >= 10) {
                await preloadGate.Task;
            }

            return new NTVirtualizeItemsProviderResult<string>(items.Skip(request.StartIndex).Take(request.Count ?? items.Count).ToList(), items.Count);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.BackgroundPreloadWindowCount, 1)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(250, 500, 5, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 5-").And.Contain("-Item 9-"));

        // Act: rows are appended to the same data set before the preload returns.
        items.AddRange(Enumerable.Range(20, 5).Select(index => $"Item {index}"));
        preloadGate.SetResult();

        // Assert
        cut.WaitForAssertion(() => {
            var update = _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState");
            update.Arguments[1].Should().Be(25);
            var topSpacer = cut.FindAll(".nt-virtualize-spacer")[0];
            topSpacer.GetAttribute("data-nt-virtualize-start").Should().Be("5");
            topSpacer.GetAttribute("data-nt-virtualize-size").Should().Be("250");
            cut.Markup.Should().Contain("-Item 5-").And.Contain("-Item 9-").And.NotContain("-Item 4-");
        });
    }

    [Fact]
    public async Task BackgroundPreloadWithTotalChangingOnEveryRequest_ReloadsOnceThenAdoptsCount() {
        // Arrange: every request reports a larger total, like a live feed that grows faster than a round trip.
        var requests = 0;
        NTVirtualizeItemsProvider<string> provider = async request => {
            var total = 100 + Interlocked.Increment(ref requests);
            await Task.Yield();
            var items = Enumerable.Range(request.StartIndex, Math.Min(request.Count ?? total, total - request.StartIndex)).Select(index => $"Item {index}").ToList();
            return new NTVirtualizeItemsProviderResult<string>(items, total);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.BackgroundPreloadWindowCount, 1)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));

        // Act: load, preload (count changed: reload), reload, preload (count changed again: adopt).
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);

        // Assert
        requests.Should().Be(4, "a second count change in the same background chain must not start another reload");
        cut.Render();
        cut.WaitForAssertion(() => _module.Invocations.Last(invocation => invocation.Identifier == "updateRenderState").Arguments[1].Should().Be(104));
        cut.Markup.Should().Contain("-Item 0-");
    }

    [Fact]
    public async Task BackgroundCountChangeReloadFailure_KeepsCachedRowsWithoutThrowing() {
        // Arrange: rows 0-4 are cached and the preload of rows 5+ waits.
        var changed = false;
        var reloadAttempts = 0;
        var preloadGate = new TaskCompletionSource();
        NTVirtualizeItemsProvider<string> provider = async request => {
            if (request.StartIndex >= 5) {
                await preloadGate.Task;
            }

            if (changed && request.StartIndex < 5) {
                Interlocked.Increment(ref reloadAttempts);
                throw new InvalidOperationException("reload failed");
            }

            var total = changed ? 50 : 100;
            return new NTVirtualizeItemsProviderResult<string>(Enumerable.Range(request.StartIndex, request.Count ?? total).Select(index => $"Item {index}").ToList(), total);
        };
        var cut = Render<NTVirtualize<string>>(parameters => parameters
            .Add(component => component.ItemsProvider, provider)
            .Add(component => component.PlaceholderPreloadWindowCount, 0)
            .Add(component => component.BackgroundPreloadWindowCount, 1)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, $"-{item}-")));
        await cut.InvokeAsync(() => cut.Instance.LoadItems(0, 4750, 0, 5));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("-Item 0-"));

        // Act: the preload reports a new total, and the reload it starts fails.
        changed = true;
        preloadGate.SetResult();
        cut.WaitForAssertion(() => reloadAttempts.Should().Be(1));
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("-Item 0-");
    }

    private sealed class TestVirtualize<TItem> : NTVirtualize<TItem> {
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);
    }
}
