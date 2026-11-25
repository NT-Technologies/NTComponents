using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Grid;

public class NTDataGridComparer_Tests : BunitContext {
    public NTDataGridComparer_Tests() {
        SetRendererInfo(new RendererInfo("Server", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Virtualization/NTVirtualize.razor.js");
        module.SetupVoid().SetVoidResult();
        JSInterop.SetupVoid("NTComponents.updateGridUri", _ => true).SetVoidResult();
    }

    [Fact]
    public void Local_Fallback_Sort_Orders_Nulls_And_Comparable_Values_In_Both_Directions() {
        var items = new[] {
            new ComparerItem("Bravo", "Bravo"),
            new ComparerItem("Alpha", "Alpha"),
            new ComparerItem("Missing", null)
        }.AsQueryable();
        var permutedItems = new[] {
            new ComparerItem("Missing", null),
            new ComparerItem("Alpha", "Alpha"),
            new ComparerItem("Bravo", "Bravo")
        }.AsQueryable();
        var cut = RenderGrid(items);
        var permutedCut = RenderGrid(permutedItems);

        cut.WaitForAssertion(() => RowLabels(cut).Should().Equal("Missing", "Alpha", "Bravo"));
        permutedCut.WaitForAssertion(() => RowLabels(permutedCut).Should().Equal("Missing", "Alpha", "Bravo"));

        cut.Find(".nt-data-grid-sort-link").Click();

        cut.WaitForAssertion(() => RowLabels(cut).Should().Equal("Bravo", "Alpha", "Missing"));
        cut.Find("th").GetAttribute("aria-sort").Should().Be("descending");
    }

    [Fact]
    public void Local_Fallback_Sort_Uses_Culture_Text_For_NonComparable_Values() {
        var items = new[] {
            new ComparerItem("Second", new DisplayValue("Bravo")),
            new ComparerItem("First", new DisplayValue("Alpha"))
        }.AsQueryable();

        var cut = RenderGrid(items);

        cut.WaitForAssertion(() => RowLabels(cut).Should().Equal("First", "Second"));
    }

    [Fact]
    public void Local_Fallback_Sort_Preserves_Input_Order_For_The_Same_Key_Instance() {
        var sharedKey = new DisplayValue("Shared");
        var items = new[] {
            new ComparerItem("First", sharedKey),
            new ComparerItem("Second", sharedKey)
        }.AsQueryable();

        var cut = RenderGrid(items);

        cut.WaitForAssertion(() => RowLabels(cut).Should().Equal("First", "Second"));
    }

    private IRenderedComponent<NTDataGrid<ComparerItem>> RenderGrid(IQueryable<ComparerItem> items) =>
        Render<NTDataGrid<ComparerItem>>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.ChildContent, builder => {
                builder.OpenComponent<FallbackSortColumn>(0);
                builder.AddAttribute(1, nameof(FallbackSortColumn.InitialSortDirection), SortDirection.Ascending);
                builder.CloseComponent();
            }));

    private static IEnumerable<string> RowLabels(IRenderedComponent<NTDataGrid<ComparerItem>> cut) => cut.FindAll("tbody tr").Select(row => row.TextContent.Trim());

    private sealed record ComparerItem(string Label, object? Value);

    private sealed record DisplayValue(string Text) {
        public override string ToString() => Text;
    }

    private sealed class FallbackSortColumn : NTDataGridColumn<ComparerItem> {
        [Parameter]
        public override bool Sortable { get; set; } = true;

        internal override string DefaultTitle => "Value";

        internal override string? SortPropertyName => "Value";

        internal override object? GetSortValue(ComparerItem item) => item.Value;

        internal override void RenderCell(RenderTreeBuilder builder, ComparerItem item) => builder.AddContent(0, item.Label);
    }
}
