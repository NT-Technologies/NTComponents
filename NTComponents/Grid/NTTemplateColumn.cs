using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>    Renders a template-only column.</summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The templates are rendered by the parent grid into static markup. Interactive row, sort, paging, or virtualized behavior depends on the parent grid render mode.")]
public sealed class NTTemplateColumn<TItem> : NTDataGridColumn<TItem> where TItem : class {
    private bool _hasSortableParameter;

    /// <summary>    Gets or sets whether the column can be sorted.</summary>
    [Parameter]
    public override bool Sortable { get; set; }

    /// <summary>    Gets or sets the sorting rules for the column.</summary>
    [Parameter]
    public NTGridSort<TItem>? SortBy { get; set; }

    /// <summary>    Gets or sets the template rendered for each row.</summary>
    [Parameter]
    public RenderFragment<TItem>? ChildContent { get; set; }

    internal override string DefaultTitle => string.Empty;

    internal override string? SortPropertyName => !string.IsNullOrWhiteSpace(Title) ? Title : SortBy?.PropertyName;

    internal override SortDirection DefaultSortDirection => SortBy?.DefaultDirection ?? base.DefaultSortDirection;

    internal override IOrderedEnumerable<TItem>? ApplyLocalSort(IEnumerable<TItem> source, SortDirection direction, bool thenBy) => SortBy?.Apply(source, direction, thenBy);

    internal override IOrderedQueryable<TItem>? ApplyQueryableSort(IQueryable<TItem> source, SortDirection direction, bool thenBy) =>
        SortBy is null || source.Provider is EnumerableQuery<TItem> ? null : SortBy.Apply(source, direction, thenBy);

    internal override IReadOnlyList<NTSortDescriptor> GetSortDescriptors(SortDirection direction) => SortBy?.GetSortDescriptors(direction) ?? [];

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _hasSortableParameter = parameters.TryGetValue<bool>(nameof(Sortable), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!_hasSortableParameter) {
            Sortable = SortBy is not null;
        }

        base.OnParametersSet();
    }

    private protected override object? AdditionalState => (ChildContent, SortBy?.StateSignature);

    private protected override object? AdditionalSortState => SortBy?.StateSignature;

    internal override void RenderCell(RenderTreeBuilder builder, TItem item) {
        if ((CellTemplate ?? ChildContent) is { } template) {
            builder.AddContent(0, template, item);
        }
    }
}
