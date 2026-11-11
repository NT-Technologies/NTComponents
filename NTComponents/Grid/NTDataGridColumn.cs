using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Linq.Expressions;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Base type for columns rendered by <see cref="NTDataGrid{TItem}" />.
/// </summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "Column definitions participate in parent grid rendering without their own browser APIs. User-driven sort, row, paging, or virtualization behavior depends on the parent grid render mode.")]
public abstract class NTDataGridColumn<TItem> : ComponentBase, IDisposable where TItem : class {
    private RenderFragment<TItem>? _previousCellTemplate;
    private string? _previousHeaderTitle;
    private string? _previousMaxWidth;
    private string? _previousMinWidth;
    private string? _previousStateSignature;
    private string? _previousSortPropertyName;
    private string? _previousSortStateSignature;
    private string? _previousTitle;
    private string? _previousWidth;
    private SortDirection? _previousInitialSortDirection;
    private NTTextAlign? _previousHeaderTextAlign;
    private NTTextAlign? _previousTextAlign;
    private NTBreakpoint? _previousHideBelow;
    private bool _hasParameterState;
    private bool _previousSortable;
    private bool _registered;

    /// <summary>
    ///     Gets or sets the header title.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets whether the column can be sorted.
    /// </summary>
    public virtual bool Sortable { get; set; }

    /// <summary>
    ///     Gets or sets the initial sort direction.
    /// </summary>
    [Parameter]
    public SortDirection? InitialSortDirection { get; set; }

    /// <summary>
    ///     Gets or sets the body cell text alignment.
    /// </summary>
    [Parameter]
    public NTTextAlign? TextAlign { get; set; }

    /// <summary>
    ///     Gets or sets the header cell text alignment.
    /// </summary>
    [Parameter]
    public NTTextAlign? HeaderTextAlign { get; set; }

    /// <summary>
    ///     Gets or sets the preferred column width.
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    ///     Gets or sets the minimum column width.
    /// </summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>
    ///     Gets or sets the maximum column width.
    /// </summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>
    ///     Gets or sets the viewport breakpoint below which the column is hidden.
    /// </summary>
    /// <remarks>
    ///     Use it on low-priority columns so narrow screens show the columns that matter instead of scrolling sideways.
    ///     The column's header and cells are removed from the layout and the accessibility tree, so show values users still
    ///     need, for example in <see cref="NTDataGrid{TItem}.RowDetailTemplate" />. Sorting by a hidden column stays applied.
    /// </remarks>
    [Parameter]
    public NTBreakpoint? HideBelow { get; set; }

    /// <summary>
    ///     Gets or sets a custom cell template.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? CellTemplate { get; set; }

    [CascadingParameter]
    internal NTDataGrid<TItem>? Owner { get; set; }

    internal int Sequence { get; set; }

    internal string BodyCellClass { get; private set; } = "nt-data-grid-cell";

    internal bool CanSort { get; private set; }

    internal string HeaderCellClass { get; private set; } = "nt-data-grid-header-cell";

    internal string? ColumnClass { get; private set; }

    internal string? ColumnStyle { get; private set; }

    internal string HeaderTitle => !string.IsNullOrWhiteSpace(Title) ? Title! : DefaultTitle;

    internal abstract string DefaultTitle { get; }

    internal abstract string? SortPropertyName { get; }

    internal virtual SortDirection DefaultSortDirection => SortDirection.Ascending;

    internal virtual object? GetSortValue(TItem item) => null;

    internal virtual IOrderedQueryable<TItem>? ApplyQueryableSort(IQueryable<TItem> source, SortDirection direction, bool thenBy) => null;

    internal virtual IOrderedEnumerable<TItem>? ApplyEnumerableSort(IEnumerable<TItem> source, SortDirection direction, bool thenBy) => null;

    internal virtual IOrderedEnumerable<TItem>? ApplyLocalSort(IEnumerable<TItem> source, SortDirection direction, bool thenBy) => null;

    internal virtual IReadOnlyList<NTSortDescriptor> GetSortDescriptors(SortDirection direction) => [new(SortPropertyName!, direction)];

    internal abstract void RenderCell(RenderTreeBuilder builder, TItem item);

    /// <inheritdoc />
    protected override void OnInitialized() {
        if (Owner is null) {
            throw new InvalidOperationException($"{GetType().Name} must be nested inside {nameof(NTDataGrid<TItem>)}.");
        }

        Owner.RegisterColumn(this);
        _registered = true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        var headerTitle = HeaderTitle;
        var sortPropertyName = SortPropertyName;
        var stateSignature = GetStateSignature();
        var sortStateSignature = GetSortStateSignature();
        var sortStateChanged = !_hasParameterState
            || _previousSortable != Sortable
            || _previousInitialSortDirection != InitialSortDirection
            || !string.Equals(_previousSortPropertyName, sortPropertyName, StringComparison.Ordinal)
            || !string.Equals(_previousSortStateSignature, sortStateSignature, StringComparison.Ordinal)
            || HasAdditionalSortStateChanged();
        var stateChanged = sortStateChanged
            || !string.Equals(_previousTitle, Title, StringComparison.Ordinal)
            || _previousTextAlign != TextAlign
            || _previousHeaderTextAlign != HeaderTextAlign
            || !string.Equals(_previousWidth, Width, StringComparison.Ordinal)
            || !string.Equals(_previousMinWidth, MinWidth, StringComparison.Ordinal)
            || !string.Equals(_previousMaxWidth, MaxWidth, StringComparison.Ordinal)
            || _previousHideBelow != HideBelow
            || !Equals(_previousCellTemplate, CellTemplate)
            || !string.Equals(_previousHeaderTitle, headerTitle, StringComparison.Ordinal)
            || !string.Equals(_previousStateSignature, stateSignature, StringComparison.Ordinal)
            || HasAdditionalStateChanged();
        if (stateChanged) {
            UpdateCellClasses();
        }

        if (_registered && stateChanged) {
            Owner?.NotifyColumnChanged(this, sortStateChanged);
        }

        _hasParameterState = true;
        _previousTitle = Title;
        _previousSortable = Sortable;
        _previousInitialSortDirection = InitialSortDirection;
        _previousTextAlign = TextAlign;
        _previousHeaderTextAlign = HeaderTextAlign;
        _previousWidth = Width;
        _previousMinWidth = MinWidth;
        _previousMaxWidth = MaxWidth;
        _previousHideBelow = HideBelow;
        _previousCellTemplate = CellTemplate;
        _previousHeaderTitle = headerTitle;
        _previousSortPropertyName = sortPropertyName;
        _previousStateSignature = stateSignature;
        _previousSortStateSignature = sortStateSignature;
        CaptureAdditionalState();
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_registered) {
            Owner?.UnregisterColumn(this);
        }
    }

    /// <summary>
    /// Determines whether derived column state affecting rendered data has changed.
    /// </summary>
    /// <returns><see langword="true" /> when derived state has changed; otherwise, <see langword="false" />.</returns>
    private protected virtual bool HasAdditionalStateChanged() => false;

    /// <summary>
    /// Determines whether derived column state affecting sorting has changed.
    /// </summary>
    /// <returns><see langword="true" /> when derived sort state has changed; otherwise, <see langword="false" />.</returns>
    private protected virtual bool HasAdditionalSortStateChanged() => false;

    /// <summary>
    /// Captures derived column state after parameter changes have been processed.
    /// </summary>
    private protected virtual void CaptureAdditionalState() { }

    /// <summary>
    /// Gets derived column state that should notify the owning grid when it changes.
    /// </summary>
    /// <returns>A stable signature for derived data-affecting state.</returns>
    protected virtual string GetStateSignature() => string.Empty;

    /// <summary>
    /// Gets derived column state that should reset the owning grid sort state when it changes.
    /// </summary>
    /// <returns>A stable signature for derived sort-affecting state.</returns>
    protected virtual string GetSortStateSignature() => string.Empty;

    private void UpdateCellClasses() {
        ColumnClass = HideBelow switch {
            NTBreakpoint.Small => "nt-data-grid-hide-below-small",
            NTBreakpoint.Medium => "nt-data-grid-hide-below-medium",
            NTBreakpoint.Large => "nt-data-grid-hide-below-large",
            NTBreakpoint.ExtraLarge => "nt-data-grid-hide-below-extra-large",
            _ => null
        };
        var hideClass = ColumnClass is null ? string.Empty : $" {ColumnClass}";
        BodyCellClass = AddTextAlignment("nt-data-grid-cell", TextAlign) + hideClass;
        CanSort = Sortable && !string.IsNullOrWhiteSpace(SortPropertyName);
        HeaderCellClass = AddTextAlignment("nt-data-grid-header-cell", HeaderTextAlign ?? TextAlign) + hideClass;
        if (CanSort) {
            HeaderCellClass += " nt-data-grid-header-cell-sortable";
        }

        var style = string.Join(" ", new[] { Declaration("width", Width), Declaration("min-width", MinWidth), Declaration("max-width", MaxWidth) }.Where(declaration => declaration is not null));
        ColumnStyle = style.Length == 0 ? null : style;
    }

    private static string? Declaration(string propertyName, string? value) => string.IsNullOrWhiteSpace(value) ? null : $"{propertyName}: {value};";

    private static string AddTextAlignment(string className, NTTextAlign? textAlign) => textAlign is null ? className : $"{className} nt-data-grid-align-{textAlign.Value.ToString().ToLowerInvariant()}";

    internal static string GetMemberName<TValue>(Expression<Func<TItem, TValue>>? expression) {
        if (expression?.Body is MemberExpression memberExpression) {
            return memberExpression.Member.Name;
        }

        if (expression?.Body is UnaryExpression { Operand: MemberExpression unaryMemberExpression }) {
            return unaryMemberExpression.Member.Name;
        }

        return string.Empty;
    }
}
