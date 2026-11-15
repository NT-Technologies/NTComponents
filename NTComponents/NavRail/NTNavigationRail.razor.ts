type Maybe<T> = T | null | undefined;

interface NavigationRailState {
    button: HTMLButtonElement | null;
    defaultOpenApplied?: boolean;
    dialogCloseTimeout?: number;
    expanded?: boolean;
    externalButton?: HTMLButtonElement | null;
    generatedDialog?: boolean;
    groupHandlers: NavigationRailGroupHandler[];
    modalDialog?: HTMLDialogElement | null;
    modalHiddenElements?: ModalHiddenElement[];
    modalOriginalNextSibling?: ChildNode | null;
    modalOriginalParent?: Node | null;
    modalPlaceholder?: HTMLElement | null;
    onClick?: () => void;
    onExternalClick?: () => void;
    onKeyDown?: (event: KeyboardEvent) => void;
    onNavigationClick?: (event: MouseEvent) => void;
    onModalDialogCancel?: (event: Event) => void;
    onModalDialogClick?: (event: MouseEvent) => void;
    onModalDialogClose?: () => void;
    onOutsidePointerDown?: (event: PointerEvent) => void;
    onRailScroll?: () => void;
    onResponsiveModalChange?: () => void;
    onSmallScreenChange?: () => void;
    popoverPositionFrame?: number;
    responsiveModalQuery?: MediaQueryList;
    restoreFocusAfterModalClose?: boolean;
    scrollContainer?: HTMLElement | null;
    smallScreenMediaQuery?: MediaQueryList;
    transitionTimeout?: number;
}

interface NavigationRailElement extends HTMLElement {
    __ntNavigationRailState?: NavigationRailState;
}

interface NavigationRailGroupHandler {
    group: HTMLElement;
    panel: HTMLElement;
    trigger: HTMLButtonElement;
    onClick: (event: MouseEvent) => void;
    onToggle: () => void;
}

interface ModalHiddenElement {
    ariaHidden: string | null;
    element: HTMLElement;
    inert: boolean;
}

interface NTComponentsGlobals {
    registerButtonInteraction?: (element: Maybe<Element>) => void;
}

interface RouteSelectionContext {
    baseHref: string;
    path: string;
}

const smallScreenMinWidth = 600;
const mediumScreenMinWidth = 840;
// Where the collapsed rail stays visible. Elsewhere (extra-small, or small in portrait) a hide-on-xs rail becomes the modal.
const railScreenQuery = `(min-width: ${mediumScreenMinWidth}px), (min-width: ${smallScreenMinWidth}px) and (orientation: landscape)`;
const mediumScreenQuery = `(min-width: ${mediumScreenMinWidth}px)`;
const navigationRailTransitionDuration = 550;
const responsiveModalClass = 'nt-navigation-rail-responsive-modal';
const selectedItemClass = 'nt-navigation-rail-item-selected';
const deselectingItemClass = 'nt-navigation-rail-item-deselecting';
const indicatorExitAnimationName = 'nt-navigation-rail-indicator-exit';
const expandedWidthProperty = '--nt-navigation-rail-expanded-width';
const measuringClass = 'nt-navigation-rail-measuring';
const expandedStateByRailId = new Map<string, boolean>();

declare global {
    interface Window {
        NTComponents?: NTComponentsGlobals;
    }
}

function getRails(): NavigationRailElement[] {
    return Array.from(document.querySelectorAll<NavigationRailElement>('.nt-navigation-rail'));
}

function getTargetRails(rail: Maybe<NavigationRailElement>): NavigationRailElement[] {
    return rail instanceof HTMLElement && rail.classList.contains('nt-navigation-rail')
        ? [rail]
        : getRails();
}

function getTargetRailsForDispose(target: Maybe<Element>): NavigationRailElement[] {
    if (!(target instanceof HTMLElement)) {
        return target == null ? getRails() : [];
    }

    if (target.classList.contains('nt-navigation-rail')) {
        return [target as NavigationRailElement];
    }

    const containingRail = target.closest<NavigationRailElement>('.nt-navigation-rail');
    return containingRail ? [containingRail] : [];
}

function isOwnedByRail(rail: NavigationRailElement, element: Element): boolean {
    return element.closest('.nt-navigation-rail') === rail;
}

function getMenuButton(rail: NavigationRailElement): HTMLButtonElement | null {
    return Array.from(rail.querySelectorAll<HTMLButtonElement>(':scope .nt-navigation-rail-menu-button'))
        .find(button => isOwnedByRail(rail, button)) ?? null;
}

function getExternalMenuButton(rail: NavigationRailElement): HTMLButtonElement | null {
    if (!rail.id) {
        return null;
    }

    const expectedId = `${rail.id}-xs-menu-button`;
    const buttonById = document.getElementById(expectedId);

    if (buttonById instanceof HTMLButtonElement
        && buttonById.dataset.ntNavigationRailExternalTrigger === 'true'
        && buttonById.getAttribute('aria-controls') === rail.id) {
        return buttonById;
    }

    return Array.from(document.querySelectorAll<HTMLButtonElement>('.nt-navigation-rail-xs-menu-button[data-nt-navigation-rail-external-trigger="true"]'))
        .find(button => button.getAttribute('aria-controls') === rail.id) ?? null;
}

function getMenuButtons(rail: NavigationRailElement): HTMLButtonElement[] {
    return Array.from(new Set([getMenuButton(rail), getExternalMenuButton(rail)]
        .filter((button): button is HTMLButtonElement => button !== null)));
}

function isModalDialogElement(element: Element | null): element is HTMLDialogElement {
    return element instanceof HTMLElement
        && element.tagName === 'DIALOG'
        && element.classList.contains('nt-navigation-rail-modal-dialog');
}

function copyScopedCssAttributes(source: HTMLElement, target: HTMLElement): void {
    source.getAttributeNames().forEach(attributeName => {
        if (attributeName.startsWith('b-')) {
            target.setAttribute(attributeName, source.getAttribute(attributeName) ?? '');
        }
    });
}

function getModalDialog(rail: NavigationRailElement): HTMLDialogElement | null {
    if (isModalDialogElement(rail.parentElement)) {
        return rail.parentElement;
    }

    const previous = rail.previousElementSibling;

    return isModalDialogElement(previous)
        ? previous
        : null;
}

function getMenuIcon(button: HTMLButtonElement): HTMLElement | null {
    return button.querySelector<HTMLElement>(':scope .nt-navigation-rail-menu-icon .nt-icon');
}

function shouldOpenByDefault(rail: NavigationRailElement, state: NavigationRailState): boolean {
    if (state.defaultOpenApplied || rail.dataset.ntNavigationRailOpenByDefault !== 'true') {
        return false;
    }

    state.defaultOpenApplied = true;
    return window.matchMedia?.(mediumScreenQuery).matches ?? window.innerWidth >= mediumScreenMinWidth;
}

function getItems(rail: NavigationRailElement): HTMLElement[] {
    return Array.from(rail.querySelectorAll<HTMLElement>(':scope .nt-navigation-rail-item'))
        .filter(item => isOwnedByRail(rail, item));
}

function getSectionHeaders(rail: NavigationRailElement): HTMLElement[] {
    return Array.from(rail.querySelectorAll<HTMLElement>(':scope .nt-navigation-rail-section-header'))
        .filter(header => isOwnedByRail(rail, header));
}

function getGroups(rail: NavigationRailElement): HTMLElement[] {
    return Array.from(rail.querySelectorAll<HTMLElement>(':scope .nt-navigation-rail-group'))
        .filter(group => isOwnedByRail(rail, group));
}

function isTopLevelGroup(rail: NavigationRailElement, group: HTMLElement): boolean {
    const parentGroup = group.parentElement?.closest<HTMLElement>('.nt-navigation-rail-group');
    return !parentGroup || !rail.contains(parentGroup);
}

function getTopLevelGroups(rail: NavigationRailElement): HTMLElement[] {
    return getGroups(rail).filter(group => isTopLevelGroup(rail, group));
}

function getGroupTrigger(group: HTMLElement): HTMLButtonElement | null {
    return group.querySelector<HTMLButtonElement>(':scope > .nt-navigation-rail-group-trigger');
}

function getGroupTriggerIcon(trigger: HTMLButtonElement): HTMLElement | null {
    return trigger.querySelector<HTMLElement>(':scope .nt-navigation-rail-item-icon .nt-icon');
}

function isMaterialSymbolIcon(icon: HTMLElement): boolean {
    return icon.classList.contains('material-symbols-outlined')
        || icon.classList.contains('material-symbols-rounded')
        || icon.classList.contains('material-symbols-sharp');
}

function normalizePath(path: string): string {
    const pathOnly = path.split(/[?#]/, 1)[0]?.replace(/^\/+|\/+$/g, '') ?? '';
    return pathOnly.length === 0 ? '' : pathOnly;
}

function isUnderBaseUri(url: URL, baseHref: string): boolean {
    return url.href.toLowerCase().startsWith(baseHref);
}

function getBaseRelativePath(url: URL, baseHref: string): string {
    return normalizePath(url.href.slice(baseHref.length)).toLowerCase();
}

function getRouteSelectionContext(): RouteSelectionContext {
    const baseUrl = new URL(document.baseURI);
    const currentUrl = new URL(window.location.href);
    const baseHref = baseUrl.href.toLowerCase();

    return {
        baseHref,
        path: isUnderBaseUri(currentUrl, baseHref)
            ? getBaseRelativePath(currentUrl, baseHref)
            : normalizePath(currentUrl.pathname).toLowerCase()
    };
}

function routeMatches(item: HTMLElement, currentLocation: RouteSelectionContext): boolean {
    const href = item.getAttribute('href');

    if (!href) {
        return false;
    }

    const itemUrl = new URL(href, document.baseURI);

    if (!isUnderBaseUri(itemUrl, currentLocation.baseHref)) {
        return false;
    }

    const targetPath = getBaseRelativePath(itemUrl, currentLocation.baseHref);
    const match = item.dataset.ntNavigationRailMatch;

    if (match === 'All') {
        return currentLocation.path === targetPath;
    }

    return currentLocation.path.startsWith(targetPath)
        && (currentLocation.path.length === targetPath.length || currentLocation.path[targetPath.length] === '/');
}

function setItemSelectionClass(item: HTMLElement, selected: boolean): void {
    const wasSelected = item.classList.contains(selectedItemClass);

    if (selected) {
        item.classList.remove(deselectingItemClass);
    }

    item.classList.toggle(selectedItemClass, selected);

    if (!wasSelected || selected) {
        return;
    }

    item.classList.add(deselectingItemClass);
    const animation = item.getAnimations?.({ subtree: true })
        .find(candidate => {
            const animationName = (candidate as CSSAnimation).animationName;
            return typeof animationName === 'string' && animationName.startsWith(indicatorExitAnimationName);
        });

    if (!animation) {
        item.classList.remove(deselectingItemClass);
        return;
    }

    const finishDeselection = (): void => item.classList.remove(deselectingItemClass);
    void animation.finished.then(finishDeselection, finishDeselection);
}

function setItemSelected(item: HTMLElement, selected: boolean): void {
    setItemSelectionClass(item, selected);

    if (selected && !isDisabledNavigationElement(item)) {
        item.setAttribute('aria-current', 'page');
    } else {
        item.removeAttribute('aria-current');
    }

    const icon = item.querySelector<HTMLElement>(':scope .nt-navigation-rail-item-icon .nt-icon');
    if (icon && isMaterialSymbolIcon(icon)) {
        icon.classList.toggle('nt-nav-rail-selected-icon', selected);
    }
}

function syncItemSelection(rail: NavigationRailElement): void {
    const routeSelection = getRouteSelectionContext();

    getItems(rail).forEach(item => {
        const hasSelectionMetadata = item.hasAttribute('data-nt-navigation-rail-match')
            || item.hasAttribute('data-nt-navigation-rail-selected');

        if (!hasSelectionMetadata) {
            return;
        }

        const selected = item.dataset.ntNavigationRailSelected === 'true'
            || routeMatches(item, routeSelection);

        setItemSelected(item, selected);
    });
}

function getGroupPanel(group: HTMLElement): HTMLElement | null {
    return group.querySelector<HTMLElement>(':scope > .nt-navigation-rail-group-panel');
}

function getGroupPanelItems(panel: HTMLElement): HTMLElement[] {
    return Array.from(panel.querySelectorAll<HTMLElement>('.nt-navigation-rail-item, .nt-navigation-rail-group-trigger'));
}

function getGroupPanelSectionHeaders(panel: HTMLElement): HTMLElement[] {
    return Array.from(panel.querySelectorAll<HTMLElement>('.nt-navigation-rail-section-header'));
}

function getItemsContainer(rail: NavigationRailElement): HTMLElement | null {
    return rail.querySelector<HTMLElement>(':scope > .nt-navigation-rail-items');
}

function isDisabledNavigationElement(element: HTMLElement): boolean {
    return element.classList.contains('nt-disabled')
        || element.getAttribute('aria-disabled') === 'true'
        || element.getAttribute('tabindex') === '-1'
        || (element instanceof HTMLButtonElement && element.disabled);
}

function isInAvailablePanel(element: HTMLElement): boolean {
    const panel = element.closest<HTMLElement>('.nt-navigation-rail-group-panel');

    if (!panel) {
        return true;
    }

    return isGroupPanelAvailable(panel);
}

function isGroupPanelAvailable(panel: HTMLElement): boolean {
    if (panel.hasAttribute('popover')) {
        return isPopoverOpen(panel);
    }

    const group = panel.parentElement;
    return group?.classList.contains('nt-navigation-rail-group-open') == true;
}

function syncGroupPanelAvailability(panel: HTMLElement): void {
    const available = isGroupPanelAvailable(panel);

    panel.inert = !available;
    panel.toggleAttribute('inert', !available);
    panel.toggleAttribute('aria-hidden', !available);

    if (!available && document.activeElement instanceof HTMLElement && panel.contains(document.activeElement)) {
        const group = panel.parentElement;
        const trigger = group instanceof HTMLElement ? getGroupTrigger(group) : null;

        trigger?.focus();
    }
}

function getFocusableDestinations(rail: NavigationRailElement): HTMLElement[] {
    return getItems(rail).filter(item => !isDisabledNavigationElement(item) && isInAvailablePanel(item));
}

function getFocusableRailElements(rail: NavigationRailElement): HTMLElement[] {
    const selectors = [
        '.nt-navigation-rail-menu-button',
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    return Array.from(rail.querySelectorAll<HTMLElement>(selectors))
        .filter(element => !isDisabledNavigationElement(element) && isInAvailablePanel(element));
}

function registerInteractions(rail: NavigationRailElement): void {
    const registerButtonInteraction = window.NTComponents?.registerButtonInteraction;

    if (!registerButtonInteraction) {
        return;
    }

    const registered = new Set<Element>();
    const register = (element: Maybe<Element>): void => {
        if (!element || registered.has(element)) {
            return;
        }

        registered.add(element);
        registerButtonInteraction(element);
    };

    getMenuButtons(rail).forEach(button => register(button));
    getGroups(rail).forEach(group => {
        register(getGroupTrigger(group));
    });
    getItems(rail).forEach(item => {
        register(item);
    });
}

function getExpanded(rail: NavigationRailElement): boolean {
    return rail.classList.contains('nt-navigation-rail-expanded');
}

function getStoredExpanded(rail: NavigationRailElement): boolean | undefined {
    const stateExpanded = rail.__ntNavigationRailState?.expanded;

    if (stateExpanded !== undefined) {
        return stateExpanded;
    }

    return rail.id ? expandedStateByRailId.get(rail.id) : undefined;
}

function storeExpanded(rail: NavigationRailElement, expanded: boolean): void {
    const state = rail.__ntNavigationRailState;

    if (state) {
        state.expanded = expanded;
    }

    if (rail.id) {
        expandedStateByRailId.set(rail.id, expanded);
    }
}

function isModalRail(rail: NavigationRailElement): boolean {
    return rail.classList.contains('nt-navigation-rail-modal')
        || rail.classList.contains(responsiveModalClass)
        || rail.classList.contains('nt-navigation-rail-hide-when-collapsed');
}

function getNestedLayout(rail: NavigationRailElement): HTMLElement | null {
    const originalParent = rail.__ntNavigationRailState?.modalOriginalParent;
    if (rail.parentElement?.classList.contains('nt-layout-nested')) {
        return rail.parentElement;
    }

    return originalParent instanceof HTMLElement && originalParent.classList.contains('nt-layout-nested')
        ? originalParent
        : null;
}

function isNestedRail(rail: NavigationRailElement): boolean {
    return getNestedLayout(rail) !== null;
}

function isRailScreen(): boolean {
    return window.matchMedia?.(railScreenQuery).matches
        ?? (window.innerWidth >= mediumScreenMinWidth || (window.innerWidth >= smallScreenMinWidth && window.innerWidth > window.innerHeight));
}

function usesHiddenCollapsedModal(rail: NavigationRailElement): boolean {
    return rail.classList.contains('nt-navigation-rail-hide-on-xs')
        || rail.classList.contains('nt-navigation-rail-hide-when-collapsed');
}

function usesAlwaysHiddenCollapsedModal(rail: NavigationRailElement): boolean {
    return rail.classList.contains('nt-navigation-rail-hide-when-collapsed');
}

function usesExpandedItemLayout(rail: NavigationRailElement, expanded: boolean): boolean {
    return expanded
        || rail.classList.contains('nt-navigation-rail-modal')
        || rail.classList.contains('nt-navigation-rail-hide-when-collapsed')
        || (rail.classList.contains('nt-navigation-rail-hide-on-xs')
            && !isRailScreen());
}

function focusFirstDestination(rail: NavigationRailElement): void {
    getFocusableDestinations(rail)[0]?.focus();
}

function focusMenuButton(rail: NavigationRailElement): void {
    const button = usesHiddenCollapsedModal(rail) && !getExpanded(rail)
        ? getExternalMenuButton(rail) ?? getMenuButton(rail)
        : getMenuButton(rail) ?? getExternalMenuButton(rail);

    button?.focus();
}

function removeOutsidePointerListener(state: NavigationRailState | undefined): void {
    if (!state?.onOutsidePointerDown) {
        return;
    }

    document.removeEventListener('pointerdown', state.onOutsidePointerDown, true);
    delete state.onOutsidePointerDown;
}

function syncOutsidePointerListener(rail: NavigationRailElement, state: NavigationRailState, expanded: boolean): void {
    if (!expanded || !isModalRail(rail)) {
        removeOutsidePointerListener(state);
        return;
    }

    if (state.onOutsidePointerDown) {
        return;
    }

    state.onOutsidePointerDown = event => {
        if (event.defaultPrevented || (event.target instanceof Node && rail.contains(event.target))) {
            return;
        }

        setExpanded(rail, false, true);
        focusMenuButton(rail);
    };

    document.addEventListener('pointerdown', state.onOutsidePointerDown, true);
}

function syncResponsiveModalClass(rail: NavigationRailElement, mediumAndUp?: boolean): void {
    const isMediumAndUp = mediumAndUp ?? window.matchMedia?.(mediumScreenQuery).matches ?? window.innerWidth >= mediumScreenMinWidth;
    rail.classList.toggle(responsiveModalClass, !isMediumAndUp);
}

function addMediaQueryChangeListener(query: MediaQueryList, listener: () => void): void {
    if (typeof query.addEventListener === 'function') {
        query.addEventListener('change', listener);
        return;
    }

    query.addListener?.(listener);
}

function removeMediaQueryChangeListener(query: MediaQueryList | undefined, listener: (() => void) | undefined): void {
    if (!query || !listener) {
        return;
    }

    if (typeof query.removeEventListener === 'function') {
        query.removeEventListener('change', listener);
        return;
    }

    query.removeListener?.(listener);
}

function watchResponsiveModal(rail: NavigationRailElement, state: NavigationRailState): void {
    if (state.responsiveModalQuery) {
        syncResponsiveModalClass(rail, state.responsiveModalQuery.matches);
        return;
    }

    const query = window.matchMedia?.(mediumScreenQuery);
    if (!query) {
        syncResponsiveModalClass(rail);
        return;
    }

    state.responsiveModalQuery = query;
    state.onResponsiveModalChange = () => {
        syncResponsiveModalClass(rail, query.matches);
        syncModalState(rail, getExpanded(rail));
    };

    syncResponsiveModalClass(rail, query.matches);
    addMediaQueryChangeListener(query, state.onResponsiveModalChange);
}

function watchSmallScreen(rail: NavigationRailElement, state: NavigationRailState): void {
    if (state.smallScreenMediaQuery) {
        return;
    }

    const query = window.matchMedia?.(railScreenQuery);
    if (!query) {
        return;
    }

    state.smallScreenMediaQuery = query;
    state.onSmallScreenChange = () => {
        applyExpandedState(rail, getExpanded(rail));
        syncExpandedWidth(rail);
        syncModalState(rail, getExpanded(rail));
    };

    addMediaQueryChangeListener(query, state.onSmallScreenChange);
}

function getOrCreateModalDialog(rail: NavigationRailElement, state: NavigationRailState): HTMLDialogElement {
    const existing = getModalDialog(rail);

    if (existing) {
        state.generatedDialog = false;
        copyScopedCssAttributes(rail, existing);
        return existing;
    }

    const dialog = document.createElement('dialog');
    dialog.className = 'nt-navigation-rail-modal-dialog';
    dialog.setAttribute('aria-label', rail.getAttribute('aria-label') ?? 'Navigation');
    copyScopedCssAttributes(rail, dialog);
    rail.before(dialog);
    state.generatedDialog = true;
    return dialog;
}

function syncModalDialogStyle(rail: NavigationRailElement, dialog: HTMLDialogElement): void {
    const scrimColor = window.getComputedStyle(rail).getPropertyValue('--nt-navigation-rail-scrim-color').trim();
    const alwaysHiddenCollapsedModal = usesAlwaysHiddenCollapsedModal(rail);
    const nestedLayout = getNestedLayout(rail);
    const usesNestedBounds = nestedLayout !== null && !isRailScreen();

    dialog.classList.toggle('nt-navigation-rail-modal-dialog-hide-on-xs', rail.classList.contains('nt-navigation-rail-hide-on-xs'));
    dialog.classList.toggle('nt-navigation-rail-modal-dialog-hide-when-collapsed', alwaysHiddenCollapsedModal);
    dialog.classList.toggle('nt-navigation-rail-modal-dialog-nested', usesNestedBounds);
    dialog.style.setProperty(expandedWidthProperty, rail.style.getPropertyValue(expandedWidthProperty));

    if (scrimColor) {
        dialog.style.setProperty('--nt-navigation-rail-scrim-color', scrimColor);
    } else {
        dialog.style.removeProperty('--nt-navigation-rail-scrim-color');
    }

    if (usesNestedBounds) {
        const nestedBounds = nestedLayout.getBoundingClientRect();
        dialog.style.setProperty('--nt-navigation-rail-modal-block-start', `${nestedBounds.top}px`);
        dialog.style.setProperty('--nt-navigation-rail-modal-block-size', `${nestedBounds.height}px`);
    } else {
        dialog.style.removeProperty('--nt-navigation-rail-modal-block-start');
        dialog.style.removeProperty('--nt-navigation-rail-modal-block-size');
    }
}

function showModalDialog(dialog: HTMLDialogElement): void {
    if (dialog.open) {
        return;
    }

    try {
        if (typeof dialog.showModal === 'function') {
            dialog.showModal();
            return;
        }
    } catch {
        // Fall back to the open attribute when the browser refuses showModal.
    }

    dialog.setAttribute('open', '');
}

function closeModalDialog(dialog: HTMLDialogElement): void {
    if (!dialog.open) {
        dialog.removeAttribute('open');
        return;
    }

    try {
        if (typeof dialog.close === 'function') {
            dialog.close();
            return;
        }
    } catch {
        // Fall back to attribute removal for partial dialog implementations.
    }

    dialog.removeAttribute('open');
}

function clearDialogCloseTimeout(state: NavigationRailState | undefined): void {
    if (state?.dialogCloseTimeout === undefined) {
        return;
    }

    window.clearTimeout(state.dialogCloseTimeout);
    delete state.dialogCloseTimeout;
}

function removeModalDialogHandlers(state: NavigationRailState | undefined): void {
    const dialog = state?.modalDialog;

    if (!dialog) {
        return;
    }

    if (state?.onModalDialogCancel) {
        dialog.removeEventListener('cancel', state.onModalDialogCancel);
        delete state.onModalDialogCancel;
    }

    if (state?.onModalDialogClick) {
        dialog.removeEventListener('click', state.onModalDialogClick);
        delete state.onModalDialogClick;
    }

    if (state?.onModalDialogClose) {
        dialog.removeEventListener('close', state.onModalDialogClose);
        delete state.onModalDialogClose;
    }
}

function registerModalDialogHandlers(rail: NavigationRailElement, state: NavigationRailState, dialog: HTMLDialogElement): void {
    if (state.modalDialog === dialog && state.onModalDialogCancel && state.onModalDialogClick && state.onModalDialogClose) {
        return;
    }

    removeModalDialogHandlers(state);
    state.modalDialog = dialog;

    state.onModalDialogCancel = event => {
        event.preventDefault();
        setExpanded(rail, false, true);
        focusMenuButton(rail);
    };

    state.onModalDialogClick = event => {
        if (event.target !== dialog) {
            return;
        }

        setExpanded(rail, false, true);
        focusMenuButton(rail);
    };

    state.onModalDialogClose = () => {
        if (getExpanded(rail)) {
            setExpanded(rail, false, true);
            focusMenuButton(rail);
        }
    };

    dialog.addEventListener('cancel', state.onModalDialogCancel);
    dialog.addEventListener('click', state.onModalDialogClick);
    dialog.addEventListener('close', state.onModalDialogClose);
}

function hostRailInModalDialog(rail: NavigationRailElement, state: NavigationRailState, dialog: HTMLDialogElement): void {
    if (rail.parentElement === dialog) {
        return;
    }

    if (!state.modalPlaceholder) {
        const placeholder = document.createElement('div');
        placeholder.className = 'nt-navigation-rail-modal-placeholder';
        placeholder.classList.toggle('nt-navigation-rail-modal-placeholder-hide-on-xs', rail.classList.contains('nt-navigation-rail-hide-on-xs'));
        placeholder.classList.toggle('nt-navigation-rail-modal-placeholder-hide-when-collapsed', usesAlwaysHiddenCollapsedModal(rail));
        placeholder.setAttribute('aria-hidden', 'true');
        copyScopedCssAttributes(rail, placeholder);

        state.modalOriginalParent = dialog.parentNode ?? rail.parentNode;
        state.modalOriginalNextSibling = rail.nextSibling;
        dialog.before(placeholder);
        state.modalPlaceholder = placeholder;
    }

    dialog.append(rail);
}

function startModalDialogEnter(dialog: HTMLDialogElement, state: NavigationRailState): void {
    if (dialog.open && !dialog.classList.contains('nt-navigation-rail-modal-dialog-exiting')) {
        return;
    }

    clearDialogCloseTimeout(state);
    dialog.classList.remove('nt-navigation-rail-modal-dialog-exiting');
    dialog.classList.add('nt-navigation-rail-modal-dialog-entering');

    state.dialogCloseTimeout = window.setTimeout(() => {
        dialog.classList.remove('nt-navigation-rail-modal-dialog-entering');
        delete state.dialogCloseTimeout;
    }, navigationRailTransitionDuration);
}

function restoreRailFromModalDialog(rail: NavigationRailElement, state: NavigationRailState | undefined): void {
    if (!state?.modalDialog || rail.parentElement !== state.modalDialog) {
        state?.modalPlaceholder?.remove();
        if (state) {
            state.modalPlaceholder = null;
            state.modalOriginalParent = null;
            state.modalOriginalNextSibling = null;
        }
        return;
    }

    const parent = state.modalOriginalParent;
    const nextSibling = state.modalOriginalNextSibling;

    if (parent) {
        if (nextSibling && nextSibling.parentNode === parent) {
            parent.insertBefore(rail, nextSibling);
        } else {
            parent.appendChild(rail);
        }
    }

    state.modalPlaceholder?.remove();
    state.modalPlaceholder = null;
    state.modalOriginalParent = null;
    state.modalOriginalNextSibling = null;
}

function getElementsToHideForModal(rail: NavigationRailElement, dialog: HTMLDialogElement): HTMLElement[] {
    const elements: HTMLElement[] = [];
    const seen = new Set<HTMLElement>();
    let current: HTMLElement | null = rail;

    while (current && current !== document.body) {
        const parent: HTMLElement | null = current.parentElement;
        if (!parent) {
            break;
        }

        Array.from(parent.children).forEach(child => {
            if (!(child instanceof HTMLElement) || child === current || child === dialog || child.contains(rail) || seen.has(child)) {
                return;
            }

            seen.add(child);
            elements.push(child);
        });

        current = parent;
    }

    return elements;
}

function hideBackgroundForModal(rail: NavigationRailElement, state: NavigationRailState, dialog: HTMLDialogElement): void {
    if (state.modalHiddenElements) {
        return;
    }

    state.modalHiddenElements = getElementsToHideForModal(rail, dialog).map(element => {
        const hiddenElement: ModalHiddenElement = {
            ariaHidden: element.getAttribute('aria-hidden'),
            element,
            inert: element.inert
        };

        element.inert = true;
        element.setAttribute('aria-hidden', 'true');
        return hiddenElement;
    });
}

function restoreBackgroundForModal(state: NavigationRailState | undefined): void {
    state?.modalHiddenElements?.forEach(({ ariaHidden, element, inert }) => {
        element.inert = inert;

        if (ariaHidden === null) {
            element.removeAttribute('aria-hidden');
        } else {
            element.setAttribute('aria-hidden', ariaHidden);
        }
    });

    if (state) {
        delete state.modalHiddenElements;
    }
}

function syncModalState(rail: NavigationRailElement, expanded: boolean, focusOnOpen = false): void {
    const state = rail.__ntNavigationRailState;

    if (!state || !isModalRail(rail)) {
        hideModalDialog(rail, state);
        removeOutsidePointerListener(state);
        return;
    }

    if (!expanded) {
        hideModalDialog(rail, state, rail.classList.contains('nt-navigation-rail-collapsing'));
        removeOutsidePointerListener(state);

        return;
    }

    if (isNestedRail(rail) && isRailScreen()) {
        hideModalDialog(rail, state);
        syncOutsidePointerListener(rail, state, expanded);

        if (focusOnOpen) {
            focusFirstDestination(rail);
        }

        return;
    }

    const dialog = getOrCreateModalDialog(rail, state);
    const shouldAnimateEnter = !dialog.open || dialog.classList.contains('nt-navigation-rail-modal-dialog-exiting');

    syncModalDialogStyle(rail, dialog);
    registerModalDialogHandlers(rail, state, dialog);
    hostRailInModalDialog(rail, state, dialog);

    if (shouldAnimateEnter) {
        startModalDialogEnter(dialog, state);
    }

    showModalDialog(dialog);
    hideBackgroundForModal(rail, state, dialog);
    syncOutsidePointerListener(rail, state, expanded);

    if (focusOnOpen) {
        focusFirstDestination(rail);
    }
}

function hideModalDialog(rail: NavigationRailElement, state: NavigationRailState | undefined, animate = false): void {
    if (!state?.modalDialog) {
        restoreBackgroundForModal(state);
        return;
    }

    if (animate && state.modalDialog.open) {
        state.modalDialog.classList.remove('nt-navigation-rail-modal-dialog-entering');
        state.modalDialog.classList.add('nt-navigation-rail-modal-dialog-exiting');
        state.restoreFocusAfterModalClose = true;
        clearDialogCloseTimeout(state);
        state.dialogCloseTimeout = window.setTimeout(() => {
            hideModalDialog(rail, state);
        }, navigationRailTransitionDuration);
        return;
    }

    clearDialogCloseTimeout(state);
    state.modalDialog.classList.remove('nt-navigation-rail-modal-dialog-entering', 'nt-navigation-rail-modal-dialog-exiting');
    closeModalDialog(state.modalDialog);
    restoreRailFromModalDialog(rail, state);
    restoreBackgroundForModal(state);

    if (state.restoreFocusAfterModalClose) {
        window.setTimeout(() => focusMenuButton(rail), 0);
        delete state.restoreFocusAfterModalClose;
    }

    if (state.generatedDialog) {
        removeModalDialogHandlers(state);
        state.modalDialog.remove();
        state.modalDialog = null;
        state.generatedDialog = false;
    }
}

function clearTransitionState(rail: NavigationRailElement): void {
    const state = rail.__ntNavigationRailState;

    if (state?.transitionTimeout !== undefined) {
        window.clearTimeout(state.transitionTimeout);
        delete state.transitionTimeout;
    }

    rail.classList.remove('nt-navigation-rail-expanding', 'nt-navigation-rail-collapsing');
}

function applyExpandedState(rail: NavigationRailElement, expanded: boolean): void {
    syncItemSelection(rail);
    rail.classList.toggle('nt-navigation-rail-expanded', expanded);
    rail.classList.toggle('nt-navigation-rail-collapsed', !expanded);

    const fullIndicator = rail.classList.contains('nt-navigation-rail-indicator-full');
    const expandedItemLayout = usesExpandedItemLayout(rail, expanded);

    getItems(rail).forEach(item => {
        item.classList.toggle('nt-navigation-rail-item-expanded', expandedItemLayout);
        item.classList.toggle('nt-navigation-rail-item-indicator-full', fullIndicator);
    });

    getSectionHeaders(rail).forEach(header => {
        header.classList.toggle('nt-navigation-rail-section-header-expanded', expandedItemLayout);
    });

    getGroups(rail).forEach(group => {
        applyGroupRailState(rail, group, expanded);
    });

    if (expanded) {
        enforceTopLevelGroupLimit(rail);
    }

    syncGroupSelection(rail);
}

// Material 3 sizes the expanded rail to its widest destination or header, within the expanded bounds. A hidden copy is measured at
// max-content so the real rail keeps its current width and can still transition to the measured value. The measuring class in the
// stylesheet applies the bounds, so the maximum accepts any CSS length. Closed inline groups are collapsed by height only, so their
// destinations count and opening a group does not resize the rail.
function syncExpandedWidth(rail: NavigationRailElement): void {
    if (!rail.isConnected || !getExpanded(rail)) {
        return;
    }

    const probe = rail.cloneNode(true) as HTMLElement;

    probe.querySelectorAll('nt-page-script').forEach(element => element.remove());
    probe.querySelectorAll('[id]').forEach(element => element.removeAttribute('id'));
    probe.removeAttribute('id');
    probe.setAttribute('aria-hidden', 'true');
    probe.inert = true;
    probe.classList.add(measuringClass);
    rail.after(probe);

    const width = Math.ceil(probe.getBoundingClientRect().width);
    probe.remove();

    // A rail under an unrendered ancestor measures 0. Keep the previous width, or the CSS minimum, so it is not 0px wide once shown.
    if (width > 0) {
        rail.style.setProperty(expandedWidthProperty, `${width}px`);
        rail.__ntNavigationRailState?.modalDialog?.style.setProperty(expandedWidthProperty, `${width}px`);
    }
}

function animateExpandedState(rail: NavigationRailElement, expanded: boolean): void {
    const state = rail.__ntNavigationRailState;

    clearTransitionState(rail);
    rail.classList.add(expanded ? 'nt-navigation-rail-expanding' : 'nt-navigation-rail-collapsing');
    applyExpandedState(rail, expanded);

    if (!state) {
        return;
    }

    state.transitionTimeout = window.setTimeout(() => {
        clearTransitionState(rail);
    }, navigationRailTransitionDuration);
}

function setExpanded(rail: NavigationRailElement, expanded: boolean, animate = false, focusOnModalOpen = false): void {
    const buttons = getMenuButtons(rail);

    storeExpanded(rail, expanded);

    if (animate && expanded !== getExpanded(rail)) {
        animateExpandedState(rail, expanded);
    } else {
        if (!animate) {
            clearTransitionState(rail);
        }

        applyExpandedState(rail, expanded);
    }

    syncExpandedWidth(rail);

    buttons.forEach(button => {
        const expandedLabel = button.dataset.ntNavigationRailExpandedLabel;
        const collapsedLabel = button.dataset.ntNavigationRailCollapsedLabel;
        const expandedIcon = button.dataset.ntNavigationRailExpandedIcon;
        const collapsedIcon = button.dataset.ntNavigationRailCollapsedIcon;
        const icon = getMenuIcon(button);

        button.setAttribute('aria-expanded', expanded ? 'true' : 'false');

        if (expanded && expandedLabel) {
            button.setAttribute('aria-label', expandedLabel);
        } else if (!expanded && collapsedLabel) {
            button.setAttribute('aria-label', collapsedLabel);
        }

        if (icon && expanded && expandedIcon) {
            icon.textContent = expandedIcon;
        } else if (icon && !expanded && collapsedIcon) {
            icon.textContent = collapsedIcon;
        }
    });

    syncModalState(rail, expanded, focusOnModalOpen);
}

function setGroupOpen(group: HTMLElement, trigger: HTMLButtonElement, open: boolean): void {
    group.classList.toggle('nt-navigation-rail-group-open', open);
    trigger.setAttribute('aria-expanded', open ? 'true' : 'false');

    const panel = getGroupPanel(group);
    if (panel) {
        syncGroupPanelAvailability(panel);
    }
}

function getGroupDefaultOpen(group: HTMLElement): boolean {
    return group.dataset.ntNavigationRailGroupExpanded === 'true';
}

function getStoredGroupOpen(group: HTMLElement): boolean {
    const storedOpen = group.dataset.ntNavigationRailGroupOpen;
    return storedOpen === undefined ? getGroupDefaultOpen(group) : storedOpen === 'true';
}

function storeGroupOpen(group: HTMLElement, open: boolean): void {
    group.dataset.ntNavigationRailGroupOpen = open ? 'true' : 'false';
}

function setInlineGroupOpen(group: HTMLElement, trigger: HTMLButtonElement, open: boolean): void {
    storeGroupOpen(group, open);
    setGroupOpen(group, trigger, open);
}

function setGroupDescendantsExpanded(panel: HTMLElement, expanded: boolean): void {
    getGroupPanelItems(panel).forEach(item => {
        item.classList.toggle('nt-navigation-rail-item-expanded', expanded);
    });

    getGroupPanelSectionHeaders(panel).forEach(header => {
        header.classList.toggle('nt-navigation-rail-section-header-expanded', expanded);
    });
}

function isPopoverOpen(panel: HTMLElement): boolean {
    try {
        return panel.matches(':popover-open');
    } catch {
        return false;
    }
}

function escapeCssIdentifier(value: string): string {
    return window.CSS?.escape?.(value) ?? value.replace(/["\\]/g, '\\$&');
}

function closeGroupPopover(panel: HTMLElement): void {
    if (isPopoverOpen(panel)) {
        panel.hidePopover?.();
    }

    syncGroupPanelAvailability(panel);
}

function closeOnNavigation(rail: NavigationRailElement, event: MouseEvent): void {
    if (!rail.hasAttribute('data-nt-close-on-navigation') || !(event.target instanceof Element)) {
        return;
    }

    const item = event.target.closest<HTMLElement>('a.nt-navigation-rail-item[href]');
    if (!item || !rail.contains(item) || isDisabledNavigationElement(item)) {
        return;
    }

    getGroups(rail)
        .map(group => ({ panel: getGroupPanel(group), trigger: getGroupTrigger(group) }))
        .filter((entry): entry is { panel: HTMLElement; trigger: HTMLButtonElement | null } => entry.panel !== null && entry.panel.contains(item) && isPopoverOpen(entry.panel))
        .reverse()
        .forEach(({ panel, trigger }) => {
            closeGroupPopover(panel);
            trigger?.setAttribute('aria-expanded', 'false');
        });

    if (isModalRail(rail) && getExpanded(rail)) {
        setExpanded(rail, false, true);
    }
}

function closeOtherTopLevelGroups(rail: NavigationRailElement, currentGroup: HTMLElement): void {
    if (rail.dataset.ntNavigationRailLimitToOneExpanded !== 'true' || !isTopLevelGroup(rail, currentGroup)) {
        return;
    }

    getTopLevelGroups(rail).forEach(group => {
        if (group === currentGroup) {
            return;
        }

        const trigger = getGroupTrigger(group);
        const panel = getGroupPanel(group);

        if (!trigger || !panel) {
            return;
        }

        closeGroupPopover(panel);

        if (getExpanded(rail)) {
            setInlineGroupOpen(group, trigger, false);
        } else {
            trigger.setAttribute('aria-expanded', 'false');
        }
    });
}

function enforceTopLevelGroupLimit(rail: NavigationRailElement): void {
    if (rail.dataset.ntNavigationRailLimitToOneExpanded !== 'true') {
        return;
    }

    const openGroups = getTopLevelGroups(rail).filter(group => group.classList.contains('nt-navigation-rail-group-open'));
    if (openGroups.length > 1) {
        closeOtherTopLevelGroups(rail, openGroups[0]);
    }
}

function closeTopmostGroupPopover(rail: NavigationRailElement): boolean {
    const openPanels = getGroups(rail)
        .map(group => getGroupPanel(group))
        .filter((panel): panel is HTMLElement => panel !== null && isPopoverOpen(panel));
    const panel = openPanels.at(-1);

    if (!panel) {
        return false;
    }

    const trigger = panel.id
        ? rail.querySelector<HTMLButtonElement>(`.nt-navigation-rail-group-trigger[aria-controls="${escapeCssIdentifier(panel.id)}"]`)
        : null;

    closeGroupPopover(panel);
    trigger?.setAttribute('aria-expanded', 'false');
    trigger?.focus();
    return true;
}

function moveDestinationFocus(rail: NavigationRailElement, event: KeyboardEvent): boolean {
    const expanded = getExpanded(rail);
    const key = event.key;
    const moveNext = key === 'ArrowDown' || (expanded && key === 'ArrowRight');
    const movePrevious = key === 'ArrowUp' || (expanded && key === 'ArrowLeft');
    const moveFirst = key === 'Home';
    const moveLast = key === 'End';

    if (!moveNext && !movePrevious && !moveFirst && !moveLast) {
        return false;
    }

    const destinations = getFocusableDestinations(rail);
    if (destinations.length === 0) {
        return false;
    }

    const active = document.activeElement;
    const currentIndex = active instanceof HTMLElement ? destinations.indexOf(active) : -1;
    let nextIndex = currentIndex;

    if (moveFirst) {
        nextIndex = 0;
    } else if (moveLast) {
        nextIndex = destinations.length - 1;
    } else if (moveNext) {
        nextIndex = currentIndex < 0 ? 0 : Math.min(destinations.length - 1, currentIndex + 1);
    } else if (movePrevious) {
        if (currentIndex <= 0) {
            return false;
        }

        nextIndex = currentIndex - 1;
    }

    if (nextIndex < 0 || nextIndex === currentIndex) {
        return false;
    }

    event.preventDefault();
    destinations[nextIndex]?.focus();
    return true;
}

function trapModalFocus(rail: NavigationRailElement, event: KeyboardEvent): boolean {
    if (!isModalRail(rail) || !getExpanded(rail) || event.key !== 'Tab') {
        return false;
    }

    const focusable = getFocusableRailElements(rail);
    if (focusable.length === 0) {
        return false;
    }

    const first = focusable[0];
    const last = focusable.at(-1);
    const active = document.activeElement;

    if (event.shiftKey && active === first) {
        event.preventDefault();
        last?.focus();
        return true;
    }

    if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
        return true;
    }

    return false;
}

function handleRailKeyDown(rail: NavigationRailElement, event: KeyboardEvent): void {
    if (event.defaultPrevented) {
        return;
    }

    if (event.key === 'Escape') {
        if (closeTopmostGroupPopover(rail)) {
            event.preventDefault();
            return;
        }

        if (isModalRail(rail) && getExpanded(rail)) {
            event.preventDefault();
            setExpanded(rail, false, true);
            focusMenuButton(rail);
        }

        return;
    }

    if (trapModalFocus(rail, event)) {
        return;
    }

    moveDestinationFocus(rail, event);
}

function runAfterNextPaint(callback: () => void): void {
    if (!window.requestAnimationFrame) {
        window.setTimeout(callback, 0);
        return;
    }

    window.requestAnimationFrame(() => {
        window.requestAnimationFrame(callback);
    });
}

function suppressPanelConversionFlash(panel: HTMLElement): void {
    panel.classList.add('nt-navigation-rail-group-panel-converting');

    runAfterNextPaint(() => {
        panel.classList.remove('nt-navigation-rail-group-panel-converting');
    });
}

function applyGroupRailState(rail: NavigationRailElement, group: HTMLElement, expanded: boolean): void {
    const trigger = getGroupTrigger(group);
    const panel = getGroupPanel(group);

    if (!trigger || !panel) {
        return;
    }

    const isInPopoverPanel = group.parentElement?.closest('.nt-navigation-rail-group-panel') !== null;
    const expandedItemLayout = usesExpandedItemLayout(rail, expanded);

    group.classList.toggle('nt-navigation-rail-group-rail-expanded', expanded);
    trigger.classList.toggle('nt-navigation-rail-item-expanded', expandedItemLayout || isInPopoverPanel);

    if (expanded) {
        closeGroupPopover(panel);
        panel.removeAttribute('popover');
        setGroupOpen(group, trigger, getStoredGroupOpen(group));
        setGroupDescendantsExpanded(panel, true);
        return;
    }

    const convertingInlinePanel = !panel.hasAttribute('popover');
    if (group.classList.contains('nt-navigation-rail-group-open')) {
        storeGroupOpen(group, true);
    }

    if (convertingInlinePanel) {
        suppressPanelConversionFlash(panel);
    }

    setGroupOpen(group, trigger, false);
    panel.setAttribute('popover', 'auto');
    setGroupDescendantsExpanded(panel, true);
}

function syncGroupSelection(rail: NavigationRailElement): void {
    getGroups(rail).reverse().forEach(group => {
        const trigger = getGroupTrigger(group);
        const panel = getGroupPanel(group);

        if (!trigger || !panel) {
            return;
        }

        const selected = panel.querySelector('.nt-navigation-rail-item-selected, .nt-navigation-rail-group-selected') !== null;
        const icon = getGroupTriggerIcon(trigger);

        group.classList.toggle('nt-navigation-rail-group-selected', selected);
        setItemSelectionClass(trigger, selected);

        if (icon && isMaterialSymbolIcon(icon)) {
            icon.classList.toggle('nt-nav-rail-selected-icon', selected);
        }
    });
}

function positionGroupPopover(trigger: HTMLButtonElement, panel: HTMLElement): void {
    const triggerRect = trigger.getBoundingClientRect();
    const panelRect = panel.getBoundingClientRect();
    const edgeMargin = 8;
    const gap = 4;
    const panelWidth = panelRect.width || panel.offsetWidth || panel.scrollWidth;
    const panelHeight = panelRect.height || panel.offsetHeight || panel.scrollHeight;
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const openToLeft = triggerRect.right + gap + panelWidth > viewportWidth - edgeMargin;
    const openUpward = triggerRect.top + panelHeight > viewportHeight - edgeMargin;
    const left = openToLeft
        ? Math.max(edgeMargin, triggerRect.left - gap - panelWidth)
        : Math.min(viewportWidth - edgeMargin - panelWidth, triggerRect.right + gap);
    const maxTop = Math.max(edgeMargin, viewportHeight - edgeMargin - panelHeight);
    const preferredTop = openUpward
        ? triggerRect.bottom - panelHeight
        : triggerRect.top;
    const top = Math.min(Math.max(edgeMargin, preferredTop), maxTop);

    panel.style.left = `${Math.max(edgeMargin, left)}px`;
    panel.style.top = `${top}px`;
    panel.style.right = 'auto';
    panel.style.bottom = 'auto';
    panel.style.transformOrigin = `${openUpward ? 'bottom' : 'top'} ${openToLeft ? 'right' : 'left'}`;
    panel.classList.toggle('nt-navigation-rail-group-panel-open-upward', openUpward);
}

function openGroupPopover(trigger: HTMLButtonElement, panel: HTMLElement): void {
    panel.setAttribute('popover', 'auto');
    positionGroupPopover(trigger, panel);
    panel.showPopover?.();
    positionGroupPopover(trigger, panel);
    setGroupDescendantsExpanded(panel, true);
    trigger.setAttribute('aria-expanded', 'true');
    syncGroupPanelAvailability(panel);
}

function syncOpenGroupPopoverPositions(state: NavigationRailState | undefined): void {
    state?.groupHandlers.forEach(({ panel, trigger }) => {
        if (isPopoverOpen(panel)) {
            positionGroupPopover(trigger, panel);
        }
    });
}

function cancelPopoverPositionFrame(state: NavigationRailState | undefined): void {
    if (state?.popoverPositionFrame === undefined) {
        return;
    }

    if (window.cancelAnimationFrame) {
        window.cancelAnimationFrame(state.popoverPositionFrame);
    } else {
        window.clearTimeout(state.popoverPositionFrame);
    }

    delete state.popoverPositionFrame;
}

function requestOpenGroupPopoverPositionSync(state: NavigationRailState): void {
    if (state.popoverPositionFrame !== undefined) {
        return;
    }

    const callback = (): void => {
        delete state.popoverPositionFrame;
        syncOpenGroupPopoverPositions(state);
    };

    state.popoverPositionFrame = window.requestAnimationFrame
        ? window.requestAnimationFrame(callback)
        : window.setTimeout(callback, 0);
}

function registerRailScrollHandler(rail: NavigationRailElement, state: NavigationRailState): void {
    const scrollContainer = getItemsContainer(rail);

    if (state.onRailScroll) {
        if (state.scrollContainer !== scrollContainer) {
            state.scrollContainer?.removeEventListener('scroll', state.onRailScroll);
            scrollContainer?.addEventListener('scroll', state.onRailScroll, { passive: true });
            state.scrollContainer = scrollContainer;
        }

        return;
    }

    state.onRailScroll = () => requestOpenGroupPopoverPositionSync(state);
    rail.addEventListener('scroll', state.onRailScroll, { passive: true });
    scrollContainer?.addEventListener('scroll', state.onRailScroll, { passive: true });
    state.scrollContainer = scrollContainer;
}

function removeRailScrollHandler(rail: NavigationRailElement, state: NavigationRailState | undefined): void {
    if (!state?.onRailScroll) {
        return;
    }

    rail.removeEventListener('scroll', state.onRailScroll);
    state.scrollContainer?.removeEventListener('scroll', state.onRailScroll);
    state.scrollContainer = null;
    delete state.onRailScroll;
    cancelPopoverPositionFrame(state);
}

function removeExternalButtonHandler(state: NavigationRailState | undefined): void {
    if (!state?.externalButton || !state.onExternalClick) {
        return;
    }

    state.externalButton.removeEventListener('click', state.onExternalClick);
    state.externalButton = null;
    delete state.onExternalClick;
}

function bindExternalButton(rail: NavigationRailElement, state: NavigationRailState, externalButton: HTMLButtonElement | null): void {
    if (state.externalButton === externalButton) {
        return;
    }

    removeExternalButtonHandler(state);
    state.externalButton = externalButton;

    if (!externalButton) {
        return;
    }

    state.onExternalClick = (): void => {
        setExpanded(rail, !getExpanded(rail), true, true);
    };

    externalButton.addEventListener('click', state.onExternalClick);
}

function registerNavigationClickHandler(rail: NavigationRailElement, state: NavigationRailState): void {
    if (state.onNavigationClick) {
        return;
    }

    state.onNavigationClick = event => closeOnNavigation(rail, event);
    rail.addEventListener('click', state.onNavigationClick);
}

function removeNavigationClickHandler(rail: NavigationRailElement, state: NavigationRailState | undefined): void {
    if (!state?.onNavigationClick) {
        return;
    }

    rail.removeEventListener('click', state.onNavigationClick);
    delete state.onNavigationClick;
}

function removeGroupHandlers(state: NavigationRailState | undefined): void {
    state?.groupHandlers.forEach(handler => {
        handler.trigger.removeEventListener('click', handler.onClick);
        handler.panel.removeEventListener('toggle', handler.onToggle);
    });

    if (state) {
        state.groupHandlers = [];
    }
}

function groupHandlersMatch(state: NavigationRailState, groups: HTMLElement[]): boolean {
    if (state.groupHandlers.length !== groups.length) {
        return false;
    }

    return groups.every((group, index) => {
        const handler = state.groupHandlers[index];

        return handler?.group === group
            && handler.trigger === getGroupTrigger(group)
            && handler.panel === getGroupPanel(group);
    });
}

function registerGroups(rail: NavigationRailElement, state: NavigationRailState): void {
    const groups = getGroups(rail);
    if (groupHandlersMatch(state, groups)) {
        applyExpandedState(rail, getExpanded(rail));
        return;
    }

    removeGroupHandlers(state);

    groups.forEach(group => {
        const trigger = getGroupTrigger(group);
        const panel = getGroupPanel(group);

        if (!trigger || !panel) {
            return;
        }

        const onClick = (event: MouseEvent): void => {
            if (trigger.disabled || trigger.classList.contains('nt-disabled')) {
                return;
            }

            event.preventDefault();

            if (getExpanded(rail)) {
                const opening = !group.classList.contains('nt-navigation-rail-group-open');
                closeGroupPopover(panel);
                if (opening) {
                    closeOtherTopLevelGroups(rail, group);
                }

                setInlineGroupOpen(group, trigger, opening);
                setGroupDescendantsExpanded(panel, true);
                return;
            }

            if (isPopoverOpen(panel)) {
                closeGroupPopover(panel);
                trigger.setAttribute('aria-expanded', 'false');
            } else {
                closeOtherTopLevelGroups(rail, group);
                openGroupPopover(trigger, panel);
            }
        };

        const onToggle = (): void => {
            if (getExpanded(rail)) {
                return;
            }

            const open = isPopoverOpen(panel);
            trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
            setGroupDescendantsExpanded(panel, true);
            syncGroupPanelAvailability(panel);
        };

        trigger.addEventListener('click', onClick);
        panel.addEventListener('toggle', onToggle);
        state.groupHandlers.push({ group, panel, trigger, onClick, onToggle });
    });

    applyExpandedState(rail, getExpanded(rail));
}

function updateRail(rail: NavigationRailElement): void {
    const button = getMenuButton(rail);
    const externalButton = getExternalMenuButton(rail);
    const existingState = rail.__ntNavigationRailState;

    if (existingState?.button === button) {
        if (!existingState.onKeyDown) {
            existingState.onKeyDown = event => handleRailKeyDown(rail, event);
            rail.addEventListener('keydown', existingState.onKeyDown);
        }

        bindExternalButton(rail, existingState, externalButton);
        registerNavigationClickHandler(rail, existingState);
        watchResponsiveModal(rail, existingState);
        watchSmallScreen(rail, existingState);
        registerRailScrollHandler(rail, existingState);
        setExpanded(rail, getStoredExpanded(rail) ?? getExpanded(rail));
        registerInteractions(rail);
        registerGroups(rail, existingState);
        return;
    }

    if (existingState?.button && existingState.onClick) {
        existingState.button.removeEventListener('click', existingState.onClick);
    }
    removeExternalButtonHandler(existingState);
    removeNavigationClickHandler(rail, existingState);
    if (existingState?.onKeyDown) {
        rail.removeEventListener('keydown', existingState.onKeyDown);
    }
    restoreBackgroundForModal(existingState);
    hideModalDialog(rail, existingState);
    removeModalDialogHandlers(existingState);
    removeMediaQueryChangeListener(existingState?.responsiveModalQuery, existingState?.onResponsiveModalChange);
    removeMediaQueryChangeListener(existingState?.smallScreenMediaQuery, existingState?.onSmallScreenChange);
    removeGroupHandlers(existingState);

    const state: NavigationRailState = { button, externalButton: null, groupHandlers: [] };
    state.onKeyDown = event => handleRailKeyDown(rail, event);
    rail.addEventListener('keydown', state.onKeyDown);

    if (button) {
        const onClick = (): void => {
            setExpanded(rail, !getExpanded(rail), true, true);
        };

        button.addEventListener('click', onClick);
        state.onClick = onClick;
    }

    bindExternalButton(rail, state, externalButton);
    registerNavigationClickHandler(rail, state);
    rail.__ntNavigationRailState = state;
    watchResponsiveModal(rail, state);
    watchSmallScreen(rail, state);
    registerRailScrollHandler(rail, state);
    registerInteractions(rail);
    registerGroups(rail, state);
    const defaultExpanded = shouldOpenByDefault(rail, state);
    const initialExpanded = rail.dataset.ntNavigationRailOpenByDefault === 'true'
        ? defaultExpanded
        : getExpanded(rail);
    setExpanded(rail, getStoredExpanded(rail) ?? initialExpanded);

    // Label widths change once web fonts load, so remeasure without rerunning the update, which would cancel a running transition.
    document.fonts?.ready.then(() => {
        if (rail.__ntNavigationRailState === state) {
            syncExpandedWidth(rail);
        }
    });
}

function disposeRail(rail: Maybe<NavigationRailElement>): void {
    if (!rail) {
        return;
    }

    const state = rail.__ntNavigationRailState;
    if (state?.button && state.onClick) {
        state.button.removeEventListener('click', state.onClick);
    }
    removeExternalButtonHandler(state);
    removeNavigationClickHandler(rail, state);
    if (state?.onKeyDown) {
        rail.removeEventListener('keydown', state.onKeyDown);
    }
    removeOutsidePointerListener(state);
    removeRailScrollHandler(rail, state);
    hideModalDialog(rail, state);
    removeModalDialogHandlers(state);
    restoreBackgroundForModal(state);
    removeMediaQueryChangeListener(state?.responsiveModalQuery, state?.onResponsiveModalChange);
    removeMediaQueryChangeListener(state?.smallScreenMediaQuery, state?.onSmallScreenChange);
    removeGroupHandlers(state);
    if (state?.transitionTimeout !== undefined) {
        window.clearTimeout(state.transitionTimeout);
    }
    clearDialogCloseTimeout(state);
    rail.classList.remove(responsiveModalClass);
    delete rail.__ntNavigationRailState;
}

export function onLoad(rail?: Maybe<NavigationRailElement>): void {
    getTargetRails(rail).forEach(updateRail);
}

export function onUpdate(rail?: Maybe<NavigationRailElement>): void {
    getTargetRails(rail).forEach(updateRail);
}

export function onDispose(rail?: Maybe<Element>): void {
    getTargetRailsForDispose(rail).forEach(disposeRail);
}
