/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { existsSync } from 'node:fs';

const bundleUrl = new URL('../../wwwroot/NTTheme.js', import.meta.url);
const runtimeSourceUrl = new URL('../NTTheme.runtime.ts', import.meta.url);
const bootstrapSourceUrl = new URL('../theme-bootstrap.ts', import.meta.url);
const runtimeTestOutputUrl = new URL('../.generated/NTTheme.runtime.js', import.meta.url);
const bootstrapTestOutputUrl = new URL('../.generated/theme-bootstrap.js', import.meta.url);
const publishedRuntimeUrl = new URL('../../wwwroot/NTTheme.runtime.js', import.meta.url);
const publishedBootstrapUrl = new URL('../../wwwroot/theme-bootstrap.js', import.meta.url);
const publishedRuntimeSourceUrl = new URL('../../wwwroot/NTTheme.runtime.ts', import.meta.url);
const publishedBootstrapSourceUrl = new URL('../../wwwroot/theme-bootstrap.ts', import.meta.url);

describe('NTTheme head bundle', () => {
    beforeEach(() => {
        document.head.innerHTML = '';
        document.body.innerHTML = '';
        localStorage.clear();
        window.NTComponents = {};
        window.__ntThemeBootstrapped = false;
        window.Blazor = { addEventListener: jest.fn() };
        jest.resetModules();
    });

    test('installs the runtime and bootstraps it from one script', async () => {
        await import(bundleUrl.href);

        expect(window.NTComponents.NTThemeRuntime).toBeDefined();
        expect(window.__ntThemeBootstrapped).toBe(true);
        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));
    });

    test('builds only the combined public bundle while retaining isolated test outputs', () => {
        expect(existsSync(runtimeSourceUrl)).toBe(true);
        expect(existsSync(bootstrapSourceUrl)).toBe(true);
        expect(existsSync(runtimeTestOutputUrl)).toBe(true);
        expect(existsSync(bootstrapTestOutputUrl)).toBe(true);
        expect(existsSync(publishedRuntimeUrl)).toBe(false);
        expect(existsSync(publishedBootstrapUrl)).toBe(false);
        expect(existsSync(publishedRuntimeSourceUrl)).toBe(false);
        expect(existsSync(publishedBootstrapSourceUrl)).toBe(false);
    });
});
