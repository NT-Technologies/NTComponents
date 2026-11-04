/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { initialActive, moveFocus, onDispose, onLoad, onUpdate, pickDate } from '../NTInputDateRange.razor.js';

const short = iso => new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' }).format(Date.parse(iso));

describe('NTInputDateRange selection logic', () => {
    it.each([
        [null, null, 'start'],
        ['2026-11-10', null, 'end'],
        ['2026-11-10', '2026-11-14', 'start'],
        [null, '2026-11-14', 'start'],
    ])('opens with start=%s end=%s on %s', (start, end, expected) => {
        expect(initialActive(start, end)).toBe(expected);
    });

    it.each([
        ['start, no end', { start: null, end: null, active: 'start' }, '2026-11-10', { start: '2026-11-10', end: null, active: 'end', close: false }],
        ['start keeps a later end', { start: '2026-11-01', end: '2026-11-20', active: 'start' }, '2026-11-10', { start: '2026-11-10', end: '2026-11-20', active: 'end', close: false }],
        ['start keeps an end on the same day', { start: '2026-11-01', end: '2026-11-10', active: 'start' }, '2026-11-10', { start: '2026-11-10', end: '2026-11-10', active: 'end', close: false }],
        ['start clears an earlier end', { start: '2026-11-01', end: '2026-11-05', active: 'start' }, '2026-11-10', { start: '2026-11-10', end: null, active: 'end', close: false }],
        ['end with no start', { start: null, end: null, active: 'end' }, '2026-11-10', { start: '2026-11-10', end: null, active: 'end', close: false }],
        ['end before start restarts', { start: '2026-11-10', end: '2026-11-20', active: 'end' }, '2026-11-05', { start: '2026-11-05', end: null, active: 'end', close: false }],
        ['end after start closes', { start: '2026-11-10', end: null, active: 'end' }, '2026-11-14', { start: '2026-11-10', end: '2026-11-14', active: 'end', close: true }],
        ['end on the start day closes', { start: '2026-11-10', end: null, active: 'end' }, '2026-11-10', { start: '2026-11-10', end: '2026-11-10', active: 'end', close: true }],
    ])('pick: %s', (_name, selection, date, expected) => {
        expect(pickDate(selection, date)).toEqual(expected);
    });

    it.each([
        ['ArrowLeft', '2026-11-03'],
        ['ArrowRight', '2026-11-05'],
        ['ArrowUp', '2026-10-28'],
        ['ArrowDown', '2026-11-11'],
        ['PageUp', '2026-10-04'],
        ['PageDown', '2026-12-04'],
        ['Home', '2026-11-01'],
        ['End', '2026-11-07'],
        ['a', null],
    ])('moves focus from a Wednesday with %s', (key, expected) => {
        expect(moveFocus('2026-11-04', key, 0)).toBe(expected);
    });

    it('clamps the day when paging into a shorter month and honors the first day of week', () => {
        expect(moveFocus('2026-01-31', 'PageDown', 0)).toBe('2026-02-28');
        expect(moveFocus('2026-11-04', 'Home', 1)).toBe('2026-11-02');
        expect(moveFocus('2026-11-04', 'End', 1)).toBe('2026-11-08');
    });
});

describe('NTInputDateRange popover behavior', () => {
    let root;
    let events;

    const input = () => root.querySelector('[data-nt-input-date-range-input]');
    const startInput = () => root.querySelector('[data-nt-input-date-range-start]');
    const endInput = () => root.querySelector('[data-nt-input-date-range-end]');
    const popover = () => root.querySelector('[data-nt-input-date-range-popover]');
    const day = iso => root.querySelector(`button[data-date="${iso}"]`);
    const cell = iso => day(iso).closest('.day-cell');
    const segment = side => root.querySelector(`[data-nt-input-date-range-segment="${side}"]`);
    const action = name => root.querySelector(`[data-nt-input-date-range-action="${name}"]`);

    function mount({ start = '', end = '', min, max, readOnly, disabled, firstDay = 0 } = {}) {
        document.body.innerHTML = `
            <div data-nt-input-date-range="true" data-first-day="${firstDay}"${min ? ` data-min="${min}"` : ''}${max ? ` data-max="${max}"` : ''}${readOnly ? ' data-readonly="true"' : ''}>
                <input type="text" readonly aria-expanded="false" data-nt-input-date-range-input="true"${disabled ? ' disabled' : ''} />
                <input type="hidden" value="${start}" data-nt-input-date-range-start="true" />
                <input type="hidden" value="${end}" data-nt-input-date-range-end="true" />
                <div popover="auto" data-nt-input-date-range-popover="true">
                    <button type="button" data-nt-input-date-range-segment="start"><span data-nt-input-date-range-segment-value="start"></span></button>
                    <button type="button" data-nt-input-date-range-segment="end"><span data-nt-input-date-range-segment-value="end"></span></button>
                    <button type="button" data-nt-input-date-range-action="prev">prev</button>
                    <div data-nt-input-date-range-month="true"></div>
                    <button type="button" data-nt-input-date-range-action="next">next</button>
                    <div role="grid" data-nt-input-date-range-grid="true"></div>
                    <button type="button" data-nt-input-date-range-action="clear">Clear</button>
                    <button type="button" data-nt-input-date-range-action="done">Done</button>
                </div>
                <tnt-page-script></tnt-page-script>
            </div>`;
        root = document.querySelector('[data-nt-input-date-range]');
        popover().showPopover = jest.fn();
        popover().hidePopover = jest.fn();
        popover().matches = jest.fn(selector => selector === ':popover-open');
        events = [];
        for (const [name, element] of [['start', startInput()], ['end', endInput()]]) {
            element.addEventListener('input', () => events.push(`input:${name}:${element.value}`));
            element.addEventListener('change', () => events.push(`change:${name}:${element.value}`));
        }

        onLoad(root.querySelector('tnt-page-script'));
    }

    function key(target, name) {
        const event = new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true });
        target.dispatchEvent(event);
        return event;
    }

    const hover = iso => day(iso).dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));

    beforeEach(() => {
        jest.useFakeTimers({ now: new Date(2026, 10, 4, 12) });
    });

    afterEach(() => {
        if (root) {
            onDispose(root.querySelector('tnt-page-script') ?? root);
        }

        document.body.innerHTML = '';
        root = null;
        jest.useRealTimers();
    });

    it('opens on click with start active, today focused, and a roving tabindex', () => {
        mount();

        input().click();

        expect(popover().showPopover).toHaveBeenCalledTimes(1);
        expect(root.dataset.open).toBe('true');
        expect(input().getAttribute('aria-expanded')).toBe('true');
        expect(segment('start').getAttribute('aria-pressed')).toBe('true');
        expect(root.querySelector('[data-nt-input-date-range-month]').textContent).toBe(new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(Date.parse('2026-11-01')));
        expect(root.querySelectorAll('.day-cell')).toHaveLength(42);
        expect(document.activeElement).toBe(day('2026-11-04'));
        expect(day('2026-11-04').tabIndex).toBe(0);
        expect(root.querySelectorAll('button.day[tabindex="0"]')).toHaveLength(1);
        expect(day('2026-11-04').getAttribute('aria-label')).toBe(new Intl.DateTimeFormat(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' }).format(Date.parse('2026-11-04')));
        expect(day('2026-11-04').classList.contains('today')).toBe(true);
    });

    it('opens on end when only a start exists and focuses the start day', () => {
        mount({ start: '2026-09-10' });

        input().click();

        expect(segment('end').getAttribute('aria-pressed')).toBe('true');
        expect(document.activeElement).toBe(day('2026-09-10'));
    });

    it('opens on start when both values exist and focuses the start day', () => {
        mount({ start: '2026-11-10', end: '2026-11-14' });

        input().click();

        expect(segment('start').getAttribute('aria-pressed')).toBe('true');
        expect(document.activeElement).toBe(day('2026-11-10'));
        expect(day('2026-11-10').classList.contains('selected')).toBe(true);
        expect(day('2026-11-14').classList.contains('selected')).toBe(true);
        expect(cell('2026-11-12').classList.contains('in-range')).toBe(true);
        expect(cell('2026-11-10').classList.contains('band-start')).toBe(true);
        expect(cell('2026-11-14').classList.contains('band-end')).toBe(true);
        expect(cell('2026-11-09').classList.contains('in-range')).toBe(false);
    });

    it('writes each pick, dispatches events, updates the display, and closes after the end pick', () => {
        mount();
        input().click();

        day('2026-11-10').click();

        expect(startInput().value).toBe('2026-11-10');
        expect(endInput().value).toBe('');
        expect(events).toEqual(['input:start:2026-11-10', 'change:start:2026-11-10']);
        expect(input().value).toBe(`${short('2026-11-10')} –`);
        expect(segment('end').getAttribute('aria-pressed')).toBe('true');
        expect(root.dataset.open).toBe('true');

        events.length = 0;
        day('2026-11-14').click();

        expect(endInput().value).toBe('2026-11-14');
        expect(events).toEqual(['input:end:2026-11-14', 'change:end:2026-11-14']);
        expect(input().value).toBe(`${short('2026-11-10')} – ${short('2026-11-14')}`);
        expect(popover().hidePopover).toHaveBeenCalledTimes(1);
        expect(root.dataset.open).toBeUndefined();
        expect(document.activeElement).toBe(input());
    });

    it('does not open when the field only receives focus', () => {
        mount();

        input().focus();

        expect(root.dataset.open).toBeUndefined();
        expect(popover().showPopover).not.toHaveBeenCalled();
    });

    it('does not reopen when focus returns to the field after closing', () => {
        mount({ start: '2026-11-10' });
        input().click();
        day('2026-11-14').click();

        expect(popover().showPopover).toHaveBeenCalledTimes(1);
    });

    it('restarts the range and clears a stale end when picking before the start', () => {
        mount({ start: '2026-11-10', end: '2026-11-20' });
        input().click();
        segment('end').click();

        day('2026-11-05').click();

        expect(startInput().value).toBe('2026-11-05');
        expect(endInput().value).toBe('');
        expect(events).toEqual(['input:start:2026-11-05', 'change:start:2026-11-05', 'input:end:', 'change:end:']);
        expect(root.dataset.open).toBe('true');
        expect(segment('end').getAttribute('aria-pressed')).toBe('true');
    });

    it('allows a same-day range', () => {
        mount();
        input().click();

        day('2026-11-10').click();
        day('2026-11-10').click();

        expect(startInput().value).toBe('2026-11-10');
        expect(endInput().value).toBe('2026-11-10');
        expect(root.dataset.open).toBeUndefined();
    });

    it('previews the band up to the hovered day while the end is being picked', () => {
        mount({ start: '2026-11-10' });
        input().click();

        hover('2026-11-14');

        for (const iso of ['2026-11-11', '2026-11-12', '2026-11-13']) {
            expect(cell(iso).classList.contains('in-range')).toBe(true);
            expect(cell(iso).classList.contains('preview')).toBe(true);
        }

        expect(cell('2026-11-10').classList.contains('band-start')).toBe(true);
        expect(cell('2026-11-14').classList.contains('band-end')).toBe(true);
        expect(day('2026-11-14').classList.contains('selected')).toBe(false);

        hover('2026-11-08');
        expect(root.querySelectorAll('.day-cell.in-range')).toHaveLength(0);

        hover('2026-11-12');
        root.querySelector('[role="grid"]').dispatchEvent(new MouseEvent('mouseleave'));
        expect(root.querySelectorAll('.day-cell.in-range')).toHaveLength(0);
        expect(root.querySelectorAll('.day-cell.preview')).toHaveLength(0);
    });

    it('does not preview while the start is being picked', () => {
        mount({ start: '2026-11-10', end: '2026-11-14' });
        input().click();

        hover('2026-11-20');

        expect(cell('2026-11-12').classList.contains('in-range')).toBe(true);
        expect(cell('2026-11-18').classList.contains('in-range')).toBe(false);
    });

    it('previews from the start over an existing end while re-picking the end', () => {
        mount({ start: '2026-11-10', end: '2026-11-14' });
        input().click();
        segment('end').click();

        hover('2026-11-18');

        expect(cell('2026-11-16').classList.contains('in-range')).toBe(true);
        expect(cell('2026-11-18').classList.contains('band-end')).toBe(true);
    });

    it('disables days outside Min and Max and ignores clicks on them', () => {
        mount({ min: '2026-11-05', max: '2026-11-20' });
        input().click();

        expect(day('2026-11-04').disabled).toBe(true);
        expect(day('2026-11-21').disabled).toBe(true);
        expect(day('2026-11-05').disabled).toBe(false);
        expect(day('2026-11-20').disabled).toBe(false);
        expect(document.activeElement).toBe(day('2026-11-05'));

        day('2026-11-04').click();
        expect(startInput().value).toBe('');
        expect(events).toEqual([]);
    });

    it('clamps keyboard focus into Min and Max', () => {
        mount({ min: '2026-11-03', max: '2026-11-20' });
        input().click();
        key(day('2026-11-04'), 'ArrowLeft');
        key(document.activeElement, 'ArrowLeft');

        expect(document.activeElement).toBe(day('2026-11-03'));
    });

    it('clears both values without closing and returns to picking the start', () => {
        mount({ start: '2026-11-10', end: '2026-11-14' });
        input().click();

        action('clear').click();

        expect(startInput().value).toBe('');
        expect(endInput().value).toBe('');
        expect(events).toEqual(['input:start:', 'change:start:', 'input:end:', 'change:end:']);
        expect(input().value).toBe('');
        expect(root.dataset.open).toBe('true');
        expect(segment('start').getAttribute('aria-pressed')).toBe('true');
        expect(root.querySelectorAll('.day-cell.in-range')).toHaveLength(0);
    });

    it('closes from Done without changing values', () => {
        mount({ start: '2026-11-10' });
        input().click();

        action('done').click();

        expect(root.dataset.open).toBeUndefined();
        expect(events).toEqual([]);
        expect(document.activeElement).toBe(input());
    });

    it('makes a clicked segment active', () => {
        mount({ start: '2026-11-10', end: '2026-11-14' });
        input().click();

        segment('end').click();

        expect(segment('end').getAttribute('aria-pressed')).toBe('true');
        expect(segment('start').getAttribute('aria-pressed')).toBe('false');
        expect(root.querySelector('[data-nt-input-date-range-segment-value="start"]').textContent).toBe(short('2026-11-10'));
        expect(root.querySelector('[data-nt-input-date-range-segment-value="end"]').textContent).toBe(short('2026-11-14'));
    });

    it('closes on Escape and returns focus to the field without reopening', () => {
        mount();
        input().click();

        const event = key(document.activeElement, 'Escape');

        expect(event.defaultPrevented).toBe(true);
        expect(popover().hidePopover).toHaveBeenCalledTimes(1);
        expect(root.dataset.open).toBeUndefined();
        expect(input().getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(input());
        expect(popover().showPopover).toHaveBeenCalledTimes(1);
    });

    it('treats light dismiss as a close without stealing focus', () => {
        mount();
        input().click();
        const outside = document.createElement('button');
        document.body.append(outside);
        outside.focus();

        popover().dispatchEvent(Object.assign(new Event('toggle'), { newState: 'closed' }));

        expect(root.dataset.open).toBeUndefined();
        expect(document.activeElement).toBe(outside);
    });

    it('moves focus with the arrow, page, and week keys across months', () => {
        mount();
        input().click();

        key(document.activeElement, 'ArrowRight');
        expect(document.activeElement).toBe(day('2026-11-05'));
        key(document.activeElement, 'ArrowDown');
        expect(document.activeElement).toBe(day('2026-11-12'));
        key(document.activeElement, 'Home');
        expect(document.activeElement).toBe(day('2026-11-08'));
        key(document.activeElement, 'End');
        expect(document.activeElement).toBe(day('2026-11-14'));
        key(document.activeElement, 'PageDown');
        expect(document.activeElement).toBe(day('2026-12-14'));
        expect(root.querySelector('[data-nt-input-date-range-month]').textContent).toContain('2026');
        expect(root.querySelectorAll('button.day[tabindex="0"]')).toHaveLength(1);
        key(document.activeElement, 'PageUp');
        key(document.activeElement, 'PageUp');
        expect(document.activeElement).toBe(day('2026-10-14'));
    });

    it('picks the focused day with Enter and Space', () => {
        mount();
        input().click();

        key(document.activeElement, 'ArrowRight');
        const enter = key(document.activeElement, 'Enter');
        key(document.activeElement, 'ArrowRight');
        key(document.activeElement, ' ');

        expect(enter.defaultPrevented).toBe(true);
        expect(startInput().value).toBe('2026-11-05');
        expect(endInput().value).toBe('2026-11-06');
    });

    it('navigates months from the header buttons', () => {
        mount();
        input().click();

        action('next').click();

        expect(day('2026-12-04').tabIndex).toBe(0);
        expect(root.querySelector('[data-nt-input-date-range-month]').textContent).toBe(new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(Date.parse('2026-12-01')));

        action('prev').click();
        action('prev').click();
        expect(day('2026-10-04').tabIndex).toBe(0);
    });

    it('picks an outside-month day and switches to its month', () => {
        mount();
        input().click();
        expect(day('2026-12-01').classList.contains('outside')).toBe(true);

        day('2026-12-01').click();

        expect(startInput().value).toBe('2026-12-01');
        expect(root.querySelector('[data-nt-input-date-range-month]').textContent).toContain('December');
    });

    it('does not open when disabled or read-only', () => {
        mount({ disabled: true });
        input().click();
        expect(root.dataset.open).toBeUndefined();

        mount({ readOnly: true });
        input().click();
        input().focus();
        expect(root.dataset.open).toBeUndefined();
        expect(popover().showPopover).not.toHaveBeenCalled();
    });

    it('opens from Enter and ArrowDown on the field', () => {
        mount();

        key(input(), 'ArrowDown');

        expect(root.dataset.open).toBe('true');
    });

    it('is idempotent across updates and stops reacting after dispose', () => {
        mount();
        onUpdate(root.querySelector('tnt-page-script'));
        onLoad(root.querySelector('tnt-page-script'));
        input().click();
        expect(popover().showPopover).toHaveBeenCalledTimes(1);
        action('done').click();

        onDispose(root.querySelector('tnt-page-script'));
        input().click();

        expect(popover().showPopover).toHaveBeenCalledTimes(1);
    });

    it('cleans up instances whose root was removed from the document', () => {
        mount();
        const detached = root;
        document.body.innerHTML = '';

        expect(() => onLoad(null)).not.toThrow();
        detached.querySelector('[data-nt-input-date-range-input]').click();
        expect(popover().showPopover).not.toHaveBeenCalled();
    });
});
