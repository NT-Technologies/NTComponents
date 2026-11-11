using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.Interfaces;

namespace NTComponents.Tests.Layout;

public class NTCanonicalView_Tests : BunitContext {
    private readonly BunitJSModuleInterop _containerModule;

    public NTCanonicalView_Tests() {
        _containerModule = SetupViewModule("./_content/NTComponents/Layout/Views/NTContainerView.razor.js");
        SetupViewModule("./_content/NTComponents/Layout/Views/NTListDetailView.razor.js");
        SetupViewModule("./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js");
    }

    [Fact]
    public async Task Disposal_While_OnLoad_Is_Awaiting_Does_Not_Invoke_OnUpdate_With_A_Disposed_Reference() {
        var cut = Render<TestContainerView>();
        var initialUpdateCount = JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate");
        var pendingOnLoad = _containerModule.SetupVoid("onLoad", _ => true);

        var renderTask = cut.InvokeAsync(() => cut.Instance.InvokeOnAfterRenderAsync(firstRender: true));
        await Task.Yield();
        pendingOnLoad.Invocations.Should().ContainSingle();
        cut.Instance.Dispose();
        pendingOnLoad.SetVoidResult();
        await renderTask;

        JSInterop.Invocations.Count(invocation => invocation.Identifier == "onUpdate").Should().Be(initialUpdateCount);
        JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "onLoad" || invocation.Identifier == "onUpdate")
            .Should().OnlyContain(invocation => invocation.Arguments[1] != null);
    }

    [Fact]
    public void ListDetail_Renders_Named_Panes_With_Material_State_Classes() {
        var cut = Render<NTListDetailView>(p => p
            .Add(c => c.DetailVisible, true)
            .Add(c => c.Mode, NTListDetailViewMode.TwoPane)
            .Add(c => c.List, Fragment("List content"))
            .Add(c => c.Detail, Fragment("Detail content")));

        var view = cut.Find("div.nt-list-detail-view");

        cut.Instance.Should().BeAssignableTo<INTPageScriptComponent<NTListDetailView>>();
        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Layout/Views/NTListDetailView.razor.js");
        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        ShouldHaveScopedCssAttribute(view);
        view.GetAttribute("class")!.Should().Contain("nt-view");
        view.GetAttribute("class")!.Should().Contain("nt-list-detail-view-detail-visible");
        view.GetAttribute("class")!.Should().Contain("nt-list-detail-view-two-pane");
        view.GetAttribute("data-detail-visible").Should().Be("true");
        cut.Find(".nt-list-detail-view-list").TextContent.Should().Contain("List content");
        cut.Find(".nt-list-detail-view-detail").TextContent.Should().Contain("Detail content");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTListDetailView.razor.js");
    }

    [Fact]
    public void ListDetail_Renders_PageScript_Inside_View_For_Static_Enhancement() {
        var cut = Render<NTListDetailView>(p => p
            .Add(c => c.List, Fragment("List content"))
            .Add(c => c.Detail, Fragment("Detail content")));

        var view = cut.Find("div.nt-list-detail-view");
        var pageScript = view.QuerySelector("nt-page-script");

        pageScript.Should().NotBeNull();
        pageScript!.GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTListDetailView.razor.js");
    }

    [Fact]
    public void ListDetail_Renders_EmptyDetail_When_Detail_Is_Not_Set() {
        var cut = Render<NTListDetailView>(p => p
            .Add(c => c.List, Fragment("List content"))
            .Add(c => c.EmptyDetail, Fragment("No detail")));

        cut.Find(".nt-list-detail-view-detail").TextContent.Should().Contain("No detail");
    }

    [Fact]
    public void ListDetail_Renders_DetailHeader_Before_Detail_Content() {
        var cut = Render<NTListDetailView>(p => p
            .Add(c => c.DetailHeader, Fragment("Back"))
            .Add(c => c.Detail, Fragment("Detail content")));

        var detailPane = cut.Find(".nt-list-detail-view-detail");

        detailPane.TextContent.Should().Contain("Back");
        detailPane.TextContent.IndexOf("Back", StringComparison.Ordinal)
            .Should()
            .BeLessThan(detailPane.TextContent.IndexOf("Detail content", StringComparison.Ordinal));
    }

    [Fact]
    public void SupportingPane_Renders_Primary_And_Supporting_Slots() {
        var cut = Render<NTSupportingPaneView>(p => p
            .Add(c => c.Mode, NTSupportingPaneViewMode.HideOnSmallScreens)
            .Add(c => c.Primary, Fragment("Primary content"))
            .Add(c => c.Supporting, Fragment("Supporting content")));

        var view = cut.Find("div.nt-supporting-pane-view");

        cut.Instance.Should().BeAssignableTo<INTPageScriptComponent<NTSupportingPaneView>>();
        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js");
        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        ShouldHaveScopedCssAttribute(view);
        view.GetAttribute("class")!.Should().Contain("nt-supporting-pane-view-hide-on-small-screens");
        view.GetAttribute("data-nt-supporting-pane-mode").Should().Be("HideOnSmallScreens");
        cut.Find(".nt-supporting-pane-view-primary").TextContent.Should().Contain("Primary content");
        cut.Find(".nt-supporting-pane-view-supporting").TextContent.Should().Contain("Supporting content");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js");
    }

    [Fact]
    public void SupportingPane_Renders_PageScript_Inside_View_For_Static_Enhancement() {
        var cut = Render<NTSupportingPaneView>(p => p
            .Add(c => c.Primary, Fragment("Primary content"))
            .Add(c => c.Supporting, Fragment("Supporting content")));

        var view = cut.Find("div.nt-supporting-pane-view");
        var pageScript = view.QuerySelector("nt-page-script");

        pageScript.Should().NotBeNull();
        pageScript!.GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js");
    }

    [Fact]
    public void Feed_Renders_ChildContent_And_MinItemWidth_Variable() {
        var cut = Render<NTFeedView>(p => p
            .Add(c => c.MinItemWidth, "240px")
            .AddChildContent("<article>Feed card</article>"));

        var view = cut.Find("div.nt-feed-view");

        ShouldHaveScopedCssAttribute(view);
        view.TextContent.Should().Contain("Feed card");
        view.GetAttribute("style")!.Should().Contain("--nt-feed-view-min-item-width:240px");
    }

    [Fact]
    public void Container_Renders_Centered_View_With_ChildContent_And_Base_Attributes() {
        var attributes = new Dictionary<string, object> {
            ["class"] = "content-shell",
            ["data-testid"] = "container-view"
        };

        var cut = Render<NTContainerView>(p => p
            .Add(c => c.ElementId, "main-container")
            .Add(c => c.ElementLang, "en")
            .Add(c => c.ElementTitle, "Main container")
            .Add(c => c.AutoFocus, true)
            .Add(c => c.AdditionalAttributes, attributes)
            .AddChildContent("<article>Container content</article>"));

        var view = cut.Find("div.nt-container-view");

        cut.Instance.Should().BeAssignableTo<INTPageScriptComponent<NTContainerView>>();
        cut.Instance.JsModulePath.Should().Be("./_content/NTComponents/Layout/Views/NTContainerView.razor.js");
        cut.Instance.IsolatedJsModule.Should().NotBeNull();
        ShouldHaveScopedCssAttribute(view);
        view.HasAttribute("data-nt-container-view").Should().BeTrue();
        view.GetAttribute("class")!.Should().Contain("nt-view");
        view.GetAttribute("class")!.Should().Contain("content-shell");
        view.GetAttribute("id").Should().Be("main-container");
        view.GetAttribute("lang").Should().Be("en");
        view.GetAttribute("title").Should().Be("Main container");
        view.GetAttribute("data-testid").Should().Be("container-view");
        view.HasAttribute("autofocus").Should().BeTrue();
        view.GetAttribute("data-nt-container-view-quick-nav-enabled").Should().Be("true");
        view.GetAttribute("style").Should().Contain("--nt-container-view-on-this-page-label-color:var(--nt-color-on-surface-variant)");
        view.GetAttribute("style").Should().Contain("--nt-container-view-on-this-page-selector-color:var(--nt-color-outline)");
        view.GetAttribute("style").Should().Contain("--nt-container-view-on-this-page-selected-text-color:var(--nt-color-on-surface)");
        view.GetAttribute("style").Should().Contain("--nt-container-view-on-this-page-text-color:var(--nt-color-on-surface)");
        cut.Find(".nt-container-view-quick-nav").HasAttribute("hidden").Should().BeTrue();
        cut.Find(".nt-container-view-quick-nav").GetAttribute("aria-label").Should().Be("On this page");
        cut.Find(".nt-container-view-quick-nav-title").TextContent.Should().Be("On this page");
        cut.FindAll(".nt-container-view-quick-nav-heading").Should().BeEmpty();
        cut.Find(".nt-container-view-content article").TextContent.Should().Be("Container content");
        view.InnerHtml.Should().Contain("<article>Container content</article>");
        cut.Find("nt-page-script").GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTContainerView.razor.js");
    }

    [Fact]
    public void Container_Renders_Optional_OnThisPage_Title_After_Label() {
        var cut = Render<NTContainerView>(p => p
            .Add(c => c.OnThisPageLabel, "Page contents")
            .Add(c => c.OnThisPageAriaLabel, "Container page contents")
            .Add(c => c.OnThisPageTitle, "Menus")
            .AddChildContent("<h2>Heading</h2><p>Content</p>"));

        var quickNav = cut.Find(".nt-container-view-quick-nav");
        var label = cut.Find(".nt-container-view-quick-nav-title");
        var title = cut.Find(".nt-container-view-quick-nav-heading");

        quickNav.GetAttribute("aria-label").Should().Be("Container page contents");
        label.TextContent.Should().Be("Page contents");
        title.TextContent.Should().Be("Menus");
        quickNav.InnerHtml.IndexOf("Page contents", StringComparison.Ordinal)
            .Should()
            .BeLessThan(quickNav.InnerHtml.IndexOf("Menus", StringComparison.Ordinal));
    }

    [Fact]
    public void Container_Renders_Collapsed_OnThisPage_Toggle_With_Custom_Label() {
        var cut = Render<NTContainerView>(p => p
            .Add(c => c.OnThisPageLabel, "Page contents")
            .AddChildContent("<h2>Heading</h2>"));

        var toggle = cut.Find(".nt-container-view-quick-nav-toggle");

        toggle.TagName.Should().Be("BUTTON");
        toggle.GetAttribute("type").Should().Be("button");
        toggle.GetAttribute("aria-expanded").Should().Be("false");
        toggle.TextContent.Should().Contain("Page contents");
    }

    [Fact]
    public void Container_Renders_PageScript_Inside_View_For_Static_Enhancement() {
        var cut = Render<NTContainerView>(p => p
            .AddChildContent("<h2>Heading</h2><p>Content</p>"));

        var view = cut.Find("div.nt-container-view");
        var pageScript = view.QuerySelector("nt-page-script");

        pageScript.Should().NotBeNull();
        pageScript!.GetAttribute("src").Should().Be("./_content/NTComponents/Layout/Views/NTContainerView.razor.js");
    }

    [Fact]
    public void Container_Can_Disable_OnThisPage_Navigation() {
        var cut = Render<NTContainerView>(p => p
            .Add(c => c.EnableOnThisPageNavigation, false)
            .AddChildContent("<h2>Heading</h2><p>Content</p>"));

        var view = cut.Find("div.nt-container-view");

        cut.Find(".nt-container-view-content h2").TextContent.Should().Be("Heading");
        view.InnerHtml.Should().Contain("<h2>Heading</h2>");
        view.HasAttribute("data-nt-container-view-quick-nav-enabled").Should().BeFalse();
        cut.Instance.IsolatedJsModule.Should().BeNull();
        cut.FindAll(".nt-container-view-quick-nav").Should().BeEmpty();
        cut.FindAll("nt-page-script").Should().BeEmpty();
    }

    [Fact]
    public void Container_Emits_OnThisPage_Color_Variables() {
        var cut = Render<NTContainerView>(p => p
            .Add(c => c.OnThisPageLabelColor, NTColor.Primary)
            .Add(c => c.OnThisPageSelectorColor, NTColor.Secondary)
            .Add(c => c.OnThisPageSelectedTextColor, NTColor.Tertiary)
            .Add(c => c.OnThisPageTextColor, NTColor.OnSurface)
            .AddChildContent("<h2>Heading</h2>"));

        var style = cut.Find("div.nt-container-view").GetAttribute("style");

        style.Should().Contain("--nt-container-view-on-this-page-label-color:var(--nt-color-primary)");
        style.Should().Contain("--nt-container-view-on-this-page-selector-color:var(--nt-color-secondary)");
        style.Should().Contain("--nt-container-view-on-this-page-selected-text-color:var(--nt-color-tertiary)");
        style.Should().Contain("--nt-container-view-on-this-page-text-color:var(--nt-color-on-surface)");
    }

    [Fact]
    public void MultiPane_Renders_ChildContent_And_Clamped_MaxPaneCount_Classes() {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.PaneCount, 8)
            .Add(c => c.MinPaneWidth, NTMultiPaneViewMinPaneWidth.Large)
            .AddChildContent("<section>First pane</section><section>Second pane</section>"));

        var view = cut.Find("div.nt-multi-pane-view");

        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-5-panes");
        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-min-pane-width-large");
        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-staged-sizing");
        view.GetAttribute("style").Should().BeNull();
        view.GetAttribute("class")!.Should().NotContain("nt-multi-pane-view-enforce-even-sizing");
        view.TextContent.Should().Contain("First pane");
        view.TextContent.Should().Contain("Second pane");
        view.Children.Should().HaveCount(2);
    }

    [Fact]
    public void MultiPane_Adds_EnforceEvenSizing_Class_When_Enabled() {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.EnforceEvenSizing, true)
            .AddChildContent("<section>Pane</section>"));

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .Contain("nt-multi-pane-view-enforce-even-sizing");

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .NotContain("nt-multi-pane-view-staged-sizing");
        cut.FindAll("style").Should().BeEmpty();
    }

    [Fact]
    public void MultiPane_Uses_Static_StagedSizing_For_MinPaneWidth() {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.PaneCount, 5)
            .Add(c => c.MinPaneWidth, NTMultiPaneViewMinPaneWidth.ExtraLarge)
            .AddChildContent("<section>One</section><section>Two</section><section>Three</section><section>Four</section><section>Five</section>"));

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .Contain("nt-multi-pane-view-staged-sizing")
            .And
            .Contain("nt-multi-pane-view-min-pane-width-extra-large");
        cut.FindAll("style").Should().BeEmpty();
    }

    [Theory]
    [InlineData(NTMultiPaneViewMinPaneWidth.Compact, "nt-multi-pane-view-min-pane-width-compact")]
    [InlineData(NTMultiPaneViewMinPaneWidth.Medium, "nt-multi-pane-view-min-pane-width-medium")]
    [InlineData(NTMultiPaneViewMinPaneWidth.Large, "nt-multi-pane-view-min-pane-width-large")]
    [InlineData(NTMultiPaneViewMinPaneWidth.ExtraLarge, "nt-multi-pane-view-min-pane-width-extra-large")]
    public void MultiPane_Adds_MinPaneWidth_Class(NTMultiPaneViewMinPaneWidth minPaneWidth, string expectedClass) {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.MinPaneWidth, minPaneWidth)
            .AddChildContent("<section>Pane</section>"));

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .Contain(expectedClass);
        cut.FindAll("style").Should().BeEmpty();
    }

    [Fact]
    public void MultiPane_Updates_Layout_Classes_On_Same_Rendered_Instance() {
        var cut = Render<MultiPaneHost>();

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .Contain("nt-multi-pane-view-2-panes")
            .And
            .Contain("nt-multi-pane-view-min-pane-width-compact");

        cut.Instance.PaneCount = 5;
        cut.Instance.MinPaneWidth = NTMultiPaneViewMinPaneWidth.ExtraLarge;
        cut.Render();

        cut.Find("div.nt-multi-pane-view")
            .GetAttribute("class")!
            .Should()
            .Contain("nt-multi-pane-view-5-panes")
            .And
            .Contain("nt-multi-pane-view-min-pane-width-extra-large")
            .And
            .NotContain("nt-multi-pane-view-2-panes")
            .And
            .NotContain("nt-multi-pane-view-min-pane-width-compact");
    }

    [Fact]
    public void MultiPane_Normalizes_Invalid_MinPaneWidth_To_Medium_Class() {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.MinPaneWidth, (NTMultiPaneViewMinPaneWidth)999)
            .AddChildContent("<section>Pane</section>"));

        var view = cut.Find("div.nt-multi-pane-view");

        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-min-pane-width-medium");
        view.GetAttribute("style").Should().BeNull();
        cut.FindAll("style").Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    public void MultiPane_Clamps_PaneCount_To_Supported_Class_And_Variables(int paneCount, int expectedPaneCount) {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.PaneCount, paneCount)
            .AddChildContent("<section>Pane</section>"));

        var view = cut.Find("div.nt-multi-pane-view");

        ShouldHaveScopedCssAttribute(view);
        view.GetAttribute("class")!.Should().Contain($"nt-multi-pane-view-{expectedPaneCount}-panes");
        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-min-pane-width-medium");
        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-staged-sizing");
        view.GetAttribute("style").Should().BeNull();
    }

    [Fact]
    public void MultiPane_Defaults_To_Staged_Sizing_Mode() {
        var cut = Render<NTMultiPaneView>(p => p
            .Add(c => c.PaneCount, 5)
            .AddChildContent("<section>One</section><section>Two</section><section>Three</section>"));

        var view = cut.Find("div.nt-multi-pane-view");

        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-5-panes");
        view.GetAttribute("class")!.Should().Contain("nt-multi-pane-view-staged-sizing");
        view.GetAttribute("class")!.Should().NotContain("nt-multi-pane-view-enforce-even-sizing");
        cut.FindAll("style").Should().BeEmpty();
    }

    [Fact]
    public void CanonicalView_Preserves_Unmatched_Id_Lang_And_Title() {
        var attributes = new Dictionary<string, object> {
            ["id"] = "feed-view",
            ["lang"] = "en",
            ["title"] = "Feed view"
        };

        var cut = Render<NTFeedView>(p => p.Add(c => c.AdditionalAttributes, attributes));

        var view = cut.Find("div.nt-feed-view");

        view.GetAttribute("id").Should().Be("feed-view");
        view.GetAttribute("lang").Should().Be("en");
        view.GetAttribute("title").Should().Be("Feed view");
    }

    [Fact]
    public void CanonicalView_Element_Parameters_Override_Unmatched_Id_Lang_And_Title() {
        var attributes = new Dictionary<string, object> {
            ["id"] = "feed-view",
            ["lang"] = "en",
            ["title"] = "Feed view"
        };

        var cut = Render<NTFeedView>(p => p
            .Add(c => c.AdditionalAttributes, attributes)
            .Add(c => c.ElementId, "explicit-id")
            .Add(c => c.ElementLang, "fr")
            .Add(c => c.ElementTitle, "Explicit title"));

        var view = cut.Find("div.nt-feed-view");

        view.GetAttribute("id").Should().Be("explicit-id");
        view.GetAttribute("lang").Should().Be("fr");
        view.GetAttribute("title").Should().Be("Explicit title");
    }

    private static RenderFragment Fragment(string content) => builder => builder.AddContent(0, content);

    private sealed class MultiPaneHost : ComponentBase {
        public int PaneCount { get; set; } = 2;

        public NTMultiPaneViewMinPaneWidth MinPaneWidth { get; set; } = NTMultiPaneViewMinPaneWidth.Compact;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTMultiPaneView>(0);
            builder.AddAttribute(1, nameof(NTMultiPaneView.PaneCount), PaneCount);
            builder.AddAttribute(2, nameof(NTMultiPaneView.MinPaneWidth), MinPaneWidth);
            builder.AddAttribute(3, nameof(NTMultiPaneView.ChildContent), Fragment("<section>Pane</section>"));
            builder.CloseComponent();
        }
    }

    private BunitJSModuleInterop SetupViewModule(string jsModulePath) {
        var module = JSInterop.SetupModule(jsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        return module;
    }

    private sealed class TestContainerView : NTContainerView {
        public Task InvokeOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);
    }

    private static void ShouldHaveScopedCssAttribute(IElement element) =>
        element.Attributes.Any(attribute => attribute.Name.StartsWith("b-", StringComparison.Ordinal)).Should().BeTrue();
}
