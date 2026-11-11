using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Virtualization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     A component that provides scrolling virtualization for a list of items.
/// </summary>
/// <typeparam name="TItem">The type of the items to be virtualized.</typeparam>
[method: DynamicDependency(nameof(LoadItems))]
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.InteractiveRequired,
    CompatibilitySummary = "Requires browser measurement and Blazor item-provider callbacks.",
    CompatibilityDetails = "Virtualization depends on JavaScript measurement, JSInvokable refresh callbacks, and ItemsProvider execution after render. Static SSR cannot provide the scrolling data window contract.")]
public partial class NTVirtualize<TItem>() : NTPageScriptComponent<NTVirtualize<TItem>> {
    private readonly Dictionary<int, TItem> _itemCache = [];
    private readonly List<int> _cacheIndexesToRemove = [];
    private readonly List<int> _trimCandidateIndexes = [];

    /// <inheritdoc />
    public override string? ElementClass => throw new NotSupportedException();

    /// <inheritdoc />
    public override string? ElementStyle => throw new NotSupportedException();

    /// <summary>
    ///     Gets or sets the content to show when the <see cref="NTVirtualizeItemsProviderResult{TItem}.TotalItemCount" /> is zero.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    ///     Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 50f;

    /// <summary>Measures variable-height items. Template root elements must carry data-nt-virtualize-index with their zero-based provider index; multiple elements may share an index.</summary>
    [Parameter]
    public bool MeasureItemSize { get; set; }

    /// <summary>Gets or sets a revision that invalidates measured item sizes when content changes, including items outside the current window.</summary>
    [Parameter]
    public int ItemSizeVersion { get; set; }

    /// <summary>Gets or sets the zero-based initial item index. Applied once on the first interactive render.</summary>
    [Parameter]
    public int InitialItemIndex { get; set; }

    /// <summary>Gets or sets the opt-in anchoring behavior on refresh. Defaults to preserving the pixel offset.</summary>
    [Parameter]
    public NTVirtualizeAnchorMode AnchorMode { get; set; }

    /// <summary>Gets or sets a stable item key, used for rendering identity and refresh anchoring.</summary>
    [Parameter]
    public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Gets or sets the equality comparer for refresh anchoring when no item key is supplied.</summary>
    [Parameter]
    public IEqualityComparer<TItem>? ItemComparer { get; set; }

    /// <summary>
    ///     Gets or sets the function providing items to the list.
    /// </summary>
    [Parameter]
    public NTVirtualizeItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    ///     Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <inheritdoc />
    public override string? JsModulePath => "./_content/NTComponents/Virtualization/NTVirtualize.razor.js";

    /// <summary>
    ///     Gets or sets the template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? LoadingTemplate { get; set; }

    /// <summary>
    ///     Gets or sets optional content rendered inside each spacer element.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext>? SpacerTemplate { get; set; }

    /// <summary>
    ///     <para>Gets or sets the maximum number of items that will be rendered, even if the client reports that its viewport is large enough to show more. The default value is 100.</para>
    ///     <para>
    ///         This should only be used as a safeguard against excessive memory usage or large data loads. Do not set this to a smaller number than you expect to fit on a realistic-sized window,
    ///         because that will leave a blank gap below and the user may not be able to see the rest of the content.
    ///     </para>
    /// </summary>
    [Parameter]
    public int MaxItemCount { get; set; } = 100;

    /// <summary>
    ///     Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling.
    ///     However, higher values mean that more elements will be present in the page.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 3;

    /// <summary>
    ///     Gets or sets how many additional visible windows of loading placeholders are rendered after the loaded item range.
    /// </summary>
    /// <remarks>
    ///     The default value of 1 renders up to one extra visible window of placeholders, so the user can scroll into already-rendered skeletons while the next item request is being triggered.
    /// </remarks>
    [Parameter]
    public int PlaceholderPreloadWindowCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets how many additional visible windows are fetched in the background after the visible item range.
    /// </summary>
    [Parameter]
    public int BackgroundPreloadWindowCount { get; set; }

    /// <summary>
    ///     Gets or sets whether cached visible ranges are re-fetched in the background and replaced when the provider returns newer items.
    /// </summary>
    [Parameter]
    public bool RevalidateCachedItems { get; set; }

    /// <summary>
    ///     Gets or sets how long, in milliseconds, scrolling must settle before rows that are not cached are requested. Placeholders render immediately while scrolling, and each window
    ///     change restarts the wait. Background preloads and revalidation started while scrolling through cached rows wait the same amount. Set to 0 to request on every window change. Defaults to 500.
    /// </summary>
    [Parameter]
    public int ScrollDebounceMilliseconds { get; set; } = 500;

    /// <summary>
    ///     Gets or sets the key used to preserve this virtualizer's scroll position in the current browser history entry.
    /// </summary>
    [Parameter]
    public string? ScrollRestorationKey { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of items kept in the virtualizer cache.
    /// </summary>
    [Parameter]
    public int MaxCachedItemCount { get; set; } = 1_000;

    /// <summary>
    ///     <para>
    ///         Gets or sets the tag name of the HTML element that will be used as the virtualization spacer. One such element will be rendered before the visible items, and one more after them, using
    ///         an explicit "height" style to control the scroll range.
    ///     </para>
    ///     <para>
    ///         The default value is "div". If you are placing the <see cref="Virtualize{TItem}" /> instance inside an element that requires a specific child tag name, consider setting that here. For
    ///         example when rendering inside a "tbody", consider setting <see cref="SpacerElement" /> to the value "tr".
    ///     </para>
    /// </summary>
    [Parameter]
    public string SpacerElement { get; set; } = "div";

    private ElementReference _afterPlaceholder;
    private bool _countChangeReloaded;
    private int _itemCount;
    private int _itemsBefore;
    private float _itemSize;
    private int _lastRenderedItemCount;
    private int _lastRenderedPlaceholderCount;
    private NTVirtualizeItemsProvider<TItem>? _lastItemsProvider;
    private bool _loading;
    private CancellationTokenSource? _prefetchCts;
    private CancellationTokenSource? _revalidateCts;
    private int _revalidatingCount;
    private int _revalidatingStartIndex = -1;
    private CancellationTokenSource? _refreshCts;
    private Exception? _refreshException;
    private float _spacerAfterSize;
    private float _spacerBeforeSize;
    private int _visibleItemCapacity;
    private bool _hasLoadedItems;
    private bool _initialized;
    private (float ItemSize, int OverscanCount, int MaxItemCount, string? ScrollRestorationKey)? _reportedOptions;
    private (int Count, int Loaded, int Placeholders, bool Measure, int Revision, bool Loading, int Start, int Capacity)? _reportedRenderState;
    private NTVirtualizeAnchorSnapshot? _pendingAnchor;
    private int _pendingAnchorIndex;
    private long _scrollOperationId;
    private int _measurementRevision;
    private int _previousItemSizeVersion;
    private bool _preserveScrollOffset;

    /// <summary>
    ///     Loads items based on client-side virtualization calculations.
    /// </summary>
    /// <param name="spacerBeforeSize">The size of the spacer before the visible items in pixels.</param>
    /// <param name="spacerAfterSize"> The size of the spacer after the visible items in pixels.</param>
    /// <param name="startIndex">      The start index of the items to request.</param>
    /// <param name="count">           The number of items to request.</param>
    [JSInvokable]
    public void LoadItems(float spacerBeforeSize, float spacerAfterSize, int startIndex, int count) {
        if (DisposalStarted) {
            return;
        }
        var resolvedStartIndex = Math.Max(0, startIndex);
        var resolvedCount = Math.Max(0, count);
        if (_hasLoadedItems && (long)resolvedStartIndex + resolvedCount > _itemCount) {
            resolvedStartIndex = Math.Max(0, _itemCount - resolvedCount);
        }

        var resolvedSpacerBeforeSize = Math.Max(0, spacerBeforeSize);
        var resolvedSpacerAfterSize = Math.Max(0, spacerAfterSize);

        if (resolvedStartIndex != _itemsBefore
            || resolvedCount != _visibleItemCapacity
            || !resolvedSpacerBeforeSize.Equals(_spacerBeforeSize)
            || !resolvedSpacerAfterSize.Equals(_spacerAfterSize)
            || (!_loading && TryGetMissingRange(resolvedStartIndex, _hasLoadedItems ? Math.Min(resolvedCount, Math.Max(0, _itemCount - resolvedStartIndex)) : resolvedCount, out _, out _))) {
            _itemsBefore = resolvedStartIndex;
            _visibleItemCapacity = resolvedCount;
            _spacerBeforeSize = resolvedSpacerBeforeSize;
            _spacerAfterSize = resolvedSpacerAfterSize;
            _prefetchCts?.Cancel();
            _revalidateCts?.Cancel();
            var refreshTask = RefreshDataCoreAsync(renderOnSuccess: true, debounce: _hasLoadedItems);

            if (!refreshTask.IsCompleted) {
                _ = InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>
    ///     Asynchronously refreshes the underlying data and re-renders the component when the refresh completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous refresh operation.</returns>
    public Task RefreshDataAsync() => RefreshDataAsync(CancellationToken.None);

    /// <summary>Refreshes the current window without remounting, honoring caller cancellation.</summary>
    public async Task RefreshDataAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!DisposalStarted) {
            await RefreshDataCoreAsync(renderOnSuccess: true, forceRefresh: true, cancellationToken);
        }
    }

    /// <summary>Scrolls to a zero-based item index after interactive initialization. The latest call wins.</summary>
    public async Task ScrollToItemAsync(int itemIndex, CancellationToken cancellationToken = default) {
        if (!_initialized || !TryGetInteropReferences(out var module, out var dotNetRef)) {
            throw new InvalidOperationException("Scrolling requires an initialized interactive virtualizer. Use InitialItemIndex to set the initial position.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var operationId = ++_scrollOperationId;
        try {
            await module.InvokeVoidAsync("scrollToItem", cancellationToken, dotNetRef, Math.Max(0, itemIndex), operationId);
        }
        finally {
            if (cancellationToken.IsCancellationRequested && !DisposalStarted) {
                try {
                    await module.InvokeVoidAsync("cancelScroll", dotNetRef, operationId);
                }
                catch (JSDisconnectedException) { }
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            CancelAllWork();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore() {
        CancelAllWork();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await base.OnAfterRenderAsync(firstRender);
        try {
            if (firstRender) {
                if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                    await module.InvokeVoidAsync("init", dotNetRef, Element, _afterPlaceholder, _itemSize, OverscanCount, MaxItemCount, ScrollRestorationKey, 50, Math.Max(0, InitialItemIndex));
                    _initialized = !DisposalStarted;
                    _reportedOptions = (ItemSize, OverscanCount, MaxItemCount, ScrollRestorationKey);
                }
            }

            if (TryGetInteropReferences(out var currentModule, out var currentRef)) {
                var options = (ItemSize, OverscanCount, MaxItemCount, ScrollRestorationKey);
                if (_reportedOptions != options) {
                    _reportedOptions = options;
                    _reportedRenderState = null;
                    await currentModule.InvokeVoidAsync("updateOptions", currentRef, ItemSize, OverscanCount, MaxItemCount, ScrollRestorationKey);
                }

                if (_pendingAnchor is { } anchor) {
                    _pendingAnchor = null;
                    _reportedRenderState = null;
                    await currentModule.InvokeVoidAsync("restoreAnchor", currentRef, anchor, _pendingAnchorIndex, (int)AnchorMode);
                }
            }

            var renderState = (_itemCount, _lastRenderedItemCount, _lastRenderedPlaceholderCount, MeasureItemSize, _measurementRevision, _loading || !_hasLoadedItems, _itemsBefore, _visibleItemCapacity);
            if ((MeasureItemSize || _reportedRenderState != renderState) && TryGetInteropReferences(out var renderModule, out var renderRef)) {
                _reportedRenderState = renderState;
                var preserveScrollOffset = _preserveScrollOffset;
                _preserveScrollOffset = false;
                await renderModule.InvokeVoidAsync("updateRenderState", renderRef, _itemCount, _lastRenderedItemCount, _lastRenderedPlaceholderCount, MeasureItemSize, _measurementRevision, _loading || !_hasLoadedItems, preserveScrollOffset);
            }
        }
        catch (JSDisconnectedException) {
            // JS runtime was disconnected, safe to ignore during render.
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (_previousItemSizeVersion != ItemSizeVersion) {
            _previousItemSizeVersion = ItemSizeVersion;
            _measurementRevision++;
        }
        if (!float.IsFinite(ItemSize) || ItemSize <= 0) {
            throw new InvalidOperationException(
                $"{nameof(NTVirtualize<>)} requires a positive value for parameter '{nameof(ItemSize)}'.");
        }

        if (_itemSize <= 0) {
            _itemSize = ItemSize;
        }
        else if (!ItemSize.Equals(_itemSize)) {
            _itemSize = ItemSize;
            ClearCachedItems(cancelForegroundRefresh: true);
        }

        if (ItemsProvider is null) {
            throw new InvalidOperationException($"{nameof(NTVirtualize<>)} requires the '{nameof(ItemsProvider)}' parameter to be specified and non-null.");
        }

        if (_lastItemsProvider is not null && ItemsProvider != _lastItemsProvider) {
            SetItemCount(0);
            _hasLoadedItems = false;
            ClearCachedItems(cancelForegroundRefresh: true);
        }

        _lastItemsProvider = ItemsProvider;
        LoadingTemplate ??= DefaultPlaceholder;
    }

    private TimeSpan ScrollDebounceDelay => TimeSpan.FromMilliseconds(Math.Max(0, ScrollDebounceMilliseconds));

    private float GetAdjustedSpacerAfterSize(int preloadedPlaceholderCount) =>
        Math.Max(0, _spacerAfterSize - Math.Max(0, preloadedPlaceholderCount) * _itemSize);

    private int GetPreloadedPlaceholderCount(int lastItemIndex) {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0 || PlaceholderPreloadWindowCount <= 0) {
            return 0;
        }

        var remainingItemCount = Math.Max(0, _itemCount - Math.Max(0, lastItemIndex));
        var requestedPlaceholderCount = _visibleItemCapacity * PlaceholderPreloadWindowCount;
        return Math.Min(remainingItemCount, requestedPlaceholderCount);
    }

    private int GetBackgroundPreloadCount(int lastItemIndex) {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0 || BackgroundPreloadWindowCount <= 0) {
            return 0;
        }

        var remainingItemCount = Math.Max(0, _itemCount - Math.Max(0, lastItemIndex));
        var requestedItemCount = _visibleItemCapacity * BackgroundPreloadWindowCount;
        return Math.Min(remainingItemCount, requestedItemCount);
    }

    private bool TryGetMissingRange(int startIndex, int count, out int missingStartIndex, out int missingCount) {
        missingStartIndex = -1;
        var missingEndIndex = -1;
        var lastIndex = Math.Max(0, startIndex) + Math.Max(0, count);

        for (var index = Math.Max(0, startIndex); index < lastIndex; index++) {
            if (_itemCache.ContainsKey(index)) {
                continue;
            }

            if (missingStartIndex < 0) {
                missingStartIndex = index;
            }

            missingEndIndex = index;
        }

        missingCount = missingStartIndex < 0 ? 0 : missingEndIndex - missingStartIndex + 1;
        return missingCount > 0;
    }

    private void StoreItems(int startIndex, IReadOnlyCollection<TItem> items) {
        var index = Math.Max(0, startIndex);
        foreach (var item in items) {
            _itemCache[index++] = item;
        }

        TrimItemCache();
    }

    private void SetItemCount(int totalItemCount) {
        _itemCount = Math.Max(0, totalItemCount);
        _cacheIndexesToRemove.Clear();
        foreach (var index in _itemCache.Keys) {
            if (index >= _itemCount) {
                _cacheIndexesToRemove.Add(index);
            }
        }

        foreach (var index in _cacheIndexesToRemove) {
            _itemCache.Remove(index);
        }
    }

    private void TrimItemCache() {
        var minimumCacheSize = Math.Max(1, _visibleItemCapacity * Math.Max(1, PlaceholderPreloadWindowCount + BackgroundPreloadWindowCount + 1));
        var maxCachedItemCount = Math.Max(minimumCacheSize, MaxCachedItemCount);
        if (_itemCache.Count <= maxCachedItemCount) {
            return;
        }

        var activeStartIndex = Math.Max(0, _itemsBefore);
        var activeEndIndex = activeStartIndex + Math.Max(0, _visibleItemCapacity) + GetPreloadedPlaceholderCount(activeStartIndex + Math.Max(0, _visibleItemCapacity));
        var activeCenterIndex = activeStartIndex + Math.Max(0, activeEndIndex - activeStartIndex) / 2;
        _trimCandidateIndexes.Clear();
        foreach (var index in _itemCache.Keys) {
            if (index < activeStartIndex || index >= activeEndIndex) {
                _trimCandidateIndexes.Add(index);
            }
        }

        _trimCandidateIndexes.Sort((left, right) => Math.Abs(right - activeCenterIndex).CompareTo(Math.Abs(left - activeCenterIndex)));
        foreach (var index in _trimCandidateIndexes) {
            if (_itemCache.Count <= maxCachedItemCount) {
                return;
            }

            _itemCache.Remove(index);
        }
    }

    private void ClearCachedItems(bool cancelForegroundRefresh) {
        _itemCache.Clear();
        CancelAndDispose(ref _prefetchCts);
        CancelAndDispose(ref _revalidateCts);
        _revalidatingStartIndex = -1;
        _revalidatingCount = 0;

        if (cancelForegroundRefresh) {
            CancelAndDispose(ref _refreshCts);
            _loading = false;
        }
    }

    private RenderFragment DefaultPlaceholder(PlaceholderContext context) => (builder) => {
        builder.OpenComponent<NTSkeleton>(0);
        builder.AddAttribute(1, "data-nt-virtualize-size", context.Size.ToString(CultureInfo.InvariantCulture));
        builder.CloseComponent();
    };

    private async ValueTask RefreshDataCoreAsync(bool renderOnSuccess, bool forceRefresh = false, CancellationToken cancellationToken = default, bool countChangeReload = false, bool debounce = false) {
        CancelAndDispose(ref _refreshCts);
        _countChangeReloaded = countChangeReload;

        // Fetch only the missing part of the active range. Cached rows keep rendering while the missing rows remain placeholders.
        var startIndex = _itemsBefore;
        var count = _visibleItemCapacity;
        var missingStartIndex = startIndex;
        var missingCount = count;
        if (!forceRefresh && !TryGetMissingRange(startIndex, _hasLoadedItems ? Math.Min(count, Math.Max(0, _itemCount - startIndex)) : count, out missingStartIndex, out missingCount)) {
            _loading = false;
            // Scrolling through cached rows would otherwise start a background request on every window change.
            var backgroundDelay = debounce ? ScrollDebounceDelay : TimeSpan.Zero;
            StartBackgroundPreload(backgroundDelay);
            StartBackgroundRevalidation(startIndex, count, backgroundDelay);
            if (renderOnSuccess) {
                await InvokeAsync(StateHasChanged);
            }

            return;
        }

        if (count <= 0) {
            return; // Browser measurement owns the first request, including InitialItemIndex.
        }

        CancelAndDispose(ref _prefetchCts);
        CancelAndDispose(ref _revalidateCts);
        _pendingAnchor = null;
        var refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _refreshCts = refreshCancellation;
        var refreshToken = refreshCancellation.Token;
        _loading = true;

        var request = new NTVirtualizeItemsProviderRequest<TItem> {
            Count = missingCount,
            StartIndex = missingStartIndex,
            CancellationToken = refreshToken
        };

        try {
            // Scroll-driven loads wait for the window to settle; the next window change cancels this wait.
            if (debounce) {
                await Task.Delay(ScrollDebounceDelay, refreshToken);
            }

            NTVirtualizeAnchorSnapshot? anchor = null;
            var anchorItem = default(TItem);
            var hasAnchorItem = false;
            var oldItemCount = _itemCount;
            if (forceRefresh && AnchorMode != NTVirtualizeAnchorMode.None && TryGetInteropReferences(out var anchorModule, out var anchorRef)) {
                anchor = await anchorModule.InvokeAsync<NTVirtualizeAnchorSnapshot?>("captureAnchor", refreshToken, anchorRef);
                hasAnchorItem = anchor is not null && _itemCache.TryGetValue(anchor.Index, out anchorItem);
            }

            var result = await ItemsProvider!(request).AsTask().WaitAsync(refreshToken);
            refreshToken.ThrowIfCancellationRequested();

            // Providers can shrink while a window is loading, including an initial index beyond the end.
            var clampedStart = Math.Min(request.StartIndex, Math.Max(0, result.TotalItemCount - Math.Max(1, count)));
            if (request.StartIndex >= result.TotalItemCount && request.StartIndex > 0) {
                request = new NTVirtualizeItemsProviderRequest<TItem> { StartIndex = clampedStart, Count = count, CancellationToken = refreshToken };
                result = await ItemsProvider!(request).AsTask().WaitAsync(refreshToken);
                refreshToken.ThrowIfCancellationRequested();
            }

            var anchorIndex = anchor?.Index ?? startIndex;
            if (anchor is not null && hasAnchorItem) {
                var match = FindAnchorIndex(result.Items, request.StartIndex, anchorItem!);
                var countDelta = result.TotalItemCount - oldItemCount;
                if (match < 0 && countDelta != 0) {
                    // A prepend can move the old item beyond this window. Probe the count-shifted window,
                    // accepting it only when stable identity confirms that the old item is actually there.
                    var shiftedStart = (int)Math.Clamp((long)startIndex + countDelta, 0, Math.Max(0, result.TotalItemCount - count));
                    if (shiftedStart != request.StartIndex) {
                        var shiftedRequest = new NTVirtualizeItemsProviderRequest<TItem> { StartIndex = shiftedStart, Count = count, CancellationToken = refreshToken };
                        var shiftedResult = await ItemsProvider!(shiftedRequest).AsTask().WaitAsync(refreshToken);
                        refreshToken.ThrowIfCancellationRequested();
                        match = FindAnchorIndex(shiftedResult.Items, shiftedStart, anchorItem!);
                        if (match >= 0) {
                            request = shiftedRequest;
                            result = shiftedResult;
                        }
                    }
                }

                if (match >= 0) {
                    anchorIndex = match;
                }
            }

            // Only apply result if the task was not canceled.
            if (!refreshToken.IsCancellationRequested && !DisposalStarted) {
                if (forceRefresh) {
                    _itemCache.Clear();
                    _measurementRevision++;
                    _preserveScrollOffset = AnchorMode == NTVirtualizeAnchorMode.None;
                }
                if (forceRefresh || request.StartIndex != missingStartIndex) {
                    _itemsBefore = request.StartIndex;
                    _spacerBeforeSize = _itemsBefore * _itemSize;
                }
                SetItemCount(result.TotalItemCount);
                _hasLoadedItems = true;
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(request.StartIndex, result.Items);
                _loading = false;
                _pendingAnchor = anchor;
                _pendingAnchorIndex = Math.Clamp(anchorIndex, 0, Math.Max(0, _itemCount - 1));

                if (renderOnSuccess) {
                    await InvokeAsync(StateHasChanged);
                }

                StartBackgroundPreload(TimeSpan.Zero);
            }
        }
        catch (OperationCanceledException) when (refreshToken.IsCancellationRequested) {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception e) {
            // A reload started by background work stays opportunistic: cached rows remain and the next foreground load surfaces the failure.
            if (refreshToken.IsCancellationRequested || DisposalStarted || countChangeReload) {
                return;
            }
            // Cache this exception so the renderer can throw it.
            _refreshException = e;

            if (TryGetInteropReferences(out var module, out var dotNetRef)) {
                try {
                    await module.InvokeVoidAsync("cancelScroll", dotNetRef, _scrollOperationId);
                }
                catch (JSDisconnectedException) { }
            }

            // Re-render the component to throw the exception.
            await InvokeAsync(StateHasChanged);
        }
        finally {
            if (ReferenceEquals(_refreshCts, refreshCancellation)) {
                _refreshCts = null;
                _loading = false;
            }
            refreshCancellation.Dispose();
        }
    }

    private int FindAnchorIndex(IReadOnlyCollection<TItem> items, int startIndex, TItem anchorItem) {
        var key = ItemKey?.Invoke(anchorItem);
        var comparer = ItemComparer ?? EqualityComparer<TItem>.Default;
        foreach (var item in items) {
            if (ItemKey is not null ? Equals(key, ItemKey(item)) : comparer.Equals(anchorItem, item)) {
                return startIndex;
            }
            startIndex++;
        }
        return -1;
    }

    private void StartBackgroundPreload(TimeSpan delay) {
        if (_loading || ItemsProvider is null || _itemCount <= 0 || _visibleItemCapacity <= 0) {
            return;
        }

        var activeLastItemIndex = Math.Min(_itemsBefore + _visibleItemCapacity, _itemCount);
        var preloadCount = GetBackgroundPreloadCount(activeLastItemIndex);
        if (!TryGetMissingRange(activeLastItemIndex, preloadCount, out var missingStartIndex, out var missingCount)) {
            return;
        }

        CancelAndDispose(ref _prefetchCts);
        _prefetchCts = new CancellationTokenSource();
        _ = InvokeAsync(() => PreloadItemsAsync(missingStartIndex, missingCount, delay, _prefetchCts.Token));
    }

    private async Task PreloadItemsAsync(int startIndex, int count, TimeSpan delay, CancellationToken cancellationToken) {
        try {
            await Task.Delay(delay, cancellationToken);
            var result = await ItemsProvider!(new NTVirtualizeItemsProviderRequest<TItem> {
                Count = count,
                StartIndex = startIndex,
                CancellationToken = cancellationToken
            });

            if (!cancellationToken.IsCancellationRequested) {
                if (await ReloadIfItemCountChangedAsync(result.TotalItemCount)) {
                    return;
                }

                SetItemCount(result.TotalItemCount);
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(startIndex, result.Items);
                if (RangeIntersectsCurrentRender(startIndex, count)) {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch {
            // Background preloads are opportunistic. The foreground load path will surface provider failures when the range is actually requested.
        }
    }

    // A different total means rows were inserted, removed, or filtered, so no cached row can be trusted. Reload the active window like
    // RefreshDataAsync: cached rows stay visible until the replacement arrives. Only one reload runs per foreground load, so a total
    // that changes on every request adopts the new count instead of chaining reloads.
    private async Task<bool> ReloadIfItemCountChangedAsync(int totalItemCount) {
        if (totalItemCount == _itemCount || _countChangeReloaded) {
            return false;
        }

        await RefreshDataCoreAsync(renderOnSuccess: true, forceRefresh: true, countChangeReload: true);
        return true;
    }

    private void StartBackgroundRevalidation(int startIndex, int count, TimeSpan delay) {
        if (!RevalidateCachedItems || _loading || ItemsProvider is null || _itemCount <= 0 || count <= 0) {
            return;
        }

        var resolvedStartIndex = Math.Max(0, startIndex);
        var resolvedCount = Math.Min(Math.Max(0, count), Math.Max(0, _itemCount - resolvedStartIndex));
        if (resolvedCount <= 0) {
            return;
        }

        if (_revalidatingStartIndex == resolvedStartIndex && _revalidatingCount == resolvedCount) {
            return;
        }

        CancelAndDispose(ref _revalidateCts);
        _revalidateCts = new CancellationTokenSource();
        _revalidatingStartIndex = resolvedStartIndex;
        _revalidatingCount = resolvedCount;
        _ = InvokeAsync(() => RevalidateItemsAsync(resolvedStartIndex, resolvedCount, delay, _revalidateCts.Token));
    }

    private async Task RevalidateItemsAsync(int startIndex, int count, TimeSpan delay, CancellationToken cancellationToken) {
        try {
            await Task.Delay(delay, cancellationToken);
            var result = await ItemsProvider!(new NTVirtualizeItemsProviderRequest<TItem> {
                Count = count,
                StartIndex = startIndex,
                CancellationToken = cancellationToken
            });

            if (!cancellationToken.IsCancellationRequested) {
                if (await ReloadIfItemCountChangedAsync(result.TotalItemCount)) {
                    return;
                }

                SetItemCount(result.TotalItemCount);
                UpdateSpacerAfterSizeFromItemCount();
                StoreItems(startIndex, result.Items);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch {
            // Revalidation is opportunistic. Cached rows stay visible and the foreground load path remains responsible for surfacing provider failures.
        }
        finally {
            if (_revalidatingStartIndex == startIndex && _revalidatingCount == count) {
                _revalidatingStartIndex = -1;
                _revalidatingCount = 0;
            }
        }
    }

    private void UpdateSpacerAfterSizeFromItemCount() {
        if (_itemCount <= 0 || _visibleItemCapacity <= 0) {
            _spacerAfterSize = 0;
            return;
        }

        if (MeasureItemSize) {
            return;
        }

        var itemsAfter = Math.Max(0, _itemCount - _visibleItemCapacity - _itemsBefore);
        _spacerAfterSize = itemsAfter * _itemSize;
    }

    private bool RangeIntersectsCurrentRender(int startIndex, int count) {
        if (count <= 0) {
            return false;
        }

        var visibleLastItemIndex = Math.Min(_itemsBefore + _visibleItemCapacity, _itemCount);
        var renderLastItemIndex = visibleLastItemIndex + GetPreloadedPlaceholderCount(visibleLastItemIndex);
        return startIndex < renderLastItemIndex && startIndex + count > _itemsBefore;
    }

    private void CancelAllWork() {
        CancelAndDispose(ref _prefetchCts);
        CancelAndDispose(ref _revalidateCts);
        CancelAndDispose(ref _refreshCts);
        _revalidatingStartIndex = -1;
        _revalidatingCount = 0;
    }

    private static void CancelAndDispose(ref CancellationTokenSource? cancellationTokenSource) {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }
}

/// <summary>
///     Represents a request for items in a virtualized list.
/// </summary>
/// <typeparam name="TItem">The type of the items being requested.</typeparam>
public struct NTVirtualizeItemsProviderRequest<TItem>() {

    /// <summary>
    ///     Gets or sets the cancellation token for the request.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    ///     Gets or sets the maximum number of items to retrieve.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    ///     Gets or sets the properties to sort on and their sort directions.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, SortDirection>> SortOnProperties { get; init; } = [];

    /// <summary>
    ///     Gets or sets the start index of the requested items.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    ///     Implicitly converts an <see cref="NTItemsProviderRequest" /> to a <see cref="NTVirtualizeItemsProviderRequest{TItem}" />.
    /// </summary>
    /// <param name="request">The items provider request to convert.</param>
    /// <returns>A new <see cref="NTVirtualizeItemsProviderRequest{TItem}" /> with properties copied from the source request.</returns>
    /// <remarks>
    ///     This conversion allows a query-friendly items provider request to be used in virtualization contexts. The <see cref="CancellationToken" /> is set to the default value since it is not
    ///     available in the source request.
    /// </remarks>
    public static implicit operator NTVirtualizeItemsProviderRequest<TItem>(NTItemsProviderRequest request) {
        return new NTVirtualizeItemsProviderRequest<TItem> {
            Count = request.Count,
            SortOnProperties = request.SortOnProperties,
            StartIndex = request.StartIndex,
            CancellationToken = default
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="NTVirtualizeItemsProviderRequest{TItem}" /> to an <see cref="NTItemsProviderRequest" />.
    /// </summary>
    /// <param name="request">The virtualize items provider request to convert.</param>
    /// <returns>A new <see cref="NTItemsProviderRequest" /> with properties copied from the source request.</returns>
    /// <remarks>This conversion enables interoperability between the virtualization-specific request type and the query-friendly items provider request type.</remarks>
    public static implicit operator NTItemsProviderRequest(NTVirtualizeItemsProviderRequest<TItem> request) {
        return new NTItemsProviderRequest {
            StartIndex = request.StartIndex,
            Count = request.Count,
            Sorts = NTItemsProviderRequest.FormatSorts(request.SortOnProperties)
        };
    }
}

/// <summary>
///     Represents a method that asynchronously provides a virtualized collection of items based on the specified request parameters.
/// </summary>
/// <remarks>
///     Use this delegate to efficiently load large datasets on demand, such as in scenarios involving UI virtualization or incremental data loading. The provider should return only the items
///     specified by the request to optimize performance and resource usage.
/// </remarks>
/// <typeparam name="TItem">The type of items to be retrieved and provided by the items provider.</typeparam>
/// <param name="request">An object containing parameters that specify how items should be retrieved, such as the range or filtering criteria.</param>
/// <returns>A task that, when completed, provides a result containing the requested items and any associated metadata.</returns>
public delegate ValueTask<NTVirtualizeItemsProviderResult<TItem>> NTVirtualizeItemsProvider<TItem>(NTVirtualizeItemsProviderRequest<TItem> request);
