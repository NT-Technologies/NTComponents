/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../NTInputDateTime.razor.js';

describe('NTInputDateTime picker behavior', () => {
    const fixedNow = new Date(2026, 2, 15, 14, 5, 9);
    let idCounter = 0;

    function completeRect(overrides = {}) {
        const rect = {
            width: 240,
            height: 40,
            top: 100,
            left: 100,
            right: 340,
            bottom: 140,
            x: 100,
            y: 100,
            ...overrides,
            toJSON: () => { },
        };

        if (overrides.left !== undefined && overrides.x === undefined) {
            rect.x = rect.left;
        }
        if (overrides.top !== undefined && overrides.y === undefined) {
            rect.y = rect.top;
        }
        if ((overrides.left !== undefined || overrides.width !== undefined) && overrides.right === undefined) {
            rect.right = rect.left + rect.width;
        }
        if ((overrides.top !== undefined || overrides.height !== undefined) && overrides.bottom === undefined) {
            rect.bottom = rect.top + rect.height;
        }

        return rect;
    }

    function setRect(element, rect) {
        jest.spyOn(element, 'getBoundingClientRect').mockReturnValue(completeRect(rect));
    }

    function createPickerFixture({
        disabledDates = null,
        disabledTimes = null,
        lazy = false,
        max = null,
        min = null,
        mode = 'date',
        openOnFocus = false,
        format = null,
        value = '',
    } = {}) {
        const suffix = ++idCounter;
        const pickerId = `nt-dtp-test-${suffix}`;

        const input = document.createElement('input');
        input.type = mode === 'datetime' ? 'datetime-local' : mode;
        input.value = value;
        input.dataset.ntDtpInput = 'true';
        input.dataset.ntDtpTarget = pickerId;
        input.dataset.ntDtpMode = mode;
        input.dataset.ntDtpOpenOnFocus = openOnFocus ? 'true' : 'false';

        if (format) {
            input.setAttribute('format', format);
            input.setAttribute('value', value);
        }

        const label = document.createElement('label');
        label.appendChild(input);

        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.dataset.ntDtpTrigger = 'true';
        trigger.dataset.ntDtpTarget = pickerId;

        if (min) {
            input.min = min;
        }
        if (max) {
            input.max = max;
        }
        if (disabledDates) {
            input.dataset.ntDtpDisabledDates = disabledDates;
        }
        if (disabledTimes) {
            input.dataset.ntDtpDisabledTimes = disabledTimes;
        }

        const picker = document.createElement('div');
        picker.id = pickerId;
        picker.tabIndex = -1;
        picker.dataset.ntDtpPicker = 'true';
        picker.dataset.ntDtpMode = mode;
        const rendersDatePanel = mode === 'date' || mode === 'datetime';
        const rendersMonthPanel = mode === 'date' || mode === 'datetime' || mode === 'month';
        const rendersYearPanel = mode === 'date' || mode === 'datetime';
        const rendersTimePanel = mode === 'datetime' || mode === 'time';

        const headline = document.createElement('div');
        headline.dataset.ntDtpHeadline = '';
        picker.appendChild(headline);

        const subhead = document.createElement('div');
        subhead.dataset.ntDtpSubhead = '';
        picker.appendChild(subhead);

        const datePanel = document.createElement('div');
        datePanel.dataset.ntDtpDatePanel = '';
        const monthLabel = document.createElement('div');
        monthLabel.dataset.ntDtpMonthLabel = '';
        datePanel.appendChild(monthLabel);
        const calendarYearLabel = document.createElement('div');
        calendarYearLabel.dataset.ntDtpCalendarYearLabel = '';
        datePanel.appendChild(calendarYearLabel);
        const showMonthsButton = document.createElement('button');
        showMonthsButton.type = 'button';
        showMonthsButton.dataset.ntDtpAction = 'show-months';
        showMonthsButton.appendChild(monthLabel);
        const menuIcon = document.createElement('span');
        menuIcon.className = 'nt-dtp-menu-button-icon';
        showMonthsButton.appendChild(menuIcon);
        datePanel.appendChild(showMonthsButton);
        const showYearsButton = document.createElement('button');
        showYearsButton.type = 'button';
        showYearsButton.dataset.ntDtpAction = 'show-years';
        showYearsButton.appendChild(calendarYearLabel);
        datePanel.appendChild(showYearsButton);
        const dayGrid = document.createElement('div');
        for (let i = 0; i < 42; i += 1) {
            const dayButton = document.createElement('button');
            dayButton.type = 'button';
            dayButton.dataset.ntDtpDayIndex = String(i);
            dayGrid.appendChild(dayButton);
        }
        datePanel.appendChild(dayGrid);
        picker.appendChild(datePanel);

        const monthPanel = document.createElement('div');
        monthPanel.dataset.ntDtpMonthPanel = '';
        const yearLabel = document.createElement('div');
        yearLabel.dataset.ntDtpYearLabel = '';
        monthPanel.appendChild(yearLabel);
        const monthGrid = document.createElement('div');
        monthGrid.dataset.ntDtpMonthGrid = '';
        for (let i = 0; i < 12; i += 1) {
            const monthButton = document.createElement('button');
            monthButton.type = 'button';
            monthButton.dataset.ntDtpMonthIndex = String(i);
            monthGrid.appendChild(monthButton);
        }
        monthPanel.appendChild(monthGrid);
        picker.appendChild(monthPanel);

        const yearPanel = document.createElement('div');
        yearPanel.dataset.ntDtpYearPanel = '';
        const yearList = document.createElement('div');
        yearList.dataset.ntDtpYearList = '';
        yearPanel.appendChild(yearList);
        picker.appendChild(yearPanel);

        const timePanel = document.createElement('div');
        timePanel.dataset.ntDtpTimePanel = '';
        const hourInput = document.createElement('input');
        hourInput.dataset.ntDtpHour = '';
        timePanel.appendChild(hourInput);
        const minuteInput = document.createElement('input');
        minuteInput.dataset.ntDtpMinute = '';
        timePanel.appendChild(minuteInput);
        const amButton = document.createElement('button');
        amButton.type = 'button';
        amButton.dataset.ntDtpAction = 'set-am';
        amButton.dataset.ntDtpMeridiem = 'am';
        timePanel.appendChild(amButton);
        const pmButton = document.createElement('button');
        pmButton.type = 'button';
        pmButton.dataset.ntDtpAction = 'set-pm';
        pmButton.dataset.ntDtpMeridiem = 'pm';
        timePanel.appendChild(pmButton);
        picker.appendChild(timePanel);

        if (lazy || !rendersDatePanel) {
            datePanel.remove();
        }
        if (lazy || !rendersMonthPanel) {
            monthPanel.remove();
        }
        if (lazy || !rendersYearPanel) {
            yearPanel.remove();
        }
        if (lazy || !rendersTimePanel) {
            timePanel.remove();
        }

        const actions = document.createElement('div');
        const confirmButton = document.createElement('button');
        confirmButton.type = 'button';
        confirmButton.dataset.ntDtpAction = 'confirm';
        actions.appendChild(confirmButton);
        const cancelButton = document.createElement('button');
        cancelButton.type = 'button';
        cancelButton.dataset.ntDtpAction = 'cancel';
        actions.appendChild(cancelButton);
        picker.appendChild(actions);

        const field = document.createElement('div');
        field.className = 'nt-input';
        field.appendChild(label);

        const root = document.createElement('div');
        root.className = 'nt-input-date-time';
        root.appendChild(field);
        root.appendChild(trigger);
        root.appendChild(picker);

        document.body.appendChild(root);

        setRect(label, { height: 48, left: 80, top: 84, width: 240 });
        setRect(input, { height: 36, left: 80, top: 90, width: 240 });
        setRect(picker, { height: 300, left: 0, top: 0, width: 320 });

        return {
            confirmButton,
            calendarYearLabel,
            cancelButton,
            hourInput,
            input,
            field,
            label,
            minuteInput,
            monthLabel,
            picker,
            showMonthsButton,
            showYearsButton,
            trigger,
            root,
            yearList,
            yearLabel,
        };
    }

    function openPicker(trigger) {
        onLoad(trigger.closest('.nt-input-date-time'), null);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }

    function createPageScriptFixture(options = {}) {
        const fixture = createPickerFixture(options);
        const script = document.createElement('nt-page-script');
        document.body.insertBefore(script, fixture.root);
        return { ...fixture, script };
    }

    function createNativePickerFixture({ disabled = false, readOnly = false, type = 'date' } = {}) {
        const input = document.createElement('input');
        input.type = type;
        input.disabled = disabled;
        input.readOnly = readOnly;
        input.dataset.ntDtpNativeInput = 'true';
        input.showPicker = jest.fn();

        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.tabIndex = -1;
        trigger.dataset.ntDtpNativeTrigger = 'true';

        const root = document.createElement('div');
        root.className = 'nt-input-date-time nt-input-date-time-native-picker';
        root.append(input, trigger);
        document.body.appendChild(root);

        return { input, root, trigger };
    }

    beforeEach(() => {
        document.body.innerHTML = '';
        jest.clearAllMocks();
        jest.useFakeTimers();
        jest.setSystemTime(fixedNow);

        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1280 });
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 720 });

        global.requestAnimationFrame = callback => {
            callback();
            return 1;
        };
        global.cancelAnimationFrame = () => { };
    });

    afterEach(() => {
        const trackedInputs = document.querySelectorAll('input[data-nt-dtp-input="true"], input[data-nt-dtp-native-input="true"]');
        trackedInputs.forEach(input => onDispose(input, null));
        document.body.innerHTML = '';
        jest.useRealTimers();
        jest.restoreAllMocks();
    });

    test('opens from trigger and shows current month/year when date value is empty', () => {
        const { calendarYearLabel, input, monthLabel, picker, root, trigger } = createPickerFixture({ mode: 'date', value: '' });

        onLoad(root, null);
        openPicker(trigger);

        const expectedMonthLabel = new Intl.DateTimeFormat(undefined, { month: 'long' }).format(new Date(2026, 2, 1));
        const expectedYearLabel = new Intl.DateTimeFormat(undefined, { year: 'numeric' }).format(new Date(2026, 2, 1));

        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
        expect(monthLabel.textContent).toBe(expectedMonthLabel);
        expect(calendarYearLabel.textContent).toBe(expectedYearLabel);
        expect(document.activeElement).toBe(input);
    });

    test.each([
        ['date', 'date', '2026-03-15'],
        ['datetime', 'datetime-local', '2026-03-15T14:05'],
        ['month', 'month', '2026-03'],
        ['time', 'time', '14:05:00'],
    ])('custom %s picker suppresses and restores the native input type', (mode, nativeType, value) => {
        const { input, root } = createPickerFixture({ mode, value });

        expect(input.type).toBe(nativeType);

        onLoad(root, null);

        expect(input.type).toBe('text');
        expect(input.value).toBe(value);

        onDispose(root, null);

        expect(input.type).toBe(nativeType);
        expect(input.value).toBe(value);
    });

    test('creates picker panel controls lazily on first open', () => {
        const { picker, trigger } = createPickerFixture({ lazy: true, mode: 'date', value: '' });

        expect(picker.querySelectorAll('[data-nt-dtp-day-index]')).toHaveLength(0);
        expect(picker.querySelectorAll('[data-nt-dtp-month-index]')).toHaveLength(0);

        openPicker(trigger);

        expect(picker.querySelectorAll('[data-nt-dtp-day-index]')).toHaveLength(42);
        expect(picker.querySelectorAll('[data-nt-dtp-month-index]')).toHaveLength(12);
        expect(picker.querySelector('[data-nt-dtp-year-list]')).not.toBeNull();
        expect(picker.querySelector('[data-nt-dtp-hour]')).toBeNull();
    });

    test.each([
        ['datetime', { dayCount: 42, hasTime: true, monthCount: 12, hasYearList: true }],
        ['month', { dayCount: 0, hasTime: false, monthCount: 12, hasYearList: false }],
        ['time', { dayCount: 0, hasTime: true, monthCount: 0, hasYearList: false }],
    ])('creates lazy picker panel controls for %s mode', (mode, expectations) => {
        const { picker, trigger } = createPickerFixture({ lazy: true, mode, value: mode === 'datetime' ? '2026-03-15T14:05' : '' });

        openPicker(trigger);

        expect(picker.querySelectorAll('[data-nt-dtp-day-index]')).toHaveLength(expectations.dayCount);
        expect(picker.querySelectorAll('[data-nt-dtp-month-index]')).toHaveLength(expectations.monthCount);
        expect(picker.querySelector('[data-nt-dtp-year-list]') !== null).toBe(expectations.hasYearList);
        expect(picker.querySelector('[data-nt-dtp-hour]') !== null).toBe(expectations.hasTime);
    });

    test('month menu button switches docked date picker to month selection view', () => {
        const { cancelButton, confirmButton, picker, showMonthsButton, trigger } = createPickerFixture({ mode: 'date', value: '2026-03-15' });

        openPicker(trigger);
        showMonthsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('nt-dtp-view-month')).toBe(true);
        expect(cancelButton.textContent).toBe('Back');
        expect(cancelButton.getAttribute('aria-label')).toBe('Back to calendar');
        expect(confirmButton.hidden).toBe(true);

        cancelButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('nt-dtp-view-month')).toBe(false);
        expect(cancelButton.textContent).toBe('Cancel');
        expect(confirmButton.hidden).toBe(false);
    });

    test('year menu button switches docked date picker to scrollable year selection view', () => {
        const { calendarYearLabel, cancelButton, confirmButton, picker, showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '2026-03-15' });
        yearList.setAttribute('b-testscope', '');

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        const selectedYear = yearList.querySelector('[data-nt-dtp-year-option="2026"]');

        expect(picker.classList.contains('nt-dtp-view-year')).toBe(true);
        expect(cancelButton.textContent).toBe('Back');
        expect(confirmButton.hidden).toBe(true);
        expect(yearList.children.length).toBeLessThanOrEqual(300);
        expect(yearList.children.length).toBeGreaterThan(100);
        expect(selectedYear).not.toBeNull();
        expect(selectedYear.hasAttribute('b-testscope')).toBe(true);

        yearList.querySelector('[data-nt-dtp-year-option="2030"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(picker.classList.contains('nt-dtp-view-year')).toBe(false);
        expect(calendarYearLabel.textContent).toBe('2030');
    });

    test('year list extends upward to year 0001', () => {
        const { showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '0105-03-15' });

        Object.defineProperty(yearList, 'clientHeight', { configurable: true, value: 240 });
        Object.defineProperty(yearList, 'scrollHeight', { configurable: true, value: 1200 });

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(yearList.querySelector('[data-nt-dtp-year-option="1"]')).toBeNull();

        yearList.scrollTop = 0;
        yearList.dispatchEvent(new Event('scroll'));

        const firstYear = yearList.querySelector('[data-nt-dtp-year-option="1"]');
        expect(firstYear).not.toBeNull();
        expect(firstYear.textContent).toBe('0001');
        expect(yearList.children.length).toBeLessThanOrEqual(300);
    });

    test('year list extends downward to year 9999 without unbounded growth', () => {
        const { showYearsButton, trigger, yearList } = createPickerFixture({ mode: 'date', value: '9800-03-15' });

        Object.defineProperty(yearList, 'clientHeight', { configurable: true, value: 240 });
        Object.defineProperty(yearList, 'scrollHeight', { configurable: true, value: 1200 });

        openPicker(trigger);
        showYearsButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(yearList.querySelector('[data-nt-dtp-year-option="9999"]')).toBeNull();

        yearList.scrollTop = 960;
        yearList.dispatchEvent(new Event('scroll'));
        yearList.dispatchEvent(new Event('scroll'));

        const lastYear = yearList.querySelector('[data-nt-dtp-year-option="9999"]');
        expect(lastYear).not.toBeNull();
        expect(lastYear.textContent).toBe('9999');
        expect(yearList.children.length).toBeLessThanOrEqual(300);
    });

    test('time input works without a visible seconds field', () => {
        const { confirmButton, hourInput, input, minuteInput, trigger } = createPickerFixture({ mode: 'time', value: '09:15:22' });

        openPicker(trigger);
        hourInput.value = '11';
        minuteInput.value = '45';
        hourInput.dispatchEvent(new Event('input', { bubbles: true }));
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('11:45:00');
    });

    test('existing hidden seconds are not preserved for time confirmation', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'time', value: '09:15:22' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('09:15:00');
    });

    test('min and max dates disable out-of-range calendar days', () => {
        const { input, picker, trigger } = createPickerFixture({
            max: '2026-03-20',
            min: '2026-03-10',
            mode: 'date',
            value: '',
        });

        openPicker(trigger);

        const outOfRange = picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="5"]');
        const inRange = picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="15"]');

        expect(outOfRange).not.toBeNull();
        expect(inRange).not.toBeNull();
        expect(outOfRange.disabled).toBe(true);
        expect(inRange.disabled).toBe(false);
    });

    test('disabled date values are blocked in the calendar', () => {
        const { input, picker, trigger } = createPickerFixture({
            disabledDates: '2026-03-15',
            mode: 'date',
            value: '',
        });

        openPicker(trigger);

        const disabledDate = picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="15"]');

        expect(disabledDate).not.toBeNull();
        expect(disabledDate.disabled).toBe(true);
    });

    test('disabled date values refresh when attributes change before reopen', () => {
        const { input, picker, root, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        let targetDate = picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="15"]');
        expect(targetDate.disabled).toBe(false);

        input.dataset.ntDtpDisabledDates = '2026-03-15';
        onUpdate(root, null);

        targetDate = picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="15"]');
        expect(targetDate.disabled).toBe(true);
    });

    test('disabled time values block confirm until time changes', () => {
        const { confirmButton, input, minuteInput, trigger } = createPickerFixture({
            disabledTimes: '14:05:00',
            mode: 'time',
            value: '14:05:09',
        });

        openPicker(trigger);

        expect(confirmButton.disabled).toBe(true);

        minuteInput.value = '06';
        minuteInput.dispatchEvent(new Event('input', { bubbles: true }));

        expect(confirmButton.disabled).toBe(false);
    });

    test('empty time confirmation emits zero seconds because seconds are not editable', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'time', value: '' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('14:05:00');
    });

    test('datetime confirmation respects the input format attribute', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:09' });
        input.setAttribute('format', 'yyyy-MM-ddTHH:mm');

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-15T14:05');
    });

    test('enhances native picker trigger without adding it to the tab order', () => {
        const { input, root, trigger } = createNativePickerFixture();

        onLoad(root, null);
        const mouseDown = new MouseEvent('mousedown', { bubbles: true, cancelable: true });
        trigger.dispatchEvent(mouseDown);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(root.dataset.ntDtpNativeEnhanced).toBe('true');
        expect(trigger.tabIndex).toBe(-1);
        expect(mouseDown.defaultPrevented).toBe(true);
        expect(input.showPicker).toHaveBeenCalledTimes(1);
        expect(document.activeElement).toBe(input);

        onDispose(root, null);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(root.dataset.ntDtpNativeEnhanced).toBeUndefined();
        expect(input.showPicker).toHaveBeenCalledTimes(1);
    });

    test('leaves native input focus available for typing', () => {
        const { input, root } = createNativePickerFixture();

        onLoad(root, null);
        input.focus();

        expect(input.showPicker).not.toHaveBeenCalled();

        onDispose(root, null);
        input.blur();
        input.focus();

        expect(input.showPicker).not.toHaveBeenCalled();
    });

    test.each([
        ['disabled', { disabled: true }],
        ['read-only', { readOnly: true }],
    ])('does not open the native picker when the input is %s', (_, options) => {
        const { input, root, trigger } = createNativePickerFixture(options);

        onLoad(root, null);
        input.dispatchEvent(new FocusEvent('focus'));
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.showPicker).not.toHaveBeenCalled();
    });

    test('custom datetime format preserves and confirms a 12-hour display value', () => {
        const { confirmButton, input, minuteInput, picker, root, trigger } = createPickerFixture({
            format: 'MM/dd/yyyy hh:mm tt',
            mode: 'datetime',
            value: '03/15/2026 02:05 PM',
        });
        input.type = 'text';
        input.value = '03/15/2026 02:05 PM';

        openPicker(trigger);

        expect(input.type).toBe('text');
        expect(input.value).toBe('03/15/2026 02:05 PM');
        expect(picker.querySelector('[data-nt-dtp-meridiem="pm"]').getAttribute('aria-pressed')).toBe('true');

        minuteInput.value = '45';
        minuteInput.dispatchEvent(new Event('input', { bubbles: true }));
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('03/15/2026 02:45 PM');

        onDispose(root, null);
        expect(input.type).toBe('text');
        expect(input.value).toBe('03/15/2026 02:45 PM');
    });

    test.each([
        ['time', 'HH:mm tt', '1:30 AM', '01:30 AM'],
        ['time', 'HH:mm tt', '13:30 AM', '01:30 AM'],
        ['datetime', 'MM/dd/yyyy HH:mm tt', '01/01/2000 13:30 AM', '01/01/2000 01:30 AM'],
    ])('honors a typed AM value in a custom %s field that uses HH with tt', (mode, format, value, expected) => {
        const { input, picker, root, trigger } = createPickerFixture({ format, mode, value });
        input.type = 'text';
        input.value = value;

        openPicker(trigger);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new FocusEvent('blur'));
        jest.runOnlyPendingTimers();

        expect(input.value).toBe(expected);
        expect(picker.querySelector('[data-nt-dtp-meridiem="am"]').getAttribute('aria-pressed')).toBe('true');
    });

    test.each([
        ['date', 'MM/dd/yyyy', '01012000', '01/01/2000'],
        ['time', 'HH:mm tt', '1330PM', '13:30 PM'],
        ['datetime', 'MM/dd/yyyy HH:mm tt', '010120001330PM', '01/01/2000 13:30 PM'],
        ['datetime', 'MM/dd/yyyy HH:mm tt', '02022020 1:30 AM', '02/02/2020 01:30 AM'],
        ['datetimeoffset', 'MM/dd/yyyy HH:mm tt', '02022020 1:30 AM', '02/02/2020 01:30 AM'],
    ])('formats a compact or partially formatted custom %s value on blur', (mode, format, value, expected) => {
        const { input, root } = createPickerFixture({ format, mode });
        input.type = 'text';
        input.value = value;
        const inputHandler = jest.fn();
        const changeHandler = jest.fn();
        input.addEventListener('input', inputHandler);
        input.addEventListener('change', changeHandler);
        onLoad(root, null);

        input.dispatchEvent(new FocusEvent('blur'));
        jest.runOnlyPendingTimers();

        expect(input.value).toBe(expected);
        expect(inputHandler).toHaveBeenCalledTimes(1);
        expect(changeHandler).toHaveBeenCalledTimes(1);
    });

    test.each(['02312000', '0101200'])('reports and preserves an invalid or incomplete custom date value on blur: %s', value => {
        const { field, input, root } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date' });
        input.type = 'text';
        input.value = value;
        const changeHandler = jest.fn();
        input.addEventListener('change', changeHandler);
        onLoad(root, null);

        input.dispatchEvent(new FocusEvent('blur'));

        expect(input.value).toBe(value);
        expect(changeHandler).not.toHaveBeenCalled();
        expect(input.validity.valid).toBe(false);
        expect(input.validationMessage).toBe('Enter a value in the format MM/dd/yyyy.');
        expect(input.getAttribute('aria-invalid')).toBe('true');
        expect(field.classList.contains('nt-dtp-parse-invalid')).toBe(true);
    });

    test('clears the custom parse error after a valid value is committed', () => {
        const { field, input, root } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date' });
        input.type = 'text';
        input.setAttribute('aria-invalid', 'false');
        input.value = '02312000';
        onLoad(root, null);
        input.dispatchEvent(new FocusEvent('blur'));

        input.value = '02022020';
        input.dispatchEvent(new FocusEvent('blur'));
        jest.runOnlyPendingTimers();

        expect(input.value).toBe('02/02/2020');
        expect(input.validity.valid).toBe(true);
        expect(input.getAttribute('aria-invalid')).toBe('false');
        expect(field.classList.contains('nt-dtp-parse-invalid')).toBe(false);
    });

    test('formats the committed value before the change event bubbles to Blazor', () => {
        const { input, root } = createPickerFixture({ format: 'MM/dd/yyyy HH:mm tt', mode: 'datetime' });
        input.type = 'text';
        input.value = '010120001330PM';
        const inputHandler = jest.fn();
        let bubbledValue = null;
        input.addEventListener('input', inputHandler);
        root.addEventListener('change', event => bubbledValue = event.target.value);
        onLoad(root, null);

        input.dispatchEvent(new Event('change', { bubbles: true }));

        expect(input.value).toBe('01/01/2000 13:30 PM');
        expect(bubbledValue).toBe('01/01/2000 13:30 PM');
        expect(inputHandler).toHaveBeenCalledTimes(1);
    });

    test('synchronizes the formatted value to an input replaced during interactive rendering', () => {
        const { input, root } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date' });
        input.id = 'replaced-date-input';
        input.type = 'text';
        input.value = '01012000';
        let bubbledValue = null;
        root.addEventListener('change', event => bubbledValue = event.target.value);
        onLoad(root, null);

        input.dispatchEvent(new FocusEvent('blur'));
        const replacement = input.cloneNode();
        replacement.value = '01012000';
        input.replaceWith(replacement);
        jest.runOnlyPendingTimers();

        expect(replacement.value).toBe('01/01/2000');
        expect(bubbledValue).toBe('01/01/2000');
    });

    test('disposing the custom picker removes blur formatting', () => {
        const { input, root } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date' });
        input.type = 'text';
        input.value = '01012000';
        onLoad(root, null);
        onDispose(root, null);

        input.dispatchEvent(new FocusEvent('blur'));

        expect(input.value).toBe('01012000');
    });

    test('existing hidden seconds are not preserved for datetime confirmation', () => {
        const { confirmButton, input, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:09' });

        openPicker(trigger);
        confirmButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-15T14:05');
    });

    test('disposing one input keeps other inputs functional', () => {
        const first = createPickerFixture({ mode: 'date' });
        const second = createPickerFixture({ mode: 'date' });

        openPicker(first.trigger);
        onDispose(first.input, null);

        openPicker(second.trigger);

        expect(second.picker.classList.contains('nt-dtp-open')).toBe(true);
    });

    test('disposing page-script element scope removes disconnected input listeners', () => {
        const { input, picker, root, trigger } = createPickerFixture({ mode: 'date' });

        openPicker(trigger);
        onDispose(root, null);
        picker.classList.add('nt-dtp-open');
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
    });

    test('disposing disconnected page-script element scope removes input listeners', () => {
        const { input, picker, script, trigger } = createPageScriptFixture({ mode: 'date' });

        onLoad(script, null);
        trigger.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        script.remove();
        onDispose(script, null);
        picker.classList.add('nt-dtp-open');
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
    });

    test('tab on input closes picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('enter on input confirms current selection and closes picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });

        openPicker(trigger);
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' }));

        expect(input.value).toBe('2026-03-15');
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('typed date updates the open picker selection before Enter confirms it', () => {
        const { input, picker, trigger } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date', value: '03/15/2026' });
        input.type = 'text';
        input.value = '03/15/2026';
        openPicker(trigger);

        input.value = '04/20/2030';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        const selectedDate = picker.querySelector('[data-nt-dtp-year="2030"][data-nt-dtp-month="3"][data-nt-dtp-day="20"]');
        const enterEvent = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' });
        input.dispatchEvent(enterEvent);

        expect(selectedDate.getAttribute('aria-selected')).toBe('true');
        expect(input.value).toBe('04/20/2030');
        expect(enterEvent.defaultPrevented).toBe(true);
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('typed time updates the open picker controls before Enter confirms it', () => {
        const { hourInput, input, picker, trigger } = createPickerFixture({ format: 'HH:mm tt', mode: 'time', value: '13:30 PM' });
        input.type = 'text';
        input.value = '13:30 PM';
        openPicker(trigger);

        input.value = '13:30 AM';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' }));

        expect(hourInput.value).toBe('1');
        expect(picker.querySelector('[data-nt-dtp-meridiem="am"]').getAttribute('aria-pressed')).toBe('true');
        expect(input.value).toBe('01:30 AM');
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('Enter preserves invalid typed text instead of confirming the stale picker selection', () => {
        const { input, picker, trigger } = createPickerFixture({ format: 'MM/dd/yyyy', mode: 'date', value: '03/15/2026' });
        input.type = 'text';
        input.value = '03/15/2026';
        openPicker(trigger);

        input.value = '02/31/2026';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        const enterEvent = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, key: 'Enter' });
        input.dispatchEvent(enterEvent);

        expect(input.value).toBe('02/31/2026');
        expect(enterEvent.defaultPrevented).toBe(true);
        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
    });

    test('Enter on an open picker input does not propagate to its form', () => {
        const { input, root, trigger } = createPickerFixture({ mode: 'date', value: '2026-03-15' });
        const form = document.createElement('form');
        root.replaceWith(form);
        form.appendChild(root);
        const formKeyDownHandler = jest.fn();
        form.addEventListener('keydown', formKeyDownHandler);
        openPicker(trigger);
        const enterEvents = ['keydown', 'keypress', 'keyup'].map(type => new KeyboardEvent(type, { bubbles: true, cancelable: true, key: 'Enter' }));

        enterEvents.forEach(event => input.dispatchEvent(event));

        enterEvents.forEach(event => expect(event.defaultPrevented).toBe(true));
        expect(formKeyDownHandler).not.toHaveBeenCalled();
    });

    test('Enter within open picker controls does not propagate to its form', () => {
        const { hourInput, root, trigger } = createPickerFixture({ mode: 'time', value: '13:30:00' });
        const form = document.createElement('form');
        root.replaceWith(form);
        form.appendChild(root);
        const formKeyDownHandler = jest.fn();
        form.addEventListener('keydown', formKeyDownHandler);
        openPicker(trigger);
        const enterEvents = ['keydown', 'keypress', 'keyup'].map(type => new KeyboardEvent(type, { bubbles: true, cancelable: true, key: 'Enter' }));

        enterEvents.forEach(event => hourInput.dispatchEvent(event));

        enterEvents.forEach(event => expect(event.defaultPrevented).toBe(true));
        expect(formKeyDownHandler).not.toHaveBeenCalled();
    });

    test('selecting a date fills the field without OK and closes the date picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '2026-03-15' });
        const inputHandler = jest.fn();
        const changeHandler = jest.fn();
        input.addEventListener('input', inputHandler);
        input.addEventListener('change', changeHandler);

        openPicker(trigger);
        picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="16"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-16');
        expect(inputHandler).toHaveBeenCalledTimes(1);
        expect(changeHandler).toHaveBeenCalledTimes(1);
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('selecting a datetime day fills the field and keeps the picker open for time edits', () => {
        const { input, minuteInput, picker, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:00' });

        openPicker(trigger);
        picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="16"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-03-16T14:05');
        expect(picker.classList.contains('nt-dtp-open')).toBe(true);

        minuteInput.value = '30';
        minuteInput.dispatchEvent(new Event('input', { bubbles: true }));

        expect(input.value).toBe('2026-03-16T14:30');
    });

    test('changing the time fills the field without OK', () => {
        const { hourInput, input, picker, trigger } = createPickerFixture({ mode: 'time', value: '09:15:00' });

        openPicker(trigger);
        hourInput.value = '11';
        hourInput.dispatchEvent(new Event('input', { bubbles: true }));

        expect(input.value).toBe('11:15:00');
        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
    });

    test('selecting a month fills the month field and closes the picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'month', value: '2026-03' });

        openPicker(trigger);
        picker.querySelector('[data-nt-dtp-month-index][data-nt-dtp-month="5"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe('2026-06');
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('cancel restores the value the field had when the picker opened', () => {
        const { cancelButton, input, minuteInput, picker, trigger } = createPickerFixture({ mode: 'datetime', value: '2026-03-15T14:05:00' });

        openPicker(trigger);
        const openValue = input.value;
        picker.querySelector('[data-nt-dtp-year="2026"][data-nt-dtp-month="2"][data-nt-dtp-day="20"]').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        minuteInput.value = '45';
        minuteInput.dispatchEvent(new Event('input', { bubbles: true }));
        expect(input.value).not.toBe(openValue);

        cancelButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(input.value).toBe(openValue);
        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
    });

    test('picker controls are removed from tab order', () => {
        const { confirmButton, hourInput, picker, root } = createPickerFixture({ mode: 'datetime' });

        onLoad(root, null);
        onUpdate(root, null);

        expect(picker.querySelectorAll('[tabindex="-1"]').length).toBeGreaterThan(0);
        expect(hourInput.getAttribute('tabindex')).toBe('-1');
        expect(confirmButton.getAttribute('tabindex')).toBe('-1');
    });

    test('small viewport uses modal layout class', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        openPicker(trigger);

        expect(picker.classList.contains('nt-dtp-modal')).toBe(true);
        expect(picker.getAttribute('aria-modal')).toBe('true');
    });

    test('small viewport modal renders immediately without waiting for animation frame', () => {
        const { picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const rafSpy = jest.fn(() => 1);
        global.requestAnimationFrame = rafSpy;

        openPicker(trigger);

        expect(rafSpy).not.toHaveBeenCalled();
        expect(picker.classList.contains('nt-dtp-modal')).toBe(true);
        expect(picker.style.visibility).toBe('visible');
    });

    test('small viewport input focus does not open modal picker', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', openOnFocus: true, value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        onLoad(root, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
        expect(picker.classList.contains('nt-dtp-modal')).toBe(false);
    });

    test('desktop input focus opens docked picker when open on focus is enabled', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', openOnFocus: true, value: '' });

        onLoad(root, null);
        input.dispatchEvent(new Event('focus'));

        expect(picker.classList.contains('nt-dtp-open')).toBe(true);
        expect(picker.classList.contains('nt-dtp-modal')).toBe(false);
    });

    test('clicking the input after trigger-open does not trigger a second open render cycle', () => {
        const { input, trigger } = createPickerFixture({ mode: 'date', value: '' });
        const rafSpy = jest.fn(callback => {
            callback();
            return 1;
        });
        global.requestAnimationFrame = rafSpy;

        openPicker(trigger);
        expect(rafSpy).toHaveBeenCalledTimes(1);

        input.dispatchEvent(new Event('click', { bubbles: true }));
        expect(rafSpy).toHaveBeenCalledTimes(1);
    });

    test('small viewport input click after trigger-open does not reopen modal picker', () => {
        const { input, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });
        const setAttributeSpy = jest.spyOn(picker, 'setAttribute');

        openPicker(trigger);
        input.dispatchEvent(new Event('click', { bubbles: true }));

        const openStateWrites = setAttributeSpy.mock.calls.filter(([name, value]) => name === 'aria-hidden' && value === 'false');
        expect(openStateWrites).toHaveLength(1);
    });

    test('desktop layout aligns picker left edge to the label left edge', () => {
        const { input, label, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        setRect(label, { height: 72, left: 36, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        openPicker(trigger);

        expect(picker.classList.contains('nt-dtp-modal')).toBe(false);
        expect(picker.style.left).toBe('36px');
    });

    test('desktop layout offsets below label bounds to avoid label overlap', () => {
        const { input, label, picker, trigger } = createPickerFixture({ mode: 'date', value: '' });
        setRect(label, { height: 100, left: 40, top: 60, width: 520 });
        setRect(input, { height: 36, left: 180, top: 88, width: 240 });

        openPicker(trigger);

        expect(picker.classList.contains('nt-dtp-modal')).toBe(false);
        expect(picker.style.top).toBe('166px');
    });

    test('small viewport does not auto-open when input is already focused before load', () => {
        const { input, picker, root } = createPickerFixture({ mode: 'date', value: '' });
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 500 });

        input.focus();
        expect(document.activeElement).toBe(input);

        onLoad(root, null);

        expect(picker.classList.contains('nt-dtp-open')).toBe(false);
        expect(picker.classList.contains('nt-dtp-modal')).toBe(false);
    });
});
