namespace NTComponents.Analyzers.Tests;

public sealed class NTDataGridStaleNavigationUri_Tests {
    private const string Merge = "new System.Collections.Generic.Dictionary<string, object?> { [\"status\"] = \"open\" }";

    private static async Task<string[]> AnalyzeAsync(string usage, string gridParameters = "", string declaration = "public class Page : ComponentBase", string members = "[Inject] private NavigationManager Navigation { get; set; } = default!;") =>
        (await AnalyzerHarness.AnalyzeAsync($$"""
            using System;
            using System.Linq;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using NTComponents;

            {{declaration}} {
                {{members}}
                private NTDataGrid<string>? _grid;
                private bool _push;

                protected override void BuildRenderTree(RenderTreeBuilder builder) {
                    builder.OpenComponent<NTDataGrid<string>>(0);
                    builder.AddComponentParameter(1, "Items", Array.Empty<string>().AsQueryable());
                    {{gridParameters}}
                    builder.CloseComponent();
                }

                private object Use() => {{usage}};
            }
            """)).Ids();

    [Theory]
    [InlineData("Navigation.Uri")]
    [InlineData("this.Navigation.Uri")]
    [InlineData("Navigation.GetUriWithQueryParameter(\"status\", \"open\")")]
    [InlineData($"Navigation.GetUriWithQueryParameters({Merge})")]
    public async Task ReadingNavigationUri_Reports(string usage) {
        var ids = await AnalyzeAsync(usage);

        Assert.Equal(["NTC1113"], ids);
    }

    [Fact]
    public async Task PrimaryConstructorNavigationManager_Reports() {
        var ids = await AnalyzeAsync("navigation.Uri", declaration: "public class Page(NavigationManager navigation) : ComponentBase", members: "");

        Assert.Equal(["NTC1113"], ids);
    }

    [Theory]
    [InlineData("builder.AddComponentParameter(2, \"PushHistoryEntries\", true);")]
    [InlineData("builder.AddComponentParameter(2, \"PushHistoryEntries\", _push);")]
    public async Task PushHistoryEntriesEnabledOrUnknown_DoesNotReport(string gridParameters) {
        var ids = await AnalyzeAsync("Navigation.Uri", gridParameters);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData($"Navigation.GetUriWithQueryParameters(new System.Collections.Generic.Dictionary<string, object?>(_grid!.QueryParameters) {{ [\"status\"] = \"open\" }})")]
    [InlineData($"Navigation.GetUriWithQueryParameters(\"https://example.test/orders\", {Merge})")]
    [InlineData("Navigation.BaseUri")]
    public async Task MergedOrUnrelatedNavigationUse_DoesNotReport(string usage) {
        var ids = await AnalyzeAsync(usage);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentWithoutNavigationManager_DoesNotReport() {
        var ids = await AnalyzeAsync("\"Uri\"", members: "");

        Assert.Empty(ids);
    }
}
