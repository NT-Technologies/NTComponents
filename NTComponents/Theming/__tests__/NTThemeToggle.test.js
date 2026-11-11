/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';

const runtimeUrl = new URL('../.generated/NTTheme.runtime.js', import.meta.url);
const toggleUrl = new URL('../NTThemeToggle.razor.js', import.meta.url);

const loadModules = async () => {
    delete window.NTComponents?.NTThemeRuntime;
    window.NTComponents = window.NTComponents || {};
    window.NTComponents.customAttribute = 'ntid';
    jest.resetModules();
    await import(runtimeUrl.href);
    return await import(toggleUrl.href);
};

const createThemeToggleElement = async () => {
    const { onLoad } = await loadModules();
    onLoad(null, null);

    const element = document.createElement('nt-theme-toggle');
    element.innerHTML = `
        <span class="material-symbols-outlined nt-theme-toggle-icon"></span>
        <select class="nt-theme-select">
            <option value="LIGHT-DEFAULT">Light - Default</option>
            <option value="DARK-HIGH">Dark - High</option>
            <option value="SYSTEM-DEFAULT">System - Default</option>
        </select>
    `;
    document.body.appendChild(element);
    return element;
};

const createThemeToggleElementWithRuntimeApply = async applyResult => {
    const { onLoad, onUpdate } = await loadModules();
    onLoad(null, null);

    const applySpy = jest.spyOn(window.NTComponents.NTThemeRuntime, 'apply').mockResolvedValue(applyResult);
    const element = document.createElement('nt-theme-toggle');
    element.innerHTML = `
        <span class="material-symbols-outlined nt-theme-toggle-icon"></span>
        <select class="nt-theme-select">
            <option value="LIGHT-DEFAULT">Light - Default</option>
            <option value="DARK-HIGH">Dark - High</option>
            <option value="SYSTEM-DEFAULT">System - Default</option>
        </select>
    `;
    document.body.appendChild(element);
    await Promise.resolve();

    return { applySpy, element, onUpdate };
};

describe('NTThemeToggle JavaScript Module', () => {
    beforeEach(() => {
        document.head.innerHTML = '';
        document.body.innerHTML = '';
        jest.clearAllMocks();
    });

    test('registers independent nt-theme-toggle custom element', async () => {
        const { onLoad } = await loadModules();
        const wasDefined = Boolean(customElements.get('nt-theme-toggle'));
        const defineSpy = jest.spyOn(customElements, 'define');

        onLoad(null, null);
        onLoad(null, null);

        expect(customElements.get('nt-theme-toggle')).toBeDefined();
        expect(defineSpy).toHaveBeenCalledTimes(wasDefined ? 0 : 1);

        defineSpy.mockRestore();
    });

    test('registers and unregisters with runtime lifecycle', async () => {
        const element = await createThemeToggleElement();
        const runtime = window.NTComponents.NTThemeRuntime;

        expect(runtime.controls.has(element)).toBe(true);

        element.remove();

        expect(runtime.controls.has(element)).toBe(false);
    });

    test('updateThemeLink keeps existing link when next file is known missing', async () => {
        const { element } = await createThemeToggleElementWithRuntimeApply({ theme: 'LIGHT' });
        document.head.innerHTML = '';
        const existingLink = document.createElement('link');
        existingLink.rel = 'stylesheet';
        existingLink.setAttribute('data-nt-theme', 'true');
        existingLink.href = '/Themes/light.css';
        document.head.appendChild(existingLink);

        const result = await element.updateThemeLink('/Themes/missing.css', false);

        expect(result).toBe(existingLink);
        expect(document.head.querySelector('link[data-nt-theme]')).toBe(existingLink);
        expect(document.head.querySelector('style[data-nt-theme-fallback]')).toBeNull();
    });

    test('themeContrastSelected applies selected theme and contrast through runtime', async () => {
        const element = await createThemeToggleElement();
        const applySpy = jest.spyOn(window.NTComponents.NTThemeRuntime, 'apply').mockImplementation(async options => {
            element.updateIcon('DARK');
            await element.initSelect('DARK', { skipApply: true });
            return { theme: 'DARK' };
        });
        const select = element.querySelector('select.nt-theme-select');

        select.value = 'DARK-HIGH';
        await element.themeContrastSelected({ target: select });

        expect(applySpy).toHaveBeenCalledWith({
            element,
            theme: 'DARK',
            contrast: 'HIGH',
            waitForLoad: true
        });
        expect(element.querySelector('.nt-theme-toggle-icon').innerHTML).toBe('light_mode');
    });

    test('first render coalesces connected and page script update theme apply', async () => {
        const { onLoad, onUpdate } = await loadModules();
        onLoad(null, null);

        const applySpy = jest.spyOn(window.NTComponents.NTThemeRuntime, 'apply').mockResolvedValue({ theme: 'LIGHT' });
        const element = document.createElement('nt-theme-toggle');
        element.innerHTML = `
            <span class="material-symbols-outlined nt-theme-toggle-icon"></span>
            <select class="nt-theme-select">
                <option value="LIGHT-DEFAULT">Light - Default</option>
                <option value="DARK-HIGH">Dark - High</option>
                <option value="SYSTEM-DEFAULT">System - Default</option>
            </select>
        `;
        document.body.appendChild(element);
        const pageScript = document.createElement('nt-page-script');
        element.after(pageScript);

        onLoad(pageScript, null);
        onUpdate(pageScript, null);
        await Promise.resolve();
        await Promise.resolve();

        expect(applySpy).toHaveBeenCalledTimes(1);
        expect(applySpy).toHaveBeenCalledWith({ element, waitForLoad: true });
    });

    test('selection relies on runtime control sync instead of repeating local select sync', async () => {
        const element = await createThemeToggleElement();
        const initSelectSpy = jest.spyOn(element, 'initSelect');
        const select = element.querySelector('select.nt-theme-select');
        jest.spyOn(window.NTComponents.NTThemeRuntime, 'apply').mockResolvedValue({ theme: 'DARK' });

        select.value = 'DARK-HIGH';
        await element.themeContrastSelected({ target: select });

        expect(initSelectSpy).not.toHaveBeenCalled();
    });

    test('connected toggle wires select change handler', async () => {
        const { applySpy, element } = await createThemeToggleElementWithRuntimeApply({ theme: 'DARK' });
        const select = element.querySelector('select.nt-theme-select');

        select.value = 'DARK-HIGH';
        select.dispatchEvent(new Event('change'));
        await Promise.resolve();
        await Promise.resolve();

        expect(applySpy).toHaveBeenCalledWith({
            element,
            theme: 'DARK',
            contrast: 'HIGH',
            waitForLoad: true
        });
        expect(element.querySelector('.nt-theme-toggle-icon').innerHTML).toBe('light_mode');
    });

    test('onUpdate resolves adjacent page script marker to nt theme toggle', async () => {
        const { applySpy, element, onUpdate } = await createThemeToggleElementWithRuntimeApply({ theme: 'LIGHT' });
        const pageScript = document.createElement('nt-page-script');
        element.after(pageScript);

        onUpdate(pageScript, null);
        await Promise.resolve();

        expect(applySpy).toHaveBeenCalledWith({ element, waitForLoad: true });
        expect(element.querySelector('.nt-theme-toggle-icon').innerHTML).toBe('dark_mode');
    });

    test('page script disposal keeps connected permanent toggle registered and onLoad can re-register it', async () => {
        const { onLoad, onDispose } = await loadModules();
        onLoad(null, null);

        const element = document.createElement('nt-theme-toggle');
        element.setAttribute('data-permanent', '');
        element.innerHTML = `
            <span class="material-symbols-outlined nt-theme-toggle-icon"></span>
            <select class="nt-theme-select">
                <option value="LIGHT-DEFAULT">Light - Default</option>
                <option value="DARK-HIGH">Dark - High</option>
                <option value="SYSTEM-DEFAULT">System - Default</option>
            </select>
        `;
        document.body.appendChild(element);
        const pageScript = document.createElement('nt-page-script');
        element.after(pageScript);
        const runtime = window.NTComponents.NTThemeRuntime;

        expect(runtime.controls.has(element)).toBe(true);

        onDispose(pageScript, null);

        expect(runtime.controls.has(element)).toBe(true);

        runtime.controls.delete(element);
        const replacementPageScript = document.createElement('nt-page-script');
        element.after(replacementPageScript);

        onLoad(replacementPageScript, null);

        expect(runtime.controls.has(element)).toBe(true);
    });
});
