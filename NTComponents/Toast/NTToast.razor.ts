type Maybe<T> = T | null | undefined;
export type ToastInput = string | ToastOptions;
export type ToastVariant = 'default' | 'success' | 'info' | 'warning' | 'error' | 'assert';

interface NTToastHostElement extends HTMLElement {
    __ntToastPortalAnchor?: Comment;
    __ntToastState?: NTToastHostState;
}

interface NTToastPageScriptElement extends HTMLElement {
    __ntToastHost?: NTToastHostElement;
}

interface NTToastHostState {
    activeToasts: ToastRecord[];
    queue: ToastRecord[];
}

interface ToastRecord {
    backgroundColor: string | null;
    closeAnimationTimeout: number | null;
    closeTimeout: number | null;
    dotNetCloseMethod: string | null;
    dotNetReference: DotNetToastReference | null;
    element: HTMLElement | null;
    host: HTMLElement | string | null;
    icon: string | null;
    iconColor: string | null;
    id: string;
    isClosing: boolean;
    message: string | null;
    showClose: boolean;
    shownAtMilliseconds: number | null;
    textColor: string | null;
    timeout: number;
    title: string;
    variant: ToastVariant;
}

export interface ToastOptions {
    backgroundColor?: string | null;
    dotNetCloseMethod?: string | null;
    dotNetReference?: DotNetToastReference | null;
    host?: HTMLElement | string | null;
    icon?: string | null;
    iconColor?: string | null;
    id?: string | null;
    message?: string | null;
    showClose?: boolean | null;
    textColor?: string | null;
    timeout?: number | null;
    title: string;
    variant?: ToastVariant | string | null;
}

export interface NTToastBridge {
    addToast(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): string;
    clearToasts(host?: HTMLElement | string | null): void;
    clearToastsFromBlazor(ids: string[]): number;
    closeToast(id?: string, host?: HTMLElement | string | null): boolean;
    closeToastFromBlazor(id: string, host?: HTMLElement | string | null): boolean;
    queueToast(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): string;
    queueToastFromBlazor(id: string, title: string, message: string | null, variant: ToastVariant | string | null, timeout: number, showClose: boolean, icon: string | null, backgroundColor: string | null, textColor: string | null, iconColor: string | null, dotNetReference: DotNetToastReference, dotNetCloseMethod: string): string;
}

export interface DotNetToastReference {
    invokeMethodAsync<T>(methodName: string, ...args: unknown[]): Promise<T>;
}

declare global {
    interface Window {
        __ntToastPendingQueue?: ToastOptions[];
        NTToast?: NTToastBridge;
    }
}

const toastHostSelector = '[data-nt-toast-host="true"]';
const closeDelayMilliseconds = 150;
const defaultTimeoutSeconds = 4;
const maxPendingToasts = 50;
const maxQueuedToasts = 50;
const maxVisibleToasts = 5;
const pendingToasts: ToastRecord[] = [];
const hostStates = new WeakMap<NTToastHostElement, NTToastHostState>();
const registeredHosts = new Set<NTToastHostElement>();
let dialogLayerObserver: MutationObserver | null = null;
let defaultHost: NTToastHostElement | null = null;
let foregroundDialog: HTMLDialogElement | null = null;
let nextToastId = 0;

const variantDefaults: Record<ToastVariant, Pick<ToastRecord, 'backgroundColor' | 'icon' | 'iconColor' | 'textColor'>> = {
    default: {
        backgroundColor: null,
        icon: 'info',
        iconColor: null,
        textColor: null
    },
    success: {
        backgroundColor: null,
        icon: 'check_circle',
        iconColor: null,
        textColor: null
    },
    info: {
        backgroundColor: null,
        icon: 'info',
        iconColor: null,
        textColor: null
    },
    warning: {
        backgroundColor: null,
        icon: 'warning',
        iconColor: null,
        textColor: null
    },
    error: {
        backgroundColor: null,
        icon: 'error',
        iconColor: null,
        textColor: null
    },
    assert: {
        backgroundColor: null,
        icon: 'rule',
        iconColor: null,
        textColor: null
    }
};

function getHosts(root: Maybe<Element | Document>): NTToastHostElement[] {
    const scope = root ?? document;
    if (scope instanceof HTMLElement && scope.localName === 'nt-page-script') {
        const pageScript = scope as NTToastPageScriptElement;
        if (pageScript.__ntToastHost) {
            return [pageScript.__ntToastHost];
        }

        const sibling = scope.previousElementSibling;
        if (sibling instanceof HTMLElement && sibling.matches(toastHostSelector)) {
            pageScript.__ntToastHost = sibling as NTToastHostElement;
            return [pageScript.__ntToastHost];
        }

        return [];
    }

    const hosts = scope instanceof HTMLElement && scope.matches(toastHostSelector) ? [scope as NTToastHostElement] : [];
    hosts.push(...Array.from(scope.querySelectorAll<NTToastHostElement>(toastHostSelector)));
    return hosts;
}

function getHost(host?: HTMLElement | string | null): NTToastHostElement | null {
    if (host instanceof HTMLElement) {
        return host.matches(toastHostSelector) ? host as NTToastHostElement : null;
    }

    if (typeof host === 'string' && host.length > 0) {
        const element = document.getElementById(host);
        return element instanceof HTMLElement && element.matches(toastHostSelector) ? element as NTToastHostElement : null;
    }

    if (defaultHost?.isConnected) {
        return defaultHost;
    }

    defaultHost = document.querySelector<NTToastHostElement>(toastHostSelector);
    return defaultHost;
}

function getOrCreateState(host: NTToastHostElement): NTToastHostState {
    const existingState = hostStates.get(host);
    if (existingState) {
        return existingState;
    }

    const state: NTToastHostState = {
        activeToasts: [],
        queue: []
    };

    host.__ntToastState = state;
    hostStates.set(host, state);
    return state;
}

function normalizeVariant(variant: string | null | undefined): ToastVariant {
    const value = variant?.toLowerCase();
    return value === 'success' || value === 'info' || value === 'warning' || value === 'error' || value === 'assert'
        ? value
        : 'default';
}

function normalizeToast(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): ToastRecord {
    const source = typeof messageOrOptions === 'string'
        ? { ...options, title: messageOrOptions }
        : { ...messageOrOptions, ...options };
    const variant = normalizeVariant(source.variant);
    const defaults = variantDefaults[variant];

    return {
        backgroundColor: source.backgroundColor ?? defaults.backgroundColor,
        closeAnimationTimeout: null,
        closeTimeout: null,
        dotNetCloseMethod: source.dotNetCloseMethod ?? null,
        dotNetReference: source.dotNetReference ?? null,
        element: null,
        host: source.host ?? null,
        icon: source.icon === '' ? null : source.icon ?? defaults.icon,
        iconColor: source.iconColor ?? defaults.iconColor,
        id: source.id?.trim() || `nt-toast-js-${++nextToastId}`,
        isClosing: false,
        message: source.message ?? null,
        showClose: source.showClose ?? true,
        shownAtMilliseconds: null,
        textColor: source.textColor ?? defaults.textColor,
        timeout: typeof source.timeout === 'number' ? source.timeout : defaultTimeoutSeconds,
        title: source.title,
        variant
    };
}

function getExplicitToastId(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): string | null {
    const id = typeof messageOrOptions === 'string' ? options?.id : options?.id ?? messageOrOptions.id;
    return id?.trim() || null;
}

function setStyleVariables(element: HTMLElement, toast: ToastRecord): void {
    if (toast.backgroundColor) {
        element.style.setProperty('--nt-toast-background-color', toast.backgroundColor);
    }
    if (toast.textColor) {
        element.style.setProperty('--nt-toast-text-color', toast.textColor);
    }
    if (toast.iconColor) {
        element.style.setProperty('--nt-toast-icon-color', toast.iconColor);
    }
    if (toast.timeout > 0) {
        element.style.setProperty('--nt-toast-timeout', `${toast.timeout}s`);
    }
}

function createIcon(iconName: string, className: string): HTMLElement {
    const icon = document.createElement('span');
    icon.className = `nt-icon material-symbols-outlined mi-medium ${className}`;
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = iconName;
    return icon;
}

function notifyToastClosed(toast: ToastRecord): void {
    if (!toast.dotNetReference || !toast.dotNetCloseMethod) {
        return;
    }

    void toast.dotNetReference.invokeMethodAsync<void>(toast.dotNetCloseMethod, toast.id)
        .catch(error => reportToastError('Failed to notify toast close.', error));
}

function notifyToastsClosed(toasts: Iterable<ToastRecord>): void {
    for (const toast of toasts) {
        notifyToastClosed(toast);
    }
}

function releaseToastReference(toast: ToastRecord): void {
    toast.dotNetCloseMethod = null;
    toast.dotNetReference = null;
}

function reportToastError(message: string, error: unknown): void {
    console.error(message, error);
}

function removeToastAt(queue: ToastRecord[], index: number, notifyDotNet: boolean): boolean {
    const removedToast = queue.splice(index, 1)[0];
    if (!removedToast) {
        return false;
    }

    if (notifyDotNet) {
        notifyToastClosed(removedToast);
    } else {
        releaseToastReference(removedToast);
    }
    return true;
}

function removePendingToast(id: string, notifyDotNet: boolean): boolean {
    const existingIndex = pendingToasts.findIndex(toast => toast.id === id);
    if (existingIndex < 0) {
        return false;
    }

    return removeToastAt(pendingToasts, existingIndex, notifyDotNet);
}

function trimToastQueue(queue: ToastRecord[], maxCount: number): void {
    while (queue.length > maxCount) {
        const toast = queue.shift();
        if (toast) {
            notifyToastClosed(toast);
        }
    }
}

function getRegisteredHosts(): NTToastHostElement[] {
    const hosts: NTToastHostElement[] = [];
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

function findHostByToastId(id: string): NTToastHostElement | null {
    for (const host of getRegisteredHosts()) {
        const state = hostStates.get(host);
        if (state?.activeToasts.some(toast => toast.id === id) || state?.queue.some(toast => toast.id === id)) {
            return host;
        }
    }

    return null;
}

function findToastById(id: string): ToastRecord | null {
    const pendingToast = pendingToasts.find(toast => toast.id === id);
    if (pendingToast) {
        return pendingToast;
    }

    for (const host of getRegisteredHosts()) {
        const state = hostStates.get(host);
        const toast = state?.activeToasts.find(toast => toast.id === id) ?? state?.queue.find(toast => toast.id === id);
        if (toast) {
            return toast;
        }
    }

    return null;
}

function createToastElement(host: NTToastHostElement, state: NTToastHostState, toast: ToastRecord): HTMLElement {
    const toastElement = document.createElement('div');
    toastElement.id = toast.id;
    toastElement.className = `nt-toast nt-toast-${toast.variant} nt-elevation-medium${toast.timeout > 0 ? ' nt-toast-has-timeout' : ''}`;
    toastElement.setAttribute('role', toast.variant === 'error' || toast.variant === 'assert' ? 'alert' : 'status');
    toastElement.setAttribute('aria-live', toast.variant === 'error' || toast.variant === 'assert' ? 'assertive' : 'polite');
    toastElement.setAttribute('aria-atomic', 'true');
    setStyleVariables(toastElement, toast);

    if (toast.icon) {
        toastElement.appendChild(createIcon(toast.icon, 'nt-toast-icon'));
    }

    const title = document.createElement('div');
    title.className = 'nt-toast-title';
    title.textContent = toast.title;
    toastElement.appendChild(title);

    if (toast.message) {
        const message = document.createElement('div');
        message.className = 'nt-toast-message';
        message.textContent = toast.message;
        toastElement.appendChild(message);
    }

    if (toast.showClose) {
        const close = document.createElement('button');
        close.className = 'nt-toast-close nt-interactable';
        close.type = 'button';
        close.setAttribute('aria-label', 'Dismiss notification');
        close.appendChild(createIcon('close', ''));
        close.addEventListener('click', () => closeToastRecord(host, state, toast));
        toastElement.appendChild(close);
    }

    if (toast.timeout > 0) {
        const progress = document.createElement('div');
        progress.className = 'nt-toast-progress';
        progress.setAttribute('aria-hidden', 'true');
        toastElement.appendChild(progress);
    }

    toast.element = toastElement;
    return toastElement;
}

function formatAnimationDelaySeconds(seconds: number): string {
    return Number(seconds.toFixed(3)).toString();
}

function syncToastProgress(toast: ToastRecord): void {
    if (toast.timeout <= 0 || toast.shownAtMilliseconds === null || !toast.element) {
        return;
    }

    const progress = toast.element.querySelector<HTMLElement>('.nt-toast-progress');
    if (!progress) {
        return;
    }

    const elapsedSeconds = Math.min(Math.max((Date.now() - toast.shownAtMilliseconds) / 1000, 0), toast.timeout);
    progress.style.animationDelay = elapsedSeconds > 0 ? `-${formatAnimationDelaySeconds(elapsedSeconds)}s` : '';
}

function syncActiveToastProgress(host: NTToastHostElement): void {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    for (const toast of state.activeToasts) {
        syncToastProgress(toast);
    }
}

function tryShowNext(host: NTToastHostElement): void {
    const state = getOrCreateState(host);
    let activeVisibleCount = state.activeToasts.reduce((count, toast) => toast.isClosing ? count : count + 1, 0);
    while (activeVisibleCount < maxVisibleToasts && state.queue.length > 0) {
        const toast = state.queue.shift();
        if (!toast) {
            return;
        }

        const element = createToastElement(host, state, toast);
        state.activeToasts.push(toast);
        host.appendChild(element);

        if (toast.timeout > 0) {
            toast.shownAtMilliseconds = Date.now();
            toast.closeTimeout = window.setTimeout(() => closeToastRecord(host, state, toast), toast.timeout * 1000);
        }

        activeVisibleCount++;
    }

    if (hasOpenDialog()) {
        syncToastHostForegroundContainer(host);
        promoteToastHostToForeground(host);
    }
}

function closeToastRecord(host: NTToastHostElement, state: NTToastHostState, toast: ToastRecord, notifyDotNet = true): boolean {
    if (toast.isClosing || !toast.element) {
        return false;
    }

    if (toast.closeTimeout !== null) {
        window.clearTimeout(toast.closeTimeout);
        toast.closeTimeout = null;
    }

    toast.isClosing = true;
    const toastElement = toast.element;
    toastElement.classList.add('nt-closing');
    if (!notifyDotNet) {
        releaseToastReference(toast);
    }

    let hasClosed = false;
    const finishClose = () => {
        if (hasClosed) {
            return;
        }

        hasClosed = true;
        toastElement.removeEventListener('animationend', handleAnimationDone);
        toastElement.removeEventListener('animationcancel', handleAnimationDone);
        if (toast.closeAnimationTimeout !== null) {
            window.clearTimeout(toast.closeAnimationTimeout);
        }

        toastElement.remove();
        toast.element = null;
        toast.closeAnimationTimeout = null;
        const existingIndex = state.activeToasts.indexOf(toast);
        if (existingIndex >= 0) {
            state.activeToasts.splice(existingIndex, 1);
        }
        if (notifyDotNet) {
            notifyToastClosed(toast);
        }
        if (host.isConnected) {
            tryShowNext(host);
        }
    };
    const handleAnimationDone = (event: AnimationEvent) => {
        if (event.target === toastElement) {
            finishClose();
        }
    };

    toastElement.addEventListener('animationend', handleAnimationDone);
    toastElement.addEventListener('animationcancel', handleAnimationDone);
    toast.closeAnimationTimeout = window.setTimeout(finishClose, closeDelayMilliseconds);

    return true;
}

function flushBrowserPendingToasts(): void {
    const pendingQueue = window.__ntToastPendingQueue;
    if (!pendingQueue || pendingQueue.length === 0) {
        return;
    }

    delete window.__ntToastPendingQueue;
    for (const toast of pendingQueue) {
        queueToast(toast);
    }
}

function flushPendingToasts(host: NTToastHostElement): void {
    if (pendingToasts.length === 0) {
        return;
    }

    const toFlush: ToastRecord[] = [];
    let writeIndex = 0;
    for (let readIndex = 0; readIndex < pendingToasts.length; readIndex++) {
        const toast = pendingToasts[readIndex];
        if (pendingToastMatchesHost(toast, host, host)) {
            toFlush.push(toast);
        } else {
            pendingToasts[writeIndex++] = toast;
        }
    }
    pendingToasts.length = writeIndex;

    if (toFlush.length === 0) {
        return;
    }

    const state = getOrCreateState(host);
    state.queue.push(...toFlush);
    trimToastQueue(state.queue, maxQueuedToasts);
    tryShowNext(host);
}

function clearPendingToasts(host?: HTMLElement | string | null): void {
    if (!host) {
        notifyToastsClosed(pendingToasts);
        pendingToasts.length = 0;
        return;
    }

    const resolvedHost = getHost(host);
    for (let index = pendingToasts.length - 1; index >= 0; index--) {
        const toast = pendingToasts[index];
        if (toast && pendingToastMatchesHost(toast, host, resolvedHost)) {
            removeToastAt(pendingToasts, index, true);
        }
    }
}

function pendingToastMatchesHost(toast: ToastRecord, host: HTMLElement | string, resolvedHost: NTToastHostElement | null): boolean {
    if (toast.host instanceof HTMLElement) {
        return toast.host === host || toast.host === resolvedHost;
    }

    if (typeof toast.host === 'string' && toast.host.length > 0) {
        return typeof host === 'string' ? toast.host === host : toast.host === resolvedHost?.id;
    }

    return resolvedHost !== null && defaultHost === resolvedHost;
}

function initializeHost(host: NTToastHostElement): void {
    registeredHosts.add(host);
    getOrCreateState(host);
    ensureDialogLayerObserver();
    syncToastHostForegroundContainer(host);
    showToastHostPopover(host);
    defaultHost = host;
    flushPendingToasts(host);
}

function showToastHostPopover(host: NTToastHostElement, moveToForeground = false): void {
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
        // Already-open or unsupported popover transitions should not block toast rendering.
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

function syncToastHostForegroundContainer(host: NTToastHostElement): void {
    const dialog = foregroundDialog?.open ? foregroundDialog : getFallbackForegroundDialog();
    if (!dialog) {
        restoreToastHostPlacement(host);
        syncActiveToastProgress(host);
        return;
    }

    if (!host.__ntToastPortalAnchor && host.parentNode) {
        host.__ntToastPortalAnchor = document.createComment('nt-toast-host');
        host.parentNode.insertBefore(host.__ntToastPortalAnchor, host);
    }

    if (host.parentNode !== dialog) {
        dialog.appendChild(host);
    }
    syncActiveToastProgress(host);
}

function restoreToastHostPlacement(host: NTToastHostElement): void {
    const anchor = host.__ntToastPortalAnchor;
    if (!anchor) {
        return;
    }

    if (anchor.parentNode) {
        anchor.parentNode.insertBefore(host, anchor);
    }

    anchor.remove();
    delete host.__ntToastPortalAnchor;
}

function syncToastHostsForegroundContainers(dialog: HTMLDialogElement | null = getFallbackForegroundDialog()): void {
    foregroundDialog = dialog;
    for (const host of getRegisteredHosts()) {
        syncToastHostForegroundContainer(host);
    }
}

function promoteToastHostToForeground(host: NTToastHostElement): void {
    showToastHostPopover(host, true);
    syncActiveToastProgress(host);
}

function promoteToastHostsToForeground(): void {
    syncToastHostsForegroundContainers(foregroundDialog?.open ? foregroundDialog : getFallbackForegroundDialog());
    for (const host of getRegisteredHosts()) {
        promoteToastHostToForeground(host);
    }
}

function ensureDialogLayerObserver(): void {
    if (dialogLayerObserver || typeof MutationObserver === 'undefined') {
        return;
    }

    dialogLayerObserver = new MutationObserver(mutations => {
        if (mutations.some(mutationTouchesDialog)) {
            syncToastHostsForegroundContainers(getForegroundDialogFromMutations(mutations));
            promoteToastHostsToForeground();
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

function hideToastHostPopover(host: NTToastHostElement): void {
    if (typeof host.hidePopover !== 'function') {
        return;
    }

    try {
        host.hidePopover();
    } catch {
        // The host may already be disconnected or closed.
    }
}

function disposeHost(host: NTToastHostElement): void {
    const state = hostStates.get(host);
    if (!state) {
        return;
    }

    const closedToasts = [...state.activeToasts, ...state.queue];
    for (const toast of closedToasts) {
        if (toast.closeTimeout !== null) {
            window.clearTimeout(toast.closeTimeout);
            toast.closeTimeout = null;
        }
        if (toast.closeAnimationTimeout !== null) {
            window.clearTimeout(toast.closeAnimationTimeout);
            toast.closeAnimationTimeout = null;
        }
        toast.element?.remove();
        toast.element = null;
    }

    state.activeToasts.length = 0;
    state.queue.length = 0;
    restoreToastHostPlacement(host);
    hideToastHostPopover(host);
    delete host.__ntToastState;
    hostStates.delete(host);
    registeredHosts.delete(host);
    releaseDialogLayerObserver();
    notifyToastsClosed(closedToasts);

    if (defaultHost === host) {
        defaultHost = null;
    }
}

export function queueToast(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): string {
    const explicitId = getExplicitToastId(messageOrOptions, options);
    if (explicitId) {
        const duplicateToast = findToastById(explicitId);
        if (duplicateToast) {
            return duplicateToast.id;
        }
    }

    const toast = normalizeToast(messageOrOptions, options);
    const host = getHost(toast.host);
    if (!host) {
        pendingToasts.push(toast);
        trimToastQueue(pendingToasts, maxPendingToasts);
        return toast.id;
    }

    const state = getOrCreateState(host);
    state.queue.push(toast);
    trimToastQueue(state.queue, maxQueuedToasts);
    tryShowNext(host);
    return toast.id;
}

export function queueToastFromBlazor(id: string, title: string, message: string | null, variant: ToastVariant | string | null, timeout: number, showClose: boolean, icon: string | null, backgroundColor: string | null, textColor: string | null, iconColor: string | null, dotNetReference: DotNetToastReference, dotNetCloseMethod: string): string {
    return queueToast({
        backgroundColor,
        dotNetCloseMethod,
        dotNetReference,
        icon,
        iconColor,
        id,
        message,
        showClose,
        textColor,
        timeout,
        title,
        variant
    });
}

export function addToast(messageOrOptions: ToastInput, options?: Partial<ToastOptions>): string {
    return queueToast(messageOrOptions, options);
}

function closeToastCore(id?: string, host?: HTMLElement | string | null, notifyDotNet = true): boolean {
    const resolvedHost = id && !host ? findHostByToastId(id) ?? getHost(host) : getHost(host);
    if (!resolvedHost) {
        return id ? removePendingToast(id, notifyDotNet) : false;
    }

    const state = getOrCreateState(resolvedHost);
    const activeToast = id ? state.activeToasts.find(toast => toast.id === id) : state.activeToasts.at(-1);
    if (activeToast) {
        return closeToastRecord(resolvedHost, state, activeToast, notifyDotNet);
    }

    if (!id) {
        return false;
    }

    const existingIndex = state.queue.findIndex(toast => toast.id === id);
    if (existingIndex < 0) {
        return removePendingToast(id, notifyDotNet);
    }

    return removeToastAt(state.queue, existingIndex, notifyDotNet);
}

export function closeToast(id?: string, host?: HTMLElement | string | null): boolean {
    return closeToastCore(id, host);
}

export function closeToastFromBlazor(id: string, host?: HTMLElement | string | null): boolean {
    return closeToastCore(id, host, false);
}

export function clearToasts(host?: HTMLElement | string | null): void {
    clearPendingToasts(host);

    if (!host) {
        for (const registeredHost of getRegisteredHosts()) {
            clearHostToasts(registeredHost);
        }
        return;
    }

    const resolvedHost = getHost(host);
    if (resolvedHost) {
        clearHostToasts(resolvedHost);
    }
}

function clearHostToasts(host: NTToastHostElement): void {
    const state = getOrCreateState(host);
    const closedToasts = [...state.activeToasts, ...state.queue];
    for (const toast of closedToasts) {
        if (toast.closeTimeout !== null) {
            window.clearTimeout(toast.closeTimeout);
            toast.closeTimeout = null;
        }
        if (toast.closeAnimationTimeout !== null) {
            window.clearTimeout(toast.closeAnimationTimeout);
            toast.closeAnimationTimeout = null;
        }
        toast.element?.remove();
        toast.element = null;
        toast.isClosing = false;
    }

    state.activeToasts.length = 0;
    state.queue.length = 0;
    notifyToastsClosed(closedToasts);
}

export function clearToastsFromBlazor(ids: string[]): number {
    const idsToClear = new Set(ids);
    if (idsToClear.size === 0) {
        return 0;
    }

    let clearedCount = 0;
    for (let index = pendingToasts.length - 1; index >= 0; index--) {
        const toast = pendingToasts[index];
        if (toast && idsToClear.has(toast.id)) {
            pendingToasts.splice(index, 1);
            releaseToastReference(toast);
            clearedCount++;
        }
    }

    for (const host of getRegisteredHosts()) {
        const state = getOrCreateState(host);
        for (const toast of [...state.activeToasts]) {
            if (idsToClear.has(toast.id) && closeToastRecord(host, state, toast, false)) {
                clearedCount++;
            }
        }

        for (let index = state.queue.length - 1; index >= 0; index--) {
            const toast = state.queue[index];
            if (toast && idsToClear.has(toast.id)) {
                state.queue.splice(index, 1);
                releaseToastReference(toast);
                clearedCount++;
            }
        }
    }

    return clearedCount;
}

export function onDispose(element?: Maybe<Element>): void {
    for (const host of getHosts(element)) {
        disposeHost(host);
    }

    if (element instanceof HTMLElement && element.localName === 'nt-page-script') {
        delete (element as NTToastPageScriptElement).__ntToastHost;
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

window.NTToast = {
    addToast,
    clearToastsFromBlazor,
    clearToasts,
    closeToastFromBlazor,
    closeToast,
    queueToast,
    queueToastFromBlazor
};

flushBrowserPendingToasts();
