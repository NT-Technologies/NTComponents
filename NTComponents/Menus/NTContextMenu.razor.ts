type Maybe<T> = T | null | undefined;

interface PointMenuElement extends HTMLElement {
    openAt?: (clientX: number, clientY: number, sourceElement?: Element | null) => void;
}

let activeMenu: PointMenuElement | null = null;

declare global {
    interface HTMLElementTagNameMap {
        'nt-context-menu': NTContextMenu;
    }
}

function isContextMenuElement(value: unknown): value is NTContextMenu {
    return value instanceof NTContextMenu;
}

export class NTContextMenu extends HTMLElement {
    private longPressStartX = 0;
    private longPressStartY = 0;
    private longPressTimer: number | null = null;
    private menu: PointMenuElement | null = null;
    private suppressNativeUntil = 0;
    private target: HTMLElement | null = null;

    public static readonly observedAttributes = ['data-target-id'];

    private readonly onDocumentClick = (event: MouseEvent): void => {
        if (event.target instanceof Node && !this.menu?.contains(event.target)) {
            this.menu?.hidePopover();
        }
    };

    private readonly onDocumentKeyDown = (event: KeyboardEvent): void => {
        if (event.key === 'Escape' && this.menu?.matches(':popover-open')) {
            event.preventDefault();
            this.menu.hidePopover();
        }
    };

    private readonly onMenuToggle = (): void => {
        if (!this.menu?.matches(':popover-open')) {
            this.removeDismissListeners();
        }
    };

    private readonly onClick = (event: MouseEvent): void => {
        if (this.shouldSuppressNativeEvent()) {
            event.preventDefault();
            event.stopPropagation();
        }
    };

    private readonly onContextMenu = (event: MouseEvent): void => {
        this.handleContextMenu(event);
    };

    private readonly onKeyDown = (event: KeyboardEvent): void => {
        this.handleKeyDown(event);
    };

    private readonly onPointerCancel = (): void => {
        this.cancelLongPress();
    };

    private readonly onPointerDown = (event: PointerEvent): void => {
        this.handlePointerDown(event);
    };

    private readonly onPointerMove = (event: PointerEvent): void => {
        this.handlePointerMove(event);
    };

    private readonly onPointerUp = (): void => {
        this.cancelLongPress();
    };

    private readonly onScroll = (): void => {
        this.cancelLongPress();
    };

    public connectedCallback(): void {
        this.update();
    }

    public attributeChangedCallback(): void {
        if (this.isConnected) {
            this.update();
        }
    }

    public disconnectedCallback(): void {
        this.dispose();
    }

    public dispose(): void {
        this.cancelLongPress();
        this.removeTargetListeners();
        this.removeTransientListeners();
        this.removeDismissListeners();
        this.menu?.removeEventListener('toggle', this.onMenuToggle);
        this.menu = null;
    }

    public update(): void {
        const targetId = this.dataset.targetId?.trim();
        const target = targetId
            ? document.getElementById(targetId)
            : this.querySelector<HTMLElement>(':scope > .nt-context-menu-target');
        const menu = this.getMenu();

        if (target !== this.target) {
            this.removeTargetListeners();
            this.target = target;
            this.addTargetListeners();
        }

        if (menu !== this.menu) {
            this.removeDismissListeners();
            this.menu?.removeEventListener('toggle', this.onMenuToggle);
            this.menu = menu;
            this.menu?.addEventListener('toggle', this.onMenuToggle);
        }
    }

    private addTargetListeners(): void {
        this.target?.addEventListener('click', this.onClick);
        this.target?.addEventListener('contextmenu', this.onContextMenu);
        this.target?.addEventListener('keydown', this.onKeyDown);
        this.target?.addEventListener('pointerdown', this.onPointerDown);
    }

    private cancelLongPress(): void {
        if (this.longPressTimer !== null) {
            window.clearTimeout(this.longPressTimer);
            this.longPressTimer = null;
        }

        this.removeTransientListeners();
    }

    private getLongPressDelay(): number {
        const delay = Number.parseInt(this.dataset.longPressDelay ?? '', 10);
        return Number.isFinite(delay) && delay >= 0 ? delay : 500;
    }

    private getMenu(): PointMenuElement | null {
        const menuId = this.dataset.menuId?.trim();
        if (menuId) {
            const menu = document.getElementById(menuId);
            if (menu instanceof HTMLElement) {
                return menu as PointMenuElement;
            }
        }

        return this.querySelector<PointMenuElement>(':scope > nt-menu');
    }

    // With data-target-selector, only gestures on (or, for the keyboard, with the selection inside) a matching element within the target open the menu.
    private getSource(node: Maybe<EventTarget>): Element | null {
        const source = node instanceof Element ? node : this.target;
        const selector = this.dataset.targetSelector?.trim();
        if (!selector) {
            return source;
        }

        const match = source?.closest(selector) ?? null;
        return match && this.target?.contains(match) ? match : null;
    }

    private getKeyboardSource(event: KeyboardEvent): Element | null {
        if (!this.dataset.targetSelector?.trim()) {
            return this.getSource(event.target);
        }

        const anchor = window.getSelection?.()?.anchorNode;
        return this.getSource(anchor instanceof Element ? anchor : anchor?.parentElement);
    }

    private handleContextMenu(event: MouseEvent): void {
        if (this.shouldSuppressNativeEvent()) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        if (this.isDisabled()) {
            return;
        }

        const source = this.getSource(event.target);
        if (!source) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.openAt(event.clientX, event.clientY, source);
    }

    private handleKeyDown(event: KeyboardEvent): void {
        if (this.isDisabled()) {
            return;
        }

        const opensContextMenu = event.key === 'ContextMenu' || (event.shiftKey && event.key === 'F10');
        if (!opensContextMenu) {
            return;
        }

        const source = this.getKeyboardSource(event);
        const rect = (source instanceof HTMLElement ? source : this.target)?.getBoundingClientRect();
        if (!source || !rect) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        this.openAt(rect.left, rect.bottom, source);
    }

    private handlePointerDown(event: PointerEvent): void {
        if (this.isDisabled() || !event.isPrimary || (event.pointerType !== 'touch' && event.pointerType !== 'pen')) {
            return;
        }

        const source = this.getSource(event.target);
        if (!source) {
            return;
        }

        this.cancelLongPress();
        event.stopPropagation();
        this.longPressStartX = event.clientX;
        this.longPressStartY = event.clientY;
        this.longPressTimer = window.setTimeout(() => {
            this.longPressTimer = null;
            this.suppressFollowUpNativeEvents();
            this.openAt(this.longPressStartX, this.longPressStartY, source);
            this.removeTransientListeners();
        }, this.getLongPressDelay());

        document.addEventListener('pointermove', this.onPointerMove);
        document.addEventListener('pointerup', this.onPointerUp);
        document.addEventListener('pointercancel', this.onPointerCancel);
        window.addEventListener('scroll', this.onScroll, true);
    }

    private handlePointerMove(event: PointerEvent): void {
        const movement = Math.hypot(event.clientX - this.longPressStartX, event.clientY - this.longPressStartY);
        if (movement > 8) {
            this.cancelLongPress();
        }
    }

    private isDisabled(): boolean {
        return this.dataset.disabled === 'true' || this.hasAttribute('disabled') || this.classList.contains('nt-disabled');
    }

    private openAt(clientX: number, clientY: number, sourceElement: Maybe<Element>): void {
        const menu = this.menu ?? this.getMenu();
        if (activeMenu !== menu) {
            activeMenu?.hidePopover();
        }
        activeMenu = menu;
        menu?.openAt?.(clientX, clientY, sourceElement ?? this.target);
        // A contextmenu can fire before pointerup, which light-dismisses auto popovers.
        document.addEventListener('click', this.onDocumentClick);
        document.addEventListener('keydown', this.onDocumentKeyDown);
    }

    private removeDismissListeners(): void {
        if (activeMenu === this.menu) {
            activeMenu = null;
        }
        document.removeEventListener('click', this.onDocumentClick);
        document.removeEventListener('keydown', this.onDocumentKeyDown);
    }

    private removeTargetListeners(): void {
        this.target?.removeEventListener('click', this.onClick);
        this.target?.removeEventListener('contextmenu', this.onContextMenu);
        this.target?.removeEventListener('keydown', this.onKeyDown);
        this.target?.removeEventListener('pointerdown', this.onPointerDown);
        this.target = null;
    }

    private removeTransientListeners(): void {
        document.removeEventListener('pointermove', this.onPointerMove);
        document.removeEventListener('pointerup', this.onPointerUp);
        document.removeEventListener('pointercancel', this.onPointerCancel);
        window.removeEventListener('scroll', this.onScroll, true);
    }

    private shouldSuppressNativeEvent(): boolean {
        return Date.now() < this.suppressNativeUntil;
    }

    private suppressFollowUpNativeEvents(): void {
        this.suppressNativeUntil = Date.now() + 750;
    }
}

export function onLoad(element: Maybe<HTMLElement>): void {
    if (!customElements.get('nt-context-menu')) {
        customElements.define('nt-context-menu', NTContextMenu);
    }

    if (isContextMenuElement(element)) {
        element.update();
    }
}

export function onUpdate(element: Maybe<HTMLElement>): void {
    if (isContextMenuElement(element)) {
        element.update();
    }
}

export function onDispose(element: Maybe<HTMLElement>): void {
    if (isContextMenuElement(element)) {
        element.dispose();
    }
}
