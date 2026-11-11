using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using NTComponents.Core;
using NTComponents.Ext;
using NTComponents.Virtualization;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
/// Density options for <see cref="NTDataGrid{TItem}" />.
/// </summary>
public enum NTDataGridDensity {
    /// <summary>
    /// Default row density.
    /// </summary>
    Standard,

    /// <summary>
    /// Compact row density.
    /// </summary>
    Compact
}

/// <summary>
/// Appearance options for <see cref="NTDataGrid{TItem}" />.
/// </summary>
public enum NTDataGridAppearance {
    /// <summary>
    /// Default grid appearance.
    /// </summary>
    Default,

    /// <summary>
    /// Shows alternating row backgrounds.
    /// </summary>
    Striped
}

/// <summary>
/// A Material 3 aligned data grid for direct, provider, paginated, and virtualized data.
/// </summary>
/// <remarks>
/// A virtualized grid caps its scroll area at <see cref="VirtualizationInitialItemCount" /> rows by default. To fill a page
/// layout instead, set <see cref="FullHeight" /> and place the grid in a parent with a bounded height, such as a flex column or
/// a grid area; the rows then scroll inside the space the parent gives the grid.
/// </remarks>
[CascadingTypeParameter(nameof(TItem))]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Renders grid markup in static SSR and enhances sorting, paging, and virtualization interactively.",
    CompatibilityDetails = "Static SSR can emit the current table rows and query-link sorting or paging. Interactive sorting, paging, row callbacks, and Virtualize mode require an interactive render mode or browser enhancement.")]
public partial class NTDataGrid<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] TItem> : IDisposable where TItem : class {
    private const string _rowClass = "nt-data-grid-row";
    private const string _clickableRowClass = "nt-data-grid-row nt-data-grid-row-clickable";
    private const string _stripedOddRowClass = "nt-data-grid-row nt-data-grid-row-striped-odd";
    private const string _stripedEvenRowClass = "nt-data-grid-row nt-data-grid-row-striped-even";
    private const string _clickableStripedOddRowClass = "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-odd";
    private const string _clickableStripedEvenRowClass = "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-even";
    private const string _emptyRowClass = "nt-data-grid-row nt-data-grid-row-empty";
    // The page dropdown renders at every width (CSS shows it below the large breakpoint), so very large page counts fall back to text.
    private const int _maxPageJumpOptions = 1_000;
    private const string _pageHrefPlaceholder = "{page}";
    private readonly List<NTDataGridColumn<TItem>> _columns = [];
    private readonly Dictionary<string, NTDataGridColumn<TItem>> _columnsBySortProperty = new(StringComparer.Ordinal);
    private readonly List<TItem> _loadedItems = [];
    private readonly List<NTSortDescriptor> _sorts = [];
    private readonly Dictionary<object, int> _rowIndices = [];
    private readonly CancellationTokenSource _componentCancellation = new();
    private IReadOnlyDictionary<string, object>? _rootAttributes;
    private CancellationTokenSource? _refreshCancellation;
    private UriState? _uriState;
    private IQueryable<TItem>? _previousItems;
    private NTDataGridItemsProvider<TItem>? _previousItemsProvider;
    private IReadOnlyList<int>? _pageSizeOptionsSource;
    private Func<TItem, object>? _previousRowKey;
    private IReadOnlyList<int> _resolvedPageSizeOptions = [];
    private string? _class;
    private string? _currentUri;
    private string? _lastNavigationManagerUri;
    private string? _previousDataState;
    private string? _style;
    private string? _detailStyle;
    private bool _hasDataStateSnapshot;
    private bool _disposed;
    private bool _refreshQueued;
    private bool _initialColumnConfigurationComplete;
    private bool _sortsInitialized;
    private bool _hasVirtualizationItemSizeParameter;
    private bool _hasRowKeyParameter;
    private bool _hasPersistPrerenderedItemsParameter;
    private bool _hasPersistenceKeyParameter;
    private SortPlan? _sortPlan;
    private int _currentPageIndex;
    private int _currentPageSize;
    private int _resolvedPageSizeOptionsPageSize;
    private int _totalItemCount;
    private int _virtualizeVersion;
    private NTVirtualize<TItem>? _virtualizer;
    private bool _updatingBrowserUri;
    private readonly HashSet<object> _expansionExceptions = [];
    private int _expansionVersion;
    private int _previousVirtualizationItemSizeVersion;
    private bool _allRowsExpanded;
    private bool AllRowsExpanded => _allRowsExpanded && _expansionExceptions.Count == 0;
    private bool? _previousDefaultRowsExpanded;
    private PersistingComponentStateSubscription _persistenceSubscription;
    private bool _persistenceRegistration;
    private bool _restoredProviderResult;
    private bool _restoreAttempted;
    private bool _hasLoadedProviderResult;
    private PersistentComponentState? _persistentComponentState;

    /// <summary>Gets or sets detail content for a parent row. Accepts any HTML or Blazor components, including a non-paginated, non-virtualized NTDataGrid.</summary>
    [Parameter]
    public RenderFragment<TItem>? RowDetailTemplate { get; set; }

    /// <summary>Gets or sets whether all rows start expanded, including rows loaded later. Changing this value resets individual expansion choices.</summary>
    [Parameter]
    public bool DefaultRowsExpanded { get; set; }

    /// <summary>Gets or sets whether the expand/collapse-all button is shown when detail content is configured.</summary>
    [Parameter]
    public bool ShowExpandAllButton { get; set; } = true;

    /// <summary>Gets or sets the button text for expanding an individual row.</summary>
    [Parameter]
    public string ExpandRowText { get; set; } = "Expand row";

    /// <summary>Gets or sets the button text for collapsing an individual row.</summary>
    [Parameter]
    public string CollapseRowText { get; set; } = "Collapse row";

    /// <summary>Gets or sets the CSS maximum height of each detail viewport. Overflow scrolls inside the detail row.</summary>
    [Parameter]
    public string RowDetailMaxHeight { get; set; } = "320px";

    /// <summary>Returns whether the row is expanded. Supply a stable RowKey to preserve expansion across provider refreshes.</summary>
    public bool IsRowExpanded(TItem item) => RowDetailTemplate is not null && IsIdentityExpanded(GetRowIdentity(item));

    private bool IsIdentityExpanded(object identity) => _allRowsExpanded != _expansionExceptions.Contains(identity);

    /// <summary>Expands or collapses a row. Can be called from OnRowClicked.</summary>
    public Task SetRowExpandedAsync(TItem item, bool expanded) => InvokeAsync(() => {
        var key = GetRowIdentity(item);
        var changed = expanded == _allRowsExpanded ? _expansionExceptions.Remove(key) : _expansionExceptions.Add(key);
        if (!changed) {
            return;
        }
        _expansionVersion++;
        StateHasChanged();
    });

    /// <summary>Toggles a row's detail content. Can be assigned directly to OnRowClicked.</summary>
    public Task ToggleRowExpandedAsync(TItem item) => SetRowExpandedAsync(item, !IsRowExpanded(item));

    /// <summary>Expands all rows, including rows on other pages and rows not yet loaded by virtualization.</summary>
    public Task ExpandAllRowsAsync() => SetAllRowsExpandedAsync(true);

    /// <summary>Collapses all rows, including rows not currently rendered.</summary>
    public Task CollapseAllRowsAsync() => SetAllRowsExpandedAsync(false);

    private Task SetAllRowsExpandedAsync(bool expanded) => InvokeAsync(() => {
        if (_allRowsExpanded == expanded && _expansionExceptions.Count == 0) {
            return;
        }
        _allRowsExpanded = expanded;
        _expansionExceptions.Clear();
        _expansionVersion++;
        StateHasChanged();
    });

    /// <summary>
    /// Gets or sets the direct item source.
    /// </summary>
    [Parameter]
    public IQueryable<TItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the provider item source.
    /// </summary>
    [Parameter]
    public NTDataGridItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>Gets or sets whether the initial non-virtualized provider page is transferred from prerender to hydration. Requires a non-empty PersistenceKey when enabled.</summary>
    [Parameter]
    public bool PersistPrerenderedItems { get; set; }

    /// <summary>Gets or sets a stable, page-unique key for prerendered provider results. Required when PersistPrerenderedItems is enabled. Include external filters or data versions in this value.</summary>
    [Parameter]
    public string? PersistenceKey { get; set; }

    /// <summary>
    /// Gets or sets the grid columns.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the table caption.
    /// </summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>
    /// Gets or sets the message rendered when no items are available.
    /// </summary>
    [Parameter]
    public string EmptyText { get; set; } = "No data";

    /// <summary>
    /// Gets or sets whether pagination controls are shown.
    /// </summary>
    [Parameter]
    public bool ShowPagination { get; set; }

    /// <summary>
    /// Gets or sets the current zero-based page index.
    /// </summary>
    [Parameter]
    public int PageIndex { get; set; }

    /// <summary>
    /// Invoked when <see cref="PageIndex" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> PageIndexChanged { get; set; }

    /// <summary>
    /// Gets or sets the current page size.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Invoked when <see cref="PageSize" /> changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>
    /// Gets or sets the available page sizes.
    /// </summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50];

    /// <summary>
    /// Invoked when the resolved total item count changes.
    /// </summary>
    [Parameter]
    public EventCallback<int> TotalItemCountChanged { get; set; }

    /// <summary>
    /// Gets or sets whether the body is rendered through Blazor virtualization.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>
    /// Gets or sets whether the grid fills the block size of its parent and scrolls its rows inside that space.
    /// </summary>
    /// <remarks>
    /// The grid becomes a flex column that grows inside a flex parent and stretches inside a grid parent or a parent with a set
    /// height, and a virtualized grid drops its default <see cref="VirtualizationInitialItemCount" />-row height cap. Give the
    /// parent a bounded height. Without one, the grid stops at the viewport height.
    /// </remarks>
    [Parameter]
    public bool FullHeight { get; set; }

    /// <summary>Measures ordinary row heights. Detail rows always enable measurement.</summary>
    [Parameter]
    public bool VirtualizationMeasureItemSize { get; set; }

    /// <summary>Change this revision when externally controlled row content invalidates measured heights.</summary>
    [Parameter]
    public int VirtualizationItemSizeVersion { get; set; }

    /// <summary>Gets or sets the zero-based item initially shown by virtualization.</summary>
    [Parameter]
    public int InitialItemIndex { get; set; }

    /// <summary>Gets or sets how virtualization preserves the visible position during data updates.</summary>
    [Parameter]
    public NTVirtualizeAnchorMode AnchorMode { get; set; }

    /// <summary>Gets or sets item identity comparison when no RowKey is supplied.</summary>
    [Parameter]
    public IEqualityComparer<TItem>? ItemComparer { get; set; }

    /// <summary>Persists page size in the URL and enables page-size links during static SSR. Disabled by default for compatibility.</summary>
    [Parameter]
    public bool PersistPageSizeInQuery { get; set; }

    /// <summary>Creates browser history entries for sorting and paging. The default replaces the current entry.</summary>
    [Parameter]
    public bool PushHistoryEntries { get; set; }

    /// <summary>Scrolls a virtualized grid to a zero-based item. Requires an interactive rendered virtualizer.</summary>
    public Task ScrollToItemAsync(int index, CancellationToken cancellationToken = default) {
        if (!Virtualize || _virtualizer is null) {
            throw new InvalidOperationException("Scrolling requires a rendered virtualized grid.");
        }
        return _virtualizer.ScrollToItemAsync(index, cancellationToken);
    }

    /// <summary>
    /// Gets or sets the approximate virtualized row height in pixels.
    /// </summary>
    [Parameter]
    public float VirtualizationItemSize { get; set; } = 36;

    /// <summary>
    /// Gets or sets the number of extra virtualized rows requested around the visible range.
    /// </summary>
    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets how many additional visible windows of placeholder rows are rendered after loaded virtualized rows.
    /// </summary>
    [Parameter]
    public int VirtualizationPlaceholderPreloadWindowCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets how many additional visible windows of virtualized rows are fetched in the background.
    /// </summary>
    [Parameter]
    public int VirtualizationBackgroundPreloadWindowCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of virtualized rows cached by the grid.
    /// </summary>
    [Parameter]
    public int VirtualizationMaxCachedItemCount { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets whether cached virtualized rows are re-fetched in the background when they become visible again.
    /// </summary>
    [Parameter]
    public bool VirtualizationRevalidateCachedItems { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, scrolling must settle before virtualized rows that are not cached are requested. Set to 0 to request on every window change.
    /// </summary>
    [Parameter]
    public int VirtualizationScrollDebounceMilliseconds { get; set; } = 500;

    /// <summary>
    /// Gets or sets the first server-rendered request size for provider-backed virtual grids.
    /// </summary>
    [Parameter]
    public int VirtualizationInitialItemCount { get; set; } = 20;

    /// <summary>
    /// Gets or sets the prefix that separates this grid's URL page and sort parameters from other grids. Defaults to <c>ntdg</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A prefix of <c>claims</c> produces <c>claims-page</c> (one-based page number) and
    /// <c>claims-sort</c> (for example, <c>TotalPaid:desc</c>). The grid reads these values
    /// from the URL and updates them when the user pages or sorts. An empty or whitespace
    /// prefix uses the unprefixed names <c>page</c> and <c>sort</c>.
    /// </para>
    /// <para>
    /// Use a stable, distinct prefix for every grid on a page, including nested grids that
    /// only sort. For example, use <c>claims</c> for the parent and <c>payments-123</c> for
    /// claim 123's payment table. Sharing a prefix causes grids to share URL parameter names.
    /// </para>
    /// <para>
    /// Virtualized grids also use this prefix for the browser-history scroll restoration key
    /// (for example, <c>claims-scroll</c>); the scroll offset is not a URL query parameter.
    /// Page size is persisted only when PersistPageSizeInQuery is enabled. Row expansion and nested detail scroll positions are not persisted.
    /// </para>
    /// </remarks>
    [Parameter]
    public string QueryParameterPrefix { get; set; } = "ntdg";

    /// <summary>
    /// Gets or sets whether sorting can be applied to more than one column at a time.
    /// </summary>
    [Parameter]
    public bool AllowMultiSort { get; set; } = true;

    /// <summary>
    /// Gets or sets the grid density.
    /// </summary>
    [Parameter]
    public NTDataGridDensity Density { get; set; }

    /// <summary>
    ///     Gets or sets whether mouse users can drag the table sideways when it is wider than the grid.
    /// </summary>
    /// <remarks>
    ///     A press that moves less than a few pixels stays a click. Once it becomes a drag, the release does not click the
    ///     button, link, or row under the pointer. Presses on form fields and the scrollbar keep their usual behavior, and touch
    ///     and pen input pan natively either way. While the table overflows, it shows a grab cursor on hover and fades the edges
    ///     that have more columns past them; the fades need scroll-driven animations, so Firefox skips them. Disable it when
    ///     users need to select cell text with the mouse.
    /// </remarks>
    [Parameter]
    public bool DragToScroll { get; set; } = true;

    /// <summary>
    /// Gets or sets the grid appearance.
    /// </summary>
    [Parameter]
    public NTDataGridAppearance Appearance { get; set; }

    /// <summary>
    /// Gets or sets whether divider lines are shown between body cells.
    /// </summary>
    [Parameter]
    public bool ShowCellDividers { get; set; }

    /// <summary>
    /// Gets or sets a row key selector.
    /// </summary>
    [Parameter]
    public Func<TItem, object>? RowKey { get; set; } = item => (object?)item ?? DBNull.Value;

    /// <summary>
    /// Invoked when a data row is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<TItem> OnRowClicked { get; set; }

    /// <summary>
    /// Gets or sets the pagination navigation label.
    /// </summary>
    [Parameter]
    public string PaginationAriaLabel { get; set; } = "Data grid pagination";

    /// <summary>
    /// Gets or sets unmatched attributes for the grid root.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject]
    private NavigationManager _navManager { get; set; } = default!;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    [Inject]
    private IServiceProvider _services { get; set; } = default!;

    // With detail rows, the provider's item count does not describe physical table rows.
    private long AccessibleRowCount => RowDetailTemplate is null ? (long)_totalItemCount + 1 : -1;

    private bool MeasureVirtualizedRows => VirtualizationMeasureItemSize || RowDetailTemplate is not null;

    private int VisibleColumnCount => Math.Max(_columns.Count + (RowDetailTemplate is not null ? 1 : 0), 1);

    private int CurrentPageIndex => Math.Max(0, _currentPageIndex);

    private int CurrentPageSize => Math.Max(1, _currentPageSize);

    private int PageCount => Math.Max(1, (int)Math.Ceiling(_totalItemCount / (double)CurrentPageSize));

    private bool CanGoToPreviousPage => ShowPagination && CurrentPageIndex > 0;

    private bool CanGoToNextPage => ShowPagination && CurrentPageIndex < PageCount - 1;

    private bool HasRowClickCallback => OnRowClicked.HasDelegate;

    private bool HasStripedRows => Appearance == NTDataGridAppearance.Striped;

    private float DefaultVirtualizationItemSize => Density == NTDataGridDensity.Compact ? 28 : 36;

    private float ResolvedVirtualizationItemSize => Math.Max(1, _hasVirtualizationItemSizeParameter ? VirtualizationItemSize : DefaultVirtualizationItemSize);

    private string RootClass => CssClassBuilder.Create("nt-data-grid")
        .AddClass("nt-data-grid-compact", Density == NTDataGridDensity.Compact)
        .AddClass("nt-data-grid-striped", HasStripedRows)
        .AddClass("nt-data-grid-cell-dividers", ShowCellDividers)
        .AddClass("nt-data-grid-virtualized", Virtualize)
        .AddClass("nt-data-grid-full-height", FullHeight)
        .AddClass(_class, !string.IsNullOrWhiteSpace(_class))
        .Build();

    private string? ScrollStyle => Virtualize && !FullHeight ? $"max-height: {Math.Max(1, VirtualizationInitialItemCount) * ResolvedVirtualizationItemSize}px;" : null;

    private string VirtualizedScrollRestorationKey => GetQueryName("scroll");

    /// <summary>
    /// Refreshes the data grid by re-resolving the current data source and updating the rendered rows.
    /// </summary>
    public Task RefreshDataGridAsync(CancellationToken cancellationToken = default) {
        if (_disposed) {
            return Task.CompletedTask;
        }

        return InvokeAsync(() => RefreshDataGridCoreAsync(cancellationToken));
    }

    private async Task RefreshDataGridCoreAsync(CancellationToken cancellationToken) {
        if (_disposed) {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (Virtualize) {
            if (_virtualizer is not null) {
                await _virtualizer.RefreshDataAsync(cancellationToken);
            }
        }
        else {
            await RefreshDataAsync(cancellationToken);
        }

        if (!_disposed) {
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        _navManager.LocationChanged += OnLocationChanged;
        if (PersistPrerenderedItems && !Virtualize && ItemsProvider is not null && !string.IsNullOrWhiteSpace(PersistenceKey)
            && _services.GetService<PersistentComponentState>() is { } state) {
            _persistentComponentState = state;
            _persistenceSubscription = state.RegisterOnPersisting(PersistProviderResultAsync);
            _persistenceRegistration = true;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "PersistentComponentState serializes caller-owned grid items; trimmed applications must preserve serializable TItem members.")]
    private bool TryRestoreProviderResult(int startIndex, int? count, out PersistedProviderResult<TItem> result) {
        result = default!;
        if (_restoreAttempted || !PersistPrerenderedItems || Virtualize || ItemsProvider is null || string.IsNullOrWhiteSpace(PersistenceKey)) {
            return false;
        }
        _restoreAttempted = true;
        var key = GetPersistenceStateKey(startIndex, count);
        if (_persistentComponentState?.TryTakeFromJson<PersistedProviderResult<TItem>>(key, out var restored) != true || restored is null) {
            return false;
        }
        result = restored;
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "PersistentComponentState serializes caller-owned grid items; trimmed applications must preserve serializable TItem members.")]
    private Task PersistProviderResultAsync() {
        if (_persistentComponentState is { } state && PersistPrerenderedItems && !Virtualize && ItemsProvider is not null && !string.IsNullOrWhiteSpace(PersistenceKey)
            && _hasLoadedProviderResult && !_restoredProviderResult) {
            var startIndex = ShowPagination ? CurrentPageIndex * CurrentPageSize : 0;
            int? count = ShowPagination || ItemsProvider is not null ? CurrentPageSize : null;
            state.PersistAsJson(GetPersistenceStateKey(startIndex, count), new PersistedProviderResult<TItem>(_loadedItems.ToArray(), _totalItemCount));
        }
        return Task.CompletedTask;
    }

    private string GetPersistenceStateKey(int startIndex, int? count) => $"NTDataGrid:{typeof(TItem).FullName}:{PersistenceKey}:{startIndex}:{count}:{SerializeSorts(_sorts)}";

    /// <summary>Represents the short-lived provider result transferred from prerender to hydration.</summary>
    public sealed record PersistedProviderResult<T>(IReadOnlyList<T> Items, int TotalItemCount);

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs args) {
        if (_disposed || _updatingBrowserUri || !PushHistoryEntries) {
            return;
        }
        try {
            await InvokeAsync(async () => {
                SetCurrentUri(args.Location);
                _currentPageIndex = Math.Max(0, PageIndex);
                _currentPageSize = PageSize > 0 ? PageSize : 10;
                ResetSorts();
                ApplyQueryState();
                if (Virtualize) {
                    ResetVirtualizedData();
                }
                else {
                    await RefreshDataAsync();
                }
                if (!_disposed) {
                    StateHasChanged();
                }
            });
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception exception) {
            await DispatchExceptionAsync(exception);
        }
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) {
        _hasVirtualizationItemSizeParameter |= parameters.TryGetValue<float>(nameof(VirtualizationItemSize), out _);
        _hasRowKeyParameter |= parameters.TryGetValue<Func<TItem, object>?>(nameof(RowKey), out _);
        _hasPersistPrerenderedItemsParameter |= parameters.TryGetValue<bool>(nameof(PersistPrerenderedItems), out _);
        _hasPersistenceKeyParameter |= parameters.TryGetValue<string?>(nameof(PersistenceKey), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "NTBA0003:Lifecycle method should use meaningful exception handling or owner ErrorBoundary coverage", Justification = "ItemsProvider failures intentionally propagate to the consumer-owned ErrorBoundary.")]
    protected override async Task OnParametersSetAsync() {
        if (_previousVirtualizationItemSizeVersion != VirtualizationItemSizeVersion) {
            _previousVirtualizationItemSizeVersion = VirtualizationItemSizeVersion;
            _expansionVersion++;
        }
        if (_previousDefaultRowsExpanded != DefaultRowsExpanded) {
            _previousDefaultRowsExpanded = DefaultRowsExpanded;
            _expansionVersion++;
            _allRowsExpanded = DefaultRowsExpanded;
            _expansionExceptions.Clear();
        }
        ValidateConfiguration();
        CaptureRootAttributes();
        _detailStyle = RowDetailTemplate is null ? null : $"max-height: {RowDetailMaxHeight};";
        _currentPageSize = PageSize > 0 ? PageSize : 10;
        _currentPageIndex = Math.Max(0, PageIndex);
        ApplyQueryState();
        var dataStateChanged = CaptureDataStateChanged();

        if (Virtualize) {
            if (dataStateChanged) {
                ResetVirtualizedData();
            }
        }
        else if (_initialColumnConfigurationComplete && _columns.Count > 0 && dataStateChanged && !_refreshQueued) {
            await RefreshDataAsync();
        }
    }

    private RenderFragment ColumnBoundary => builder => {
        builder.OpenComponent<NTDataGridColumnBoundary<TItem>>(0);
        builder.CloseComponent();
    };

    internal async Task CompleteInitialColumnConfigurationAsync() {
        if (_initialColumnConfigurationComplete) {
            return;
        }

        _initialColumnConfigurationComplete = true;
        if (_columns.Count == 0 || _disposed) {
            return;
        }

        if (Virtualize) {
            ResetVirtualizedData();
        }
        else {
            await RefreshDataAsync();
        }

        if (!_disposed) {
            await InvokeAsync(StateHasChanged);
        }
    }

    internal void RegisterColumn(NTDataGridColumn<TItem> column) {
        if (_columns.Contains(column)) {
            return;
        }

        column.Sequence = _columns.Count;
        _columns.Add(column);
        RebuildColumnSortLookup();
        TryAddInitialSort(column);
    }

    internal void NotifyColumnChanged(NTDataGridColumn<TItem> column, bool sortStateChanged) {
        if (!_columns.Contains(column)) {
            return;
        }

        if (sortStateChanged) {
            RebuildColumnSortLookup();
            ResetSorts();
        }

        if (!_initialColumnConfigurationComplete) {
            return;
        }

        QueueRefresh();
    }

    internal void UnregisterColumn(NTDataGridColumn<TItem> column) {
        if (_columns.Remove(column)) {
            ReindexColumns();
            RebuildColumnSortLookup();
            ResetSorts();
            if (_initialColumnConfigurationComplete) {
                QueueRefresh();
            }
        }
    }

    private void QueueRefresh() {
        if (_refreshQueued) {
            return;
        }

        _refreshQueued = true;
        _ = InvokeAsync(async () => {
            _refreshQueued = false;
            if (_disposed) {
                return;
            }

            InitializeSorts();
            if (!Virtualize) {
                await RefreshDataAsync();
            }
            else {
                ResetVirtualizedData();
            }

            if (!_disposed) {
                StateHasChanged();
            }
        });
    }

    private void ReindexColumns() {
        for (var i = 0; i < _columns.Count; i++) {
            _columns[i].Sequence = i;
        }
    }

    private void RebuildColumnSortLookup() {
        _columnsBySortProperty.Clear();
        foreach (var column in _columns) {
            if (!string.IsNullOrWhiteSpace(column.SortPropertyName)) {
                _columnsBySortProperty.TryAdd(column.SortPropertyName, column);
            }
        }

        InvalidateSortPlan();
    }

    private void ValidateConfiguration() {
        if (_hasPersistenceKeyParameter && !_hasPersistPrerenderedItemsParameter) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} {nameof(PersistenceKey)} requires {nameof(PersistPrerenderedItems)}.");
        }

        if (PersistPrerenderedItems && string.IsNullOrWhiteSpace(PersistenceKey)) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} persistence requires a non-empty {nameof(PersistenceKey)}.");
        }

        if (Items is not null && ItemsProvider is not null) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} requires either {nameof(Items)} or {nameof(ItemsProvider)}, not both.");
        }

        if (Items is null && ItemsProvider is null) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} requires {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        if (Virtualize && ShowPagination) {
            throw new InvalidOperationException($"{nameof(NTDataGrid<TItem>)} does not support using {nameof(Virtualize)} and {nameof(ShowPagination)} together.");
        }
    }

    private async Task RefreshDataAsync(CancellationToken cancellationToken = default) {
        var refreshCancellation = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(_componentCancellation.Token, cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(_componentCancellation.Token);
        var previousRefreshCancellation = _refreshCancellation;
        _refreshCancellation = refreshCancellation;

        try {
            previousRefreshCancellation?.Cancel();
            InitializeSorts();
            _previousDataState = GetDataState();
            var startIndex = ShowPagination ? CurrentPageIndex * CurrentPageSize : 0;
            int? count = ShowPagination || ItemsProvider is not null ? CurrentPageSize : null;
            if (TryRestoreProviderResult(startIndex, count, out var restoredResult)) {
                _loadedItems.Clear();
                _loadedItems.AddRange(restoredResult.Items);
                _rowIndices.Clear();
                RegisterRowIndices(startIndex, _loadedItems);
                await SetTotalItemCountAsync(restoredResult.TotalItemCount);
                _restoredProviderResult = true;
                _hasLoadedProviderResult = true;
                return;
            }
            var result = await ResolveItemsAsync(startIndex, count, refreshCancellation.Token);
            refreshCancellation.Token.ThrowIfCancellationRequested();
            _loadedItems.Clear();
            _loadedItems.AddRange(result.Items);
            _rowIndices.Clear();
            RegisterRowIndices(startIndex, _loadedItems);
            await SetTotalItemCountAsync(result.TotalItemCount);
            _restoredProviderResult = false;
            _hasLoadedProviderResult = true;
        }
        catch (OperationCanceledException) when (!_disposed && !ReferenceEquals(_refreshCancellation, refreshCancellation)) {
            // A newer refresh owns the rendered state.
        }
        finally {
            if (ReferenceEquals(_refreshCancellation, refreshCancellation)) {
                _refreshCancellation = null;
            }

            refreshCancellation.Dispose();
        }
    }

    private async ValueTask<NTVirtualizeItemsProviderResult<TItem>> ProvideVirtualizedItemsAsync(NTVirtualizeItemsProviderRequest<TItem> request) {
        InitializeSorts();
        var requestedCount = request.Count > 0 ? request.Count : VirtualizationInitialItemCount;
        var result = await ResolveItemsAsync(request.StartIndex, requestedCount, request.CancellationToken);
        request.CancellationToken.ThrowIfCancellationRequested();
        if (_disposed) {
            return new NTVirtualizeItemsProviderResult<TItem>([], 0);
        }
        RegisterRowIndices(request.StartIndex, result.Items);
        TrimVirtualizedRowIndices(request.StartIndex, result.Items.Count);
        var totalChanged = _totalItemCount != Math.Max(0, result.TotalItemCount);
        await SetTotalItemCountAsync(result.TotalItemCount);
        if (totalChanged && !_disposed) {
            await InvokeAsync(StateHasChanged);
        }
        return new NTVirtualizeItemsProviderResult<TItem>(result.Items, result.TotalItemCount);
    }

    private async ValueTask<NTItemsProviderResult<TItem>> ResolveItemsAsync(int startIndex, int? count, CancellationToken cancellationToken) {
        if (ItemsProvider is not null) {
            return await ItemsProvider(new NTDataGridItemsProviderRequest<TItem> {
                StartIndex = Math.Max(0, startIndex),
                Count = count,
                CancellationToken = cancellationToken,
                Sorts = GetProviderSortDescriptors()
            });
        }

        if (Items is null) {
            return new NTItemsProviderResult<TItem>([], 0);
        }

        // In-memory queryables would recompile an expression tree for every Count and enumeration, so run the same operators over the underlying sequence.
        if (Items.Provider is EnumerableQuery<TItem> && TryApplyEnumerableSorts(Items, out var sorted)) {
            var totalCount = sorted.Count();
            var page = sorted.Skip(Math.Max(0, startIndex));
            if (count is > 0) {
                page = page.Take(count.Value);
            }

            return new NTItemsProviderResult<TItem>(page.ToArray(), totalCount);
        }

        if (TryApplyQueryableSorts(Items, out var query)) {
            var totalCount = query.Count();
            var page = query.Skip(Math.Max(0, startIndex));
            if (count is > 0) {
                page = page.Take(count.Value);
            }

            return new NTItemsProviderResult<TItem>(page.ToArray(), totalCount);
        }

        var localItems = ApplyLocalSorts(Items).ToArray();
        var localStartIndex = Math.Min(Math.Max(0, startIndex), localItems.Length);
        var localCount = count is > 0 ? Math.Min(count.Value, localItems.Length - localStartIndex) : localItems.Length - localStartIndex;
        var localPage = localStartIndex == 0 && localCount == localItems.Length
            ? localItems
            : localItems.AsSpan(localStartIndex, localCount).ToArray();
        return new NTItemsProviderResult<TItem>(localPage, localItems.Length);
    }

    private bool TryApplyEnumerableSorts(IEnumerable<TItem> source, out IEnumerable<TItem> result) {
        result = source;
        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var sort in GetSortPlan().ResolvedSorts) {
            ordered = sort.Column.ApplyEnumerableSort(ordered ?? source, sort.Direction, ordered is not null);
            if (ordered is null) {
                return false;
            }
        }

        result = ordered ?? source;
        return true;
    }

    private bool TryApplyQueryableSorts(IQueryable<TItem> source, out IQueryable<TItem> query) {
        query = source;
        IOrderedQueryable<TItem>? ordered = null;
        foreach (var sort in GetSortPlan().ResolvedSorts) {
            ordered = sort.Column.ApplyQueryableSort(ordered ?? source, sort.Direction, ordered is not null);
            if (ordered is null) {
                return false;
            }
        }

        query = ordered ?? source;
        return true;
    }

    private IEnumerable<TItem> ApplyLocalSorts(IEnumerable<TItem> source) {
        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var sort in GetSortPlan().ResolvedSorts) {
            var customSort = sort.Column.ApplyLocalSort(ordered ?? source, sort.Direction, ordered is not null);
            ordered = customSort ?? (ordered is null
                ? sort.Direction == SortDirection.Descending
                    ? source.OrderByDescending(sort.Column.GetSortValue, NTDataGridObjectComparer.Instance)
                    : source.OrderBy(sort.Column.GetSortValue, NTDataGridObjectComparer.Instance)
                : sort.Direction == SortDirection.Descending
                    ? ordered.ThenByDescending(sort.Column.GetSortValue, NTDataGridObjectComparer.Instance)
                    : ordered.ThenBy(sort.Column.GetSortValue, NTDataGridObjectComparer.Instance));
        }

        return ordered ?? source;
    }

    private IReadOnlyList<NTSortDescriptor> GetProviderSortDescriptors() => GetSortPlan().ProviderDescriptors;

    private SortPlan GetSortPlan() {
        if (_sortPlan is not null) {
            return _sortPlan;
        }

        var resolvedSorts = new List<ResolvedSort>(_sorts.Count);
        var providerDescriptors = new List<NTSortDescriptor>(_sorts.Count);
        foreach (var sort in _sorts) {
            if (_columnsBySortProperty.TryGetValue(sort.PropertyName, out var column)) {
                resolvedSorts.Add(new ResolvedSort(column, sort.Direction));
                providerDescriptors.AddRange(column.GetSortDescriptors(sort.Direction));
            }
            else {
                providerDescriptors.Add(sort);
            }
        }

        _sortPlan = new SortPlan(resolvedSorts.ToArray(), Array.AsReadOnly(providerDescriptors.ToArray()));
        return _sortPlan;
    }

    private void InvalidateSortPlan() => _sortPlan = null;

    private async Task SetTotalItemCountAsync(int totalItemCount) {
        totalItemCount = Math.Max(0, totalItemCount);
        if (_totalItemCount == totalItemCount) {
            return;
        }

        _totalItemCount = totalItemCount;
        if (TotalItemCountChanged.HasDelegate) {
            await TotalItemCountChanged.InvokeAsync(totalItemCount);
        }
    }

    private void InitializeSorts() {
        if (_sortsInitialized || _columns.Count == 0) {
            return;
        }

        _sortsInitialized = true;
        foreach (var column in _columns) {
            TryAddInitialSort(column);
        }
    }

    private void ResetSorts() {
        _sortsInitialized = false;
        SetSorts([]);
        ApplyQueryState();
        InitializeSorts();
    }

    private void TryAddInitialSort(NTDataGridColumn<TItem> column) {
        if (!_sortsInitialized || column.InitialSortDirection is null || !CanSort(column)) {
            return;
        }

        if (!AllowMultiSort && _sorts.Count > 0) {
            return;
        }

        if (_sorts.Any(sort => string.Equals(sort.PropertyName, column.SortPropertyName, StringComparison.Ordinal))) {
            return;
        }

        _sorts.Add(new NTSortDescriptor(column.SortPropertyName!, column.InitialSortDirection.Value));
        InvalidateSortPlan();
    }

    private bool CanSort(NTDataGridColumn<TItem> column) => column.CanSort;

    private async Task SortByColumnAsync(NTDataGridColumn<TItem> column) {
        if (!CanSort(column)) {
            return;
        }

        var nextSorts = GetNextSorts(column);
        SetSorts(nextSorts);

        _currentPageIndex = 0;
        await PageIndexChanged.InvokeAsync(_currentPageIndex);
        if (Virtualize) {
            _virtualizeVersion++;
            StateHasChanged();
        }
        else {
            await RefreshDataAsync();
        }

        await UpdateBrowserUriAsync();
    }

    private async Task GoToPageAsync(int pageIndex) {
        if (!ShowPagination) {
            return;
        }

        var nextPageIndex = Math.Clamp(pageIndex, 0, PageCount - 1);
        if (nextPageIndex == CurrentPageIndex) {
            return;
        }

        _currentPageIndex = nextPageIndex;
        await PageIndexChanged.InvokeAsync(nextPageIndex);
        await RefreshDataAsync();
        await UpdateBrowserUriAsync();
    }

    // Static SSR has no .NET event handling, so the page dropdown navigates through an inline handler there instead.
    private object PageJumpOnChange => RendererInfo.IsInteractive
        ? EventCallback.Factory.Create<ChangeEventArgs>(this, JumpToPageAsync)
        : "window.NTComponents?.jumpToGridPage(this)";

    private async Task JumpToPageAsync(ChangeEventArgs args) {
        if (int.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var page)) {
            await GoToPageAsync(page - 1);
        }
    }

    private async Task ChangePageSizeAsync(ChangeEventArgs args) {
        if (!int.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var pageSize) || pageSize <= 0) {
            return;
        }

        _currentPageSize = pageSize;
        _currentPageIndex = 0;
        await PageSizeChanged.InvokeAsync(pageSize);
        await PageIndexChanged.InvokeAsync(0);
        await RefreshDataAsync();
        await UpdateBrowserUriAsync();
    }

    private async ValueTask UpdateBrowserUriAsync() {
        if (_disposed || !RendererInfo.IsInteractive) {
            return;
        }

        var uri = BuildPageHref(CurrentPageIndex);
        if (PushHistoryEntries) {
            _updatingBrowserUri = true;
            try {
                _navManager.NavigateTo(uri);
            }
            finally {
                _updatingBrowserUri = false;
            }
        }
        else {
            await _jsRuntime.UpdateUriAsync(uri);
        }
        SetCurrentUri(uri);
    }

    private string BuildSortHref(NTDataGridColumn<TItem> column) {
        var nextSorts = GetNextSorts(column);
        return BuildHref(0, nextSorts);
    }

    private string BuildPageHref(int pageIndex) => BuildHref(Math.Clamp(pageIndex, 0, PageCount - 1), _sorts);

    private string BuildHref(int pageIndex, IReadOnlyList<NTSortDescriptor> sorts, int? pageSize = null) =>
        BuildHref((Math.Max(0, pageIndex) + 1).ToString(CultureInfo.InvariantCulture), sorts, pageSize);

    private string BuildHref(string pageValue, IReadOnlyList<NTSortDescriptor> sorts, int? pageSize = null) {
        var sortValue = SerializeSorts(sorts);
        var uriState = GetUriState();
        var href = uriState.PageHrefPrefix + pageValue;
        if (!string.IsNullOrWhiteSpace(sortValue)) {
            href += $"&{Uri.EscapeDataString(uriState.SortName)}={Uri.EscapeDataString(sortValue)}";
        }

        if (PersistPageSizeInQuery) {
            href += $"&{Uri.EscapeDataString(uriState.PageSizeName)}={(pageSize ?? CurrentPageSize).ToString(CultureInfo.InvariantCulture)}";
        }
        return href + uriState.Fragment;
    }

    private UriState GetUriState() {
        var currentUri = GetCurrentUri();
        if (_uriState is not null
            && string.Equals(_uriState.SourceUri, currentUri, StringComparison.Ordinal)
            && string.Equals(_uriState.GridQueryPrefix, QueryParameterPrefix, StringComparison.Ordinal)) {
            return _uriState;
        }

        var uri = _navManager.ToAbsoluteUri(currentUri);
        var query = ParseQuery(uri.Query);
        var pageParameter = GetQueryName("page");
        var pageSizeParameter = GetQueryName("pageSize");
        var sortParameter = GetQueryName("sort");
        var baseQuery = string.Join("&", query
            .Where(pair => !pair.Key.Equals(pageParameter, StringComparison.OrdinalIgnoreCase)
                && !pair.Key.Equals(pageSizeParameter, StringComparison.OrdinalIgnoreCase)
                && !pair.Key.Equals(sortParameter, StringComparison.OrdinalIgnoreCase))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var pageHrefPrefix = uri.GetLeftPart(UriPartial.Path)
            + "?"
            + (string.IsNullOrEmpty(baseQuery) ? string.Empty : baseQuery + "&")
            + Uri.EscapeDataString(pageParameter)
            + "=";

        _uriState = new UriState(currentUri, QueryParameterPrefix, pageHrefPrefix, pageParameter, pageSizeParameter, sortParameter, uri.Fragment, query);
        return _uriState;
    }

    private string GetCurrentUri() {
        var navigationManagerUri = _navManager.Uri;
        if (!string.Equals(_lastNavigationManagerUri, navigationManagerUri, StringComparison.Ordinal)) {
            _lastNavigationManagerUri = navigationManagerUri;
            SetCurrentUri(navigationManagerUri);
        }

        return _currentUri ?? navigationManagerUri;
    }

    private void SetCurrentUri(string uri) {
        if (string.Equals(_currentUri, uri, StringComparison.Ordinal)) {
            return;
        }

        _currentUri = uri;
        _uriState = null;
    }

    private IReadOnlyList<NTSortDescriptor> GetNextSorts(NTDataGridColumn<TItem> column) {
        if (!CanSort(column)) {
            return _sorts;
        }

        return ApplySortTransition(_sorts, column.SortPropertyName!, column.DefaultSortDirection, AllowMultiSort);
    }

    private static IReadOnlyList<NTSortDescriptor> ApplySortTransition(IEnumerable<NTSortDescriptor> currentSorts, string propertyName, SortDirection defaultDirection, bool allowMultiSort) {
        var nextSorts = currentSorts.ToList();
        var currentIndex = nextSorts.FindIndex(sort => string.Equals(sort.PropertyName, propertyName, StringComparison.Ordinal));
        if (!allowMultiSort) {
            SortDirection? currentDirection = currentIndex < 0 ? null : nextSorts[currentIndex].Direction;
            var nextDirection = GetNextSortDirection(currentDirection, defaultDirection);
            return nextDirection is null ? [] : [new NTSortDescriptor(propertyName, nextDirection.Value)];
        }

        if (currentIndex < 0) {
            nextSorts.Add(new NTSortDescriptor(propertyName, defaultDirection));
        }
        else {
            var nextDirection = GetNextSortDirection(nextSorts[currentIndex].Direction, defaultDirection);
            if (nextDirection is null) {
                nextSorts.RemoveAt(currentIndex);
            }
            else {
                nextSorts[currentIndex] = nextSorts[currentIndex] with { Direction = nextDirection.Value };
            }
        }

        return nextSorts;
    }

    private static SortDirection? GetNextSortDirection(SortDirection? currentDirection, SortDirection defaultDirection) {
        if (currentDirection is null) {
            return defaultDirection;
        }

        return currentDirection == defaultDirection
            ? defaultDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending
            : null;
    }

    private void ApplyQueryState() {
        var uriState = GetUriState();
        var query = uriState.Query;
        if (query.TryGetValue(uriState.PageName, out var pageValue) && int.TryParse(pageValue, out var pageNumber)) {
            _currentPageIndex = Math.Max(1, pageNumber) - 1;
        }

        if (PersistPageSizeInQuery && query.TryGetValue(uriState.PageSizeName, out var pageSizeValue) && int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0) {
            _currentPageSize = pageSize;
        }

        if (query.TryGetValue(uriState.SortName, out var sortValue)) {
            SetSorts(NormalizeSorts(ParseSorts(sortValue)));
            _sortsInitialized = true;
        }
    }

    private void SetSorts(IReadOnlyList<NTSortDescriptor> sorts) {
        if (_sorts.Count == sorts.Count) {
            var unchanged = true;
            for (var i = 0; i < sorts.Count; i++) {
                if (_sorts[i] != sorts[i]) {
                    unchanged = false;
                    break;
                }
            }

            if (unchanged) {
                return;
            }
        }

        _sorts.Clear();
        _sorts.AddRange(sorts);
        InvalidateSortPlan();
    }

    private string GetQueryName(string name) => string.IsNullOrWhiteSpace(QueryParameterPrefix) ? name : $"{QueryParameterPrefix}-{name}";

    private static Dictionary<string, string> ParseQuery(string query) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed)) {
            return result;
        }

        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var split = part.Split('=', 2);
            var key = Uri.UnescapeDataString(split[0].Replace("+", " ", StringComparison.Ordinal));
            var value = split.Length > 1 ? Uri.UnescapeDataString(split[1].Replace("+", " ", StringComparison.Ordinal)) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static IReadOnlyList<NTSortDescriptor> ParseSorts(string value) {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(split => split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
            .Select(split => new NTSortDescriptor(split[0], split.Length > 1 && split[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending))
            .ToArray();
    }

    private IReadOnlyList<NTSortDescriptor> NormalizeSorts(IEnumerable<NTSortDescriptor> sorts) => AllowMultiSort ? sorts.ToArray() : sorts.Take(1).ToArray();

    private static string SerializeSorts(IReadOnlyList<NTSortDescriptor> sorts) => sorts.Count == 0 ? string.Empty :
        string.Join(",", sorts.Select(sort => $"{sort.PropertyName}:{(sort.Direction == SortDirection.Descending ? "desc" : "asc")}"));

    private static string GetSortIndicatorClass(SortDirection sortDirection) => sortDirection switch {
        SortDirection.Ascending => "nt-data-grid-sort-indicator nt-data-grid-sort-indicator-ascending",
        SortDirection.Descending => "nt-data-grid-sort-indicator nt-data-grid-sort-indicator-descending",
        _ => "nt-data-grid-sort-indicator"
    };

    private static string GetSortIndicator() => "arrow_drop_down";

    private IEnumerable<int> GetVisiblePageIndices() {
        const int maxPagesToShow = 5;
        var totalPages = PageCount;
        var pagesToShow = Math.Min(maxPagesToShow, totalPages);
        var startIndex = Math.Max(0, CurrentPageIndex - pagesToShow / 2);
        var endIndex = Math.Min(totalPages - 1, startIndex + pagesToShow - 1);
        if (endIndex - startIndex + 1 < pagesToShow) {
            startIndex = Math.Max(0, endIndex - pagesToShow + 1);
        }

        for (var pageIndex = startIndex; pageIndex <= endIndex; pageIndex++) {
            yield return pageIndex;
        }
    }

    private string GetPaginationButtonClass(bool disabled, bool currentPage, string? roleClass = null) => CssClassBuilder.Create("pagination-btn")
        .AddClass(roleClass, !string.IsNullOrWhiteSpace(roleClass))
        .AddClass("current-page", currentPage)
        .AddClass("tnt-disabled", disabled)
        .Build();

    private IReadOnlyList<int> GetResolvedPageSizeOptions() {
        if (ReferenceEquals(_pageSizeOptionsSource, PageSizeOptions) && _resolvedPageSizeOptionsPageSize == CurrentPageSize) {
            return _resolvedPageSizeOptions;
        }

        _pageSizeOptionsSource = PageSizeOptions;
        _resolvedPageSizeOptionsPageSize = CurrentPageSize;
        _resolvedPageSizeOptions = PageSizeOptions
            .Where(option => option > 0)
            .Append(CurrentPageSize)
            .Distinct()
            .OrderBy(option => option)
            .ToArray();
        return _resolvedPageSizeOptions;
    }

    private string GetRowClass(bool hasIndex, int rowIndex) {
        if (!HasStripedRows || !hasIndex) {
            return HasRowClickCallback ? _clickableRowClass : _rowClass;
        }

        return (HasRowClickCallback, rowIndex % 2 == 0) switch {
            (true, true) => _clickableStripedOddRowClass,
            (true, false) => _clickableStripedEvenRowClass,
            (false, true) => _stripedOddRowClass,
            _ => _stripedEvenRowClass
        };
    }

    private object GetRowIdentity(TItem item) => RowKey?.Invoke(item) ?? (object?)item ?? DBNull.Value;

    private void RegisterRowIndices(int startIndex, IReadOnlyCollection<TItem> items) {
        var index = Math.Max(0, startIndex);
        foreach (var item in items) {
            _rowIndices[GetRowIdentity(item)] = index++;
        }
    }

    private void TrimVirtualizedRowIndices(int activeStartIndex, int activeItemCount) {
        var maxRowIndexCount = Math.Max(Math.Max(1, activeItemCount), VirtualizationMaxCachedItemCount);
        if (_rowIndices.Count <= maxRowIndexCount) {
            return;
        }

        var activeEndIndex = Math.Max(0, activeStartIndex) + Math.Max(0, activeItemCount);
        var activeCenterIndex = activeStartIndex + Math.Max(0, activeItemCount) / 2;
        var keysToRemove = _rowIndices
            .Where(pair => pair.Value < activeStartIndex || pair.Value >= activeEndIndex)
            .OrderByDescending(pair => Math.Abs(pair.Value - activeCenterIndex))
            .Select(pair => pair.Key)
            .Take(_rowIndices.Count - maxRowIndexCount)
            .ToArray();

        foreach (var key in keysToRemove) {
            _rowIndices.Remove(key);
        }
    }

    private void ResetVirtualizedData() {
        _rowIndices.Clear();
        _loadedItems.Clear();
        _totalItemCount = 0;
        _virtualizeVersion++;
    }

    private void CaptureRootAttributes() {
        _class = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out var classValue) ? Convert.ToString(classValue, CultureInfo.InvariantCulture) : null;
        _style = AdditionalAttributes != null && AdditionalAttributes.TryGetValue("style", out var styleValue) ? Convert.ToString(styleValue, CultureInfo.InvariantCulture) : null;
        _rootAttributes = AdditionalAttributes?
            .Where(attribute => !attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase) && !attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.OrdinalIgnoreCase);
    }

    private bool CaptureDataStateChanged() {
        var state = GetDataState();
        var changed = !_hasDataStateSnapshot
            || !ReferenceEquals(Items, _previousItems)
            || ItemsProvider != _previousItemsProvider
            || RowKey != _previousRowKey
            || !string.Equals(_previousDataState, state, StringComparison.Ordinal);

        _hasDataStateSnapshot = true;
        _previousItems = Items;
        _previousItemsProvider = ItemsProvider;
        _previousRowKey = RowKey;
        _previousDataState = state;
        return changed;
    }

    private string GetDataState() => $"{CurrentPageIndex}|{CurrentPageSize}|{ShowPagination}|{Virtualize}|{AllowMultiSort}|{GetCurrentUri()}|{SerializeSorts(_sorts)}";

    private Task InvokeRowClickedAsync(TItem item) => HasRowClickCallback ? OnRowClicked.InvokeAsync(item) : Task.CompletedTask;

    private Task InvokeRowClickedFromKeyboardAsync(TItem item, KeyboardEventArgs args) =>
        args.Key is "Enter" or " " or "Spacebar" ? InvokeRowClickedAsync(item) : Task.CompletedTask;

    private RenderFragment RenderRowFragment(TItem item) => builder => RenderRow(builder, item);

    private void RenderRow(RenderTreeBuilder builder, TItem item) {
        var key = RowKey?.Invoke(item) ?? item;
        var identity = key ?? DBNull.Value;
        var hasIndex = _rowIndices.TryGetValue(identity, out var index);
        var expanded = RowDetailTemplate is not null && IsIdentityExpanded(identity);
        builder.OpenRegion(0);
        builder.OpenElement(0, "tr");
        builder.SetKey(key);
        builder.AddAttribute(1, "class", GetRowClass(hasIndex, index));
        if (HasRowClickCallback) {
            builder.AddAttribute(2, "tabindex", "0");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => InvokeRowClickedAsync(item)));
            builder.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, args => InvokeRowClickedFromKeyboardAsync(item, args)));
        }

        builder.AddAttribute(5, "data-nt-virtualize-index", Virtualize && MeasureVirtualizedRows && hasIndex ? index : (int?)null);
        builder.AddAttribute(6, "aria-rowindex", RowDetailTemplate is null && hasIndex ? (long)index + 2 : (long?)null);
        // The expansion cell and detail row stay Razor templates (NTDataGrid.razor) so they carry the CSS-isolation scope attribute.
        if (RowDetailTemplate is not null) {
            builder.AddContent(7, RenderExpansionCell(item, expanded));
        }
        for (var i = 0; i < _columns.Count; i++) {
            var column = _columns[i];
            builder.OpenElement(20, "td");
            builder.AddAttribute(21, "class", column.BodyCellClass);
            builder.OpenElement(22, "div");
            builder.AddAttribute(23, "class", "nt-data-grid-cell-content");
            builder.OpenRegion(24);
            column.RenderCell(builder, item);
            builder.CloseRegion();
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
        if (expanded) {
            builder.AddContent(29, RenderRowDetail(item, identity, Virtualize && hasIndex ? index : null));
        }
        builder.CloseRegion();
    }
    private RenderFragment RenderEmptyRow() => builder => {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", _emptyRowClass);
        builder.OpenElement(2, "td");
        builder.AddAttribute(3, "colspan", VisibleColumnCount);
        builder.AddContent(4, EmptyText);
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment RenderLoadingRow(PlaceholderContext context) => builder => {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", "nt-data-grid-row nt-data-grid-row-placeholder");
        var resolvedRowSize = Math.Max(1, context.Size);
        var rowSize = resolvedRowSize.ToString(CultureInfo.InvariantCulture);
        builder.AddAttribute(2, "data-nt-virtualize-size", rowSize);
        builder.AddAttribute(3, "data-nt-virtualize-placeholder", "true");
        builder.AddAttribute(4, "aria-rowindex", RowDetailTemplate is null ? (long)context.Index : (long?)null);
        builder.AddAttribute(5, "aria-hidden", "true");
        builder.OpenElement(6, "td");
        builder.AddAttribute(7, "class", "nt-data-grid-cell nt-data-grid-cell-placeholder");
        builder.AddAttribute(8, "colspan", VisibleColumnCount);
        builder.AddAttribute(9, "data-nt-virtualize-size", rowSize);
        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "class", "nt-data-grid-row-skeleton");
        builder.OpenComponent<NTSkeleton>(13);
        builder.AddAttribute(14, nameof(NTSkeleton.Shape), NTSkeletonShape.Rectangle);
        builder.CloseComponent();
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment RenderSpacerCell(PlaceholderContext context) => builder => {
        var resolvedSize = Math.Max(0, context.Size);
        var size = resolvedSize.ToString(CultureInfo.InvariantCulture);
        builder.OpenElement(0, "td");
        builder.AddAttribute(1, "class", "nt-data-grid-spacer-cell");
        builder.AddAttribute(2, "colspan", VisibleColumnCount);
        builder.AddAttribute(3, "data-nt-virtualize-size", size);
        builder.CloseElement();
    };

    private readonly record struct ResolvedSort(NTDataGridColumn<TItem> Column, SortDirection Direction);

    private sealed record SortPlan(ResolvedSort[] ResolvedSorts, IReadOnlyList<NTSortDescriptor> ProviderDescriptors);

    private sealed record UriState(string SourceUri, string? GridQueryPrefix, string PageHrefPrefix, string PageName, string PageSizeName, string SortName, string Fragment, IReadOnlyDictionary<string, string> Query);

    private sealed class NTDataGridObjectComparer : IComparer<object?> {
        public static readonly NTDataGridObjectComparer Instance = new();

        public int Compare(object? x, object? y) {
            if (ReferenceEquals(x, y)) {
                return 0;
            }

            if (x is null) {
                return -1;
            }

            if (y is null) {
                return 1;
            }

            if (x is IComparable comparable) {
                return comparable.CompareTo(y);
            }

            return StringComparer.CurrentCulture.Compare(Convert.ToString(x, CultureInfo.CurrentCulture), Convert.ToString(y, CultureInfo.CurrentCulture));
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _navManager.LocationChanged -= OnLocationChanged;
        if (_persistenceRegistration) {
            _persistenceSubscription.Dispose();
        }
        _refreshCancellation = null;
        try {
            _componentCancellation.Cancel();
        }
        finally {
            _componentCancellation.Dispose();
        }
    }
}
