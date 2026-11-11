import { clearThemePreview, initializeThemePreview, updateThemePreview } from '../../../NTComponents.Site/wwwroot/js/theme-creator.js';

describe('Material theme preview', () => {
    const request = {
        primary: '#6750a4',
        variant: 'tonal-spot',
        harmonizeExtendedColors: false,
        extendedColors: { success: '#00c853', info: '#0091ea', warning: '#ffab00', assert: '#aa00ff' }
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="theme-root"><div id="theme-preview"></div></div>';
    });

    afterEach(() => {
        clearThemePreview();
        document.documentElement.removeAttribute('style');
    });

    test('GeneratedPreview_UpdatesColors_WithoutChangingRoot', () => {
        document.documentElement.style.setProperty('--nt-color-primary', 'rgb(1 2 3)');
        const originalRootStyle = document.documentElement.style.cssText;
        const preview = document.getElementById('theme-preview');
        initializeThemePreview('theme-root', 'theme-preview');

        updateThemePreview(request, 'light.css');

        expect(preview.style.getPropertyValue('--nt-color-primary')).toBe('rgb(101 85 143)');
        expect(preview.style.getPropertyValue('--nt-color-on-primary')).toBe('rgb(255 255 255)');
        expect(document.documentElement.style.cssText).toBe(originalRootStyle);

        updateThemePreview({ ...request, variant: 'vibrant' }, 'light.css');

        expect(preview.style.getPropertyValue('--nt-color-primary')).toBe('rgb(111 25 255)');
        expect(document.documentElement.style.cssText).toBe(originalRootStyle);
    });

    test('ClearedPreview_RestoresBothNamespaces_AndRemovesAddedColors', () => {
        const preview = document.getElementById('theme-preview');
        preview.style.setProperty('--nt-color-primary', 'rgb(1 2 3)');
        preview.style.setProperty('--other-property', 'original');
        const originalPreviewStyle = preview.style.cssText;
        initializeThemePreview('theme-root', 'theme-preview');
        updateThemePreview(request, 'light.css');
        updateThemePreview({ ...request, variant: 'vibrant' }, 'dark.css');
        expect(preview.style.getPropertyValue('--nt-color-primary')).not.toBe('rgb(1 2 3)');

        clearThemePreview();

        expect(preview.style.cssText).toBe(originalPreviewStyle);
        expect(preview.style.getPropertyValue('--nt-color-on-primary')).toBe('');
        clearThemePreview();
        expect(preview.style.cssText).toBe(originalPreviewStyle);
    });
});
