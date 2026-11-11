/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';

const bootstrapUrl = new URL('../.generated/theme-bootstrap.js', import.meta.url);

const importBootstrap = () => {
    jest.resetModules();
    return import(bootstrapUrl.href);
};

describe('theme bootstrap', () => {
    beforeEach(() => {
        document.head.innerHTML = '';
        window.__ntThemeBootstrapped = false;
        window.__ntThemeEnhancedNavigationState = { preserveHeadAttributes: false };
        window.NTComponents = {
            NTThemeRuntime: {
                apply: jest.fn(),
                restoreThemeState: jest.fn()
            }
        };
        delete window.Blazor;
        jest.clearAllMocks();
    });

    test('applies theme immediately and registers enhancedload with Blazor', async () => {
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();

        expect(window.NTComponents.NTThemeRuntime.restoreThemeState).toHaveBeenCalledWith({ waitForLoad: false });
        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledWith({ waitForLoad: false });
        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancednavigationstart', expect.any(Function));
        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));
        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancednavigationend', expect.any(Function));

        const enhancedLoadHandler = window.Blazor.addEventListener.mock.calls.find(([eventName]) => eventName === 'enhancedload')[1];
        enhancedLoadHandler();

        expect(window.NTComponents.NTThemeRuntime.restoreThemeState).toHaveBeenCalledTimes(2);
        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledTimes(2);
    });

    test('does not bootstrap twice', async () => {
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();
        await importBootstrap();

        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledTimes(1);
        expect(window.Blazor.addEventListener).toHaveBeenCalledTimes(3);
    });

    test('enhancedload does not reapply an already restored active theme', async () => {
        window.NTComponents.NTThemeRuntime.hasCurrentThemeState = jest.fn()
            .mockReturnValueOnce(false)
            .mockReturnValueOnce(true);
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();
        const enhancedLoadHandler = window.Blazor.addEventListener.mock.calls.find(([eventName]) => eventName === 'enhancedload')[1];
        enhancedLoadHandler();

        expect(window.NTComponents.NTThemeRuntime.restoreThemeState).toHaveBeenCalledTimes(2);
        expect(window.NTComponents.NTThemeRuntime.apply).toHaveBeenCalledTimes(1);
    });

    test('defers enhancedload registration until Blazor is available', async () => {
        await importBootstrap();
        window.Blazor = { addEventListener: jest.fn() };

        document.dispatchEvent(new Event('DOMContentLoaded'));

        expect(window.Blazor.addEventListener).toHaveBeenCalledWith('enhancedload', expect.any(Function));
    });

    test('carries active theme attributes into an enhanced navigation response before DOM synchronization', async () => {
        document.head.innerHTML = `
            <style id="nt-theme-critical" data-permanent="nt-theme-critical"></style>
            <link id="nt-theme-default-light" rel="stylesheet" href="/Themes/light.css" media="not all" data-permanent="nt-theme-default-light">
            <link id="nt-theme-default-dark" rel="stylesheet" href="/Themes/dark.css" media="not all" data-permanent="nt-theme-default-dark">
            <link id="nt-theme-active-slot" rel="stylesheet" href="/Themes/dark-hc.css" data-nt-theme="true" data-nt-theme-loaded="true" data-permanent="nt-theme-active-slot">
            <link id="nt-theme-pending-slot" data-permanent="nt-theme-pending-slot">
        `;
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();
        const enhancedNavigationStartHandler = window.Blazor.addEventListener.mock.calls.find(([eventName]) => eventName === 'enhancednavigationstart')[1];
        enhancedNavigationStartHandler();

        const response = new DOMParser().parseFromString(`
            <html><head>
                <style id="nt-theme-critical" data-nt-theme-critical="true" data-permanent="nt-theme-critical">Canvas</style>
                <link id="nt-theme-default-light" rel="stylesheet" href="/Themes/light.css" media="(prefers-color-scheme: light)" data-nt-theme-default="true" data-permanent="nt-theme-default-light">
                <link id="nt-theme-default-dark" rel="stylesheet" href="/Themes/dark.css" media="(prefers-color-scheme: dark)" data-nt-theme-default="true" data-permanent="nt-theme-default-dark">
                <link id="nt-theme-active-slot" data-permanent="nt-theme-active-slot">
                <link id="nt-theme-pending-slot" data-permanent="nt-theme-pending-slot">
                <script id="nt-theme-config" type="application/json">{}</script>
            </head><body></body></html>
        `, 'text/html');

        const activeTheme = response.getElementById('nt-theme-active-slot');
        expect(activeTheme.getAttribute('href')).toBe('/Themes/dark-hc.css');
        expect(activeTheme.getAttribute('data-nt-theme')).toBe('true');
        expect(activeTheme.getAttribute('data-nt-theme-loaded')).toBe('true');
        expect(response.getElementById('nt-theme-default-light').getAttribute('media')).toBe('not all');
        expect(response.getElementById('nt-theme-default-light').hasAttribute('data-nt-theme-default')).toBe(false);
        expect(response.getElementById('nt-theme-critical').hasAttribute('data-nt-theme-critical')).toBe(false);
    });

    test('exits when runtime is missing', async () => {
        window.NTComponents = {};
        window.Blazor = { addEventListener: jest.fn() };

        await importBootstrap();

        expect(window.Blazor.addEventListener).not.toHaveBeenCalled();
    });
});
