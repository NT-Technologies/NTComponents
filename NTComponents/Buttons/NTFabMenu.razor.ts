type Maybe<T> = T | null | undefined;

interface DotNetFabMenuRef {
    invokeMethodAsync(methodName: 'NotifyFabMenuExpandedChanged', expanded: boolean): Promise<unknown> | void;
}

interface NTComponentsGlobals {
    registerButtonInteraction?: (element: Maybe<Element>) => void;
}

declare global {
    interface Window {
        NTComponents?: NTComponentsGlobals;
    }

    interface HTMLElementTagNameMap {
        'nt-fab-menu': NTFabMenu;
    }
}

function isDotNetObjectReference(value: unknown): value is DotNetFabMenuRef {
    return typeof (value as DotNetFabMenuRef | null)?.invokeMethodAsync === 'function';
}

function isFabMenuElement(value: unknown): value is NTFabMenu {
    return value instanceof NTFabMenu;
}

function updateAllFabMenus(): void {
    document.querySelectorAll<NTFabMenu>('nt-fab-menu').forEach((element) => {
        if (isFabMenuElement(element)) {
            element.update();
        }
    });
}

export class NTFabMenu extends HTMLElement {
    private button: HTMLButtonElement | null = null;
    private dotNetRef: DotNetFabMenuRef | null = null;
    private expanded: boolean | null = null;
    private interactionsRegistered = false;
    private items: Element[] = [];
    private panel: HTMLDivElement | null = null;

    private readonly onPanelClick = (event: MouseEvent): void => {
        this.handlePanelClick(event);
    };

    private readonly onToggle = (): void => {
        this.updateOpenState(this.panel?.matches(':popover-open') ?? false);
    };

    public connectedCallback(): void {
        this.update();
    }

    public disconnectedCallback(): void {
        this.removeElementListeners();
    }

    public removeElementListeners(): void {
        this.panel?.removeEventListener('toggle', this.onToggle);
        this.panel?.removeEventListener('click', this.onPanelClick);
        this.button = null;
        this.interactionsRegistered = false;
        this.items = [];
        this.panel = null;
    }

    // Goes through the native popover so the toggle event reports the change like any user action.
    public setOpen(open: boolean): void {
        const panel = this.panel;
        if (!panel || panel.matches(':popover-open') === open) {
            return;
        }

        if (open) {
            panel.showPopover?.();
        } else {
            panel.hidePopover?.();
        }
    }

    public update(dotNetRef: Maybe<DotNetFabMenuRef> = null): void {
        this.setDotNetRef(dotNetRef);
        this.updateElements();

        // A Blazor re-render rewrites the class attribute, so restore the open-state decorations from the browser.
        if (this.panel) {
            this.renderOpenState(this.panel.matches(':popover-open'));
        }
    }

    private handlePanelClick(event: MouseEvent): void {
        const panel = this.panel;
        if (!panel || panel.dataset.closeOnItemClick !== 'true') {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        const item = target?.closest('.nt-fab-menu-item');
        if (!item || item.classList.contains('nt-fab-menu-item-disabled')) {
            return;
        }

        if (panel.matches(':popover-open')) {
            panel.hidePopover?.();
        }
    }

    private registerButtonInteractions(): void {
        const registerButtonInteraction = window.NTComponents?.registerButtonInteraction;
        if (!registerButtonInteraction) {
            this.interactionsRegistered = false;
            return;
        }

        if (this.button) {
            registerButtonInteraction(this.button);
        }

        for (const item of this.items) {
            registerButtonInteraction(item);
        }

        this.interactionsRegistered = true;
    }

    private setDotNetRef(dotNetRef: Maybe<unknown>): void {
        if (isDotNetObjectReference(dotNetRef)) {
            this.dotNetRef = dotNetRef;
        }
    }

    private updateElements(): void {
        const panel = this.querySelector<HTMLDivElement>(':scope .nt-fab-menu-panel');
        const button = this.querySelector<HTMLButtonElement>(':scope .nt-fab-menu-button');
        const items = Array.from(this.querySelectorAll<Element>(':scope .nt-fab-menu-item'));
        const panelChanged = panel !== this.panel;
        const buttonChanged = button !== this.button;
        const itemsChanged = this.items.length !== items.length || !this.items.every((item, index) => item === items[index]);

        if (!panelChanged && !buttonChanged && !itemsChanged) {
            if (!this.interactionsRegistered) {
                this.registerButtonInteractions();
            }
            return;
        }

        if (panelChanged) {
            this.panel?.removeEventListener('toggle', this.onToggle);
            this.panel?.removeEventListener('click', this.onPanelClick);
            this.panel = panel;
            this.panel?.addEventListener('toggle', this.onToggle);
            this.panel?.addEventListener('click', this.onPanelClick);
        }

        this.button = button;
        this.items = items;

        if (buttonChanged || itemsChanged || !this.interactionsRegistered) {
            this.interactionsRegistered = false;
            this.registerButtonInteractions();
        }
    }

    // Writes only the DOM decorations. The tracked state changes only on toggle events, so each change reaches .NET exactly once.
    private renderOpenState(isOpen: boolean): void {
        this.classList.toggle('nt-fab-menu-expanded', isOpen);
        this.button?.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    }

    private updateOpenState(isOpen: boolean): void {
        const panel = this.panel;
        const button = this.button;
        if (!panel || !button) {
            return;
        }

        const wasOpen = this.expanded === true;
        const stateChanged = this.expanded !== isOpen;
        const focusWasInsidePanel = stateChanged && !isOpen && wasOpen && document.activeElement instanceof Node && panel.contains(document.activeElement);

        this.renderOpenState(isOpen);

        if (!stateChanged) {
            return;
        }

        this.expanded = isOpen;

        if (isOpen || focusWasInsidePanel) {
            button.focus({ preventScroll: true });
        }

        if (isDotNetObjectReference(this.dotNetRef)) {
            Promise.resolve(this.dotNetRef.invokeMethodAsync('NotifyFabMenuExpandedChanged', isOpen)).catch(() => {
                // Ignore late/disconnected interop failures from native popover notifications.
            });
        }
    }
}

export function onLoad(element: Maybe<HTMLElement> = null, dotNetRef: Maybe<DotNetFabMenuRef> = null): void {
    if (!customElements.get('nt-fab-menu')) {
        customElements.define('nt-fab-menu', NTFabMenu);
    }

    if (isFabMenuElement(element)) {
        element.update(dotNetRef);
        return;
    }

    updateAllFabMenus();
}

export function onUpdate(element: Maybe<HTMLElement> = null, dotNetRef: Maybe<DotNetFabMenuRef> = null): void {
    if (isFabMenuElement(element)) {
        element.update(dotNetRef);
        return;
    }

    updateAllFabMenus();
}

export function setOpen(element: Maybe<HTMLElement>, open: boolean): void {
    if (isFabMenuElement(element)) {
        element.setOpen(open);
    }
}

export function onDispose(element: Maybe<HTMLElement> = null): void {
    if (isFabMenuElement(element)) {
        element.removeElementListeners();
        return;
    }

    document.querySelectorAll<NTFabMenu>('nt-fab-menu').forEach((fabMenu) => {
        if (isFabMenuElement(fabMenu)) {
            fabMenu.removeElementListeners();
        }
    });
}
