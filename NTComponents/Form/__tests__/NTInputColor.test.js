/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { getPicker, onDispose as disposePicker } from '../NTColorPicker.razor.js';
import { onDispose, onLoad, onUpdate } from '../NTInputColor.razor.js';
import { mountInputColor } from './colorPickerFixture.js';

describe('NTInputColor popover behavior', () => {
    let root;
    let input;
    let events;

    const trigger = () => root.querySelector('[data-nt-input-color-trigger]');
    const popover = () => root.querySelector('[data-nt-input-color-popover]');
    const swatch = () => root.querySelector('[data-nt-input-color-swatch]');
    const area = () => root.querySelector('[data-nt-color-area]');
    const action = name => root.querySelector(`[data-nt-input-color-action="${name}"]`);

    function mount(options = {}) {
        root = mountInputColor({ value: '#ff0000', ...options });
        input = root.querySelector('input[data-nt-input-color-input]');
        events = [];
        input.addEventListener('input', () => events.push(`input:${input.value}`));
        input.addEventListener('change', () => events.push(`change:${input.value}`));
        onLoad(root.querySelector('nt-page-script'));
        return root;
    }

    function key(target, name, init = {}) {
        target.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true, ...init }));
    }

    afterEach(() => {
        if (root) {
            onDispose(root.querySelector('nt-page-script') ?? root);
            disposePicker(root.querySelector('[data-nt-color-picker]'));
        }

        document.body.innerHTML = '';
        root = null;
        jest.restoreAllMocks();
    });

    it('shows the current color in the swatch on load and clears it for invalid text', () => {
        mount();

        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#ff0000');
        input.value = 'nope';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('');
    });

    it('follows typed hex values in the swatch and normalizes them on change', () => {
        mount();

        input.value = '#ABC';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#aabbcc');
        input.dispatchEvent(new Event('change', { bubbles: true }));
        expect(input.value).toBe('#aabbcc');

        input.value = 'abcdef';
        input.dispatchEvent(new Event('change', { bubbles: true }));
        expect(input.value).toBe('#abcdef');
    });

    it('leaves invalid text untouched on change so validation can report it', () => {
        mount();

        input.value = 'not a color';
        input.dispatchEvent(new Event('change', { bubbles: true }));

        expect(input.value).toBe('not a color');
    });

    it('opens from the trigger, loads the field value into the picker, and marks the trigger expanded', () => {
        mount({ value: '#00ff00' });
        popover().showPopover = jest.fn();

        trigger().click();

        expect(popover().showPopover).toHaveBeenCalledTimes(1);
        expect(trigger().getAttribute('aria-expanded')).toBe('true');
        expect(root.dataset.open).toBe('true');
        expect(getPicker(root).getValue()).toBe('#00ff00');
        expect(document.activeElement).toBe(area());
    });

    it('does not open a disabled or read-only field', () => {
        mount({ disabled: true });
        trigger().disabled = false;
        trigger().click();
        expect(root.dataset.open).toBeUndefined();
        document.body.innerHTML = '';

        mount({ readOnly: true });
        trigger().disabled = false;
        trigger().click();
        expect(root.dataset.open).toBeUndefined();
    });

    it('previews picker changes in the field with input events only', () => {
        mount();
        trigger().click();

        key(area(), 'ArrowLeft');

        expect(input.value).toBe('#ff0303');
        expect(events).toEqual(['input:#ff0303']);
        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#ff0303');
    });

    it('commits with a change event when OK is pressed and closes', () => {
        mount();
        popover().hidePopover = jest.fn();
        popover().matches = jest.fn(selector => selector === ':popover-open');
        trigger().click();
        key(area(), 'ArrowLeft');

        action('confirm').click();

        expect(events).toEqual(['input:#ff0303', 'change:#ff0303']);
        expect(popover().hidePopover).toHaveBeenCalledTimes(1);
        expect(root.dataset.open).toBeUndefined();
        expect(trigger().getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(trigger());
    });

    it('does not dispatch change when OK is pressed without a change', () => {
        mount();
        trigger().click();

        action('confirm').click();

        expect(events).toEqual([]);
    });

    it('restores the snapshot when Cancel is pressed', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');
        key(area(), 'ArrowLeft');

        action('cancel').click();

        expect(input.value).toBe('#ff0000');
        expect(events.at(-1)).toBe('input:#ff0000');
        expect(events.some(entry => entry.startsWith('change'))).toBe(false);
        expect(getPicker(root).getValue()).toBe('#ff0000');
        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#ff0000');
        expect(root.dataset.open).toBeUndefined();
    });

    it('restores the snapshot when Escape is pressed', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');

        key(document.body, 'Escape');

        expect(input.value).toBe('#ff0000');
        expect(events.some(entry => entry.startsWith('change'))).toBe(false);
        expect(root.dataset.open).toBeUndefined();
    });

    it('keeps the change when the user clicks outside', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');

        document.body.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));

        expect(input.value).toBe('#ff0303');
        expect(events).toEqual(['input:#ff0303', 'change:#ff0303']);
        expect(root.dataset.open).toBeUndefined();
    });

    it('ignores pointer presses inside the field', () => {
        mount();
        trigger().click();

        input.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));

        expect(root.dataset.open).toBe('true');
    });

    it('keeps the change when the browser light dismisses the popover', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');

        const toggle = new Event('toggle');
        toggle.newState = 'closed';
        popover().dispatchEvent(toggle);

        expect(events).toEqual(['input:#ff0303', 'change:#ff0303']);
        expect(root.dataset.open).toBeUndefined();
    });

    it('closes when the trigger is pressed again', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');

        trigger().click();

        expect(events.at(-1)).toBe('change:#ff0303');
        expect(root.dataset.open).toBeUndefined();
    });

    it('does not reopen when the trigger press already light dismissed the popover', () => {
        mount();
        trigger().click();
        trigger().dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));
        const toggle = new Event('toggle');
        toggle.newState = 'closed';
        popover().dispatchEvent(toggle);

        trigger().click();

        expect(root.dataset.open).toBeUndefined();
    });

    it('snapshots again on each open', () => {
        mount();
        trigger().click();
        key(area(), 'ArrowLeft');
        action('confirm').click();
        trigger().click();
        key(area(), 'ArrowLeft');

        action('cancel').click();

        expect(input.value).toBe('#ff0303');
    });

    it('syncs typed values into the open picker', () => {
        mount();
        trigger().click();

        input.value = '#0000ff';
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(getPicker(root).getValue()).toBe('#0000ff');
    });

    it('refreshes the swatch when the field gains focus after a programmatic value change', () => {
        mount();

        input.value = '#00ff00';
        input.dispatchEvent(new Event('focus'));

        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#00ff00');
    });

    it('refreshes the swatch on update', () => {
        mount();

        input.value = '#123456';
        onUpdate(root.querySelector('nt-page-script'));

        expect(swatch().style.getPropertyValue('--nt-input-color-swatch')).toBe('#123456');
    });

    it('stops reacting after disposal', () => {
        mount();
        onDispose(root.querySelector('nt-page-script'));

        trigger().click();

        expect(root.dataset.open).toBeUndefined();
    });

    it('ignores markup that has no field or popover', () => {
        const orphan = document.createElement('div');
        orphan.dataset.ntInputColor = 'true';
        orphan.innerHTML = '<nt-page-script></nt-page-script>';
        document.body.append(orphan);

        expect(() => onLoad(orphan.firstElementChild)).not.toThrow();
        expect(() => onDispose(orphan.firstElementChild)).not.toThrow();
    });
});
