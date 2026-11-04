type Maybe<T> = T | null | undefined;
type Active = 'start' | 'end';
type FinishMode = 'done' | 'escape' | 'dismiss';

interface Selection {
    start: string | null;
    end: string | null;
    active: Active;
}

interface PickResult extends Selection {
    close: boolean;
}

interface FieldInstance {
    abort: AbortController;
    active: Active;
    built: string;
    endInput: HTMLInputElement;
    focus: string;
    grid: HTMLElement;
    hover: string | null;
    input: HTMLInputElement;
    open: boolean;
    popover: HTMLElement;
    root: HTMLElement;
    startInput: HTMLInputElement;
    view: string;
}

const ROOT_SELECTOR = '[data-nt-input-date-range]';
const DAY_MS = 86400000;
const instances = new Map<HTMLElement, FieldInstance>();

// Dates are 'yyyy-MM-dd' strings, so string comparison is date comparison and no time zone is involved.
const toIso = (time: number): string => new Date(time).toISOString().slice(0, 10);
const addDays = (date: string, days: number): string => toIso(Date.parse(date) + days * DAY_MS);
const dayOfWeek = (date: string): number => new Date(Date.parse(date)).getUTCDay();
const monthStart = (date: string): string => `${date.slice(0, 8)}01`;

function addMonths(date: string, months: number): string {
    const year = Number(date.slice(0, 4));
    const month = Number(date.slice(5, 7)) - 1 + months;
    const lastDay = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
    return toIso(Date.UTC(year, month, Math.min(Number(date.slice(8, 10)), lastDay)));
}

function today(): string {
    const now = new Date();
    return toIso(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()));
}

export function initialActive(start: Maybe<string>, end: Maybe<string>): Active {
    return !start ? 'start' : !end ? 'end' : 'start';
}

export function pickDate(selection: Selection, date: string): PickResult {
    const { start, end, active } = selection;
    if (active === 'start') {
        return { start: date, end: end && end < date ? null : end, active: 'end', close: false };
    }

    if (!start) {
        return { start: date, end, active: 'end', close: false };
    }

    if (date < start) {
        return { start: date, end: null, active: 'end', close: false };
    }

    return { start, end: date, active, close: true };
}

export function moveFocus(date: string, key: string, firstDay: number): string | null {
    switch (key) {
        case 'ArrowLeft': return addDays(date, -1);
        case 'ArrowRight': return addDays(date, 1);
        case 'ArrowUp': return addDays(date, -7);
        case 'ArrowDown': return addDays(date, 7);
        case 'PageUp': return addMonths(date, -1);
        case 'PageDown': return addMonths(date, 1);
        case 'Home': return addDays(date, -((dayOfWeek(date) - firstDay + 7) % 7));
        case 'End': return addDays(date, 6 - ((dayOfWeek(date) - firstDay + 7) % 7));
        default: return null;
    }
}

function format(date: string, options: Intl.DateTimeFormatOptions): string {
    return new Intl.DateTimeFormat(undefined, { ...options, timeZone: 'UTC' }).format(Date.parse(date));
}

function formatShort(date: string): string {
    return format(date, { month: 'short', day: 'numeric', year: 'numeric' });
}

function resolveRoot(element: Maybe<Element>): HTMLElement | null {
    return element?.closest<HTMLElement>(ROOT_SELECTOR) ?? null;
}

function selection(instance: FieldInstance): Selection {
    return { start: instance.startInput.value || null, end: instance.endInput.value || null, active: instance.active };
}

function firstDayOfWeek(instance: FieldInstance): number {
    return Number(instance.root.dataset.firstDay) || 0;
}

function isDisabledDate(instance: FieldInstance, date: string): boolean {
    const { min, max } = instance.root.dataset;
    return (!!min && date < min) || (!!max && date > max);
}

function clamp(instance: FieldInstance, date: string): string {
    const { min, max } = instance.root.dataset;
    return min && date < min ? min : max && date > max ? max : date;
}

function build(instance: FieldInstance): void {
    const firstDay = firstDayOfWeek(instance);
    const weekdays = document.createElement('div');
    weekdays.className = 'week';
    weekdays.setAttribute('role', 'row');
    for (let i = 0; i < 7; i++) {
        const date = addDays('2023-01-01', firstDay + i);
        const weekday = document.createElement('span');
        weekday.className = 'weekday';
        weekday.setAttribute('role', 'columnheader');
        weekday.setAttribute('aria-label', format(date, { weekday: 'long' }));
        weekday.textContent = format(date, { weekday: 'narrow' });
        weekdays.append(weekday);
    }

    const rows: HTMLElement[] = [weekdays];
    let date = addDays(instance.view, -((dayOfWeek(instance.view) - firstDay + 7) % 7));
    for (let week = 0; week < 6; week++) {
        const row = document.createElement('div');
        row.className = 'week';
        row.setAttribute('role', 'row');
        for (let i = 0; i < 7; i++) {
            const cell = document.createElement('div');
            cell.className = 'day-cell';
            cell.setAttribute('role', 'gridcell');
            cell.dataset.date = date;
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'day';
            button.dataset.date = date;
            button.textContent = String(Number(date.slice(8, 10)));
            button.setAttribute('aria-label', format(date, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }));
            cell.append(button);
            row.append(cell);
            date = addDays(date, 1);
        }

        rows.push(row);
    }

    instance.grid.replaceChildren(...rows);
    instance.built = instance.view;
    const label = instance.popover.querySelector('[data-nt-input-date-range-month]');
    if (label) {
        label.textContent = format(instance.view, { month: 'long', year: 'numeric' });
    }
}

function paint(instance: FieldInstance): void {
    const { start, end, active } = selection(instance);
    const previewEnd = active === 'end' && start && instance.hover && instance.hover >= start ? instance.hover : end;
    const previewing = previewEnd !== end;
    const banded = !!start && !!previewEnd && previewEnd > start;
    const current = today();

    for (const cell of instance.grid.querySelectorAll<HTMLElement>('.day-cell')) {
        const date = cell.dataset.date!;
        const button = cell.firstElementChild as HTMLButtonElement;
        const selected = date === start || date === end;
        const inRange = banded && date > start! && date < previewEnd!;
        cell.classList.toggle('in-range', inRange);
        cell.classList.toggle('band-start', banded && date === start);
        cell.classList.toggle('band-end', banded && date === previewEnd);
        cell.classList.toggle('preview', previewing && (inRange || (banded && date === previewEnd)));
        cell.setAttribute('aria-selected', String(selected || inRange));
        button.classList.toggle('selected', selected);
        button.classList.toggle('today', date === current);
        button.classList.toggle('outside', date.slice(0, 7) !== instance.view.slice(0, 7));
        button.disabled = isDisabledDate(instance, date);
        button.tabIndex = date === instance.focus ? 0 : -1;
    }

    for (const side of ['start', 'end'] as const) {
        const value = side === 'start' ? start : end;
        const segment = instance.popover.querySelector<HTMLElement>(`[data-nt-input-date-range-segment="${side}"]`);
        segment?.setAttribute('aria-pressed', String(active === side));
        segment?.classList.toggle('active', active === side);
        const text = instance.popover.querySelector(`[data-nt-input-date-range-segment-value="${side}"]`);
        if (text) {
            text.textContent = value ? formatShort(value) : '–';
        }
    }
}

function sync(instance: FieldInstance): void {
    if (instance.built !== instance.view) {
        build(instance);
    }

    paint(instance);
}

function focusDay(instance: FieldInstance): void {
    instance.grid.querySelector<HTMLElement>(`button[data-date="${instance.focus}"]`)?.focus({ preventScroll: true });
}

function setFocusDate(instance: FieldInstance, date: string, focusButton: boolean): void {
    instance.focus = clamp(instance, date);
    instance.view = monthStart(instance.focus);
    sync(instance);
    if (focusButton) {
        focusDay(instance);
    }
}

function writeDisplay(instance: FieldInstance): void {
    const { start, end } = selection(instance);
    instance.input.value = start || end ? `${start ? formatShort(start) : ''} – ${end ? formatShort(end) : ''}`.trim() : '';
}

function commit(instance: FieldInstance, start: string | null, end: string | null): void {
    const pairs: [HTMLInputElement, string][] = [[instance.startInput, start ?? ''], [instance.endInput, end ?? '']];
    const updates = pairs.filter(([input, value]) => input.value !== value);
    for (const [input, value] of updates) {
        input.value = value;
    }

    writeDisplay(instance);
    for (const [input] of updates) {
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

function finish(instance: FieldInstance, mode: FinishMode): void {
    if (!instance.open) {
        return;
    }

    instance.open = false;
    instance.hover = null;
    try {
        if (typeof instance.popover.hidePopover === 'function' && instance.popover.matches(':popover-open')) {
            instance.popover.hidePopover();
        }
    }
    catch {
        // The popover was already hidden by light dismiss.
    }

    delete instance.root.dataset.open;
    instance.input.setAttribute('aria-expanded', 'false');
    if (mode !== 'dismiss') {
        instance.input.focus({ preventScroll: true });
    }
}

function open(instance: FieldInstance): void {
    if (instance.open || instance.input.disabled || instance.root.dataset.readonly === 'true') {
        return;
    }

    const { start, end } = selection(instance);
    instance.active = initialActive(start, end);
    instance.open = true;
    instance.hover = null;
    instance.root.dataset.open = 'true';
    instance.input.setAttribute('aria-expanded', 'true');
    instance.focus = clamp(instance, (instance.active === 'start' ? start : end ?? start) ?? today());
    instance.view = monthStart(instance.focus);

    try {
        instance.popover.showPopover?.();
    }
    catch {
        // Already showing or not connected; the open state above still drives the field.
    }

    sync(instance);
    focusDay(instance);
}

function pick(instance: FieldInstance, date: string): void {
    if (isDisabledDate(instance, date)) {
        return;
    }

    const result = pickDate(selection(instance), date);
    instance.active = result.active;
    instance.hover = null;
    commit(instance, result.start, result.end);
    if (result.close) {
        finish(instance, 'done');
    }
    else {
        setFocusDate(instance, date, true);
    }
}

function onAction(instance: FieldInstance, action: string): void {
    if (action === 'prev' || action === 'next') {
        setFocusDate(instance, addMonths(instance.focus, action === 'prev' ? -1 : 1), false);
    }
    else if (action === 'clear') {
        instance.active = 'start';
        commit(instance, null, null);
        sync(instance);
    }
    else if (action === 'done') {
        finish(instance, 'done');
    }
}

function onSegment(instance: FieldInstance, side: Active): void {
    instance.active = side;
    const { start, end } = selection(instance);
    setFocusDate(instance, (side === 'start' ? start : end) ?? instance.focus, false);
}

function onGridKeydown(instance: FieldInstance, event: KeyboardEvent): void {
    const date = (event.target as HTMLElement).closest<HTMLElement>('button[data-date]')?.dataset.date;
    if (!date) {
        return;
    }

    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        pick(instance, date);
        return;
    }

    const next = moveFocus(date, event.key, firstDayOfWeek(instance));
    if (next) {
        event.preventDefault();
        setFocusDate(instance, next, true);
    }
}

function setHover(instance: FieldInstance, target: EventTarget | null): void {
    const date = (target as Element | null)?.closest<HTMLElement>('button[data-date]')?.dataset.date ?? null;
    if (instance.active === 'end' && instance.hover !== date) {
        instance.hover = date;
        paint(instance);
    }
}

function createInstance(root: HTMLElement): FieldInstance | null {
    const input = root.querySelector<HTMLInputElement>('input[data-nt-input-date-range-input]');
    const startInput = root.querySelector<HTMLInputElement>('input[data-nt-input-date-range-start]');
    const endInput = root.querySelector<HTMLInputElement>('input[data-nt-input-date-range-end]');
    const popover = root.querySelector<HTMLElement>('[data-nt-input-date-range-popover]');
    const grid = root.querySelector<HTMLElement>('[data-nt-input-date-range-grid]');
    if (!input || !startInput || !endInput || !popover || !grid) {
        return null;
    }

    const instance: FieldInstance = {
        abort: new AbortController(),
        active: 'start',
        built: '',
        endInput,
        focus: today(),
        grid,
        hover: null,
        input,
        open: false,
        popover,
        root,
        startInput,
        view: monthStart(today()),
    };

    const options = { signal: instance.abort.signal };
    // Open on click, not focus: focus fires on pointerdown, and the popover's light dismiss would close it again on
    // pointerup because the field sits outside the popover.
    input.addEventListener('click', () => open(instance), options);
    input.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === ' ' || event.key === 'ArrowDown') {
            event.preventDefault();
            open(instance);
        }
    }, options);
    popover.addEventListener('toggle', event => {
        if ((event as Event & { newState?: string }).newState === 'closed') {
            finish(instance, 'dismiss');
        }
    }, options);
    popover.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            finish(instance, 'escape');
        }
    }, options);
    popover.addEventListener('click', event => {
        const target = event.target as Element;
        const action = target.closest<HTMLElement>('[data-nt-input-date-range-action]')?.dataset.ntInputDateRangeAction;
        const segment = target.closest<HTMLElement>('[data-nt-input-date-range-segment]')?.dataset.ntInputDateRangeSegment;
        const day = target.closest<HTMLElement>('button[data-date]');
        if (action) {
            onAction(instance, action);
        }
        else if (segment === 'start' || segment === 'end') {
            onSegment(instance, segment);
        }
        else if (day?.dataset.date) {
            pick(instance, day.dataset.date);
        }
    }, options);
    grid.addEventListener('keydown', event => onGridKeydown(instance, event), options);
    grid.addEventListener('mouseover', event => setHover(instance, event.target), options);
    grid.addEventListener('focusin', event => setHover(instance, event.target), options);
    grid.addEventListener('mouseleave', () => setHover(instance, null), options);
    return instance;
}

function disposeInstance(root: HTMLElement, instance: FieldInstance): void {
    instance.abort.abort();
    instances.delete(root);
}

function cleanupDisconnected(): void {
    for (const [root, instance] of instances) {
        if (!root.isConnected) {
            disposeInstance(root, instance);
        }
    }
}

export function onLoad(element: Maybe<Element>, _dotNetRef?: unknown): void {
    cleanupDisconnected();
    const root = resolveRoot(element);
    if (root && !instances.has(root)) {
        const instance = createInstance(root);
        if (instance) {
            instances.set(root, instance);
        }
    }
}

export function onUpdate(element: Maybe<Element>, _dotNetRef?: unknown): void {
    onLoad(element);
}

export function onDispose(element: Maybe<Element>, _dotNetRef?: unknown): void {
    const root = resolveRoot(element);
    const instance = root ? instances.get(root) : undefined;
    if (root && instance) {
        disposeInstance(root, instance);
    }

    cleanupDisconnected();
}
