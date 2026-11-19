using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Virtualization;
using System.Linq.Expressions;

namespace NTComponents.Tests.Grid;

public class NTDataGridProviderRefresh_Tests : BunitContext {
    public NTDataGridProviderRefresh_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Virtualization/NTVirtualize.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        module.SetupVoid("init", _ => true).SetVoidResult();
        module.SetupVoid("updateRenderState", _ => true).SetVoidResult();
        module.SetupVoid("updateOptions", _ => true).SetVoidResult();
        module.SetupVoid("scrollToItem", _ => true).SetVoidResult();
        JSInterop.SetupVoid("NTComponents.updateUri", _ => true).SetVoidResult();
    }

    [Fact]
    public async Task InitialColumns_UseOneUncanceledProviderRequestAfterConfigurationSettles() {
        var requests = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var items = new[] { new GridItem(1, "First", 10), new GridItem(2, "Second", 20) };

        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, async request => {
                requests.Add(request);
                await release.Task.WaitAsync(request.CancellationToken);
                return new NTItemsProviderResult<GridItem>(items, 3);
            })
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));

        cut.WaitForAssertion(() => requests.Should().NotBeEmpty());
        await cut.InvokeAsync(async () => await Task.Yield());

        requests.Should().ContainSingle();
        requests[0].StartIndex.Should().Be(0);
        requests[0].Count.Should().Be(2);
        requests[0].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Ascending));
        requests[0].CancellationToken.IsCancellationRequested.Should().BeFalse();

        release.SetResult();
        cut.WaitForAssertion(() => {
            cut.FindAll("thead th").Should().HaveCount(3);
            cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(2);
        });
        requests.Should().ContainSingle();
    }

    [Fact]
    public void SortingPagingPageSizeAndProviderReplacement_EachRefreshOnce() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var replacementCalls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Descending));

        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(3));
        calls[2].StartIndex.Should().Be(2);
        calls[2].Count.Should().Be(2);

        cut.Find(".nt-data-grid-page-size select").Change("3");
        cut.WaitForAssertion(() => calls.Should().HaveCount(4));
        calls[3].StartIndex.Should().Be(0);
        calls[3].Count.Should().Be(3);

        NTDataGridItemsProvider<GridItem> replacement = request => {
            replacementCalls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, replacement)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 3)
            .Add(grid => grid.ChildContent, Columns));

        cut.WaitForAssertion(() => replacementCalls.Should().HaveCount(1));
        replacementCalls[0].Count.Should().Be(3);
        calls.Should().HaveCount(4);
    }

    [Fact]
    public void UnchangedParentRender_DoesNotRefreshInitialSortAgain() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));

        calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Virtualized_UnchangedParentRenderAfterFirstLoad_KeepsVirtualizer() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries: false));
        var virtualizer = await LoadFirstWindowAsync(cut, calls);
        calls[0].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Ascending));

        // Re-renders like a parent would: same provider, and Blazor treats delegate parameters as changed, so the grid's OnParametersSetAsync runs.
        cut.Render(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries: false));

        cut.FindComponent<NTVirtualize<GridItem>>().Instance.Should().BeSameAs(virtualizer);
        calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Virtualized_UnchangedParentRenderAfterHeaderSort_KeepsVirtualizer(bool pushHistoryEntries) {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries));
        await LoadFirstWindowAsync(cut, calls);

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        calls.Clear();
        var virtualizer = await LoadFirstWindowAsync(cut, calls);
        calls[0].Sorts.Should().Equal(new NTSortDescriptor("Name", SortDirection.Descending));

        cut.Render(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries));

        cut.FindComponent<NTVirtualize<GridItem>>().Instance.Should().BeSameAs(virtualizer);
        calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Virtualized_UnchangedParentRenderAfterHistoryNavigation_KeepsVirtualizer() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries: true));
        await LoadFirstWindowAsync(cut, calls);

        Services.GetRequiredService<NavigationManager>().NavigateTo("?ntdg-sort=Amount:desc");
        calls.Clear();
        var virtualizer = await LoadFirstWindowAsync(cut, calls);
        calls[0].Sorts.Should().Equal(new NTSortDescriptor("Amount", SortDirection.Descending));

        cut.Render(parameters => AddVirtualizedParameters(parameters, provider, pushHistoryEntries: true));

        cut.FindComponent<NTVirtualize<GridItem>>().Instance.Should().BeSameAs(virtualizer);
        calls.Should().ContainSingle();
    }

    [Fact]
    public async Task HistoryEntries_LateLocationChangedForOwnNavigation_DoesNotRefreshAgain() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: true));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));
        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        navigation.Uri.Should().Contain("ntdg-page=2");

        // A real browser confirms the grid's NavigateTo later, after the grid has already applied that URL.
        await cut.InvokeAsync(() => navigation.NavigateTo(navigation.Uri));

        calls.Should().HaveCount(2);
        cut.Find(".pagination-btn[aria-current='page']").TextContent.Should().Be("2");
    }

    [Fact]
    public async Task HistoryEntries_LateLocationChangedWithDifferentlyEncodedOwnUrl_DoesNotRefreshAgain() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: true));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));
        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        navigation.Uri.Should().Contain("ntdg-sort=Name%3Aasc");

        await cut.InvokeAsync(() => navigation.NavigateTo(navigation.Uri.Replace("%3A", ":", StringComparison.Ordinal)));

        calls.Should().HaveCount(2);
    }

    [Fact]
    public void ParentRenderWhileBrowserUriUpdates_KeepsNewPage() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries: false));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));
        var uriUpdate = JSInterop.SetupVoid("NTComponents.updateUri", _ => true);

        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        cut.Render(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries: false));
        uriUpdate.SetVoidResult();

        calls.Should().HaveCount(2);
        cut.Find(".pagination-btn[aria-current='page']").TextContent.Should().Be("2");
    }

    [Fact]
    public void HistoryGrids_PagingOneGrid_DoesNotRefreshTheOther() {
        var first = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var second = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var cut = Render(builder => {
            RenderHistoryGrid(builder, "first", TrackRequests(first));
            RenderHistoryGrid(builder, "second", TrackRequests(second));
        });
        cut.WaitForAssertion(() => {
            first.Should().HaveCount(1);
            second.Should().HaveCount(1);
        });

        cut.FindAll(".pagination-next-page")[0].Click();

        cut.WaitForAssertion(() => first.Should().HaveCount(2));
        Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("first-page=2");
        second.Should().ContainSingle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrelatedQueryChange_DoesNotRefresh(bool pushHistoryEntries) {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("?filter=open"));
        cut.Render(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries));

        calls.Should().ContainSingle();
    }

    [Fact]
    public void HistoryEntries_PageSizeChange_KeepsNewSize() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: true));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.Find(".nt-data-grid-page-size select").Change("3");

        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].Count.Should().Be(3);
        cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(3);
    }

    [Fact]
    public void HistoryEntries_InitialSortCycledToNone_StaysUnsorted() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: true));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => calls.Should().HaveCount(3));
        calls[2].Sorts.Should().BeEmpty();
        // An explicit empty sort keeps the URL from meaning "use the initial sort" on Back, reload, or a shared link.
        Services.GetRequiredService<NavigationManager>().Uri.Should().MatchRegex("ntdg-sort=(&|#|$)");
    }

    [Fact]
    public void StaticSortLinkToNone_KeepsExplicitEmptySort() {
        SetRendererInfo(new RendererInfo("Static", false));
        Services.GetRequiredService<NavigationManager>().NavigateTo("?ntdg-sort=Name%3Adesc");
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: false));

        cut.WaitForAssertion(() => calls.Should().NotBeEmpty());
        cut.FindAll("a.nt-data-grid-sort-link").Single(link => link.TextContent.Contains("Name", StringComparison.Ordinal))
            .GetAttribute("href").Should().MatchRegex("ntdg-sort=(&|#|$)");
    }

    [Fact]
    public async Task HistoryEntries_UnrelatedQueryChange_UpdatesPaginationLinksWithoutRefreshing() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, TrackRequests(calls), pushHistoryEntries: true));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("?filter=open"));

        cut.Find(".pagination-next-page").GetAttribute("href").Should().Contain("filter=open");
        calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalNavigationToGridQuery_RefreshesOnce(bool pushHistoryEntries) {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var provider = TrackRequests(calls);
        var cut = Render<NTDataGrid<GridItem>>(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));

        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("?filter=open&ntdg-page=2"));
        cut.Render(parameters => AddPagedParameters(parameters, provider, pushHistoryEntries));

        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].StartIndex.Should().Be(2);
        cut.Find(".pagination-btn[aria-current='page']").TextContent.Should().Be("2");
    }

    [Fact]
    public void DataAndSortColumnChanges_EachRefreshOnce() {
        var calls = new List<NTDataGridItemsProviderRequest<GridItem>>();
        NTDataGridItemsProvider<GridItem> provider = request => {
            calls.Add(request);
            return ValueTask.FromResult(CreateResult(request));
        };
        var format = "D2";
        Expression<Func<GridItem, int>> property = item => item.Id;
        RenderFragment columns = builder => {
            builder.OpenComponent<NTPropertyColumn<GridItem, int>>(0);
            builder.AddAttribute(1, nameof(NTPropertyColumn<GridItem, int>.Property), property);
            builder.AddAttribute(2, nameof(NTPropertyColumn<GridItem, int>.Format), format);
            builder.AddAttribute(3, nameof(NTPropertyColumn<GridItem, int>.InitialSortDirection), SortDirection.Ascending);
            builder.CloseComponent();
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(1));
        calls[0].Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Ascending));

        format = "D3";
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(2));
        calls[1].Sorts.Should().Equal(new NTSortDescriptor("Id", SortDirection.Ascending));

        property = item => item.Amount;
        cut.Render(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, columns));
        cut.WaitForAssertion(() => calls.Should().HaveCount(3));
        calls[2].Sorts.Should().Equal(new NTSortDescriptor("Amount", SortDirection.Ascending));
    }

    [Fact]
    public async Task PagingWhileSortRequestIsPending_CancelsOnlySupersededRequest() {
        var pending = new List<NTDataGridItemsProviderRequest<GridItem>>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holdRequests = false;
        NTDataGridItemsProvider<GridItem> provider = async request => {
            if (holdRequests) {
                pending.Add(request);
                await release.Task.WaitAsync(request.CancellationToken);
            }

            return CreateResult(request);
        };
        var cut = Render<NTDataGrid<GridItem>>(parameters => parameters
            .Add(grid => grid.ItemsProvider, provider)
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 2)
            .Add(grid => grid.ChildContent, Columns));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(2));
        holdRequests = true;

        cut.FindAll("thead button").Single(button => button.TextContent.Contains("Name", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => pending.Should().HaveCount(1));
        pending[0].CancellationToken.IsCancellationRequested.Should().BeFalse();

        cut.Find(".pagination-next-page").Click();
        cut.WaitForAssertion(() => pending.Should().HaveCount(2));
        pending[0].CancellationToken.IsCancellationRequested.Should().BeTrue();
        pending[1].CancellationToken.IsCancellationRequested.Should().BeFalse();
        pending[1].StartIndex.Should().Be(2);

        release.SetResult();
        cut.WaitForAssertion(() => cut.Find("tbody").TextContent.Should().Contain("Third"));
        pending[1].CancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    private static NTDataGridItemsProvider<GridItem> TrackRequests(List<NTDataGridItemsProviderRequest<GridItem>> calls) => request => {
        calls.Add(request);
        return ValueTask.FromResult(CreateResult(request));
    };

    private static ComponentParameterCollectionBuilder<NTDataGrid<GridItem>> AddPagedParameters(ComponentParameterCollectionBuilder<NTDataGrid<GridItem>> parameters, NTDataGridItemsProvider<GridItem> provider, bool pushHistoryEntries) => parameters
        .Add(grid => grid.ItemsProvider, provider)
        .Add(grid => grid.ShowPagination, true)
        .Add(grid => grid.PageSize, 2)
        .Add(grid => grid.PushHistoryEntries, pushHistoryEntries)
        .Add(grid => grid.ChildContent, Columns);

    private static void RenderHistoryGrid(RenderTreeBuilder builder, string queryParameterPrefix, NTDataGridItemsProvider<GridItem> provider) {
        builder.OpenComponent<NTDataGrid<GridItem>>(0);
        builder.SetKey(queryParameterPrefix);
        builder.AddAttribute(1, nameof(NTDataGrid<GridItem>.ItemsProvider), provider);
        builder.AddAttribute(2, nameof(NTDataGrid<GridItem>.ShowPagination), true);
        builder.AddAttribute(3, nameof(NTDataGrid<GridItem>.PageSize), 2);
        builder.AddAttribute(4, nameof(NTDataGrid<GridItem>.PushHistoryEntries), true);
        builder.AddAttribute(5, nameof(NTDataGrid<GridItem>.QueryParameterPrefix), queryParameterPrefix);
        builder.AddAttribute(6, nameof(NTDataGrid<GridItem>.ChildContent), Columns);
        builder.CloseComponent();
    }

    private static ComponentParameterCollectionBuilder<NTDataGrid<GridItem>> AddVirtualizedParameters(ComponentParameterCollectionBuilder<NTDataGrid<GridItem>> parameters, NTDataGridItemsProvider<GridItem> provider, bool pushHistoryEntries) => parameters
        .Add(grid => grid.ItemsProvider, provider)
        .Add(grid => grid.Virtualize, true)
        .Add(grid => grid.VirtualizationInitialItemCount, 2)
        .Add(grid => grid.VirtualizationPlaceholderPreloadWindowCount, 0)
        .Add(grid => grid.VirtualizationBackgroundPreloadWindowCount, 0)
        .Add(grid => grid.PushHistoryEntries, pushHistoryEntries)
        .Add(grid => grid.ChildContent, Columns);

    private static async Task<NTVirtualize<GridItem>> LoadFirstWindowAsync(IRenderedComponent<NTDataGrid<GridItem>> cut, List<NTDataGridItemsProviderRequest<GridItem>> calls) {
        var virtualizer = cut.FindComponent<NTVirtualize<GridItem>>().Instance;
        await cut.InvokeAsync(() => virtualizer.LoadItems(0, 0, 0, 2));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.nt-data-grid-row").Should().HaveCount(2));
        calls.Should().ContainSingle();
        return virtualizer;
    }

    private static NTItemsProviderResult<GridItem> CreateResult(NTDataGridItemsProviderRequest<GridItem> request) {
        var items = new[] { new GridItem(1, "First", 10), new GridItem(2, "Second", 20), new GridItem(3, "Third", 30), new GridItem(4, "Fourth", 40), new GridItem(5, "Fifth", 50) };
        return new NTItemsProviderResult<GridItem>(items.Skip(request.StartIndex).Take(request.Count ?? items.Length).ToArray(), items.Length);
    }

    private static RenderFragment Columns => builder => {
        builder.OpenComponent<NTPropertyColumn<GridItem, int>>(0);
        builder.AddAttribute(1, nameof(NTPropertyColumn<GridItem, int>.Property), (Expression<Func<GridItem, int>>)(item => item.Id));
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<GridItem, string>>(2);
        builder.AddAttribute(3, nameof(NTPropertyColumn<GridItem, string>.Property), (Expression<Func<GridItem, string>>)(item => item.Name));
        builder.AddAttribute(4, nameof(NTPropertyColumn<GridItem, string>.InitialSortDirection), SortDirection.Ascending);
        builder.CloseComponent();
        builder.OpenComponent<NTPropertyColumn<GridItem, int>>(5);
        builder.AddAttribute(6, nameof(NTPropertyColumn<GridItem, int>.Property), (Expression<Func<GridItem, int>>)(item => item.Amount));
        builder.CloseComponent();
    };

    private sealed record GridItem(int Id, string Name, int Amount);
}
