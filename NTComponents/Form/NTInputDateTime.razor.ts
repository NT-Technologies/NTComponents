type DateTimePickerMode = 'date' | 'datetime' | 'month' | 'none' | 'time';
type DatePickerView = 'calendar' | 'month' | 'year';
type Meridiem = 'am' | 'pm';
type Maybe<T> = T | null | undefined;
type Normalizer = (token: string) => string | null;

interface PickerDraft {
    day: number;
    hour: number;
    minute: number;
    month: number;
    second: number;
    year: number;
}

interface PickerElements {
    amButton: HTMLButtonElement | null;
    calendarYearLabel: HTMLElement | null;
    cancelButton: HTMLButtonElement | null;
    confirmButton: HTMLButtonElement | null;
    dayButtons: HTMLButtonElement[];
    headline: HTMLElement | null;
    hourInput: HTMLInputElement | null;
    minuteInput: HTMLInputElement | null;
    monthButtons: HTMLButtonElement[];
    monthLabel: HTMLElement | null;
    pmButton: HTMLButtonElement | null;
    subhead: HTMLElement | null;
    surface: HTMLElement | null;
    yearLabel: HTMLElement | null;
    yearList: HTMLElement | null;
}

interface PickerState {
    didMakePickerUntabbable: boolean;
    disabledDateSet: Set<string>;
    disabledTimeSet: Set<string>;
    dateView: DatePickerView;
    draft: PickerDraft;
    elements: PickerElements;
    input: HTMLInputElement;
    isModal: boolean;
    isOpen: boolean;
    label: HTMLLabelElement | null;
    lastRenderSignature: string | null;
    maxDraft: PickerDraft | null;
    meridiem: Meridiem;
    minDraft: PickerDraft | null;
    mode: DateTimePickerMode;
    nativeInputType: string;
    onInputCommit: () => void;
    onInputFocus: () => void;
    onInputInput: () => void;
    onInputKeyDown: (event: KeyboardEvent) => void;
    onEnterKeyPress: (event: KeyboardEvent) => void;
    onEnterKeyUp: (event: KeyboardEvent) => void;
    onLabelClick: (event: MouseEvent) => void;
    onLabelMouseDown: (event: MouseEvent) => void;
    onPickerClick: (event: MouseEvent) => void;
    onPickerInput: (event: Event) => void;
    onPickerKeyDown: (event: KeyboardEvent) => void;
    onYearListScroll: (event: Event) => void;
    onTriggerClick: (event: MouseEvent) => void;
    onTriggerMouseDown: (event: MouseEvent) => void;
    openOnFocus: boolean;
    openValue: string;
    picker: HTMLElement;
    suppressEnterKeySequence: boolean;
    trigger: HTMLButtonElement | null;
    viewMonth: number;
    viewYear: number;
    yearListStart: number | null;
    yearListEnd: number | null;
    yearListMaxYear: number | null;
    yearListMinYear: number | null;
    yearListSelectedYear: number | null;
}

interface NativePickerState {
    input: HTMLInputElement;
    onTriggerClick: () => void;
    onTriggerMouseDown: (event: MouseEvent) => void;
    root: HTMLElement;
    trigger: HTMLButtonElement;
}

const pickerStateByInput = new Map<HTMLInputElement, PickerState>();
const nativePickerStateByInput = new Map<HTMLInputElement, NativePickerState>();
const DEFAULT_MODAL_VIEWPORT_WIDTH_BREAKPOINT = 750;
const CALENDAR_DAY_COUNT = 42;
const MIN_PICKER_YEAR = 1;
const MAX_PICKER_YEAR = 9999;
const MONTH_COUNT = 12;
const YEAR_LIST_BATCH_SIZE = 100;
const YEAR_LIST_MAX_OPTION_COUNT = 300;
const YEAR_LIST_SCROLL_THRESHOLD = 96;
const PICKER_INPUT_SELECTOR = 'input[data-nt-dtp-input="true"][data-nt-dtp-target]';
const NATIVE_PICKER_INPUT_SELECTOR = 'input[data-nt-dtp-native-input="true"]';
const WEEKDAY_LABELS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
const MODE_CONFIG: Record<DateTimePickerMode, { hasDate: boolean; hasTime: boolean; supportsSubSelection: boolean }> = {
    date: { hasDate: true, hasTime: false, supportsSubSelection: true },
    datetime: { hasDate: true, hasTime: true, supportsSubSelection: true },
    month: { hasDate: false, hasTime: false, supportsSubSelection: false },
    none: { hasDate: false, hasTime: false, supportsSubSelection: false },
    time: { hasDate: false, hasTime: true, supportsSubSelection: false },
};

let activePickerState: PickerState | null = null;
let pendingFrame: number | null = null;
let pointerDownInsideActivePicker = false;
let globalHandlersAttached = false;
const formatterByKey = new Map<string, Intl.DateTimeFormat>();
const lifecycleScopeByPageScript = new WeakMap<Element, Element>();

function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
}

function toInt(value: Maybe<string>, fallback = 0): number {
    if (value == null) {
        return fallback;
    }

    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function padTwo(value: number): string {
    return String(value).padStart(2, '0');
}

function padYear(value: number): string {
    const sign = value < 0 ? '-' : '';
    const absoluteYear = Math.abs(value);
    return `${sign}${String(absoluteYear).padStart(4, '0')}`;
}

const FORMATTER_OPTIONS = {
    dayHeadline: { weekday: 'short', month: 'short', day: 'numeric' },
    month: { month: 'long' },
    monthShort: { month: 'short' },
    monthYear: { month: 'long', year: 'numeric' },
    time: { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true },
    year: { year: 'numeric' },
} satisfies Record<string, Intl.DateTimeFormatOptions>;

function formatLocal(key: keyof typeof FORMATTER_OPTIONS, date: Date): string {
    let formatter = formatterByKey.get(key);
    if (!formatter) {
        formatter = new Intl.DateTimeFormat(undefined, FORMATTER_OPTIONS[key]);
        formatterByKey.set(key, formatter);
    }

    return formatter.format(date);
}

function createLocalDate(year: number, month: number, day: number, hour = 0, minute = 0, second = 0): Date {
    const date = new Date(0);
    date.setFullYear(year, month, day);
    date.setHours(hour, minute, second, 0);
    return date;
}

function toTwelveHour(hour24: number): { hour: number; meridiem: Meridiem } {
    const normalized = clamp(hour24, 0, 23);
    const meridiem = normalized >= 12 ? 'pm' : 'am';
    const hour = normalized % 12 || 12;
    return { hour, meridiem };
}

function toTwentyFourHour(hour12: number, meridiem: Meridiem): number {
    const normalized = clamp(hour12, 1, 12);
    if (meridiem === 'pm') {
        return normalized === 12 ? 12 : normalized + 12;
    }

    return normalized === 12 ? 0 : normalized;
}

function shiftYearMonth(year: number, month: number, delta: number): { month: number; year: number } {
    const totalMonths = (year * 12) + month + delta;
    const nextYear = Math.floor(totalMonths / 12);
    const nextMonth = ((totalMonths % 12) + 12) % 12;
    return { year: nextYear, month: nextMonth };
}

function shouldUseModalLayout(state: Maybe<PickerState>): boolean {
    const viewportWidth = Math.max(document.documentElement?.clientWidth ?? 0, window.innerWidth ?? 0);
    const rawBreakpoint = state?.picker ? getComputedStyle(state.picker).getPropertyValue('--nt-dtp-modal-breakpoint') : '';
    const parsedBreakpoint = Number.parseFloat(rawBreakpoint);
    const breakpoint = Number.isFinite(parsedBreakpoint) ? parsedBreakpoint : DEFAULT_MODAL_VIEWPORT_WIDTH_BREAKPOINT;
    return viewportWidth <= breakpoint;
}

function shouldOpenFromInputInteraction(state: Maybe<PickerState>): boolean {
    return !!state?.openOnFocus && !shouldUseModalLayout(state);
}

function formatDateKey(year: number, month: number, day: number): string {
    return `${padYear(year)}-${padTwo(month + 1)}-${padTwo(day)}`;
}

function formatTimeKey(hour: number, minute: number, second: number): string {
    return `${padTwo(hour)}:${padTwo(minute)}:${padTwo(second)}`;
}

function isValidDate(year: number, month: number, day: number): boolean {
    const candidate = createLocalDate(year, month, day);
    return candidate.getFullYear() === year && candidate.getMonth() === month && candidate.getDate() === day;
}

function normalizeDateToken(token: string): string | null {
    const trimmed = token?.trim();
    if (!trimmed) {
        return null;
    }

    const match = /^(-?\d{4,})-(\d{2})-(\d{2})$/.exec(trimmed);
    if (!match) {
        return null;
    }

    const year = toInt(match[1], 0);
    const month = toInt(match[2], 0) - 1;
    const day = toInt(match[3], 0);
    if (month < 0 || month > 11 || day < 1 || day > 31 || !isValidDate(year, month, day)) {
        return null;
    }

    return formatDateKey(year, month, day);
}

function normalizeTimeToken(token: string): string | null {
    const trimmed = token?.trim();
    if (!trimmed) {
        return null;
    }

    const match = /^(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(trimmed);
    if (!match) {
        return null;
    }

    const hour = toInt(match[1], 0);
    const minute = toInt(match[2], 0);
    const second = toInt(match[3], 0);
    if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59) {
        return null;
    }

    return formatTimeKey(hour, minute, second);
}

function parseDisabledSet(rawValue: Maybe<string>, normalizer: Normalizer): Set<string> {
    const values = new Set<string>();
    if (!rawValue) {
        return values;
    }

    const tokens = rawValue.split(/[,\s;|]+/);
    for (const token of tokens) {
        const normalized = normalizer(token);
        if (normalized) {
            values.add(normalized);
        }
    }

    return values;
}

function collectPickerElements(picker: HTMLElement): PickerElements {
    return {
        amButton: picker.querySelector<HTMLButtonElement>('[data-nt-dtp-meridiem="am"]'),
        calendarYearLabel: picker.querySelector<HTMLElement>('[data-nt-dtp-calendar-year-label]'),
        cancelButton: picker.querySelector<HTMLButtonElement>('[data-nt-dtp-action="cancel"]'),
        confirmButton: picker.querySelector<HTMLButtonElement>('[data-nt-dtp-action="confirm"]'),
        dayButtons: Array.from(picker.querySelectorAll<HTMLButtonElement>('[data-nt-dtp-day-index]')),
        headline: picker.querySelector<HTMLElement>('[data-nt-dtp-headline]'),
        hourInput: picker.querySelector<HTMLInputElement>('[data-nt-dtp-hour]'),
        minuteInput: picker.querySelector<HTMLInputElement>('[data-nt-dtp-minute]'),
        monthButtons: Array.from(picker.querySelectorAll<HTMLButtonElement>('[data-nt-dtp-month-index]')),
        monthLabel: picker.querySelector<HTMLElement>('[data-nt-dtp-month-label]'),
        pmButton: picker.querySelector<HTMLButtonElement>('[data-nt-dtp-meridiem="pm"]'),
        subhead: picker.querySelector<HTMLElement>('[data-nt-dtp-subhead]'),
        surface: picker.querySelector<HTMLElement>('[data-nt-dtp-surface]'),
        yearLabel: picker.querySelector<HTMLElement>('[data-nt-dtp-year-label]'),
        yearList: picker.querySelector<HTMLElement>('[data-nt-dtp-year-list]'),
    };
}

function applyScopeAttribute<TElement extends HTMLElement>(element: TElement, scopeAttributeName: string | null): TElement {
    if (scopeAttributeName) {
        element.setAttribute(scopeAttributeName, '');
    }

    return element;
}

function createScopedElement<K extends keyof HTMLElementTagNameMap>(tagName: K, scopeAttributeName: string | null, className?: string): HTMLElementTagNameMap[K] {
    const element = document.createElement(tagName);
    applyScopeAttribute(element, scopeAttributeName);
    if (className) {
        element.className = className;
    }

    return element;
}

function createIcon(icon: string, scopeAttributeName: string | null, additionalClass?: string): HTMLSpanElement {
    const span = createScopedElement('span', scopeAttributeName, additionalClass ? `nt-icon material-symbols-outlined mi-medium ${additionalClass}` : 'nt-icon material-symbols-outlined mi-medium');
    span.textContent = icon;
    return span;
}

function createPickerButton(scopeAttributeName: string | null, className: string, action: string, ariaLabel?: string): HTMLButtonElement {
    const button = createScopedElement('button', scopeAttributeName, className);
    button.type = 'button';
    button.dataset.ntDtpAction = action;
    if (ariaLabel) {
        button.setAttribute('aria-label', ariaLabel);
    }

    return button;
}

function getPickerContentElement(picker: HTMLElement, scopeAttributeName: string | null): HTMLElement {
    const existingContent = picker.querySelector<HTMLElement>('[data-nt-dtp-content]');
    if (existingContent) {
        return existingContent;
    }

    const content = createScopedElement('div', scopeAttributeName, 'nt-dtp-content');
    content.dataset.ntDtpContent = '';
    const actions = picker.querySelector('[data-nt-dtp-action="cancel"]')?.closest('.nt-dtp-actions');
    const surface = picker.querySelector<HTMLElement>('[data-nt-dtp-surface]') ?? picker;
    surface.insertBefore(content, actions ?? null);
    return content;
}

function createDatePanel(scopeAttributeName: string | null): HTMLElement {
    const panel = createScopedElement('div', scopeAttributeName, 'nt-dtp-date-panel');
    panel.dataset.ntDtpDatePanel = '';
    const nav = createScopedElement('div', scopeAttributeName, 'nt-dtp-calendar-nav');
    const previousButton = createPickerButton(scopeAttributeName, 'nt-dtp-nav-button', 'prev-month', 'Previous month');
    previousButton.appendChild(createIcon('chevron_left', scopeAttributeName));

    const menuButtons = createScopedElement('div', scopeAttributeName, 'nt-dtp-menu-buttons');
    const showMonthsButton = createPickerButton(scopeAttributeName, 'nt-dtp-menu-button', 'show-months', 'Change month');
    const monthLabel = createScopedElement('span', scopeAttributeName);
    monthLabel.dataset.ntDtpMonthLabel = '';
    monthLabel.textContent = 'Month';
    showMonthsButton.append(monthLabel, createIcon('arrow_drop_down', scopeAttributeName, 'nt-dtp-menu-button-icon'));
    const showYearsButton = createPickerButton(scopeAttributeName, 'nt-dtp-menu-button', 'show-years', 'Change year');
    const yearLabel = createScopedElement('span', scopeAttributeName);
    yearLabel.dataset.ntDtpCalendarYearLabel = '';
    yearLabel.textContent = 'Year';
    showYearsButton.append(yearLabel, createIcon('arrow_drop_down', scopeAttributeName, 'nt-dtp-menu-button-icon'));
    menuButtons.append(showMonthsButton, showYearsButton);

    const nextButton = createPickerButton(scopeAttributeName, 'nt-dtp-nav-button', 'next-month', 'Next month');
    nextButton.appendChild(createIcon('chevron_right', scopeAttributeName));
    nav.append(previousButton, menuButtons, nextButton);

    const weekdays = createScopedElement('div', scopeAttributeName, 'nt-dtp-weekdays');
    weekdays.setAttribute('aria-hidden', 'true');
    for (const label of WEEKDAY_LABELS) {
        const weekday = createScopedElement('span', scopeAttributeName);
        weekday.textContent = label;
        weekdays.appendChild(weekday);
    }

    const dayGrid = createScopedElement('div', scopeAttributeName, 'nt-dtp-day-grid');
    dayGrid.setAttribute('role', 'grid');
    dayGrid.setAttribute('aria-label', 'Calendar days');
    for (let index = 0; index < CALENDAR_DAY_COUNT; index += 1) {
        const button = createScopedElement('button', scopeAttributeName, 'nt-dtp-day');
        button.type = 'button';
        button.dataset.ntDtpDayIndex = String(index);
        button.setAttribute('role', 'gridcell');
        dayGrid.appendChild(button);
    }

    panel.append(nav, weekdays, dayGrid);
    return panel;
}

function createMonthPanel(scopeAttributeName: string | null): HTMLElement {
    const panel = createScopedElement('div', scopeAttributeName, 'nt-dtp-month-panel');
    panel.dataset.ntDtpMonthPanel = '';
    const nav = createScopedElement('div', scopeAttributeName, 'nt-dtp-calendar-nav');
    const previousButton = createPickerButton(scopeAttributeName, 'nt-dtp-nav-button', 'prev-year', 'Previous year');
    previousButton.appendChild(createIcon('chevron_left', scopeAttributeName));
    const yearLabel = createScopedElement('div', scopeAttributeName, 'nt-dtp-month-label');
    yearLabel.dataset.ntDtpYearLabel = '';
    yearLabel.textContent = 'Year';
    const nextButton = createPickerButton(scopeAttributeName, 'nt-dtp-nav-button', 'next-year', 'Next year');
    nextButton.appendChild(createIcon('chevron_right', scopeAttributeName));
    nav.append(previousButton, yearLabel, nextButton);

    const monthGrid = createScopedElement('div', scopeAttributeName, 'nt-dtp-month-grid');
    monthGrid.dataset.ntDtpMonthGrid = '';
    monthGrid.setAttribute('role', 'grid');
    monthGrid.setAttribute('aria-label', 'Months');
    for (let index = 0; index < MONTH_COUNT; index += 1) {
        const button = createScopedElement('button', scopeAttributeName, 'nt-dtp-month-option');
        button.type = 'button';
        button.dataset.ntDtpMonthIndex = String(index);
        button.setAttribute('role', 'gridcell');
        monthGrid.appendChild(button);
    }

    panel.append(nav, monthGrid);
    return panel;
}

function createYearPanel(scopeAttributeName: string | null): HTMLElement {
    const panel = createScopedElement('div', scopeAttributeName, 'nt-dtp-year-panel');
    panel.dataset.ntDtpYearPanel = '';
    const yearList = createScopedElement('div', scopeAttributeName, 'nt-dtp-year-list');
    yearList.dataset.ntDtpYearList = '';
    yearList.setAttribute('role', 'listbox');
    yearList.setAttribute('aria-label', 'Years');
    panel.appendChild(yearList);
    return panel;
}

function createTimePanel(scopeAttributeName: string | null): HTMLElement {
    const panel = createScopedElement('div', scopeAttributeName, 'nt-dtp-time-panel');
    panel.dataset.ntDtpTimePanel = '';
    const fields = createScopedElement('div', scopeAttributeName, 'nt-dtp-time-fields');

    const createTimeField = (text: string, dataKey: 'ntDtpHour' | 'ntDtpMinute'): HTMLLabelElement => {
        const label = createScopedElement('label', scopeAttributeName, 'nt-dtp-time-field');
        const input = createScopedElement('input', scopeAttributeName);
        input.inputMode = 'numeric';
        input.autocomplete = 'off';
        input.setAttribute('aria-label', text);
        input.dataset[dataKey] = '';
        const caption = createScopedElement('span', scopeAttributeName);
        caption.textContent = text;
        label.append(input, caption);
        return label;
    };

    const separator = createScopedElement('span', scopeAttributeName, 'nt-dtp-time-separator');
    separator.setAttribute('aria-hidden', 'true');
    separator.textContent = ':';

    const meridiemGroup = createScopedElement('div', scopeAttributeName, 'nt-dtp-meridiem-field');
    meridiemGroup.setAttribute('role', 'group');
    meridiemGroup.setAttribute('aria-label', 'AM or PM');
    const amButton = createPickerButton(scopeAttributeName, 'nt-dtp-meridiem-button', 'set-am');
    amButton.dataset.ntDtpMeridiem = 'am';
    amButton.textContent = 'AM';
    const pmButton = createPickerButton(scopeAttributeName, 'nt-dtp-meridiem-button', 'set-pm');
    pmButton.dataset.ntDtpMeridiem = 'pm';
    pmButton.textContent = 'PM';
    meridiemGroup.append(amButton, pmButton);

    fields.append(createTimeField('Hour', 'ntDtpHour'), separator, createTimeField('Minute', 'ntDtpMinute'), meridiemGroup);
    panel.appendChild(fields);
    return panel;
}

function refreshPickerElements(state: PickerState): void {
    const previousYearList = state.elements.yearList;
    state.elements = collectPickerElements(state.picker);
    if (previousYearList !== state.elements.yearList) {
        previousYearList?.removeEventListener('scroll', state.onYearListScroll);
        state.elements.yearList?.addEventListener('scroll', state.onYearListScroll);
        state.yearListStart = null;
        state.yearListEnd = null;
        state.yearListMinYear = null;
        state.yearListMaxYear = null;
        state.yearListSelectedYear = null;
    }
}

function ensureLazyPickerStructure(state: PickerState): boolean {
    const scopeAttributeName = getCssScopeAttributeName(state.picker);
    const content = getPickerContentElement(state.picker, scopeAttributeName);
    const { hasDate, hasTime } = getModeConfig(state.mode);
    const panels: [string, boolean, (scope: string | null) => HTMLElement][] = [
        ['[data-nt-dtp-date-panel]', hasDate, createDatePanel],
        ['[data-nt-dtp-month-panel]', hasDate || state.mode === 'month', createMonthPanel],
        ['[data-nt-dtp-year-panel]', hasDate, createYearPanel],
        ['[data-nt-dtp-time-panel]', hasTime, createTimePanel],
    ];
    let changed = false;

    for (const [selector, wanted, create] of panels) {
        const existing = content.querySelector(selector);
        if (wanted && !existing) {
            content.appendChild(create(scopeAttributeName));
            changed = true;
        }
        else if (!wanted && existing) {
            existing.remove();
            changed = true;
        }
    }

    if (changed) {
        refreshPickerElements(state);
    }

    return changed;
}

function cloneDraft(draft: PickerDraft): PickerDraft {
    return {
        day: draft.day,
        hour: draft.hour,
        minute: draft.minute,
        month: draft.month,
        second: draft.second,
        year: draft.year,
    };
}

function createDefaultDraft(): PickerDraft {
    const now = new Date();
    return {
        day: now.getDate(),
        hour: now.getHours(),
        minute: now.getMinutes(),
        month: now.getMonth(),
        second: 0,
        year: now.getFullYear(),
    };
}

function parseMode(state: PickerState): DateTimePickerMode {
    const mode = state.input?.dataset?.ntDtpMode ?? state.picker?.dataset?.ntDtpMode ?? 'none';
    if (mode === 'date' || mode === 'month' || mode === 'time' || mode === 'datetime') {
        return mode;
    }

    return 'none';
}

function getNativeInputType(mode: DateTimePickerMode): string | null {
    if (mode === 'datetime') {
        return 'datetime-local';
    }

    return mode === 'date' || mode === 'month' || mode === 'time' ? mode : null;
}

function setInputTypePreservingValue(input: HTMLInputElement, type: string): void {
    if (input.type === type) {
        return;
    }

    const value = input.value || input.getAttribute('value') || '';
    input.type = type;
    input.value = value;
}

function suppressNativeInputPicker(state: PickerState): void {
    const nativeInputType = getNativeInputType(state.mode);
    if (!nativeInputType) {
        return;
    }

    if (state.input.type !== 'text') {
        state.nativeInputType = nativeInputType;
    }
    setInputTypePreservingValue(state.input, 'text');
}

function restoreNativeInputPicker(state: PickerState): void {
    if (state.nativeInputType) {
        setInputTypePreservingValue(state.input, state.nativeInputType);
    }
}

function getModeConfig(mode: DateTimePickerMode): { hasDate: boolean; hasTime: boolean; supportsSubSelection: boolean } {
    return MODE_CONFIG[mode] ?? MODE_CONFIG.none;
}

const FORMAT_TOKEN_PATTERN = /yyyy|MM|M|dd|d|HH|H|hh|h|mm|m|ss|s|tt/g;

function escapeRegex(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

interface FormatPattern {
    regexes: RegExp[];
    tokens: string[];
}

const formatPatternByFormat = new Map<string, FormatPattern>();

function getFormatPattern(format: string): FormatPattern {
    let formatPattern = formatPatternByFormat.get(format);
    if (formatPattern) {
        return formatPattern;
    }

    const tokenMatches = Array.from(format.matchAll(FORMAT_TOKEN_PATTERN));
    const tokens = tokenMatches.map(match => match[0]);
    const buildLiteralPattern = (literal: string, allowOmittedPunctuation: boolean) => Array.from(literal)
        .map(character => allowOmittedPunctuation && !/\s/.test(character) ? `${escapeRegex(character)}?` : escapeRegex(character))
        .join('');
    const buildPattern = (includeLiterals: boolean, allowOmittedPunctuation = false) => {
        let pattern = '^';
        let previousIndex = 0;

        for (const match of tokenMatches) {
            if (includeLiterals) {
                pattern += buildLiteralPattern(format.slice(previousIndex, match.index), allowOmittedPunctuation);
            }
            pattern += match[0] === 'yyyy'
                ? '(\\d{4})'
                : match[0] === 'tt'
                    ? '(AM|PM)'
                    : match[0].length === 2
                        ? includeLiterals ? '(\\d{1,2})' : '(\\d{2})'
                        : '(\\d{1,2})';
            previousIndex = match.index + match[0].length;
        }

        if (includeLiterals) {
            pattern += buildLiteralPattern(format.slice(previousIndex), allowOmittedPunctuation);
        }
        return `${pattern}$`;
    };

    const patterns = [buildPattern(true), buildPattern(true, true)];
    if (tokens.every(token => token === 'yyyy' || token.length === 2)) {
        patterns.push(buildPattern(false));
    }

    formatPattern = { regexes: patterns.map(pattern => new RegExp(pattern, 'i')), tokens };
    formatPatternByFormat.set(format, formatPattern);
    return formatPattern;
}

function parseDraftWithPattern(format: string, value: string): PickerDraft | null {
    const { regexes, tokens } = getFormatPattern(format);
    let match: RegExpExecArray | null = null;
    for (const regex of regexes) {
        match = regex.exec(value);
        if (match) {
            break;
        }
    }

    if (!match) {
        return null;
    }

    const draft = createDefaultDraft();
    let hour12: number | null = null;
    let meridiem: string | null = null;

    for (let index = 0; index < tokens.length; index += 1) {
        const token = tokens[index];
        const part = match[index + 1];
        const parsedPart = toInt(part);
        switch (token) {
            case 'yyyy':
                if (parsedPart < MIN_PICKER_YEAR || parsedPart > MAX_PICKER_YEAR) {
                    return null;
                }
                draft.year = parsedPart;
                break;
            case 'MM':
            case 'M':
                if (parsedPart < 1 || parsedPart > MONTH_COUNT) {
                    return null;
                }
                draft.month = parsedPart - 1;
                break;
            case 'dd':
            case 'd':
                if (parsedPart < 1 || parsedPart > 31) {
                    return null;
                }
                draft.day = parsedPart;
                break;
            case 'HH':
            case 'H':
                if (parsedPart < 0 || parsedPart > 23) {
                    return null;
                }
                draft.hour = parsedPart;
                break;
            case 'hh':
            case 'h':
                if (parsedPart < 1 || parsedPart > 12) {
                    return null;
                }
                hour12 = parsedPart;
                break;
            case 'mm':
            case 'm':
                if (parsedPart < 0 || parsedPart > 59) {
                    return null;
                }
                draft.minute = parsedPart;
                break;
            case 'ss':
            case 's':
                if (parsedPart < 0 || parsedPart > 59) {
                    return null;
                }
                draft.second = parsedPart;
                break;
            case 'tt':
                meridiem = part.toLowerCase();
                break;
        }
    }

    if (hour12 !== null) {
        draft.hour = toTwentyFourHour(hour12, meridiem === 'pm' ? 'pm' : 'am');
    }
    else if (meridiem !== null) {
        draft.hour = toTwentyFourHour(toTwelveHour(draft.hour).hour, meridiem === 'pm' ? 'pm' : 'am');
    }

    const hasYear = tokens.includes('yyyy');
    const hasMonth = tokens.includes('MM') || tokens.includes('M');
    const hasDay = tokens.includes('dd') || tokens.includes('d');
    if (hasYear && hasMonth && hasDay && !isValidDate(draft.year, draft.month, draft.day)) {
        return null;
    }

    return draft;
}

function parseDraft(mode: DateTimePickerMode, value: Maybe<string>, format?: Maybe<string>): PickerDraft | null {
    if (!value) {
        return null;
    }

    if (format) {
        const formattedDraft = parseDraftWithPattern(format, value);
        if (formattedDraft) {
            return formattedDraft;
        }
    }

    if (mode === 'date') {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
        if (!match) {
            return null;
        }

        const year = toInt(match[1], 0);
        const month = clamp(toInt(match[2], 1), 1, 12) - 1;
        const day = clamp(toInt(match[3], 1), 1, 31);
        return {
            day,
            hour: 0,
            minute: 0,
            month,
            second: 0,
            year,
        };
    }

    if (mode === 'time') {
        const match = /^(\d{2}):(\d{2})(?::(\d{2})(?:\.\d{1,3})?)?$/.exec(value);
        if (!match) {
            return null;
        }

        const current = createDefaultDraft();
        return {
            day: current.day,
            hour: clamp(toInt(match[1], 0), 0, 23),
            minute: clamp(toInt(match[2], 0), 0, 59),
            month: current.month,
            second: 0,
            year: current.year,
        };
    }

    if (mode === 'month') {
        const match = /^(\d{4})-(\d{2})$/.exec(value);
        if (!match) {
            return null;
        }

        return {
            day: 1,
            hour: 0,
            minute: 0,
            month: clamp(toInt(match[2], 1), 1, 12) - 1,
            second: 0,
            year: toInt(match[1], 0),
        };
    }

    if (mode === 'datetime') {
        const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2})(?:\.\d{1,3})?)?$/.exec(value);
        if (!match) {
            return null;
        }

        return {
            day: clamp(toInt(match[3], 1), 1, 31),
            hour: clamp(toInt(match[4], 0), 0, 23),
            minute: clamp(toInt(match[5], 0), 0, 59),
            month: clamp(toInt(match[2], 1), 1, 12) - 1,
            second: 0,
            year: toInt(match[1], 0),
        };
    }

    return null;
}

function formatDraftWithPattern(format: string, draft: PickerDraft): string {
    return format.replace(FORMAT_TOKEN_PATTERN, token => {
        switch (token) {
            case 'yyyy':
                return padYear(draft.year);
            case 'MM':
                return padTwo(draft.month + 1);
            case 'M':
                return String(draft.month + 1);
            case 'dd':
                return padTwo(draft.day);
            case 'd':
                return String(draft.day);
            case 'HH':
                return padTwo(draft.hour);
            case 'H':
                return String(draft.hour);
            case 'hh':
                return padTwo(toTwelveHour(draft.hour).hour);
            case 'h':
                return String(toTwelveHour(draft.hour).hour);
            case 'mm':
                return padTwo(draft.minute);
            case 'm':
                return String(draft.minute);
            case 'ss':
                return padTwo(draft.second);
            case 's':
                return String(draft.second);
            case 'tt':
                return draft.hour >= 12 ? 'PM' : 'AM';
            default:
                return token;
        }
    });
}

function formatDraft(mode: DateTimePickerMode, draft: PickerDraft, format: Maybe<string>): string {
    if (format) {
        return formatDraftWithPattern(format, draft);
    }

    if (mode === 'date') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}-${padTwo(draft.day)}`;
    }

    if (mode === 'time') {
        return `${padTwo(draft.hour)}:${padTwo(draft.minute)}:${padTwo(draft.second)}`;
    }

    if (mode === 'month') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}`;
    }

    if (mode === 'datetime') {
        return `${padYear(draft.year)}-${padTwo(draft.month + 1)}-${padTwo(draft.day)}T${padTwo(draft.hour)}:${padTwo(draft.minute)}`;
    }

    return '';
}

function toDateComparable(draft: Pick<PickerDraft, 'day' | 'month' | 'year'>): number {
    return (draft.year * 10000) + ((draft.month + 1) * 100) + draft.day;
}

function toTimeComparable(draft: Pick<PickerDraft, 'hour' | 'minute' | 'second'>): number {
    return (draft.hour * 3600) + (draft.minute * 60) + draft.second;
}

function toMonthComparable(draft: Pick<PickerDraft, 'month' | 'year'>): number {
    return (draft.year * 100) + (draft.month + 1);
}

function toDateTimeComparable(draft: PickerDraft): number {
    return createLocalDate(draft.year, draft.month, draft.day, draft.hour, draft.minute, draft.second).getTime();
}

function updateConstraints(state: PickerState): void {
    state.minDraft = parseDraft(state.mode, state.input?.min);
    state.maxDraft = parseDraft(state.mode, state.input?.max);
}

function updateDisabledSets(state: PickerState): void {
    state.disabledDateSet = parseDisabledSet(state.input?.dataset?.ntDtpDisabledDates, normalizeDateToken);
    state.disabledTimeSet = parseDisabledSet(state.input?.dataset?.ntDtpDisabledTimes, normalizeTimeToken);
}

function getComparable(mode: DateTimePickerMode): ((draft: PickerDraft) => number) | null {
    switch (mode) {
        case 'date': return toDateComparable;
        case 'time': return toTimeComparable;
        case 'month': return toMonthComparable;
        case 'datetime': return toDateTimeComparable;
        default: return null;
    }
}

function clampDraftToConstraints(state: PickerState, draft: PickerDraft): PickerDraft {
    const comparable = getComparable(state.mode);
    if (comparable) {
        const value = comparable(draft);
        if (state.minDraft && value < comparable(state.minDraft)) {
            return cloneDraft(state.minDraft);
        }
        if (state.maxDraft && value > comparable(state.maxDraft)) {
            return cloneDraft(state.maxDraft);
        }
    }

    return cloneDraft(draft);
}

function setInputValue(state: PickerState, value: string): void {
    if (!state.input) {
        return;
    }

    state.input.value = value;
    state.input.dispatchEvent(new Event('input', { bubbles: true }));
    state.input.dispatchEvent(new Event('change', { bubbles: true }));
}

// Writes the draft to the input as soon as it changes so selections never wait for OK.
function commitDraft(state: PickerState): boolean {
    state.draft = clampDraftToConstraints(state, state.draft);
    if (isDraftDisabled(state, state.draft)) {
        return false;
    }

    const value = formatDraft(state.mode, state.draft, state.input.getAttribute('format'));
    if (value !== state.input.value) {
        setInputValue(state, value);
    }

    return true;
}

function setInputParseError(state: PickerState, message: string | null): void {
    const field = state.input.closest('.nt-input');
    if (message) {
        if (state.input.dataset.ntDtpParseInvalid !== 'true') {
            state.input.dataset.ntDtpPreviousAriaInvalid = state.input.getAttribute('aria-invalid') ?? '';
        }

        state.input.dataset.ntDtpParseInvalid = 'true';
        state.input.setCustomValidity(message);
        state.input.setAttribute('aria-invalid', 'true');
        field?.classList.add('nt-dtp-parse-invalid');
        return;
    }

    state.input.setCustomValidity('');
    if (state.input.dataset.ntDtpParseInvalid !== 'true') {
        return;
    }

    const previousAriaInvalid = state.input.dataset.ntDtpPreviousAriaInvalid;
    if (previousAriaInvalid) {
        state.input.setAttribute('aria-invalid', previousAriaInvalid);
    }
    else {
        state.input.removeAttribute('aria-invalid');
    }
    delete state.input.dataset.ntDtpParseInvalid;
    delete state.input.dataset.ntDtpPreviousAriaInvalid;
    field?.classList.remove('nt-dtp-parse-invalid');
}

function trySyncDraftFromInputValue(state: PickerState): boolean {
    const parsed = parseDraft(state.mode, state.input?.value, state.input?.getAttribute('format'));
    if (!parsed) {
        return false;
    }

    state.draft = clampDraftToConstraints(state, parsed);
    state.viewYear = state.draft.year;
    state.viewMonth = state.draft.month;
    state.meridiem = toTwelveHour(state.draft.hour).meridiem;
    return true;
}

function syncDraftFromInputValue(state: PickerState): void {
    updateConstraints(state);
    if (trySyncDraftFromInputValue(state)) {
        return;
    }

    const current = createDefaultDraft();
    state.draft = clampDraftToConstraints(state, current);
    state.viewYear = current.year;
    state.viewMonth = current.month;
    state.meridiem = toTwelveHour(state.draft.hour).meridiem;
}

function isDateDisabled(state: PickerState, year: number, month: number, day: number): boolean {
    if (state.disabledDateSet?.has(formatDateKey(year, month, day))) {
        return true;
    }

    const value = toDateComparable({ year, month, day });
    return (!!state.minDraft && value < toDateComparable(state.minDraft)) || (!!state.maxDraft && value > toDateComparable(state.maxDraft));
}

function isTimeDisabled(state: PickerState, hour: number, minute: number, second: number): boolean {
    return state.disabledTimeSet?.has(formatTimeKey(hour, minute, second)) ?? false;
}

function isDraftDisabled(state: PickerState, draft: Maybe<PickerDraft>): boolean {
    if (!draft) {
        return false;
    }

    const checksDate = state.mode === 'date' || state.mode === 'datetime';
    const checksTime = state.mode === 'time' || state.mode === 'datetime';
    return (checksDate && isDateDisabled(state, draft.year, draft.month, draft.day)) || (checksTime && isTimeDisabled(state, draft.hour, draft.minute, draft.second));
}

function renderHeader(state: PickerState): void {
    const { headline, subhead } = state.elements;

    if (!headline || !subhead) {
        return;
    }

    const local = createLocalDate(state.draft.year, state.draft.month, state.draft.day, state.draft.hour, state.draft.minute, state.draft.second);

    if (state.mode === 'time') {
        headline.textContent = formatLocal('time', local);
        subhead.textContent = 'Select a time';
        return;
    }

    if (state.mode === 'month') {
        headline.textContent = formatLocal('monthYear', local);
        subhead.textContent = 'Select a month';
        return;
    }

    headline.textContent = formatLocal('dayHeadline', local);
    subhead.textContent = formatLocal(state.mode === 'date' ? 'monthYear' : 'time', local);
}

function renderTimeFields(state: PickerState): void {
    const { amButton, hourInput, minuteInput, pmButton } = state.elements;
    const twelveHour = toTwelveHour(state.draft.hour);

    state.meridiem = twelveHour.meridiem;

    if (hourInput) {
        hourInput.value = String(twelveHour.hour);
    }
    if (minuteInput) {
        minuteInput.value = padTwo(state.draft.minute);
    }
    if (amButton) {
        const isSelected = state.meridiem === 'am';
        amButton.classList.toggle('nt-dtp-meridiem-selected', isSelected);
        amButton.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }
    if (pmButton) {
        const isSelected = state.meridiem === 'pm';
        pmButton.classList.toggle('nt-dtp-meridiem-selected', isSelected);
        pmButton.setAttribute('aria-pressed', isSelected ? 'true' : 'false');
    }
}

function renderCalendar(state: PickerState): void {
    const { calendarYearLabel, dayButtons, monthLabel } = state.elements;

    if (!monthLabel || !dayButtons?.length) {
        return;
    }

    const monthStart = createLocalDate(state.viewYear, state.viewMonth, 1);
    monthLabel.textContent = formatLocal('month', monthStart);
    if (calendarYearLabel) {
        calendarYearLabel.textContent = formatLocal('year', monthStart);
    }

    const startOffset = monthStart.getDay();
    const rangeStart = createLocalDate(state.viewYear, state.viewMonth, 1 - startOffset);
    const today = new Date();
    const todayKey = toDateComparable({ year: today.getFullYear(), month: today.getMonth(), day: today.getDate() });
    const selectedKey = toDateComparable(state.draft);

    dayButtons.forEach((button: HTMLButtonElement, index: number) => {
        const cellDate = createLocalDate(rangeStart.getFullYear(), rangeStart.getMonth(), rangeStart.getDate() + index);
        const year = cellDate.getFullYear();
        const month = cellDate.getMonth();
        const day = cellDate.getDate();
        const cellKey = toDateComparable({ year, month, day });
        const inCurrentMonth = month === state.viewMonth && year === state.viewYear;
        const selected = cellKey === selectedKey;
        const isToday = cellKey === todayKey;
        const disabled = isDateDisabled(state, year, month, day);

        button.textContent = String(day);
        button.dataset.ntDtpYear = String(year);
        button.dataset.ntDtpMonth = String(month);
        button.dataset.ntDtpDay = String(day);
        button.disabled = disabled;
        button.classList.toggle('nt-dtp-outside-month', !inCurrentMonth);
        button.classList.toggle('nt-dtp-selected-day', selected);
        button.classList.toggle('nt-dtp-today', isToday);
        button.setAttribute('aria-selected', selected ? 'true' : 'false');
        button.setAttribute('aria-current', isToday ? 'date' : 'false');
    });
}

function isMonthDisabled(state: PickerState, year: number, month: number): boolean {
    const value = toMonthComparable({ year, month });
    return (!!state.minDraft && value < toMonthComparable(state.minDraft)) || (!!state.maxDraft && value > toMonthComparable(state.maxDraft));
}

function renderMonthGrid(state: PickerState): void {
    const { monthButtons, yearLabel } = state.elements;

    if (!yearLabel || !monthButtons?.length) {
        return;
    }

    yearLabel.textContent = padYear(state.viewYear);

    const today = new Date();
    const todayKey = toMonthComparable({ year: today.getFullYear(), month: today.getMonth() });
    const selectedKey = toMonthComparable(state.draft);

    monthButtons.forEach((button: HTMLButtonElement) => {
        const month = toInt(button.dataset.ntDtpMonthIndex, 0);
        const cellKey = toMonthComparable({ year: state.viewYear, month });
        const selected = cellKey === selectedKey;
        const isToday = cellKey === todayKey;
        const disabled = isMonthDisabled(state, state.viewYear, month);
        const monthLabel = formatLocal('monthShort', createLocalDate(state.viewYear, month, 1));

        button.textContent = monthLabel;
        button.dataset.ntDtpYear = String(state.viewYear);
        button.dataset.ntDtpMonth = String(month);
        button.disabled = disabled;
        button.classList.toggle('nt-dtp-selected-day', selected);
        button.classList.toggle('nt-dtp-today', isToday);
        button.setAttribute('aria-selected', selected ? 'true' : 'false');
        button.setAttribute('aria-current', isToday ? 'date' : 'false');
    });
}

function getYearListMinYear(state: PickerState): number {
    return clamp(state.minDraft?.year ?? MIN_PICKER_YEAR, MIN_PICKER_YEAR, MAX_PICKER_YEAR);
}

function getYearListMaxYear(state: PickerState): number {
    return clamp(state.maxDraft?.year ?? MAX_PICKER_YEAR, getYearListMinYear(state), MAX_PICKER_YEAR);
}

function getInitialYearListRange(state: PickerState): { end: number; start: number } {
    const selectedYear = state.draft?.year ?? state.viewYear;
    const minYear = getYearListMinYear(state);
    const maxYear = getYearListMaxYear(state);

    return {
        end: Math.min(selectedYear + YEAR_LIST_BATCH_SIZE, maxYear),
        start: Math.max(selectedYear - YEAR_LIST_BATCH_SIZE, minYear),
    };
}

function isYearDisabled(state: PickerState, year: number): boolean {
    return (!!state.minDraft && year < state.minDraft.year) || (!!state.maxDraft && year > state.maxDraft.year);
}

function getCssScopeAttributeName(element: HTMLElement): string | null {
    for (const attribute of Array.from(element.attributes)) {
        if (attribute.name.startsWith('b-')) {
            return attribute.name;
        }
    }

    return null;
}

function createYearOption(year: number, scopeAttributeName: string | null): HTMLButtonElement {
    const yearButton = document.createElement('button');
    yearButton.type = 'button';
    yearButton.className = 'nt-dtp-year-option';
    yearButton.tabIndex = -1;
    yearButton.dataset.ntDtpYearOption = String(year);
    yearButton.setAttribute('role', 'option');
    yearButton.textContent = padYear(year);
    return applyScopeAttribute(yearButton, scopeAttributeName);
}

function appendYearOptions(yearList: HTMLElement, start: number, end: number, placement: 'afterbegin' | 'beforeend'): void {
    const fragment = document.createDocumentFragment();
    const scopeAttributeName = getCssScopeAttributeName(yearList);
    for (let year = start; year <= end; year += 1) {
        fragment.appendChild(createYearOption(year, scopeAttributeName));
    }

    if (placement === 'afterbegin') {
        yearList.prepend(fragment);
        return;
    }

    yearList.append(fragment);
}

function syncYearOptionStates(state: PickerState, yearList: HTMLElement): void {
    const selectedYear = state.draft.year;
    const todayYear = new Date().getFullYear();
    const yearButtons = yearList.querySelectorAll<HTMLButtonElement>('[data-nt-dtp-year-option]');

    yearButtons.forEach((button: HTMLButtonElement) => {
        const year = toInt(button.dataset.ntDtpYearOption, state.viewYear);
        const selected = year === selectedYear;
        const isToday = year === todayYear;
        button.disabled = isYearDisabled(state, year);
        button.classList.toggle('nt-dtp-selected-year', selected);
        button.classList.toggle('nt-dtp-current-year', isToday);
        button.setAttribute('aria-selected', selected ? 'true' : 'false');
        button.setAttribute('aria-current', isToday ? 'date' : 'false');
    });
}

function getFirstRenderedYear(yearList: HTMLElement): number | null {
    const firstButton = yearList.firstElementChild as HTMLButtonElement | null;
    return firstButton ? toInt(firstButton.dataset.ntDtpYearOption, 0) : null;
}

function getLastRenderedYear(yearList: HTMLElement): number | null {
    const lastButton = yearList.lastElementChild as HTMLButtonElement | null;
    return lastButton ? toInt(lastButton.dataset.ntDtpYearOption, 0) : null;
}

function synchronizeRenderedYearRange(state: PickerState, yearList: HTMLElement): void {
    state.yearListStart = getFirstRenderedYear(yearList);
    state.yearListEnd = getLastRenderedYear(yearList);
}

function trimYearListWindow(state: PickerState, yearList: HTMLElement, preserveEdge: 'end' | 'start'): void {
    while (yearList.childElementCount > YEAR_LIST_MAX_OPTION_COUNT) {
        if (preserveEdge === 'end') {
            yearList.firstElementChild?.remove();
        }
        else {
            yearList.lastElementChild?.remove();
        }
    }

    synchronizeRenderedYearRange(state, yearList);
}

function renderYearList(state: PickerState): void {
    const yearList = state.elements.yearList;
    if (!yearList) {
        return;
    }

    const selectedYear = state.draft?.year ?? state.viewYear;
    const targetRange = getInitialYearListRange(state);
    const minYear = getYearListMinYear(state);
    const maxYear = getYearListMaxYear(state);
    const shouldResetRange = state.yearListStart == null
        || state.yearListEnd == null
        || state.yearListSelectedYear !== selectedYear
        || state.yearListMinYear !== minYear
        || state.yearListMaxYear !== maxYear
        || yearList.childElementCount === 0;
    const range: { end: number; start: number } = shouldResetRange
        ? targetRange
        : {
            end: clamp(state.yearListEnd ?? targetRange.end, minYear, maxYear),
            start: clamp(state.yearListStart ?? targetRange.start, minYear, maxYear),
        };

    if (state.yearListStart !== range.start || state.yearListEnd !== range.end || yearList.childElementCount === 0) {
        yearList.replaceChildren();
        appendYearOptions(yearList, range.start, range.end, 'beforeend');
        state.yearListStart = range.start;
        state.yearListEnd = range.end;
        state.yearListMinYear = minYear;
        state.yearListMaxYear = maxYear;
        state.yearListSelectedYear = selectedYear;
        trimYearListWindow(state, yearList, 'start');
    }

    syncYearOptionStates(state, yearList);

    if (state.dateView === 'year') {
        const selectedButton = yearList.querySelector<HTMLElement>('.nt-dtp-selected-year') ?? yearList.querySelector<HTMLElement>('.nt-dtp-current-year');
        selectedButton?.scrollIntoView?.({ block: 'center' });
    }
}

function extendYearList(state: PickerState, yearList: HTMLElement, direction: 'down' | 'up'): void {
    if (state.yearListStart == null || state.yearListEnd == null || yearList.childElementCount === 0) {
        renderYearList(state);
        return;
    }

    if (direction === 'up') {
        const minYear = getYearListMinYear(state);
        const nextStart = Math.max(minYear, state.yearListStart - YEAR_LIST_BATCH_SIZE);
        if (nextStart >= state.yearListStart) {
            return;
        }

        const previousScrollHeight = yearList.scrollHeight;
        appendYearOptions(yearList, nextStart, state.yearListStart - 1, 'afterbegin');
        state.yearListStart = nextStart;
        trimYearListWindow(state, yearList, 'start');
        syncYearOptionStates(state, yearList);
        yearList.scrollTop += yearList.scrollHeight - previousScrollHeight;
        return;
    }

    const maxYear = getYearListMaxYear(state);
    const nextEnd = Math.min(maxYear, state.yearListEnd + YEAR_LIST_BATCH_SIZE);
    if (nextEnd <= state.yearListEnd) {
        return;
    }

    appendYearOptions(yearList, state.yearListEnd + 1, nextEnd, 'beforeend');
    state.yearListEnd = nextEnd;
    trimYearListWindow(state, yearList, 'end');
    syncYearOptionStates(state, yearList);
}

function handleYearListScroll(state: PickerState, yearList: HTMLElement): void {
    if (state.dateView !== 'year') {
        return;
    }

    if (yearList.scrollTop <= YEAR_LIST_SCROLL_THRESHOLD) {
        extendYearList(state, yearList, 'up');
        return;
    }

    const distanceFromBottom = yearList.scrollHeight - yearList.clientHeight - yearList.scrollTop;
    if (distanceFromBottom <= YEAR_LIST_SCROLL_THRESHOLD) {
        extendYearList(state, yearList, 'down');
    }
}

function isSubSelectionView(state: PickerState): boolean {
    return (state.dateView === 'month' || state.dateView === 'year') && getModeConfig(state.mode).supportsSubSelection;
}

function renderActionState(state: PickerState): void {
    const { cancelButton, confirmButton } = state.elements;
    const subSelection = isSubSelectionView(state);

    if (cancelButton) {
        cancelButton.textContent = subSelection ? 'Back' : 'Cancel';
        cancelButton.setAttribute('aria-label', subSelection ? 'Back to calendar' : 'Cancel');
    }

    if (confirmButton) {
        confirmButton.hidden = subSelection;
        confirmButton.setAttribute('aria-hidden', subSelection ? 'true' : 'false');
        confirmButton.disabled = isDraftDisabled(state, state.draft);
    }
}

function makePickerUntabbable(state: Maybe<PickerState>): void {
    if (!state?.picker) {
        return;
    }

    const focusableElements = state.picker.querySelectorAll('button, input, select, textarea, a[href], [tabindex]');
    for (const element of focusableElements) {
        if (element.getAttribute('tabindex') !== '-1') {
            element.setAttribute('tabindex', '-1');
        }
    }
}

function getRenderSignature(state: PickerState): string {
    return [
        state.mode,
        state.dateView,
        state.viewYear,
        state.viewMonth,
        state.draft?.year,
        state.draft?.month,
        state.draft?.day,
        state.draft?.hour,
        state.draft?.minute,
        state.draft?.second,
        state.meridiem,
        state.input?.value ?? '',
        state.input?.min ?? '',
        state.input?.max ?? '',
        state.input?.dataset?.ntDtpDisabledDates ?? '',
        state.input?.dataset?.ntDtpDisabledTimes ?? '',
    ].join('|');
}

function renderPicker(state: PickerState): void {
    if (!state.picker) {
        return;
    }

    const structureChanged = ensureLazyPickerStructure(state);
    const renderSignature = getRenderSignature(state);
    if (!structureChanged && state.lastRenderSignature === renderSignature) {
        if (state.isOpen && (!state.isModal || !shouldUseModalLayout(state))) {
            schedulePositionUpdate();
        }
        return;
    }

    state.lastRenderSignature = renderSignature;
    state.picker.classList.remove('nt-dtp-mode-date', 'nt-dtp-mode-month', 'nt-dtp-mode-time', 'nt-dtp-mode-datetime');
    state.picker.classList.add(`nt-dtp-mode-${state.mode}`);
    state.picker.classList.toggle('nt-dtp-view-month', state.dateView === 'month' && (state.mode === 'date' || state.mode === 'datetime'));
    state.picker.classList.toggle('nt-dtp-view-year', state.dateView === 'year' && (state.mode === 'date' || state.mode === 'datetime'));
    state.picker.dataset.ntDtpMode = state.mode;

    renderHeader(state);

    const modeConfig = getModeConfig(state.mode);
    if (state.mode === 'month' || state.dateView === 'month') {
        renderMonthGrid(state);
    }

    if (state.dateView === 'year') {
        renderYearList(state);
    }

    if (modeConfig.hasDate) {
        renderCalendar(state);
    }

    if (modeConfig.hasTime) {
        renderTimeFields(state);
    }

    renderActionState(state);
    if (structureChanged) {
        makePickerUntabbable(state);
        state.picker.dataset.ntDtpTabbableMarker = '';
        state.didMakePickerUntabbable = true;
    }

    if (state.isOpen && (!state.isModal || !shouldUseModalLayout(state))) {
        schedulePositionUpdate();
    }
}

function isTargetInsideState(state: Maybe<PickerState>, target: EventTarget | null): boolean {
    if (!state || !target) {
        return false;
    }

    const label = state.label ?? state.input?.closest?.('label') ?? state.input?.labels?.[0] ?? null;
    const surface = state.elements.surface;
    const targetIsBackdrop = state.isModal && state.picker === target;

    if (targetIsBackdrop) {
        return false;
    }

    return target instanceof Node && (label?.contains(target) || state.trigger?.contains(target) || surface?.contains(target) || (!state.isModal && state.picker.contains(target)));
}

function findTrigger(input: HTMLInputElement, picker: HTMLElement): HTMLButtonElement | null {
    const targetId = input.dataset.ntDtpTarget ?? picker.id;
    const scopedRoot = input.closest<HTMLElement>('.nt-input-date-time') ?? picker.closest<HTMLElement>('.nt-input-date-time');
    const scopedTrigger = scopedRoot?.querySelector<HTMLButtonElement>('[data-nt-dtp-trigger="true"][data-nt-dtp-target]');
    if (scopedTrigger?.dataset.ntDtpTarget === targetId) {
        return scopedTrigger;
    }

    const inputParentTrigger = input.parentElement?.querySelector<HTMLButtonElement>('[data-nt-dtp-trigger="true"][data-nt-dtp-target]');
    if (inputParentTrigger?.dataset.ntDtpTarget === targetId) {
        return inputParentTrigger;
    }

    return null;
}

function setTrigger(state: PickerState, trigger: HTMLButtonElement | null): void {
    if (state.trigger === trigger) {
        return;
    }

    state.trigger?.removeEventListener('mousedown', state.onTriggerMouseDown);
    state.trigger?.removeEventListener('click', state.onTriggerClick);
    state.trigger = trigger;
    state.trigger?.addEventListener('mousedown', state.onTriggerMouseDown);
    state.trigger?.addEventListener('click', state.onTriggerClick);
}

function getLabelAnchorRect(state: PickerState): DOMRect | null {
    const labelElement = state.input.closest('label') ?? state.input.labels?.[0] ?? null;
    return labelElement?.getBoundingClientRect ? labelElement.getBoundingClientRect() : null;
}

function positionPicker(state: Maybe<PickerState>): void {
    if (!state?.isOpen || !state.input?.isConnected || !state.picker?.isConnected) {
        return;
    }

    const picker = state.picker;
    const useModalLayout = shouldUseModalLayout(state);
    state.isModal = useModalLayout;
    picker.classList.toggle('nt-dtp-modal', useModalLayout);
    picker.setAttribute('aria-modal', useModalLayout ? 'true' : 'false');

    if (useModalLayout) {
        picker.style.left = '0px';
        picker.style.top = '0px';
        picker.style.visibility = 'visible';
        return;
    }

    const inputRect = state.input.getBoundingClientRect();
    const labelAnchorRect = getLabelAnchorRect(state);
    const horizontalAnchorRect = labelAnchorRect ?? inputRect;
    const anchorTop = labelAnchorRect ? Math.min(labelAnchorRect.top, inputRect.top) : inputRect.top;
    const anchorBottom = labelAnchorRect ? Math.max(labelAnchorRect.bottom, inputRect.bottom) : inputRect.bottom;

    picker.style.visibility = 'hidden';
    picker.style.left = '0px';
    picker.style.top = '0px';

    const pickerRect = picker.getBoundingClientRect();
    const viewportWidth = Math.max(document.documentElement?.clientWidth ?? 0, window.innerWidth ?? 0);
    const viewportHeight = Math.max(document.documentElement?.clientHeight ?? 0, window.innerHeight ?? 0);
    const edgePadding = 8;
    const offset = 6;

    const maxLeft = Math.max(edgePadding, viewportWidth - pickerRect.width - edgePadding);
    const left = clamp(horizontalAnchorRect.left, edgePadding, maxLeft);

    const spaceBelow = Math.max(0, viewportHeight - anchorBottom - edgePadding);
    const spaceAbove = Math.max(0, anchorTop - edgePadding);
    const openBelow = spaceBelow >= pickerRect.height || spaceBelow >= spaceAbove;

    const rawTop = openBelow
        ? anchorBottom + offset
        : anchorTop - pickerRect.height - offset;

    const maxTop = Math.max(edgePadding, viewportHeight - pickerRect.height - edgePadding);
    const top = clamp(rawTop, edgePadding, maxTop);

    picker.style.left = `${Math.round(left)}px`;
    picker.style.top = `${Math.round(top)}px`;
    picker.style.visibility = 'visible';
}

function schedulePositionUpdate(): void {
    if (!activePickerState?.isOpen) {
        return;
    }

    if (shouldUseModalLayout(activePickerState)) {
        if (pendingFrame !== null) {
            cancelAnimationFrame(pendingFrame);
            pendingFrame = null;
        }

        positionPicker(activePickerState);
        return;
    }

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
    }

    pendingFrame = requestAnimationFrame(() => {
        pendingFrame = null;
        positionPicker(activePickerState);
    });
}

function closePicker(state: Maybe<PickerState>, restoreFocus = false): void {
    if (!state?.isOpen || !state.picker) {
        return;
    }

    state.isOpen = false;
    state.isModal = false;
    state.picker.classList.remove('nt-dtp-open');
    state.picker.classList.remove('nt-dtp-modal');
    state.picker.setAttribute('aria-hidden', 'true');
    state.picker.setAttribute('aria-modal', 'false');
    state.picker.style.visibility = 'hidden';

    if (activePickerState === state) {
        activePickerState = null;
    }

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
        pendingFrame = null;
    }

    if (restoreFocus && state.input?.focus) {
        state.input.focus({ preventScroll: true });
    }
}

function openPicker(state: Maybe<PickerState>): void {
    if (!state?.input || !state?.picker) {
        return;
    }

    if (state.input.disabled || state.input.readOnly) {
        return;
    }

    if (activePickerState && activePickerState !== state) {
        closePicker(activePickerState);
    }

    const nextMode = parseMode(state);
    if (nextMode === 'none') {
        return;
    }

    if (state.isOpen && activePickerState === state && state.mode === nextMode) {
        schedulePositionUpdate();
        return;
    }

    state.mode = nextMode;
    state.dateView = 'calendar';
    updateDisabledSets(state);
    syncDraftFromInputValue(state);
    state.openValue = state.input.value;
    state.picker.classList.add('nt-dtp-open');
    state.picker.setAttribute('aria-hidden', 'false');
    state.isOpen = true;
    activePickerState = state;

    if (shouldUseModalLayout(state)) {
        state.isModal = true;
        state.picker.classList.add('nt-dtp-modal');
        state.picker.setAttribute('aria-modal', 'true');
        state.picker.style.left = '0px';
        state.picker.style.top = '0px';
        state.picker.style.visibility = 'visible';
    }
    else {
        state.isModal = false;
        state.picker.classList.remove('nt-dtp-modal');
        state.picker.setAttribute('aria-modal', 'false');
    }

    renderPicker(state);

    if (document.activeElement !== state.input && state.input?.focus) {
        state.input.focus({ preventScroll: true });
    }
}

function handleDaySelection(state: PickerState, button: Maybe<HTMLButtonElement>): void {
    if (!button || button.disabled) {
        return;
    }

    state.draft.year = toInt(button.dataset.ntDtpYear, state.draft.year);
    state.draft.month = toInt(button.dataset.ntDtpMonth, state.draft.month);
    state.draft.day = toInt(button.dataset.ntDtpDay, state.draft.day);
    state.viewYear = state.draft.year;
    state.viewMonth = state.draft.month;
    if (commitDraft(state) && state.mode === 'date') {
        closePicker(state);
        return;
    }

    renderPicker(state);
}

function handlePickerAction(state: PickerState, action: Maybe<string>): void {
    switch (action) {
        case 'prev-month':
        case 'next-month': {
            const shifted = shiftYearMonth(state.viewYear, state.viewMonth, action === 'next-month' ? 1 : -1);
            state.viewYear = shifted.year;
            state.viewMonth = shifted.month;
            state.dateView = 'calendar';
            break;
        }
        case 'show-months':
            state.dateView = 'month';
            break;
        case 'show-years':
            state.dateView = 'year';
            break;
        case 'prev-year':
            state.viewYear -= 1;
            break;
        case 'next-year':
            state.viewYear += 1;
            break;
        case 'set-am':
        case 'set-pm':
            state.meridiem = action === 'set-pm' ? 'pm' : 'am';
            state.draft.hour = toTwentyFourHour(toTwelveHour(state.draft.hour).hour, state.meridiem);
            commitDraft(state);
            break;
        case 'cancel':
            if (!isSubSelectionView(state)) {
                if (state.input.value !== state.openValue) {
                    setInputValue(state, state.openValue);
                }
                closePicker(state);
                return;
            }

            state.dateView = 'calendar';
            break;
        case 'confirm':
            if (commitDraft(state)) {
                closePicker(state);
                return;
            }

            break;
        default:
            return;
    }

    renderPicker(state);
}

function syncDraftFromTimeFields(state: PickerState): void {
    const { hourInput, minuteInput } = state.elements;

    if (!hourInput || !minuteInput) {
        return;
    }

    const fallbackHour = toTwelveHour(state.draft.hour).hour;
    const selectedMeridiem = state.meridiem === 'pm' ? 'pm' : 'am';
    const hour12 = clamp(toInt(hourInput.value, fallbackHour), 1, 12);
    state.draft.hour = toTwentyFourHour(hour12, selectedMeridiem);
    state.draft.minute = clamp(toInt(minuteInput.value, state.draft.minute), 0, 59);
    state.draft.second = 0;
    commitDraft(state);
    state.meridiem = toTwelveHour(state.draft.hour).meridiem;
    renderPicker(state);
}

function configureStateFromAttributes(state: PickerState): void {
    refreshPickerElements(state);

    state.label = state.input.closest('label') ?? state.input.labels?.[0] ?? state.label ?? null;
    state.mode = parseMode(state);
    suppressNativeInputPicker(state);
    state.openOnFocus = state.input?.dataset?.ntDtpOpenOnFocus !== 'false';
    setTrigger(state, findTrigger(state.input, state.picker));
    updateConstraints(state);
    updateDisabledSets(state);
    state.draft = clampDraftToConstraints(state, state.draft);

    if (state.picker) {
        const ariaHidden = state.isOpen ? 'false' : 'true';
        if (state.picker.getAttribute('aria-hidden') !== ariaHidden) {
            state.picker.setAttribute('aria-hidden', ariaHidden);
        }

        if (!state.isOpen) {
            if (state.picker.classList.contains('nt-dtp-open')) {
                state.picker.classList.remove('nt-dtp-open');
            }

            if (state.picker.style.visibility !== 'hidden') {
                state.picker.style.visibility = 'hidden';
            }
        }
    }

    const tabbableMarker = `${state.elements.dayButtons.length}:${state.elements.monthButtons.length}:${state.elements.yearList ? 'y' : 'n'}:${state.elements.hourInput ? 'h' : 'n'}:${state.elements.minuteInput ? 'm' : 'n'}:${state.elements.confirmButton ? 'c' : 'n'}:${state.elements.cancelButton ? 'x' : 'n'}`;
    if (!state.didMakePickerUntabbable || state.picker.dataset.ntDtpTabbableMarker !== tabbableMarker) {
        makePickerUntabbable(state);
        state.didMakePickerUntabbable = true;
        state.picker.dataset.ntDtpTabbableMarker = tabbableMarker;
    }

    if (state.isOpen) {
        renderPicker(state);
    }
}

function createPickerState(input: HTMLInputElement, picker: HTMLElement): PickerState {
    const defaultDraft = createDefaultDraft();
    const state: PickerState = {
        draft: cloneDraft(defaultDraft),
        elements: collectPickerElements(picker),
        disabledDateSet: new Set(),
        disabledTimeSet: new Set(),
        dateView: 'calendar',
        didMakePickerUntabbable: false,
        input,
        isOpen: false,
        isModal: false,
        label: input.closest('label') ?? input.labels?.[0] ?? null,
        lastRenderSignature: null,
        maxDraft: null,
        meridiem: 'am',
        minDraft: null,
        mode: 'none',
        nativeInputType: input.type,
        onInputCommit: () => { },
        onInputFocus: () => { },
        onInputInput: () => { },
        onInputKeyDown: () => { },
        onEnterKeyPress: () => { },
        onEnterKeyUp: () => { },
        onLabelClick: () => { },
        onLabelMouseDown: () => { },
        onPickerClick: () => { },
        onPickerInput: () => { },
        onPickerKeyDown: () => { },
        onYearListScroll: () => { },
        onTriggerClick: () => { },
        onTriggerMouseDown: () => { },
        openOnFocus: true,
        openValue: '',
        picker,
        suppressEnterKeySequence: false,
        trigger: null,
        viewMonth: defaultDraft.month,
        viewYear: defaultDraft.year,
        yearListEnd: null,
        yearListMaxYear: null,
        yearListMinYear: null,
        yearListSelectedYear: null,
        yearListStart: null,
    };

    state.onInputFocus = () => {
        if (shouldOpenFromInputInteraction(state)) {
            openPicker(state);
        }
    };

    state.onInputCommit = () => {
        const value = state.input.value.trim();
        const format = state.input.getAttribute('format');
        if (!format) {
            return;
        }

        const parsed = parseDraft(state.mode, value, format);
        if (!parsed) {
            setInputParseError(state, value ? `Enter a value in the format ${format}.` : null);
            return;
        }

        setInputParseError(state, null);

        const formattedValue = formatDraft(state.mode, parsed, format);
        if (formattedValue !== state.input.value) {
            state.input.value = formattedValue;
            state.input.dispatchEvent(new Event('input', { bubbles: true }));
            const inputId = state.input.id;
            setTimeout(() => {
                const input = state.input.isConnected ? state.input : document.getElementById(inputId);
                if (!isPickerInput(input)) {
                    return;
                }
                if (input.value !== value && input.value !== formattedValue) {
                    return;
                }

                input.value = formattedValue;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            });
        }
    };

    state.onLabelMouseDown = event => {
        const target = event.target instanceof Element ? event.target : null;
        const trigger = target?.closest('[data-nt-dtp-trigger="true"]');
        if (!trigger) {
            return;
        }

        event.preventDefault();
    };

    state.onLabelClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const trigger = target?.closest('[data-nt-dtp-trigger="true"]');
        if (!trigger) {
            return;
        }

        event.preventDefault();
        openPicker(state);
    };

    state.onTriggerMouseDown = event => {
        event.preventDefault();
    };

    state.onTriggerClick = event => {
        event.preventDefault();
        openPicker(state);
    };

    state.onInputInput = () => {
        if (trySyncDraftFromInputValue(state)) {
            if (state.isOpen) {
                renderPicker(state);
            }
        }
    };

    state.onInputKeyDown = event => {
        if (event.key === 'Escape' && state.isOpen) {
            event.preventDefault();
            closePicker(state);
            return;
        }

        if (event.key === 'Enter' && state.isOpen) {
            event.preventDefault();
            event.stopPropagation();
            state.suppressEnterKeySequence = true;
            if (state.input.value.trim() && !trySyncDraftFromInputValue(state)) {
                return;
            }
            handlePickerAction(state, 'confirm');
            return;
        }

        if (event.key === 'Tab' && state.isOpen) {
            closePicker(state);
            return;
        }

        if (event.key === 'ArrowDown' && event.altKey && shouldOpenFromInputInteraction(state)) {
            event.preventDefault();
            openPicker(state);
        }
    };

    state.onPickerClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const actionButton = target?.closest<HTMLElement>('[data-nt-dtp-action]');
        if (actionButton) {
            event.preventDefault();
            handlePickerAction(state, actionButton.dataset.ntDtpAction);
            return;
        }

        const dayButton = target?.closest<HTMLButtonElement>('[data-nt-dtp-day-index]');
        if (dayButton) {
            event.preventDefault();
            handleDaySelection(state, dayButton);
            return;
        }

        const monthButton = target?.closest<HTMLButtonElement>('[data-nt-dtp-month-index]');
        if (monthButton) {
            event.preventDefault();
            state.draft.year = toInt(monthButton.dataset.ntDtpYear, state.draft.year);
            state.draft.month = toInt(monthButton.dataset.ntDtpMonth, state.draft.month);
            state.viewYear = state.draft.year;
            state.viewMonth = state.draft.month;
            if (state.mode === 'month' && commitDraft(state)) {
                closePicker(state);
                return;
            }
            if (state.mode === 'date' || state.mode === 'datetime') {
                state.dateView = 'calendar';
            }
            renderPicker(state);
        }

        const yearButton = target?.closest<HTMLButtonElement>('[data-nt-dtp-year-option]');
        if (yearButton) {
            event.preventDefault();
            if (yearButton.disabled) {
                return;
            }

            state.draft.year = toInt(yearButton.dataset.ntDtpYearOption, state.draft.year);
            state.viewYear = state.draft.year;
            state.dateView = 'calendar';
            renderPicker(state);
        }
    };

    state.onPickerInput = event => {
        const target = event.target instanceof Element ? event.target : null;
        if (target?.matches('[data-nt-dtp-hour], [data-nt-dtp-minute]')) {
            syncDraftFromTimeFields(state);
        }
    };

    state.onPickerKeyDown = event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closePicker(state, true);
            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            event.stopPropagation();
            state.suppressEnterKeySequence = true;
            handlePickerAction(state, 'confirm');
            return;
        }

        if (event.key === 'Tab') {
            closePicker(state);
        }
    };

    state.onYearListScroll = event => {
        const target = event.target instanceof HTMLElement ? event.target : null;
        if (target?.matches('[data-nt-dtp-year-list]')) {
            handleYearListScroll(state, target);
        }
    };

    state.onEnterKeyPress = event => {
        if (event.key === 'Enter' && state.suppressEnterKeySequence) {
            event.preventDefault();
            event.stopPropagation();
        }
    };

    state.onEnterKeyUp = event => {
        if (event.key === 'Enter' && state.suppressEnterKeySequence) {
            event.preventDefault();
            event.stopPropagation();
            state.suppressEnterKeySequence = false;
        }
    };

    input.addEventListener('blur', state.onInputCommit);
    input.addEventListener('change', state.onInputCommit);
    input.addEventListener('focus', state.onInputFocus);
    input.addEventListener('input', state.onInputInput);
    input.addEventListener('keydown', state.onInputKeyDown);
    input.addEventListener('keypress', state.onEnterKeyPress);
    input.addEventListener('keyup', state.onEnterKeyUp);
    state.label?.addEventListener('mousedown', state.onLabelMouseDown);
    state.label?.addEventListener('click', state.onLabelClick);
    setTrigger(state, findTrigger(input, picker));
    picker.addEventListener('click', state.onPickerClick);
    picker.addEventListener('input', state.onPickerInput);
    picker.addEventListener('keydown', state.onPickerKeyDown);
    picker.addEventListener('keypress', state.onEnterKeyPress);
    picker.addEventListener('keyup', state.onEnterKeyUp);
    state.elements.yearList?.addEventListener('scroll', state.onYearListScroll);

    configureStateFromAttributes(state);
    return state;
}

function cleanupPickerState(state: Maybe<PickerState>): void {
    if (!state) {
        return;
    }

    closePicker(state);
    setInputParseError(state, null);

    state.input?.removeEventListener('blur', state.onInputCommit);
    state.input?.removeEventListener('change', state.onInputCommit);
    state.input?.removeEventListener('focus', state.onInputFocus);
    state.input?.removeEventListener('input', state.onInputInput);
    state.input?.removeEventListener('keydown', state.onInputKeyDown);
    state.input?.removeEventListener('keypress', state.onEnterKeyPress);
    state.input?.removeEventListener('keyup', state.onEnterKeyUp);
    state.label?.removeEventListener('mousedown', state.onLabelMouseDown);
    state.label?.removeEventListener('click', state.onLabelClick);
    setTrigger(state, null);
    state.picker?.removeEventListener('click', state.onPickerClick);
    state.picker?.removeEventListener('input', state.onPickerInput);
    state.picker?.removeEventListener('keydown', state.onPickerKeyDown);
    state.picker?.removeEventListener('keypress', state.onEnterKeyPress);
    state.picker?.removeEventListener('keyup', state.onEnterKeyUp);
    state.elements.yearList?.removeEventListener('scroll', state.onYearListScroll);
    restoreNativeInputPicker(state);
}

function shouldAutoOpenForFocusedInput(state: Maybe<PickerState>): boolean {
    if (!state?.input || state.isOpen || !shouldOpenFromInputInteraction(state)) {
        return false;
    }

    if (state.input.disabled || state.input.readOnly) {
        return false;
    }

    return document.activeElement === state.input;
}

function isPickerInput(element: Maybe<Element>): element is HTMLInputElement {
    return element instanceof HTMLInputElement && element.matches(PICKER_INPUT_SELECTOR);
}

function isNativePickerInput(element: Maybe<Element>): element is HTMLInputElement {
    return element instanceof HTMLInputElement && element.matches(NATIVE_PICKER_INPUT_SELECTOR);
}

function getLifecycleScope(element: Maybe<Element>): Element | Document | null {
    if (!element) {
        return null;
    }

    if (isPickerInput(element) || isNativePickerInput(element) || element.matches('.nt-input-date-time, [data-nt-dtp-picker="true"], [data-nt-dtp-trigger="true"], [data-nt-dtp-native-trigger="true"]')) {
        return element;
    }

    const nextElement = element.nextElementSibling;
    if (nextElement?.matches?.('.nt-input-date-time')) {
        return nextElement;
    }

    return element.closest?.('.nt-input-date-time') ?? element;
}

function queryPickerInputs(scope: Maybe<Element | Document>): HTMLInputElement[] {
    if (isPickerInput(scope as Element)) {
        return [scope as HTMLInputElement];
    }

    const root = scope ?? document;
    return Array.from(root.querySelectorAll<HTMLInputElement>(PICKER_INPUT_SELECTOR));
}

function queryNativePickerInputs(scope: Maybe<Element | Document>): HTMLInputElement[] {
    if (isNativePickerInput(scope as Element)) {
        return [scope as HTMLInputElement];
    }

    const root = scope ?? document;
    return Array.from(root.querySelectorAll<HTMLInputElement>(NATIVE_PICKER_INPUT_SELECTOR));
}

function cleanupNativePickerState(state: Maybe<NativePickerState>): void {
    if (!state) {
        return;
    }

    state.trigger.removeEventListener('mousedown', state.onTriggerMouseDown);
    state.trigger.removeEventListener('click', state.onTriggerClick);
    delete state.root.dataset.ntDtpNativeEnhanced;
}

function showNativePicker(input: HTMLInputElement): void {
    if (input.disabled || input.readOnly) {
        return;
    }

    try {
        input.showPicker();
    }
    catch {
        // The native field remains usable when showPicker is unavailable or browser policy blocks it.
    }
}

function syncNativePickerInput(input: HTMLInputElement): void {
    const root = input.closest<HTMLElement>('.nt-input-date-time');
    const trigger = root?.querySelector<HTMLButtonElement>('[data-nt-dtp-native-trigger="true"]') ?? null;
    const existing = nativePickerStateByInput.get(input);
    if (existing?.root === root && existing.trigger === trigger) {
        return;
    }

    cleanupNativePickerState(existing);
    nativePickerStateByInput.delete(input);
    if (!root || !trigger) {
        return;
    }

    const state: NativePickerState = {
        input,
        onTriggerClick: () => {
            if (input.disabled || input.readOnly) {
                return;
            }

            if (document.activeElement !== input) {
                input.focus({ preventScroll: true });
            }

            showNativePicker(input);
        },
        onTriggerMouseDown: event => event.preventDefault(),
        root,
        trigger,
    };

    trigger.addEventListener('mousedown', state.onTriggerMouseDown);
    trigger.addEventListener('click', state.onTriggerClick);
    root.dataset.ntDtpNativeEnhanced = 'true';
    nativePickerStateByInput.set(input, state);
}

function synchronizeNativePickers(scope: Maybe<Element | Document> = document): void {
    for (const [input, state] of nativePickerStateByInput) {
        if (!input.isConnected || !state.root.isConnected || !state.trigger.isConnected) {
            cleanupNativePickerState(state);
            nativePickerStateByInput.delete(input);
        }
    }

    for (const input of queryNativePickerInputs(scope)) {
        syncNativePickerInput(input);
    }
}

function disposeNativePickerStatesForElement(element: Maybe<Element>): void {
    if (!element) {
        for (const state of nativePickerStateByInput.values()) {
            cleanupNativePickerState(state);
        }
        nativePickerStateByInput.clear();
        return;
    }

    for (const input of queryNativePickerInputs(element)) {
        cleanupNativePickerState(nativePickerStateByInput.get(input));
        nativePickerStateByInput.delete(input);
    }
}

function findPickerForInput(input: HTMLInputElement): HTMLElement | null {
    const targetId = input.dataset.ntDtpTarget;
    if (!targetId) {
        return null;
    }

    const scopedRoot = input.closest<HTMLElement>('.nt-input-date-time');
    const scopedPicker = scopedRoot
        ? Array.from(scopedRoot.querySelectorAll<HTMLElement>('[data-nt-dtp-picker="true"]')).find(candidate => candidate.id === targetId) ?? null
        : null;

    return scopedPicker ?? document.getElementById(targetId);
}

function syncPickerInput(input: HTMLInputElement): void {
    const existing = pickerStateByInput.get(input);
    if (existing) {
        configureStateFromAttributes(existing);
        if (shouldAutoOpenForFocusedInput(existing)) {
            openPicker(existing);
        }
        return;
    }

    const picker = findPickerForInput(input);
    if (!picker) {
        return;
    }

    const state = createPickerState(input, picker);
    pickerStateByInput.set(input, state);
    if (shouldAutoOpenForFocusedInput(state)) {
        openPicker(state);
    }
}

function cleanupDisconnectedPickerStates(): void {
    for (const [input, state] of pickerStateByInput) {
        if (!input.isConnected || !state.picker?.isConnected) {
            cleanupPickerState(state);
            pickerStateByInput.delete(input);
        }
    }
}

function synchronizePickers(scope: Maybe<Element | Document> = document): void {
    cleanupDisconnectedPickerStates();

    const inputs = queryPickerInputs(scope);
    for (const input of inputs) {
        syncPickerInput(input);
    }

    if (activePickerState && (!activePickerState.input?.isConnected || !activePickerState.picker?.isConnected)) {
        closePicker(activePickerState);
        activePickerState = null;
    }

    if (pickerStateByInput.size > 0) {
        attachGlobalHandlers();
    }
    else {
        detachGlobalHandlers();
    }
}

function synchronizePickerForElement(element: Maybe<Element>): void {
    cleanupDisconnectedPickerStates();

    if (!element) {
        cleanupGlobalResourcesIfIdle();
        return;
    }

    const inputs = queryPickerInputs(element);
    if (inputs.length > 0) {
        for (const input of inputs) {
            syncPickerInput(input);
        }
    }
    else {
        const state = getStateForElement(element);
        if (state) {
            configureStateFromAttributes(state);
        }
    }

    if (pickerStateByInput.size > 0) {
        attachGlobalHandlers();
    }
    else {
        detachGlobalHandlers();
    }
}

function onDocumentMouseDown(event: MouseEvent): void {
    pointerDownInsideActivePicker = isTargetInsideState(activePickerState, event.target);
}

function onDocumentClick(event: MouseEvent): void {
    if (!activePickerState) {
        return;
    }

    const clickedInside = isTargetInsideState(activePickerState, event.target);
    if (!pointerDownInsideActivePicker && !clickedInside) {
        closePicker(activePickerState);
    }

    pointerDownInsideActivePicker = false;
}

function onDocumentFocusIn(event: FocusEvent): void {
    if (!activePickerState) {
        return;
    }

    if (!isTargetInsideState(activePickerState, event.target)) {
        closePicker(activePickerState);
    }
}

function onWindowLayoutChange(): void {
    if (activePickerState?.isOpen && activePickerState.isModal && shouldUseModalLayout(activePickerState)) {
        return;
    }

    schedulePositionUpdate();
}

function onDocumentKeyDown(event: KeyboardEvent): void {
    if (!activePickerState) {
        return;
    }

    if (event.key === 'Escape') {
        closePicker(activePickerState, true);
    }
}

function attachGlobalHandlers(): void {
    if (globalHandlersAttached) {
        return;
    }

    document.addEventListener('mousedown', onDocumentMouseDown);
    document.addEventListener('click', onDocumentClick);
    document.addEventListener('focusin', onDocumentFocusIn);
    document.addEventListener('keydown', onDocumentKeyDown);
    window.addEventListener('resize', onWindowLayoutChange, { passive: true });
    window.addEventListener('scroll', onWindowLayoutChange, true);
    globalHandlersAttached = true;
}

function detachGlobalHandlers(): void {
    if (!globalHandlersAttached) {
        return;
    }

    document.removeEventListener('mousedown', onDocumentMouseDown);
    document.removeEventListener('click', onDocumentClick);
    document.removeEventListener('focusin', onDocumentFocusIn);
    document.removeEventListener('keydown', onDocumentKeyDown);
    window.removeEventListener('resize', onWindowLayoutChange);
    window.removeEventListener('scroll', onWindowLayoutChange, true);
    globalHandlersAttached = false;
}

function cleanupGlobalResourcesIfIdle(): void {
    if (pickerStateByInput.size > 0) {
        return;
    }

    activePickerState = null;
    pointerDownInsideActivePicker = false;

    if (pendingFrame !== null) {
        cancelAnimationFrame(pendingFrame);
        pendingFrame = null;
    }

    detachGlobalHandlers();
}

function disposeAll(): void {
    for (const state of pickerStateByInput.values()) {
        cleanupPickerState(state);
    }

    pickerStateByInput.clear();
    disposeNativePickerStatesForElement(null);
    cleanupGlobalResourcesIfIdle();
}

function getStateForElement(element: Maybe<Element>): PickerState | null {
    if (!element) {
        return null;
    }

    if (element instanceof HTMLInputElement && pickerStateByInput.has(element)) {
        return pickerStateByInput.get(element) ?? null;
    }

    for (const state of pickerStateByInput.values()) {
        if (state.picker === element || state.picker?.contains?.(element)) {
            return state;
        }
    }

    return null;
}

function disposeStateForElement(element: Maybe<Element>): void {
    if (element) {
        const inputs = queryPickerInputs(element);
        if (inputs.length > 0) {
            for (const input of inputs) {
                const state = pickerStateByInput.get(input);
                cleanupPickerState(state);
                pickerStateByInput.delete(input);
            }

            cleanupGlobalResourcesIfIdle();
            return;
        }
    }

    const state = getStateForElement(element);
    if (!state) {
        cleanupDisconnectedPickerStates();
        cleanupGlobalResourcesIfIdle();
        return;
    }

    cleanupPickerState(state);
    if (state.input) {
        pickerStateByInput.delete(state.input);
    }

    cleanupGlobalResourcesIfIdle();
}

export function onLoad(element: Maybe<Element>, dotNetRef: unknown): void {
    const scope = getLifecycleScope(element);
    if (scope) {
        if (element && scope instanceof Element && element !== scope) {
            lifecycleScopeByPageScript.set(element, scope);
        }

        synchronizePickers(scope);
        synchronizeNativePickers(scope);
    }
}

export function onUpdate(element: Maybe<Element>, dotNetRef: unknown): void {
    const scope = getLifecycleScope(element);
    if (element && scope instanceof Element && element !== scope) {
        lifecycleScopeByPageScript.set(element, scope);
    }

    synchronizePickerForElement(scope instanceof Element ? scope : null);
    synchronizeNativePickers(scope instanceof Element ? scope : null);
}

export function onDispose(element: Maybe<Element>, dotNetRef: unknown): void {
    if (!element) {
        disposeAll();
        return;
    }

    const scope = lifecycleScopeByPageScript.get(element) ?? getLifecycleScope(element);
    disposeStateForElement(scope instanceof Element ? scope : element);
    disposeNativePickerStatesForElement(scope instanceof Element ? scope : element);
}
