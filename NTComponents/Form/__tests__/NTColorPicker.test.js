/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { formatColor, getPicker, hslToRgb, hsvToRgb, normalizeColor, onDispose, onLoad, onUpdate, parseColor, rgbToHsl, rgbToHsv } from '../NTColorPicker.razor.js';
import { mountPicker } from './colorPickerFixture.js';

describe('NTColorPicker color conversions', () => {
    it.each([
        ['#ff0000', { h: 0, s: 1, v: 1 }],
        ['#00ff00', { h: 120, s: 1, v: 1 }],
        ['#0000ff', { h: 240, s: 1, v: 1 }],
        ['#808080', { h: 0, s: 0, v: 128 / 255 }],
        ['#000000', { h: 0, s: 0, v: 0 }],
    ])('rgbToHsv converts %s', (hex, expected) => {
        const { r, g, b } = parseColor(hex);
        const hsv = rgbToHsv(r, g, b);

        expect(hsv.h).toBeCloseTo(expected.h, 5);
        expect(hsv.s).toBeCloseTo(expected.s, 5);
        expect(hsv.v).toBeCloseTo(expected.v, 5);
    });

    it('round trips every 17th rgb value through hsv and hsl', () => {
        for (let r = 0; r < 256; r += 17) {
            for (let g = 0; g < 256; g += 17) {
                for (let b = 0; b < 256; b += 17) {
                    const hsv = rgbToHsv(r, g, b);
                    expect(hsvToRgb(hsv.h, hsv.s, hsv.v)).toEqual({ r, g, b });
                    const hsl = rgbToHsl(r, g, b);
                    expect(hslToRgb(hsl.h, hsl.s, hsl.l)).toEqual({ r, g, b });
                }
            }
        }
    });

    it('rgbToHsl matches known values', () => {
        const red = rgbToHsl(255, 0, 0);
        expect([red.h, red.s, red.l]).toEqual([0, 1, 0.5]);
        const white = rgbToHsl(255, 255, 255);
        expect([white.s, white.l]).toEqual([0, 1]);
    });

    it.each([
        ['#abc', '#aabbcc'],
        ['ABCDEF', '#abcdef'],
        ['#AABBCC', '#aabbcc'],
        ['  #6750a4 ', '#6750a4'],
    ])('normalizeColor accepts %s', (text, expected) => {
        expect(normalizeColor(text, false)).toBe(expected);
    });

    it.each(['', '#12', '#12345', '#1234567', 'red', '#ggg', null, undefined])('normalizeColor rejects %p', text => {
        expect(normalizeColor(text, true)).toBeNull();
    });

    it('keeps alpha only when alpha is shown and below 100 percent', () => {
        expect(normalizeColor('#11223380', true)).toBe('#11223380');
        expect(normalizeColor('#112233ff', true)).toBe('#112233');
        expect(normalizeColor('#11223380', false)).toBe('#112233');
        expect(parseColor('#00000080').a).toBeCloseTo(128 / 255, 5);
        expect(formatColor({ r: 1, g: 2, b: 3, a: 0.5 }, true)).toBe('#01020380');
        expect(formatColor({ r: 1, g: 2, b: 3, a: 0.5 }, false)).toBe('#010203');
    });
});

describe('NTColorPicker behavior', () => {
    let root;

    const area = () => root.querySelector('[data-nt-color-area]');
    const hidden = () => root.querySelector('[data-nt-color-value]');
    const field = key => root.querySelector(`[data-nt-color-field="${key}"]`);

    function listen(target, type) {
        const handler = jest.fn();
        target.addEventListener(type, handler);
        return handler;
    }

    function key(target, name, init = {}) {
        const event = new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true, ...init });
        target.dispatchEvent(event);
        return event;
    }

    function pointer(target, type, clientX, clientY) {
        target.dispatchEvent(new MouseEvent(type, { clientX, clientY, bubbles: true, cancelable: true, button: 0 }));
    }

    function mockAreaRect() {
        jest.spyOn(area(), 'getBoundingClientRect').mockReturnValue({ left: 0, top: 0, width: 200, height: 100, right: 200, bottom: 100, x: 0, y: 0, toJSON: () => { } });
    }

    function change(target, value) {
        target.value = value;
        target.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function mount(options) {
        root = mountPicker(options);
        onLoad(root);
        return root;
    }

    afterEach(() => {
        if (root) {
            onDispose(root);
        }

        document.body.innerHTML = '';
        root = null;
        jest.restoreAllMocks();
    });

    it('reflects the initial value in every control', () => {
        mount({ value: '#6750a4' });

        expect(field('hex').value).toBe('#6750a4');
        expect([field('r').value, field('g').value, field('b').value]).toEqual(['103', '80', '164']);
        expect(area().getAttribute('aria-valuetext')).toBe('Saturation 51%, brightness 64%');
        expect(root.querySelector('[data-nt-color-hue]').value).toBe('256');
        expect(hidden().value).toBe('#6750a4');
        expect(root.style.getPropertyValue('--nt-color-picker-solid')).toBe('#6750a4');
    });

    it('starts neutral when there is no value and does not write the hidden input', () => {
        mount({});

        expect(field('hex').value).toBe('#000000');
        expect(hidden().value).toBe('');
    });

    it('steps saturation and brightness from the keyboard and commits each step', () => {
        mount({ value: '#ff0000' });
        const input = listen(hidden(), 'input');
        const changed = listen(hidden(), 'change');

        key(area(), 'ArrowLeft');
        expect(hidden().value).toBe('#ff0303');
        key(area(), 'ArrowRight');
        key(area(), 'ArrowDown', { shiftKey: true });
        expect(hidden().value).toBe('#e60000');
        expect(input).toHaveBeenCalledTimes(3);
        expect(changed).toHaveBeenCalledTimes(3);
    });

    it('supports Home, End, PageUp, and PageDown', () => {
        mount({ value: '#ff0000' });

        key(area(), 'Home');
        expect(hidden().value).toBe('#ffffff');
        key(area(), 'PageDown');
        expect(area().getAttribute('aria-valuetext')).toBe('Saturation 0%, brightness 90%');
        key(area(), 'End');
        expect(area().getAttribute('aria-valuetext')).toBe('Saturation 100%, brightness 90%');
        key(area(), 'PageUp');
        expect(area().getAttribute('aria-valuetext')).toBe('Saturation 100%, brightness 100%');
    });

    it('ignores unrelated keys', () => {
        mount({ value: '#ff0000' });
        const event = key(area(), 'a');

        expect(event.defaultPrevented).toBe(false);
        expect(hidden().value).toBe('#ff0000');
    });

    it('drags the area with the pointer and commits once on release', () => {
        mount({ value: '#ff0000' });
        mockAreaRect();
        const input = listen(hidden(), 'input');
        const changed = listen(hidden(), 'change');

        pointer(area(), 'pointerdown', 100, 50);
        expect(hidden().value).toBe('#804040');
        pointer(area(), 'pointermove', 200, 0);
        expect(hidden().value).toBe('#ff0000');
        pointer(area(), 'pointermove', 0, 100);
        expect(hidden().value).toBe('#000000');
        pointer(area(), 'pointerup', 0, 100);

        expect(input).toHaveBeenCalledTimes(3);
        expect(changed).toHaveBeenCalledTimes(1);
    });

    it('jumps the thumb when the area is clicked', () => {
        mount({ value: '#ff0000' });
        mockAreaRect();
        const changed = listen(hidden(), 'change');

        pointer(area(), 'pointerdown', 100, 50);
        pointer(area(), 'pointerup', 100, 50);

        expect(hidden().value).toBe('#804040');
        expect(changed).toHaveBeenCalledTimes(1);
        expect(area().style.getPropertyValue('--nt-color-area-x')).toBe('50%');
        expect(area().style.getPropertyValue('--nt-color-area-y')).toBe('50%');
    });

    it('ignores the pointer and keyboard when disabled', () => {
        mount({ value: '#ff0000', disabled: true });
        mockAreaRect();

        pointer(area(), 'pointerdown', 100, 50);
        pointer(area(), 'pointermove', 0, 0);
        key(area(), 'ArrowLeft');

        expect(hidden().value).toBe('#ff0000');
    });

    it('ignores pointer movement when no drag started', () => {
        mount({ value: '#ff0000' });
        mockAreaRect();

        pointer(area(), 'pointermove', 0, 0);

        expect(hidden().value).toBe('#ff0000');
    });

    it('updates the hue from the slider and commits on change', () => {
        mount({ value: '#ff0000' });
        const slider = root.querySelector('[data-nt-color-hue]');
        const input = listen(hidden(), 'input');
        const changed = listen(hidden(), 'change');

        slider.value = '120';
        slider.dispatchEvent(new Event('input', { bubbles: true }));
        expect(hidden().value).toBe('#00ff00');
        expect(changed).not.toHaveBeenCalled();
        slider.dispatchEvent(new Event('change', { bubbles: true }));

        expect(input).toHaveBeenCalledTimes(1);
        expect(changed).toHaveBeenCalledTimes(1);
        expect(root.style.getPropertyValue('--nt-color-picker-hue')).toBe('hsl(120 100% 50%)');
    });

    it('does not let native slider events escape the picker', () => {
        mount({ value: '#ff0000' });
        const outside = jest.fn();
        document.body.addEventListener('input', outside);
        document.body.addEventListener('change', outside);
        const slider = root.querySelector('[data-nt-color-hue]');

        slider.value = '10';
        slider.dispatchEvent(new Event('input', { bubbles: true }));
        slider.dispatchEvent(new Event('change', { bubbles: true }));
        const afterSlider = outside.mock.calls.length;
        key(area(), 'ArrowLeft');

        expect(afterSlider).toBe(2);
        document.body.removeEventListener('input', outside);
        document.body.removeEventListener('change', outside);
    });

    it('writes an eight digit value from the opacity slider', () => {
        mount({ value: '#ff0000', showAlpha: true });
        const slider = root.querySelector('[data-nt-color-alpha]');

        slider.value = '50';
        slider.dispatchEvent(new Event('input', { bubbles: true }));
        slider.dispatchEvent(new Event('change', { bubbles: true }));

        expect(hidden().value).toBe('#ff000080');
        expect(field('a').value).toBe('50');
        expect(field('hex').value).toBe('#ff000080');
        slider.value = '100';
        slider.dispatchEvent(new Event('input', { bubbles: true }));
        expect(hidden().value).toBe('#ff0000');
    });

    it('drops alpha from values when alpha is disabled', () => {
        mount({ value: '#ff000080' });

        expect(hidden().value).toBe('#ff0000');
        expect(field('hex').value).toBe('#ff0000');
        expect(field('a')).toBeNull();
    });

    it('switches formats with aria-pressed and shows only that format', () => {
        mount({ value: '#6750a4' });
        const visible = () => [...root.querySelectorAll('[data-nt-color-fieldset]')].filter(element => !element.hidden).map(element => element.dataset.ntColorFieldset);
        const pressed = () => [...root.querySelectorAll('[data-nt-color-format]')].filter(button => button.getAttribute('aria-pressed') === 'true').map(button => button.dataset.ntColorFormat);

        expect(visible()).toEqual(['hex']);
        root.querySelector('[data-nt-color-format="rgb"]').click();
        expect(visible()).toEqual(['rgb', 'rgb', 'rgb']);
        expect(pressed()).toEqual(['rgb']);
        root.querySelector('[data-nt-color-format="hsl"]').click();
        expect(visible()).toEqual(['hsl', 'hsl', 'hsl']);
        expect(pressed()).toEqual(['hsl']);
        expect(root.dataset.format).toBe('hsl');
    });

    it('shows the alpha field in rgb and hsl but not hex', () => {
        mount({ value: '#6750a4', showAlpha: true });
        const alpha = field('a').closest('[data-nt-color-fieldset]');

        expect(alpha.hidden).toBe(true);
        root.querySelector('[data-nt-color-format="rgb"]').click();
        expect(alpha.hidden).toBe(false);
        root.querySelector('[data-nt-color-format="hsl"]').click();
        expect(alpha.hidden).toBe(false);
    });

    it('commits a valid rgb channel on change and reverts an invalid one', () => {
        mount({ value: '#ff0000' });
        const changed = listen(hidden(), 'change');

        change(field('g'), '255');
        expect(hidden().value).toBe('#ffff00');
        expect(changed).toHaveBeenCalledTimes(1);

        change(field('g'), '300');
        expect(field('g').value).toBe('255');
        change(field('r'), 'abc');
        expect(field('r').value).toBe('255');
        expect(hidden().value).toBe('#ffff00');
        expect(changed).toHaveBeenCalledTimes(1);
    });

    it('commits the hex field on Enter and normalizes short values', () => {
        mount({ value: '#ff0000' });
        const changed = listen(hidden(), 'change');

        field('hex').value = '#ABC';
        const event = key(field('hex'), 'Enter');

        expect(event.defaultPrevented).toBe(true);
        expect(hidden().value).toBe('#aabbcc');
        expect(field('hex').value).toBe('#aabbcc');
        expect(changed).toHaveBeenCalledTimes(1);
    });

    it('reverts an invalid hex entry without emitting events', () => {
        mount({ value: '#ff0000' });
        const changed = listen(hidden(), 'change');

        field('hex').value = 'nope';
        key(field('hex'), 'Enter');
        change(field('hex'), 'nope');

        expect(field('hex').value).toBe('#ff0000');
        expect(changed).not.toHaveBeenCalled();
    });

    it('does not emit a second change when Enter is followed by the native change', () => {
        mount({ value: '#ff0000' });
        const changed = listen(hidden(), 'change');

        field('hex').value = '#00ff00';
        key(field('hex'), 'Enter');
        field('hex').dispatchEvent(new Event('change', { bubbles: true }));

        expect(changed).toHaveBeenCalledTimes(1);
    });

    it('commits hsl channels and keeps the hue of grey colors', () => {
        mount({ value: '#ff0000' });

        change(field('s'), '0');
        expect(hidden().value).toBe('#808080');
        change(field('l'), '100');
        expect(hidden().value).toBe('#ffffff');
        expect(field('h').value).toBe('0');
        change(field('h'), '360');
        expect(field('h').value).toBe('0');
    });

    it('commits the alpha field', () => {
        mount({ value: '#ff0000', showAlpha: true });

        change(field('a'), '25');

        expect(hidden().value).toBe('#ff000040');
        expect(root.querySelector('[data-nt-color-alpha]').value).toBe('25');
    });

    it('applies a new data-value on update without emitting events', () => {
        mount({ value: '#ff0000' });
        const input = listen(hidden(), 'input');
        const changed = listen(hidden(), 'change');

        root.dataset.value = '#0000ff';
        onUpdate(root);

        expect(field('hex').value).toBe('#0000ff');
        expect(hidden().value).toBe('#0000ff');
        expect(input).not.toHaveBeenCalled();
        expect(changed).not.toHaveBeenCalled();
    });

    it('dispatches custom events only when embedded', () => {
        mount({ value: '#ff0000', embedded: true });
        const nativeInput = listen(hidden(), 'input');
        const custom = listen(root, 'nt-color-input');
        const committed = listen(root, 'nt-color-change');

        key(area(), 'ArrowLeft');

        expect(nativeInput).not.toHaveBeenCalled();
        expect(custom).toHaveBeenCalledTimes(1);
        expect(custom.mock.calls[0][0].detail).toEqual({ value: '#ff0303' });
        expect(committed).toHaveBeenCalledTimes(1);
        expect(hidden().value).toBe('#ff0303');
    });

    it('exposes a handle to read and set the value', () => {
        mount({ value: '#ff0000' });
        const handle = getPicker(root);

        expect(handle.setValue('nope')).toBe(false);
        expect(handle.setValue('#00f')).toBe(true);
        expect(handle.getValue()).toBe('#0000ff');
        expect(field('hex').value).toBe('#0000ff');
        handle.focus();
        expect(document.activeElement).toBe(area());
    });

    it('can be found from a sibling page script marker', () => {
        root = mountPicker({ value: '#ff0000' });
        const marker = document.createElement('nt-page-script');
        root.after(marker);

        onLoad(marker);

        expect(getPicker(root)).not.toBeNull();
    });

    it('stops reacting after disposal', () => {
        mount({ value: '#ff0000' });
        const host = root;
        onDispose(host);

        key(area(), 'ArrowLeft');

        expect(getPicker(host)).toBeNull();
        expect(hidden().value).toBe('#ff0000');
    });

    it('releases pickers that left the document', () => {
        mount({ value: '#ff0000' });
        const detached = root;
        detached.remove();

        root = mountPicker({ value: '#00ff00' });
        onLoad(root);

        expect(getPicker(detached)).toBeNull();
    });
});
