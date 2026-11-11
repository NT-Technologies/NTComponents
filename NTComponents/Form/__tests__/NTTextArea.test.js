/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { autoGrowTextArea } from '../../wwwroot/NTComponents.lib.module.js';
import { enhanceAll, onDispose, onLoad, onUpdate } from '../NTTextArea.razor.js';

describe('NTTextArea browser behavior', () => {
    function createTextArea({ minVisibleLines = 2, maxVisibleLines = null, scrollHeight = 96 } = {}) {
        const root = document.createElement('div');
        root.className = 'nt-input nt-textarea';
        const controlContainer = document.createElement('span');
        controlContainer.className = 'nt-input-control-container';
        const input = document.createElement('textarea');
        input.dataset.ntTextareaAutogrow = 'true';
        input.dataset.ntTextareaMinVisibleLines = minVisibleLines.toString();
        input.style.lineHeight = '20px';
        input.style.paddingTop = '4px';
        input.style.paddingBottom = '4px';
        input.style.border = '0';
        if (maxVisibleLines !== null) {
            input.dataset.ntTextareaMaxVisibleLines = maxVisibleLines.toString();
        }

        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            get: () => scrollHeight,
        });

        controlContainer.appendChild(input);
        root.appendChild(controlContainer);
        document.body.appendChild(root);
        onLoad(input);
        return input;
    }

    afterEach(() => {
        jest.useRealTimers();
        onDispose(null);
        document.body.textContent = '';
        delete global.ResizeObserver;
        delete global.MutationObserver;
    });

    test('sizes to full scroll height by default', () => {
        const input = createTextArea({ scrollHeight: 124 });

        expect(input.style.height).toBe('124px');
        expect(input.style.overflowY).toBe('hidden');
        expect(input.closest('.nt-input').classList.contains('nt-textarea-enhanced')).toBe(true);
    });

    test('does not shrink below minimum visible lines', () => {
        const input = createTextArea({ minVisibleLines: 3, scrollHeight: 36 });

        expect(input.style.minHeight).toBe('68px');
        expect(input.style.height).toBe('68px');
        expect(input.style.overflowY).toBe('hidden');
    });

    test('caps height at max visible lines and enables vertical scrolling', () => {
        const input = createTextArea({ maxVisibleLines: 2, scrollHeight: 120 });

        expect(input.style.height).toBe('48px');
        expect(input.style.overflowY).toBe('scroll');
    });

    test('updates height on input event listener', () => {
        let scrollHeight = 64;
        const input = createTextArea({ scrollHeight });
        expect(input.style.height).toBe('64px');
        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            get: () => scrollHeight,
        });

        scrollHeight = 104;
        input.dispatchEvent(new Event('input', { bubbles: true }));

        expect(input.style.height).toBe('104px');
    });

    test('retries sizing when the shared autosize bridge is registered after lifecycle load', () => {
        jest.useFakeTimers();
        const originalAutoGrowTextArea = window.NTComponents.autoGrowTextArea;
        delete window.NTComponents.autoGrowTextArea;
        const root = document.createElement('div');
        root.className = 'nt-input nt-textarea';
        const input = document.createElement('textarea');
        input.dataset.ntTextareaAutogrow = 'true';
        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            value: 108,
        });

        root.appendChild(input);
        document.body.appendChild(root);
        onLoad(input);

        expect(input.style.height).toBe('');

        window.NTComponents.autoGrowTextArea = originalAutoGrowTextArea;
        jest.advanceTimersByTime(16);

        expect(input.style.height).toBe('108px');
        expect(root.classList.contains('nt-textarea-enhanced')).toBe(true);
    });

    test('updates height once when observed wrapper elements resize', () => {
        let resizeCallback;
        const observe = jest.fn();
        const disconnect = jest.fn();
        global.ResizeObserver = jest.fn().mockImplementation(callback => {
            resizeCallback = callback;
            return {
                observe,
                disconnect,
            };
        });

        let scrollHeight = 120;
        let scrollHeightReadCount = 0;
        const input = createTextArea({ maxVisibleLines: 2, scrollHeight });
        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            get: () => {
                scrollHeightReadCount++;
                return scrollHeight;
            },
        });

        const root = input.closest('.nt-input');
        const controlContainer = root.querySelector('.nt-input-control-container');
        expect(observe).toHaveBeenCalledWith(root);
        expect(observe).toHaveBeenCalledWith(controlContainer);
        expect(observe).not.toHaveBeenCalledWith(input);

        scrollHeight = 36;
        scrollHeightReadCount = 0;
        resizeCallback([{ target: root }, { target: controlContainer }]);

        expect(input.style.height).toBe('48px');
        expect(input.style.overflowY).toBe('hidden');
        expect(scrollHeightReadCount).toBe(1);

        onDispose(input);
        expect(disconnect).toHaveBeenCalled();
    });

    test('cleans up inline sizing when autogrow is disabled', () => {
        let mutationCallback;
        const disconnect = jest.fn();
        global.MutationObserver = jest.fn().mockImplementation(callback => {
            mutationCallback = callback;
            return {
                disconnect,
                observe: jest.fn(),
            };
        });
        const input = createTextArea({ scrollHeight: 80 });
        const root = input.closest('.nt-input');

        delete input.dataset.ntTextareaAutogrow;
        mutationCallback([{ type: 'attributes', target: input }]);

        expect(input.style.height).toBe('');
        expect(input.style.minHeight).toBe('');
        expect(input.style.overflowY).toBe('');
        expect(root.classList.contains('nt-textarea-enhanced')).toBe(false);
        expect(disconnect).toHaveBeenCalled();
    });

    test('onUpdate removes inline sizing when autogrow is disabled', () => {
        const input = createTextArea({ scrollHeight: 80 });

        delete input.dataset.ntTextareaAutogrow;
        onUpdate(input);

        expect(input.style.height).toBe('');
        expect(input.style.minHeight).toBe('');
        expect(input.style.overflowY).toBe('');
    });

    test('enhanceAll initializes dynamically rendered textareas', () => {
        const input = createTextArea({ scrollHeight: 124 });
        onDispose(input);
        input.style.height = '';

        enhanceAll(document);

        expect(input.style.height).toBe('124px');
    });

    test('page script lifecycle resolves the textarea next to the script element', () => {
        const root = document.createElement('div');
        root.className = 'nt-input nt-textarea';
        const input = document.createElement('textarea');
        input.dataset.ntTextareaAutogrow = 'true';
        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            value: 112,
        });
        const script = document.createElement('nt-page-script');

        root.appendChild(input);
        root.appendChild(script);
        document.body.appendChild(root);

        onLoad(script);

        expect(input.style.height).toBe('112px');
        expect(root.classList.contains('nt-textarea-enhanced')).toBe(true);
    });

    test('exported autoGrowTextArea can be called directly by compatibility bridges', () => {
        const root = document.createElement('div');
        root.className = 'nt-input nt-textarea';
        const input = document.createElement('textarea');
        input.dataset.ntTextareaAutogrow = 'true';
        Object.defineProperty(input, 'scrollHeight', {
            configurable: true,
            value: 88,
        });

        root.appendChild(input);
        document.body.appendChild(root);

        autoGrowTextArea(input);

        expect(input.style.height).toBe('88px');
        expect(root.classList.contains('nt-textarea-enhanced')).toBe(true);
    });
});
