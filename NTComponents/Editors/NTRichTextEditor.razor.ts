// Source of truth for the rich text editor module.
// Rebuild NTRichTextEditor.razor.js with: npm run build:rich-text-editor
type Maybe<T> = T | null | undefined;
type Alignment = '' | 'left' | 'center' | 'right' | 'justify';
type DotNetEditorMethod = 'UpdateValueFromJs' | 'CommitValueFromJs' | 'UpdateMarkupValueFromJs';

declare global {
    interface Window {
        DotNet: { createJSStreamReference(data: ArrayBufferView): unknown };
    }
}

interface InlineToolCloseOptions {
    focusSurface?: boolean;
    preserveSelection?: boolean;
}

interface RichTextEditorToolEditorState {
    selectionRange: Range | null;
    toolStates: Map<string, unknown>;
}

interface RichTextEditorToolContext<TState> {
    element: HTMLElement;
    editorState: RichTextEditorToolEditorState;
    toolState: TState;
    host: RichTextEditorToolHost;
}

interface RichTextEditorToolHost {
    getSurface(element: Maybe<ParentNode>): HTMLElement | null;
    getSelectionElement(surface: Maybe<HTMLElement>): Element | null;
    getFocusedToolCommand(element: HTMLElement): string | null;
    getToolbarButton(element: Maybe<ParentNode>, command: string, value?: string): HTMLButtonElement | null;
    setToolbarButtonPressed(element: HTMLElement, command: string, isPressed: boolean, value?: string): void;
    getToolPanel(element: Maybe<ParentNode>, command: string): HTMLElement | null;
    setToolPanelOpen(element: HTMLElement, command: string, isOpen: boolean): void;
    saveSelectionRange(surface: HTMLElement, editorState: RichTextEditorToolEditorState): void;
    restoreSelectionRange(surface: HTMLElement, editorState: RichTextEditorToolEditorState): void;
    syncValueFromSurface(element: HTMLElement, notifyDotNet: boolean): void;
    updateToolbarState(element: HTMLElement): void;
    closeOtherTools(element: HTMLElement, editorState: RichTextEditorToolEditorState, exceptCommand?: string | null): void;
}

interface RichTextEditorToolPlugin<TState> {
    command: string;
    createState(): TState;
    bind(context: RichTextEditorToolContext<TState>): void;
    unbind(context: RichTextEditorToolContext<TState>): void;
    setDisabled?(context: RichTextEditorToolContext<TState>, disabled: boolean): void;
    execute?(context: RichTextEditorToolContext<TState>, value?: string): boolean;
    close?(context: RichTextEditorToolContext<TState>, options?: InlineToolCloseOptions): void;
    syncState?(context: RichTextEditorToolContext<TState>): void;
    /** Elements this tool edits. Clicking one in the surface calls openForTarget so its settings show. */
    targetSelector?: string;
    openForTarget?(context: RichTextEditorToolContext<TState>): void;
}

interface DotNetEditorRef {
    invokeMethodAsync(methodName: DotNetEditorMethod, sequence: number, value: string): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'ReceiveValueStreamFromJs', targetMethodName: DotNetEditorMethod, sequence: number, value: unknown): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'BlurFromJs'): Promise<unknown> | void;
}

interface ImageDetails {
    src: string;
    alt: string;
    title: string;
    width: string;
    height: string;
}

interface EditorState extends RichTextEditorToolEditorState {
    dotNetRef: DotNetEditorRef | null;
    isDisposed: boolean;
    pendingSyncFrameId: number | null;
    hasUncommittedEdit: boolean;
    requiresInitialRender: boolean;
    toolbarButtons: HTMLButtonElement[];
    toolCommands: string[];
    lastValue: string;
    lastHtml: string;
    renderedValue: string;
    sentValueCount: number;
    sourceMode: boolean;
    sourceEmbeddedData: string[];
    onInput: () => void;
    onSourceInput: () => void;
    onSourceScroll: () => void;
    onWindowResize: () => void;
    onFocus: () => void;
    onFocusIn: () => void;
    onFocusOut: (event: FocusEvent) => void;
    onKeyUp: () => void;
    onMouseUp: () => void;
    onClick: (event: MouseEvent) => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onPanelKeyDown: (event: KeyboardEvent) => void;
    onToolbarMouseDown: (event: MouseEvent) => void;
}

type RegisteredTool = RichTextEditorToolPlugin<unknown>;
type RegisteredToolContext = ReturnType<typeof createRichTextEditorToolContext<unknown>>;

const richTextEditorToolRegistry: { tools: Map<string, RegisteredTool>; onChange: (() => void) | null } = {
    tools: new Map<string, RegisteredTool>(),
    onChange: null
};
const editorState = new WeakMap<HTMLElement, EditorState>();
const blockNodeTags = new Set<string>(['DIV', 'P', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'UL', 'OL', 'BLOCKQUOTE', 'PRE', 'TABLE', 'IFRAME']);
const sourceBlockTags = new Set<string>([...blockNodeTags, 'CAPTION', 'IMG', 'LI', 'TBODY', 'TD', 'TFOOT', 'TH', 'THEAD', 'TR']);
const blockSelector = 'h1, h2, h3, h4, h5, h6, pre, blockquote, table, th, td, iframe, li, p, div';
const blockLevelSelector = 'h1, h2, h3, h4, h5, h6, pre, blockquote, table, caption, thead, tbody, tfoot, tr, th, td, li, ul, ol, p, div';
const defaultTableBorderColor = '#94a3b8';
const defaultTextColor = '#1d4ed8';
const editorElements = new Set<HTMLElement>();
let selectionChangeRegistered = false;
let uncommittedEditHandlersRegistered = false;
let activeEditor: HTMLElement | null = null;
let prerenderFocusedEditorIndex = -1;
// Blazor Server closes the circuit on a message over 32 KB by default. JSON can spend 6 bytes on one character (\u0001),
// so values up to this length always fit; longer ones are streamed.
const maxInlineValueLength = 5000;
// Module-wide so a recreated editor state never reuses a number its .NET component has already seen.
let valueSequence = 0;

export function setRichTextEditorToolRegistryChangedCallback(callback: (() => void) | null): void {
    richTextEditorToolRegistry.onChange = callback;
}

export function registerRichTextEditorTool<TState>(tool: RichTextEditorToolPlugin<TState>): RichTextEditorToolPlugin<TState> {
    richTextEditorToolRegistry.tools.set(tool.command, tool as RegisteredTool);
    richTextEditorToolRegistry.onChange?.();
    return tool;
}

export function getRichTextEditorTool(command: string): RegisteredTool | null {
    return richTextEditorToolRegistry.tools.get(command) ?? null;
}

export function getRichTextEditorToolState<TState>(state: RichTextEditorToolEditorState, tool: RichTextEditorToolPlugin<TState>): TState {
    const existingState = state.toolStates.get(tool.command);
    if (existingState) {
        return existingState as TState;
    }

    const nextState = tool.createState();
    state.toolStates.set(tool.command, nextState);
    return nextState;
}

export function createRichTextEditorToolContext<TState>(element: HTMLElement, state: RichTextEditorToolEditorState, host: RichTextEditorToolHost, tool: RichTextEditorToolPlugin<TState>): RichTextEditorToolContext<TState> {
    return {
        element,
        editorState: state,
        host,
        toolState: getRichTextEditorToolState(state, tool)
    };
}

function toArray<T>(values: Maybe<Iterable<T> | ArrayLike<T>>): T[] {
    return values ? Array.from(values) : [];
}

function qs<T extends Element>(root: Maybe<ParentNode>, selector: string): T | null {
    return (root?.querySelector?.(selector) as T | null) ?? null;
}

function isElement(value: unknown): value is Element {
    return value instanceof Element;
}

function isHtmlButtonElement(value: unknown): value is HTMLButtonElement {
    return value instanceof HTMLButtonElement;
}

function isHtmlElement(value: unknown): value is HTMLElement {
    return value instanceof HTMLElement;
}

function isHtmlInputElement(value: unknown): value is HTMLInputElement {
    return value instanceof HTMLInputElement;
}

function setDisabled(control: Maybe<HTMLButtonElement | HTMLInputElement>, disabled: boolean): void {
    if (control) {
        control.disabled = disabled;
    }
}

function isHtmlSpanElement(value: unknown): value is HTMLSpanElement {
    return value instanceof HTMLSpanElement;
}

function isTableCellElement(value: unknown): value is HTMLTableCellElement {
    return value instanceof HTMLTableCellElement;
}

function isTableRowElement(value: unknown): value is HTMLTableRowElement {
    return value instanceof HTMLTableRowElement;
}

function getTableCells(row: Maybe<HTMLTableRowElement>): HTMLTableCellElement[] {
    return toArray(row?.children).filter(isTableCellElement);
}

function clearPendingSync(state: Maybe<EditorState>): void {
    if (!state || state.pendingSyncFrameId === null) {
        return;
    }

    window.cancelAnimationFrame(state.pendingSyncFrameId);
    state.pendingSyncFrameId = null;
}

function placeCaretAtEnd(surface: HTMLElement): void {
    surface.focus({ preventScroll: true });

    const selection = window.getSelection?.();
    if (!selection) {
        return;
    }

    const range = document.createRange();
    range.selectNodeContents(surface);
    range.collapse(false);
    selection.removeAllRanges();
    selection.addRange(range);
}

function getSurface(element: Maybe<ParentNode>): HTMLElement | null {
    return qs<HTMLElement>(element, '.nt-rich-text-editor-surface');
}

function getHtmlSource(element: Maybe<ParentNode>): HTMLTextAreaElement | null {
    return qs<HTMLTextAreaElement>(element, '[data-role="html-source"]');
}

function syncHtmlSourceScroll(source: HTMLTextAreaElement): void {
    const code = source.parentElement?.querySelector<HTMLElement>('.nt-rich-text-editor-source-highlight code');
    if (code) {
        code.style.transform = `translate(${-source.scrollLeft}px, ${-source.scrollTop}px)`;
    }
}

function updateHtmlSourceView(source: HTMLTextAreaElement): void {
    const code = source.parentElement?.querySelector<HTMLElement>('.nt-rich-text-editor-source-highlight code');
    if (code) {
        const fragment = document.createDocumentFragment();
        const value = source.value;
        const append = (text: string, kind?: string): void => {
            if (!text) {
                return;
            }

            const node = kind ? document.createElement('span') : document.createTextNode(text);
            if (kind) {
                (node as HTMLElement).className = `syntax-${kind}`;
            }
            node.textContent = text;
            fragment.appendChild(node);
        };

        let index = 0;
        while (index < value.length) {
            if (value.startsWith('<!--', index)) {
                const end = value.indexOf('-->', index + 4);
                const next = end < 0 ? value.length : end + 3;
                append(value.slice(index, next), 'comment');
                index = next;
                continue;
            }

            if (/^<![A-Za-z]/.test(value.slice(index))) {
                const end = value.indexOf('>', index + 2);
                const next = end < 0 ? value.length : end + 1;
                append(value.slice(index, next), 'tag');
                index = next;
                continue;
            }

            const tag = value[index] === '<' ? /^<(\/?)([A-Za-z][\w:-]*)/.exec(value.slice(index)) : null;
            if (!tag) {
                const next = value.indexOf('<', index + 1);
                const end = next < 0 ? value.length : next;
                append(value.slice(index, end));
                index = end;
                continue;
            }

            append(`<${tag[1]}`, 'tag');
            append(tag[2], 'tag');
            index += tag[0].length;
            let expectsValue = false;
            while (index < value.length && value[index] !== '>') {
                const remaining = value.slice(index);
                const whitespace = /^\s+/.exec(remaining);
                if (whitespace) {
                    append(whitespace[0]);
                    index += whitespace[0].length;
                } else if (value[index] === '=') {
                    append('=', 'tag');
                    expectsValue = true;
                    index++;
                } else if (value[index] === '/' && value[index + 1] === '>') {
                    append('/', 'tag');
                    index++;
                } else if (value[index] === '"' || value[index] === "'") {
                    const quote = value[index];
                    const end = value.indexOf(quote, index + 1);
                    const next = end < 0 ? value.length : end + 1;
                    append(value.slice(index, next), 'value');
                    index = next;
                    expectsValue = false;
                } else {
                    const token = /^[^\s=<>"']+/.exec(remaining);
                    if (token) {
                        append(token[0], expectsValue ? 'value' : 'attribute');
                        index += token[0].length;
                        expectsValue = false;
                    } else {
                        append(value[index]);
                        index++;
                    }
                }
            }
            if (value[index] === '>') {
                append('>', 'tag');
                index++;
            }
        }

        if (value.endsWith('\n')) {
            append('\u200b');
        }
        code.replaceChildren(fragment);
    }

    source.style.height = 'auto';
    source.style.height = `${source.scrollHeight}px`;
    if (code) {
        code.style.width = `${source.clientWidth}px`;
        syncHtmlSourceScroll(source);
    }
}

// The source view shows each embedded image as a short token so base64 data does not bury the markup. Tokens are
// display-only: reading the source back swaps the original data URLs in, and an unknown token is left as typed.
const embeddedDataPattern = /data:([\w.+/-]+);base64,([A-Za-z0-9+/=]{64,})/g;
const embeddedDataTokenPattern = /\[image-data-(\d+)[^\]]*\]/g;

function writeHtmlSource(source: HTMLTextAreaElement, state: EditorState, html: string): void {
    source.value = formatHtmlSource(html).replace(embeddedDataPattern, (_match, type: string, data: string) => {
        const index = state.sourceEmbeddedData.includes(data) ? state.sourceEmbeddedData.indexOf(data) : state.sourceEmbeddedData.push(data) - 1;
        return `data:${type};base64,[image-data-${index + 1}, ${Math.ceil((data.length * 3) / 4 / 1024)} KB]`;
    });
}

function readHtmlSource(source: HTMLTextAreaElement, state: EditorState): string {
    return flattenEditorHtml(source.value.replace(embeddedDataTokenPattern, (token, index: string) => state.sourceEmbeddedData[Number(index) - 1] ?? token));
}

function setHtmlSourceMode(element: HTMLElement, state: EditorState, enabled: boolean): void {
    const surface = getSurface(element);
    const source = getHtmlSource(element);
    if (!surface || !source || state.sourceMode === enabled) {
        return;
    }

    if (enabled) {
        syncValueFromSurface(element, true);
        closeOtherTools(element, state);
        state.sourceEmbeddedData = [];
        writeHtmlSource(source, state, state.lastHtml);
    } else {
        surface.innerHTML = readHtmlSource(source, state);
        state.selectionRange = null;
    }

    state.sourceMode = enabled;
    surface.hidden = enabled;
    source.hidden = !enabled;
    for (const button of state.toolbarButtons) {
        button.disabled = enabled && button.dataset.command !== 'editHtml';
    }
    toolHost.getToolbarButton(element, 'editHtml')?.setAttribute('aria-pressed', `${enabled}`);
    if (enabled) {
        updateHtmlSourceView(source);
        source.focus();
    } else {
        syncValueFromSurface(element, true);
        surface.focus({ preventScroll: true });
    }
}

function getSourceValueElement(element: Maybe<ParentNode>): HTMLDivElement | null {
    return qs<HTMLDivElement>(element, '.nt-rich-text-editor-value');
}

function getHiddenInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '.nt-rich-text-editor-hidden-input');
}

function getToolbarButtons(element: Maybe<ParentNode>): HTMLButtonElement[] {
    return toArray(element?.querySelectorAll?.('.nt-rich-text-editor-toolbar .nt-rich-text-editor-toolbar-button')).filter(isHtmlButtonElement);
}

function isAwaitingDotNet(element: Maybe<HTMLElement>): boolean {
    return element?.dataset.interactive === 'true' && !editorState.get(element)?.dotNetRef;
}

// An interactive editor stays read-only until .NET can receive its value; the page script may start the module first.
function isEditorEditable(element: Maybe<HTMLElement>): boolean {
    return element?.dataset.editable === 'true' && !isAwaitingDotNet(element);
}

function getEditorIndex(element: HTMLElement): number {
    return toArray(document.querySelectorAll('nt-rich-text-editor')).indexOf(element);
}

// Interactivity replaces the prerendered DOM, so focus taken while waiting has to be carried over to the new surface.
function shouldTakeFocus(element: HTMLElement, surface: HTMLElement): boolean {
    const wasFocusedWhilePrerendered = prerenderFocusedEditorIndex === getEditorIndex(element)
        && (document.activeElement === document.body || document.activeElement === null);
    if (wasFocusedWhilePrerendered) {
        prerenderFocusedEditorIndex = -1;
    }

    return wasFocusedWhilePrerendered || document.activeElement === surface;
}

function setActiveEditor(element: Maybe<HTMLElement>): void {
    activeEditor = element ?? null;
}

function clearActiveEditor(element: Maybe<HTMLElement>): void {
    if (activeEditor === element) {
        activeEditor = null;
    }
}

// A streamed value is read asynchronously, so a later small value can reach .NET first. Each call carries a rising
// sequence and .NET drops values older than the newest it applied. Chaining sends instead would queue every keystroke
// behind a slow stream and lose sends still queued when the page goes away.
function sendValue(state: EditorState, methodName: DotNetEditorMethod, value: string): void {
    const dotNetRef = state.dotNetRef;
    if (!dotNetRef) {
        return;
    }

    // Markup-only updates leave the bound value alone, so renders do not have to wait for them.
    const isValue = methodName !== 'UpdateMarkupValueFromJs';
    if (isValue) {
        state.sentValueCount++;
    }

    const sequence = ++valueSequence;
    new Promise<unknown>(resolve => resolve(value.length > maxInlineValueLength
        ? dotNetRef.invokeMethodAsync('ReceiveValueStreamFromJs', methodName, sequence, window.DotNet.createJSStreamReference(new TextEncoder().encode(value)))
        : dotNetRef.invokeMethodAsync(methodName, sequence, value)))
        .catch(() => {
            // A call that never reached .NET must not make later renders look stale.
            if (isValue) {
                state.sentValueCount--;
            }
        });
}

function extractUrlScheme(value: string): string {
    const match = value.match(/^([a-z][a-z0-9+.-]*):/i);
    return match?.[1]?.toLowerCase() ?? '';
}

function normalizeSafeUrl(value: unknown, kind: 'link' | 'iframe' | 'image'): string {
    const url = normalizeUrlScheme(value, kind);
    return kind === 'iframe' && !isCrossOriginHttpUrl(url) ? '' : url;
}

// The iframe sandbox grants allow-same-origin, which only stays a sandbox for another origin's content.
function isCrossOriginHttpUrl(url: string): boolean {
    try {
        const resolved = new URL(url, document.baseURI);
        return ['http:', 'https:'].includes(resolved.protocol) && resolved.origin !== window.location.origin;
    }
    catch {
        return false;
    }
}

function normalizeUrlScheme(value: unknown, kind: 'link' | 'iframe' | 'image'): string {
    // Browsers ignore control characters in URLs, so "java\tscript:" must not slip past the scheme check.
    const trimmed = `${value ?? ''}`.replace(/[\u0000-\u001F\u007F]/g, '').trim();
    if (!trimmed) {
        return '';
    }

    // A typed "example.com/page" would otherwise resolve against the host page.
    if (kind !== 'image' && /^[a-z\d-]+(?:\.[a-z\d-]+)*\.[a-z]{2,}(?::\d+)?(?:[/?#]|$)/i.test(trimmed)) {
        return `https://${trimmed}`;
    }

    if (trimmed.startsWith('#')
        || trimmed.startsWith('/')
        || trimmed.startsWith('./')
        || trimmed.startsWith('../')
        || trimmed.startsWith('?')) {
        return trimmed;
    }

    const scheme = extractUrlScheme(trimmed);
    if (!scheme) {
        return trimmed;
    }

    if (kind === 'link' && ['http', 'https', 'mailto', 'tel'].includes(scheme)) {
        return trimmed;
    }

    if (kind === 'iframe' && ['http', 'https'].includes(scheme)) {
        return trimmed;
    }

    if (kind === 'image' && ['http', 'https', 'data', 'blob'].includes(scheme)) {
        return trimmed;
    }

    return '';
}

function unbindToolbarButtons(buttons: Maybe<HTMLButtonElement[]>, state: Maybe<EditorState>): void {
    if (!state) {
        return;
    }

    for (const button of buttons ?? []) {
        button.removeEventListener('mousedown', state.onToolbarMouseDown);
        button.removeEventListener('click', handleToolbarCommand);
    }
}

function bindToolbarButtons(element: Maybe<HTMLElement>, state: EditorState): void {
    unbindToolbarButtons(state?.toolbarButtons, state);

    const buttons = getToolbarButtons(element);
    for (const button of buttons) {
        button.addEventListener('mousedown', state.onToolbarMouseDown);
        button.addEventListener('click', handleToolbarCommand);
    }

    state.toolbarButtons = buttons;
}

function getChildNodeIndex(node: Node): number {
    const parentNode = node.parentNode;
    return parentNode ? toArray<Node>(parentNode.childNodes).indexOf(node) : -1;
}

function getBoundaryAdjacentElement(surface: HTMLElement, container: Node, offset: number, direction: 'before' | 'after'): Element | null {
    let currentNode: Node | null = container;
    let currentOffset = offset;

    while (currentNode) {
        if (currentNode.nodeType === Node.TEXT_NODE) {
            const textLength = currentNode.textContent?.length ?? 0;
            if (direction === 'before') {
                if (currentOffset !== 0) {
                    return null;
                }
            } else if (currentOffset !== textLength) {
                return null;
            }

            const textNodeIndex = getChildNodeIndex(currentNode);
            currentNode = currentNode.parentNode;
            if (!currentNode || textNodeIndex < 0) {
                return null;
            }

            currentOffset = direction === 'before' ? textNodeIndex : textNodeIndex + 1;
            continue;
        }

        if (currentNode.nodeType !== Node.ELEMENT_NODE) {
            return null;
        }

        const currentElement = currentNode as Element;
        const childNodes = toArray(currentElement.childNodes);
        const candidateNode = direction === 'before' ? childNodes[currentOffset - 1] : childNodes[currentOffset];
        if (candidateNode) {
            // Only inline neighbours (images, links, formatting) describe the caret; a sibling block does not.
            return candidateNode instanceof Element && !candidateNode.matches(blockLevelSelector) && surface.contains(candidateNode) ? candidateNode : null;
        }

        if (currentElement === surface || currentElement.matches(blockLevelSelector)) {
            return null;
        }

        const elementNodeIndex = getChildNodeIndex(currentElement);
        currentNode = currentElement.parentNode;
        if (!currentNode || elementNodeIndex < 0) {
            return null;
        }

        currentOffset = direction === 'before' ? elementNodeIndex : elementNodeIndex + 1;
    }

    return null;
}

function getSelectionElement(surface: Maybe<HTMLElement>): Element | null {
    if (!surface) {
        return null;
    }

    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    let node: Node | null = selection?.anchorNode ?? surface;

    // A clicked image is selected as the single node between two offsets of its parent.
    const selectedNode = range && !range.collapsed && range.startContainer === range.endContainer && range.endOffset - range.startOffset === 1
        ? range.startContainer.childNodes[range.startOffset]
        : null;
    if (selectedNode instanceof Element && surface.contains(selectedNode)) {
        return selectedNode;
    }

    if (range?.collapsed) {
        const adjacentElement = getBoundaryAdjacentElement(surface, range.startContainer, range.startOffset, 'before')
            ?? getBoundaryAdjacentElement(surface, range.startContainer, range.startOffset, 'after');
        if (adjacentElement) {
            return adjacentElement;
        }
    }

    if (node?.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    if (node instanceof Element && surface.contains(node)) {
        return node;
    }

    return null;
}

function getCurrentBlockElement(surface: Maybe<HTMLElement>): Element | null {
    const selectionElement = getSelectionElement(surface);
    if (selectionElement) {
        return selectionElement.closest(blockSelector) ?? selectionElement;
    }

    return null;
}

function getCurrentAlignment(surface: Maybe<HTMLElement>, blockElement: Maybe<Element>): Alignment {
    const selectionElement = getSelectionElement(surface) ?? blockElement;
    let node = selectionElement;

    while (node instanceof Element && surface?.contains(node)) {
        const alignment = getElementAlignment(node);
        if (alignment) {
            return alignment;
        }

        node = node.parentElement;
    }

    return getElementAlignment(blockElement) || 'left';
}

// Bounded by the surface so a host page that wraps the editor in an li or b does not count.
function closestInSurface(surface: Maybe<HTMLElement>, selector: string): Element | null {
    const match = getSelectionElement(surface)?.closest?.(selector) ?? null;
    return match && match !== surface && surface?.contains(match) ? match : null;
}

function hasAncestorTag(surface: Maybe<HTMLElement>, tagNames: string): boolean {
    return Boolean(closestInSurface(surface, tagNames));
}

function getElementTextColor(element: unknown): string {
    if (!(element instanceof Element)) {
        return '';
    }

    const styleColor = element instanceof HTMLElement ? element.style.color.trim() : '';
    if (styleColor) {
        return styleColor;
    }

    const styleAttribute = element.getAttribute?.('style') ?? '';
    const match = styleAttribute.match(/color:\s*([^;]+)/i);
    return match?.[1]?.trim?.() ?? '';
}

function normalizeHexColor(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    if (/^#[\da-f]{6}$/i.test(trimmed)) {
        return trimmed.toLowerCase();
    }

    if (/^#[\da-f]{3}$/i.test(trimmed)) {
        return `#${trimmed[1]}${trimmed[1]}${trimmed[2]}${trimmed[2]}${trimmed[3]}${trimmed[3]}`.toLowerCase();
    }

    return '';
}

function convertRgbChannelToHex(value: string): string {
    const numericValue = Number.parseInt(value, 10);
    if (Number.isNaN(numericValue)) {
        return '';
    }

    return Math.min(Math.max(numericValue, 0), 255).toString(16).padStart(2, '0');
}

function normalizeTextColorValue(value: unknown): string {
    const hexColor = normalizeHexColor(value);
    if (hexColor) {
        return hexColor;
    }

    const rgbMatch = `${value ?? ''}`.trim().match(/^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})(?:\s*,\s*[\d.]+\s*)?\)$/i);
    if (!rgbMatch) {
        return '';
    }

    const red = convertRgbChannelToHex(rgbMatch[1]);
    const green = convertRgbChannelToHex(rgbMatch[2]);
    const blue = convertRgbChannelToHex(rgbMatch[3]);
    return red && green && blue ? `#${red}${green}${blue}` : '';
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll('\'', '&#39;');
}

// A document without a browsing context: probing a clone here loads no images or frames.
const inertDocument = document.implementation.createHTMLDocument('');
const allowedHtmlTags = new Set(['A', 'B', 'BLOCKQUOTE', 'BR', 'CAPTION', 'CODE', 'DEL', 'DIV', 'EM', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'I', 'IFRAME', 'IMG', 'LI', 'OL', 'P', 'PRE', 'S', 'SPAN', 'STRIKE', 'STRONG', 'TABLE', 'TBODY', 'TD', 'TFOOT', 'TH', 'THEAD', 'TR', 'U', 'UL']);
const droppedHtmlTags = new Set(['SCRIPT', 'STYLE', 'TEMPLATE']);

function unwrapUnsafeElement(element: Element): void {
    if (droppedHtmlTags.has(element.tagName)) {
        element.remove();
        return;
    }

    element.replaceWith(...toArray(element.childNodes));
}

function sanitizeDimensionAttribute(value: unknown, allowPercent = false): string {
    const trimmed = `${value ?? ''}`.trim();
    if (/^\d+$/.test(trimmed)) {
        return trimmed;
    }

    return allowPercent && /^\d+(?:\.\d+)?%$/.test(trimmed) ? trimmed : '';
}

function sanitizeElementStyle(element: HTMLElement): void {
    const color = normalizeTextColorValue(element.style.color);
    const textAlign = normalizeAlignment(element.style.textAlign);
    const tableBorderColor = normalizeTableBorderColor(element.style.getPropertyValue('--nt-rich-text-table-border-color'));
    element.removeAttribute('style');

    if (color) {
        element.style.color = color;
    }

    if (textAlign) {
        element.style.textAlign = textAlign;
    }

    if (tableBorderColor && element instanceof HTMLTableElement) {
        element.style.setProperty('--nt-rich-text-table-border-color', tableBorderColor);
    }
}

function sanitizeElementAttributes(element: Element): void {
    const tagName = element.tagName;
    for (const attribute of toArray(element.attributes)) {
        const name = attribute.name.toLowerCase();
        if (name.startsWith('on') || name === 'srcdoc') {
            element.removeAttribute(attribute.name);
            continue;
        }

        if (name === 'style' && element instanceof HTMLElement) {
            sanitizeElementStyle(element);
            continue;
        }

        if (name === 'role') {
            if (tagName !== 'SPAN' || element.getAttribute('role') !== 'group') {
                element.removeAttribute(attribute.name);
            }
            continue;
        }

        // Firefox justify commands write align="..."; keep one representation of alignment.
        if (name === 'align') {
            const alignment = normalizeAlignment(attribute.value);
            element.removeAttribute(attribute.name);
            if (alignment && element instanceof HTMLElement && !(element instanceof HTMLImageElement) && !element.style.textAlign) {
                element.style.textAlign = alignment;
            }
            continue;
        }

        if (!['alt', 'aria-label', 'data-border-color', 'data-language', 'height', 'href', 'loading', 'scope', 'src', 'title', 'width'].includes(name)) {
            element.removeAttribute(attribute.name);
        }
    }

    if (element instanceof HTMLAnchorElement) {
        const href = normalizeSafeUrl(element.getAttribute('href') ?? '', 'link');
        href ? element.setAttribute('href', href) : element.removeAttribute('href');
    }

    if (element instanceof HTMLImageElement) {
        const src = normalizeSafeUrl(element.getAttribute('src') ?? '', 'image');
        src ? element.setAttribute('src', src) : element.remove();
        element.setAttribute('alt', element.getAttribute('alt') ?? '');
        const width = normalizeImageDimension(element.getAttribute('width') ?? '');
        const height = normalizeImageDimension(element.getAttribute('height') ?? '');
        width ? element.setAttribute('width', width) : element.removeAttribute('width');
        height ? element.setAttribute('height', height) : element.removeAttribute('height');
    }

    if (element instanceof HTMLIFrameElement) {
        const src = normalizeSafeUrl(element.getAttribute('src') ?? '', 'iframe');
        src ? element.setAttribute('src', src) : element.remove();
        element.setAttribute('title', element.getAttribute('title')?.trim() || defaultIframeTitle);
        element.setAttribute('width', sanitizeDimensionAttribute(element.getAttribute('width'), true) || defaultIframeWidth);
        element.setAttribute('height', sanitizeDimensionAttribute(element.getAttribute('height')) || defaultIframeHeight);
        element.setAttribute('loading', 'lazy');
        element.setAttribute('sandbox', iframeSandbox);
    }

    if (element instanceof HTMLTableElement) {
        const borderColor = getTableBorderColor(element);
        if (borderColor) {
            element.setAttribute('data-border-color', borderColor);
            element.style.setProperty('--nt-rich-text-table-border-color', borderColor);
        }
    }

    if (element instanceof HTMLTableCellElement && element.tagName === 'TH') {
        const scope = element.getAttribute('scope')?.trim().toLowerCase();
        if (scope && !['col', 'row', 'colgroup', 'rowgroup'].includes(scope)) {
            element.removeAttribute('scope');
        }
    }
}

function sanitizeHtmlFragment(root: ParentNode): void {
    for (const element of toArray(root.querySelectorAll('*'))) {
        if (!allowedHtmlTags.has(element.tagName)) {
            unwrapUnsafeElement(element);
            continue;
        }

        sanitizeElementAttributes(element);
        // Chrome wraps moved text in style spans; once the styles are stripped the span carries nothing.
        if (element.tagName === 'SPAN' && element.attributes.length === 0) {
            unwrapElement(element);
        }
    }
}

// Structural repairs for markup produced by browser editing commands.
function normalizeEditorStructure(surface: HTMLElement): void {
    // Chrome nests lists inside the paragraph they were created from, which no parser can round-trip.
    for (const wrapper of toArray(surface.querySelectorAll('p'))) {
        if (wrapper.querySelector(':scope > :is(ul, ol, table, pre, blockquote, div, p, h1, h2, h3, h4, h5, h6)')) {
            unwrapElement(wrapper);
        }
    }

    // Outdent, list removal, and typing into an empty editor leave bare inline content at the root.
    let run: HTMLParagraphElement | null = null;
    for (const node of toArray(surface.childNodes)) {
        const isInline = node.nodeType === Node.TEXT_NODE || (node instanceof Element && !node.matches(blockLevelSelector));
        if (!isInline) {
            run = null;
            continue;
        }

        if (!run) {
            if (node.nodeType === Node.TEXT_NODE && !node.textContent?.trim()) {
                continue;
            }

            run = surface.ownerDocument.createElement('p');
            node.before(run);
        }

        run.appendChild(node);
    }
}

// Cleans the live surface in place. Existing text nodes are moved, never recreated, so the caret survives.
function cleanSurface(surface: HTMLElement, repairStructure: boolean): void {
    const clean = (root: HTMLElement): void => {
        sanitizeHtmlFragment(root);
        if (repairStructure) {
            normalizeEditorStructure(root);
        }
    };

    const probe = inertDocument.importNode(surface, true) as HTMLElement;
    clean(probe);
    if (probe.innerHTML === surface.innerHTML) {
        return;
    }

    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    const saved = range && surface.contains(range.commonAncestorContainer)
        ? [toTextBoundary(range.startContainer, range.startOffset), toTextBoundary(range.endContainer, range.endOffset)]
        : null;

    clean(surface);
    if (!saved || !selection) {
        return;
    }

    const [start, end] = saved;
    if (!surface.contains(start.node) || !surface.contains(end.node)) {
        placeCaretAtEnd(surface);
        return;
    }

    const restored = document.createRange();
    restored.setStart(start.node, Math.min(start.offset, getNodeLength(start.node)));
    restored.setEnd(end.node, Math.min(end.offset, getNodeLength(end.node)));
    selection.removeAllRanges();
    selection.addRange(restored);
}

function getNodeLength(node: Node): number {
    return node.nodeType === Node.TEXT_NODE ? node.textContent?.length ?? 0 : node.childNodes.length;
}

// Element boundaries shift when siblings are unwrapped, so anchor them to an adjacent text node when one exists.
function toTextBoundary(node: Node, offset: number): { node: Node; offset: number } {
    if (node.nodeType !== Node.TEXT_NODE) {
        const after = node.childNodes[offset];
        if (after?.nodeType === Node.TEXT_NODE) {
            return { node: after, offset: 0 };
        }

        const before = node.childNodes[offset - 1];
        if (before?.nodeType === Node.TEXT_NODE) {
            return { node: before, offset: before.textContent?.length ?? 0 };
        }
    }

    return { node, offset };
}

function sanitizeEditorHtml(html: string): string {
    const template = document.createElement('template');
    template.innerHTML = html;
    sanitizeHtmlFragment(template.content);
    return template.innerHTML;
}

function hasOnlySourceBlocks(parent: ParentNode): boolean {
    const children = Array.from(parent.childNodes);
    return children.some((child) => child instanceof Element)
        && children.every((child) => child instanceof Element
            ? sourceBlockTags.has(child.tagName)
            : child.nodeType === Node.TEXT_NODE && !child.textContent?.trim());
}

function flattenEditorHtml(html: string): string {
    const template = document.createElement('template');
    template.innerHTML = html;
    sanitizeHtmlFragment(template.content);

    const flattenChildren = (parent: ParentNode, preserveWhitespace = false): void => {
        const preserve = preserveWhitespace || parent instanceof Element && (parent.tagName === 'PRE' || parent.tagName === 'CODE');
        const removeFormattingSpace = hasOnlySourceBlocks(parent);
        for (const child of Array.from(parent.childNodes)) {
            if (child.nodeType === Node.TEXT_NODE && !preserve) {
                const value = (child.textContent ?? '').replace(/[ \t\r\n\f]+/g, ' ');
                if (value === ' ' && removeFormattingSpace) {
                    child.parentNode?.removeChild(child);
                } else {
                    child.textContent = value;
                }
            } else if (child instanceof Element) {
                flattenChildren(child, preserve);
            }
        }
    };

    flattenChildren(template.content);
    return template.innerHTML.replace(/\r\n|[\r\n]/g, '&#10;').replace(/\t/g, '&#9;');
}

function formatHtmlSource(html: string): string {
    const template = document.createElement('template');
    template.innerHTML = html;
    if (!hasOnlySourceBlocks(template.content)) {
        return template.innerHTML;
    }

    const formatElement = (element: Element, depth: number): string => {
        const indent = '  '.repeat(depth);
        if (!hasOnlySourceBlocks(element)) {
            return indent + element.outerHTML;
        }

        const closingTag = `</${element.tagName.toLowerCase()}>`;
        const openingTag = (element.cloneNode(false) as Element).outerHTML.slice(0, -closingTag.length);
        const children = Array.from(element.children).map((child) => formatElement(child, depth + 1)).join('\n');
        return `${indent}${openingTag}\n${children}\n${indent}${closingTag}`;
    };

    return Array.from(template.content.children).map((element) => formatElement(element, 0)).join('\n');
}

function normalizeImageDimension(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    return /^\d+$/.test(trimmed) ? trimmed : '';
}

function normalizeTableBorderColor(value: unknown): string {
    const trimmed = `${value ?? ''}`.trim();
    return /^#[\da-f]{6}$/i.test(trimmed) ? trimmed.toLowerCase() : '';
}

function buildTableStyleAttribute(borderColor: unknown): string {
    const normalizedBorderColor = normalizeTableBorderColor(borderColor);
    if (!normalizedBorderColor) {
        return '';
    }

    const encodedBorderColor = escapeHtml(normalizedBorderColor);
    return ` data-border-color="${encodedBorderColor}" style="--nt-rich-text-table-border-color:${encodedBorderColor};"`;
}

function buildImageHtml({ src, alt = '', title = '', width = '', height = '' }: ImageDetails): string {
    const normalizedSource = normalizeSafeUrl(src, 'image');
    if (!normalizedSource) {
        return '';
    }

    const normalizedWidth = normalizeImageDimension(width);
    const normalizedHeight = normalizeImageDimension(height);
    const titleAttribute = title.trim() ? ` title="${escapeHtml(title.trim())}"` : '';
    const widthAttribute = normalizedWidth ? ` width="${escapeHtml(normalizedWidth)}"` : '';
    const heightAttribute = normalizedHeight ? ` height="${escapeHtml(normalizedHeight)}"` : '';
    return `<img src="${escapeHtml(normalizedSource)}" alt="${escapeHtml(alt)}"${titleAttribute}${widthAttribute}${heightAttribute} />`;
}

function renderHtmlImage(_match: string, src: string, alt = '', title = '', width = '', height = ''): string {
    return buildImageHtml({ src, alt, title, width, height });
}

function getImageDetails(imageElement: unknown): ImageDetails {
    if (!(imageElement instanceof HTMLImageElement)) {
        return {
            src: '',
            alt: '',
            title: '',
            width: '',
            height: ''
        };
    }

    return {
        src: imageElement.getAttribute('src') ?? '',
        alt: imageElement.getAttribute('alt') ?? '',
        title: imageElement.getAttribute('title') ?? '',
        width: normalizeImageDimension(imageElement.getAttribute('width') ?? ''),
        height: normalizeImageDimension(imageElement.getAttribute('height') ?? '')
    };
}

interface ImageToolState {
    imageEditorInputs: HTMLInputElement[];
    imageFileInput: HTMLInputElement | null;
    imageApplyButton: HTMLButtonElement | null;
    imageCancelButton: HTMLButtonElement | null;
    imageTarget: HTMLImageElement | null;
    // A chosen file waits here as a data URL until Insert is clicked; it is never shown in the URL field.
    uploadedImageUrl: string;
    onImageFileChange?: (event: Event) => void;
    onImageApply?: (event: MouseEvent) => void;
    onImageCancel?: (event: MouseEvent) => void;
    onImageEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface TableToolState {
    tableEditorInputs: HTMLInputElement[];
    tableApplyButton: HTMLButtonElement | null;
    tableCancelButton: HTMLButtonElement | null;
    tableTarget: HTMLTableElement | null;
    tableGrid: HTMLElement | null;
    tableSurface: HTMLElement | null;
    tableContextCell: HTMLTableCellElement | null;
    onTableGridEvent?: (event: Event) => void;
    onTableContextTrigger?: (event: Event) => void;
    onTableMenuClick?: (event: MouseEvent) => void;
    onTableApply?: (event: MouseEvent) => void;
    onTableCancel?: (event: MouseEvent) => void;
    onTableEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface TextColorToolState {
    textColorEditorInputs: HTMLInputElement[];
    textColorApplyButton: HTMLButtonElement | null;
    textColorCancelButton: HTMLButtonElement | null;
    onTextColorApply?: (event: MouseEvent) => void;
    onTextColorCancel?: (event: MouseEvent) => void;
    onTextColorEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface LinkToolState {
    linkEditorInputs: HTMLInputElement[];
    linkApplyButton: HTMLButtonElement | null;
    linkCancelButton: HTMLButtonElement | null;
    linkTarget: HTMLAnchorElement | null;
    linkSelectedText: string;
    onLinkApply?: (event: MouseEvent) => void;
    onLinkCancel?: (event: MouseEvent) => void;
    onLinkEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface IframeToolState {
    iframeEditorInputs: HTMLInputElement[];
    iframeApplyButton: HTMLButtonElement | null;
    iframeCancelButton: HTMLButtonElement | null;
    iframeTarget: HTMLIFrameElement | null;
    onIframeApply?: (event: MouseEvent) => void;
    onIframeCancel?: (event: MouseEvent) => void;
    onIframeEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface AriaLabelToolState {
    input: HTMLInputElement | null;
    applyButton: HTMLButtonElement | null;
    cancelButton: HTMLButtonElement | null;
    target: HTMLSpanElement | null;
    onApply?: (event: MouseEvent) => void;
    onCancel?: (event: MouseEvent) => void;
    onKeyDown?: (event: KeyboardEvent) => void;
}

const defaultIframeTitle = 'Embedded content';
const defaultIframeWidth = '100%';
const defaultIframeHeight = '315';
// Embeds need scripts and their own origin. Same-origin sources are refused (see normalizeSafeUrl), because with both
// flags a same-origin frame could remove its own sandbox.
const iframeSandbox = 'allow-scripts allow-same-origin allow-popups allow-presentation';

function getRoleInput(element: Maybe<ParentNode>, role: string): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, `[data-role="${role}"]`);
}

function getRoleButton(element: Maybe<ParentNode>, role: string): HTMLButtonElement | null {
    return qs<HTMLButtonElement>(element, `[data-role="${role}"]`);
}

function getToolEditor(element: Maybe<ParentNode>, command: string): HTMLElement | null {
    return qs<HTMLElement>(element, `[data-tool-command="${command}"]`);
}

function closeInlineTool<TState>(context: RichTextEditorToolContext<TState>, command: string, { focusSurface = false, preserveSelection = false }: InlineToolCloseOptions = {}): void {
    const { element, editorState, host } = context;
    host.setToolPanelOpen(element, command, false);
    if (!preserveSelection) {
        editorState.selectionRange = null;
    }

    if (focusSurface) {
        host.getSurface(element)?.focus({ preventScroll: true });
    }
}

function getSelectionClosest<TElement extends Element>(surface: Maybe<HTMLElement>, selector: string, guard: (value: unknown) => value is TElement): TElement | null {
    const candidate = closestInSurface(surface, selector);
    return guard(candidate) ? candidate : null;
}

function isHtmlAnchorElement(value: unknown): value is HTMLAnchorElement {
    return value instanceof HTMLAnchorElement;
}

function isHtmlImageElement(value: unknown): value is HTMLImageElement {
    return value instanceof HTMLImageElement;
}

function isHtmlIFrameElement(value: unknown): value is HTMLIFrameElement {
    return value instanceof HTMLIFrameElement;
}

function getImageUrlInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return getRoleInput(element, 'image-url');
}

function getImageAltInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return getRoleInput(element, 'image-alt');
}

function getImageTitleInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return getRoleInput(element, 'image-title');
}

function getImageWidthInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return getRoleInput(element, 'image-width');
}

function getImageHeightInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return getRoleInput(element, 'image-height');
}

function handleImageFileSelection(context: RichTextEditorToolContext<ImageToolState>, file: File | null): void {
    const { element, toolState } = context;
    toolState.uploadedImageUrl = '';
    if (!(file instanceof File) || !file.type.startsWith('image/')) {
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        // The read is async: the panel may have been closed, or the editor disabled, while it ran.
        if (getToolEditor(element, 'image')?.hidden !== false || !isEditorEditable(element)) {
            return;
        }

        if (typeof reader.result !== 'string') {
            return;
        }

        // The file replaces any typed URL; a URL typed afterwards wins again (see applyImageEditor).
        toolState.uploadedImageUrl = reader.result;
        const imageUrlInput = getImageUrlInput(element);
        if (imageUrlInput) {
            imageUrlInput.value = '';
        }

        const imageAltInput = getImageAltInput(element);
        if (imageAltInput && !imageAltInput.value.trim()) {
            imageAltInput.value = file.name.replace(/\.[^.]+$/, '');
        }
    };

    reader.readAsDataURL(file);
}

function initializeImageToolStateHandlers(context: RichTextEditorToolContext<ImageToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onImageApply) {
        return;
    }

    toolState.onImageFileChange = (event: Event) => {
        const target = event.target instanceof HTMLInputElement ? event.target : null;
        handleImageFileSelection(context, target?.files?.[0] ?? null);
    };

    toolState.onImageApply = (event: MouseEvent) => {
        event.preventDefault();
        void applyImageEditor(context)
            .then((applied) => {
                if (applied) {
                    host.syncValueFromSurface(element, true);
                    host.updateToolbarState(element);
                }
            });
    };

    toolState.onImageCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeImageEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onImageEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeImageEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        const target = event.target instanceof HTMLInputElement ? event.target : null;
        if (event.key === 'Enter' && target?.type !== 'file') {
            event.preventDefault();
            void applyImageEditor(context)
                .then((applied) => {
                    if (applied) {
                        host.syncValueFromSurface(element, true);
                        host.updateToolbarState(element);
                    }
                });
        }
    };
}

function bindImageToolControls(context: RichTextEditorToolContext<ImageToolState>): void {
    const { element, toolState } = context;
    initializeImageToolStateHandlers(context);
    unbindImageToolControls(context);
    toolState.imageFileInput = getRoleInput(element, 'image-file');
    toolState.imageApplyButton = getRoleButton(element, 'image-apply');
    toolState.imageCancelButton = getRoleButton(element, 'image-cancel');
    toolState.imageEditorInputs = [getImageUrlInput(element), getImageAltInput(element), getImageTitleInput(element), getImageWidthInput(element), getImageHeightInput(element)].filter(isHtmlInputElement);
    if (toolState.onImageFileChange) {
        toolState.imageFileInput?.addEventListener('change', toolState.onImageFileChange);
    }

    if (toolState.onImageApply) {
        toolState.imageApplyButton?.addEventListener('click', toolState.onImageApply);
    }

    if (toolState.onImageCancel) {
        toolState.imageCancelButton?.addEventListener('click', toolState.onImageCancel);
    }

    if (toolState.onImageEditorKeyDown) {
        for (const input of toolState.imageEditorInputs) {
            input.addEventListener('keydown', toolState.onImageEditorKeyDown);
        }
    }
}

function unbindImageToolControls(context: RichTextEditorToolContext<ImageToolState>): void {
    const { toolState } = context;
    if (toolState.onImageFileChange) {
        toolState.imageFileInput?.removeEventListener('change', toolState.onImageFileChange);
    }

    if (toolState.onImageApply) {
        toolState.imageApplyButton?.removeEventListener('click', toolState.onImageApply);
    }

    if (toolState.onImageCancel) {
        toolState.imageCancelButton?.removeEventListener('click', toolState.onImageCancel);
    }

    if (toolState.onImageEditorKeyDown) {
        for (const input of toolState.imageEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onImageEditorKeyDown);
        }
    }
}

// Clearing a file input fires no change event; send one so NTFileUpload's chosen-file text resets too (via Blazor
// when interactive, via the library module in static SSR). This also clears the staged upload.
function clearImageFileInput(toolState: ImageToolState): void {
    const input = toolState.imageFileInput;
    if (input?.value) {
        input.value = '';
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

function closeImageEditor(context: RichTextEditorToolContext<ImageToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.imageTarget = null;
    clearImageFileInput(context.toolState);
    context.toolState.uploadedImageUrl = '';
    closeInlineTool(context, 'image', options);
}

function openImageEditor(context: RichTextEditorToolContext<ImageToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface || !getToolEditor(element, 'image')) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'image');
    const existingImage = getSelectionClosest(surface, 'img', isHtmlImageElement);
    const imageDetails = getImageDetails(existingImage);
    host.saveSelectionRange(surface, editorState);
    toolState.imageTarget = existingImage;
    clearImageFileInput(toolState);
    // An embedded (uploaded) image keeps its data URL out of the URL field; Insert keeps it unless a URL is typed.
    const isEmbedded = imageDetails.src.startsWith('data:');
    toolState.uploadedImageUrl = isEmbedded ? imageDetails.src : '';
    const imageUrlInput = getImageUrlInput(element);
    if (imageUrlInput) {
        imageUrlInput.value = isEmbedded ? '' : imageDetails.src;
    }

    const imageAltInput = getImageAltInput(element);
    if (imageAltInput) {
        imageAltInput.value = imageDetails.alt;
    }

    const imageTitleInput = getImageTitleInput(element);
    if (imageTitleInput) {
        imageTitleInput.value = imageDetails.title;
    }

    const imageWidthInput = getImageWidthInput(element);
    if (imageWidthInput) {
        imageWidthInput.value = imageDetails.width;
    }

    const imageHeightInput = getImageHeightInput(element);
    if (imageHeightInput) {
        imageHeightInput.value = imageDetails.height;
    }

    host.setToolPanelOpen(element, 'image', true);
    if (focusInput) {
        imageUrlInput?.focus();
        if (selectInputText) {
            imageUrlInput?.select();
        }
    }

    return false;
}

async function applyImageEditor(context: RichTextEditorToolContext<ImageToolState>): Promise<boolean> {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const typedUrl = getImageUrlInput(element)?.value?.trim() ?? '';
    const imageUrl = normalizeSafeUrl(typedUrl || toolState.uploadedImageUrl, 'image');
    if (!imageUrl) {
        getImageUrlInput(element)?.focus();
        return false;
    }

    const imageAlt = getImageAltInput(element)?.value?.trim?.() ?? '';
    const imageTitle = getImageTitleInput(element)?.value?.trim?.() ?? '';
    const imageWidth = normalizeImageDimension(getImageWidthInput(element)?.value ?? '');
    const imageHeight = normalizeImageDimension(getImageHeightInput(element)?.value ?? '');
    const imageTarget = toolState.imageTarget;

    if (imageTarget instanceof HTMLImageElement && surface.contains(imageTarget)) {
        imageTarget.setAttribute('src', imageUrl);
        imageTarget.setAttribute('alt', imageAlt);
        imageTitle ? imageTarget.setAttribute('title', imageTitle) : imageTarget.removeAttribute('title');
        imageWidth ? imageTarget.setAttribute('width', imageWidth) : imageTarget.removeAttribute('width');
        imageHeight ? imageTarget.setAttribute('height', imageHeight) : imageTarget.removeAttribute('height');
    } else {
        host.restoreSelectionRange(surface, editorState);
        const imageHtml = buildImageHtml({ src: imageUrl, alt: imageAlt, title: imageTitle, width: imageWidth, height: imageHeight });
        if (!imageHtml) {
            getImageUrlInput(element)?.focus();
            return false;
        }

        document.execCommand('insertHTML', false, imageHtml);
    }

    closeImageEditor(context, { focusSurface: true });
    return true;
}

function buildTableHtml(columns: number, rows: number): string {
    const headerMarkup = Array.from({ length: columns }, (_, columnIndex) => `<th>${escapeHtml(`Header ${columnIndex + 1}`)}</th>`).join('');
    const bodyMarkup = rows > 1 ? `<tbody>${`<tr>${'<td><br></td>'.repeat(columns)}</tr>`.repeat(rows - 1)}</tbody>` : '';
    return `<table${buildTableStyleAttribute(defaultTableBorderColor)}><thead><tr>${headerMarkup}</tr></thead>${bodyMarkup}</table>`;
}

function getTableGridButtons(grid: Maybe<ParentNode>): HTMLButtonElement[] {
    return toArray(grid?.querySelectorAll('button[data-col]')).filter(isHtmlButtonElement);
}

// Highlights the rectangle from the top-left cell to columns x rows and makes its last cell the roving tab stop.
function highlightTableGrid(element: Maybe<ParentNode>, grid: Maybe<ParentNode>, columns: number, rows: number): void {
    for (const button of getTableGridButtons(grid)) {
        const column = Number(button.dataset.col);
        const row = Number(button.dataset.row);
        button.toggleAttribute('data-selected', column <= columns && row <= rows);
        button.tabIndex = column === Math.max(columns, 1) && row === Math.max(rows, 1) ? 0 : -1;
    }

    const label = qs<HTMLElement>(element, '[data-role="table-grid-label"]');
    if (label) {
        label.textContent = columns && rows ? `${columns} × ${rows}` : 'Insert table';
    }
}

function insertTable(context: RichTextEditorToolContext<TableToolState>, columns: number, rows: number): void {
    const { element, editorState, host } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return;
    }

    host.restoreSelectionRange(surface, editorState);
    document.execCommand('insertHTML', false, `${buildTableHtml(columns, rows)}<p><br></p>`);
    closeTableEditor(context, { focusSurface: true });
    host.syncValueFromSurface(element, true);
    host.updateToolbarState(element);
}

function handleTableGridEvent(context: RichTextEditorToolContext<TableToolState>, event: Event): void {
    const { element, host, toolState } = context;
    const button = event.target instanceof Element ? event.target.closest<HTMLButtonElement>('button[data-col]') : null;
    if (!button) {
        return;
    }

    const column = Number(button.dataset.col);
    const row = Number(button.dataset.row);
    if (event.type === 'click') {
        event.preventDefault();
        insertTable(context, column, row);
    } else if (event.type === 'keydown') {
        const key = (event as KeyboardEvent).key;
        if (key === 'Escape') {
            event.preventDefault();
            closeTableEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        const columnOffset = key === 'ArrowRight' ? 1 : key === 'ArrowLeft' ? -1 : 0;
        const rowOffset = key === 'ArrowDown' ? 1 : key === 'ArrowUp' ? -1 : 0;
        if (columnOffset || rowOffset) {
            event.preventDefault();
            toolState.tableGrid?.querySelector<HTMLButtonElement>(`button[data-col="${column + columnOffset}"][data-row="${row + rowOffset}"]`)?.focus();
        }
    } else {
        highlightTableGrid(element, toolState.tableGrid, column, row);
    }
}

function createTableCell(tagName: 'th' | 'td'): HTMLTableCellElement {
    const cell = document.createElement(tagName);
    cell.append(document.createElement('br'));
    return cell;
}

function isHeaderTableRow(row: HTMLTableRowElement): boolean {
    const cells = getTableCells(row);
    return row.parentElement?.tagName === 'THEAD' || (cells.length > 0 && cells.every((cell) => cell.tagName === 'TH'));
}

// Edits the live table around a cell and returns the cell that should receive the caret (null when the table is gone).
function applyTableAction(cell: HTMLTableCellElement, action: string): HTMLTableCellElement | null {
    const row = cell.parentElement;
    const table = cell.closest('table');
    if (!(row instanceof HTMLTableRowElement) || !table) {
        return null;
    }

    const rows = toArray(table.rows);
    const rowIndex = rows.indexOf(row);
    const columnIndex = getTableCells(row).indexOf(cell);
    if (action === 'row-above' || action === 'row-below') {
        // New rows are always body rows: inserting next to the header starts the body instead of adding a second header.
        const newRow = document.createElement('tr');
        getTableCells(row).forEach(() => newRow.append(createTableCell('td')));
        if (row.parentElement instanceof HTMLTableSectionElement && row.parentElement.tagName === 'THEAD') {
            (table.tBodies[0] ?? table.appendChild(document.createElement('tbody'))).prepend(newRow);
        } else {
            row.insertAdjacentElement(action === 'row-above' ? 'beforebegin' : 'afterend', newRow);
        }

        return getTableCells(newRow)[columnIndex] ?? cell;
    }

    if (action === 'column-left' || action === 'column-right') {
        const insertIndex = action === 'column-left' ? columnIndex : columnIndex + 1;
        let focusCell = cell;
        for (const tableRow of rows) {
            const newCell = createTableCell(isHeaderTableRow(tableRow) ? 'th' : 'td');
            const beforeCell = getTableCells(tableRow)[insertIndex];
            if (beforeCell) {
                tableRow.insertBefore(newCell, beforeCell);
            } else {
                tableRow.append(newCell);
            }

            if (tableRow === row) {
                focusCell = newCell;
            }
        }

        return focusCell;
    }

    if (action === 'delete-row' && rows.length > 1) {
        const neighborCells = getTableCells(rows[rowIndex + 1] ?? rows[rowIndex - 1]);
        const section = row.parentElement;
        row.remove();
        if (section && section !== table && !section.children.length) {
            section.remove();
        }

        return neighborCells[columnIndex] ?? neighborCells[0] ?? null;
    }

    if (action === 'delete-column' && rows.some((tableRow) => getTableCells(tableRow).length > 1)) {
        rows.forEach((tableRow) => getTableCells(tableRow)[columnIndex]?.remove());
        const remainingCells = getTableCells(row);
        return remainingCells[Math.min(columnIndex, remainingCells.length - 1)] ?? null;
    }

    // Delete table, and deleting the last remaining row or column.
    table.remove();
    return null;
}

function initializeTableToolStateHandlers(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onTableApply) {
        return;
    }

    toolState.onTableApply = (event: MouseEvent) => {
        event.preventDefault();
        if (applyTableEditor(context)) {
            host.syncValueFromSurface(element, true);
            host.updateToolbarState(element);
        }
    };

    toolState.onTableCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeTableEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onTableEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeTableEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            if (applyTableEditor(context)) {
                host.syncValueFromSurface(element, true);
                host.updateToolbarState(element);
            }
        }
    };

    toolState.onTableGridEvent = (event: Event) => handleTableGridEvent(context, event);

    // Remember which cell the context menu was opened on (right-click or the keyboard menu keys).
    toolState.onTableContextTrigger = (event: Event) => {
        const surface = host.getSurface(element);
        if (event.type === 'keydown') {
            const keyEvent = event as KeyboardEvent;
            if (keyEvent.key !== 'ContextMenu' && !(keyEvent.shiftKey && keyEvent.key === 'F10')) {
                return;
            }
        }

        const candidate = event.type === 'contextmenu'
            ? (event.target instanceof Element ? event.target.closest('td, th') : null)
            : getSelectionClosest(surface, 'td, th', isTableCellElement);
        toolState.tableContextCell = isTableCellElement(candidate) && surface?.contains(candidate) ? candidate : null;
    };

    toolState.onTableMenuClick = (event: MouseEvent) => {
        const item = event.target instanceof Element ? event.target.closest<HTMLElement>('[data-table-action]') : null;
        const cell = toolState.tableContextCell;
        const surface = host.getSurface(element);
        if (!item || !surface || !cell || !surface.contains(cell) || !isEditorEditable(element)) {
            return;
        }

        toolState.tableContextCell = null;
        const caretCell = applyTableAction(cell, item.dataset.tableAction ?? '');
        if (caretCell) {
            surface.focus({ preventScroll: true });
            const range = document.createRange();
            range.selectNodeContents(caretCell);
            range.collapse(true);
            window.getSelection?.()?.removeAllRanges();
            window.getSelection?.()?.addRange(range);
        } else {
            placeCaretAtEnd(surface);
        }

        host.syncValueFromSurface(element, true);
        host.updateToolbarState(element);
    };
}

const tableGridEventTypes = ['click', 'mouseover', 'focusin', 'keydown'];

function bindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, host, toolState } = context;
    initializeTableToolStateHandlers(context);
    unbindTableToolControls(context);
    toolState.tableApplyButton = getRoleButton(element, 'table-apply');
    toolState.tableCancelButton = getRoleButton(element, 'table-cancel');
    toolState.tableEditorInputs = [getRoleInput(element, 'table-border-color'), getRoleInput(element, 'table-caption')].filter(isHtmlInputElement);
    toolState.tableGrid = qs<HTMLElement>(element, '[data-role="table-grid"]');
    toolState.tableSurface = host.getSurface(element);
    if (toolState.onTableApply) {
        toolState.tableApplyButton?.addEventListener('click', toolState.onTableApply);
    }

    if (toolState.onTableCancel) {
        toolState.tableCancelButton?.addEventListener('click', toolState.onTableCancel);
    }

    if (toolState.onTableEditorKeyDown) {
        for (const input of toolState.tableEditorInputs) {
            input.addEventListener('keydown', toolState.onTableEditorKeyDown);
        }
    }

    if (toolState.onTableGridEvent) {
        tableGridEventTypes.forEach((type) => toolState.tableGrid?.addEventListener(type, toolState.onTableGridEvent!));
    }

    if (toolState.onTableContextTrigger) {
        toolState.tableSurface?.addEventListener('contextmenu', toolState.onTableContextTrigger);
        toolState.tableSurface?.addEventListener('keydown', toolState.onTableContextTrigger);
    }

    if (toolState.onTableMenuClick) {
        element.addEventListener('click', toolState.onTableMenuClick);
    }
}

function unbindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, toolState } = context;
    if (toolState.onTableApply) {
        toolState.tableApplyButton?.removeEventListener('click', toolState.onTableApply);
    }

    if (toolState.onTableCancel) {
        toolState.tableCancelButton?.removeEventListener('click', toolState.onTableCancel);
    }

    if (toolState.onTableEditorKeyDown) {
        for (const input of toolState.tableEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onTableEditorKeyDown);
        }
    }

    if (toolState.onTableGridEvent) {
        tableGridEventTypes.forEach((type) => toolState.tableGrid?.removeEventListener(type, toolState.onTableGridEvent!));
    }

    if (toolState.onTableContextTrigger) {
        toolState.tableSurface?.removeEventListener('contextmenu', toolState.onTableContextTrigger);
        toolState.tableSurface?.removeEventListener('keydown', toolState.onTableContextTrigger);
    }

    if (toolState.onTableMenuClick) {
        element.removeEventListener('click', toolState.onTableMenuClick);
    }
}

function closeTableEditor(context: RichTextEditorToolContext<TableToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.tableTarget = null;
    closeInlineTool(context, 'table', options);
}

// Inserting shows the size grid; editing an existing table shows its border color and caption.
function openTableEditor(context: RichTextEditorToolContext<TableToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface || !getToolEditor(element, 'table')) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'table');
    const existingTable = getSelectionClosest(surface, 'table', (value): value is HTMLTableElement => value instanceof HTMLTableElement);
    host.saveSelectionRange(surface, editorState);
    toolState.tableTarget = existingTable;
    const insertPanel = qs<HTMLElement>(element, '[data-role="table-insert"]');
    const editPanel = qs<HTMLElement>(element, '[data-role="table-edit"]');
    if (insertPanel) {
        insertPanel.hidden = existingTable !== null;
    }

    if (editPanel) {
        editPanel.hidden = existingTable === null;
    }

    const borderColorInput = getRoleInput(element, 'table-border-color');
    const captionInput = getRoleInput(element, 'table-caption');
    if (borderColorInput) {
        borderColorInput.value = getTableBorderColor(existingTable) || defaultTableBorderColor;
    }

    if (captionInput) {
        captionInput.value = existingTable?.querySelector(':scope > caption')?.textContent?.trim() ?? '';
    }

    highlightTableGrid(element, toolState.tableGrid, 0, 0);
    host.setToolPanelOpen(element, 'table', true);
    if (focusInput) {
        const focusTarget = existingTable ? captionInput : getTableGridButtons(toolState.tableGrid)[0];
        focusTarget?.focus();
        if (existingTable && selectInputText) {
            captionInput?.select();
        }
    }

    return false;
}

// Applies border color and caption to the existing table in place so cell content and attributes are never rebuilt.
function applyTableEditor(context: RichTextEditorToolContext<TableToolState>): boolean {
    const { element, host, toolState } = context;
    const surface = host.getSurface(element);
    const table = toolState.tableTarget;
    if (!surface || !(table instanceof HTMLTableElement) || !surface.contains(table)) {
        return false;
    }

    const borderColor = normalizeTableBorderColor(getRoleInput(element, 'table-border-color')?.value ?? '') || defaultTableBorderColor;
    const captionText = getRoleInput(element, 'table-caption')?.value?.trim?.() ?? '';
    table.setAttribute('data-border-color', borderColor);
    table.style.setProperty('--nt-rich-text-table-border-color', borderColor);
    let caption = table.querySelector(':scope > caption');
    if (!captionText) {
        caption?.remove();
    } else {
        caption ??= table.insertBefore(document.createElement('caption'), table.firstChild);
        caption.textContent = captionText;
    }

    closeTableEditor(context, { focusSurface: true });
    return true;
}

function unwrapElement(element: Element): void {
    element.replaceWith(...Array.from(element.childNodes));
}

function initializeTextColorToolStateHandlers(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onTextColorApply) {
        return;
    }

    toolState.onTextColorApply = (event: MouseEvent) => {
        event.preventDefault();
        if (applyTextColorEditor(context)) {
            host.syncValueFromSurface(element, true);
            host.updateToolbarState(element);
        }
    };

    toolState.onTextColorCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeTextColorEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onTextColorEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeTextColorEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            if (applyTextColorEditor(context)) {
                host.syncValueFromSurface(element, true);
                host.updateToolbarState(element);
            }
        }
    };
}

function bindTextColorToolControls(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { element, toolState } = context;
    initializeTextColorToolStateHandlers(context);
    unbindTextColorToolControls(context);
    toolState.textColorApplyButton = getRoleButton(element, 'text-color-apply');
    toolState.textColorCancelButton = getRoleButton(element, 'text-color-cancel');
    toolState.textColorEditorInputs = [getRoleInput(element, 'text-color-value')].filter(isHtmlInputElement);
    if (toolState.onTextColorApply) {
        toolState.textColorApplyButton?.addEventListener('click', toolState.onTextColorApply);
    }

    if (toolState.onTextColorCancel) {
        toolState.textColorCancelButton?.addEventListener('click', toolState.onTextColorCancel);
    }

    if (toolState.onTextColorEditorKeyDown) {
        for (const input of toolState.textColorEditorInputs) {
            input.addEventListener('keydown', toolState.onTextColorEditorKeyDown);
        }
    }
}

function unbindTextColorToolControls(context: RichTextEditorToolContext<TextColorToolState>): void {
    const { toolState } = context;
    if (toolState.onTextColorApply) {
        toolState.textColorApplyButton?.removeEventListener('click', toolState.onTextColorApply);
    }

    if (toolState.onTextColorCancel) {
        toolState.textColorCancelButton?.removeEventListener('click', toolState.onTextColorCancel);
    }

    if (toolState.onTextColorEditorKeyDown) {
        for (const input of toolState.textColorEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onTextColorEditorKeyDown);
        }
    }
}

function closeTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>, options: InlineToolCloseOptions = {}): void {
    closeInlineTool(context, 'textColor', options);
}

function openTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'textColor');
    const existingSpan = closestInSurface(surface, 'span[style*="color"]');
    host.saveSelectionRange(surface, editorState);
    if (existingSpan && editorState.selectionRange?.collapsed) {
        // A caret in colored text (e.g. after clicking it) recolors that whole span; the live caret is left alone.
        editorState.selectionRange.selectNodeContents(existingSpan);
    }
    const textColorInput = getRoleInput(element, 'text-color-value');
    if (textColorInput) {
        textColorInput.value = normalizeTextColorValue(getElementTextColor(existingSpan)) || defaultTextColor;
    }

    host.setToolPanelOpen(element, 'textColor', true);
    if (focusInput) {
        textColorInput?.focus();
        if (selectInputText) {
            textColorInput?.select();
        }
    }

    return false;
}

function applyTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>): boolean {
    const { element, editorState, host } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const color = normalizeTextColorValue(getRoleInput(element, 'text-color-value')?.value ?? '') || defaultTextColor;
    host.restoreSelectionRange(surface, editorState);
    const range = window.getSelection?.()?.rangeCount ? window.getSelection()!.getRangeAt(0) : null;
    if (!range || range.collapsed || !surface.contains(range.commonAncestorContainer)) {
        getRoleInput(element, 'text-color-value')?.focus();
        return false;
    }

    // The native command splits partial spans, recolors nested ones, stays inside each block, and joins the undo
    // stack. styleWithCSS makes it write <span style="color"> instead of <font>.
    document.execCommand('styleWithCSS', false, 'true');
    document.execCommand('foreColor', false, color);
    document.execCommand('styleWithCSS', false, 'false');
    closeTextColorEditor(context, { focusSurface: true });
    return true;
}

function initializeLinkToolStateHandlers(context: RichTextEditorToolContext<LinkToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onLinkApply) {
        return;
    }

    toolState.onLinkApply = (event: MouseEvent) => {
        event.preventDefault();
        if (applyLinkEditor(context)) {
            host.syncValueFromSurface(element, true);
            host.updateToolbarState(element);
        }
    };

    toolState.onLinkCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeLinkEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onLinkEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeLinkEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            if (applyLinkEditor(context)) {
                host.syncValueFromSurface(element, true);
                host.updateToolbarState(element);
            }
        }
    };
}

function bindLinkToolControls(context: RichTextEditorToolContext<LinkToolState>): void {
    const { element, toolState } = context;
    initializeLinkToolStateHandlers(context);
    unbindLinkToolControls(context);
    toolState.linkApplyButton = getRoleButton(element, 'link-apply');
    toolState.linkCancelButton = getRoleButton(element, 'link-cancel');
    toolState.linkEditorInputs = [getRoleInput(element, 'link-url'), getRoleInput(element, 'link-text'), getRoleInput(element, 'link-aria-label'), getRoleInput(element, 'link-title')].filter(isHtmlInputElement);
    if (toolState.onLinkApply) {
        toolState.linkApplyButton?.addEventListener('click', toolState.onLinkApply);
    }

    if (toolState.onLinkCancel) {
        toolState.linkCancelButton?.addEventListener('click', toolState.onLinkCancel);
    }

    if (toolState.onLinkEditorKeyDown) {
        for (const input of toolState.linkEditorInputs) {
            input.addEventListener('keydown', toolState.onLinkEditorKeyDown);
        }
    }
}

function unbindLinkToolControls(context: RichTextEditorToolContext<LinkToolState>): void {
    const { toolState } = context;
    if (toolState.onLinkApply) {
        toolState.linkApplyButton?.removeEventListener('click', toolState.onLinkApply);
    }

    if (toolState.onLinkCancel) {
        toolState.linkCancelButton?.removeEventListener('click', toolState.onLinkCancel);
    }

    if (toolState.onLinkEditorKeyDown) {
        for (const input of toolState.linkEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onLinkEditorKeyDown);
        }
    }
}

function closeLinkEditor(context: RichTextEditorToolContext<LinkToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.linkTarget = null;
    context.toolState.linkSelectedText = '';
    closeInlineTool(context, 'link', options);
}

function openLinkEditor(context: RichTextEditorToolContext<LinkToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'link');
    const existingLink = getSelectionClosest(surface, 'a', isHtmlAnchorElement);
    const selection = window.getSelection?.();
    const selectedText = selection?.toString?.().trim?.() ?? '';
    const linkText = existingLink?.textContent?.trim?.() || selectedText;
    const linkUrl = existingLink?.getAttribute('href') ?? '';
    const linkAriaLabel = existingLink?.getAttribute('aria-label') ?? '';
    const linkTitle = existingLink?.getAttribute('title') ?? '';
    host.saveSelectionRange(surface, editorState);
    toolState.linkTarget = existingLink;
    // The prefilled text, so apply can tell whether the user edited it.
    toolState.linkSelectedText = linkText;
    const linkUrlInput = getRoleInput(element, 'link-url');
    const linkTextInput = getRoleInput(element, 'link-text');
    if (linkUrlInput) {
        linkUrlInput.value = linkUrl;
    }

    if (linkTextInput) {
        linkTextInput.value = linkText;
    }

    const linkAriaLabelInput = getRoleInput(element, 'link-aria-label');
    if (linkAriaLabelInput) {
        linkAriaLabelInput.value = linkAriaLabel;
    }

    const linkTitleInput = getRoleInput(element, 'link-title');
    if (linkTitleInput) {
        linkTitleInput.value = linkTitle;
    }

    host.setToolPanelOpen(element, 'link', true);
    if (focusInput) {
        linkUrlInput?.focus();
        if (selectInputText) {
            linkUrlInput?.select();
        }
    }

    return false;
}

function applyLinkEditor(context: RichTextEditorToolContext<LinkToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const linkTarget = toolState.linkTarget instanceof HTMLAnchorElement && surface.contains(toolState.linkTarget) ? toolState.linkTarget : null;
    const rawUrl = getRoleInput(element, 'link-url')?.value?.trim() ?? '';
    if (!rawUrl && linkTarget) {
        // Clearing the URL of an existing link removes the link and keeps its text.
        unwrapElement(linkTarget);
        closeLinkEditor(context, { focusSurface: true });
        return true;
    }

    const linkUrl = normalizeSafeUrl(rawUrl, 'link');
    if (!linkUrl) {
        getRoleInput(element, 'link-url')?.focus();
        return false;
    }

    const linkText = getRoleInput(element, 'link-text')?.value?.trim?.() ?? '';
    // Rewriting textContent flattens formatting inside the link, so only do it when the user changed the text.
    const isTextChanged = linkText !== '' && linkText !== toolState.linkSelectedText;
    const linkAriaLabel = getRoleInput(element, 'link-aria-label')?.value?.trim?.() ?? '';
    const linkTitle = getRoleInput(element, 'link-title')?.value?.trim?.() ?? '';
    let anchor = linkTarget;
    if (anchor) {
        anchor.setAttribute('href', linkUrl);
        if (isTextChanged) {
            anchor.textContent = linkText;
        }
    } else {
        host.restoreSelectionRange(surface, editorState);
        const range = window.getSelection?.()?.rangeCount ? window.getSelection()!.getRangeAt(0) : null;
        if (!range || !surface.contains(range.commonAncestorContainer)) {
            getRoleInput(element, 'link-url')?.focus();
            return false;
        }

        // Native commands keep the selection's formatting and land on the undo stack.
        if (!range.collapsed && !isTextChanged) {
            document.execCommand('createLink', false, linkUrl);
        } else {
            document.execCommand('insertHTML', false, `<a href="${escapeHtml(linkUrl)}">${escapeHtml(linkText || toolState.linkSelectedText || linkUrl)}</a>`);
        }

        anchor = getSelectionClosest(surface, 'a', isHtmlAnchorElement);
    }

    if (anchor) {
        linkAriaLabel ? anchor.setAttribute('aria-label', linkAriaLabel) : anchor.removeAttribute('aria-label');
        linkTitle ? anchor.setAttribute('title', linkTitle) : anchor.removeAttribute('title');
    }

    closeLinkEditor(context, { focusSurface: true });
    return true;
}

function normalizeIframeField(value: unknown, fallbackValue: string): string {
    const trimmed = `${value ?? ''}`.trim();
    return trimmed.length > 0 ? trimmed : fallbackValue;
}

function buildIframeHtml(url: string, title: string, width: string, height: string): string {
    const normalizedUrl = normalizeSafeUrl(url, 'iframe');
    if (!normalizedUrl) {
        return '';
    }

    // Sandbox flags apply when a frame navigates, so the attribute must be in the inserted markup, not added later.
    return `<iframe src="${escapeHtml(normalizedUrl)}" title="${escapeHtml(title)}" width="${escapeHtml(width)}" height="${escapeHtml(height)}" loading="lazy" sandbox="${iframeSandbox}"></iframe>`;
}

function initializeIframeToolStateHandlers(context: RichTextEditorToolContext<IframeToolState>): void {
    const { element, host, toolState } = context;
    if (toolState.onIframeApply) {
        return;
    }

    toolState.onIframeApply = (event: MouseEvent) => {
        event.preventDefault();
        if (applyIframeEditor(context)) {
            host.syncValueFromSurface(element, true);
            host.updateToolbarState(element);
        }
    };

    toolState.onIframeCancel = (event: MouseEvent) => {
        event.preventDefault();
        closeIframeEditor(context, { focusSurface: true });
        host.updateToolbarState(element);
    };

    toolState.onIframeEditorKeyDown = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeIframeEditor(context, { focusSurface: true });
            host.updateToolbarState(element);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            if (applyIframeEditor(context)) {
                host.syncValueFromSurface(element, true);
                host.updateToolbarState(element);
            }
        }
    };
}

function bindIframeToolControls(context: RichTextEditorToolContext<IframeToolState>): void {
    const { element, toolState } = context;
    initializeIframeToolStateHandlers(context);
    unbindIframeToolControls(context);
    toolState.iframeApplyButton = getRoleButton(element, 'iframe-apply');
    toolState.iframeCancelButton = getRoleButton(element, 'iframe-cancel');
    toolState.iframeEditorInputs = [getRoleInput(element, 'iframe-url'), getRoleInput(element, 'iframe-title'), getRoleInput(element, 'iframe-width'), getRoleInput(element, 'iframe-height')].filter(isHtmlInputElement);
    if (toolState.onIframeApply) {
        toolState.iframeApplyButton?.addEventListener('click', toolState.onIframeApply);
    }

    if (toolState.onIframeCancel) {
        toolState.iframeCancelButton?.addEventListener('click', toolState.onIframeCancel);
    }

    if (toolState.onIframeEditorKeyDown) {
        for (const input of toolState.iframeEditorInputs) {
            input.addEventListener('keydown', toolState.onIframeEditorKeyDown);
        }
    }
}

function unbindIframeToolControls(context: RichTextEditorToolContext<IframeToolState>): void {
    const { toolState } = context;
    if (toolState.onIframeApply) {
        toolState.iframeApplyButton?.removeEventListener('click', toolState.onIframeApply);
    }

    if (toolState.onIframeCancel) {
        toolState.iframeCancelButton?.removeEventListener('click', toolState.onIframeCancel);
    }

    if (toolState.onIframeEditorKeyDown) {
        for (const input of toolState.iframeEditorInputs ?? []) {
            input.removeEventListener('keydown', toolState.onIframeEditorKeyDown);
        }
    }
}

function closeIframeEditor(context: RichTextEditorToolContext<IframeToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.iframeTarget = null;
    closeInlineTool(context, 'iframe', options);
}

function openIframeEditor(context: RichTextEditorToolContext<IframeToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'iframe');
    const existingIframe = getSelectionClosest(surface, 'iframe', isHtmlIFrameElement);
    host.saveSelectionRange(surface, editorState);
    toolState.iframeTarget = existingIframe;
    const iframeUrlInput = getRoleInput(element, 'iframe-url');
    const iframeTitleInput = getRoleInput(element, 'iframe-title');
    const iframeWidthInput = getRoleInput(element, 'iframe-width');
    const iframeHeightInput = getRoleInput(element, 'iframe-height');
    if (iframeUrlInput) {
        iframeUrlInput.value = existingIframe?.getAttribute('src') ?? '';
    }

    if (iframeTitleInput) {
        iframeTitleInput.value = existingIframe?.getAttribute('title') ?? defaultIframeTitle;
    }

    if (iframeWidthInput) {
        iframeWidthInput.value = existingIframe?.getAttribute('width') ?? defaultIframeWidth;
    }

    if (iframeHeightInput) {
        iframeHeightInput.value = existingIframe?.getAttribute('height') ?? defaultIframeHeight;
    }

    host.setToolPanelOpen(element, 'iframe', true);
    if (focusInput) {
        iframeUrlInput?.focus();
        if (selectInputText) {
            iframeUrlInput?.select();
        }
    }

    return false;
}

function applyIframeEditor(context: RichTextEditorToolContext<IframeToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const iframeUrl = normalizeSafeUrl(getRoleInput(element, 'iframe-url')?.value ?? '', 'iframe');
    if (!iframeUrl) {
        getRoleInput(element, 'iframe-url')?.focus();
        return false;
    }

    const iframeTitle = normalizeIframeField(getRoleInput(element, 'iframe-title')?.value, defaultIframeTitle);
    // Match the sanitizer: plain pixel numbers (or a width percentage), so "500px" is kept rather than reset on save.
    const iframeWidth = sanitizeDimensionAttribute(`${getRoleInput(element, 'iframe-width')?.value ?? ''}`.replace(/px$/i, ''), true) || defaultIframeWidth;
    const iframeHeight = sanitizeDimensionAttribute(`${getRoleInput(element, 'iframe-height')?.value ?? ''}`.replace(/px$/i, '')) || defaultIframeHeight;
    const iframeTarget = toolState.iframeTarget;
    if (iframeTarget instanceof HTMLIFrameElement && surface.contains(iframeTarget)) {
        // Before src, so the navigation it starts is sandboxed.
        iframeTarget.setAttribute('sandbox', iframeSandbox);
        iframeTarget.setAttribute('src', iframeUrl);
        iframeTarget.setAttribute('title', iframeTitle);
        iframeTarget.setAttribute('width', iframeWidth);
        iframeTarget.setAttribute('height', iframeHeight);
        iframeTarget.setAttribute('loading', 'lazy');
    } else {
        host.restoreSelectionRange(surface, editorState);
        const iframeHtml = buildIframeHtml(iframeUrl, iframeTitle, iframeWidth, iframeHeight);
        if (!iframeHtml) {
            getRoleInput(element, 'iframe-url')?.focus();
            return false;
        }

        document.execCommand('insertHTML', false, iframeHtml);
    }

    closeIframeEditor(context, { focusSurface: true });
    return true;
}

function closeAriaLabelEditor(context: RichTextEditorToolContext<AriaLabelToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.target = null;
    closeInlineTool(context, 'ariaLabel', options);
}

function openAriaLabelEditor(context: RichTextEditorToolContext<AriaLabelToolState>, { focusInput = true }: { focusInput?: boolean } = {}): boolean {
    const { element, editorState: state, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const target = getSelectionClosest(surface, 'span[role="group"][aria-label]', isHtmlSpanElement);
    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    if (!target && (!range || range.collapsed || !surface.contains(range.commonAncestorContainer) || range.cloneContents().querySelector(blockSelector))) {
        return false;
    }

    host.closeOtherTools(element, state, 'ariaLabel');
    host.saveSelectionRange(surface, state);
    toolState.target = target;
    if (toolState.input) {
        toolState.input.value = target?.getAttribute('aria-label') ?? '';
    }

    host.setToolPanelOpen(element, 'ariaLabel', true);
    if (focusInput) {
        toolState.input?.focus();
    }

    return false;
}

function applyAriaLabelEditor(context: RichTextEditorToolContext<AriaLabelToolState>): boolean {
    const { element, editorState: state, host, toolState } = context;
    const surface = host.getSurface(element);
    const label = toolState.input?.value.trim() ?? '';
    const target = surface && toolState.target && surface.contains(toolState.target) ? toolState.target : null;
    if (!label && target) {
        // Clearing the label removes the group and keeps its text.
        unwrapElement(target);
    } else if (!surface || !label) {
        toolState.input?.focus();
        return false;
    } else if (target) {
        target.setAttribute('aria-label', label);
    } else {
        host.restoreSelectionRange(surface, state);
        const selection = window.getSelection?.();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || range.collapsed || !surface.contains(range.commonAncestorContainer) || range.cloneContents().querySelector(blockSelector)) {
            toolState.input?.focus();
            return false;
        }

        const wrapper = document.createElement('span');
        wrapper.setAttribute('role', 'group');
        wrapper.setAttribute('aria-label', label);
        wrapper.appendChild(range.extractContents());
        range.insertNode(wrapper);
    }

    closeAriaLabelEditor(context, { focusSurface: true });
    return true;
}

function bindAriaLabelTool(context: RichTextEditorToolContext<AriaLabelToolState>): void {
    const { element, host, toolState } = context;
    toolState.input = getRoleInput(element, 'aria-label-value');
    toolState.applyButton = getRoleButton(element, 'aria-label-apply');
    toolState.cancelButton = getRoleButton(element, 'aria-label-cancel');
    toolState.onApply ??= (event) => {
        event.preventDefault();
        if (applyAriaLabelEditor(context)) {
            host.syncValueFromSurface(element, true);
        }
    };
    toolState.onCancel ??= (event) => {
        event.preventDefault();
        closeAriaLabelEditor(context, { focusSurface: true });
    };
    toolState.onKeyDown ??= (event) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeAriaLabelEditor(context, { focusSurface: true });
        } else if (event.key === 'Enter') {
            event.preventDefault();
            if (applyAriaLabelEditor(context)) {
                host.syncValueFromSurface(element, true);
            }
        }
    };
    toolState.applyButton?.addEventListener('click', toolState.onApply);
    toolState.cancelButton?.addEventListener('click', toolState.onCancel);
    toolState.input?.addEventListener('keydown', toolState.onKeyDown);
}

function unbindAriaLabelTool({ toolState }: RichTextEditorToolContext<AriaLabelToolState>): void {
    if (toolState.onApply) {
        toolState.applyButton?.removeEventListener('click', toolState.onApply);
    }
    if (toolState.onCancel) {
        toolState.cancelButton?.removeEventListener('click', toolState.onCancel);
    }
    if (toolState.onKeyDown) {
        toolState.input?.removeEventListener('keydown', toolState.onKeyDown);
    }
}

// The element whose click opened each tool's panel, keyed by that editor's tool state.
const clickedPanelTargets = new WeakMap<object, Element>();

// Panels open from their toolbar button or a click on an element they edit. They close once focus is back in the text,
// unless the selection is still on the clicked element; nothing reopens a panel without a new click.
function syncPanelTool<TState>(context: RichTextEditorToolContext<TState>, command: string, selector: string, close: (context: RichTextEditorToolContext<TState>, options: InlineToolCloseOptions) => void): void {
    const { element, host, toolState } = context;
    const activeTarget = closestInSurface(host.getSurface(element), selector);
    host.setToolbarButtonPressed(element, command, Boolean(activeTarget));
    // Closing an already closed panel would still clear the selection that another open panel saved.
    if (host.getToolPanel(element, command)?.hidden !== false) {
        return;
    }

    const focusedToolCommand = host.getFocusedToolCommand(element);
    const isInspectingClickedTarget = activeTarget !== null && clickedPanelTargets.get(toolState as object) === activeTarget;
    if (focusedToolCommand !== command && !isInspectingClickedTarget) {
        clickedPanelTargets.delete(toolState as object);
        close(context, { preserveSelection: Boolean(focusedToolCommand) });
    }
}

// Opens the settings of the innermost clicked element a tool edits (a link inside a table opens the link tool).
function openToolForClickedElement(element: HTMLElement, state: EditorState, surface: HTMLElement, event: MouseEvent): void {
    // An iframe takes its own clicks, so while editable it ignores pointer events (see SCSS) and is hit-tested here.
    const clickedFrame = toArray(surface.querySelectorAll('iframe')).find((frame) => {
        const rect = frame.getBoundingClientRect();
        return event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom;
    });
    const clicked = clickedFrame ?? (event.target instanceof Element ? event.target : null);
    if (!clicked || !surface.contains(clicked) || clicked === surface) {
        return;
    }

    if (clicked instanceof HTMLImageElement || clicked instanceof HTMLIFrameElement) {
        // Browsers do not select media on click; select it so the tool edits this element.
        const range = document.createRange();
        range.selectNode(clicked);
        window.getSelection?.()?.removeAllRanges();
        window.getSelection?.()?.addRange(range);
    }

    for (let node: Element | null = clicked; node && node !== surface; node = node.parentElement) {
        for (const command of state.toolCommands) {
            const tool = getRichTextEditorTool(command);
            if (tool?.targetSelector && tool.openForTarget && node.matches(tool.targetSelector)) {
                const context = createRichTextEditorToolContext(element, state, toolHost, tool);
                tool.openForTarget(context);
                clickedPanelTargets.set(context.toolState as object, node);
                updateToolbarState(element);
                return;
            }
        }
    }
}

registerRichTextEditorTool<AriaLabelToolState>({
    command: 'ariaLabel',
    createState: () => ({ input: null, applyButton: null, cancelButton: null, target: null }),
    bind: bindAriaLabelTool,
    unbind: unbindAriaLabelTool,
    setDisabled: ({ toolState }, disabled) => {
        setDisabled(toolState.input, disabled);
        setDisabled(toolState.applyButton, disabled);
        setDisabled(toolState.cancelButton, disabled);
    },
    execute: (context) => openAriaLabelEditor(context),
    close: closeAriaLabelEditor,
    targetSelector: 'span[role="group"][aria-label]',
    openForTarget: (context) => { openAriaLabelEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'ariaLabel', 'span[role="group"][aria-label]', closeAriaLabelEditor)
});

registerRichTextEditorTool<ImageToolState>({
    command: 'image',
    createState: () => ({ imageEditorInputs: [], imageFileInput: null, imageApplyButton: null, imageCancelButton: null, imageTarget: null, uploadedImageUrl: '' }),
    bind: bindImageToolControls,
    unbind: unbindImageToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.imageEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.imageFileInput, disabled);
        setDisabled(toolState.imageApplyButton, disabled);
        setDisabled(toolState.imageCancelButton, disabled);
    },
    execute: (context) => openImageEditor(context),
    close: closeImageEditor,
    targetSelector: 'img',
    openForTarget: (context) => { openImageEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'image', 'img', closeImageEditor)
});

registerRichTextEditorTool<TableToolState>({
    command: 'table',
    createState: () => ({ tableEditorInputs: [], tableApplyButton: null, tableCancelButton: null, tableTarget: null, tableGrid: null, tableSurface: null, tableContextCell: null }),
    bind: bindTableToolControls,
    unbind: unbindTableToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.tableEditorInputs.forEach((input) => setDisabled(input, disabled));
        getTableGridButtons(toolState.tableGrid).forEach((button) => setDisabled(button, disabled));
        setDisabled(toolState.tableApplyButton, disabled);
        setDisabled(toolState.tableCancelButton, disabled);
    },
    execute: (context) => openTableEditor(context),
    close: closeTableEditor,
    targetSelector: 'table',
    openForTarget: (context) => { openTableEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'table', 'table', closeTableEditor)
});

registerRichTextEditorTool<TextColorToolState>({
    command: 'textColor',
    createState: () => ({ textColorEditorInputs: [], textColorApplyButton: null, textColorCancelButton: null }),
    bind: bindTextColorToolControls,
    unbind: unbindTextColorToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.textColorEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.textColorApplyButton, disabled);
        setDisabled(toolState.textColorCancelButton, disabled);
    },
    execute: (context) => openTextColorEditor(context),
    close: closeTextColorEditor,
    targetSelector: 'span[style*="color"]',
    openForTarget: (context) => { openTextColorEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'textColor', 'span[style*="color"]', closeTextColorEditor)
});

registerRichTextEditorTool<LinkToolState>({
    command: 'link',
    createState: () => ({ linkEditorInputs: [], linkApplyButton: null, linkCancelButton: null, linkTarget: null, linkSelectedText: '' }),
    bind: bindLinkToolControls,
    unbind: unbindLinkToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.linkEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.linkApplyButton, disabled);
        setDisabled(toolState.linkCancelButton, disabled);
    },
    execute: (context) => openLinkEditor(context),
    close: closeLinkEditor,
    targetSelector: 'a',
    openForTarget: (context) => { openLinkEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'link', 'a', closeLinkEditor)
});

registerRichTextEditorTool<IframeToolState>({
    command: 'iframe',
    createState: () => ({ iframeEditorInputs: [], iframeApplyButton: null, iframeCancelButton: null, iframeTarget: null }),
    bind: bindIframeToolControls,
    unbind: unbindIframeToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.iframeEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.iframeApplyButton, disabled);
        setDisabled(toolState.iframeCancelButton, disabled);
    },
    execute: (context) => openIframeEditor(context),
    close: closeIframeEditor,
    targetSelector: 'iframe',
    openForTarget: (context) => { openIframeEditor(context, { focusInput: false }); },
    syncState: (context) => syncPanelTool(context, 'iframe', 'iframe', closeIframeEditor)
});

setRichTextEditorToolRegistryChangedCallback(initializeAllEditors);

function getTableBorderColor(tableElement: Maybe<HTMLTableElement>): string {
    if (!(tableElement instanceof HTMLTableElement)) {
        return '';
    }

    const dataBorderColor = normalizeTableBorderColor(tableElement.getAttribute('data-border-color') ?? '');
    if (dataBorderColor) {
        return dataBorderColor;
    }

    const styleAttribute = tableElement.getAttribute('style') ?? '';
    const styleMatch = styleAttribute.match(/--nt-rich-text-table-border-color:\s*([^;]+)/i);
    return normalizeTableBorderColor(styleMatch?.[1] ?? '');
}

function normalizeAlignment(value: unknown): Alignment {
    const normalized = `${value ?? ''}`.trim().toLowerCase();
    if (normalized === 'start') {
        return 'left';
    }

    if (normalized === 'end') {
        return 'right';
    }

    return normalized === 'left'
        || normalized === 'center'
        || normalized === 'right'
        || normalized === 'justify'
        ? normalized
        : '';
}

function getElementAlignment(element: unknown): Alignment {
    if (!(element instanceof Element)) {
        return '';
    }

    const textAlign = element instanceof HTMLElement ? element.style.textAlign : '';
    return normalizeAlignment(textAlign || element.getAttribute?.('align') || '');
}

function getTableRows(tableElement: HTMLTableElement): HTMLTableRowElement[] {
    const rows: HTMLTableRowElement[] = [];
    const headRows = toArray(tableElement.querySelectorAll(':scope > thead > tr')).filter(isTableRowElement);
    const bodyRows = toArray(tableElement.querySelectorAll(':scope > tbody > tr')).filter(isTableRowElement);

    if (headRows.length > 0) {
        rows.push(headRows[0]);
    } else {
        const firstRow = tableElement.querySelector(':scope > tr');
        if (firstRow instanceof HTMLTableRowElement) {
            rows.push(firstRow);
        }
    }

    rows.push(...bodyRows);
    return rows;
}

// Browsers leave <p><br></p> behind when everything is deleted; that is still an empty editor.
function isSurfaceEmpty(surface: HTMLElement): boolean {
    return !surface.textContent?.trim() && !surface.querySelector('img, iframe, table');
}

function surfaceToValue(surface: HTMLElement): string {
    return isSurfaceEmpty(surface) ? '' : flattenEditorHtml(surface.innerHTML);
}

// The value span belongs to Blazor: replacing its text node here would hide every later render from this script.
function updateSourceValue(element: HTMLElement, value: string): void {
    const hiddenInput = getHiddenInput(element);
    if (hiddenInput) {
        hiddenInput.value = value;
    }
}

function updateEmptyState(surface: Maybe<HTMLElement>): void {
    if (!surface) {
        return;
    }

    surface.dataset.empty = isSurfaceEmpty(surface) ? 'true' : 'false';
}

function applyHtmlToSurface(element: HTMLElement, html: string): void {
    const surface = getSurface(element);
    const state = editorState.get(element);
    if (!surface) {
        return;
    }

    const sanitizedHtml = flattenEditorHtml(html);
    surface.innerHTML = sanitizedHtml;
    updateEmptyState(surface);
    updateSourceValue(element, sanitizedHtml);
    if (state) {
        state.lastValue = sanitizedHtml;
        state.lastHtml = sanitizedHtml;
    }

    updateToolbarState(element);
}

function scheduleSyncValueFromSurface(element: HTMLElement, notifyDotNet: boolean): void {
    const state = editorState.get(element);
    if (!state) {
        return;
    }

    clearPendingSync(state);
    state.pendingSyncFrameId = window.requestAnimationFrame(() => {
        state.pendingSyncFrameId = null;
        syncValueFromSurface(element, notifyDotNet);
    });
}

function syncValueFromSurface(element: HTMLElement, notifyDotNet: boolean): void {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || state.isDisposed || !surface) {
        return;
    }

    cleanSurface(surface, true);
    const value = surfaceToValue(surface);
    const didValueChange = value !== state.lastValue || value !== state.lastHtml;
    state.lastValue = value;
    state.lastHtml = value;
    updateEmptyState(surface);
    updateSourceValue(element, value);
    updateToolbarState(element);

    state.hasUncommittedEdit ||= didValueChange;
    if (notifyDotNet && didValueChange) {
        sendValue(state, 'UpdateValueFromJs', value);
    }
}

function commitUncommittedEdit(element: HTMLElement): void {
    if (editorState.get(element)?.hasUncommittedEdit) {
        commitValue(element);
    }
}

function commitValue(element: HTMLElement): void {
    const state = editorState.get(element);
    const surface = getSurface(element);
    if (!state || state.isDisposed || !surface) {
        return;
    }

    if (state.sourceMode) {
        const source = getHtmlSource(element);
        if (source) {
            surface.innerHTML = readHtmlSource(source, state);
        }
    }

    clearPendingSync(state);
    // Only repair structure the user just produced; merely focusing and leaving must not rewrite a loaded value.
    cleanSurface(surface, state.hasUncommittedEdit);
    const value = surfaceToValue(surface);
    // lastHtml is what the surface held at the last render or sync, so a tool that edits the DOM without input still counts.
    const isEdited = state.hasUncommittedEdit || value !== state.lastHtml;
    if (state.sourceMode) {
        const source = getHtmlSource(element);
        if (source) {
            writeHtmlSource(source, state, value);
            updateHtmlSourceView(source);
        }
    }

    state.lastValue = value;
    state.lastHtml = value;
    state.hasUncommittedEdit = false;
    updateEmptyState(surface);
    updateSourceValue(element, value);
    updateToolbarState(element);

    if (isEdited) {
        sendValue(state, 'CommitValueFromJs', value);
        return;
    }

    // An unedited blur still reaches .NET for touch validation and OnBlurCallback, without the value.
    const dotNetRef = state.dotNetRef;
    if (dotNetRef) {
        new Promise<unknown>(resolve => resolve(dotNetRef.invokeMethodAsync('BlurFromJs'))).catch(() => { });
    }
}

function updateToolbarState(element: HTMLElement): void {
    const state = editorState.get(element);
    if (!state) {
        return;
    }

    if (state.sourceMode) {
        toolHost.getToolbarButton(element, 'editHtml')?.setAttribute('aria-pressed', 'true');
        return;
    }

    const surface = getSurface(element);
    const focusedToolCommand = getFocusedToolCommand(element);
    const selectionElement = getSelectionElement(surface);
    const hasLocalSelectionContext = Boolean(selectionElement);
    const toolbarButtons = state.toolbarButtons;

    if (!hasLocalSelectionContext && !focusedToolCommand) {
        for (const button of toolbarButtons) {
            const command = button.dataset.command ?? '';
            if (!command || getRichTextEditorTool(command)) {
                continue;
            }

            button.setAttribute('aria-pressed', 'false');
        }

        forEachRegisteredToolContext(element, state, (tool, context) => {
            tool.syncState?.(context);
        });

        return;
    }

    const blockElement = getCurrentBlockElement(surface);
    const activeBlockTag = blockElement?.tagName?.toLowerCase?.() ?? 'p';
    const activeAlignment = getCurrentAlignment(surface, blockElement);
    const isBold = hasAncestorTag(surface, 'strong, b');
    const isItalic = hasAncestorTag(surface, 'em, i');
    const isUnderline = hasAncestorTag(surface, 'u');
    const isStrikethrough = hasAncestorTag(surface, 's, strike, del');
    const isUnorderedList = hasAncestorTag(surface, 'ul');
    const isOrderedList = hasAncestorTag(surface, 'ol');
    const isBlockQuote = hasAncestorTag(surface, 'blockquote');
    const isCodeBlock = hasAncestorTag(surface, 'pre');

    for (const button of toolbarButtons) {
        const command = button.dataset.command ?? '';
        if (command && getRichTextEditorTool(command)) {
            continue;
        }

        let isSelected = false;

        if (command === 'paragraph') {
            isSelected = activeBlockTag === 'p' || activeBlockTag === 'div';
        } else if (command === 'heading') {
            isSelected = activeBlockTag === `h${button.dataset.value ?? '1'}`;
        } else if (command === 'alignLeft') {
            isSelected = activeAlignment === 'left';
        } else if (command === 'alignCenter') {
            isSelected = activeAlignment === 'center';
        } else if (command === 'alignRight') {
            isSelected = activeAlignment === 'right';
        } else if (command === 'alignJustify') {
            isSelected = activeAlignment === 'justify';
        } else if (command === 'unorderedList') {
            isSelected = isUnorderedList;
        } else if (command === 'orderedList') {
            isSelected = isOrderedList;
        } else if (command === 'blockquote') {
            isSelected = isBlockQuote;
        } else if (command === 'codeBlock') {
            isSelected = isCodeBlock;
        } else if (command === 'bold') {
            isSelected = isBold;
        } else if (command === 'italic') {
            isSelected = isItalic;
        } else if (command === 'underline') {
            isSelected = isUnderline;
        } else if (command === 'strikeThrough') {
            isSelected = isStrikethrough;
        }

        button.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }

    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.syncState?.(context);
    });
}

function getFocusedToolCommand(element: HTMLElement): string | null {
    const activeElement = document.activeElement;
    if (!(activeElement instanceof Element) || !element.contains(activeElement)) {
        return null;
    }

    return activeElement.closest('[data-tool-command]')?.getAttribute('data-tool-command') ?? null;
}

function collectToolCommands(element: Maybe<ParentNode>, toolbarButtons: HTMLButtonElement[] = getToolbarButtons(element)): string[] {
    const commands = new Set<string>();
    for (const button of toolbarButtons) {
        const command = button.dataset.command?.trim();
        if (command) {
            commands.add(command);
        }
    }

    for (const panel of toArray(element?.querySelectorAll?.('[data-tool-command]')).filter(isElement)) {
        const command = panel.getAttribute('data-tool-command')?.trim();
        if (command) {
            commands.add(command);
        }
    }

    return Array.from(commands);
}

function forEachRegisteredToolContext(
    element: HTMLElement,
    state: EditorState,
    callback: (tool: RegisteredTool, context: RegisteredToolContext) => void
): void {
    for (const command of state.toolCommands) {
        const registeredTool = getRichTextEditorTool(command);
        if (!registeredTool) {
            continue;
        }

        callback(registeredTool, createRichTextEditorToolContext(element, state, toolHost, registeredTool));
    }
}

function closeOtherTools(element: HTMLElement, state: EditorState, exceptCommand: string | null = null): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        if (!tool || tool.command === exceptCommand) {
            return;
        }

        tool.close?.(context, { preserveSelection: true });
    });
}

function saveSelectionRange(surface: HTMLElement, state: RichTextEditorToolEditorState): void {
    const selection = window.getSelection?.();
    if (!selection || selection.rangeCount === 0) {
        state.selectionRange = null;
        return;
    }

    const range = selection.getRangeAt(0);
    const commonAncestor = range.commonAncestorContainer;
    if (!surface.contains(commonAncestor)) {
        state.selectionRange = null;
        return;
    }

    state.selectionRange = range.cloneRange();
}

function restoreSelectionRange(surface: HTMLElement, state: Maybe<RichTextEditorToolEditorState>): void {
    if (!state?.selectionRange) {
        placeCaretAtEnd(surface);
        return;
    }

    const { selectionRange } = state;
    const startContainer = selectionRange.startContainer;
    const endContainer = selectionRange.endContainer;
    const isRangeAttached = startContainer.isConnected
        && endContainer.isConnected
        && surface.contains(startContainer)
        && surface.contains(endContainer);

    if (!isRangeAttached) {
        state.selectionRange = null;
        placeCaretAtEnd(surface);
        return;
    }

    surface.focus({ preventScroll: true });

    const selection = window.getSelection?.();
    if (!selection) {
        return;
    }

    selection.removeAllRanges();
    selection.addRange(selectionRange);
}

// Open panels float in the top layer next to what they edit, so opening one never pushes the document down.
const floatingPanelAnchors = new Map<HTMLElement, Element | Range>();
let floatingPanelFrame: number | null = null;

function repositionFloatingPanels(): void {
    floatingPanelFrame ??= window.requestAnimationFrame(() => {
        floatingPanelFrame = null;
        floatingPanelAnchors.forEach((anchor, panel) => {
            if (panel.isConnected) {
                placeFloatingPanel(panel, anchor);
            } else {
                floatingPanelAnchors.delete(panel);
            }
        });

        // A panel still open when its editor was disposed is pruned above; detach once none remain.
        if (!floatingPanelAnchors.size) {
            window.removeEventListener('scroll', repositionFloatingPanels, true);
            window.removeEventListener('resize', repositionFloatingPanels);
        }
    });
}

function setToolPanelFloating(element: HTMLElement, command: string, panel: HTMLElement, isOpen: boolean): void {
    // Without popover support (older browsers, jsdom) the panel simply stays in the document flow.
    if (typeof panel.showPopover !== 'function') {
        return;
    }

    const isShown = panel.matches(':popover-open');
    if (!isOpen) {
        if (isShown) {
            panel.hidePopover();
        }

        floatingPanelAnchors.delete(panel);
        if (!floatingPanelAnchors.size) {
            window.removeEventListener('scroll', repositionFloatingPanels, true);
            window.removeEventListener('resize', repositionFloatingPanels);
        }

        return;
    }

    if (!isShown) {
        panel.showPopover();
    }

    if (!floatingPanelAnchors.size) {
        window.addEventListener('scroll', repositionFloatingPanels, { capture: true, passive: true });
        window.addEventListener('resize', repositionFloatingPanels, { passive: true });
    }

    const anchor = getFloatingPanelAnchor(element, command);
    floatingPanelAnchors.set(panel, anchor);
    placeFloatingPanel(panel, anchor);
}

// The element the tool edits (clicked image, table, link...), else the selection, else the tool's toolbar button.
function getFloatingPanelAnchor(element: HTMLElement, command: string): Element | Range {
    const surface = getSurface(element);
    const selector = getRichTextEditorTool(command)?.targetSelector;
    const target = selector ? closestInSurface(surface, selector) : null;
    if (target) {
        return target;
    }

    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    if (range && surface?.contains(range.commonAncestorContainer) && range.getBoundingClientRect().height > 0) {
        return range.cloneRange();
    }

    return getCurrentBlockElement(surface) ?? toolHost.getToolbarButton(element, command) ?? element;
}

// Below the anchor when it fits (or fits better than above), aligned to its start, kept inside the viewport.
function placeFloatingPanel(panel: HTMLElement, anchor: Element | Range): void {
    const edge = 8;
    const gap = 6;
    const anchorRect = anchor.getBoundingClientRect();
    const panelRect = panel.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth;
    const viewportHeight = window.innerHeight;
    const spaceBelow = viewportHeight - anchorRect.bottom - gap - edge;
    const spaceAbove = anchorRect.top - gap - edge;
    const top = panelRect.height <= spaceBelow || spaceBelow >= spaceAbove ? anchorRect.bottom + gap : anchorRect.top - gap - panelRect.height;
    panel.style.top = `${Math.round(Math.min(Math.max(top, edge), Math.max(edge, viewportHeight - panelRect.height - edge)))}px`;
    panel.style.left = `${Math.round(Math.min(Math.max(anchorRect.left, edge), Math.max(edge, viewportWidth - panelRect.width - edge)))}px`;
}

const toolHost: RichTextEditorToolHost = {
    closeOtherTools,
    getFocusedToolCommand,
    getSelectionElement,
    getSurface,
    getToolPanel: (element: Maybe<ParentNode>, command: string) =>
        qs<HTMLElement>(element, `[data-tool-command="${command}"]`),
    getToolbarButton: (element: Maybe<ParentNode>, command: string, value?: string) => {
        const matchingButtons = getToolbarButtons(element).filter((button) => button.dataset.command === command);
        if (typeof value === 'undefined') {
            return matchingButtons[0] ?? null;
        }

        return matchingButtons.find((button) => button.dataset.value === value) ?? null;
    },
    restoreSelectionRange,
    saveSelectionRange,
    setToolPanelOpen: (element: HTMLElement, command: string, isOpen: boolean) => {
        const button = getToolbarButtons(element).find((candidate) => candidate.dataset.command === command) ?? null;
        button?.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

        const panel = qs<HTMLElement>(element, `[data-tool-command="${command}"]`);
        if (!panel) {
            return;
        }

        panel.hidden = !isOpen;
        panel.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        setToolPanelFloating(element, command, panel, isOpen);
    },
    setToolbarButtonPressed: (element: HTMLElement, command: string, isPressed: boolean, value?: string) => {
        const button = toolHost.getToolbarButton(element, command, value);
        button?.setAttribute('aria-pressed', isPressed ? 'true' : 'false');
    },
    syncValueFromSurface,
    updateToolbarState
};

function executeEditorCommand(element: HTMLElement, command: string | undefined, value?: string): boolean {
    const surface = getSurface(element);
    const state = editorState.get(element);
    if (!surface || !command) {
        return false;
    }

    if (!isEditorEditable(element)) {
        updateToolbarState(element);
        return false;
    }

    if (command === 'editHtml' && state) {
        setHtmlSourceMode(element, state, !state.sourceMode);
        return false;
    }

    if (state?.sourceMode) {
        return false;
    }

    // Every surface refocus uses preventScroll: the caret is already in view, and a plain focus() scrolls the page to
    // the editor's top whenever that top is above the viewport.
    surface.focus({ preventScroll: true });
    const tool = state ? getRichTextEditorTool(command) : null;
    if (tool && state) {
        // Panel tools return false from execute: opening a panel changes nothing yet.
        const toolContext = createRichTextEditorToolContext(element, state, toolHost, tool);
        if (!tool.execute?.(toolContext, value)) {
            updateToolbarState(element);
            return false;
        }

        syncValueFromSurface(element, true);
        return true;
    }

    switch (command) {
        case 'undo':
            document.execCommand('undo', false);
            break;
        case 'redo':
            document.execCommand('redo', false);
            break;
        case 'heading': {
            const headingTag = `H${value ?? '1'}`;
            document.execCommand('formatBlock', false, closestInSurface(surface, headingTag.toLowerCase()) ? 'P' : headingTag);
            break;
        }
        case 'paragraph':
            document.execCommand('formatBlock', false, 'P');
            break;
        case 'alignLeft':
            document.execCommand('justifyLeft', false);
            break;
        case 'alignCenter':
            document.execCommand('justifyCenter', false);
            break;
        case 'alignRight':
            document.execCommand('justifyRight', false);
            break;
        case 'alignJustify':
            document.execCommand('justifyFull', false);
            break;
        case 'unorderedList':
        case 'orderedList':
            keepCaretTextOffset(surface, () => document.execCommand(command === 'unorderedList' ? 'insertUnorderedList' : 'insertOrderedList', false));
            break;
        case 'blockquote':
            if (hasAncestorTag(surface, 'blockquote')) {
                document.execCommand('outdent', false);
            } else {
                document.execCommand('formatBlock', false, 'BLOCKQUOTE');
            }
            break;
        case 'codeBlock': {
            const codeBlock = closestInSurface(surface, 'pre');
            if (codeBlock) {
                // formatBlock only converts the line holding the caret and leaves the rest of the block inside <pre>.
                replaceCodeBlockWithParagraphs(codeBlock);
            } else {
                document.execCommand('formatBlock', false, 'PRE');
            }
            break;
        }
        default:
            document.execCommand(command, false);
            break;
    }

    syncValueFromSurface(element, true);
    return true;
}

// Chromium moves a collapsed caret to the start of the item when it turns a paragraph into a list, so the next
// Enter splits the item instead of adding one. Put the caret back at the same text offset within its block.
function keepCaretTextOffset(surface: HTMLElement, command: () => void): void {
    const selection = window.getSelection?.();
    const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
    const block = range?.collapsed ? closestInSurface(surface, blockSelector) : null;
    if (!selection || !range || !block) {
        command();
        return;
    }

    const prefix = document.createRange();
    prefix.setStart(block, 0);
    prefix.setEnd(range.startContainer, range.startOffset);
    let remaining = prefix.toString().length;
    command();

    const newBlock = closestInSurface(surface, blockSelector);
    const walker = newBlock ? document.createTreeWalker(newBlock, NodeFilter.SHOW_TEXT) : null;
    while (walker?.nextNode()) {
        const length = walker.currentNode.textContent?.length ?? 0;
        if (remaining <= length) {
            const caret = document.createRange();
            caret.setStart(walker.currentNode, remaining);
            caret.collapse(true);
            selection.removeAllRanges();
            selection.addRange(caret);
            return;
        }

        remaining -= length;
    }
}

function replaceCodeBlockWithParagraphs(codeBlock: Element): void {
    const lines = ((codeBlock as HTMLElement).innerText ?? codeBlock.textContent ?? '').replace(/\n$/, '').split('\n');
    const paragraphs = lines.map((line) => {
        const paragraph = document.createElement('p');
        paragraph.append(line || document.createElement('br'));
        return paragraph;
    });

    codeBlock.replaceWith(...paragraphs);
    const caret = document.createRange();
    caret.selectNodeContents(paragraphs[paragraphs.length - 1]);
    caret.collapse(false);
    window.getSelection?.()?.removeAllRanges();
    window.getSelection?.()?.addRange(caret);
}

function handleToolbarCommand(event: MouseEvent): void {
    const button = event.currentTarget instanceof HTMLButtonElement ? event.currentTarget : null;
    const element = button?.closest?.('nt-rich-text-editor');
    if (!(element instanceof HTMLElement)) {
        return;
    }

    const surface = getSurface(element);
    if (!surface || !button || button.disabled) {
        return;
    }

    event.preventDefault();
    executeEditorCommand(element, button.dataset.command, button.dataset.value);
}

function eventMatchesShortcut(event: KeyboardEvent, shortcutExpression: string): boolean {
    const normalizedShortcut = shortcutExpression.trim();
    if (!normalizedShortcut) {
        return false;
    }

    const shortcutParts = normalizedShortcut.split('+').map((part) => part.trim()).filter(Boolean);
    const keyPart = shortcutParts[shortcutParts.length - 1]?.toLowerCase() ?? '';
    if (!keyPart) {
        return false;
    }

    const requiresPrimaryModifier = shortcutParts.some((part) => part === 'Control' || part === 'Meta');
    const requiresAlt = shortcutParts.includes('Alt');
    const requiresShift = shortcutParts.includes('Shift');
    const hasPrimaryModifier = event.ctrlKey || event.metaKey;

    // Alt and Shift change event.key on AZERTY and macOS layouts, so letters and digits also match their physical key.
    const physicalKey = /^[a-z]$/.test(keyPart) ? `Key${keyPart.toUpperCase()}` : /^\d$/.test(keyPart) ? `Digit${keyPart}` : '';
    return hasPrimaryModifier === requiresPrimaryModifier
        && event.altKey === requiresAlt
        && event.shiftKey === requiresShift
        && (event.key?.toLowerCase() === keyPart || (physicalKey !== '' && event.code === physicalKey));
}

function tryHandleShortcut(element: HTMLElement, event: KeyboardEvent): boolean {
    for (const button of getToolbarButtons(element)) {
        if (button.disabled) {
            continue;
        }

        const shortcutExpression = button.getAttribute('aria-keyshortcuts');
        if (!shortcutExpression) {
            continue;
        }

        const shortcuts = shortcutExpression.split(/\s+/).map((value) => value.trim()).filter(Boolean);
        if (!shortcuts.some((shortcut) => eventMatchesShortcut(event, shortcut))) {
            continue;
        }

        event.preventDefault();
        executeEditorCommand(element, button.dataset.command, button.dataset.value);
        return true;
    }

    return false;
}

function handleEditorKeyDown(element: HTMLElement, event: KeyboardEvent): void {
    const surface = getSurface(element);
    if (!surface) {
        return;
    }

    if (!isEditorEditable(element)) {
        return;
    }

    if (tryHandleShortcut(element, event)) {
        return;
    }

    if (event.key !== 'Tab') {
        return;
    }

    const cell = closestInSurface(surface, 'td, th');
    if (cell) {
        moveToAdjacentTableCell(cell, event);
        return;
    }

    const listItem = closestInSurface(surface, 'li');
    const outerList = listItem?.parentElement?.parentElement?.closest('ul, ol, li') ?? null;
    // Shift+Tab on a top-level item would unwrap the list; let focus leave the editor instead.
    if (!listItem || (event.shiftKey && !surface.contains(outerList))) {
        return;
    }

    event.preventDefault();
    document.execCommand(event.shiftKey ? 'outdent' : 'indent', false);
    syncValueFromSurface(element, element.dataset.bindOnInput === 'true');
}

// Tab walks the cells of one table; past either end it leaves the table like any other Tab.
function moveToAdjacentTableCell(cell: Element, event: KeyboardEvent): void {
    const table = cell.closest('table');
    const cells = toArray(table?.querySelectorAll('th, td')).filter((candidate) => candidate.closest('table') === table);
    const target = cells[cells.indexOf(cell) + (event.shiftKey ? -1 : 1)];
    if (!target) {
        return;
    }

    event.preventDefault();
    const range = document.createRange();
    range.selectNodeContents(target);
    window.getSelection?.()?.removeAllRanges();
    window.getSelection?.()?.addRange(range);
}

function bindRegisteredToolControls(element: HTMLElement, state: EditorState): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.bind(context);
    });
}

function unbindRegisteredToolControls(element: HTMLElement, state: EditorState): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.unbind(context);
    });
}

function setRegisteredToolDisabledState(element: HTMLElement, state: EditorState, disabled: boolean): void {
    forEachRegisteredToolContext(element, state, (tool, context) => {
        tool.setDisabled?.(context, disabled);
    });
}

function ensureState(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): EditorState | null {
    if (!element) {
        return null;
    }

    const existingState = editorState.get(element);
    if (existingState) {
        existingState.dotNetRef = dotNetRef ?? existingState.dotNetRef;
        return existingState;
    }

    const surface = getSurface(element);
    if (!surface) {
        return null;
    }

    const state: EditorState = {
        dotNetRef,
        isDisposed: false,
        pendingSyncFrameId: null,
        hasUncommittedEdit: false,
        requiresInitialRender: true,
        toolbarButtons: [],
        toolCommands: [],
        selectionRange: null,
        toolStates: new Map<string, unknown>(),
        lastValue: getSourceValueElement(element)?.textContent ?? '',
        lastHtml: '',
        renderedValue: '',
        sentValueCount: 0,
        sourceMode: false,
        sourceEmbeddedData: [],
        onInput: () => {
            state.hasUncommittedEdit = true;
            const shouldNotify = element.dataset.bindOnInput === 'true';
            if (shouldNotify) {
                syncValueFromSurface(element, true);
                return;
            }

            scheduleSyncValueFromSurface(element, false);
        },
        onSourceInput: () => {
            const source = getHtmlSource(element);
            if (!source) {
                return;
            }

            updateHtmlSourceView(source);
            const html = readHtmlSource(source, state);
            surface.innerHTML = html;
            updateEmptyState(surface);
            const hiddenInput = getHiddenInput(element);
            if (hiddenInput) {
                hiddenInput.value = html;
            }

            state.hasUncommittedEdit = true;
            if (element.dataset.bindOnInput === 'true') {
                if (html !== state.lastValue || html !== state.lastHtml) {
                    state.lastValue = html;
                    state.lastHtml = html;
                    updateSourceValue(element, html);
                    sendValue(state, 'UpdateValueFromJs', html);
                }
            }
        },
        onSourceScroll: () => {
            const source = getHtmlSource(element);
            if (source) {
                syncHtmlSourceScroll(source);
            }
        },
        onWindowResize: () => {
            const source = getHtmlSource(element);
            if (state.sourceMode && source) {
                updateHtmlSourceView(source);
            }
        },
        onFocus: () => {
            // Enter after a heading or quote otherwise creates <div>. Firefox only accepts this while an editor is focused.
            document.execCommand('defaultParagraphSeparator', false, 'p');
            if (isAwaitingDotNet(element)) {
                prerenderFocusedEditorIndex = getEditorIndex(element);
            }

            setActiveEditor(element);
            updateToolbarState(element);
        },
        onFocusIn: () => {
            setActiveEditor(element);
            updateToolbarState(element);
        },
        onFocusOut: (event: FocusEvent) => {
            // Commit before this event ends: a click handler that removes the editor can run before deferred work would.
            const isMovingWithinEditor = event.relatedTarget instanceof Node && element.contains(event.relatedTarget);
            const isWindowLosingFocus = !document.hasFocus() && element.contains(document.activeElement);
            if (isMovingWithinEditor || isWindowLosingFocus) {
                return;
            }

            clearActiveEditor(element);
            updateToolbarState(element);
            commitValue(element);
        },
        onKeyUp: () => {
            updateToolbarState(element);
        },
        onMouseUp: () => {
            updateToolbarState(element);
        },
        onClick: (event: MouseEvent) => {
            if (isEditorEditable(element) && !state.sourceMode) {
                openToolForClickedElement(element, state, surface, event);
            }
        },
        onKeyDown: (event: KeyboardEvent) => {
            handleEditorKeyDown(element, event);
        },
        onPanelKeyDown: (event: KeyboardEvent) => {
            // Input handlers close their own panel; this covers focus on a panel's buttons.
            const command = event.target instanceof Element ? event.target.closest('[data-tool-command]')?.getAttribute('data-tool-command') : null;
            const tool = command ? getRichTextEditorTool(command) : null;
            if (event.key === 'Escape' && !event.defaultPrevented && tool) {
                event.preventDefault();
                tool.close?.(createRichTextEditorToolContext(element, state, toolHost, tool), { focusSurface: true });
            }
        },
        onToolbarMouseDown: (event: MouseEvent) => {
            event.preventDefault();
        }
    };

    surface.addEventListener('input', state.onInput);
    surface.addEventListener('focus', state.onFocus);
    surface.addEventListener('keyup', state.onKeyUp);
    surface.addEventListener('mouseup', state.onMouseUp);
    surface.addEventListener('click', state.onClick);
    surface.addEventListener('keydown', state.onKeyDown);
    getHtmlSource(element)?.addEventListener('input', state.onSourceInput);
    getHtmlSource(element)?.addEventListener('scroll', state.onSourceScroll);
    window.addEventListener('resize', state.onWindowResize);
    element.addEventListener('focusin', state.onFocusIn);
    element.addEventListener('focusout', state.onFocusOut);
    element.addEventListener('keydown', state.onPanelKeyDown);

    state.toolCommands = collectToolCommands(element, state.toolbarButtons);
    bindToolbarButtons(element, state);
    bindRegisteredToolControls(element, state);
    editorElements.add(element);
    editorState.set(element, state);
    return state;
}

function synchronizeElement(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    if (!element) {
        return;
    }

    const state = ensureState(element, dotNetRef);
    if (!state) {
        return;
    }

    state.isDisposed = false;
    state.dotNetRef = dotNetRef ?? state.dotNetRef;

    const value = flattenEditorHtml(getSourceValueElement(element)?.textContent ?? '');
    const didRenderNewValue = value !== state.renderedValue;
    state.renderedValue = value;

    // Typed text wins until it is committed, and a render that has not seen everything sent to .NET carries older text.
    const isStaleRender = Number(element.dataset.receivedValueCount ?? 0) < state.sentValueCount;
    if (state.requiresInitialRender || (didRenderNewValue && !state.hasUncommittedEdit && !isStaleRender && value !== state.lastValue)) {
        applyHtmlToSurface(element, value);
        if (state.sourceMode) {
            const source = getHtmlSource(element);
            if (source) {
                writeHtmlSource(source, state, value);
                updateHtmlSourceView(source);
            }
        }
        state.requiresInitialRender = false;
    } else {
        updateEmptyState(getSurface(element));
        if (!state.sourceMode) {
            updateSourceValue(element, state.lastValue);
        }
    }

    const isEditable = isEditorEditable(element);
    const surface = getSurface(element);
    if (surface) {
        const becameEditable = isEditable && surface.getAttribute('contenteditable') !== 'true';
        surface.setAttribute('contenteditable', isEditable ? 'true' : 'false');
        surface.setAttribute('aria-readonly', isEditable ? 'false' : 'true');
        if (isAwaitingDotNet(element) && document.activeElement === surface) {
            prerenderFocusedEditorIndex = getEditorIndex(element);
        }

        if (becameEditable && shouldTakeFocus(element, surface)) {
            placeCaretAtEnd(surface);
        }
    }
    const source = getHtmlSource(element);
    if (source) {
        source.disabled = !isEditable;
    }

    bindToolbarButtons(element, state);
    state.toolCommands = collectToolCommands(element, state.toolbarButtons);
    unbindRegisteredToolControls(element, state);
    bindRegisteredToolControls(element, state);

    for (const button of state.toolbarButtons) {
        button.disabled = !isEditable || (state.sourceMode && button.dataset.command !== 'editHtml');
    }

    setRegisteredToolDisabledState(element, state, !isEditable);

    if (!isEditable) {
        closeOtherTools(element, state);
    }

    if (surface) {
        cleanSurface(surface, false);
    }

    const currentHtml = surface ? surfaceToValue(surface) : '';
    if (currentHtml !== state.lastHtml) {
        state.lastHtml = currentHtml;
        sendValue(state, 'UpdateMarkupValueFromJs', currentHtml);
    }
    updateToolbarState(element);
}

function initializeAllEditors(): void {
    for (const element of toArray(document.querySelectorAll('nt-rich-text-editor')).filter(isHtmlElement)) {
        synchronizeElement(element, null);
    }
}

function getLifecycleEditorElement(element: Maybe<HTMLElement>): HTMLElement | null {
    if (!element) {
        return null;
    }

    if (element.matches('nt-rich-text-editor')) {
        return element;
    }

    const closestEditor = element.closest?.('nt-rich-text-editor');
    return closestEditor instanceof HTMLElement ? closestEditor : null;
}

function registerSelectionChangeHandler(): void {
    if (selectionChangeRegistered) {
        return;
    }

    selectionChangeRegistered = true;
    document.addEventListener('selectionchange', () => {
        const selectionAnchorNode = window.getSelection?.()?.anchorNode ?? null;
        const selectionEditor = selectionAnchorNode instanceof Element
            ? selectionAnchorNode.closest('nt-rich-text-editor')
            : selectionAnchorNode?.parentElement?.closest?.('nt-rich-text-editor') ?? null;
        const editorToUpdate = selectionEditor instanceof HTMLElement
            ? selectionEditor
            : activeEditor;

        if (editorToUpdate && editorToUpdate.isConnected) {
            updateToolbarState(editorToUpdate);
        }
    });
}

function commitAllUncommittedEdits(): void {
    editorElements.forEach(commitUncommittedEdit);
}

function registerUncommittedEditHandlers(): void {
    if (uncommittedEditHandlersRegistered) {
        return;
    }

    uncommittedEditHandlersRegistered = true;

    // A script or assistive-technology click moves no focus, and the app handler that runs next may switch records.
    window.addEventListener('click', (event) => {
        if (activeEditor && !(event.target instanceof Node && activeEditor.contains(event.target))) {
            commitUncommittedEdit(activeEditor);
        }
    }, true);

    // beforeunload gives a Server commit a head start on the circuit teardown; pagehide covers browsers that skip it.
    window.addEventListener('beforeunload', commitAllUncommittedEdits, true);
    window.addEventListener('pagehide', commitAllUncommittedEdits, true);
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            commitAllUncommittedEdits();
        }
    });
}

export function focusEditor(element: Maybe<HTMLElement>): void {
    getSurface(getLifecycleEditorElement(element))?.focus();
}

export function onLoad(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    registerSelectionChangeHandler();
    registerUncommittedEditHandlers();

    const editorElement = getLifecycleEditorElement(element);
    if (!editorElement) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(editorElement, dotNetRef);
}

export function onUpdate(element: Maybe<HTMLElement>, dotNetRef: DotNetEditorRef | null): void {
    const editorElement = getLifecycleEditorElement(element);
    if (!editorElement) {
        initializeAllEditors();
        return;
    }

    synchronizeElement(editorElement, dotNetRef);
}

export function onDispose(element: Maybe<HTMLElement>): void {
    const editorElement = getLifecycleEditorElement(element);
    if (!editorElement) {
        return;
    }

    const state = editorState.get(editorElement);
    const surface = getSurface(editorElement);
    if (state) {
        commitUncommittedEdit(editorElement);
        state.isDisposed = true;
        state.dotNetRef = null;
        clearPendingSync(state);
    }

    if (state && surface) {
        surface.removeEventListener('input', state.onInput);
        surface.removeEventListener('focus', state.onFocus);
        surface.removeEventListener('keyup', state.onKeyUp);
        surface.removeEventListener('mouseup', state.onMouseUp);
        surface.removeEventListener('click', state.onClick);
        surface.removeEventListener('keydown', state.onKeyDown);
    }

    if (state) {
        getHtmlSource(editorElement)?.removeEventListener('input', state.onSourceInput);
        getHtmlSource(editorElement)?.removeEventListener('scroll', state.onSourceScroll);
        window.removeEventListener('resize', state.onWindowResize);
    }

    if (state) {
        editorElement.removeEventListener('focusin', state.onFocusIn);
        editorElement.removeEventListener('focusout', state.onFocusOut);
        editorElement.removeEventListener('keydown', state.onPanelKeyDown);
    }

    unbindToolbarButtons(state?.toolbarButtons ?? getToolbarButtons(editorElement), state);
    if (state) {
        unbindRegisteredToolControls(editorElement, state);
    }

    clearActiveEditor(editorElement);
    editorElements.delete(editorElement);
    editorState.delete(editorElement);
}

export const __testHooks = {
    blockNodeTags,
    blockSelector,
    defaultTableBorderColor,
    defaultTextColor,
    editorElements,
    editorState,
    toolHost,
    toArray,
    qs,
    isElement,
    isHtmlButtonElement,
    isHtmlElement,
    isHtmlInputElement,
    isHtmlSpanElement,
    isTableCellElement,
    isTableRowElement,
    getTableCells,
    clearPendingSync,
    placeCaretAtEnd,
    getSurface,
    getSourceValueElement,
    getHiddenInput,
    getToolbarButtons,
    isEditorEditable,
    setActiveEditor,
    clearActiveEditor,
    sendValue,
    extractUrlScheme,
    normalizeSafeUrl,
    unbindToolbarButtons,
    bindToolbarButtons,
    getChildNodeIndex,
    getBoundaryAdjacentElement,
    getSelectionElement,
    getCurrentBlockElement,
    getCurrentAlignment,
    hasAncestorTag,
    getElementTextColor,
    normalizeHexColor,
    convertRgbChannelToHex,
    normalizeTextColorValue,
    escapeHtml,
    sanitizeDimensionAttribute,
    sanitizeElementStyle,
    sanitizeElementAttributes,
    sanitizeHtmlFragment,
    sanitizeEditorHtml,
    normalizeImageDimension,
    normalizeTableBorderColor,
    buildTableStyleAttribute,
    buildImageHtml,
    renderHtmlImage,
    getImageDetails,
    buildTableHtml,
    applyTableAction,
    highlightTableGrid,
    getTableBorderColor,
    normalizeAlignment,
    getElementAlignment,
    getTableRows,
    surfaceToValue,
    isSurfaceEmpty,
    cleanSurface,
    normalizeEditorStructure,
    updateSourceValue,
    updateEmptyState,
    applyHtmlToSurface,
    scheduleSyncValueFromSurface,
    syncValueFromSurface,
    commitValue,
    updateToolbarState,
    getFocusedToolCommand,
    collectToolCommands,
    forEachRegisteredToolContext,
    closeOtherTools,
    saveSelectionRange,
    restoreSelectionRange,
    executeEditorCommand,
    handleToolbarCommand,
    eventMatchesShortcut,
    tryHandleShortcut,
    handleEditorKeyDown,
    bindRegisteredToolControls,
    unbindRegisteredToolControls,
    setRegisteredToolDisabledState,
    ensureState,
    synchronizeElement,
    initializeAllEditors,
    getLifecycleEditorElement,
    registerSelectionChangeHandler
};
