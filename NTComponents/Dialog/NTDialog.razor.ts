type DialogReference = HTMLDialogElement | string;
type Maybe<T> = T | null | undefined;

interface DotNetDialogReference {
    invokeMethodAsync<T>(methodName: string, ...args: unknown[]): Promise<T>;
}

interface NTDialogElement extends HTMLDialogElement {
    __ntDialogState?: NTDialogState;
}

interface NTDialogState {
    autoOpenApplied: boolean;
    dotNetRef: DotNetDialogReference | null;
    dialogId: string;
    mutationObserver: MutationObserver | null;
    onCancel: (event: Event) => void;
    onClick: (event: MouseEvent) => void;
    onClose: () => void;
    resizeObserver: ResizeObserver | null;
    scrollUpdateFrame: number | null;
    scrollUpdateTimeout: number | null;
    scrollableContent: HTMLElement | null;
}

const initializedDialogsById = new Map<string, NTDialogElement>();
const closeAnimationFallbackMilliseconds = 250;
const dialogClosingClass = 'nt-dialog-closing';
let documentCommandClickListening = false;

function isDotNetReference(value: unknown): value is DotNetDialogReference {
    return typeof value === 'object'
        && value !== null
        && 'invokeMethodAsync' in value
        && typeof (value as DotNetDialogReference).invokeMethodAsync === 'function';
}

function getDialog(dialogOrId: DialogReference): NTDialogElement | null {
    if (dialogOrId instanceof HTMLDialogElement) {
        return dialogOrId as NTDialogElement;
    }

    const element = document.getElementById(dialogOrId);
    return element instanceof HTMLDialogElement ? element as NTDialogElement : null;
}

function getDialogs(root: Maybe<Element | Document>): NTDialogElement[] {
    const scope = root ?? document;
    if (scope instanceof HTMLElement && scope.localName === 'nt-page-script') {
        const sibling = scope.previousElementSibling;
        return sibling instanceof HTMLDialogElement && sibling.matches('[data-nt-dialog="true"]')
            ? [sibling as NTDialogElement]
            : [];
    }

    const dialogs = scope instanceof HTMLDialogElement && scope.matches('[data-nt-dialog="true"]')
        ? [scope as NTDialogElement]
        : [];

    dialogs.push(...Array.from(scope.querySelectorAll<NTDialogElement>('dialog[data-nt-dialog="true"]')));
    return dialogs;
}

function getDialogReturnValueFromCommand(element: Element): string {
    if (element instanceof HTMLButtonElement || element instanceof HTMLInputElement) {
        return element.value;
    }

    return '';
}

function getCommandButton(target: EventTarget | null): HTMLButtonElement | HTMLInputElement | null {
    if (!(target instanceof Element)) {
        return null;
    }

    const button = target.closest('button[commandfor], input[commandfor]');
    return button instanceof HTMLButtonElement || button instanceof HTMLInputElement ? button : null;
}

function updateScrollableState(dialog: NTDialogElement): void {
    const content = dialog.querySelector<HTMLElement>('.nt-dialog-content');
    if (!content) {
        return;
    }

    content.classList.toggle('nt-dialog-content-scrollable', content.scrollHeight > content.clientHeight);
}

function scheduleScrollableStateUpdate(dialog: NTDialogElement): void {
    if (!dialog.open) {
        return;
    }

    const state = dialog.__ntDialogState;
    if (!state) {
        updateScrollableState(dialog);
        return;
    }

    if (state.scrollUpdateFrame !== null || state.scrollUpdateTimeout !== null) {
        return;
    }

    if (typeof window.requestAnimationFrame === 'function') {
        state.scrollUpdateFrame = window.requestAnimationFrame(() => {
            state.scrollUpdateFrame = null;
            updateScrollableState(dialog);
        });
        return;
    }

    state.scrollUpdateTimeout = window.setTimeout(() => {
        state.scrollUpdateTimeout = null;
        updateScrollableState(dialog);
    }, 0);
}

function observeScrollableContent(dialog: NTDialogElement): void {
    const state = dialog.__ntDialogState;
    if (!state) {
        return;
    }

    const content = dialog.querySelector<HTMLElement>('.nt-dialog-content');
    if (state.scrollableContent === content) {
        return;
    }

    state.resizeObserver?.disconnect();
    state.mutationObserver?.disconnect();
    state.resizeObserver = null;
    state.mutationObserver = null;
    state.scrollableContent = content;

    if (!content) {
        return;
    }

    if (typeof ResizeObserver !== 'undefined') {
        state.resizeObserver = new ResizeObserver(() => scheduleScrollableStateUpdate(dialog));
        state.resizeObserver.observe(content);
    }

    state.mutationObserver = new MutationObserver(() => scheduleScrollableStateUpdate(dialog));
    state.mutationObserver.observe(content, { childList: true, characterData: true, subtree: true });
    scheduleScrollableStateUpdate(dialog);
}

function isModalDialog(dialog: HTMLDialogElement): boolean {
    if (typeof dialog.matches !== 'function') {
        return false;
    }

    try {
        return dialog.matches(':modal');
    } catch {
        return false;
    }
}

function showDialog(dialog: HTMLDialogElement): boolean {
    if (dialog.open) {
        if (isModalDialog(dialog)) {
            return false;
        }

        dialog.removeAttribute('open');
    }

    dialog.classList.remove(dialogClosingClass);

    try {
        dialog.showModal();
        scheduleScrollableStateUpdate(dialog as NTDialogElement);
        return true;
    } catch {
        dialog.setAttribute('open', '');
        scheduleScrollableStateUpdate(dialog as NTDialogElement);
        return true;
    }
}

function openInitialModalDialog(dialog: NTDialogElement): void {
    if (!dialog.open || dialog.__ntDialogState?.autoOpenApplied === true) {
        return;
    }

    if (dialog.__ntDialogState) {
        dialog.__ntDialogState.autoOpenApplied = true;
    }

    showDialog(dialog);
}

function prefersReducedMotion(): boolean {
    return typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function waitForCloseAnimation(dialog: HTMLDialogElement): Promise<void> {
    if (prefersReducedMotion()) {
        return Promise.resolve();
    }

    dialog.classList.add(dialogClosingClass);
    void dialog.offsetWidth;

    return new Promise<void>(resolve => {
        let resolved = false;
        const resolveOnce = (): void => {
            if (resolved) {
                return;
            }

            resolved = true;
            window.clearTimeout(timeout);
            dialog.removeEventListener('animationcancel', onAnimationEnd);
            dialog.removeEventListener('animationend', onAnimationEnd);
            resolve();
        };

        const onAnimationEnd = (event: AnimationEvent): void => {
            if (event.target === dialog && event.animationName === 'nt-dialog-exit') {
                resolveOnce();
            }
        };

        const timeout = window.setTimeout(resolveOnce, closeAnimationFallbackMilliseconds);
        dialog.addEventListener('animationcancel', onAnimationEnd);
        dialog.addEventListener('animationend', onAnimationEnd);
    });
}

async function closeDialogElement(dialog: HTMLDialogElement, returnValue: string | null = ''): Promise<boolean> {
    if (!dialog.open) {
        dialog.removeAttribute('open');
        return false;
    }

    const closeEvent = new Promise<boolean>(resolve => dialog.addEventListener('close', () => resolve(true), { once: true }));

    try {
        await waitForCloseAnimation(dialog);
        dialog.close(returnValue ?? '');
        dialog.classList.remove(dialogClosingClass);
        return await Promise.race([closeEvent, new Promise<boolean>(resolve => window.setTimeout(() => resolve(true), 0))]);
    } catch {
        dialog.classList.remove(dialogClosingClass);
        dialog.removeAttribute('open');
        return true;
    }
}

async function requestOpen(dialog: NTDialogElement): Promise<boolean> {
    const dotNetRef = dialog.__ntDialogState?.dotNetRef;
    if (!dotNetRef) {
        return true;
    }

    return await dotNetRef.invokeMethodAsync<boolean>('RequestOpenFromJavaScript');
}

async function notifyOpened(dialog: NTDialogElement): Promise<void> {
    await dialog.__ntDialogState?.dotNetRef?.invokeMethodAsync<void>('NotifyOpenedFromJavaScript');
}

async function requestClose(dialog: NTDialogElement, returnValue: string | null = ''): Promise<boolean> {
    const dotNetRef = dialog.__ntDialogState?.dotNetRef;
    if (!dotNetRef) {
        return true;
    }

    return await dotNetRef.invokeMethodAsync<boolean>('RequestCloseFromJavaScript', returnValue ?? '');
}

async function notifyClosed(dialog: NTDialogElement): Promise<void> {
    await dialog.__ntDialogState?.dotNetRef?.invokeMethodAsync<void>('NotifyClosedFromJavaScript', dialog.returnValue);
}

async function openWithLifecycle(dialog: NTDialogElement): Promise<boolean> {
    if (dialog.open) {
        return false;
    }

    if (!await requestOpen(dialog)) {
        return false;
    }

    const opened = showDialog(dialog);
    if (opened) {
        await notifyOpened(dialog);
    }

    return opened;
}

async function closeWithLifecycle(dialog: NTDialogElement, returnValue: string | null = ''): Promise<boolean> {
    if (!dialog.open) {
        return false;
    }

    if (!await requestClose(dialog, returnValue)) {
        return false;
    }

    return await closeDialogElement(dialog, returnValue);
}

function onDocumentCommandClick(event: MouseEvent): void {
    const button = getCommandButton(event.target);
    if (!button) {
        return;
    }

    const dialogId = button.getAttribute('commandfor');
    if (!dialogId) {
        return;
    }

    const dialog = initializedDialogsById.get(dialogId);
    if (!dialog?.__ntDialogState) {
        return;
    }

    const command = button.getAttribute('command');
    if (command !== 'show-modal' && command !== 'close' && command !== 'request-close') {
        return;
    }

    event.preventDefault();
    if (command === 'show-modal') {
        void openWithLifecycle(dialog);
        return;
    }

    void closeWithLifecycle(dialog, getDialogReturnValueFromCommand(button));
}

function ensureDocumentCommandClickListener(): void {
    if (documentCommandClickListening) {
        return;
    }

    document.addEventListener('click', onDocumentCommandClick, true);
    documentCommandClickListening = true;
}

function releaseDocumentCommandClickListener(): void {
    if (!documentCommandClickListening || initializedDialogsById.size > 0) {
        return;
    }

    document.removeEventListener('click', onDocumentCommandClick, true);
    documentCommandClickListening = false;
}

function initializeDialog(dialog: NTDialogElement, dotNetRef: Maybe<unknown>): void {
    const effectiveDotNetRef = isDotNetReference(dotNetRef) ? dotNetRef : null;

    if (dialog.__ntDialogState) {
        dialog.__ntDialogState.dotNetRef = effectiveDotNetRef;
        if (dialog.__ntDialogState.dialogId !== dialog.id) {
            initializedDialogsById.delete(dialog.__ntDialogState.dialogId);
            dialog.__ntDialogState.dialogId = dialog.id;
            initializedDialogsById.set(dialog.id, dialog);
        }
        observeScrollableContent(dialog);
        openInitialModalDialog(dialog);
        return;
    }

    const state: NTDialogState = {
        autoOpenApplied: false,
        dotNetRef: effectiveDotNetRef,
        dialogId: dialog.id,
        mutationObserver: null,
        onCancel: event => {
            if (dialog.dataset.ntDialogCloseOnEscape === 'false') {
                event.preventDefault();
                event.stopPropagation();
                return;
            }

            if (state.dotNetRef) {
                event.preventDefault();
                void closeWithLifecycle(dialog);
            }
        },
        onClick: event => {
            if (event.target === dialog && dialog.dataset.ntDialogCloseOnBackdrop === 'true') {
                event.preventDefault();
                void closeWithLifecycle(dialog);
            }
        },
        onClose: () => {
            void notifyClosed(dialog);
        },
        resizeObserver: null,
        scrollUpdateFrame: null,
        scrollUpdateTimeout: null,
        scrollableContent: null
    };

    dialog.__ntDialogState = state;
    dialog.addEventListener('cancel', state.onCancel);
    dialog.addEventListener('click', state.onClick);
    dialog.addEventListener('close', state.onClose);
    initializedDialogsById.set(dialog.id, dialog);
    ensureDocumentCommandClickListener();
    observeScrollableContent(dialog);
    openInitialModalDialog(dialog);
}

function disposeDialog(dialog: NTDialogElement): void {
    const state = dialog.__ntDialogState;
    if (!state) {
        return;
    }

    dialog.removeEventListener('cancel', state.onCancel);
    dialog.removeEventListener('click', state.onClick);
    dialog.removeEventListener('close', state.onClose);
    initializedDialogsById.delete(state.dialogId);
    state.resizeObserver?.disconnect();
    state.mutationObserver?.disconnect();
    if (state.scrollUpdateFrame !== null && typeof window.cancelAnimationFrame === 'function') {
        window.cancelAnimationFrame(state.scrollUpdateFrame);
    }
    if (state.scrollUpdateTimeout !== null) {
        window.clearTimeout(state.scrollUpdateTimeout);
    }
    delete dialog.__ntDialogState;
    releaseDocumentCommandClickListener();
}

export function closeDialog(dialogOrId: DialogReference, returnValue: string | null = ''): Promise<boolean> {
    const dialog = getDialog(dialogOrId);
    return dialog ? closeWithLifecycle(dialog, returnValue) : Promise.resolve(false);
}

export function closeDialogFromBlazor(dialogOrId: DialogReference, returnValue: string | null = ''): Promise<boolean> {
    const dialog = getDialog(dialogOrId);
    return dialog ? closeDialogElement(dialog, returnValue) : Promise.resolve(false);
}

export function isOpen(dialogOrId: DialogReference): boolean {
    return getDialog(dialogOrId)?.open === true;
}

export function onDispose(element?: Maybe<Element>): void {
    for (const dialog of getDialogs(element)) {
        disposeDialog(dialog);
    }
}

export function onLoad(element?: Maybe<Element>, dotNetRef?: Maybe<unknown>): void {
    for (const dialog of getDialogs(element)) {
        initializeDialog(dialog, dotNetRef);
    }
}

export function onUpdate(element?: Maybe<Element>, dotNetRef?: Maybe<unknown>): void {
    for (const dialog of getDialogs(element)) {
        initializeDialog(dialog, dotNetRef);
    }
}

export function openDialog(dialogOrId: DialogReference): Promise<boolean> {
    const dialog = getDialog(dialogOrId);
    return dialog ? openWithLifecycle(dialog) : Promise.resolve(false);
}

export function openDialogFromBlazor(dialogOrId: DialogReference): boolean {
    const dialog = getDialog(dialogOrId);
    return dialog ? showDialog(dialog) : false;
}

export function requestCloseDialog(dialogOrId: DialogReference, returnValue: string | null = ''): Promise<boolean> {
    const dialog = getDialog(dialogOrId);
    return dialog ? closeWithLifecycle(dialog, returnValue) : Promise.resolve(false);
}
