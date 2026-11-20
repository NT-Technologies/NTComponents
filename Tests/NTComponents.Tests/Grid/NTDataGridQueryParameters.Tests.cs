using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace NTComponents.Tests.Grid;

public class NTDataGridQueryParameters_Tests : BunitContext {
    private readonly IQueryable<Item> _items = new[] { new Item("Gamma"), new Item("Alpha"), new Item("Beta") }.AsQueryable();

    public NTDataGridQueryParameters_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        JSInterop.SetupVoid("NTComponents.updateUri", _ => true).SetVoidResult();
    }

    [Fact]
    public void AfterHeaderSort_HostUrlKeepsGridState() {
        // Arrange
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("https://example.test/orders?status=open");
        var cut = RenderGrid(NameColumn(initialSort: null));

        // Act
        cut.Find(".nt-data-grid-sort-link").Click();
        var hostUri = navigation.GetUriWithQueryParameters(new Dictionary<string, object?>(cut.Instance.QueryParameters) { ["status"] = "closed" });

        // Assert: NavigationManager.Uri misses the sort, but a host URL built with QueryParameters keeps it.
        navigation.Uri.Should().NotContain("ntdg-sort");
        hostUri.Should().Be("https://example.test/orders?status=closed&ntdg-page=1&ntdg-sort=Name%3Aasc");
    }

    [Fact]
    public void ClearedInitialSort_WritesExplicitEmptySort() {
        // Arrange
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderGrid(NameColumn(SortDirection.Ascending));

        // Act: ascending, descending, then cleared.
        cut.Find(".nt-data-grid-sort-link").Click();
        cut.Find(".nt-data-grid-sort-link").Click();

        // Assert: an empty value keeps "unsorted" from reverting to the initial sort; null would remove the parameter.
        cut.Instance.QueryParameters["ntdg-sort"].Should().Be(string.Empty);
        navigation.GetUriWithQueryParameters(cut.Instance.QueryParameters).Should().MatchRegex("ntdg-sort=(&|$)");
    }

    [Fact]
    public void UnsortedGridWithoutInitialSort_RemovesSortParameter() {
        // Arrange
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderGrid(NameColumn(initialSort: null));

        // Act
        var parameters = cut.Instance.QueryParameters;

        // Assert
        parameters["ntdg-sort"].Should().BeNull();
        navigation.GetUriWithQueryParameters("https://example.test/orders?ntdg-sort=Name%3Aasc", parameters).Should().NotContain("ntdg-sort");
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, 1)]
    public void AfterPaging_ReportsPageAndPersistedPageSize(bool persistPageSize, int? expectedPageSize) {
        // Arrange
        var cut = RenderGrid(NameColumn(initialSort: null), parameters => parameters
            .Add(grid => grid.ShowPagination, true)
            .Add(grid => grid.PageSize, 1)
            .Add(grid => grid.PersistPageSizeInQuery, persistPageSize));

        // Act
        cut.Find(".pagination-next-page").Click();

        // Assert
        cut.Instance.QueryParameters.Should().BeEquivalentTo(new Dictionary<string, object?> {
            ["ntdg-page"] = 2,
            ["ntdg-sort"] = null,
            ["ntdg-pageSize"] = expectedPageSize
        });
    }

    [Theory]
    [InlineData("orders", "orders-page", "orders-sort", "orders-pageSize")]
    [InlineData(" ", "page", "sort", "pageSize")]
    public void QueryParameterPrefix_NamesParameters(string prefix, string page, string sort, string pageSize) {
        // Arrange
        var cut = RenderGrid(NameColumn(initialSort: null), parameters => parameters.Add(grid => grid.QueryParameterPrefix, prefix));

        // Act
        var parameters = cut.Instance.QueryParameters;

        // Assert
        parameters.Keys.Should().BeEquivalentTo(page, sort, pageSize);
    }

    private IRenderedComponent<NTDataGrid<Item>> RenderGrid(RenderFragment columns, Action<ComponentParameterCollectionBuilder<NTDataGrid<Item>>>? configure = null) =>
        Render<NTDataGrid<Item>>(parameters => {
            parameters.Add(grid => grid.Items, _items).Add(grid => grid.ChildContent, columns);
            configure?.Invoke(parameters);
        });

    private static RenderFragment NameColumn(SortDirection? initialSort) => builder => {
        builder.OpenComponent<NTPropertyColumn<Item, string>>(0);
        builder.AddAttribute(1, nameof(NTPropertyColumn<Item, string>.Title), "Name");
        builder.AddAttribute(2, nameof(NTPropertyColumn<Item, string>.Property), (Expression<Func<Item, string>>)(item => item.Name));
        if (initialSort is { } direction) {
            builder.AddAttribute(3, nameof(NTPropertyColumn<Item, string>.InitialSortDirection), direction);
        }

        builder.CloseComponent();
    };

    private sealed record Item(string Name);
}
