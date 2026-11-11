type Maybe<T> = T | null | undefined;
export type SnackbarActionCallback = () => boolean | void | Promise<boolean | void>;
export type SnackbarInput = string | SnackbarOptions;

interface NTSnackbarHostElement extends HTMLElement {
    __ntSnackbarPortalAnchor?: Comment;
    __ntSnackbarState?: NTSnackbarHostState;
}

interface NTSnackbarPageScriptElement extends HTMLElement {
    __ntSnackbarHost?: NTSnackbarHostElement;
}

interface NTSnackbarHostState {
    activeElement: HTMLElement | null;
    activeSnackbar: SnackbarRecord | null;
    closeAnimationTimeout: number | null;
    closeTimeout: number | null;
    isClosing: boolean;
    queue: SnackbarRecord[];
}

interface SnackbarActionRecord {
    callback: SnackbarActionCallback | null;
    index: number;
    label: string;
}

interface SnackbarRecord {
    actionColor: string | null;
    actions: SnackbarActionRecord[];
    backgroundColor: string | null;
    dotNetActionMethod: string | null;
    dotNetCloseMethod: string | null;
    dotNetReference: DotNetSnackbarReference | null;
    host: HTMLElement | string | null;
    id: string;
    isActionInProgress: boolean;
    message: string;
    showClose: boolean;
    textColor: string | null;
    timeout: number;
}

export interface SnackbarActionOptions {
    actionCallback?: SnackbarActionCallback | null;
    label: string;
}

export interface SnackbarOptions {
    actionCallback?: SnackbarActionCallback | null;
    actionLabel?: string | null;
    actions?: SnackbarActionOptions[] | null;
    actionColor?: string | null;
    backgroundColor?: string | null;
    dotNetActionMethod?: string | null;
    dotNetCloseMethod?: string | null;
    dotNetReference?: DotNetSnackbarReference | null;
    host?: HTMLElement | string | null;
    id?: string | null;
    message: string;
    showClose?: boolean | null;
    textColor?: string | null;
    timeout?: number | null;
}

export interface NTSnackbarBridge {
    addSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string;
    clearSnackbars(host?: HTMLElement | string | null): void;
    closeSnackbar(id?: string, host?: HTMLElement | string | null): boolean;
    closeSnackbarFromBlazor(id: string, host?: HTMLElement | string | null): boolean;
    queueSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string;
    queueSnackbarFromBlazor(id: string, message: string, actionLabels: string[] | string | null, timeout: number, showClose: boolean, backgroundColor: string, textColor: string, actionColor: string, dotNetReference: DotNetSnackbarReference, dotNetActionMethod: string, dotNetCloseMethod: string): string;
}

export interface DotNetSnackbarReference {
    invokeMethodAsync<T>(methodName: string, ...args: unknown[]): Promise<T>;
}

declare global {
    interface Window {
        __ntSnackbarPendingQueue?: SnackbarOptions[];
        NTSnackbar?: NTSnackbarBridge;
    }
}

const snackbarHostSelector = '[data-nt-snackbar-host="true"]';
const closeDelayMilliseconds = 200;
const defaultActionColor = 'var(--nt-color-inverse-primary)';
const defaultBackgroundColor = 'var(--nt-color-inverse-surface)';
const defaultTextColor = 'var(--nt-color-inverse-on-surface)';
const maxPendingSnackbars = 50;
const maxQueuedSnackbars = 50;
const pendingSnackbars: SnackbarRecord[] = [];
const hostStates = new WeakMap<NTSnackbarHostElement, NTSnackbarHostState>();
const registeredHosts = new Set<NTSnackbarHostElement>();
let dialogLayerObserver: MutationObserver | null = null;
let defaultHost: NTSnackbarHostElement | null = null;
let foregroundDialog: HTMLDialogElement | null = null;
let nextSnackbarId = 0;

function getHosts(root: Maybe<Element | Document>): NTSnackbarHostElement[] {
    const scope = root ?? document;
    if (scope instanceof HTMLElement && scope.localName === 'nt-page-script') {
        const pageScript = scope as NTSnackbarPageScriptElement;
        if (pageScript.__ntSnackbarHost) {
            return [pageScript.__ntSnackbarHost];
        }

        const sibling = scope.previousElementSibling;
        if (sibling instanceof HTMLElement && sibling.matches(snackbarHostSelector)) {
            pageScript.__ntSnackbarHost = sibling as NTSnackbarHostElement;
            return [pageScript.__ntSnackbarHost];
        }

        return [];
    }

    const hosts = scope instanceof HTMLElement && scope.matches(snackbarHostSelector) ? [scope as NTSnackbarHostElement] : [];
    hosts.push(...Array.from(scope.querySelectorAll<NTSnackbarHostElement>(snackbarHostSelector)));
    return hosts;
}

function getHost(host?: HTMLElement | string | null): NTSnackbarHostElement | null {
    if (host instanceof HTMLElement) {
        return host.matches(snackbarHostSelector) ? host as NTSnackbarHostElement : null;
    }

    if (typeof host === 'string' && host.length > 0) {
        const element = document.getElementById(host);
        return element instanceof HTMLElement && element.matches(snackbarHostSelector) ? element as NTSnackbarHostElement : null;
    }

    if (defaultHost?.isConnected) {
        return defaultHost;
    }

    defaultHost = document.querySelector<NTSnackbarHostElement>(snackbarHostSelector);
    return defaultHost;
}

function getOrCreateState(host: NTSnackbarHostElement): NTSnackbarHostState {
    const existingState = hostStates.get(host);
    if (existingState) {
        return existingState;
    }

    const state: NTSnackbarHostState = {
        activeElement: null,
        activeSnackbar: null,
        closeAnimationTimeout: null,
        closeTimeout: null,
        isClosing: false,
        queue: []
    };

    host.__ntSnackbarState = state;
    hostStates.set(host, state);
    return state;
}

function normalizeSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): SnackbarRecord {
    const source = typeof messageOrOptions === 'string'
        ? { ...options, message: messageOrOptions }
        : { ...messageOrOptions, ...options };

    const actions = normalizeActions(source);
    const hasAction = actions.length > 0;
    const timeout = typeof source.timeout === 'number' ? source.timeout : hasAction ? 0 : 4;

    return {
        actionColor: source.actionColor ?? defaultActionColor,
        actions,
        backgroundColor: source.backgroundColor ?? defaultBackgroundColor,
        dotNetActionMethod: source.dotNetActionMethod ?? null,
        dotNetCloseMethod: source.dotNetCloseMethod ?? null,
        dotNetReference: source.dotNetReference ?? null,
        host: source.host ?? null,
        id: source.id?.trim() || `nt-snackbar-js-${++nextSnackbarId}`,
        isActionInProgress: false,
        message: source.message,
        showClose: source.showClose ?? hasAction,
        textColor: source.textColor ?? defaultTextColor,
        timeout
    };
}

function normalizeActions(source: Partial<SnackbarOptions>): SnackbarActionRecord[] {
    const actions: SnackbarActionRecord[] = [];
    if (Array.isArray(source.actions)) {
        for (let index = 0; index < source.actions.length; index++) {
            const action = source.actions[index];
            const label = action?.label?.trim();
            if (label) {
                actions.push({
                    callback: typeof action.actionCallback === 'function' ? action.actionCallback : null,
                    index,
                    label
                });
            }
        }
    }

    if (actions.length === 0) {
        const label = source.actionLabel?.trim();
        if (label) {
            actions.push({
                callback: typeof source.actionCallback === 'function' ? source.actionCallback : null,
                index: 0,
                label
            });
        }
    }

    return actions;
}

function getExplicitSnackbarId(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string | null {
    const id = typeof messageOrOptions === 'string' ? options?.id : options?.id ?? messageOrOptions.id;
    return id?.trim() || null;
}

function setStyleVariables(element: HTMLElement, snackbar: SnackbarRecord): void {
    if (snackbar.backgroundColor && snackbar.backgroundColor !== defaultBackgroundColor) {
        element.style.setProperty('--nt-snackbar-background-color', snackbar.backgroundColor);
    }

    if (snackbar.textColor && snackbar.textColor !== defaultTextColor) {
        element.style.setProperty('--nt-snackbar-text-color', snackbar.textColor);
    }

    if (snackbar.actionColor && snackbar.actionColor !== defaultActionColor) {
        element.style.setProperty('--nt-snackbar-action-color', snackbar.actionColor);
    }
}

function createCloseIcon(): HTMLElement {
    const icon = document.createElement('span');
    icon.className = 'nt-icon material-symbols-outlined mi-medium';
    icon.textContent = 'close';
    return icon;
}

function invokeSnackbarAction(snackbar: SnackbarRecord, action: SnackbarActionRecord): Promise<boolean> {
    if (action.callback) {
        return Promise.resolve(action.callback()).then(result => result !== false);
    }

    if (snackbar.dotNetReference && snackbar.dotNetActionMethod) {
        return snackbar.dotNetReference.invokeMethodAsync<boolean>(snackbar.dotNetActionMethod, snackbar.id, action.index)
            .then(result => result !== false);
    }

    return Promise.resolve(true);
}

function notifySnackbarClosed(snackbar: SnackbarRecord): void {
    if (!snackbar.dotNetReference || !snackbar.dotNetCloseMethod) {
        return;
    }

    void snackbar.dotNetReference.invokeMethodAsync<void>(snackbar.dotNetCloseMethod, snackbar.id)
        .catch(error => reportSnackbarError('Failed to notify snackbar close.', error));
}

function notifySnackbarsClosed(snackbars: Iterable<SnackbarRecord>): void {
    for (const snackbar of snackbars) {
        notifySnackbarClosed(snackbar);
    }
}

function releaseSnackbarReference(snackbar: SnackbarRecord): void {
    for (const action of snackbar.actions) {
        action.callback = null;
    }
    snackbar.dotNetActionMethod = null;
    snackbar.dotNetCloseMethod = null;
    snackbar.dotNetReference = null;
}

function reportSnackbarError(message: string, error: unknown): void {
    console.error(message, error);
}

function removeSnackbarAt(queue: SnackbarRecord[], index: number, notifyDotNet: boolean): boolean {
    const removedSnackbar = queue.splice(index, 1)[0];
    if (!removedSnackbar) {
        return false;
    }

    if (notifyDotNet) {
        notifySnackbarClosed(removedSnackbar);
    } else {
        releaseSnackbarReference(removedSnackbar);
    }
    return true;
}

function removePendingSnackbar(id: string, notifyDotNet: boolean): boolean {
    const existingIndex = pendingSnackbars.findIndex(snackbar => snackbar.id === id);
    if (existingIndex < 0) {
        return false;
    }

    return removeSnackbarAt(pendingSnackbars, existingIndex, notifyDotNet);
}

function trimPendingSnackbars(): void {
    while (pendingSnackbars.length > maxPendingSnackbars) {
        const snackbar = pendingSnackbars.shift();
        if (snackbar) {
            notifySnackbarClosed(snackbar);
        }
    }
}

function trimQueuedSnackbars(queue: SnackbarRecord[]): void {
    while (queue.length > maxQueuedSnackbars) {
        const snackbar = queue.shift();
        if (snackbar) {
            notifySnackbarClosed(snackbar);
        }
    }
}

function getRegisteredHosts(): NTSnackbarHostElement[] {
    const hosts: NTSnackbarHostElement[] = [];
    for (const host of Array.from(registeredHosts)) {
        if (!host.isConnected) {
            disposeHost(host);
            continue;
        }

        hosts.push(host);
    }

    if (hosts.length === 0) {
        releaseDialogLayerObserver();
    }

    return hosts;
}

function findHostBySnackbarId(id: string): NTSnackbarHostElement | null {
    for (const host of getRegisteredHosts()) {
        const state = hostStates.get(host);
        if (state?.activeSnackbar?.id === id || state?.queue.some(snackbar => snackbar.id === id)) {
            return host;
        }
    }

    return null;
}

function findSnackbarById(id: string): SnackbarRecord | null {
    const pendingSnackbar = pendingSnackbars.find(snackbar => snackbar.id === id);
    if (pendingSnackbar) {
        return pendingSnackbar;
    }

    for (const host of getRegisteredHosts()) {
        const state = hostStates.get(host);
        const snackbar = state?.activeSnackbar?.id === id ? state.activeSnackbar : state?.queue.find(snackbar => snackbar.id === id);
        if (snackbar) {
            return snackbar;
        }
    }

    return null;
}

function createSnackbarElement(host: NTSnackbarHostElement, state: NTSnackbarHostState, snackbar: SnackbarRecord): HTMLElement {
    const stackItem = document.createElement('div');
    stackItem.className = 'nt-snackbar-stack-item nt-snackbar-front nt-snackbar-stack-depth-0';
    stackItem.style.setProperty('--nt-snackbar-stack-depth', '0');

    const snackbarElement = document.createElement('div');
    snackbarElement.id = snackbar.id;
    snackbarElement.className = 'nt-snackbar nt-elevation-medium';
    snackbarElement.setAttribute('role', 'status');
    snackbarElement.setAttribute('aria-live', 'polite');
    snackbarElement.setAttribute('aria-atomic', 'true');
    setStyleVariables(snackbarElement, snackbar);

    const message = document.createElement('div');
    message.className = 'nt-snackbar-message';
    message.textContent = snackbar.message;
    snackbarElement.appendChild(message);

    if (snackbar.actions.length > 0 || snackbar.showClose) {
        const actions = document.createElement('div');
        actions.className = 'nt-snackbar-actions';
        const actionButtons: HTMLButtonElement[] = [];

        for (const snackbarAction of snackbar.actions) {
            const action = document.createElement('button');
            action.className = 'nt-snackbar-action nt-interactable';
            action.type = 'button';
            action.textContent = snackbarAction.label;
            actionButtons.push(action);
            action.addEventListener('click', () => {
                if (snackbar.isActionInProgress) {
                    return;
                }

                snackbar.isActionInProgress = true;
                for (const actionButton of actionButtons) {
                    actionButton.disabled = true;
                }

                void invokeSnackbarAction(snackbar, snackbarAction)
                    .then(shouldClose => {
                        if (shouldClose) {
                            closeActiveSnackbar(host, state);
                            return;
                        }

                        snackbar.isActionInProgress = false;
                        for (const actionButton of actionButtons) {
                            actionButton.disabled = false;
                        }
                    })
                    .catch(error => {
                        snackbar.isActionInProgress = false;
                        for (const actionButton of actionButtons) {
                            actionButton.disabled = false;
                        }
                        reportSnackbarError('Snackbar action failed.', error);
                    });
            });
            actions.appendChild(action);
        }

        if (snackbar.showClose) {
            const close = document.createElement('button');
            close.className = 'nt-snackbar-close nt-interactable';
            close.type = 'button';
            close.setAttribute('aria-label', 'Dismiss notification');
            close.appendChild(createCloseIcon());
            close.addEventListener('click', () => closeActiveSnackbar(host, state));
            actions.appendChild(close);
        }

        snackbarElement.appendChild(actions);
    }

    stackItem.appendChild(snackbarElement);
    return stackItem;
}

function tryShowNext(host: NTSnackbarHostElement): void {
    const state = getOrCreateState(host);
    if (state.activeElement || state.isClosing || state.queue.length === 0) {
        return;
    }

    const snackbar = state.queue.shift();
    if (!snackbar) {
        return;
    }

    const element = createSnackbarElement(host, state, snackbar);
    state.activeSnackbar = snackbar;
    state.activeElement = element;
    host.appendChild(element);

    if (snackbar.timeout > 0) {
        state.closeTimeout = window.setTimeout(() => closeActiveSnackbar(host, state), snackbar.timeout * 1000);
    }

    if (hasOpenDialog()) {
        syncSnackbarHostForegroundContainer(host);
        promoteSnackbarHostToForeground(host);
    }
}

function closeActiveSnackbar(host: NTSnackbarHostElement, state: NTSnackbarHostState, notifyDotNet = true): boolean {
    const activeElement = state.activeElement;
    const activeSnackbar = state.activeSnackbar;
    if (!activeElement || state.isClosing) {
        return false;
    }

    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
        state.closeTimeout = null;
    }

    activeElement.querySelector('.nt-snackbar')?.classList.add('nt-closing');
    state.isClosing = true;
    if (!notifyDotNet && activeSnackbar) {
        releaseSnackbarReference(activeSnackbar);
    }

    state.closeAnimationTimeout = window.setTimeout(() => {
        activeElement.remove();
        state.closeAnimationTimeout = null;
        state.activeElement = null;
        state.activeSnackbar = null;
        state.isClosing = false;
        if (notifyDotNet && activeSnackbar) {
            notifySnackbarClosed(activeSnackbar);
        }
        if (host.isConnected) {
            tryShowNext(host);
        } else {
            hostStates.delete(host);
            registeredHosts.delete(host);
            if (defaultHost === host) {
                defaultHost = null;
            }
        }
    }, closeDelayMilliseconds);

    return true;
}

function flushBrowserPendingSnackbars(): void {
    const pendingQueue = window.__ntSnackbarPendingQueue;
    if (!pendingQueue || pendingQueue.length === 0) {
        return;
    }

    delete window.__ntSnackbarPendingQueue;
    for (const snackbar of pendingQueue) {
        queueSnackbar(snackbar);
    }
}

function flushPendingSnackbars(host: NTSnackbarHostElement): void {
    if (pendingSnackbars.length === 0) {
        return;
    }

    const toFlush: SnackbarRecord[] = [];
    let writeIndex = 0;
    for (let readIndex = 0; readIndex < pendingSnackbars.length; readIndex++) {
        const snackbar = pendingSnackbars[readIndex];
        if (pendingSnackbarMatchesHost(snackbar, host, host)) {
            toFlush.push(snackbar);
        } else {
            pendingSnackbars[writeIndex++] = snackbar;
        }
    }
    pendingSnackbars.length = writeIndex;

    if (toFlush.length === 0) {
        return;
    }

    const state = getOrCreateState(host);
    state.queue.push(...toFlush);
    trimQueuedSnackbars(state.queue);
    tryShowNext(host);
}

function clearPendingSnackbars(host?: HTMLElement | string | null): void {
    if (!host) {
        notifySnackbarsClosed(pendingSnackbars);
        pendingSnackbars.length = 0;
        return;
    }

    const resolvedHost = getHost(host);
    for (let index = pendingSnackbars.length - 1; index >= 0; index--) {
        const snackbar = pendingSnackbars[index];
        if (snackbar && pendingSnackbarMatchesHost(snackbar, host, resolvedHost)) {
            removeSnackbarAt(pendingSnackbars, index, true);
        }
    }
}

function pendingSnackbarMatchesHost(snackbar: SnackbarRecord, host: HTMLElement | string, resolvedHost: NTSnackbarHostElement | null): boolean {
    if (snackbar.host instanceof HTMLElement) {
        return snackbar.host === host || snackbar.host === resolvedHost;
    }

    if (typeof snackbar.host === 'string' && snackbar.host.length > 0) {
        return typeof host === 'string' ? snackbar.host === host : snackbar.host === resolvedHost?.id;
    }

    return resolvedHost !== null && defaultHost === resolvedHost;
}

function initializeHost(host: NTSnackbarHostElement): void {
    registeredHosts.add(host);
    getOrCreateState(host);
    ensureDialogLayerObserver();
    syncSnackbarHostForegroundContainer(host);
    showSnackbarHostPopover(host);
    defaultHost = host;
    flushPendingSnackbars(host);
}

function showSnackbarHostPopover(host: NTSnackbarHostElement, moveToForeground = false): void {
    if (typeof host.showPopover !== 'function') {
        return;
    }

    if (moveToForeground && typeof host.hidePopover === 'function') {
        try {
            host.hidePopover();
        } catch {
            // The host may not be open yet.
        }
    }

    try {
        host.showPopover();
    } catch {
        // Already-open or unsupported popover transitions should not block snackbar rendering.
    }
}

function hasOpenDialog(): boolean {
    return document.querySelector('dialog[open]') !== null;
}

function getFallbackForegroundDialog(): HTMLDialogElement | null {
    const openDialogs = Array.from(document.querySelectorAll<HTMLDialogElement>('dialog[open]'));
    return openDialogs.at(-1) ?? null;
}

function isOpenDialogNode(node: Node): boolean {
    if (node instanceof HTMLDialogElement) {
        return node.open;
    }

    return node instanceof Element && node.querySelector('dialog[open]') !== null;
}

function getOpenDialogNode(node: Node): HTMLDialogElement | null {
    if (node instanceof HTMLDialogElement) {
        return node.open ? node : null;
    }

    return node instanceof Element ? node.querySelector<HTMLDialogElement>('dialog[open]') : null;
}

function getForegroundDialogFromMutations(mutations: MutationRecord[]): HTMLDialogElement | null {
    for (const mutation of mutations) {
        if (mutation.type === 'attributes' && mutation.target instanceof HTMLDialogElement && mutation.target.open) {
            return mutation.target;
        }

        for (const node of mutation.addedNodes) {
            const dialog = getOpenDialogNode(node);
            if (dialog) {
                return dialog;
            }
        }
    }

    return getFallbackForegroundDialog();
}

function mutationTouchesDialog(mutation: MutationRecord): boolean {
    if (mutation.type === 'attributes' && mutation.target instanceof HTMLDialogElement) {
        return true;
    }

    for (const node of mutation.addedNodes) {
        if (isOpenDialogNode(node)) {
            return true;
        }
    }

    return false;
}

function syncSnackbarHostForegroundContainer(host: NTSnackbarHostElement): void {
    const dialog = foregroundDialog?.open ? foregroundDialog : getFallbackForegroundDialog();
    if (!dialog) {
        restoreSnackbarHostPlacement(host);
        return;
    }

    if (!host.__ntSnackbarPortalAnchor && host.parentNode) {
        host.__ntSnackbarPortalAnchor = document.createComment('nt-snackbar-host');
        host.parentNode.insertBefore(host.__ntSnackbarPortalAnchor, host);
    }

    if (host.parentNode !== dialog) {
        dialog.appendChild(host);
    }
}

function restoreSnackbarHostPlacement(host: NTSnackbarHostElement): void {
    const anchor = host.__ntSnackbarPortalAnchor;
    if (!anchor) {
        return;
    }

    if (anchor.parentNode) {
        anchor.parentNode.insertBefore(host, anchor);
    }

    anchor.remove();
    delete host.__ntSnackbarPortalAnchor;
}

function syncSnackbarHostsForegroundContainers(dialog: HTMLDialogElement | null = getFallbackForegroundDialog()): void {
    foregroundDialog = dialog;
    for (const host of getRegisteredHosts()) {
        syncSnackbarHostForegroundContainer(host);
    }
}

function promoteSnackbarHostToForeground(host: NTSnackbarHostElement): void {
    showSnackbarHostPopover(host, true);
}

function promoteSnackbarHostsToForeground(): void {
    syncSnackbarHostsForegroundContainers(foregroundDialog?.open ? foregroundDialog : getFallbackForegroundDialog());
    for (const host of getRegisteredHosts()) {
        promoteSnackbarHostToForeground(host);
    }
}

function ensureDialogLayerObserver(): void {
    if (dialogLayerObserver || typeof MutationObserver === 'undefined') {
        return;
    }

    dialogLayerObserver = new MutationObserver(mutations => {
        if (mutations.some(mutationTouchesDialog)) {
            syncSnackbarHostsForegroundContainers(getForegroundDialogFromMutations(mutations));
            promoteSnackbarHostsToForeground();
        }
    });
    dialogLayerObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['open'], childList: true, subtree: true });
}

function releaseDialogLayerObserver(): void {
    if (registeredHosts.size > 0) {
        return;
    }

    dialogLayerObserver?.disconnect();
    dialogLayerObserver = null;
}

function hideSnackbarHostPopover(host: NTSnackbarHostElement): void {
    if (typeof host.hidePopover !== 'function') {
        return;
    }

    try {
        host.hidePopover();
    } catch {
        // The host may already be disconnected or closed.
    }
}

function disposeHost(host: NTSnackbarHostElement): void {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
        state.closeTimeout = null;
    }

    if (state.closeAnimationTimeout !== null) {
        window.clearTimeout(state.closeAnimationTimeout);
        state.closeAnimationTimeout = null;
    }

    const closedSnackbars = [
        ...(state.activeSnackbar ? [state.activeSnackbar] : []),
        ...state.queue
    ];
    state.queue.length = 0;
    state.activeElement?.remove();
    restoreSnackbarHostPlacement(host);
    hideSnackbarHostPopover(host);
    delete host.__ntSnackbarState;
    hostStates.delete(host);
    registeredHosts.delete(host);
    releaseDialogLayerObserver();
    notifySnackbarsClosed(closedSnackbars);

    if (defaultHost === host) {
        defaultHost = null;
    }
}

export function queueSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string {
    const explicitId = getExplicitSnackbarId(messageOrOptions, options);
    if (explicitId) {
        const duplicateSnackbar = findSnackbarById(explicitId);
        if (duplicateSnackbar) {
            return duplicateSnackbar.id;
        }
    }

    const snackbar = normalizeSnackbar(messageOrOptions, options);
    const host = getHost(snackbar.host);
    if (!host) {
        pendingSnackbars.push(snackbar);
        trimPendingSnackbars();
        return snackbar.id;
    }

    const state = getOrCreateState(host);
    state.queue.push(snackbar);
    trimQueuedSnackbars(state.queue);
    tryShowNext(host);
    return snackbar.id;
}

export function queueSnackbarFromBlazor(id: string, message: string, actionLabels: string[] | string | null, timeout: number, showClose: boolean, backgroundColor: string, textColor: string, actionColor: string, dotNetReference: DotNetSnackbarReference, dotNetActionMethod: string, dotNetCloseMethod: string): string {
    const actions = Array.isArray(actionLabels)
        ? actionLabels.map(label => ({ label }))
        : actionLabels
            ? [{ label: actionLabels }]
            : [];

    return queueSnackbar({
        actionColor,
        actions,
        backgroundColor,
        dotNetActionMethod,
        dotNetCloseMethod,
        dotNetReference,
        id,
        message,
        showClose,
        textColor,
        timeout
    });
}

export function addSnackbar(messageOrOptions: SnackbarInput, options?: Partial<SnackbarOptions>): string {
    return queueSnackbar(messageOrOptions, options);
}

function closeSnackbarCore(id?: string, host?: HTMLElement | string | null, notifyDotNet = true): boolean {
    const resolvedHost = id && !host ? findHostBySnackbarId(id) ?? getHost(host) : getHost(host);
    if (!resolvedHost) {
        return id ? removePendingSnackbar(id, notifyDotNet) : false;
    }

    const state = getOrCreateState(resolvedHost);
    if (!id || state.activeSnackbar?.id === id) {
        return closeActiveSnackbar(resolvedHost, state, notifyDotNet);
    }

    const existingIndex = state.queue.findIndex(snackbar => snackbar.id === id);
    if (existingIndex < 0) {
        return removePendingSnackbar(id, notifyDotNet);
    }

    return removeSnackbarAt(state.queue, existingIndex, notifyDotNet);
}

export function closeSnackbar(id?: string, host?: HTMLElement | string | null): boolean {
    return closeSnackbarCore(id, host);
}

export function closeSnackbarFromBlazor(id: string, host?: HTMLElement | string | null): boolean {
    return closeSnackbarCore(id, host, false);
}

export function clearSnackbars(host?: HTMLElement | string | null): void {
    clearPendingSnackbars(host);

    if (!host) {
        for (const registeredHost of getRegisteredHosts()) {
            clearHostSnackbars(registeredHost);
        }
        return;
    }

    const resolvedHost = getHost(host);
    if (resolvedHost) {
        clearHostSnackbars(resolvedHost);
    }
}

function clearHostSnackbars(host: NTSnackbarHostElement): void {
    const state = getOrCreateState(host);
    if (state.closeTimeout !== null) {
        window.clearTimeout(state.closeTimeout);
        state.closeTimeout = null;
    }

    if (state.closeAnimationTimeout !== null) {
        window.clearTimeout(state.closeAnimationTimeout);
        state.closeAnimationTimeout = null;
    }

    const closedSnackbars = [
        ...(state.activeSnackbar ? [state.activeSnackbar] : []),
        ...state.queue
    ];
    state.queue.length = 0;
    state.activeElement?.remove();
    state.activeElement = null;
    state.activeSnackbar = null;
    state.isClosing = false;
    notifySnackbarsClosed(closedSnackbars);
}

export function onDispose(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        disposeHost(host);
    }

    if (element instanceof HTMLElement && element.localName === 'nt-page-script') {
        delete (element as NTSnackbarPageScriptElement).__ntSnackbarHost;
    }
}

export function onLoad(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        initializeHost(host);
    }
}

export function onUpdate(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        initializeHost(host);
    }
}

window.NTSnackbar = {
    addSnackbar,
    clearSnackbars,
    closeSnackbarFromBlazor,
    closeSnackbar,
    queueSnackbar,
    queueSnackbarFromBlazor
};

flushBrowserPendingSnackbars();
