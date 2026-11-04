// Source of truth for the rich text editor module.
// Rebuild NTRichTextEditor.razor.js with: npm run build:rich-text-editor
type Maybe<T> = T | null | undefined;
type Alignment = '' | 'left' | 'center' | 'right' | 'justify';
type DotNetEditorMethod = 'UpdateValueFromJs' | 'CommitValueFromJs' | 'UpdateMarkupValueFromJs';

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
}

interface DotNetEditorRef {
    invokeMethodAsync(methodName: 'UpdateValueFromJs' | 'CommitValueFromJs', value: string, html: string): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'UpdateMarkupValueFromJs', html: string): Promise<unknown> | void;
}

interface ImageDetails {
    src: string;
    alt: string;
    title: string;
    width: string;
    height: string;
}

interface ExistingTableContent {
    caption: string;
    headers: string[];
    rows: string[][];
}

interface TableEditorDetails {
    columns: number;
    rows: number;
    borderColor: string;
    caption: string;
}

interface TableHtmlOptions {
    columns: number;
    rows: number;
    borderColor: string;
    caption?: string;
    existingContent?: ExistingTableContent | null;
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
    onInput: () => void;
    onSourceInput: () => void;
    onSourceScroll: () => void;
    onWindowResize: () => void;
    onFocus: () => void;
    onFocusIn: () => void;
    onFocusOut: (event: FocusEvent) => void;
    onKeyUp: () => void;
    onMouseUp: () => void;
    onKeyDown: (event: KeyboardEvent) => void;
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
const defaultTableBorderColor = '#94a3b8';
const defaultTextColor = '#1d4ed8';
const editorElements = new Set<HTMLElement>();
let selectionChangeRegistered = false;
let uncommittedEditHandlersRegistered = false;
let activeEditor: HTMLElement | null = null;
let prerenderFocusedEditorIndex = -1;

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
    surface.focus();

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
    return qs<HTMLElement>(element, '.tnt-rich-text-editor-surface');
}

function getHtmlSource(element: Maybe<ParentNode>): HTMLTextAreaElement | null {
    return qs<HTMLTextAreaElement>(element, '[data-role="html-source"]');
}

function syncHtmlSourceScroll(source: HTMLTextAreaElement): void {
    const code = source.parentElement?.querySelector<HTMLElement>('.tnt-rich-text-editor-source-highlight code');
    if (code) {
        code.style.transform = `translate(${-source.scrollLeft}px, ${-source.scrollTop}px)`;
    }
}

function updateHtmlSourceView(source: HTMLTextAreaElement): void {
    const code = source.parentElement?.querySelector<HTMLElement>('.tnt-rich-text-editor-source-highlight code');
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

function setHtmlSourceMode(element: HTMLElement, state: EditorState, enabled: boolean): void {
    const surface = getSurface(element);
    const source = getHtmlSource(element);
    if (!surface || !source || state.sourceMode === enabled) {
        return;
    }

    if (enabled) {
        syncValueFromSurface(element, true);
        closeOtherTools(element, state);
        source.value = formatHtmlSource(state.lastHtml);
    } else {
        surface.innerHTML = flattenEditorHtml(source.value);
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
        surface.focus();
    }
}

function getSourceValueElement(element: Maybe<ParentNode>): HTMLDivElement | null {
    return qs<HTMLDivElement>(element, '.tnt-rich-text-editor-value');
}

function getHiddenInput(element: Maybe<ParentNode>): HTMLInputElement | null {
    return qs<HTMLInputElement>(element, '.tnt-rich-text-editor-hidden-input');
}

function getLengthElement(element: Maybe<Element>): HTMLElement | null {
    return qs<HTMLElement>(element?.closest?.('.tnt-input-container') ?? null, '.tnt-input-length');
}

function getToolbarButtons(element: Maybe<ParentNode>): HTMLButtonElement[] {
    return toArray(element?.querySelectorAll?.('.tnt-rich-text-editor-toolbar .tnt-rich-text-editor-toolbar-button')).filter(isHtmlButtonElement);
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

function invokeDotNetVoid(
    dotNetRef: Maybe<DotNetEditorRef>,
    methodName: DotNetEditorMethod,
    ...args: string[]
): void {
    if (!dotNetRef) {
        return;
    }

    const invocation = methodName === 'UpdateMarkupValueFromJs'
        ? dotNetRef.invokeMethodAsync('UpdateMarkupValueFromJs', args[0] ?? '')
        : dotNetRef.invokeMethodAsync(methodName, args[0] ?? '', args[1] ?? '');

    Promise.resolve(invocation)
        .catch(() => {
            // Ignore late/disconnected interop failures from fire-and-forget editor sync paths.
        });
}

function sendValue(state: EditorState, methodName: 'UpdateValueFromJs' | 'CommitValueFromJs', value: string, html: string): void {
    if (!state.dotNetRef) {
        return;
    }

    state.sentValueCount++;
    Promise.resolve(state.dotNetRef.invokeMethodAsync(methodName, value, html))
        .catch(() => {
            // A call that never reached .NET must not make later renders look stale.
            state.sentValueCount--;
        });
}

function extractUrlScheme(value: string): string {
    const match = value.match(/^([a-z][a-z0-9+.-]*):/i);
    return match?.[1]?.toLowerCase() ?? '';
}

function normalizeSafeUrl(value: unknown, kind: 'link' | 'iframe' | 'image'): string {
    const trimmed = `${value ?? ''}`.trim();
    if (!trimmed) {
        return '';
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

        if (direction === 'before') {
            if (currentOffset > 0) {
                const candidateNode = childNodes[currentOffset - 1];
                return candidateNode instanceof Element && surface.contains(candidateNode) ? candidateNode : null;
            }
        } else if (currentOffset < childNodes.length) {
            const candidateNode = childNodes[currentOffset];
            return candidateNode instanceof Element && surface.contains(candidateNode) ? candidateNode : null;
        }

        if (currentElement === surface) {
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

function hasAncestorTag(surface: Maybe<HTMLElement>, tagNames: string): boolean {
    const selectionElement = getSelectionElement(surface);
    return Boolean(selectionElement?.closest?.(tagNames));
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

        if (!['align', 'alt', 'aria-label', 'data-border-color', 'data-language', 'height', 'href', 'loading', 'scope', 'src', 'title', 'width'].includes(name)) {
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
    }
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
    onTableApply?: (event: MouseEvent) => void;
    onTableCancel?: (event: MouseEvent) => void;
    onTableEditorKeyDown?: (event: KeyboardEvent) => void;
}

interface TextColorToolState {
    textColorEditorInputs: HTMLInputElement[];
    textColorApplyButton: HTMLButtonElement | null;
    textColorCancelButton: HTMLButtonElement | null;
    textColorTarget: HTMLSpanElement | null;
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
        host.getSurface(element)?.focus();
    }
}

function getSelectionClosest<TElement extends Element>(surface: Maybe<HTMLElement>, selector: string, guard: (value: unknown) => value is TElement): TElement | null {
    const selectionElement = getSelectionElement(surface);
    const candidate = selectionElement?.closest?.(selector) ?? null;
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
    const { element, host } = context;
    if (!(file instanceof File) || !file.type.startsWith('image/')) {
        return;
    }

    const reader = new FileReader();
    reader.onload = () => {
        const imageUrlInput = getImageUrlInput(element);
        if (typeof reader.result === 'string' && imageUrlInput) {
            imageUrlInput.value = reader.result;
            const imageAltInput = getImageAltInput(element);
            if (imageAltInput && !imageAltInput.value.trim()) {
                imageAltInput.value = file.name.replace(/\.[^.]+$/, '');
            }

            void applyImageEditor(context)
                .then((applied) => {
                    if (applied) {
                        host.syncValueFromSurface(element, true);
                        host.updateToolbarState(element);
                    }
                });
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

function closeImageEditor(context: RichTextEditorToolContext<ImageToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.imageTarget = null;
    context.toolState.imageFileInput && (context.toolState.imageFileInput.value = '');
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
    const imageUrlInput = getImageUrlInput(element);
    if (imageUrlInput) {
        imageUrlInput.value = imageDetails.src;
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

    if (toolState.imageFileInput) {
        toolState.imageFileInput.value = '';
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

    const imageUrl = normalizeSafeUrl(getImageUrlInput(element)?.value ?? '', 'image');
    if (!imageUrl) {
        getImageUrlInput(element)?.focus();
        return false;
    }

    const imageUrlInput = getImageUrlInput(element);
    if (imageUrlInput) {
        imageUrlInput.value = imageUrl;
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

function getTableEditorDetails(tableElement: Maybe<HTMLTableElement>): TableEditorDetails {
    if (!(tableElement instanceof HTMLTableElement)) {
        return {
            columns: 3,
            rows: 2,
            borderColor: defaultTableBorderColor,
            caption: ''
        };
    }

    const rows = getTableRows(tableElement);
    const headerCells = getTableCells(rows[0]);
    const bodyRows = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0);
    return {
        columns: Math.min(Math.max(headerCells.length || 3, 1), 8),
        rows: Math.min(Math.max(bodyRows.length || 2, 1), 12),
        borderColor: getTableBorderColor(tableElement) || defaultTableBorderColor,
        caption: tableElement.querySelector(':scope > caption')?.textContent?.trim() ?? ''
    };
}

function getExistingTableContent(tableElement: Maybe<HTMLTableElement>): ExistingTableContent {
    if (!(tableElement instanceof HTMLTableElement)) {
        return {
            caption: '',
            headers: [],
            rows: []
        };
    }

    const rows = getTableRows(tableElement);
    const headerCells = getTableCells(rows[0]);
    const bodyRows = rows.slice(1)
        .map((row) => getTableCells(row))
        .filter((cells) => cells.length > 0);
    return {
        caption: tableElement.querySelector(':scope > caption')?.innerHTML?.trim() ?? '',
        headers: headerCells.map((cell) => cell.innerHTML.trim()),
        rows: bodyRows.map((cells) => cells.map((cell) => cell.innerHTML.trim()))
    };
}

function getTableCellMarkup(content: string | undefined, fallbackText: string, tagName: 'th' | 'td'): string {
    const normalizedContent = `${content ?? ''}`.trim();
    return normalizedContent.length > 0 ? `<${tagName}>${normalizedContent}</${tagName}>` : `<${tagName}>${escapeHtml(fallbackText)}</${tagName}>`;
}

function clampTableColumns(value: number): number {
    return Math.min(Math.max(value, 1), 8);
}

function clampTableRows(value: number): number {
    return Math.min(Math.max(value, 1), 12);
}

function buildTableHtml({ columns, rows, borderColor, caption = '', existingContent = null }: TableHtmlOptions): string {
    const normalizedBorderColor = normalizeTableBorderColor(borderColor) || defaultTableBorderColor;
    const captionContent = `${caption || existingContent?.caption || ''}`.trim();
    const captionMarkup = captionContent ? `<caption>${escapeHtml(captionContent)}</caption>` : '';
    const headerMarkup = Array.from({ length: columns }, (_, columnIndex) => getTableCellMarkup(existingContent?.headers?.[columnIndex], `Header ${columnIndex + 1}`, 'th')).join('');
    const bodyMarkup = Array.from({ length: rows }, (_, rowIndex) => `<tr>${Array.from({ length: columns }, (_, columnIndex) => getTableCellMarkup(existingContent?.rows?.[rowIndex]?.[columnIndex], `Cell ${rowIndex + 1}-${columnIndex + 1}`, 'td')).join('')}</tr>`).join('');
    return `<table${buildTableStyleAttribute(normalizedBorderColor)}>${captionMarkup}<thead><tr>${headerMarkup}</tr></thead><tbody>${bodyMarkup}</tbody></table>`;
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
}

function bindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { element, toolState } = context;
    initializeTableToolStateHandlers(context);
    unbindTableToolControls(context);
    toolState.tableApplyButton = getRoleButton(element, 'table-apply');
    toolState.tableCancelButton = getRoleButton(element, 'table-cancel');
    toolState.tableEditorInputs = [getRoleInput(element, 'table-columns'), getRoleInput(element, 'table-rows'), getRoleInput(element, 'table-border-color'), getRoleInput(element, 'table-caption')].filter(isHtmlInputElement);
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
}

function unbindTableToolControls(context: RichTextEditorToolContext<TableToolState>): void {
    const { toolState } = context;
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
}

function closeTableEditor(context: RichTextEditorToolContext<TableToolState>, options: InlineToolCloseOptions = {}): void {
    context.toolState.tableTarget = null;
    closeInlineTool(context, 'table', options);
}

function openTableEditor(context: RichTextEditorToolContext<TableToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface || !getToolEditor(element, 'table')) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'table');
    const existingTable = getSelectionClosest(surface, 'table', (value): value is HTMLTableElement => value instanceof HTMLTableElement);
    const tableDetails = getTableEditorDetails(existingTable);
    host.saveSelectionRange(surface, editorState);
    toolState.tableTarget = existingTable;
    const columnsInput = getRoleInput(element, 'table-columns');
    const rowsInput = getRoleInput(element, 'table-rows');
    const borderColorInput = getRoleInput(element, 'table-border-color');
    if (columnsInput) {
        columnsInput.value = `${tableDetails.columns}`;
    }

    if (rowsInput) {
        rowsInput.value = `${tableDetails.rows}`;
    }

    if (borderColorInput) {
        borderColorInput.value = tableDetails.borderColor;
    }

    const captionInput = getRoleInput(element, 'table-caption');
    if (captionInput) {
        captionInput.value = tableDetails.caption;
    }

    host.setToolPanelOpen(element, 'table', true);
    if (focusInput) {
        columnsInput?.focus();
        if (selectInputText) {
            columnsInput?.select();
        }
    }

    return false;
}

function applyTableEditor(context: RichTextEditorToolContext<TableToolState>): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const requestedColumns = Number.parseInt(getRoleInput(element, 'table-columns')?.value ?? '', 10);
    const requestedRows = Number.parseInt(getRoleInput(element, 'table-rows')?.value ?? '', 10);
    if (Number.isNaN(requestedColumns)) {
        getRoleInput(element, 'table-columns')?.focus();
        return false;
    }

    if (Number.isNaN(requestedRows)) {
        getRoleInput(element, 'table-rows')?.focus();
        return false;
    }

    const columns = clampTableColumns(requestedColumns);
    const rows = clampTableRows(requestedRows);
    const borderColor = normalizeTableBorderColor(getRoleInput(element, 'table-border-color')?.value ?? '') || defaultTableBorderColor;
    const caption = getRoleInput(element, 'table-caption')?.value?.trim?.() ?? '';
    const tableTarget = toolState.tableTarget;
    if (tableTarget instanceof HTMLTableElement && surface.contains(tableTarget)) {
        const template = document.createElement('template');
        template.innerHTML = buildTableHtml({ columns, rows, borderColor, caption, existingContent: getExistingTableContent(tableTarget) });
        const replacementTable = template.content.querySelector('table');
        if (!(replacementTable instanceof HTMLTableElement)) {
            return false;
        }

        tableTarget.replaceWith(replacementTable);
        toolState.tableTarget = replacementTable;
    } else {
        host.restoreSelectionRange(surface, editorState);
        document.execCommand('insertHTML', false, `${buildTableHtml({ columns, rows, borderColor, caption })}<p><br></p>`);
    }

    closeTableEditor(context, { focusSurface: true });
    return true;
}

function unwrapElement(element: Element): void {
    element.replaceWith(...Array.from(element.childNodes));
}

function mergeAdjacentColorSiblings(span: HTMLSpanElement, color: string): HTMLSpanElement {
    let current = span;
    let previous = current.previousSibling;
    while (previous instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(previous)) === color) {
        const source = previous;
        previous = source.previousSibling;
        current.prepend(...Array.from(source.childNodes));
        source.remove();
    }

    let next = current.nextSibling;
    while (next instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(next)) === color) {
        const source = next;
        next = source.nextSibling;
        current.append(...Array.from(source.childNodes));
        source.remove();
    }

    current.normalize();
    return current;
}

function normalizeTextColorSpan(span: HTMLSpanElement): HTMLSpanElement {
    const color = normalizeTextColorValue(getElementTextColor(span));
    if (!color) {
        return span;
    }

    span.style.color = color;
    for (const descendant of Array.from(span.querySelectorAll('span[style*="color"]'))) {
        if (descendant instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(descendant)) === color) {
            unwrapElement(descendant);
        }
    }

    const parent = span.parentElement;
    if (parent instanceof HTMLSpanElement && normalizeTextColorValue(getElementTextColor(parent)) === color) {
        for (const childNode of Array.from(span.childNodes)) {
            parent.insertBefore(childNode, span);
        }

        span.remove();
        return mergeAdjacentColorSiblings(parent, color);
    }

    return mergeAdjacentColorSiblings(span, color);
}

function applyTextColorRange(range: Range, color: string): void {
    const selectedContent = range.extractContents();
    const wrapper = document.createElement('span');
    wrapper.style.color = color;
    wrapper.appendChild(selectedContent);
    range.insertNode(wrapper);
    const normalizedWrapper = normalizeTextColorSpan(wrapper);
    const selection = window.getSelection?.();
    if (selection) {
        const updatedRange = document.createRange();
        updatedRange.selectNodeContents(normalizedWrapper);
        selection.removeAllRanges();
        selection.addRange(updatedRange);
    }
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
    context.toolState.textColorTarget = null;
    closeInlineTool(context, 'textColor', options);
}

function openTextColorEditor(context: RichTextEditorToolContext<TextColorToolState>, { focusInput = true, selectInputText = true }: { focusInput?: boolean; selectInputText?: boolean } = {}): boolean {
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    host.closeOtherTools(element, editorState, 'textColor');
    const selectionElement = host.getSelectionElement(surface);
    const existingSpanCandidate = selectionElement?.closest?.('span[style*="color"]') ?? null;
    const existingSpan = isHtmlSpanElement(existingSpanCandidate) ? existingSpanCandidate : null;
    host.saveSelectionRange(surface, editorState);
    toolState.textColorTarget = existingSpan;
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
    const { element, editorState, host, toolState } = context;
    const surface = host.getSurface(element);
    if (!surface) {
        return false;
    }

    const color = normalizeTextColorValue(getRoleInput(element, 'text-color-value')?.value ?? '') || defaultTextColor;
    const textColorTarget = toolState.textColorTarget;
    if (textColorTarget instanceof HTMLSpanElement && surface.contains(textColorTarget)) {
        textColorTarget.style.color = color;
        normalizeTextColorSpan(textColorTarget);
    } else {
        host.restoreSelectionRange(surface, editorState);
        const selection = window.getSelection?.();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || range.collapsed || !surface.contains(range.commonAncestorContainer)) {
            getRoleInput(element, 'text-color-value')?.focus();
            return false;
        }

        applyTextColorRange(range, color);
    }

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
    toolState.linkSelectedText = selectedText;
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

    const linkUrl = normalizeSafeUrl(getRoleInput(element, 'link-url')?.value ?? '', 'link');
    if (!linkUrl) {
        getRoleInput(element, 'link-url')?.focus();
        return false;
    }

    const linkTextInput = getRoleInput(element, 'link-text')?.value?.trim?.() ?? '';
    const linkText = linkTextInput || toolState.linkSelectedText || linkUrl;
    const linkAriaLabel = getRoleInput(element, 'link-aria-label')?.value?.trim?.() ?? '';
    const linkTitle = getRoleInput(element, 'link-title')?.value?.trim?.() ?? '';
    const linkTarget = toolState.linkTarget;
    if (linkTarget instanceof HTMLAnchorElement && surface.contains(linkTarget)) {
        linkTarget.setAttribute('href', linkUrl);
        linkAriaLabel ? linkTarget.setAttribute('aria-label', linkAriaLabel) : linkTarget.removeAttribute('aria-label');
        linkTitle ? linkTarget.setAttribute('title', linkTitle) : linkTarget.removeAttribute('title');
        linkTarget.textContent = linkText;
    } else {
        host.restoreSelectionRange(surface, editorState);
        const selection = window.getSelection?.();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || !surface.contains(range.commonAncestorContainer)) {
            getRoleInput(element, 'link-url')?.focus();
            return false;
        }

        const anchor = document.createElement('a');
        anchor.setAttribute('href', linkUrl);
        if (linkAriaLabel) {
            anchor.setAttribute('aria-label', linkAriaLabel);
        }

        if (linkTitle) {
            anchor.setAttribute('title', linkTitle);
        }

        anchor.textContent = linkText;
        range.deleteContents();
        range.insertNode(anchor);
        if (selection) {
            const updatedRange = document.createRange();
            updatedRange.selectNodeContents(anchor);
            selection.removeAllRanges();
            selection.addRange(updatedRange);
        }
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

    return `<iframe src="${escapeHtml(normalizedUrl)}" title="${escapeHtml(title)}" width="${escapeHtml(width)}" height="${escapeHtml(height)}" loading="lazy"></iframe>`;
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
    const iframeWidth = normalizeIframeField(getRoleInput(element, 'iframe-width')?.value, defaultIframeWidth);
    const iframeHeight = normalizeIframeField(getRoleInput(element, 'iframe-height')?.value, defaultIframeHeight);
    const iframeTarget = toolState.iframeTarget;
    if (iframeTarget instanceof HTMLIFrameElement && surface.contains(iframeTarget)) {
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

function openAriaLabelEditor(context: RichTextEditorToolContext<AriaLabelToolState>): boolean {
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
    toolState.input?.focus();
    return false;
}

function applyAriaLabelEditor(context: RichTextEditorToolContext<AriaLabelToolState>): boolean {
    const { element, editorState: state, host, toolState } = context;
    const surface = host.getSurface(element);
    const label = toolState.input?.value.trim() ?? '';
    if (!surface || !label) {
        toolState.input?.focus();
        return false;
    }

    if (toolState.target && surface.contains(toolState.target)) {
        toolState.target.setAttribute('aria-label', label);
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
    execute: openAriaLabelEditor,
    close: closeAriaLabelEditor,
    syncState: ({ element, host }) => {
        const target = getSelectionClosest(host.getSurface(element), 'span[role="group"][aria-label]', isHtmlSpanElement);
        host.setToolbarButtonPressed(element, 'ariaLabel', Boolean(target));
    }
});

registerRichTextEditorTool<ImageToolState>({
    command: 'image',
    createState: () => ({ imageEditorInputs: [], imageFileInput: null, imageApplyButton: null, imageCancelButton: null, imageTarget: null }),
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
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const existingImage = getSelectionClosest(surface, 'img', isHtmlImageElement) instanceof HTMLImageElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);
        host.setToolbarButtonPressed(element, 'image', existingImage);
        if (focusedToolCommand === 'image') {
            return;
        }

        if (focusedToolCommand || !existingImage) {
            closeImageEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openImageEditor(context, { focusInput: false, selectInputText: false });
    }
});

registerRichTextEditorTool<TableToolState>({
    command: 'table',
    createState: () => ({ tableEditorInputs: [], tableApplyButton: null, tableCancelButton: null, tableTarget: null }),
    bind: bindTableToolControls,
    unbind: unbindTableToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.tableEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.tableApplyButton, disabled);
        setDisabled(toolState.tableCancelButton, disabled);
    },
    execute: (context) => openTableEditor(context),
    close: closeTableEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const existingTable = getSelectionClosest(surface, 'table', (value): value is HTMLTableElement => value instanceof HTMLTableElement) instanceof HTMLTableElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);
        host.setToolbarButtonPressed(element, 'table', existingTable);
        if (focusedToolCommand === 'table') {
            return;
        }

        if (focusedToolCommand || !existingTable) {
            closeTableEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openTableEditor(context, { focusInput: false, selectInputText: false });
    }
});

registerRichTextEditorTool<TextColorToolState>({
    command: 'textColor',
    createState: () => ({ textColorEditorInputs: [], textColorApplyButton: null, textColorCancelButton: null, textColorTarget: null }),
    bind: bindTextColorToolControls,
    unbind: unbindTextColorToolControls,
    setDisabled: ({ toolState }, disabled) => {
        toolState.textColorEditorInputs.forEach((input) => setDisabled(input, disabled));
        setDisabled(toolState.textColorApplyButton, disabled);
        setDisabled(toolState.textColorCancelButton, disabled);
    },
    execute: (context) => openTextColorEditor(context),
    close: closeTextColorEditor,
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const selectionElement = host.getSelectionElement(surface);
        const existingColor = selectionElement?.closest?.('span[style*="color"]') instanceof HTMLSpanElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);
        host.setToolbarButtonPressed(element, 'textColor', existingColor);
        if (focusedToolCommand === 'textColor') {
            return;
        }

        if (focusedToolCommand || !existingColor) {
            closeTextColorEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openTextColorEditor(context, { focusInput: false, selectInputText: false });
    }
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
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const existingLink = getSelectionClosest(surface, 'a', isHtmlAnchorElement) instanceof HTMLAnchorElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);
        host.setToolbarButtonPressed(element, 'link', existingLink);
        if (focusedToolCommand === 'link') {
            return;
        }

        if (focusedToolCommand || !existingLink) {
            closeLinkEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openLinkEditor(context, { focusInput: false, selectInputText: false });
    }
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
    syncState: (context) => {
        const { element, host } = context;
        const surface = host.getSurface(element);
        const existingIframe = getSelectionClosest(surface, 'iframe', isHtmlIFrameElement) instanceof HTMLIFrameElement;
        const focusedToolCommand = host.getFocusedToolCommand(element);
        host.setToolbarButtonPressed(element, 'iframe', existingIframe);
        if (focusedToolCommand === 'iframe') {
            return;
        }

        if (focusedToolCommand || !existingIframe) {
            closeIframeEditor(context, { preserveSelection: Boolean(focusedToolCommand) });
            return;
        }

        openIframeEditor(context, { focusInput: false, selectInputText: false });
    }
});

setRichTextEditorToolRegistryChangedCallback(initializeAllEditors);

function normalizeTableCells<T>(cells: T[], targetCount: number, filler?: T): T[] {
    const normalized = cells.slice(0, targetCount);
    while (normalized.length < targetCount) {
        normalized.push(filler ?? ('' as T));
    }

    return normalized;
}

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

function surfaceToValue(_element: HTMLElement, surface: HTMLElement): string {
    return flattenEditorHtml(surface.innerHTML);
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

    const hasContent = Boolean(surface.textContent?.trim().length)
        || Boolean(surface.querySelector('strong, b, em, i, u, br, p, div, ul, ol, li, blockquote, pre, table, img, iframe'));
    surface.dataset.empty = hasContent ? 'false' : 'true';
}

function updateLength(element: HTMLElement, value: string): void {
    const lengthElement = getLengthElement(element);
    if (!lengthElement) {
        return;
    }

    const maxLength = getSurface(element)?.dataset.maxlength;
    if (!maxLength) {
        return;
    }

    lengthElement.textContent = `${value.length}/${maxLength}`;
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
    updateLength(element, sanitizedHtml);
    updateSourceValue(element, sanitizedHtml);
    if (state) {
        state.lastValue = sanitizedHtml;
        state.lastHtml = sanitizedHtml;
    }

    updateToolbarState(element);
}

function applySourceToSurface(element: HTMLElement, value: string): void {
    applyHtmlToSurface(element, value);
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

    const sanitizedHtml = sanitizeEditorHtml(surface.innerHTML);
    if (sanitizedHtml !== surface.innerHTML) {
        surface.innerHTML = sanitizedHtml;
    }

    const html = flattenEditorHtml(sanitizedHtml);

    const value = html;
    const didValueChange = value !== state.lastValue || html !== state.lastHtml;
    state.lastValue = value;
    state.lastHtml = html;
    updateEmptyState(surface);
    updateLength(element, value);
    updateSourceValue(element, value);
    updateToolbarState(element);

    state.hasUncommittedEdit ||= didValueChange;
    if (notifyDotNet && didValueChange) {
        sendValue(state, 'UpdateValueFromJs', value, html);
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
            surface.innerHTML = flattenEditorHtml(source.value);
        }
    }

    clearPendingSync(state);
    const sanitizedHtml = sanitizeEditorHtml(surface.innerHTML);
    if (sanitizedHtml !== surface.innerHTML) {
        surface.innerHTML = sanitizedHtml;
    }

    const html = flattenEditorHtml(sanitizedHtml);
    if (state.sourceMode) {
        const source = getHtmlSource(element);
        if (source) {
            source.value = formatHtmlSource(html);
            updateHtmlSourceView(source);
        }
    }

    const value = html;
    state.lastValue = value;
    state.lastHtml = html;
    state.hasUncommittedEdit = false;
    updateEmptyState(surface);
    updateLength(element, value);
    updateSourceValue(element, value);
    updateToolbarState(element);

    sendValue(state, 'CommitValueFromJs', value, html);
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

    surface.focus();

    const selection = window.getSelection?.();
    if (!selection) {
        return;
    }

    selection.removeAllRanges();
    selection.addRange(selectionRange);
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

    surface.focus();
    let didChange = true;
    const tool = state ? getRichTextEditorTool(command) : null;
    if (tool && state) {
        const toolContext = createRichTextEditorToolContext(element, state, toolHost, tool);
        didChange = tool.execute?.(toolContext, value) ?? false;
        if (!didChange) {
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
        case 'heading':
            document.execCommand('formatBlock', false, `H${value ?? '1'}`);
            break;
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
            document.execCommand('insertUnorderedList', false);
            break;
        case 'orderedList':
            document.execCommand('insertOrderedList', false);
            break;
        case 'blockquote':
            if (hasAncestorTag(surface, 'blockquote')) {
                document.execCommand('outdent', false);
            } else {
                document.execCommand('formatBlock', false, 'BLOCKQUOTE');
            }
            break;
        case 'codeBlock':
            if (hasAncestorTag(surface, 'pre')) {
                document.execCommand('formatBlock', false, 'P');
            } else {
                document.execCommand('formatBlock', false, 'PRE');
            }
            break;
        default:
            document.execCommand(command, false);
            break;
    }

    if (!didChange) {
        updateToolbarState(element);
        return false;
    }

    syncValueFromSurface(element, true);
    updateToolbarState(element);
    return true;
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

    return hasPrimaryModifier === requiresPrimaryModifier
        && event.altKey === requiresAlt
        && event.shiftKey === requiresShift
        && event.key.toLowerCase() === keyPart;
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

    if (event.key === 'Tab' && hasAncestorTag(surface, 'li')) {
        event.preventDefault();
        document.execCommand(event.shiftKey ? 'outdent' : 'indent', false);
        const shouldNotify = element.dataset.bindOnInput === 'true';
        syncValueFromSurface(element, shouldNotify);
        updateToolbarState(element);
    }
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
            const html = flattenEditorHtml(source.value);
            surface.innerHTML = html;
            updateEmptyState(surface);
            updateLength(element, html);
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
                    sendValue(state, 'UpdateValueFromJs', html, html);
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
        onKeyDown: (event: KeyboardEvent) => {
            handleEditorKeyDown(element, event);
        },
        onToolbarMouseDown: (event: MouseEvent) => {
            event.preventDefault();
        }
    };

    surface.addEventListener('input', state.onInput);
    surface.addEventListener('focus', state.onFocus);
    surface.addEventListener('keyup', state.onKeyUp);
    surface.addEventListener('mouseup', state.onMouseUp);
    surface.addEventListener('keydown', state.onKeyDown);
    getHtmlSource(element)?.addEventListener('input', state.onSourceInput);
    getHtmlSource(element)?.addEventListener('scroll', state.onSourceScroll);
    window.addEventListener('resize', state.onWindowResize);
    element.addEventListener('focusin', state.onFocusIn);
    element.addEventListener('focusout', state.onFocusOut);

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
        applySourceToSurface(element, value);
        if (state.sourceMode) {
            const source = getHtmlSource(element);
            if (source) {
                source.value = formatHtmlSource(value);
                updateHtmlSourceView(source);
            }
        }
        state.requiresInitialRender = false;
    } else {
        updateLength(element, state.lastValue);
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

    const sanitizedHtml = surface ? sanitizeEditorHtml(surface.innerHTML) : '';
    if (surface && sanitizedHtml !== surface.innerHTML) {
        surface.innerHTML = sanitizedHtml;
    }

    const currentHtml = flattenEditorHtml(sanitizedHtml);

    if (currentHtml !== state.lastHtml) {
        state.lastHtml = currentHtml;
        invokeDotNetVoid(state.dotNetRef, 'UpdateMarkupValueFromJs', currentHtml);
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
    getLengthElement,
    getToolbarButtons,
    isEditorEditable,
    setActiveEditor,
    clearActiveEditor,
    invokeDotNetVoid,
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
    normalizeTableCells,
    getTableBorderColor,
    normalizeAlignment,
    getElementAlignment,
    getTableRows,
    surfaceToValue,
    updateSourceValue,
    updateEmptyState,
    updateLength,
    applyHtmlToSurface,
    applySourceToSurface,
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
