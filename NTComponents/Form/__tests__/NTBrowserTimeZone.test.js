import { onDispose, onLoad, onUpdate } from '../NTBrowserTimeZone.razor.js';
import { jest } from '@jest/globals';

function createField(value = 'UTC') {
    const input = document.createElement('input');
    input.type = 'hidden';
    input.dataset.ntBrowserTimeZone = 'true';
    input.value = value;

    const script = document.createElement('nt-page-script');
    document.body.append(input, script);
    return { input, script };
}

describe('NTBrowserTimeZone browser behavior', () => {
    beforeEach(() => {
        document.body.innerHTML = '';
        jest.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue({ timeZone: 'America/Denver' });
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    test('fills the SSR-rendered field once across repeated static lifecycle calls', () => {
        const { input, script } = createField();
        const changes = [];
        input.addEventListener('change', event => changes.push(event));

        onLoad(script);
        onLoad(script);
        onUpdate(script);
        onUpdate(script);

        expect(input.value).toBe('America/Denver');
        expect(changes).toHaveLength(1);
        expect(changes[0].bubbles).toBe(true);
    });

    test('corrects a server-provided value to the current browser time zone', () => {
        const { input, script } = createField('America/New_York');

        onLoad(script);

        expect(input.value).toBe('America/Denver');
    });

    test('re-announces an SSR-captured value for each interactive load but not updates', () => {
        const { input } = createField('America/Denver');
        let changeCount = 0;
        input.addEventListener('change', () => changeCount++);

        onLoad(input);
        onUpdate(input);
        onLoad(input);
        onUpdate(input);

        expect(changeCount).toBe(2);
    });

    test('uses the SSR-rendered UTC fallback when the browser cannot resolve a time zone', () => {
        Intl.DateTimeFormat.prototype.resolvedOptions.mockImplementation(() => {
            throw new Error('Unavailable');
        });
        const { input } = createField();
        let changeCount = 0;
        input.addEventListener('change', () => changeCount++);

        expect(() => onLoad(input)).not.toThrow();
        expect(input.value).toBe('UTC');
        expect(changeCount).toBe(1);
    });

    test('preserves an existing value when browser detection fails', () => {
        Intl.DateTimeFormat.prototype.resolvedOptions.mockImplementation(() => {
            throw new Error('Unavailable');
        });
        const { input, script } = createField('America/New_York');

        onLoad(script);

        expect(input.value).toBe('America/New_York');
    });

    test('preserves an existing value when constructing Intl.DateTimeFormat fails', () => {
        jest.spyOn(Intl, 'DateTimeFormat').mockImplementation(() => {
            throw new Error('Unavailable');
        });
        const { input, script } = createField('America/New_York');
        const onChange = jest.fn();
        input.addEventListener('change', onChange);

        expect(() => onLoad(script)).not.toThrow();
        expect(input.value).toBe('America/New_York');
        expect(onChange).not.toHaveBeenCalled();
    });

    test.each([
        ['an empty string', ''],
        ['undefined', undefined],
        ['null', null],
        ['a number', 42],
        ['an object', { id: 'America/Denver' }],
        ['whitespace', '   ']
    ])('preserves an existing value when browser detection returns %s', (_description, timeZone) => {
        Intl.DateTimeFormat.prototype.resolvedOptions.mockReturnValue({ timeZone });
        const { input, script } = createField('America/New_York');
        const onChange = jest.fn();
        input.addEventListener('change', onChange);

        onLoad(script);

        expect(input.value).toBe('America/New_York');
        expect(onChange).not.toHaveBeenCalled();
    });

    test('preserves an unknown value when the fallback is disabled', () => {
        Intl.DateTimeFormat.prototype.resolvedOptions.mockReturnValue({ timeZone: '' });
        const { input } = createField('');
        let changeCount = 0;
        input.addEventListener('change', () => changeCount++);

        onLoad(input);

        expect(input.value).toBe('');
        expect(changeCount).toBe(0);
    });

    test.each([null, undefined, document.createElement('div'), document.createElement('input'), document.createElement('nt-page-script')])('ignores a missing or unrelated element', element => {
        expect(() => {
            onLoad(element);
            onUpdate(element);
        }).not.toThrow();
        expect(Intl.DateTimeFormat.prototype.resolvedOptions).not.toHaveBeenCalled();
    });

    test('ignores a marked input after it is disconnected', () => {
        const { input } = createField('America/New_York');
        const onChange = jest.fn();
        input.addEventListener('change', onChange);
        input.remove();

        onLoad(input);
        onUpdate(input);

        expect(input.value).toBe('America/New_York');
        expect(onChange).not.toHaveBeenCalled();
    });

    test('disposal is repeatable and leaves the field unchanged', () => {
        const { input } = createField('America/New_York');
        const onChange = jest.fn();
        input.addEventListener('change', onChange);

        onDispose(input);
        onDispose(input);

        expect(input.value).toBe('America/New_York');
        expect(onChange).not.toHaveBeenCalled();
    });
});
