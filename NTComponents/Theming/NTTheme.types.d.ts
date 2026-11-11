type NTThemeName = 'DARK' | 'LIGHT';
type NTThemePreference = NTThemeName | 'SYSTEM';
type NTThemeContrastName = 'DEFAULT' | 'MEDIUM' | 'HIGH';
type NTThemeLinkStatus = 'true' | 'error';

interface NTThemeConfiguration {
    themeStorageKey: string;
    contrastStorageKey: string;
    defaultTheme: NTThemePreference;
    defaultContrast: NTThemeContrastName;
    themesRoot: string;
    lightDefaultCss: string;
    lightMediumCss: string;
    lightHighCss: string;
    darkDefaultCss: string;
    darkMediumCss: string;
    darkHighCss: string;
}

interface NTThemeResult {
    themePreference: NTThemePreference;
    theme: NTThemeName;
    contrast: NTThemeContrastName;
    href: string;
}

interface NTThemeStylesheetOptions {
    waitForLoad?: boolean;
}

interface NTThemeApplyOptions extends NTThemeStylesheetOptions {
    element?: Element | null;
    theme?: unknown;
    contrast?: unknown;
}

interface NTThemeControl {
    updateIcon?: (theme: NTThemeName) => void;
    initSelect?: (theme: NTThemeName, options: { skipApply: boolean }) => void;
}

interface NTThemeRuntimeBridge {
    apply: (options?: NTThemeApplyOptions) => Promise<NTThemeResult>;
    hasCurrentThemeState?: () => boolean;
    restoreThemeState?: (options?: NTThemeStylesheetOptions) => HTMLLinkElement | Promise<HTMLLinkElement> | null;
    [name: string]: unknown;
}

interface NTThemeComponentsGlobals {
    NTThemeRuntime?: NTThemeRuntimeBridge;
}

type NTThemeHostWindow = Window & {
    NTComponents?: NTThemeComponentsGlobals;
    NTComponentsThemeConfig?: Partial<NTThemeConfiguration>;
    __ntThemeBootstrapped?: boolean;
    __ntThemeEnhancedNavigationState?: { preserveHeadAttributes: boolean };
    __ntThemeDomParserPatched?: boolean;
    Blazor?: {
        addEventListener?: (eventName: string, listener: () => void) => void;
    };
};
