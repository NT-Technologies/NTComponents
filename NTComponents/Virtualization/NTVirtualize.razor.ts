type Maybe<T> = T | null | undefined;

interface DotNetVirtualizeRef {
    _id?: number | string;
    invokeMethodAsync(methodName: 'LoadItems', spacerBeforeSize: number, spacerAfterSize: number, startIndex: number, count: number): Promise<unknown> | void;
}

interface ScrollMetrics {
    containerSize: number;
    scrollTop: number;
}

interface VirtualizeState {
    measureItemSize: boolean;
    itemSizeVersion: number;
    measuredSizes: Map<number, number>;
    itemCount: number;
    itemSize: number;
    itemsBefore: number;
    maxItemCount: number;
    overscanCount: number;
    unusedItemCapacity: number;
    visibleItemCapacity: number;
}

interface AnchorSnapshot {
    index: number;
    offset: number;
    atStart: boolean;
    atEnd: boolean;
    version: number;
}

interface VirtualizeObservers {
    updateRenderState: (itemCount: number, placeholderCount: number, measureItemSize: boolean, itemSizeVersion: number, loading: boolean, preserveScrollOffset: boolean) => void;
    updateOptions: (itemSize: number, overscanCount: number, maxItemCount: number, restorationKey?: string | null) => void;
    scrollToItem: (index: number, operationId: number) => Promise<void>;
    cancelScroll: (operationId?: number) => void;
    captureAnchor: () => AnchorSnapshot;
    restoreAnchor: (snapshot: AnchorSnapshot, index: number, mode: number) => void;
    dispose: () => void;
    state: VirtualizeState;
}

type DotNetVirtualizeRefKey = DotNetVirtualizeRef | number | string;

const observersByDotNetRef = new Map<DotNetVirtualizeRefKey, VirtualizeObservers>();
const scrollPositionsStateKey = '__ntVirtualizeScrollPositions';
const scrollPersistenceInterval = 100;
const scrollInterruptKeys = new Set(['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End', ' ']);
const inputEvents = ['wheel', 'touchstart', 'pointerdown', 'keydown'];

/**
 * Initializes the virtualization component by setting up intersection observers and mutation observers
 * for the top and bottom spacers to handle dynamic loading of items in a virtualized list.
 */
export function init(dotNetRef: Maybe<DotNetVirtualizeRef>, topSpacer: HTMLElement, bottomSpacer: HTMLElement, itemSize: number, overscanCount: number, maxItemCount: number, scrollRestorationKey?: string | null, rootMargin = 50, initialItemIndex = 0): void {
    if (!dotNetRef) {
        return;
    }

    const activeDotNetRef = dotNetRef;
    const dotNetRefKey = getDotNetRefKey(activeDotNetRef);
    const existingState = observersByDotNetRef.get(dotNetRefKey)?.state;
    dispose(activeDotNetRef);
    if (!topSpacer.isConnected || !bottomSpacer.isConnected) {
        return;
    }

    let scrollContainer = findClosestScrollContainer(topSpacer);
    const overflowAnchorElement = scrollContainer ?? document.documentElement;
    const previousOverflowAnchor = overflowAnchorElement.style.overflowAnchor;
    overflowAnchorElement.style.overflowAnchor = 'none';

    if (isValidTableElement(bottomSpacer.parentElement)) {
        topSpacer.style.display = 'table-row';
        bottomSpacer.style.display = 'table-row';
    }

    const intersectionObserver = new IntersectionObserver(intersectionCallback, {
        root: scrollContainer,
        rootMargin: `${Math.max(0, rootMargin)}px`,
    });

    intersectionObserver.observe(topSpacer);
    intersectionObserver.observe(bottomSpacer);

    const mutationObserverBefore = createSpacerMutationObserver(topSpacer);
    const mutationObserverAfter = createSpacerMutationObserver(bottomSpacer);
    const scrollUpdateTargets = getScrollUpdateTargets(topSpacer, scrollContainer);
    let resolvedScrollRestorationKey = scrollRestorationKey?.trim() || null;
    let pendingScrollTop = getPersistedScrollPosition(resolvedScrollRestorationKey);
    let scheduledScrollUpdate: number | null = null;
    let lastScrollPersistenceTime = Number.NEGATIVE_INFINITY;
    const scheduleFrame = window.requestAnimationFrame ?? ((callback: FrameRequestCallback) => window.setTimeout(() => callback(performance.now()), 0));
    const cancelFrame = window.cancelAnimationFrame ?? window.clearTimeout;
    const scrollListenerOptions: AddEventListenerOptions = { passive: true, capture: true };
    let lastObservedScrollTop = -1;
    let interactionVersion = 0;
    let expectedScrollTop = getObservedScrollTop();
    const scrollCallback = (): void => {
        const observedScrollTop = getObservedScrollTop();
        if (Math.abs(observedScrollTop - expectedScrollTop) > 1) {
            interactionVersion++;
            expectedScrollTop = observedScrollTop;
            pendingAnchor = null;
            cancelScrollOperation();
        }
        if (scheduledScrollUpdate === null) {
            scheduledScrollUpdate = scheduleFrame(() => {
                scheduledScrollUpdate = null;
                requestVisibleItemDistribution(false);
                persistScrollPosition();
            });
        }
    };

    const flushScrollPersistence = (): void => persistScrollPosition(true);

    scrollUpdateTargets.forEach(target => target.addEventListener('scroll', scrollCallback, scrollListenerOptions));
    document.addEventListener('click', flushScrollPersistence, true);
    window.addEventListener('pagehide', flushScrollPersistence);
    const scrollPollInterval = window.setInterval(() => {
        const observedScrollTop = getObservedScrollTop();
        if (observedScrollTop !== lastObservedScrollTop) {
            lastObservedScrollTop = observedScrollTop;
            scrollCallback();
        }
    }, 100);

    const state: VirtualizeState = existingState ?? {
        measureItemSize: false,
        itemSizeVersion: 0,
        measuredSizes: new Map(),
        itemSize: 0,
        itemCount: 0,
        itemsBefore: 0,
        visibleItemCapacity: 0,
        unusedItemCapacity: 0,
        overscanCount: 0,
        maxItemCount: 0,
    };
    state.itemSize = Math.max(0, itemSize ?? 0);
    state.overscanCount = Math.max(0, overscanCount ?? 0);
    state.maxItemCount = Math.max(0, maxItemCount ?? 0);

    const resizeObserver = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => {
        if (measureItems()) {
            requestVisibleItemDistribution(true);
        }
    });
    let measuredElements = new Set<Element>();
    let cumulativeSizes: { index: number; delta: number }[] | null = null;
    let lastRenderLoading = true;
    let lastRenderPlaceholderCount = 0;
    let hasKnownItemCount = state.itemCount > 0;
    let pendingIndex: number | null = initialItemIndex > 0 ? Math.floor(initialItemIndex) : null;
    let pendingOperation: { id: number; resolve: () => void; reject: (error: unknown) => void } | null = null;
    let pendingAnchor: { snapshot: AnchorSnapshot; index: number; mode: number } | null = null;
    if (pendingIndex !== null) {
        pendingScrollTop = null;
    }
    const interruptScroll = (event: Event): void => {
        if (!(event instanceof KeyboardEvent) || scrollInterruptKeys.has(event.key)) {
            interactionVersion++;
            pendingAnchor = null;
            pendingScrollTop = null;
            cancelScrollOperation();
        }
    };
    const inputTarget = scrollContainer ?? window;
    inputEvents.forEach(event => inputTarget.addEventListener(event, interruptScroll, { passive: true }));

    function cancelScrollOperation(operationId?: number): void {
        if (operationId === undefined || pendingOperation?.id === operationId || (operationId === 0 && pendingOperation === null)) {
            pendingIndex = null;
            pendingOperation?.resolve();
            pendingOperation = null;
        }
    }

    function getContainer(): HTMLElement | null {
        return scrollContainer ??= findClosestScrollContainer(topSpacer);
    }

    function getMetrics(): ScrollMetrics {
        return getScrollMetrics(scrollContainer, topSpacer);
    }

    function scrollToTop(top: number): void {
        if (scrollContainer) {
            scrollContainer.scrollTop = top;
        }
        else {
            window.scrollTo(0, top);
        }
    }

    function clampItemIndex(index: number): number {
        return Math.min(Math.max(0, index), Math.max(0, state.itemCount - 1));
    }

    function getRenderedRange(): [number, number] {
        return [
            Number(topSpacer.getAttribute('data-nt-virtualize-start') ?? state.itemsBefore),
            Number(bottomSpacer.getAttribute('data-nt-virtualize-end') ?? Math.min(state.itemCount, state.itemsBefore + state.visibleItemCapacity)),
        ];
    }

    function setItemScrollOffset(offset: number): void {
        scrollToTop(Math.max(0, getObservedScrollTop() + offset - getMetrics().scrollTop));
        expectedScrollTop = getObservedScrollTop();
    }

    function afterRender(placeholderCount: number, loading: boolean): void {
        hasKnownItemCount ||= state.itemCount > 0 || !loading;
        lastRenderLoading = loading;
        lastRenderPlaceholderCount = placeholderCount;
        if (pendingAnchor) {
            const { snapshot, index, mode } = pendingAnchor;
            pendingAnchor = null;
            if (snapshot.version === interactionVersion) {
                const { containerSize } = getMetrics();
                setItemScrollOffset(mode === 1 && snapshot.atStart ? 0
                    : mode === 2 && snapshot.atEnd ? Math.max(0, itemOffset(state.itemCount) - containerSize)
                    : itemOffset(clampItemIndex(index)) + snapshot.offset);
            }
        }
        if (pendingIndex === null || loading) {
            return;
        }
        if (state.itemCount === 0) {
            if (placeholderCount === 0) {
                cancelScrollOperation();
            }
            return;
        }
        pendingIndex = Math.min(pendingIndex, state.itemCount - 1);
        const [renderedStart, renderedEnd] = getRenderedRange();
        if (pendingIndex >= renderedStart && pendingIndex < renderedEnd && placeholderCount === 0) {
            setItemScrollOffset(Math.min(itemOffset(pendingIndex), Math.max(0, itemOffset(state.itemCount) - getMetrics().containerSize)));
            cancelScrollOperation();
            persistScrollPosition(true);
        }
    }

    function measureItems(reset = false, preserveScrollOffset = false, previousScrollTop = getObservedScrollTop()): boolean {
        const scrollOffset = getMetrics().scrollTop;
        const anchorIndex = clampItemIndex(itemAtOffset(scrollOffset));
        const anchorOffset = itemOffset(anchorIndex);
        const offsetWithinItem = Math.max(0, scrollOffset - anchorOffset);
        if (reset) {
            state.measuredSizes.clear();
            cumulativeSizes = null;
        }
        const sizes = new Map<number, number>();
        const elements = new Set<Element>();
        for (let row = topSpacer.nextElementSibling; state.measureItemSize && row && row !== bottomSpacer; row = row.nextElementSibling) {
            const indexAttribute = row.getAttribute('data-nt-virtualize-index');
            const index = indexAttribute === null ? -1 : Number(indexAttribute);
            if (Number.isInteger(index) && index >= 0) {
                sizes.set(index, (sizes.get(index) ?? 0) + row.getBoundingClientRect().height);
                elements.add(row);
                if (!measuredElements.has(row)) {
                    resizeObserver?.observe(row);
                }
            }
        }
        for (const element of measuredElements) {
            if (!elements.has(element)) {
                resizeObserver?.unobserve(element);
            }
        }
        measuredElements = elements;
        let changed = reset;
        for (const [index, size] of sizes) {
            if (size > 0 && Math.abs((state.measuredSizes.get(index) ?? state.itemSize) - size) > 0.5) {
                state.measuredSizes.set(index, size);
                cumulativeSizes = null;
                changed = true;
            }
        }
        if (changed && pendingScrollTop === null) {
            const anchorSize = state.measuredSizes.get(anchorIndex) ?? state.itemSize;
            const nextOffsetWithinItem = Math.min(offsetWithinItem, Math.max(0, anchorSize - 1));
            const adjustment = itemOffset(anchorIndex) - anchorOffset + nextOffsetWithinItem - offsetWithinItem;
            // Update the currently rendered spacers before adjusting scrollTop. Waiting for
            // the asynchronous Blazor render would move the anchor a second time or clamp it.
            const [renderedStart, renderedEnd] = getRenderedRange();
            setSpacerSize(topSpacer, itemOffset(renderedStart));
            setSpacerSize(bottomSpacer, itemOffset(state.itemCount) - itemOffset(renderedEnd));
            if (preserveScrollOffset || Math.abs(adjustment) > 0.5) {
                scrollToTop(previousScrollTop + (preserveScrollOffset ? 0 : adjustment));
                expectedScrollTop = getObservedScrollTop();
                persistScrollPosition(true);
            }
        }
        return changed;
    }

    function setSpacerSize(spacer: HTMLElement, size: number): void {
        const height = `${Math.max(0, size)}px`;
        spacer.style.height = height;
        if (spacer.firstElementChild instanceof HTMLTableCellElement) {
            spacer.firstElementChild.style.height = height;
        }
    }

    function itemOffset(index: number): number {
        if (cumulativeSizes === null) {
            let delta = 0;
            cumulativeSizes = [...state.measuredSizes].sort(([a], [b]) => a - b).map(([measuredIndex, size]) => ({ index: measuredIndex, delta: delta += size - state.itemSize }));
        }
        let low = 0;
        let high = cumulativeSizes.length;
        while (low < high) {
            const middle = (low + high) >>> 1;
            if (cumulativeSizes[middle]!.index < index) {
                low = middle + 1;
            }
            else {
                high = middle;
            }
        }
        return index * state.itemSize + (low > 0 ? cumulativeSizes[low - 1]!.delta : 0);
    }

    function itemAtOffset(offset: number): number {
        if (state.measuredSizes.size === 0) {
            return Math.floor(offset / state.itemSize);
        }
        let low = 0;
        let high = state.itemCount;
        while (low < high) {
            const middle = Math.ceil((low + high) / 2);
            if (itemOffset(middle) <= offset) {
                low = middle;
            }
            else {
                high = middle - 1;
            }
        }
        return low;
    }

    observersByDotNetRef.set(dotNetRefKey, {
        updateRenderState: (itemCount, placeholderCount, measureItemSize, itemSizeVersion, loading, preserveScrollOffset) => {
            const previousItemCount = state.itemCount;
            state.itemCount = Math.max(0, itemCount);
            const measurementChanged = state.measureItemSize !== measureItemSize;
            state.measureItemSize = measureItemSize;
            const versionChanged = state.itemSizeVersion !== itemSizeVersion;
            state.itemSizeVersion = itemSizeVersion;
            for (const index of state.measuredSizes.keys()) {
                if (index >= state.itemCount) {
                    state.measuredSizes.delete(index);
                    cumulativeSizes = null;
                }
            }
            const scrollTopBeforeSizing = preserveScrollOffset ? getObservedScrollTop() : undefined;
            applySizes(topSpacer.parentElement);
            const sizesChanged = (measureItemSize || measurementChanged) && measureItems(versionChanged || measurementChanged, preserveScrollOffset, scrollTopBeforeSizing);
            afterRender(placeholderCount, loading);
            const restoredScrollPosition = restoreScrollPosition();

            requestVisibleItemDistribution(versionChanged || sizesChanged || state.itemCount !== previousItemCount || restoredScrollPosition);
        },
        updateOptions: (size, overscan, maximum, restorationKey) => {
            const sizeChanged = state.itemSize !== size;
            state.itemSize = Math.max(1, size);
            state.overscanCount = Math.max(0, overscan);
            state.maxItemCount = Math.max(1, maximum);
            const key = restorationKey?.trim() || null;
            if (key !== resolvedScrollRestorationKey) {
                resolvedScrollRestorationKey = key;
                pendingScrollTop = getPersistedScrollPosition(key);
            }
            if (sizeChanged) {
                state.measuredSizes.clear();
                cumulativeSizes = null;
                measureItems();
            }
            restoreScrollPosition();
            requestVisibleItemDistribution(true);
        },
        scrollToItem: (index, operationId) => {
            cancelScrollOperation();
            pendingAnchor = null;
            pendingScrollTop = null;
            pendingIndex = Math.max(0, Math.floor(index));
            return new Promise<void>((resolve, reject) => {
                pendingOperation = { id: operationId, resolve, reject };
                afterRender(lastRenderPlaceholderCount, lastRenderLoading);
                requestVisibleItemDistribution(pendingOperation !== null);
            });
        },
        cancelScroll: cancelScrollOperation,
        captureAnchor: () => {
            const { scrollTop, containerSize } = getMetrics();
            const index = clampItemIndex(itemAtOffset(scrollTop));
            return { index, offset: scrollTop - itemOffset(index), atStart: scrollTop <= 1, atEnd: scrollTop + containerSize >= itemOffset(state.itemCount) - 1, version: interactionVersion };
        },
        restoreAnchor: (snapshot, index, mode) => {
            if (snapshot.version === interactionVersion) {
                // Matching one item does not establish that every measured item moved by
                // the same delta (sorting and replacements can also move that identity).
                // Re-measure the new window rather than attach old heights to new items.
                state.measuredSizes.clear();
                cumulativeSizes = null;
                pendingAnchor = { snapshot, index, mode };
            }
        },
        dispose: () => {
            cancelScrollOperation();
            inputEvents.forEach(event => inputTarget.removeEventListener(event, interruptScroll));
            resizeObserver?.disconnect();
            intersectionObserver.disconnect();
            mutationObserverBefore.disconnect();
            mutationObserverAfter.disconnect();
            if (scheduledScrollUpdate !== null) {
                cancelFrame(scheduledScrollUpdate);
                scheduledScrollUpdate = null;
            }
            window.clearInterval(scrollPollInterval);
            scrollUpdateTargets.forEach(target => target.removeEventListener('scroll', scrollCallback, scrollListenerOptions));
            document.removeEventListener('click', flushScrollPersistence, true);
            window.removeEventListener('pagehide', flushScrollPersistence);
            if (overflowAnchorElement.isConnected) {
                overflowAnchorElement.style.overflowAnchor = previousOverflowAnchor;
            }
            observersByDotNetRef.delete(dotNetRefKey);
        },
        state,
    });

    function createSpacerMutationObserver(spacer: HTMLElement): MutationObserver {
        const observerOptions: MutationObserverInit = { attributes: true, attributeFilter: ['style', 'data-nt-virtualize-size'] };
        const mutationObserver = new MutationObserver((_, observer) => {
            if (isValidTableElement(spacer.parentElement)) {
                observer.disconnect();
                spacer.style.display = 'table-row';
                observer.observe(spacer, observerOptions);
            }

            intersectionObserver.unobserve(spacer);
            intersectionObserver.observe(spacer);

            if (restoreScrollPosition()) {
                requestVisibleItemDistribution(true);
            }
        });

        mutationObserver.observe(spacer, observerOptions);
        return mutationObserver;
    }

    function persistScrollPosition(force = false): void {
        const now = performance.now();
        if (!resolvedScrollRestorationKey || pendingScrollTop !== null || (!force && now - lastScrollPersistenceTime < scrollPersistenceInterval)) {
            return;
        }

        lastScrollPersistenceTime = now;
        const scrollTop = getObservedScrollTop();
        const historyState = isRecord(history.state) ? history.state : {};
        const existingPositions = isRecord(historyState[scrollPositionsStateKey]) ? historyState[scrollPositionsStateKey] : {};
        if (existingPositions[resolvedScrollRestorationKey] === scrollTop) {
            return;
        }

        try {
            history.replaceState({
                ...historyState,
                [scrollPositionsStateKey]: {
                    ...existingPositions,
                    [resolvedScrollRestorationKey]: scrollTop,
                },
            }, '');
        }
        catch {
            // Scroll restoration must never interrupt virtualization when history state is unavailable.
        }
    }

    function restoreScrollPosition(): boolean {
        if (pendingScrollTop === null || (pendingScrollTop > 0 && state.itemCount === 0)) {
            return false;
        }

        const container = getContainer();
        const targetScrollTop = Math.min(pendingScrollTop, Math.max(0, itemOffset(state.itemCount) - getMetrics().containerSize));
        const availableScrollTop = container
            ? Math.max(0, container.scrollHeight - container.clientHeight)
            : Math.max(0, document.documentElement.scrollHeight - (window.innerHeight || document.documentElement.clientHeight));
        if (availableScrollTop + 1 < targetScrollTop) {
            return false;
        }

        scrollToTop(targetScrollTop);
        if (Math.abs(getObservedScrollTop() - targetScrollTop) >= 1) {
            return false;
        }

        pendingScrollTop = null;
        expectedScrollTop = getObservedScrollTop();
        return true;
    }

    function intersectionCallback(entries: IntersectionObserverEntry[]): void {
        if (topSpacer.parentElement && bottomSpacer.parentElement && entries.some(entry => entry.isIntersecting)) {
            requestVisibleItemDistribution(false);
        }
    }

    function requestVisibleItemDistribution(forceUpdate: boolean): void {
        if (!topSpacer.parentElement || !bottomSpacer.parentElement) {
            return;
        }

        getContainer();
        const { scrollTop, containerSize } = getMetrics();
        const requestedOffset = pendingIndex === null ? scrollTop : itemOffset(hasKnownItemCount ? clampItemIndex(pendingIndex) : pendingIndex);
        const firstVisibleIndex = itemAtOffset(requestedOffset);
        let visibleItemCapacity = Math.ceil(containerSize / state.itemSize) + 2 * state.overscanCount;
        if (state.measureItemSize) {
            // The estimate is a minimum: shorter measured rows need more items to fill the viewport.
            const viewportEnd = requestedOffset + containerSize;
            const endIndex = itemAtOffset(viewportEnd);
            const requiredCount = endIndex - firstVisibleIndex + (itemOffset(endIndex) < viewportEnd ? 1 : 0);
            visibleItemCapacity = Math.max(visibleItemCapacity, requiredCount + 2 * state.overscanCount);
        }
        const unusedItemCapacity = Math.max(0, visibleItemCapacity - (state.maxItemCount + state.overscanCount * 2));
        visibleItemCapacity -= unusedItemCapacity;
        let itemsBefore = Math.max(0, firstVisibleIndex - state.overscanCount);
        if (hasKnownItemCount && itemsBefore + visibleItemCapacity > state.itemCount) {
            itemsBefore = Math.max(0, state.itemCount - visibleItemCapacity);
        }

        if (forceUpdate || itemsBefore !== state.itemsBefore || visibleItemCapacity !== state.visibleItemCapacity || unusedItemCapacity !== state.unusedItemCapacity) {
            state.itemsBefore = itemsBefore;
            state.visibleItemCapacity = visibleItemCapacity;
            state.unusedItemCapacity = unusedItemCapacity;

            const itemsAfter = Math.max(0, state.itemCount - visibleItemCapacity - itemsBefore);
            const bottomSpacerSize = itemOffset(state.itemCount) - itemOffset(state.itemCount - itemsAfter) + unusedItemCapacity * state.itemSize;

            const operation = pendingOperation;
            void Promise.resolve(activeDotNetRef.invokeMethodAsync('LoadItems', itemOffset(itemsBefore), bottomSpacerSize, itemsBefore, visibleItemCapacity)).catch(error => {
                if (operation && pendingOperation === operation) {
                    pendingIndex = null;
                    pendingOperation = null;
                    operation.reject(error);
                }
            });
        }
    }

    function getObservedScrollTop(): number {
        const container = getContainer();
        return container ? Math.max(0, container.scrollTop) : Math.max(0, window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0);
    }
}

function getDotNetRefKey(dotNetRef: DotNetVirtualizeRef): DotNetVirtualizeRefKey {
    return dotNetRef._id ?? dotNetRef;
}

function getObservers(dotNetRef: Maybe<DotNetVirtualizeRef>): VirtualizeObservers | undefined {
    return dotNetRef ? observersByDotNetRef.get(getDotNetRefKey(dotNetRef)) : undefined;
}

function getPersistedScrollPosition(scrollRestorationKey: string | null): number | null {
    const positions = scrollRestorationKey && isRecord(history.state) ? history.state[scrollPositionsStateKey] : null;
    const scrollTop = isRecord(positions) ? positions[scrollRestorationKey!] : null;
    return typeof scrollTop === 'number' && Number.isFinite(scrollTop) && scrollTop >= 0 ? scrollTop : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isValidTableElement(element: Maybe<Element>): boolean {
    return element instanceof HTMLElement
        && (((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
            || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group'));
}

function getScrollMetrics(scrollContainer: Maybe<HTMLElement>, topSpacer: HTMLElement): ScrollMetrics {
    const topSpacerRect = topSpacer.getBoundingClientRect();
    // Read once; the header scan below only reads layout, so the rect cannot change before it is reused.
    const scrollContainerRect = scrollContainer?.getBoundingClientRect();
    const viewportTop = scrollContainer && scrollContainerRect ? scrollContainerRect.top + scrollContainer.clientTop : 0;
    let headerInset = 0;
    for (const cell of topSpacer.closest('table')?.tHead?.querySelectorAll('th') ?? []) {
        const style = getComputedStyle(cell);
        if (style.position === 'sticky' && style.top !== 'auto') {
            const rect = cell.getBoundingClientRect();
            if (rect.top <= viewportTop + (parsePixelValue(style.top) ?? 0) + 1 && rect.bottom > viewportTop) {
                headerInset = Math.max(headerInset, rect.bottom - viewportTop);
            }
        }
    }

    // max-height can be larger than clientHeight before initial overflow exists.
    const containerSize = scrollContainer
        ? Math.max(scrollContainer.clientHeight, parsePixelValue(getComputedStyle(scrollContainer).maxHeight) ?? 0)
        : window.innerHeight || document.documentElement.clientHeight;

    return {
        scrollTop: Math.max(0, viewportTop + headerInset - topSpacerRect.top),
        containerSize: Math.max(0, containerSize - headerInset),
    };
}

function parsePixelValue(value: string): number | null {
    const parsed = !value || value === 'none' ? Number.NaN : Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
}

function findClosestScrollContainer(element: Maybe<HTMLElement>): HTMLElement | null {
    for (let current = element; current && current !== document.body && current !== document.documentElement; current = current.parentElement) {
        const overflowY = getComputedStyle(current).overflowY;
        if (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay' || (overflowY !== 'visible' && current.scrollHeight > current.clientHeight)) {
            return current;
        }
    }

    return null;
}

function getScrollUpdateTargets(element: HTMLElement, scrollContainer: Maybe<HTMLElement>): EventTarget[] {
    const targets = new Set<EventTarget>();
    if (scrollContainer) {
        targets.add(scrollContainer);
    }

    for (let current: Maybe<HTMLElement> = element; current && current !== document.body && current !== document.documentElement; current = current.parentElement) {
        targets.add(current);
    }

    return [...targets, window];
}

function dispose(dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    getObservers(dotNetRef)?.dispose();
}

export function updateRenderState(dotNetRef: Maybe<DotNetVirtualizeRef>, itemCount: Maybe<number>, _lastRenderedItemCount: Maybe<number>, _lastRenderedPlaceholderCount: Maybe<number>, measureItemSize = false, itemSizeVersion = 0, loading = false, preserveScrollOffset = false): void {
    getObservers(dotNetRef)?.updateRenderState(itemCount ?? 0, _lastRenderedPlaceholderCount ?? 0, measureItemSize, itemSizeVersion, loading, preserveScrollOffset);
}

function applySizes(element: Maybe<HTMLElement>): void {
    for (const target of element ? [element, ...element.querySelectorAll<HTMLElement>('[data-nt-virtualize-size]')] : []) {
        const size = Number(target.getAttribute('data-nt-virtualize-size'));
        if (target.hasAttribute('data-nt-virtualize-size') && Number.isFinite(size) && size >= 0) {
            const height = `${size}px`;
            setStyle(target, 'height', height);
            setStyle(target, 'flex-shrink', '0');
            if (target.firstElementChild instanceof HTMLTableCellElement) {
                setStyle(target.firstElementChild, 'height', height);
            }
            if (target.hasAttribute('data-nt-virtualize-placeholder')) {
                setStyle(target, '--nt-data-grid-placeholder-row-height', height);
            }
        }
    }
}

// Writes only on change so style mutation observers are not retriggered.
function setStyle(element: HTMLElement, name: string, value: string): void {
    if (element.style.getPropertyValue(name) !== value) {
        element.style.setProperty(name, value);
    }
}

export function updateOptions(dotNetRef: DotNetVirtualizeRef, itemSize: number, overscanCount: number, maxItemCount: number, scrollRestorationKey?: string | null): void {
    getObservers(dotNetRef)?.updateOptions(itemSize, overscanCount, maxItemCount, scrollRestorationKey);
}

export function scrollToItem(dotNetRef: DotNetVirtualizeRef, index: number, operationId: number): Promise<void> {
    return getObservers(dotNetRef)?.scrollToItem(index, operationId) ?? Promise.resolve();
}

export function cancelScroll(dotNetRef: DotNetVirtualizeRef, operationId: number): void {
    getObservers(dotNetRef)?.cancelScroll(operationId);
}

export function captureAnchor(dotNetRef: DotNetVirtualizeRef): AnchorSnapshot | null {
    return getObservers(dotNetRef)?.captureAnchor() ?? null;
}

export function restoreAnchor(dotNetRef: DotNetVirtualizeRef, snapshot: AnchorSnapshot, index: number, anchorMode: number): void {
    getObservers(dotNetRef)?.restoreAnchor(snapshot, index, anchorMode);
}

export function onLoad(element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    applySizes(element?.parentElement);
}

export function onUpdate(element: Maybe<HTMLElement>, _dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    applySizes(element?.parentElement);
}

export function onDispose(_element: Maybe<HTMLElement>, dotNetRef: Maybe<DotNetVirtualizeRef>): void {
    dispose(dotNetRef);
}
