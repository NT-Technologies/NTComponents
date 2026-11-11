interface NTComponentsWindow {
    customAttribute?: string;
    NTThemeRuntime?: NTThemeRuntime;
}

interface NTThemeConfiguration {
    contrastStorageKey?: string;
    darkDefaultCss?: string;
    darkHighCss?: string;
    darkMediumCss?: string;
    defaultContrast?: string;
    defaultTheme?: string;
    lightDefaultCss?: string;
    lightHighCss?: string;
    lightMediumCss?: string;
    themeStorageKey?: string;
    themesRoot?: string;
}

interface NTThemeApplyOptions {
    contrast?: string;
    element: HTMLElement;
    theme?: string;
    waitForLoad?: boolean;
}

interface NTThemeApplyResult {
    theme: string;
}

interface NTThemeRuntime {
    apply(options: NTThemeApplyOptions): Promise<NTThemeApplyResult>;
    applyStylesheet(href: string, options?: { waitForLoad?: boolean }): Promise<HTMLLinkElement | null>;
    cleanupInvalidStoredValues(config: NTThemeConfiguration): void;
    controls?: Set<HTMLElement>;
    getConfiguration(element: HTMLElement): NTThemeConfiguration;
    getFallbackCss(): string;
    getStoredContrast(config: NTThemeConfiguration): string | null;
    getStoredTheme(config: NTThemeConfiguration): string | null;
    injectFallbackStyles(): void;
    normalizeContrast(contrast: string | null | undefined, fallback: string): string;
    normalizeTheme(theme: string | null | undefined, fallback: string): string;
    registerControl(element: HTMLElement): void;
    removeCriticalThemeStyles(): void;
    safeSetStorage(key: string, value: string): void;
    systemPrefersDark(): boolean;
    unregisterControl(element: HTMLElement): void;
}

declare global {
    interface HTMLElementTagNameMap {
        'nt-theme-toggle': NTThemeToggleElement;
    }
}

const defaultCustomAttribute = 'nt-theme-toggle';
const themeStorageKey = 'NTComponentsStoredThemeKey';
const contrastStorageKey = 'NTComponentsStoredContrastKey';
const prefersDark = 'DARK';
const prefersLight = 'LIGHT';
const prefersSystem = 'SYSTEM';
const contrastDefault = 'DEFAULT';
const contrastMedium = 'MEDIUM';
const contrastHigh = 'HIGH';
const toggleTagName = 'nt-theme-toggle';
const selectSelector = 'select.nt-theme-select';
const iconSelector = 'span.nt-theme-toggle-icon';
const themeLinkSelector = 'link[data-nt-theme]';

function getNTComponents(): NTComponentsWindow | undefined {
    return (window as Window & { NTComponents?: NTComponentsWindow }).NTComponents;
}

function getCustomAttribute(): string {
    return getNTComponents()?.customAttribute ?? defaultCustomAttribute;
}

class NTThemeToggleElement extends HTMLElement {
    static observedAttributes = [getCustomAttribute()];

    private readonly themeContrastSelectedHandler: (event: Event) => Promise<void>;
    private syncPromise: Promise<void> | null = null;

    constructor() {
        super();
        this.themeContrastSelectedHandler = this.themeContrastSelected.bind(this);
    }

    get runtime(): NTThemeRuntime | undefined {
        return getNTComponents()?.NTThemeRuntime;
    }

    get themeCssMap(): Record<string, Record<string, string>> {
        const config = this.runtime?.getConfiguration(this) ?? {};
        return {
            LIGHT: {
                DEFAULT: config.lightDefaultCss ?? 'light.css',
                MEDIUM: config.lightMediumCss ?? 'light-mc.css',
                HIGH: config.lightHighCss ?? 'light-hc.css'
            },
            DARK: {
                DEFAULT: config.darkDefaultCss ?? 'dark.css',
                MEDIUM: config.darkMediumCss ?? 'dark-mc.css',
                HIGH: config.darkHighCss ?? 'dark-hc.css'
            }
        };
    }

    connectedCallback(): void {
        this.registerWithRuntime();
        void this.syncFromRuntime();
    }

    disconnectedCallback(): void {
        this.runtime?.unregisterControl(this);
        this.getSelect()?.removeEventListener('change', this.themeContrastSelectedHandler);
    }

    getConfiguration(): NTThemeConfiguration {
        return this.runtime?.getConfiguration(this) ?? {};
    }

    getStoredTheme(): string | null {
        return this.runtime?.getStoredTheme(this.getConfiguration()) ?? null;
    }

    setStoredTheme(theme: string): void {
        const config = this.getConfiguration();
        this.runtime?.safeSetStorage(config.themeStorageKey ?? themeStorageKey, this.validateTheme(theme));
    }

    getStoredContrast(): string | null {
        return this.runtime?.getStoredContrast(this.getConfiguration()) ?? null;
    }

    setStoredContrast(contrast: string): void {
        const config = this.getConfiguration();
        this.runtime?.safeSetStorage(config.contrastStorageKey ?? contrastStorageKey, this.validateContrast(contrast));
    }

    validateTheme(theme: string | null | undefined): string {
        return this.runtime?.normalizeTheme(theme, prefersSystem) ?? prefersSystem;
    }

    validateContrast(contrast: string | null | undefined): string {
        return this.runtime?.normalizeContrast(contrast, contrastDefault) ?? contrastDefault;
    }

    cleanupInvalidStoredValues(): void {
        this.runtime?.cleanupInvalidStoredValues(this.getConfiguration());
    }

    systemPrefersDark(): boolean {
        return this.runtime?.systemPrefersDark() ?? false;
    }

    getThemesRoot(): string {
        const themesRoot = this.getConfiguration().themesRoot ?? '/Themes';
        return themesRoot.endsWith('/') ? themesRoot : `${themesRoot}/`;
    }

    removeCriticalThemeStyles(): void {
        this.runtime?.removeCriticalThemeStyles();
    }

    async updateThemeAttributes(): Promise<string> {
        if (!this.runtime) {
            return prefersLight;
        }

        const result = await this.runtime.apply({ element: this, waitForLoad: true });
        return result.theme;
    }

    updateIcon(currentTheme: string): void {
        const iconElement = this.querySelector<HTMLElement>(iconSelector);
        if (iconElement) {
            iconElement.textContent = currentTheme === prefersDark ? 'light_mode' : 'dark_mode';
        }
    }

    async initSelect(currentTheme: string, options: { skipApply?: boolean } = {}): Promise<void> {
        const config = this.getConfiguration();
        const theme = this.getStoredTheme() ?? config.defaultTheme ?? prefersSystem;
        const contrast = this.getStoredContrast() ?? config.defaultContrast ?? contrastDefault;
        const combinedValue = `${this.validateTheme(theme)}-${this.validateContrast(contrast)}`;
        const comboSelect = this.getSelect();

        if (!comboSelect) {
            return;
        }

        comboSelect.removeEventListener('change', this.themeContrastSelectedHandler);
        comboSelect.addEventListener('change', this.themeContrastSelectedHandler);

        const fallbackValues = [`${this.validateTheme(theme)}-DEFAULT`, 'LIGHT-DEFAULT'];
        const valueToSelect = [combinedValue, ...fallbackValues].find(value => this.hasOptionValue(comboSelect, value)) ?? combinedValue;

        comboSelect.value = valueToSelect;

        for (const option of comboSelect.options) {
            const isSelected = option.value === valueToSelect;

            if (option.selected !== isSelected) {
                option.selected = isSelected;
            }

            if (isSelected && option.getAttribute('selected') !== 'true') {
                option.setAttribute('selected', 'true');
            }
            else if (!isSelected && option.hasAttribute('selected')) {
                option.removeAttribute('selected');
            }
        }

        if (!options.skipApply && currentTheme) {
            this.updateIcon(currentTheme);
        }
    }

    getFallbackCss(): string {
        return this.runtime?.getFallbackCss() ?? '';
    }

    injectFallbackStyles(): void {
        this.runtime?.injectFallbackStyles();
    }

    async updateThemeLink(cssHref: string, exists = true): Promise<HTMLLinkElement | Element | null> {
        if (!this.runtime) {
            return null;
        }

        if (!exists) {
            const current = document.querySelector(themeLinkSelector);

            if (!current) {
                this.injectFallbackStyles();
                this.removeCriticalThemeStyles();
            }

            return current;
        }

        return await this.runtime.applyStylesheet(new URL(cssHref, window.location.href).href, { waitForLoad: true });
    }

    async themeContrastSelected(event: Event): Promise<void> {
        const select = event.target instanceof HTMLSelectElement ? event.target : null;
        if (!select) {
            return;
        }

        const [rawTheme = prefersSystem, rawContrast = contrastDefault] = select.value.split('-');
        const result = await this.runtime?.apply({
            element: this,
            theme: this.validateTheme(rawTheme),
            contrast: this.validateContrast(rawContrast),
            waitForLoad: true
        });

        if (result && !this.isRegisteredWithRuntime()) {
            this.updateIcon(result.theme);
            await this.initSelect(result.theme, { skipApply: true });
        }
    }

    attributeChangedCallback(name: string): void {
        if (name !== getCustomAttribute()) {
            return;
        }

        this.cleanupInvalidStoredValues();
        void this.syncFromRuntime();
    }

    private getSelect(): HTMLSelectElement | null {
        return this.querySelector<HTMLSelectElement>(selectSelector);
    }

    private hasOptionValue(select: HTMLSelectElement, value: string): boolean {
        for (const option of select.options) {
            if (option.value === value) {
                return true;
            }
        }

        return false;
    }

    isRegisteredWithRuntime(): boolean {
        return this.runtime?.controls instanceof Set && this.runtime.controls.has(this);
    }

    registerWithRuntime(): void {
        this.runtime?.registerControl(this);
    }

    async syncFromRuntime(): Promise<void> {
        if (!this.runtime) {
            return;
        }

        if (this.syncPromise) {
            await this.syncPromise;
            return;
        }

        this.registerWithRuntime();
        this.syncPromise = (async () => {
            const currentTheme = await this.updateThemeAttributes();
            this.updateIcon(currentTheme);
            await this.initSelect(currentTheme, { skipApply: true });
        })();

        try {
            await this.syncPromise;
        }
        finally {
            this.syncPromise = null;
        }
    }
}

function getThemeToggleElement(element?: Element | null): NTThemeToggleElement | null {
    if (isThemeToggleElement(element)) {
        return element;
    }

    const previousElement = element?.previousElementSibling;
    if (isThemeToggleElement(previousElement)) {
        return previousElement;
    }

    return null;
}

function isThemeToggleElement(element?: Element | null): element is NTThemeToggleElement {
    return element?.tagName?.toLowerCase() === toggleTagName
        && typeof (element as NTThemeToggleElement).updateThemeAttributes === 'function';
}

function defineThemeToggleElement(): void {
    if (!customElements.get(toggleTagName)) {
        customElements.define(toggleTagName, NTThemeToggleElement);
    }
}

export function onLoad(element?: Element | null): void {
    defineThemeToggleElement();

    getThemeToggleElement(element)?.registerWithRuntime();
}

export function onUpdate(element?: Element | null): void {
    const themeToggle = getThemeToggleElement(element);
    if (themeToggle) {
        void themeToggle.syncFromRuntime();
    }
}

export function onDispose(element?: Element | null): void {
    const themeToggle = getThemeToggleElement(element);
    if (themeToggle && !themeToggle.isConnected) {
        themeToggle.runtime?.unregisterControl(themeToggle);
    }
}

export const __testHooks = {
    contrastDefault,
    contrastHigh,
    contrastMedium,
    contrastStorageKey,
    defineThemeToggleElement,
    getCustomAttribute,
    getThemeToggleElement,
    NTThemeToggleElement,
    prefersDark,
    prefersLight,
    prefersSystem,
    themeStorageKey
};
