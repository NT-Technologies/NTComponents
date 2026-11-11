import { onDispose as disposeRangeSlider, onLoad as loadRangeSlider } from '../NTInputRangeSlider.razor.js';
import { onDispose as disposeSlider, onLoad as loadSlider } from '../NTInputSlider.razor.js';

function createPageScript() {
    return document.createElement('nt-page-script');
}

function createSlider({ centered = false, value = '25' } = {}) {
    const root = document.createElement('div');
    root.className = centered ? 'nt-slider nt-slider-centered' : 'nt-slider nt-slider-standard-variant';
    root.style.setProperty('--nt-slider-start-percent', '0%');
    root.style.setProperty('--nt-slider-end-percent', '0%');

    const script = createPageScript();
    const input = document.createElement('input');
    input.type = 'range';
    input.dataset.ntSliderInput = 'true';
    input.min = '0';
    input.max = '100';
    input.value = value;

    const output = document.createElement('output');
    output.className = 'nt-slider-value-indicator';

    const icon = document.createElement('span');
    icon.className = 'nt-slider-inset-icon nt-slider-inset-icon-inactive';

    root.append(script, input, output, icon);
    document.body.append(root);
    return { icon, input, output, root, script };
}

function createRangeSlider({ start = '20', end = '80' } = {}) {
    const form = document.createElement('form');
    const root = document.createElement('div');
    root.className = 'nt-slider nt-slider-range nt-slider-horizontal';

    const script = createPageScript();
    const startInput = document.createElement('input');
    startInput.type = 'range';
    startInput.dataset.ntSliderRangeStart = 'true';
    startInput.name = 'model.Range.Start';
    startInput.min = '0';
    startInput.max = end;
    startInput.value = start;

    const endInput = document.createElement('input');
    endInput.type = 'range';
    endInput.dataset.ntSliderRangeEnd = 'true';
    endInput.name = 'model.Range.End';
    endInput.min = start;
    endInput.max = '100';
    endInput.value = end;

    const startOutput = document.createElement('output');
    startOutput.className = 'nt-slider-value-indicator nt-slider-start-value';
    const endOutput = document.createElement('output');
    endOutput.className = 'nt-slider-value-indicator nt-slider-end-value';

    root.append(script, startInput, endInput, startOutput, endOutput);
    form.append(root);
    document.body.append(form);
    return { endInput, endOutput, form, root, script, startInput, startOutput };
}

describe('NTInputSlider browser behavior', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        disposeSlider(null);
        disposeRangeSlider(null);
    });

    afterEach(() => {
        disposeSlider(null);
        disposeRangeSlider(null);
    });

    test('single slider updates SSR-rendered track variables and value indicator on native input', () => {
        const { icon, input, output, root, script } = createSlider();
        loadSlider(script);

        input.value = '75';
        input.dispatchEvent(new Event('input'));

        expect(root.style.getPropertyValue('--nt-slider-start-percent')).toBe('0%');
        expect(root.style.getPropertyValue('--nt-slider-end-percent')).toBe('75%');
        expect(root.style.getPropertyValue('--nt-slider-end-gap')).toBe('8px');
        expect(output.textContent).toBe('75');
        expect(icon.classList.contains('nt-slider-inset-icon-active')).toBe(true);
        expect(root.style.getPropertyValue('--nt-slider-inset-icon-position')).toBe('16px');
    });

    test('centered slider keeps active track around the midpoint in SSR enhancement', () => {
        const { input, root, script } = createSlider({ centered: true, value: '25' });
        input.min = '-100';
        input.max = '100';
        loadSlider(script);

        input.value = '-50';
        input.dispatchEvent(new Event('input'));

        expect(root.style.getPropertyValue('--nt-slider-start-percent')).toBe('25%');
        expect(root.style.getPropertyValue('--nt-slider-end-percent')).toBe('50%');
        expect(root.style.getPropertyValue('--nt-slider-start-gap')).toBe('8px');
        expect(root.style.getPropertyValue('--nt-slider-end-gap')).toBe('0px');
    });

    test('range slider prevents start from crossing end before static SSR form submit', () => {
        const { endInput, form, root, script, startInput, startOutput } = createRangeSlider();
        loadRangeSlider(script);

        startInput.value = '90';
        startInput.dispatchEvent(new Event('input'));
        form.dispatchEvent(new Event('submit'));

        expect(startInput.value).toBe('80');
        expect(endInput.value).toBe('80');
        expect(startInput.max).toBe('80');
        expect(endInput.min).toBe('80');
        expect(root.style.getPropertyValue('--nt-slider-start-percent')).toBe('80%');
        expect(root.style.getPropertyValue('--nt-slider-end-percent')).toBe('80%');
        expect(startOutput.textContent).toBe('80');
    });

    test('range slider prevents end from crossing start before static SSR form submit', () => {
        const { endInput, endOutput, form, root, script, startInput } = createRangeSlider();
        loadRangeSlider(script);

        endInput.value = '10';
        endInput.dispatchEvent(new Event('input'));
        form.dispatchEvent(new Event('submit'));

        expect(startInput.value).toBe('20');
        expect(endInput.value).toBe('20');
        expect(startInput.max).toBe('20');
        expect(endInput.min).toBe('20');
        expect(root.style.getPropertyValue('--nt-slider-start-percent')).toBe('20%');
        expect(root.style.getPropertyValue('--nt-slider-end-percent')).toBe('20%');
        expect(endOutput.textContent).toBe('20');
    });
});
