(() => {
    const rootWindow = window as NTThemeHostWindow;
    const ntComponents = rootWindow.NTComponents = rootWindow.NTComponents || {};

    if (ntComponents.NTThemeRuntime) {
        return;
    }

    const validThemes = new Set<NTThemePreference>(['DARK', 'LIGHT', 'SYSTEM']);
    const validContrasts = new Set<NTThemeContrastName>(['DEFAULT', 'MEDIUM', 'HIGH']);
    const defaultConfig: NTThemeConfiguration = {
        themeStorageKey: 'NTComponentsStoredThemeKey',
        contrastStorageKey: 'NTComponentsStoredContrastKey',
        defaultTheme: 'SYSTEM',
        defaultContrast: 'DEFAULT',
        themesRoot: '/Themes',
        lightDefaultCss: 'light.css',
        lightMediumCss: 'light-mc.css',
        lightHighCss: 'light-hc.css',
        darkDefaultCss: 'dark.css',
        darkMediumCss: 'dark-mc.css',
        darkHighCss: 'dark-hc.css',
    };
    const themeStateElementId = 'nt-theme-state';
    const activeThemeSlotId = 'nt-theme-active-slot';
    const pendingThemeSlotId = 'nt-theme-pending-slot';

    const fallbackCss = ':root{--nt-color-primary:rgb(84 90 146);--nt-color-surface-tint:rgb(84 90 146);--nt-color-on-primary:rgb(255 255 255);--nt-color-primary-container:rgb(224 224 255);--nt-color-on-primary-container:rgb(60 66 121);--nt-color-secondary:rgb(92 93 114);--nt-color-on-secondary:rgb(255 255 255);--nt-color-secondary-container:rgb(225 224 249);--nt-color-on-secondary-container:rgb(68 69 89);--nt-color-tertiary:rgb(120 83 107);--nt-color-on-tertiary:rgb(255 255 255);--nt-color-tertiary-container:rgb(255 215 239);--nt-color-on-tertiary-container:rgb(94 60 83);--nt-color-error:rgb(186 26 26);--nt-color-on-error:rgb(255 255 255);--nt-color-error-container:rgb(255 218 214);--nt-color-on-error-container:rgb(147 0 10);--nt-color-background:rgb(251 248 255);--nt-color-on-background:rgb(27 27 33);--nt-color-surface:rgb(251 248 255);--nt-color-on-surface:rgb(27 27 33);--nt-color-surface-variant:rgb(227 225 236);--nt-color-on-surface-variant:rgb(70 70 79);--nt-color-outline:rgb(119 118 128);--nt-color-outline-variant:rgb(199 197 208);--nt-color-shadow:rgb(0 0 0);--nt-color-scrim:rgb(0 0 0);--nt-color-inverse-surface:rgb(48 48 54);--nt-color-inverse-on-surface:rgb(242 239 247);--nt-color-inverse-primary:rgb(189 194 255);--nt-color-primary-fixed:rgb(224 224 255);--nt-color-on-primary-fixed:rgb(15 21 75);--nt-color-primary-fixed-dim:rgb(189 194 255);--nt-color-on-primary-fixed-variant:rgb(60 66 121);--nt-color-secondary-fixed:rgb(225 224 249);--nt-color-on-secondary-fixed:rgb(24 26 44);--nt-color-secondary-fixed-dim:rgb(196 196 221);--nt-color-on-secondary-fixed-variant:rgb(68 69 89);--nt-color-tertiary-fixed:rgb(255 215 239);--nt-color-on-tertiary-fixed:rgb(46 17 38);--nt-color-tertiary-fixed-dim:rgb(231 185 213);--nt-color-on-tertiary-fixed-variant:rgb(94 60 83);--nt-color-surface-dim:rgb(219 217 224);--nt-color-surface-bright:rgb(251 248 255);--nt-color-surface-container-lowest:rgb(255 255 255);--nt-color-surface-container-low:rgb(245 242 250);--nt-color-surface-container:rgb(239 237 244);--nt-color-surface-container-high:rgb(234 231 239);--nt-color-surface-container-highest:rgb(228 225 233);--nt-color-info:rgb(67 94 145);--nt-color-on-info:rgb(255 255 255);--nt-color-info-container:rgb(215 226 255);--nt-color-on-info-container:rgb(42 70 119);--nt-color-success:rgb(49 106 66);--nt-color-on-success:rgb(255 255 255);--nt-color-success-container:rgb(179 241 190);--nt-color-on-success-container:rgb(22 81 44);--nt-color-warning:rgb(111 93 13);--nt-color-on-warning:rgb(255 255 255);--nt-color-warning-container:rgb(251 225 134);--nt-color-on-warning-container:rgb(85 69 0);--nt-color-assert:rgb(124 78 126);--nt-color-on-assert:rgb(255 255 255);--nt-color-assert-container:rgb(255 214 252);--nt-color-on-assert-container:rgb(98 55 101);}';

    let listening = false;
    let mediaQueryList: MediaQueryList | null = null;
    let activeElementConfig: Partial<NTThemeConfiguration> = {};
    const controls = new Set<NTThemeControl>();

    const upper = (value: unknown): string => typeof value === 'string' ? value.trim().toUpperCase() : '';
    const safeGetStorage = (key: string): string | null => {
        try {
            return rootWindow.localStorage?.getItem(key) ?? null;
        } catch {
            return null;
        }
    };
    const safeSetStorage = (key: string, value: string): void => {
        try {
            rootWindow.localStorage?.setItem(key, value);
        } catch {
            // Theme still applies even when persistence is unavailable.
        }
    };
    const safeRemoveStorage = (key: string): void => {
        try {
            rootWindow.localStorage?.removeItem(key);
        } catch {
            // Storage may be unavailable under browser privacy settings.
        }
    };

    const readJsonConfig = (): Partial<NTThemeConfiguration> => {
        const configElement = document.getElementById('nt-theme-config');
        if (!configElement?.textContent) {
            return {};
        }

        try {
            return JSON.parse(configElement.textContent) as Partial<NTThemeConfiguration>;
        } catch {
            return {};
        }
    };
    const getThemeStateElement = (): HTMLScriptElement | null => document.getElementById(themeStateElementId) as HTMLScriptElement | null;
    const ensureThemeStateElement = (): HTMLScriptElement => {
        let stateElement = getThemeStateElement();

        if (!stateElement) {
            stateElement = document.createElement('script');
            stateElement.type = 'application/json';
            stateElement.id = themeStateElementId;
            stateElement.setAttribute('data-permanent', themeStateElementId);
            document.head.appendChild(stateElement);
        }

        return stateElement;
    };
    const readThemeState = (): Partial<NTThemeResult> => {
        const stateElement = getThemeStateElement();
        if (!stateElement?.textContent) {
            return {};
        }

        try {
            const state: unknown = JSON.parse(stateElement.textContent);
            return state && typeof state === 'object' ? state as Partial<NTThemeResult> : {};
        } catch {
            return {};
        }
    };
    const writeThemeState = (result: NTThemeResult): HTMLScriptElement => {
        const stateElement = ensureThemeStateElement();
        stateElement.textContent = JSON.stringify({
            themePreference: result.themePreference,
            theme: result.theme,
            contrast: result.contrast,
            href: result.href,
        });
        return stateElement;
    };

    const readAttribute = (element: Element | null | undefined, ntName: string, legacyName: string): string | undefined => element?.getAttribute(ntName) || element?.getAttribute(legacyName) || undefined;
    const getElementConfig = (element?: Element | null): Partial<NTThemeConfiguration> => {
        if (!element) {
            return {};
        }

        return {
            themesRoot: readAttribute(element, 'nt-themes-root', 'tnt-themes-root'),
            lightDefaultCss: readAttribute(element, 'nt-light-default', 'tnt-light-default'),
            lightMediumCss: readAttribute(element, 'nt-light-medium', 'tnt-light-medium'),
            lightHighCss: readAttribute(element, 'nt-light-high', 'tnt-light-high'),
            darkDefaultCss: readAttribute(element, 'nt-dark-default', 'tnt-dark-default'),
            darkMediumCss: readAttribute(element, 'nt-dark-medium', 'tnt-dark-medium'),
            darkHighCss: readAttribute(element, 'nt-dark-high', 'tnt-dark-high'),
            defaultTheme: readAttribute(element, 'nt-default-theme', 'tnt-default-theme'),
            defaultContrast: readAttribute(element, 'nt-default-contrast', 'tnt-default-contrast'),
        } as Partial<NTThemeConfiguration>;
    };

    const cleanConfig = <T extends object>(config: T): Partial<T> => Object.fromEntries(Object.entries(config).filter(([, value]) => value !== undefined && value !== null && value !== '')) as Partial<T>;
    const getConfiguration = (element?: Element | null): NTThemeConfiguration => {
        const elementConfig = cleanConfig(getElementConfig(element));
        if (element && Object.keys(elementConfig).length > 0) {
            activeElementConfig = elementConfig;
        }

        const configured = {
            ...defaultConfig,
            ...cleanConfig(rootWindow.NTComponentsThemeConfig || {}),
            ...cleanConfig(readJsonConfig()),
            ...(element ? elementConfig : activeElementConfig),
        };

        configured.defaultTheme = normalizeTheme(configured.defaultTheme, defaultConfig.defaultTheme);
        configured.defaultContrast = normalizeContrast(configured.defaultContrast, defaultConfig.defaultContrast);
        return configured;
    };

    function normalizeTheme(theme: unknown, fallback: NTThemePreference = 'SYSTEM'): NTThemePreference {
        const normalized = upper(theme);
        return validThemes.has(normalized as NTThemePreference) ? normalized as NTThemePreference : fallback;
    }
    function normalizeContrast(contrast: unknown, fallback: NTThemeContrastName = 'DEFAULT'): NTThemeContrastName {
        const normalized = upper(contrast);
        return validContrasts.has(normalized as NTThemeContrastName) ? normalized as NTThemeContrastName : fallback;
    }
    const systemPrefersDark = (): boolean => rootWindow.matchMedia?.('(prefers-color-scheme: dark)').matches === true;
    const resolveActualTheme = (theme: NTThemePreference): NTThemeName => theme === 'SYSTEM' ? (systemPrefersDark() ? 'DARK' : 'LIGHT') : theme;
    // Reads one stored value, removing it when it is present but invalid; returns the normalized valid value or null.
    const readStoredValue = <T extends string>(key: string, valid: Set<T>, removeInvalid: boolean): T | null => {
        const stored = safeGetStorage(key);
        const normalized = upper(stored);
        if (valid.has(normalized as T)) {
            return normalized as T;
        }

        if (stored && removeInvalid) {
            safeRemoveStorage(key);
        }

        return null;
    };
    const getStoredTheme = (config: NTThemeConfiguration): NTThemePreference | null => readStoredValue(config.themeStorageKey, validThemes, false);
    const getStoredContrast = (config: NTThemeConfiguration): NTThemeContrastName | null => readStoredValue(config.contrastStorageKey, validContrasts, false);
    const cleanupInvalidStoredValues = (config: NTThemeConfiguration): void => {
        readStoredValue(config.themeStorageKey, validThemes, true);
        readStoredValue(config.contrastStorageKey, validContrasts, true);
    };

    const hasScheme = (value: string): boolean => /^[a-z][a-z0-9+.-]*:/i.test(value);
    const safeFileName = (value: unknown, fallback: string): string => {
        const fileName = typeof value === 'string' ? value.trim() : '';
        return fileName && !hasScheme(fileName) && !fileName.startsWith('//') && !fileName.startsWith('/') ? fileName : fallback;
    };
    const safeThemesRoot = (root: unknown): string => {
        const value = typeof root === 'string' && root.trim() ? root.trim() : defaultConfig.themesRoot;

        if (value.startsWith('//')) {
            return defaultConfig.themesRoot;
        }

        try {
            const url = new URL(value.endsWith('/') ? value : `${value}/`, rootWindow.location.href);
            return url.origin === rootWindow.location.origin ? url.href : new URL(defaultConfig.themesRoot, rootWindow.location.href).href;
        } catch {
            return new URL(defaultConfig.themesRoot, rootWindow.location.href).href;
        }
    };

    type NTThemeCssKey = 'lightDefaultCss' | 'lightMediumCss' | 'lightHighCss' | 'darkDefaultCss' | 'darkMediumCss' | 'darkHighCss';
    const cssFileKeys: Record<NTThemeName, Record<NTThemeContrastName, NTThemeCssKey>> = {
        LIGHT: { DEFAULT: 'lightDefaultCss', MEDIUM: 'lightMediumCss', HIGH: 'lightHighCss' },
        DARK: { DEFAULT: 'darkDefaultCss', MEDIUM: 'darkMediumCss', HIGH: 'darkHighCss' },
    };
    // Only the requested file name is validated; the default-contrast file is the fallback.
    const getCssFile = (config: NTThemeConfiguration, theme: NTThemeName, contrast: NTThemeContrastName): string => {
        const keys = cssFileKeys[theme];
        const cssFile = (key: NTThemeCssKey | undefined): string => key ? safeFileName(config[key], defaultConfig[key]) : '';
        return cssFile(keys[contrast]) || cssFile(keys.DEFAULT) || defaultConfig.lightDefaultCss;
    };

    const resolveThemeHref = (config: NTThemeConfiguration, theme: NTThemeName, contrast: NTThemeContrastName): string => {
        const rootUrl = safeThemesRoot(config.themesRoot);
        const cssUrl = new URL(getCssFile(config, theme, contrast), rootUrl);
        return cssUrl.origin === rootWindow.location.origin ? cssUrl.href : new URL(defaultConfig.lightDefaultCss, safeThemesRoot(defaultConfig.themesRoot)).href;
    };

    const resetThemeLink = (link: HTMLLinkElement): void => {
        link.removeAttribute('rel');
        link.removeAttribute('href');
        link.removeAttribute('media');
        link.removeAttribute('as');
        link.removeAttribute('data-nt-theme');
        link.removeAttribute('data-tnt-theme');
        link.removeAttribute('data-nt-theme-default');
        link.removeAttribute('data-tnt-theme-default');
        link.removeAttribute('data-nt-theme-pending');
        link.removeAttribute('data-nt-theme-loaded');
        link.removeAttribute('data-tnt-theme-loaded');
        link.setAttribute('data-permanent', link.id);
    };
    const deactivateDefaultThemeLink = (link: HTMLLinkElement): void => {
        link.rel = 'stylesheet';
        link.media = 'not all';
        link.removeAttribute('as');
        link.removeAttribute('data-nt-theme');
        link.removeAttribute('data-tnt-theme');
        link.removeAttribute('data-nt-theme-default');
        link.removeAttribute('data-tnt-theme-default');
        link.removeAttribute('data-nt-theme-pending');
        link.removeAttribute('data-nt-theme-loaded');
        link.removeAttribute('data-tnt-theme-loaded');
        link.setAttribute('data-permanent', link.id);
    };
    const releaseThemeLink = (link?: HTMLLinkElement | null): void => {
        if (link?.id === 'nt-theme-default-light' || link?.id === 'nt-theme-default-dark') {
            deactivateDefaultThemeLink(link);
        } else if (link?.id === activeThemeSlotId || link?.id === pendingThemeSlotId) {
            resetThemeLink(link);
        } else {
            link?.remove();
        }
    };
    const removeCriticalThemeStyles = (): void => {
        document.querySelectorAll<HTMLStyleElement>('style[data-tnt-theme-critical],style[data-nt-theme-critical]').forEach(style => {
            if (style.id === 'nt-theme-critical') {
                style.removeAttribute('data-tnt-theme-critical');
                style.removeAttribute('data-nt-theme-critical');
                style.textContent = '';
            } else {
                style.remove();
            }
        });
    };
    const removeFallbackStyles = (): void => document.querySelector('style[data-tnt-theme],style[data-nt-theme-fallback]')?.remove();
    const removeDefaultThemeStylesheets = (): void => document.querySelectorAll<HTMLLinkElement>('link[data-nt-theme-default],link[data-tnt-theme-default]').forEach(releaseThemeLink);
    const injectFallbackStyles = (): void => {
        let style = document.head.querySelector<HTMLStyleElement>('style[data-nt-theme-fallback]');
        if (!style) {
            style = document.createElement('style');
            style.setAttribute('data-nt-theme-fallback', 'true');
            style.setAttribute('data-tnt-theme', 'true');
            style.setAttribute('data-permanent', '');
            document.head.appendChild(style);
        }

        style.textContent = fallbackCss;
    };

    const waitForLink = (link?: HTMLLinkElement | null): Promise<NTThemeLinkStatus> => {
        if (!link) {
            return Promise.resolve('error');
        }

        if (link.getAttribute('data-nt-theme-loaded') === 'true' || link.getAttribute('data-tnt-theme-loaded') === 'true' || link.sheet) {
            link.setAttribute('data-nt-theme-loaded', 'true');
            link.setAttribute('data-tnt-theme-loaded', 'true');
            return Promise.resolve('true');
        }

        return new Promise<NTThemeLinkStatus>(resolve => {
            const complete = (status: NTThemeLinkStatus): void => {
                link.setAttribute('data-nt-theme-loaded', status);
                link.setAttribute('data-tnt-theme-loaded', status);
                resolve(status);
            };

            link.addEventListener('load', () => complete('true'), { once: true });
            link.addEventListener('error', () => complete('error'), { once: true });
        });
    };

    const markThemeLink = (link: HTMLLinkElement): void => {
        link.rel = 'stylesheet';
        link.removeAttribute('as');
        link.removeAttribute('media');
        link.removeAttribute('data-nt-theme-default');
        link.removeAttribute('data-tnt-theme-default');
        link.removeAttribute('data-nt-theme-pending');
        link.setAttribute('data-nt-theme', 'true');
        link.setAttribute('data-tnt-theme', 'true');
        link.setAttribute('data-nt-theme-loaded', 'false');
        link.setAttribute('data-tnt-theme-loaded', 'false');
        link.setAttribute('data-permanent', link.id);
    };

    const findCurrentThemeLink = (): HTMLLinkElement | null => document.head.querySelector<HTMLLinkElement>('link[data-nt-theme],link[data-tnt-theme]');
    const ensureThemeSlot = (id: string): HTMLLinkElement => {
        const existing = document.getElementById(id);
        if (existing instanceof HTMLLinkElement) {
            return existing;
        }

        existing?.remove();
        const link = document.createElement('link');
        link.id = id;
        link.setAttribute('data-permanent', id);
        document.head.appendChild(link);
        return link;
    };
    const findMatchingDefaultThemeLink = (href: string): HTMLLinkElement | undefined => Array.from(document.head.querySelectorAll<HTMLLinkElement>('link[data-nt-theme-default],link[data-tnt-theme-default]'))
        .find(link => new URL(link.href, rootWindow.location.href).href === href);
    const findAvailableThemeSlot = (current: HTMLLinkElement | null): HTMLLinkElement => {
        const activeSlot = ensureThemeSlot(activeThemeSlotId);
        return activeSlot === current ? ensureThemeSlot(pendingThemeSlotId) : activeSlot;
    };
    const releaseOtherActiveThemeLinks = (active: HTMLLinkElement): void => document.head.querySelectorAll<HTMLLinkElement>('link[data-nt-theme],link[data-tnt-theme]').forEach(link => {
        if (link !== active) {
            releaseThemeLink(link);
        }
    });
    const applyStylesheet = async (href: string, options: NTThemeStylesheetOptions = {}): Promise<HTMLLinkElement> => {
        const waitForLoad = options.waitForLoad !== false;
        const current = findCurrentThemeLink();
        const currentHref = current ? new URL(current.href, rootWindow.location.href).href : null;
        const dispatchThemeFailed = (detail: { href: string; reason: string }): boolean => document.dispatchEvent(new CustomEvent('nt-theme-failed', { detail }));
        const finishLinkLoad = async (link: HTMLLinkElement): Promise<NTThemeLinkStatus> => {
            const status = await waitForLink(link);

            if (status === 'true') {
                removeCriticalThemeStyles();
                removeFallbackStyles();
                removeDefaultThemeStylesheets();
            } else {
                dispatchThemeFailed({ href: link.href, reason: 'stylesheet-error' });
                injectFallbackStyles();
            }

            return status;
        };

        if (current && currentHref === href) {
            releaseOtherActiveThemeLinks(current);
            if (waitForLoad) {
                await finishLinkLoad(current);
            } else {
                finishLinkLoad(current);
            }

            return current;
        }

        if (!current) {
            const link = findMatchingDefaultThemeLink(href) || ensureThemeSlot(activeThemeSlotId);
            markThemeLink(link);
            link.href = href;
            releaseOtherActiveThemeLinks(link);

            if (waitForLoad) {
                await finishLinkLoad(link);
            } else {
                finishLinkLoad(link);
            }

            return link;
        }

        const pending = findAvailableThemeSlot(current);
        resetThemeLink(pending);
        // Set the href before rel so the browser never sees a preload link without a URL.
        pending.href = href;
        pending.as = 'style';
        pending.rel = 'preload';
        pending.setAttribute('data-nt-theme-pending', 'true');
        pending.setAttribute('data-nt-theme-loaded', 'false');
        pending.setAttribute('data-tnt-theme-loaded', 'false');
        pending.setAttribute('data-permanent', pending.id);
        current.after(pending);

        const promotePending = (status: NTThemeLinkStatus): HTMLLinkElement => {
            if (status === 'true') {
                releaseThemeLink(current);
                pending.rel = 'stylesheet';
                pending.removeAttribute('as');
                pending.removeAttribute('data-nt-theme-pending');
                markThemeLink(pending);
                pending.setAttribute('data-nt-theme-loaded', 'true');
                pending.setAttribute('data-tnt-theme-loaded', 'true');
                releaseOtherActiveThemeLinks(pending);
                removeCriticalThemeStyles();
                removeFallbackStyles();
                removeDefaultThemeStylesheets();
                return pending;
            }

            const pendingHref = pending.href;
            releaseThemeLink(pending);
            dispatchThemeFailed({ href: pendingHref, reason: 'stylesheet-error' });
            return current;
        };

        if (!waitForLoad) {
            waitForLink(pending).then(promotePending);
            return pending;
        }

        return promotePending(await waitForLink(pending));
    };
    const normalizeStateHref = (href: unknown): string | null => {
        if (typeof href !== 'string' || !href.trim()) {
            return null;
        }

        try {
            const url = new URL(href, rootWindow.location.href);
            return url.origin === rootWindow.location.origin ? url.href : null;
        } catch {
            return null;
        }
    };
    const restoreThemeState = (options: NTThemeStylesheetOptions = {}): HTMLLinkElement | Promise<HTMLLinkElement> | null => {
        const href = normalizeStateHref(readThemeState().href);
        if (!href) {
            return null;
        }

        const current = findCurrentThemeLink();
        const currentHref = current ? new URL(current.href, rootWindow.location.href).href : null;

        if (currentHref === href) {
            return current;
        }

        return applyStylesheet(href, { waitForLoad: options.waitForLoad });
    };
    const hasCurrentThemeState = (): boolean => {
        const href = normalizeStateHref(readThemeState().href);
        const activeThemes = document.head.querySelectorAll<HTMLLinkElement>('link[data-nt-theme],link[data-tnt-theme]');
        if (!href || activeThemes.length !== 1) {
            return false;
        }

        return new URL(activeThemes[0].href, rootWindow.location.href).href === href;
    };

    const syncControls = (result: NTThemeResult): void => {
        controls.forEach(control => {
            control.updateIcon?.(result.theme);
            control.initSelect?.(result.theme, { skipApply: true });
        });
    };
    const dispatchThemeChanged = (result: NTThemeResult): void => {
        document.dispatchEvent(new CustomEvent('nt-theme-changed', { detail: result }));
        document.dispatchEvent(new CustomEvent('tnt-theme-changed', { detail: result }));
    };
    const resolveThemeState = (config: NTThemeConfiguration): NTThemeResult => {
        cleanupInvalidStoredValues(config);
        const themePreference = normalizeTheme(getStoredTheme(config) || config.defaultTheme, defaultConfig.defaultTheme);
        const contrast = normalizeContrast(getStoredContrast(config) || config.defaultContrast, defaultConfig.defaultContrast);
        const theme = resolveActualTheme(themePreference);
        return { themePreference, theme, contrast, href: resolveThemeHref(config, theme, contrast) };
    };

    const apply = async (options: NTThemeApplyOptions = {}): Promise<NTThemeResult> => {
        const config = getConfiguration(options.element);

        if (options.theme) {
            safeSetStorage(config.themeStorageKey, normalizeTheme(options.theme, config.defaultTheme));
        }

        if (options.contrast) {
            safeSetStorage(config.contrastStorageKey, normalizeContrast(options.contrast, config.defaultContrast));
        }

        const result = resolveThemeState(config);
        writeThemeState(result);
        await applyStylesheet(result.href, { waitForLoad: options.waitForLoad });
        syncControls(result);
        dispatchThemeChanged(result);
        ensureListeners();
        return result;
    };

    function ensureListeners(): void {
        if (listening) {
            return;
        }

        listening = true;

        if (rootWindow.matchMedia) {
            mediaQueryList = rootWindow.matchMedia('(prefers-color-scheme: dark)');
            const handleMediaChange = () => {
                const config = getConfiguration();
                const storedTheme = normalizeTheme(getStoredTheme(config) || config.defaultTheme, config.defaultTheme);
                if (storedTheme === 'SYSTEM') {
                    apply({ waitForLoad: true });
                }
            };
            if (mediaQueryList.addEventListener) {
                mediaQueryList.addEventListener('change', handleMediaChange);
            } else {
                mediaQueryList.addListener?.(handleMediaChange);
            }
        }

        rootWindow.addEventListener?.('storage', event => {
            const config = getConfiguration();
            if (event.key === config.themeStorageKey || event.key === config.contrastStorageKey) {
                apply({ waitForLoad: true });
            }
        });
    }

    ntComponents.NTThemeRuntime = {
        apply,
        applyStylesheet,
        cleanupInvalidStoredValues,
        controls,
        defaultConfig,
        fallbackCss,
        getConfiguration,
        getCssFile,
        getFallbackCss: () => fallbackCss,
        getStoredContrast,
        getStoredTheme,
        hasCurrentThemeState,
        readThemeState,
        injectFallbackStyles,
        normalizeContrast,
        normalizeTheme,
        registerControl: (element: NTThemeControl) => controls.add(element),
        removeCriticalThemeStyles,
        removeDefaultThemeStylesheets,
        restoreThemeState,
        resolveThemeHref,
        safeSetStorage,
        systemPrefersDark,
        unregisterControl: (element: NTThemeControl) => controls.delete(element),
        writeThemeState,
    };
})();
