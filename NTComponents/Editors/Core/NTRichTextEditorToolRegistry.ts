export type Maybe<T> = T | null | undefined;

export interface InlineToolCloseOptions {
    focusSurface?: boolean;
    preserveSelection?: boolean;
}

export interface RichTextEditorToolEditorState {
    selectionRange: Range | null;
    toolStates: Map<string, unknown>;
}

export interface RichTextEditorToolContext<TState> {
    element: HTMLElement;
    editorState: RichTextEditorToolEditorState;
    toolState: TState;
    host: RichTextEditorToolHost;
}

export interface RichTextEditorToolHost {
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

export interface RichTextEditorToolPlugin<TState> {
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
