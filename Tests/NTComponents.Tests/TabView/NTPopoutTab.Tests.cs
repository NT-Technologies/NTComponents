using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.TabView;

public class NTPopoutTab_Tests : BunitContext {
    private static readonly string[] _labels = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight"];

    public NTPopoutTab_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Renderer.SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public void Docked_RendersTabPanelContractAndCornerButton() {
        var cut = RenderTabs(new bool[2]);

        var panel = cut.Find("#" + cut.Find("[role='tab']").GetAttribute("aria-controls"));
        panel.GetAttribute("role").Should().Be("tabpanel");
        panel.GetAttribute("data-nt-tab-panel").Should().Be("true");
        panel.HasAttribute("data-nt-window").Should().BeFalse();
        panel.ClassList.Should().Contain("nt-tab-view-panel").And.NotContain("nt-window");
        panel.QuerySelector(".nt-popout-tab-action button[aria-label='Open One in a window']").Should().NotBeNull();
        cut.FindAll("nt-page-script[src*='NTWindow']").Should().BeEmpty();
    }

    [Fact]
    public void StaticRendering_OmitsPopoutButton() {
        Renderer.SetRendererInfo(new RendererInfo("Static", false));

        var cut = RenderTabs(new bool[2]);

        cut.FindAll(".nt-popout-tab-action").Should().BeEmpty();
        cut.FindAll("[role='tabpanel']").Should().HaveCount(2);
    }

    [Theory]
    [InlineData(NTPopoutTabButtonPlacement.TopStart, "nt-popout-tab-action-top-start")]
    [InlineData(NTPopoutTabButtonPlacement.BottomEnd, "nt-popout-tab-action-bottom-end")]
    [InlineData(NTPopoutTabButtonPlacement.BottomStart, "nt-popout-tab-action-bottom-start")]
    public void PopoutButtonPlacement_AppliesCornerClass(NTPopoutTabButtonPlacement placement, string expectedClass) {
        var cut = RenderTabs(new bool[1], placement);

        cut.Find(".nt-popout-tab-action").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void PopoutButtonPlacementNone_OmitsButton() {
        var cut = RenderTabs(new bool[1], NTPopoutTabButtonPlacement.None);

        cut.FindAll(".nt-popout-tab-action").Should().BeEmpty();
    }

    [Fact]
    public void PopoutButton_FloatsTabAndCloseReturnsItToSamePositionKeepingContentState() {
        var poppedOut = new bool[3];
        var cut = RenderTabs(poppedOut);
        CounterOf(cut, "Two").Increment();

        FindPanelByText(cut, "Two content").QuerySelector(".nt-popout-tab-action button")!.Click();

        cut.WaitForAssertion(() => HeaderLabels(cut).Should().Equal("One", "Three"));
        poppedOut.Should().Equal(false, true, false);
        var window = cut.Find("[data-nt-tab-detached='true']");
        window.GetAttribute("data-nt-window").Should().Be("true");
        window.GetAttribute("role").Should().Be("dialog");
        window.HasAttribute("hidden").Should().BeFalse();
        window.QuerySelector(".nt-window-title")!.TextContent.Should().Be("Two");
        window.QuerySelector(".nt-popout-tab-action").Should().BeNull();
        CounterOf(cut, "Two").Count.Should().Be(1);

        window.QuerySelector("[data-nt-window-action='close']")!.Click();

        cut.WaitForAssertion(() => HeaderLabels(cut).Should().Equal("One", "Two", "Three"));
        poppedOut.Should().Equal(false, false, false);
        cut.FindAll("[data-nt-tab-detached]").Should().BeEmpty();
        cut.FindAll("[role='tabpanel']").Should().HaveCount(3);
        CounterOf(cut, "Two").Count.Should().Be(1, "the content component instance survives pop-out and return");
    }

    [Fact]
    public void EightTabs_PoppedOutAndReturnedInAnyOrder_KeepDeclarationOrder() {
        var poppedOut = new bool[8];
        var cut = RenderTabs(poppedOut);

        foreach (var index in new[] { 6, 1, 4, 0, 7, 3, 5, 2 }) {
            poppedOut[index] = true;
            cut.Render();
        }

        cut.WaitForAssertion(() => cut.FindAll("[role='tab']").Should().BeEmpty());
        cut.FindAll("[data-nt-tab-detached='true']").Should().HaveCount(8);
        cut.FindAll("[role='tabpanel']").Should().BeEmpty();

        var returned = new List<int>();
        foreach (var index in new[] { 5, 0, 7, 2, 4, 1, 6, 3 }) {
            poppedOut[index] = false;
            returned.Add(index);
            cut.Render();
            var expected = returned.Order().Select(i => _labels[i]).ToArray();
            cut.WaitForAssertion(() => HeaderLabels(cut).Should().Equal(expected));
        }

        cut.FindAll("[role='tabpanel']").Select(panel => panel.GetAttribute("aria-labelledby")).Should()
            .Equal(cut.FindAll("[role='tab']").Select(tab => tab.Id));
    }

    [Fact]
    public void PoppedOutSelectedTab_MovesInitialSelectionToNextAvailableTab() {
        var poppedOut = new bool[3];
        var cut = RenderTabs(poppedOut);

        poppedOut[0] = true;
        cut.Render();

        cut.WaitForAssertion(() => cut.Find("[role='tab'][aria-selected='true']").TextContent.Should().Contain("Two"));
        cut.FindAll("[role='tabpanel']:not([hidden])").Should().ContainSingle().Which.TextContent.Should().Contain("Two content");
    }

    [Fact]
    public void HeaderContextMenu_TargetsTabButtonAndOffersPopout() {
        var cut = RenderTabs(new bool[2]);

        var tabs = cut.FindAll("[role='tab']");
        var menus = cut.FindAll(".nt-tab-view-header > nt-context-menu");
        menus.Select(menu => menu.GetAttribute("data-target-id")).Should().Equal(tabs.Select(tab => tab.Id));
        cut.FindAll("[role='tablist'] nt-context-menu").Should().BeEmpty();
        cut.Find("nt-menu[aria-label='Two options']").TextContent.Should().Contain("Open Two in a window");
    }

    private static string[] HeaderLabels(IRenderedComponent<PopoutTabsHost> cut) => [.. cut.FindAll(".nt-tab-view-label").Select(label => label.TextContent)];

    private static AngleSharp.Dom.IElement FindPanelByText(IRenderedComponent<PopoutTabsHost> cut, string text) =>
        cut.FindAll("[role='tabpanel']").Single(panel => panel.TextContent.Contains(text, StringComparison.Ordinal));

    private static Counter CounterOf(IRenderedComponent<PopoutTabsHost> cut, string label) =>
        cut.FindComponents<Counter>().Single(counter => counter.Instance.Label == label).Instance;

    private IRenderedComponent<PopoutTabsHost> RenderTabs(bool[] poppedOut, NTPopoutTabButtonPlacement placement = NTPopoutTabButtonPlacement.TopEnd) {
        var cut = Render<PopoutTabsHost>(parameters => parameters
            .Add(p => p.PoppedOut, poppedOut)
            .Add(p => p.Placement, placement));
        cut.WaitForAssertion(() => cut.FindAll("[role='tab']").Should().HaveCount(poppedOut.Count(value => !value)));
        return cut;
    }

    /// <summary>Renders one NTPopoutTab per entry, two-way bound to the array so tests can pop tabs out and back in.</summary>
    public sealed class PopoutTabsHost : ComponentBase {
        [Parameter]
        public bool[] PoppedOut { get; set; } = [];

        [Parameter]
        public NTPopoutTabButtonPlacement Placement { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenComponent<NTTabView>(0);
            builder.AddComponentParameter(1, nameof(NTTabView.ChildContent), (RenderFragment)(tabs => {
                for (var index = 0; index < PoppedOut.Length; index++) {
                    var current = index;
                    tabs.OpenComponent<NTPopoutTab>(2);
                    tabs.SetKey(current);
                    tabs.AddComponentParameter(3, nameof(NTTab.Label), _labels[current]);
                    tabs.AddComponentParameter(4, nameof(NTPopoutTab.PoppedOut), PoppedOut[current]);
                    tabs.AddComponentParameter(5, nameof(NTPopoutTab.PoppedOutChanged), EventCallback.Factory.Create<bool>(this, value => PoppedOut[current] = value));
                    tabs.AddComponentParameter(6, nameof(NTPopoutTab.PopoutButtonPlacement), Placement);
                    tabs.AddComponentParameter(7, nameof(NTTab.ChildContent), (RenderFragment)(content => {
                        content.OpenComponent<Counter>(0);
                        content.AddComponentParameter(1, nameof(Counter.Label), _labels[current]);
                        content.CloseComponent();
                    }));
                    tabs.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }

    /// <summary>Holds private state so tests can tell a preserved component instance from a recreated one.</summary>
    public sealed class Counter : ComponentBase {
        [Parameter]
        public string Label { get; set; } = string.Empty;

        public int Count { get; private set; }

        public void Increment() => Count++;

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, $"{Label} content");
    }
}
