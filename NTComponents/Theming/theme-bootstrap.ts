(() => {
    const rootWindow = window as NTThemeHostWindow;
    const permanentThemeElementIds = [
        'nt-theme-critical',
        'nt-theme-default-light',
        'nt-theme-default-dark',
        'nt-theme-active-slot',
        'nt-theme-pending-slot',
    ];

    if (rootWindow.__ntThemeBootstrapped) {
        return;
    }

    rootWindow.__ntThemeBootstrapped = true;

    const runtime = rootWindow.NTComponents?.NTThemeRuntime;
    if (!runtime) {
        return;
    }

    const enhancedNavigationState = rootWindow.__ntThemeEnhancedNavigationState ??= { preserveHeadAttributes: false };
    if (!rootWindow.__ntThemeDomParserPatched) {
        const parseFromString = DOMParser.prototype.parseFromString;
        DOMParser.prototype.parseFromString = function (input: string, format: DOMParserSupportedType): Document {
            const parsedDocument = parseFromString.call(this, input, format);
            const state = rootWindow.__ntThemeEnhancedNavigationState;
            if (!state?.preserveHeadAttributes || format !== 'text/html' || !parsedDocument.getElementById('nt-theme-config')) {
                return parsedDocument;
            }

            state.preserveHeadAttributes = false;
            for (const id of permanentThemeElementIds) {
                const currentElement = document.getElementById(id);
                const responseElement = parsedDocument.getElementById(id);
                if (!currentElement || !responseElement || currentElement.tagName !== responseElement.tagName) {
                    continue;
                }

                for (const attribute of Array.from(responseElement.attributes)) {
                    responseElement.removeAttribute(attribute.name);
                }
                for (const attribute of Array.from(currentElement.attributes)) {
                    responseElement.setAttribute(attribute.name, attribute.value);
                }
            }

            return parsedDocument;
        };
        rootWindow.__ntThemeDomParserPatched = true;
    }

    const restoreOrApplyTheme = () => {
        enhancedNavigationState.preserveHeadAttributes = false;
        runtime.restoreThemeState?.({ waitForLoad: false });
        if (!runtime.hasCurrentThemeState?.()) {
            runtime.apply({ waitForLoad: false });
        }
    };
    restoreOrApplyTheme();

    // WebAssembly can connect controls before the head script initializes the runtime.
    document.querySelectorAll<HTMLElement & { syncFromRuntime?: () => Promise<void> }>('nt-theme-toggle').forEach(control => {
        void control.syncFromRuntime?.();
    });

    const registerEnhancedNavigationHandlers = () => {
        rootWindow.Blazor?.addEventListener?.('enhancednavigationstart', () => enhancedNavigationState.preserveHeadAttributes = true);
        rootWindow.Blazor?.addEventListener?.('enhancedload', restoreOrApplyTheme);
        rootWindow.Blazor?.addEventListener?.('enhancednavigationend', () => enhancedNavigationState.preserveHeadAttributes = false);
    };

    if (rootWindow.Blazor?.addEventListener) {
        registerEnhancedNavigationHandlers();
    } else {
        document.addEventListener('DOMContentLoaded', registerEnhancedNavigationHandlers, { once: true });
    }
})();
