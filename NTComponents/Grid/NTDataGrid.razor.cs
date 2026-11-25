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

/// <summary>Density options for <see cref="NTDataGrid{TItem}" />.</summary>
public enum NTDataGridDensity {
    /// <summary>Default row density.</summary>
    Standard,

    /// <summary>Compact row density.</summary>
    Compact
}

/// <summary>Appearance options for <see cref="NTDataGrid{TItem}" />.</summary>
public enum NTDataGridAppearance {
    /// <summary>Default grid appearance.</summary>
    Default,

    /// <summary>Shows alternating row backgrounds.</summary>
    Striped
}

/// <summary>A Material 3 aligned data grid for direct, provider, paginated, and virtualized data.</summary>
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
    // Indexed by striped state (none, odd, even) * 2 + clickable, so rendering a row allocates no class string.
    private static readonly string[] _rowClasses = [
        "nt-data-grid-row",
        "nt-data-grid-row nt-data-grid-row-clickable",
        "nt-data-grid-row nt-data-grid-row-striped-odd",
        "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-odd",
        "nt-data-grid-row nt-data-grid-row-striped-even",
        "nt-data-grid-row nt-data-grid-row-clickable nt-data-grid-row-striped-even"
    ];
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
    private (IQueryable<TItem>? Items, NTDataGridItemsProvider<TItem>? Provider, Func<TItem, object>? RowKey, bool Paginated, bool Virtualized, bool MultiSort)? _previousParameters;
    private IReadOnlyList<int>? _pageSizeOptionsSource;
    private IReadOnlyList<int> _resolvedPageSizeOptions = [];
    private (int Current, int Starting) _resolvedPageSizeOptionsKey;
    private string? _class;
    private string? _currentUri;
    private string? _lastNavigationManagerUri;
    private string? _style;
    private string? _detailStyle;
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
    private int? _appliedPageSize;
    private int _totalItemCount;
    private int _virtualizeVersion;
    private NTVirtualize<TItem>? _virtualizer;
    private readonly HashSet<object> _expansionExceptions = [];
    private int _expansionVersion;
    private int _previousVirtualizationItemSizeVersion;
    private bool _allRowsExpanded;
    private bool AllRowsExpanded => _allRowsExpanded && _expansionExceptions.Count == 0;
    private bool? _previousDefaultRowsExpanded;
    private PersistingComponentStateSubscription _persistenceSubscription;
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
        if (expanded == _allRowsExpanded ? _expansionExceptions.Remove(key) : _expansionExceptions.Add(key)) {
            StateHasChanged();
        }
    });

    /// <summary>Toggles a row's detail content. Can be assigned directly to OnRowClicked.</summary>
    public Task ToggleRowExpandedAsync(TItem item) => SetRowExpandedAsync(item, !IsRowExpanded(item));

    /// <summary>Expands all rows, including rows on other pages and rows not yet loaded by virtualization.</summary>
    public Task ExpandAllRowsAsync() => SetAllRowsExpandedAsync(true);

    /// <summary>Collapses all rows, including rows not currently rendered.</summary>
    public Task CollapseAllRowsAsync() => SetAllRowsExpandedAsync(false);

    private Task SetAllRowsExpandedAsync(bool expanded) => InvokeAsync(() => {
        if (_allRowsExpanded != expanded || _expansionExceptions.Count > 0) {
            _allRowsExpanded = expanded;
            _expansionExceptions.Clear();
            _expansionVersion++;
            StateHasChanged();
        }
    });

    /// <summary>Gets or sets the direct item source.</summary>
    [Parameter]
    public IQueryable<TItem>? Items { get; set; }

    /// <summary>Gets or sets the provider item source.</summary>
    [Parameter]
    public NTDataGridItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>Gets or sets whether the initial non-virtualized provider page is transferred from prerender to hydration. Requires a non-empty PersistenceKey when enabled.</summary>
    [Parameter]
    public bool PersistPrerenderedItems { get; set; }

    /// <summary>Gets or sets a stable, page-unique key for prerendered provider results. Required when PersistPrerenderedItems is enabled. Include external filters or data versions in this value.</summary>
    [Parameter]
    public string? PersistenceKey { get; set; }

    /// <summary>Gets or sets the grid columns.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the table caption.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>Gets or sets the message rendered when no items are available.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "No data";

    /// <summary>Gets or sets whether pagination controls are shown.</summary>
    [Parameter]
    public bool ShowPagination { get; set; }

    /// <summary>Gets or sets the current zero-based page index.</summary>
    [Parameter]
    public int PageIndex { get; set; }

    /// <summary>Invoked when <see cref="PageIndex" /> changes.</summary>
    [Parameter]
    public EventCallback<int> PageIndexChanged { get; set; }

    /// <summary>Gets or sets the current page size.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Invoked when <see cref="PageSize" /> changes.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>Gets or sets the available page sizes.</summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50];

    /// <summary>Invoked when the resolved total item count changes.</summary>
    [Parameter]
    public EventCallback<int> TotalItemCountChanged { get; set; }

    /// <summary>Gets or sets whether the body is rendered through Blazor virtualization.</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>Gets or sets whether the grid fills the block size of its parent and scrolls its rows inside that space.</summary>
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

    /// <summary>Obsolete. Creates browser history entries for sorting and paging. The default replaces the current entry.</summary>
    /// <remarks>
    /// Back should return to the previous page, not undo a sort or page change, so this option will be removed in the next major
    /// version. Each entry is also a Blazor navigation, which on pages without an interactive router makes the server render the page
    /// again, including a prerendered grid's provider call.
    /// </remarks>
    [Obsolete("Sorting and paging should not add browser history entries; Back returns to the previous page. This parameter will be removed in the next major version.")]
    [Parameter]
    public bool PushHistoryEntries { get; set; }

    // The grid keeps honoring the obsolete parameter until it is removed.
#pragma warning disable CS0618
    private bool PushesHistoryEntries => PushHistoryEntries;
#pragma warning restore CS0618

    /// <summary>The grid's current page, sort, and page-size query parameters, named with <see cref="QueryParameterPrefix" />.</summary>
    /// <remarks>
    /// The grid updates the address bar without a Blazor navigation, so <see cref="NavigationManager.Uri" /> does not include its latest
    /// page and sort. A page that builds a URL for itself, for example to keep a filter in the query string, should pass these to
    /// <c>NavigationManager.GetUriWithQueryParameters</c> together with its own parameters; otherwise its navigation drops the grid's state.
    /// A null value removes that parameter, matching the URL the grid writes. A new dictionary is returned on each read.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> QueryParameters => new Dictionary<string, object?> {
        [GetQueryName("page")] = Math.Clamp(CurrentPageIndex, 0, PageCount - 1) + 1,
        [GetQueryName("sort")] = GetSortQueryValue(_sorts),
        [GetQueryName("pageSize")] = PersistPageSizeInQuery ? CurrentPageSize : null
    };

    /// <summary>Scrolls a virtualized grid to a zero-based item. Requires an interactive rendered virtualizer.</summary>
    public Task ScrollToItemAsync(int index, CancellationToken cancellationToken = default) => Virtualize && _virtualizer is not null
        ? _virtualizer.ScrollToItemAsync(index, cancellationToken)
        : throw new InvalidOperationException("Scrolling requires a rendered virtualized grid.");

    /// <summary>Gets or sets the approximate virtualized row height in pixels.</summary>
    [Parameter]
    public float VirtualizationItemSize { get; set; } = 36;

    /// <summary>Gets or sets the number of extra virtualized rows requested around the visible range.</summary>
    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 3;

    /// <summary>Gets or sets how many additional visible windows of placeholder rows are rendered after loaded virtualized rows.</summary>
    [Parameter]
    public int VirtualizationPlaceholderPreloadWindowCount { get; set; } = 1;

    /// <summary>Gets or sets how many additional visible windows of virtualized rows are fetched in the background.</summary>
    [Parameter]
    public int VirtualizationBackgroundPreloadWindowCount { get; set; } = 1;

    /// <summary>Gets or sets the maximum number of virtualized rows cached by the grid.</summary>
    [Parameter]
    public int VirtualizationMaxCachedItemCount { get; set; } = 1_000;

    /// <summary>Gets or sets whether cached virtualized rows are re-fetched in the background when they become visible again.</summary>
    [Parameter]
    public bool VirtualizationRevalidateCachedItems { get; set; }

    /// <summary>Gets or sets how long, in milliseconds, scrolling must settle before virtualized rows that are not cached are requested. Set to 0 to request on every window change.</summary>
    [Parameter]
    public int VirtualizationScrollDebounceMilliseconds { get; set; } = 500;

    /// <summary>Gets or sets the first server-rendered request size for provider-backed virtual grids.</summary>
    [Parameter]
    public int VirtualizationInitialItemCount { get; set; } = 20;

    /// <summary>Gets or sets the prefix that separates this grid's URL page and sort parameters from other grids. Defaults to <c>ntdg</c>.</summary>
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

    /// <summary>Gets or sets whether sorting can be applied to more than one column at a time.</summary>
    [Parameter]
    public bool AllowMultiSort { get; set; } = true;

    /// <summary>Gets or sets the grid density.</summary>
    [Parameter]
    public NTDataGridDensity Density { get; set; }

    /// <summary>    Gets or sets whether mouse users can drag the table sideways when it is wider than the grid.</summary>
    /// <remarks>
    ///     A press that moves less than a few pixels stays a click. Once it becomes a drag, the release does not click the
    ///     button, link, or row under the pointer. Presses on form fields and the scrollbar keep their usual behavior, and touch
    ///     and pen input pan natively either way. While the table overflows, it shows a grab cursor on hover and fades the edges
    ///     that have more columns past them; the fades need scroll-driven animations, so Firefox skips them. Disable it when
    ///     users need to select cell text with the mouse.
    /// </remarks>
    [Parameter]
    public bool DragToScroll { get; set; } = true;

    /// <summary>Gets or sets the grid appearance.</summary>
    [Parameter]
    public NTDataGridAppearance Appearance { get; set; }

    /// <summary>Gets or sets whether divider lines are shown between body cells.</summary>
    [Parameter]
    public bool ShowCellDividers { get; set; }

    /// <summary>Gets or sets a row key selector.</summary>
    [Parameter]
    public Func<TItem, object>? RowKey { get; set; } = item => (object?)item ?? DBNull.Value;

    /// <summary>Invoked when a data row is clicked.</summary>
    [Parameter]
    public EventCallback<TItem> OnRowClicked { get; set; }

    /// <summary>Gets or sets the pagination navigation label.</summary>
    [Parameter]
    public string PaginationAriaLabel { get; set; } = "Data grid pagination";

    /// <summary>Gets or sets unmatched attributes for the grid root.</summary>
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

    private int StartingPageSize => PageSize > 0 ? PageSize : 10;

    private int PageCount => Math.Max(1, (int)Math.Ceiling(_totalItemCount / (double)CurrentPageSize));

    private bool CanGoToPreviousPage => ShowPagination && CurrentPageIndex > 0;

    private bool CanGoToNextPage => ShowPagination && CurrentPageIndex < PageCount - 1;

    private bool HasRowClickCallback => OnRowClicked.HasDelegate;

    private bool PersistsProviderResult => PersistPrerenderedItems && !Virtualize && ItemsProvider is not null && !string.IsNullOrWhiteSpace(PersistenceKey);

    private float ResolvedVirtualizationItemSize => Math.Max(1, _hasVirtualizationItemSizeParameter ? VirtualizationItemSize : Density == NTDataGridDensity.Compact ? 28 : 36);

    private string RootClass => CssClassBuilder.Create("nt-data-grid")
        .AddClass("nt-data-grid-compact", Density == NTDataGridDensity.Compact)
        .AddClass("nt-data-grid-striped", Appearance == NTDataGridAppearance.Striped)
        .AddClass("nt-data-grid-cell-dividers", ShowCellDividers)
        .AddClass("nt-data-grid-virtualized", Virtualize)
        .AddClass("nt-data-grid-full-height", FullHeight)
        .AddClass(_class, !string.IsNullOrWhiteSpace(_class))
        .Build();

    private string? ScrollStyle => Virtualize && !FullHeight ? $"max-height: {Math.Max(1, VirtualizationInitialItemCount) * ResolvedVirtualizationItemSize}px;" : null;

    /// <summary>Refreshes the data grid by re-resolving the current data source and updating the rendered rows.</summary>
    public Task RefreshDataGridAsync(CancellationToken cancellationToken = default) => _disposed ? Task.CompletedTask : InvokeAsync(async () => {
        if (_disposed) {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!Virtualize) {
            await RefreshDataAsync(cancellationToken);
        }
        else if (_virtualizer is not null) {
            await _virtualizer.RefreshDataAsync(cancellationToken);
        }

        if (!_disposed) {
            StateHasChanged();
        }
    });

    // Virtualized grids reload through the virtualizer, so they only reset their cached state.
    private async Task ReloadAsync() {
        if (Virtualize) {
            ResetVirtualizedData();
        }
        else {
            await RefreshDataAsync();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        _navManager.LocationChanged += OnLocationChanged;
        if (PersistsProviderResult && _services.GetService<PersistentComponentState>() is { } state) {
            _persistentComponentState = state;
            _persistenceSubscription = state.RegisterOnPersisting(PersistProviderResultAsync);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "PersistentComponentState serializes caller-owned grid items; trimmed applications must preserve serializable TItem members.")]
    private bool TryRestoreProviderResult(int startIndex, int? count, out NTItemsProviderResult<TItem> result) {
        result = default;
        if (_restoreAttempted || !PersistsProviderResult) {
            return false;
        }
        _restoreAttempted = true;
        if (_persistentComponentState?.TryTakeFromJson<PersistedProviderResult<TItem>>(GetPersistenceStateKey(startIndex, count), out var restored) != true || restored is null) {
            return false;
        }
        result = new NTItemsProviderResult<TItem>(restored.Items.ToArray(), restored.TotalItemCount);
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "PersistentComponentState serializes caller-owned grid items; trimmed applications must preserve serializable TItem members.")]
    private Task PersistProviderResultAsync() {
        if (_persistentComponentState is { } state && PersistsProviderResult && _hasLoadedProviderResult && !_restoredProviderResult) {
            state.PersistAsJson(GetPersistenceStateKey(ShowPagination ? CurrentPageIndex * CurrentPageSize : 0, CurrentPageSize), new PersistedProviderResult<TItem>(_loadedItems.ToArray(), _totalItemCount));
        }
        return Task.CompletedTask;
    }

    private string GetPersistenceStateKey(int startIndex, int? count) => $"NTDataGrid:{typeof(TItem).FullName}:{PersistenceKey}:{startIndex}:{count}:{SerializeSorts(_sorts)}";

    /// <summary>Represents the short-lived provider result transferred from prerender to hydration.</summary>
    public sealed record PersistedProviderResult<T>(IReadOnlyList<T> Items, int TotalItemCount);

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs args) {
        if (_disposed || !PushesHistoryEntries) {
            return;
        }
        try {
            await InvokeAsync(async () => {
                // This also runs for the grid's own NavigateTo and for other query changes, so reload only when this grid's view changes.
                var shownViewState = GetViewState();
                _currentUri = args.Location;
                _currentPageIndex = Math.Max(0, PageIndex);
                // Without the size in the URL, history doesn't track it, so keep the size the grid is showing.
                if (PersistPageSizeInQuery) {
                    _currentPageSize = StartingPageSize;
                }
                ResetSorts();
                ApplyQueryState();
                if (shownViewState != GetViewState()) {
                    await ReloadAsync();
                }
                // Render even without a reload: pagination links are built from the current URL.
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
        var shownViewState = GetViewState();
        // Apply PageSize only when it changes, so a parent that doesn't bind it can't undo the user's pick by rendering again.
        if (_appliedPageSize != PageSize) {
            _appliedPageSize = PageSize;
            _currentPageSize = StartingPageSize;
        }
        _currentPageIndex = Math.Max(0, PageIndex);
        ApplyQueryState();
        if ((CaptureParametersChanged() || shownViewState != GetViewState()) && (Virtualize || _initialColumnConfigurationComplete && _columns.Count > 0 && !_refreshQueued)) {
            await ReloadAsync();
        }
    }

    private RenderFragment ColumnBoundary => builder => {
        builder.OpenComponent<NTDataGridColumnBoundary<TItem>>(0);
        builder.CloseComponent();
    };

    internal async Task CompleteInitialColumnConfigurationAsync() {
        if (!_initialColumnConfigurationComplete) {
            _initialColumnConfigurationComplete = true;
            if (_columns.Count > 0 && !_disposed) {
                await ReloadAsync();
                if (!_disposed) {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
    }

    internal void RegisterColumn(NTDataGridColumn<TItem> column) {
        if (!_columns.Contains(column)) {
            column.Sequence = _columns.Count;
            _columns.Add(column);
            RebuildColumnSortLookup();
            TryAddInitialSort(column);
        }
    }

    internal void NotifyColumnChanged(NTDataGridColumn<TItem> column, bool sortStateChanged) {
        if (_columns.Contains(column)) {
            if (sortStateChanged) {
                RebuildColumnSortLookup();
                ResetSorts();
            }

            if (_initialColumnConfigurationComplete) {
                QueueRefresh();
            }
        }
    }

    internal void UnregisterColumn(NTDataGridColumn<TItem> column) {
        if (_columns.Remove(column)) {
            for (var i = 0; i < _columns.Count; i++) {
                _columns[i].Sequence = i;
            }

            RebuildColumnSortLookup();
            ResetSorts();
            if (_initialColumnConfigurationComplete) {
                QueueRefresh();
            }
        }
    }

    private void QueueRefresh() {
        if (!_refreshQueued) {
            _refreshQueued = true;
            _ = InvokeAsync(async () => {
                _refreshQueued = false;
                if (!_disposed) {
                    InitializeSorts();
                    await ReloadAsync();
                    if (!_disposed) {
                        StateHasChanged();
                    }
                }
            });
        }
    }

    private void RebuildColumnSortLookup() {
        _columnsBySortProperty.Clear();
        foreach (var column in _columns.Where(column => !string.IsNullOrWhiteSpace(column.SortPropertyName))) {
            _columnsBySortProperty.TryAdd(column.SortPropertyName!, column);
        }

        _sortPlan = null;
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
        var refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(_componentCancellation.Token, cancellationToken);
        var previousRefreshCancellation = _refreshCancellation;
        _refreshCancellation = refreshCancellation;

        try {
            previousRefreshCancellation?.Cancel();
            InitializeSorts();
            var startIndex = ShowPagination ? CurrentPageIndex * CurrentPageSize : 0;
            int? count = ShowPagination || ItemsProvider is not null ? CurrentPageSize : null;
            var restored = TryRestoreProviderResult(startIndex, count, out var result);
            if (!restored) {
                result = await ResolveItemsAsync(startIndex, count, refreshCancellation.Token);
                refreshCancellation.Token.ThrowIfCancellationRequested();
            }
            _loadedItems.Clear();
            _loadedItems.AddRange(result.Items);
            _rowIndices.Clear();
            RegisterRowIndices(startIndex, _loadedItems);
            await SetTotalItemCountAsync(result.TotalItemCount);
            _restoredProviderResult = restored;
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
        var result = await ResolveItemsAsync(request.StartIndex, request.Count > 0 ? request.Count : VirtualizationInitialItemCount, request.CancellationToken);
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
                Sorts = GetSortPlan().ProviderDescriptors
            });
        }

        if (Items is null) {
            return new NTItemsProviderResult<TItem>([], 0);
        }

        // In-memory queryables would recompile an expression tree for every Count and enumeration, so run the same operators over the underlying sequence.
        if (Items.Provider is EnumerableQuery<TItem> && TryApplyEnumerableSorts(Items, out var sorted)) {
            return Page(sorted.ToArray());
        }

        if (TryApplyQueryableSorts(Items, out var query)) {
            var totalCount = query.Count();
            var page = query.Skip(Math.Max(0, startIndex));
            return new NTItemsProviderResult<TItem>((count is > 0 ? page.Take(count.Value) : page).ToArray(), totalCount);
        }

        return Page(ApplyLocalSorts(Items).ToArray());

        NTItemsProviderResult<TItem> Page(TItem[] all) {
            var page = all.Skip(Math.Max(0, startIndex));
            return new NTItemsProviderResult<TItem>((count is > 0 ? page.Take(count.Value) : page).ToArray(), all.Length);
        }
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
            var descending = sort.Direction == SortDirection.Descending;
            var comparer = NTDataGridObjectComparer.Instance;
            ordered = sort.Column.ApplyLocalSort(ordered ?? source, sort.Direction, ordered is not null)
                ?? (ordered is null
                    ? descending ? source.OrderByDescending(sort.Column.GetSortValue, comparer) : source.OrderBy(sort.Column.GetSortValue, comparer)
                    : descending ? ordered.ThenByDescending(sort.Column.GetSortValue, comparer) : ordered.ThenBy(sort.Column.GetSortValue, comparer));
        }

        return ordered ?? source;
    }

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

        return _sortPlan = new SortPlan(resolvedSorts.ToArray(), Array.AsReadOnly(providerDescriptors.ToArray()));
    }

    private async Task SetTotalItemCountAsync(int totalItemCount) {
        totalItemCount = Math.Max(0, totalItemCount);
        if (_totalItemCount != totalItemCount) {
            _totalItemCount = totalItemCount;
            await TotalItemCountChanged.InvokeAsync(totalItemCount);
        }
    }

    private void InitializeSorts() {
        if (!_sortsInitialized && _columns.Count > 0) {
            _sortsInitialized = true;
            foreach (var column in _columns) {
                TryAddInitialSort(column);
            }
        }
    }

    private void ResetSorts() {
        _sortsInitialized = false;
        SetSorts([]);
        ApplyQueryState();
        InitializeSorts();
    }

    private void TryAddInitialSort(NTDataGridColumn<TItem> column) {
        if (_sortsInitialized && column.InitialSortDirection is { } direction && column.CanSort
            && (AllowMultiSort || _sorts.Count == 0)
            && !_sorts.Any(sort => string.Equals(sort.PropertyName, column.SortPropertyName, StringComparison.Ordinal))) {
            _sorts.Add(new NTSortDescriptor(column.SortPropertyName!, direction));
            _sortPlan = null;
        }
    }

    private async Task SortByColumnAsync(NTDataGridColumn<TItem> column) {
        if (column.CanSort) {
            SetSorts(GetNextSorts(column));
            _currentPageIndex = 0;
            await PageIndexChanged.InvokeAsync(0);
            if (Virtualize) {
                _virtualizeVersion++;
                StateHasChanged();
            }
            else {
                await RefreshDataAsync();
            }

            await UpdateBrowserUriAsync();
        }
    }

    private async Task GoToPageAsync(int pageIndex) {
        var nextPageIndex = Math.Clamp(pageIndex, 0, PageCount - 1);
        if (ShowPagination && nextPageIndex != CurrentPageIndex) {
            _currentPageIndex = nextPageIndex;
            await PageIndexChanged.InvokeAsync(nextPageIndex);
            await RefreshDataAsync();
            await UpdateBrowserUriAsync();
        }
    }

    // Interactive pagination clicks are handled in .NET; without the opt-out, enhanced navigation also follows the link.
    private string? PaginationEnhanceNav => RendererInfo.IsInteractive ? "false" : null;

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
        if (int.TryParse(Convert.ToString(args.Value, CultureInfo.InvariantCulture), out var pageSize) && pageSize > 0) {
            _currentPageSize = pageSize;
            _currentPageIndex = 0;
            await PageSizeChanged.InvokeAsync(pageSize);
            await PageIndexChanged.InvokeAsync(0);
            await RefreshDataAsync();
            await UpdateBrowserUriAsync();
        }
    }

    private async ValueTask UpdateBrowserUriAsync() {
        if (!_disposed && RendererInfo.IsInteractive) {
            // Record the URL before awaiting the browser, so a parent render in between reads the page the grid is already showing.
            _currentUri = BuildPageHref(CurrentPageIndex);
            if (PushesHistoryEntries) {
                // Another grid's replaceState may have written keys NavigationManager never saw, so rebuild on the address bar.
                _currentUri = await _jsRuntime.InvokeAsync<string>("NTComponents.getCurrentLocation");
                _currentUri = BuildPageHref(CurrentPageIndex);
                if (!_disposed) {
                    _navManager.NavigateTo(_currentUri);
                }
            }
            else {
                await _jsRuntime.InvokeVoidAsync("NTComponents.updateGridUri", _currentUri, GridQueryNames);
            }
        }
    }

    private string BuildSortHref(NTDataGridColumn<TItem> column) => BuildHref(0, GetNextSorts(column));

    private string BuildPageHref(int pageIndex) => BuildHref(Math.Clamp(pageIndex, 0, PageCount - 1), _sorts);

    private string BuildHref(int pageIndex, IReadOnlyList<NTSortDescriptor> sorts, int? pageSize = null) =>
        BuildHref((Math.Max(0, pageIndex) + 1).ToString(CultureInfo.InvariantCulture), sorts, pageSize);

    private string BuildHref(string pageValue, IReadOnlyList<NTSortDescriptor> sorts, int? pageSize = null) {
        var uriState = GetUriState();
        var href = $"{uriState.HrefPrefix}{Uri.EscapeDataString(GetQueryName("page"))}={pageValue}";
        if (GetSortQueryValue(sorts) is { } sortValue) {
            href += $"&{Uri.EscapeDataString(GetQueryName("sort"))}={Uri.EscapeDataString(sortValue)}";
        }

        if (PersistPageSizeInQuery) {
            href += $"&{Uri.EscapeDataString(GetQueryName("pageSize"))}={(pageSize ?? CurrentPageSize).ToString(CultureInfo.InvariantCulture)}";
        }
        return href + uriState.HrefSuffix;
    }

    // A missing sort means "use the initial sort", so an unsorted grid with an initial sort needs an explicit empty value.
    private string? GetSortQueryValue(IReadOnlyList<NTSortDescriptor> sorts) {
        var value = SerializeSorts(sorts);
        return value.Length > 0 || _columns.Any(column => column.InitialSortDirection is not null && column.CanSort) ? value : null;
    }

    private UriState GetUriState() {
        var currentUri = GetCurrentUri();
        if (_uriState?.SourceUri == currentUri && _uriState.GridQueryPrefix == QueryParameterPrefix) {
            return _uriState;
        }

        var uri = _navManager.ToAbsoluteUri(currentUri);
        var gridQueryNames = GridQueryNames;
        // Keep the other parts as written, in order and with repeated keys, and put the grid's parts where its first part was.
        var parts = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var splitIndex = parts.TakeWhile(part => !IsGridPart(part)).Count();
        return _uriState = new UriState(
            currentUri,
            QueryParameterPrefix,
            $"{uri.GetLeftPart(UriPartial.Path)}?{string.Concat(parts[..splitIndex].Select(part => part + "&"))}",
            string.Concat(parts[splitIndex..].Where(part => !IsGridPart(part)).Select(part => "&" + part)) + uri.Fragment,
            ParseQuery(uri.Query));

        bool IsGridPart(string part) => gridQueryNames.Contains(UnescapeQueryPart(part.Split('=', 2)[0]), StringComparer.OrdinalIgnoreCase);
    }

    private string[] GridQueryNames => [GetQueryName("page"), GetQueryName("pageSize"), GetQueryName("sort")];

    private string GetCurrentUri() {
        if (_lastNavigationManagerUri != _navManager.Uri) {
            _currentUri = _lastNavigationManagerUri = _navManager.Uri;
        }

        return _currentUri!;
    }

    private IReadOnlyList<NTSortDescriptor> GetNextSorts(NTDataGridColumn<TItem> column) => column.CanSort
        ? ApplySortTransition(_sorts, column.SortPropertyName!, column.DefaultSortDirection, AllowMultiSort)
        : _sorts;

    private static IReadOnlyList<NTSortDescriptor> ApplySortTransition(IEnumerable<NTSortDescriptor> currentSorts, string propertyName, SortDirection defaultDirection, bool allowMultiSort) {
        var nextSorts = currentSorts.ToList();
        var currentIndex = nextSorts.FindIndex(sort => string.Equals(sort.PropertyName, propertyName, StringComparison.Ordinal));
        var nextDirection = GetNextSortDirection(currentIndex < 0 ? null : nextSorts[currentIndex].Direction, defaultDirection);
        if (!allowMultiSort) {
            return nextDirection is { } direction ? [new NTSortDescriptor(propertyName, direction)] : [];
        }

        if (currentIndex < 0) {
            nextSorts.Add(new NTSortDescriptor(propertyName, defaultDirection));
        }
        else if (nextDirection is null) {
            nextSorts.RemoveAt(currentIndex);
        }
        else {
            nextSorts[currentIndex] = nextSorts[currentIndex] with { Direction = nextDirection.Value };
        }

        return nextSorts;
    }

    private static SortDirection? GetNextSortDirection(SortDirection? currentDirection, SortDirection defaultDirection) => currentDirection switch {
        null => defaultDirection,
        _ when currentDirection == defaultDirection => defaultDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending,
        _ => null
    };

    private void ApplyQueryState() {
        var query = GetUriState().Query;
        if (query.TryGetValue(GetQueryName("page"), out var pageValue) && int.TryParse(pageValue, out var pageNumber)) {
            _currentPageIndex = Math.Max(1, pageNumber) - 1;
        }

        if (PersistPageSizeInQuery && query.TryGetValue(GetQueryName("pageSize"), out var pageSizeValue) && int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0) {
            _currentPageSize = pageSize;
        }

        if (query.TryGetValue(GetQueryName("sort"), out var sortValue)) {
            SetSorts(NormalizeSorts(ParseSorts(sortValue)));
            _sortsInitialized = true;
        }
    }

    private void SetSorts(IReadOnlyList<NTSortDescriptor> sorts) {
        if (!_sorts.SequenceEqual(sorts)) {
            _sorts.Clear();
            _sorts.AddRange(sorts);
            _sortPlan = null;
        }
    }

    private string GetQueryName(string name) => string.IsNullOrWhiteSpace(QueryParameterPrefix) ? name : $"{QueryParameterPrefix}-{name}";

    private static Dictionary<string, string> ParseQuery(string query) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var split = part.Split('=', 2);
            result[UnescapeQueryPart(split[0])] = split.Length > 1 ? UnescapeQueryPart(split[1]) : string.Empty;
        }

        return result;
    }

    private static string UnescapeQueryPart(string value) => Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));

    private static IReadOnlyList<NTSortDescriptor> ParseSorts(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(split => split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
            .Select(split => new NTSortDescriptor(split[0], split.Length > 1 && split[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending))
            .ToArray();

    private IReadOnlyList<NTSortDescriptor> NormalizeSorts(IEnumerable<NTSortDescriptor> sorts) => AllowMultiSort ? sorts.ToArray() : sorts.Take(1).ToArray();

    private static string SerializeSorts(IReadOnlyList<NTSortDescriptor> sorts) =>
        string.Join(",", sorts.Select(sort => $"{sort.PropertyName}:{(sort.Direction == SortDirection.Descending ? "desc" : "asc")}"));

    private IEnumerable<int> GetVisiblePageIndices() {
        var pagesToShow = Math.Min(5, PageCount);
        return Enumerable.Range(Math.Clamp(CurrentPageIndex - pagesToShow / 2, 0, PageCount - pagesToShow), pagesToShow);
    }

    private string GetPaginationButtonClass(bool disabled, bool currentPage, string? roleClass = null) => CssClassBuilder.Create("pagination-btn")
        .AddClass(roleClass, !string.IsNullOrWhiteSpace(roleClass))
        .AddClass("current-page", currentPage)
        .AddClass("nt-disabled", disabled)
        .Build();

    private IReadOnlyList<int> GetResolvedPageSizeOptions() {
        var key = (CurrentPageSize, StartingPageSize);
        if (!ReferenceEquals(_pageSizeOptionsSource, PageSizeOptions) || _resolvedPageSizeOptionsKey != key) {
            _pageSizeOptionsSource = PageSizeOptions;
            _resolvedPageSizeOptionsKey = key;
            // The starting size stays selectable, so the user can go back to it after picking another size.
            _resolvedPageSizeOptions = PageSizeOptions.Where(option => option > 0).Append(CurrentPageSize).Append(StartingPageSize).Distinct().Order().ToArray();
        }

        return _resolvedPageSizeOptions;
    }

    private string GetRowClass(bool hasIndex, int rowIndex) => _rowClasses[(Appearance == NTDataGridAppearance.Striped && hasIndex ? rowIndex % 2 == 0 ? 2 : 4 : 0) + (HasRowClickCallback ? 1 : 0)];

    private object GetRowIdentity(TItem item) => RowKey?.Invoke(item) ?? (object?)item ?? DBNull.Value;

    private void RegisterRowIndices(int startIndex, IReadOnlyCollection<TItem> items) {
        var index = Math.Max(0, startIndex);
        foreach (var item in items) {
            _rowIndices[GetRowIdentity(item)] = index++;
        }
    }

    private void TrimVirtualizedRowIndices(int activeStartIndex, int activeItemCount) {
        var maxRowIndexCount = Math.Max(Math.Max(1, activeItemCount), VirtualizationMaxCachedItemCount);
        if (_rowIndices.Count > maxRowIndexCount) {
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
    }

    private void ResetVirtualizedData() {
        _rowIndices.Clear();
        _loadedItems.Clear();
        _totalItemCount = 0;
        _virtualizeVersion++;
    }

    private void CaptureRootAttributes() {
        _class = GetAttribute("class");
        _style = GetAttribute("style");
        _rootAttributes = AdditionalAttributes?
            .Where(attribute => !attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase) && !attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.OrdinalIgnoreCase);

        string? GetAttribute(string name) => AdditionalAttributes != null && AdditionalAttributes.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

    // Only parent inputs belong here; the grid changes its page, page size, sorts, and URL itself. The first call (null snapshot) always reports a change.
    private bool CaptureParametersChanged() {
        var previous = _previousParameters;
        _previousParameters = (Items, ItemsProvider, RowKey, ShowPagination, Virtualize, AllowMultiSort);
        return previous is not { } p
            || !ReferenceEquals(Items, p.Items)
            || ItemsProvider != p.Provider
            || RowKey != p.RowKey
            || (ShowPagination, Virtualize, AllowMultiSort) != (p.Paginated, p.Virtualized, p.MultiSort);
    }

    // What the grid is showing. URL changes that leave this alone, including the grid's own navigation echoing back, do not reload.
    private string GetViewState() => $"{CurrentPageIndex}|{CurrentPageSize}|{SerializeSorts(_sorts)}";

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
        foreach (var column in _columns) {
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
        builder.AddAttribute(1, "class", "nt-data-grid-row nt-data-grid-row-empty");
        builder.OpenElement(2, "td");
        builder.AddAttribute(3, "colspan", VisibleColumnCount);
        builder.AddContent(4, EmptyText);
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment RenderLoadingRow(PlaceholderContext context) => builder => {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", "nt-data-grid-row nt-data-grid-row-placeholder");
        var rowSize = Math.Max(1, context.Size).ToString(CultureInfo.InvariantCulture);
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
        builder.OpenElement(0, "td");
        builder.AddAttribute(1, "class", "nt-data-grid-spacer-cell");
        builder.AddAttribute(2, "colspan", VisibleColumnCount);
        builder.AddAttribute(3, "data-nt-virtualize-size", Math.Max(0, context.Size).ToString(CultureInfo.InvariantCulture));
        builder.CloseElement();
    };

    private readonly record struct ResolvedSort(NTDataGridColumn<TItem> Column, SortDirection Direction);

    private sealed record SortPlan(ResolvedSort[] ResolvedSorts, IReadOnlyList<NTSortDescriptor> ProviderDescriptors);

    private sealed record UriState(string SourceUri, string? GridQueryPrefix, string HrefPrefix, string HrefSuffix, IReadOnlyDictionary<string, string> Query);

    private sealed class NTDataGridObjectComparer : IComparer<object?> {
        public static readonly NTDataGridObjectComparer Instance = new();

        public int Compare(object? x, object? y) => ReferenceEquals(x, y) ? 0
            : x is null ? -1
            : y is null ? 1
            : x is IComparable comparable ? comparable.CompareTo(y)
            : StringComparer.CurrentCulture.Compare(Convert.ToString(x, CultureInfo.CurrentCulture), Convert.ToString(y, CultureInfo.CurrentCulture));
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _navManager.LocationChanged -= OnLocationChanged;
        if (_persistentComponentState is not null) {
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
