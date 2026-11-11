const pageScriptInfoBySrc = new Map();
const enhancedLoadBlazorInstances = new WeakSet();
const richTextEditorToolRegistry = {
    tools: new Map(),
    onChange: null
};
let rippleHandlersRegistered = false;
const rippleReleaseTimeoutByHost = new WeakMap();
// Hosts that may still hold ripple nodes, so global releases avoid scanning every host in the document.
const activeRippleHosts = new Set();
const registeredButtonInteractions = new WeakSet();

export function setRichTextEditorToolRegistryChangedCallback(callback) {
    richTextEditorToolRegistry.onChange = callback;
}

export function registerRichTextEditorTool(tool) {
    richTextEditorToolRegistry.tools.set(tool.command, tool);
    richTextEditorToolRegistry.onChange?.();
    return tool;
}

export function getRichTextEditorTool(command) {
    return richTextEditorToolRegistry.tools.get(command) ?? null;
}

export function getRichTextEditorToolState(editorState, tool) {
    const existingState = editorState.toolStates.get(tool.command);
    if (existingState) {
        return existingState;
    }

    const state = tool.createState();
    editorState.toolStates.set(tool.command, state);
    return state;
}

export function createRichTextEditorToolContext(element, editorState, host, tool) {
    return {
        element,
        editorState,
        host,
        toolState: getRichTextEditorToolState(editorState, tool)
    };
}

function registerPageScriptElement(src, element) {
    if (!src) {
        throw new Error('Must provide a non-empty value for the "src" attribute.');
    }

    let pageScriptInfo = pageScriptInfoBySrc.get(src);

    if (pageScriptInfo) {
        pageScriptInfo.referenceCount++;
        pageScriptInfo.elements.add(element);
        pageScriptInfo.module?.onUpdate?.(element);
    } else {
        pageScriptInfo = { referenceCount: 1, module: null, elements: new Set([element]) };
        pageScriptInfoBySrc.set(src, pageScriptInfo);
        initializePageScriptModule(src, pageScriptInfo);
    }
}

function unregisterPageScriptElement(src, element) {
    if (!src) {
        return;
    }

    const pageScriptInfo = pageScriptInfoBySrc.get(src);
    if (!pageScriptInfo) {
        return;
    }

    pageScriptInfo.referenceCount--;
    pageScriptInfo.elements.delete(element);
    pageScriptInfo.module?.onDispose?.(element);
}

async function initializePageScriptModule(src, pageScriptInfo) {
    // If the path is relative, normalize it by by making it an absolute URL
    // with document's the base HREF.
    if (src.startsWith("./")) {
        src = new URL(src.substr(2), document.baseURI).toString();
    }

    const module = await import(src);

    if (pageScriptInfo.referenceCount <= 0) {
        // All page-script elements with the same 'src' were
        // unregistered while we were loading the module.
        return;
    }

    pageScriptInfo.module = module;
    for (const element of pageScriptInfo.elements) {
        module.onLoad?.(element);
        module.onUpdate?.(element);
    }
}

function onEnhancedLoad() {
    // Start by invoking 'onDispose' on any modules that are no longer referenced.
    for (const [src, { module, referenceCount }] of pageScriptInfoBySrc) {
        if (referenceCount <= 0) {
            module?.onDispose?.();
            pageScriptInfoBySrc.delete(src);
        }
    }

    // Then invoke 'onUpdate' on the remaining modules.
    for (const { elements, module } of pageScriptInfoBySrc.values()) {
        for (const element of elements) {
            module?.onUpdate?.(element);
        }
    }

    window.NTComponents?.setupRipple?.();
    enhanceAutocompleteInputs();
    enhanceAnimationWrappers();
}

function enhanceAutocompleteInputs() {
    if (!document.querySelector('[data-nt-autocomplete-input="true"]')) {
        return;
    }

    import('./Form/NTAutocomplete.razor.js')
        .then(module => module.enhanceAll?.(document))
        .catch(() => { });
}

function enhanceAnimationWrappers() {
    if (!document.querySelector('[data-nt-animation-trigger="visible"]')) {
        return;
    }

    import('./Animation/NTAnimation.razor.js')
        .then(module => module.enhanceAll?.(document))
        .catch(() => { });
}

function getRippleHost(element) {
    for (const child of element?.children ?? []) {
        if (child.classList.contains('nt-button-ripple-host')) {
            return child;
        }
    }

    return element?.querySelector?.('.nt-navigation-rail-item-ripple.nt-button-ripple-host') ?? null;
}

function clearRippleReleaseTimeout(host) {
    const existingTimeout = rippleReleaseTimeoutByHost.get(host);
    if (existingTimeout) {
        clearTimeout(existingTimeout);
        rippleReleaseTimeoutByHost.delete(host);
    }
}

function releaseRippleHost(host) {
    if (!host) {
        return;
    }

    clearRippleReleaseTimeout(host);

    const ripples = [];
    for (const child of host.children) {
        if (child.classList.contains('nt-button-ripple')) {
            ripples.push(child);
        }
    }

    if (!ripples.length) {
        activeRippleHosts.delete(host);
        return;
    }

    for (const ripple of ripples) {
        ripple.classList.add('nt-button-ripple-releasing');
    }

    const timeoutId = setTimeout(() => {
        for (const ripple of ripples) {
            ripple.remove();
        }
        rippleReleaseTimeoutByHost.delete(host);
        activeRippleHosts.delete(host);
    }, 600);

    rippleReleaseTimeoutByHost.set(host, timeoutId);
}

function releaseAllRippleHosts() {
    for (const host of activeRippleHosts) {
        if (host.isConnected) {
            releaseRippleHost(host);
        } else {
            activeRippleHosts.delete(host);
        }
    }
}

// Creates a ripple at the pointer position of the event, or centered when no pointer event is given.
function createRipple(element, pointerEvent) {
    const host = getRippleHost(element);
    if (!host || element.disabled) {
        return false;
    }

    const container = host.classList.contains('nt-navigation-rail-item-ripple') ? host.parentElement ?? element : element;
    const width = container.offsetWidth || 0;
    const height = container.offsetHeight || 0;
    let x = width / 2;
    let y = height / 2;
    if (pointerEvent) {
        const coords = getCoords(container);
        x = (pointerEvent.pageX ?? (pointerEvent.clientX + window.scrollX)) - coords.left;
        y = (pointerEvent.pageY ?? (pointerEvent.clientY + window.scrollY)) - coords.top;
    }

    clearRippleReleaseTimeout(host);

    const rippleElement = document.createElement('span');
    rippleElement.classList.add('nt-button-ripple');
    rippleElement.style.pointerEvents = 'none';
    rippleElement.style.setProperty('--nt-button-ripple-origin-x', `${x}px`);
    rippleElement.style.setProperty('--nt-button-ripple-origin-y', `${y}px`);
    rippleElement.style.setProperty('--nt-button-ripple-width', `${width * 2}px`);
    rippleElement.style.setProperty('--nt-button-ripple-height', `${height * 2}px`);
    host.appendChild(rippleElement);
    activeRippleHosts.add(host);

    void rippleElement.offsetWidth;

    setTimeout(() => {
        rippleElement.classList.add('nt-button-ripple-active');
    }, 1);

    return true;
}

function findRippleElementFromEventTarget(target) {
    return target?.closest?.('.nt-ripple') ?? null;
}

function isKeyboardRippleEvent(event) {
    return !event.repeat && (event.key === ' ' || event.key === 'Enter' || event.key === 'Spacebar');
}

function handleDelegatedRipplePointerDown(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (element) {
        createRipple(element, event);
    }
}

function handleDelegatedRipplePointerUp(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (element) {
        releaseRippleHost(getRippleHost(element));
    } else {
        releaseAllRippleHosts();
    }
}

function handleDelegatedRippleKeyDown(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (element && isKeyboardRippleEvent(event)) {
        createRipple(element, null);
    }
}

function handleDelegatedRippleKeyUp(event) {
    const element = findRippleElementFromEventTarget(event.target);
    if (element && isKeyboardRippleEvent(event)) {
        releaseRippleHost(getRippleHost(element));
    }
}

function ensureRippleHandlers() {
    if (rippleHandlersRegistered) {
        return;
    }

    rippleHandlersRegistered = true;
    if (window.PointerEvent) {
        document.addEventListener('pointerdown', handleDelegatedRipplePointerDown);
        document.addEventListener('pointerup', handleDelegatedRipplePointerUp);
        document.addEventListener('pointercancel', handleDelegatedRipplePointerUp);
        document.addEventListener('pointerleave', handleDelegatedRipplePointerUp);
    } else {
        document.addEventListener('mousedown', handleDelegatedRipplePointerDown);
        document.addEventListener('mouseup', handleDelegatedRipplePointerUp);
        document.addEventListener('mouseleave', handleDelegatedRipplePointerUp);
    }

    document.addEventListener('keydown', handleDelegatedRippleKeyDown);
    document.addEventListener('keyup', handleDelegatedRippleKeyUp);
    window.addEventListener('blur', releaseAllRippleHosts);
}

function setPressedShape(element, isPressed) {
    element?.classList?.toggle('nt-button--pressed-shape', isPressed);
}

// Shared listeners for every registered button interaction; currentTarget is the registered element.
function onButtonPointerDown(event) {
    setPressedShape(event.currentTarget, true);
    createRipple(event.currentTarget, event);
}

function onButtonPointerUp(event) {
    releaseRippleHost(getRippleHost(event.currentTarget));
}

function onButtonKeyDown(event) {
    if (isKeyboardRippleEvent(event)) {
        setPressedShape(event.currentTarget, true);
        createRipple(event.currentTarget, null);
    }
}

function onButtonKeyUp(event) {
    if (isKeyboardRippleEvent(event)) {
        onButtonPointerExit(event);
    }
}

function onButtonPointerExit(event) {
    setPressedShape(event.currentTarget, false);
    releaseRippleHost(getRippleHost(event.currentTarget));
}

function registerButtonInteraction(element) {
    if (!element || registeredButtonInteractions.has(element)) {
        return;
    }

    if (window.PointerEvent) {
        element.addEventListener('pointerdown', onButtonPointerDown);
        element.addEventListener('pointerup', onButtonPointerUp);
        element.addEventListener('pointercancel', onButtonPointerExit);
        element.addEventListener('pointerleave', onButtonPointerExit);
    } else {
        element.addEventListener('mousedown', onButtonPointerDown);
        element.addEventListener('mouseup', onButtonPointerUp);
        element.addEventListener('mouseleave', onButtonPointerExit);
        element.addEventListener('touchstart', onButtonPointerDown, { passive: true });
        element.addEventListener('touchend', onButtonPointerExit);
        element.addEventListener('touchcancel', onButtonPointerExit);
    }

    element.addEventListener('keydown', onButtonKeyDown);
    element.addEventListener('keyup', onButtonKeyUp);
    element.addEventListener('blur', onButtonPointerExit);
    registeredButtonInteractions.add(element);
}

function registerButtonInteractions(element) {
    if (!element) {
        return;
    }

    const buttonSelector = '.nt-button, .nt-icon-button, .nt-btn-grp-btn, .nt-split-button-segment';

    if (element.matches?.(buttonSelector)) {
        registerButtonInteraction(element);
    }

    element.querySelectorAll?.(buttonSelector).forEach((button) => {
        registerButtonInteraction(button);
    });
}

function registerRippleHost(host) {
    registerButtonInteraction(host?.closest?.('button, a, [role="button"], [tabindex]') ?? host?.parentElement ?? null);
}

// Inline component scripts call these; the registration goes through window.NTComponents and retries briefly in case it is not attached yet.
function startRegistration(script, registrationName) {
    const element = script?.previousElementSibling;
    if (!element) {
        return;
    }

    let attempts = 0;
    const tryRegister = () => {
        if (typeof window.NTComponents?.[registrationName] === 'function') {
            window.NTComponents[registrationName](element);
            return;
        }

        if (attempts++ < 20) {
            setTimeout(tryRegister, 0);
        }
    };

    tryRegister();
}

function startButtonInteraction(script) {
    startRegistration(script, 'registerButtonInteractions');
}

function startRippleHost(script) {
    startRegistration(script, 'registerRippleHost');
}

function setupPageScriptElement() {
    if (customElements.get('nt-page-script')) {
        return;
    }

    customElements.define('nt-page-script', class extends HTMLElement {
        static observedAttributes = ['src'];

        pendingUnregisterTimeout = null;
        registeredSrc = null;

        connectedCallback() {
            this.cancelPendingUnregister();
            this.registerSrc(this.src);
        }

        attributeChangedCallback(name, oldValue, newValue) {
            if (name !== 'src') {
                return;
            }

            this.src = newValue;

            if (!this.isConnected || oldValue === newValue) {
                return;
            }

            this.unregisterSrc(oldValue);
            this.registerSrc(newValue);
        }

        disconnectedCallback() {
            this.scheduleUnregister(this.registeredSrc);
        }

        registerSrc(src) {
            this.cancelPendingUnregister();

            if (!src || this.registeredSrc === src) {
                return;
            }

            if (this.registeredSrc) {
                this.unregisterSrc(this.registeredSrc);
            }

            this.registeredSrc = src;
            registerPageScriptElement(src, this);
        }

        cancelPendingUnregister() {
            if (this.pendingUnregisterTimeout === null) {
                return;
            }

            clearTimeout(this.pendingUnregisterTimeout);
            this.pendingUnregisterTimeout = null;
        }

        scheduleUnregister(src) {
            this.cancelPendingUnregister();

            if (!src) {
                return;
            }

            this.pendingUnregisterTimeout = setTimeout(() => {
                this.pendingUnregisterTimeout = null;
                if (!this.isConnected) {
                    this.unregisterSrc(src);
                }
            }, 0);
        }

        unregisterSrc(src) {
            this.cancelPendingUnregister();

            if (!src || this.registeredSrc !== src) {
                return;
            }

            unregisterPageScriptElement(src, this);
            this.registeredSrc = null;
        }
    });
}

function setupEnhancedNavigation(blazor) {
    if (!blazor?.addEventListener || enhancedLoadBlazorInstances.has(blazor)) {
        return;
    }

    enhancedLoadBlazorInstances.add(blazor);
    blazor.addEventListener('enhancedload', onEnhancedLoad);
}

function syncInputCounter(input, counter) {
    if (!input || !counter) {
        return;
    }

    const maxLength = input.maxLength > 0 ? input.maxLength : input.getAttribute('maxlength');
    if (!maxLength) {
        return;
    }

    const text = `${input.value?.length ?? 0}/${maxLength}`;
    counter.textContent = text;
    counter.setAttribute('aria-label', `Character count ${text}`);
}

export function updateInputCounter(input) {
    const root = input?.closest?.('.nt-input');
    const counter = root?.querySelector?.(':scope .nt-input-counter');
    syncInputCounter(input, counter);
}

function getTextAreaMaxVisibleLines(input) {
    const value = parseInt(input?.dataset?.ntTextareaMaxVisibleLines ?? '', 10);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function getTextAreaMinVisibleLines(input) {
    const value = parseInt(input?.dataset?.ntTextareaMinVisibleLines ?? '', 10);
    return Number.isFinite(value) && value > 0 ? value : null;
}

function parseCssPixelValue(value) {
    const parsed = parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}

function getTextAreaLineHeight(style) {
    const parsed = parseCssPixelValue(style.lineHeight);
    if (parsed > 0) {
        return parsed;
    }

    const fontSize = parseCssPixelValue(style.fontSize);
    return fontSize > 0 ? fontSize * 1.2 : 20;
}

function getTextAreaVerticalInset(style) {
    return parseCssPixelValue(style.paddingTop)
        + parseCssPixelValue(style.paddingBottom)
        + parseCssPixelValue(style.borderTopWidth)
        + parseCssPixelValue(style.borderBottomWidth);
}

function clearTextAreaAutoGrowInput(input) {
    if (!(input instanceof HTMLTextAreaElement)) {
        return;
    }

    input.style.removeProperty('height');
    input.style.removeProperty('min-height');
    input.style.removeProperty('overflow-y');
    input.closest?.('.nt-input')?.classList?.remove('nt-textarea-enhanced');
}

export function autoGrowTextArea(input) {
    if (!(input instanceof HTMLTextAreaElement) || input.dataset?.ntTextareaAutogrow !== 'true') {
        clearTextAreaAutoGrowInput(input);
        return;
    }

    input.closest?.('.nt-input')?.classList?.add('nt-textarea-enhanced');
    const style = window.getComputedStyle(input);
    const lineHeight = getTextAreaLineHeight(style);
    const verticalInset = getTextAreaVerticalInset(style);
    const minVisibleLines = getTextAreaMinVisibleLines(input);
    const maxVisibleLines = getTextAreaMaxVisibleLines(input);
    const minHeight = minVisibleLines === null ? 0 : Math.ceil(lineHeight * minVisibleLines + verticalInset);
    input.style.height = 'auto';
    input.style.minHeight = '0px';
    const scrollHeight = input.scrollHeight;
    input.style.minHeight = minHeight > 0 ? `${minHeight}px` : '';

    if (maxVisibleLines === null) {
        input.style.height = `${Math.ceil(Math.max(scrollHeight, minHeight))}px`;
        input.style.overflowY = 'hidden';
        return;
    }

    const maxHeight = Math.ceil(lineHeight * maxVisibleLines + verticalInset);
    input.style.height = `${Math.ceil(Math.max(Math.min(scrollHeight, maxHeight), minHeight))}px`;
    input.style.overflowY = scrollHeight > maxHeight ? 'scroll' : 'hidden';
}

function getInputCurrencySymbol(input) {
    const symbol = input?.getAttribute?.('currencySymbol');
    if (symbol) {
        return symbol;
    }

    let cultureCode = input?.getAttribute?.('cultureCode');
    if (!cultureCode) {
        cultureCode = 'en-US';
    }

    let currencyCode = input?.getAttribute?.('currencyCode');
    if (!currencyCode) {
        currencyCode = 'USD';
    }

    try {
        const formatter = new Intl.NumberFormat(cultureCode, {
            style: 'currency',
            currency: currencyCode,
            currencyDisplay: 'narrowSymbol'
        });
        const currencyPart = formatter.formatToParts(0).find(part => part.type === 'currency');
        return currencyPart?.value || currencyCode;
    } catch {
        return currencyCode;
    }
}

function getCurrencyInputOptions(input) {
    const decimalDigits = parseInt(input?.getAttribute?.('currencyDecimalDigits') ?? '2', 10);
    return {
        currencyCode: input?.getAttribute?.('currencyCode') || 'USD',
        decimalDigits: Number.isNaN(decimalDigits) ? 2 : decimalDigits,
        decimalSeparator: input?.getAttribute?.('currencyDecimalSeparator') || '.',
        groupSeparator: input?.getAttribute?.('currencyGroupSeparator') || ',',
        symbol: getInputCurrencySymbol(input)
    };
}

function limitCurrencyFraction(value, options) {
    if (!value || !options.decimalSeparator) {
        return value;
    }

    const decimalIndex = value.indexOf(options.decimalSeparator);
    if (decimalIndex < 0) {
        return value;
    }

    if (options.decimalDigits <= 0) {
        return value.substring(0, decimalIndex);
    }

    const fractionalStart = decimalIndex + options.decimalSeparator.length;
    const maximumLength = fractionalStart + options.decimalDigits;
    return value.length <= maximumLength ? value : value.substring(0, maximumLength);
}

function replaceAllLiteral(value, searchValue, replacementValue) {
    if (!searchValue) {
        return value;
    }

    return value.split(searchValue).join(replacementValue);
}

function toCurrencyDecimalValue(value, options) {
    if (!value) {
        return '';
    }

    let normalized = limitCurrencyFraction(value, options);
    normalized = replaceAllLiteral(normalized, options.symbol, '');
    normalized = replaceAllLiteral(normalized, options.currencyCode, '');
    normalized = normalized.replace(/\s|\u00a0/g, '');
    normalized = replaceAllLiteral(normalized, options.groupSeparator, '');
    if (options.decimalSeparator !== '.') {
        normalized = replaceAllLiteral(normalized, options.decimalSeparator, '.');
    }

    normalized = normalized.replace(/(?!^-)[^0-9.-]/g, '');
    const decimalIndex = normalized.indexOf('.');
    if (decimalIndex >= 0) {
        normalized = `${normalized.substring(0, decimalIndex + 1)}${normalized.substring(decimalIndex + 1).replace(/\./g, '')}`;
    }

    return normalized;
}

function syncCurrencyFormValue(input, options) {
    const container = input?.closest?.('.nt-input-control-container');
    const formValueInput = container?.querySelector?.('input[type="hidden"][name]');
    if (!formValueInput) {
        return;
    }

    formValueInput.value = toCurrencyDecimalValue(input.value, options);
}

export function prefixCurrencyInput(input) {
    if (!input) {
        return;
    }

    const options = getCurrencyInputOptions(input);
    if (input.disabled || input.readOnly) {
        syncCurrencyFormValue(input, options);
        return;
    }

    if (!input.value) {
        syncCurrencyFormValue(input, options);
        return;
    }

    const selectionStart = input.selectionStart;
    const selectionEnd = input.selectionEnd;
    let offset = 0;

    if (options.symbol && !input.value.startsWith(options.symbol)) {
        input.value = `${options.symbol}${input.value}`;
        offset += options.symbol.length;
    }

    const limitedValue = limitCurrencyFraction(input.value, options);
    if (limitedValue !== input.value) {
        input.value = limitedValue;
    }

    syncCurrencyFormValue(input, options);

    if (selectionStart !== null && selectionEnd !== null) {
        input.setSelectionRange(selectionStart + offset, selectionEnd + offset);
    }
}

function getPhoneMask(input) {
    return input?.getAttribute?.('phoneMask') || '(###) ###-####';
}

function countPhoneMaskDigits(mask) {
    return (mask.match(/#/g) || []).length;
}

function getPhoneCountryCodeDigitCount(mask) {
    const trimmedMask = (mask || '').trimStart();
    if (!trimmedMask.startsWith('+')) {
        return 0;
    }

    let digitCount = 0;
    for (const character of trimmedMask.substring(1)) {
        if (character !== '#') {
            break;
        }

        digitCount++;
    }

    return digitCount;
}

export function formatPhoneValue(value, mask = '(###) ###-####') {
    if (!value || !mask || !mask.includes('#')) {
        return value || '';
    }

    const maximumDigits = countPhoneMaskDigits(mask);
    const digits = value.replace(/\D/g, '').substring(0, maximumDigits);
    if (!digits) {
        return '';
    }

    let formatted = '';
    let digitIndex = 0;
    for (const character of mask) {
        if (character === '#') {
            if (digitIndex >= digits.length) {
                break;
            }

            formatted += digits[digitIndex];
            digitIndex++;
            continue;
        }

        if (digitIndex >= digits.length && formatted.length > 0) {
            break;
        }

        formatted += character;
    }

    return formatted;
}

export function normalizePhoneValue(value, mask = '(###) ###-####') {
    if (!value || !mask || !mask.includes('#')) {
        return value || '';
    }

    const maximumDigits = countPhoneMaskDigits(mask);
    const digits = value.replace(/\D/g, '').substring(0, maximumDigits);
    if (!digits) {
        return '';
    }

    const countryCodeDigitCount = getPhoneCountryCodeDigitCount(mask);
    return countryCodeDigitCount > 0 && digits.length > countryCodeDigitCount
        ? `${digits.substring(0, countryCodeDigitCount)} ${digits.substring(countryCodeDigitCount)}`
        : digits;
}

function syncPhoneFormValue(input) {
    const container = input?.closest?.('.nt-input-control-container');
    const formValueInput = container?.querySelector?.('input[type="hidden"][name]');
    if (!formValueInput) {
        return;
    }

    formValueInput.value = normalizePhoneValue(input.value, getPhoneMask(input));
}

export function applyPhoneMaskInput(input) {
    if (!input) {
        return;
    }

    if (input.disabled || input.readOnly) {
        syncPhoneFormValue(input);
        return;
    }

    const formatted = formatPhoneValue(input.value, getPhoneMask(input));
    if (formatted === input.value) {
        syncPhoneFormValue(input);
        return;
    }

    input.value = formatted;
    syncPhoneFormValue(input);
    if (input.selectionStart !== null && input.selectionEnd !== null) {
        input.setSelectionRange(input.value.length, input.value.length);
    }
}

// Drag-to-scroll: mouse users can drag a [data-nt-drag-scroll] container sideways. Touch and pen already pan natively.
// One set of document listeners serves every container, so it works for static SSR and interactive rendering alike.
const dragScrollThreshold = 5;
const dragScrollIgnoredTargets = 'input, textarea, select, [contenteditable]:not([contenteditable="false"])';
let dragScroll = null;
let dragScrollSuppressClick = false;
let dragScrollReady = false;

function onDragScrollPointerDown(event) {
    // Every press starts fresh, so a release the page never saw cannot leave an old press blocking native dragging.
    dragScroll?.scroller.removeAttribute('data-nt-dragging');
    dragScroll = null;
    if (event.pointerType !== 'mouse' || event.button !== 0 || !(event.target instanceof Element)) {
        return;
    }

    const scroller = event.target.closest('[data-nt-drag-scroll]');
    if (!scroller || scroller.scrollWidth <= scroller.clientWidth || event.target.closest(dragScrollIgnoredTargets)) {
        return;
    }

    // Presses on the scrollbar keep the native scrollbar drag.
    const rect = scroller.getBoundingClientRect();
    const x = event.clientX - rect.left - scroller.clientLeft;
    const y = event.clientY - rect.top - scroller.clientTop;
    if (x < 0 || x >= scroller.clientWidth || y < 0 || y >= scroller.clientHeight) {
        return;
    }

    dragScroll = { scroller, pointerId: event.pointerId, startX: event.clientX, scrollLeft: scroller.scrollLeft, dragging: false };
}

function onDragScrollPointerMove(event) {
    if (dragScroll?.pointerId !== event.pointerId) {
        return;
    }

    const deltaX = event.clientX - dragScroll.startX;
    if (!dragScroll.dragging) {
        // Small movements stay clicks, so buttons, links and row clicks keep working.
        if (Math.abs(deltaX) < dragScrollThreshold) {
            return;
        }

        dragScroll.dragging = true;
        dragScroll.scroller.setPointerCapture?.(event.pointerId);
        dragScroll.scroller.setAttribute('data-nt-dragging', '');
        window.getSelection?.()?.removeAllRanges();
    }

    dragScroll.scroller.scrollLeft = dragScroll.scrollLeft - deltaX;
}

function onDragScrollPointerEnd(event) {
    if (dragScroll?.pointerId !== event.pointerId) {
        return;
    }

    if (dragScroll.dragging) {
        dragScroll.scroller.removeAttribute('data-nt-dragging');
        // The browser fires click right after pointerup; swallow it so the element under the pointer ignores the drag.
        dragScrollSuppressClick = event.type === 'pointerup';
        setTimeout(() => dragScrollSuppressClick = false);
    }

    dragScroll = null;
}

function onDragScrollClick(event) {
    if (dragScrollSuppressClick) {
        dragScrollSuppressClick = false;
        event.preventDefault();
        event.stopImmediatePropagation();
    }
}

// Marks containers that can scroll sideways so CSS shows a grab cursor. CSS cannot tell whether content overflows, and the
// cursor only matters while hovering, so this checks when the pointer enters the container or moves within it.
function onDragScrollPointerOver(event) {
    const scroller = event.target instanceof Element ? event.target.closest('[data-nt-drag-scroll]') : null;
    scroller?.toggleAttribute('data-nt-can-drag', scroller.scrollWidth > scroller.clientWidth);
}

// Native link and image dragging would cancel the pointer stream mid-scroll; explicitly draggable content keeps it.
function onDragScrollDragStart(event) {
    if (dragScroll && !(event.target instanceof Element && event.target.closest('[draggable="true"]'))) {
        event.preventDefault();
    }
}

function setupDragScroll() {
    if (dragScrollReady) {
        return;
    }

    dragScrollReady = true;
    document.addEventListener('pointerover', onDragScrollPointerOver);
    document.addEventListener('pointerdown', onDragScrollPointerDown);
    document.addEventListener('pointermove', onDragScrollPointerMove);
    document.addEventListener('pointerup', onDragScrollPointerEnd);
    document.addEventListener('pointercancel', onDragScrollPointerEnd);
    document.addEventListener('dragstart', onDragScrollDragStart);
    // Capture on window runs before Blazor's and the target's own click handlers.
    window.addEventListener('click', onDragScrollClick, true);
}

// The listeners do not depend on Blazor, so attach them as soon as the module loads.
setupDragScroll();

// A static SSR NTFileUpload has no .NET handler to show the chosen files, so show their names here as the interactive
// component does. Only the static input is marked, so Blazor-rendered text is never touched.
function onStaticFileUploadChange(event) {
    const input = event.target;
    const display = input instanceof HTMLInputElement && input.matches('[data-nt-file-upload-static]')
        ? input.closest('.nt-file-upload')?.querySelector('.nt-file-upload-value')
        : null;
    if (!(display instanceof HTMLElement)) {
        return;
    }

    display.dataset.ntEmptyText ??= display.textContent ?? '';
    const names = Array.from(input.files ?? [], (file) => file.name);
    display.textContent = names.length ? names.join(', ') : display.dataset.ntEmptyText;
}

document.addEventListener('change', onStaticFileUploadChange);

function startBlazorComponents(blazor) {
    setupPageScriptElement();
    setupEnhancedNavigation(blazor);
    window.NTComponents?.setupRipple?.();
    enhanceAutocompleteInputs();
    enhanceAnimationWrappers();
}

export function afterWebStarted(blazor) {
    startBlazorComponents(blazor);
}

export function afterStarted(blazor) {
    startBlazorComponents(blazor);
}
function getCoords(elem) { // crossbrowser version
    var box = elem.getBoundingClientRect();

    var body = document.body;
    var docEl = document.documentElement;

    var scrollTop = window.scrollY || docEl.scrollTop || body.scrollTop;
    var scrollLeft = window.scrollX || docEl.scrollLeft || body.scrollLeft;

    var clientTop = docEl.clientTop || body.clientTop || 0;
    var clientLeft = docEl.clientLeft || body.clientLeft || 0;

    var top = box.top + scrollTop - clientTop;
    var left = box.left + scrollLeft - clientLeft;

    return { top: Math.round(top), left: Math.round(left) };
}

const isNumericInput = (event) => {
    const key = event.keyCode;
    return ((key >= 48 && key <= 57) || // Allow number line
        (key >= 96 && key <= 105) // Allow number pad
    );
};

const isModifierKey = (event) => {
    const key = event.keyCode;
    return (event.shiftKey === true || key === 35 || key === 36) || // Allow Shift, Home, End
        (key === 8 || key === 9 || key === 13 || key === 46) || // Allow Backspace, Tab, Enter, Delete
        (key > 36 && key < 41) || // Allow left, up, right, down
        (
            // Allow Ctrl/Command + A,C,V,X,Z
            (event.ctrlKey === true || event.metaKey === true) &&
            (key === 65 || key === 67 || key === 86 || key === 88 || key === 90)
        )
};

window.NTComponents = Object.assign(window.NTComponents || {}, {
    customAttribute: "ntid",
    getColorValueFromEnumName: function (colorName) {
        if (!colorName || typeof colorName !== 'string') return null;

        // Convert PascalCase or camelCase to kebab-case (e.g., 'OnPrimaryContainer' -> 'on-primary-container')
        const kebab = colorName.replace(/(?<=.)([A-Z])/g, '-$1').toLowerCase();
        // Compose the CSS variable name
        const rootStyle = getComputedStyle(document.documentElement);
        let value = rootStyle.getPropertyValue(`--nt-color-${kebab}`);
        if (!value) return null;
        value = value.trim();

        // If value is in rgb(X, Y, Z) or rgba(X, Y, Z, A) format (commas or spaces), convert to hex
        // Support both rgb(224,224,255) and rgb(224 224 255)
        const rgbRegex = /^rgb\s*\(\s*(\d{1,3})[ ,]+(\d{1,3})[ ,]+(\d{1,3})\s*\)$/i;
        const rgbaRegex = /^rgba\s*\(\s*(\d{1,3})[ ,]+(\d{1,3})[ ,]+(\d{1,3})[ ,]+(0|1|0?\.\d+)\s*\)$/i;
        let match = value.match(rgbRegex);
        if (match) {
            // Convert rgb to hex
            const r = parseInt(match[1], 10).toString(16).padStart(2, '0');
            const g = parseInt(match[2], 10).toString(16).padStart(2, '0');
            const b = parseInt(match[3], 10).toString(16).padStart(2, '0');
            return `#${r}${g}${b}`;
        }
        match = value.match(rgbaRegex);
        if (match) {
            // Convert rgba to hex (ignore alpha for hex, or append as 2-digit hex if needed)
            const r = parseInt(match[1], 10).toString(16).padStart(2, '0');
            const g = parseInt(match[2], 10).toString(16).padStart(2, '0');
            const b = parseInt(match[3], 10).toString(16).padStart(2, '0');
            // Optionally include alpha as hex
            // const a = Math.round(parseFloat(match[4]) * 255).toString(16).padStart(2, '0');
            return `#${r}${g}${b}`;
        }
        return value;
    },
    openModalDialog: (dialogId) => {
        const dialog = document.getElementById(dialogId);
        if (dialog) {
            dialog.showModal();

            dialog.addEventListener('cancel', e => {
                e.preventDefault();
                e.stopPropagation();
            });
        }
    },
    enableRipple: (element) => {
        function setRippleOffset(e) {
            const boundingRect = element.getBoundingClientRect();
            const x = e.clientX - boundingRect.left - (boundingRect.width / 2);
            const y = e.clientY - boundingRect.top - (boundingRect.height / 2);
            element.style.setProperty('--ripple-offset-x', `${x}px`);
            element.style.setProperty('--ripple-offset-y', `${y}px`);
        }

        if (element) {
            element.addEventListener('click', setRippleOffset);
        }
    },
    downloadFileFromStream: async (fileName, contentStreamReference) => {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        NTComponents.downloadFileFromBlob(fileName, blob);
    },
    downloadFromUrl: async (fileName, url) => {
        const blob = await fetch(url).then(r => r.blob())
        NTComponents.downloadFileFromBlob(fileName, blob);
    },
    downloadFileFromBlob: (fileName, blob) => {
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    },
    enforcePhoneFormat: (event) => {
        // Input must be of a valid number format or a modifier key, and not longer than ten digits
        if (!isNumericInput(event) && !isModifierKey(event)) {
            event.preventDefault();
        }
    },
    enforceCurrencyFormat: (event) => {
        // Input must be of a valid number format or a modifier key, and not longer than ten digits
        if (!isNumericInput(event) && !isModifierKey(event) && event.keyCode != 188 && event.keyCode != 190 && event.keyCode != 110) {
            event.preventDefault();
        }
    },
    formatToCurrency: (event) => {
        if (isModifierKey(event)) { return; }

        const numberRegex = new RegExp('[0-9.]', 'g');
        let numbers = '';
        let result;
        while ((result = numberRegex.exec(event.target.value)) != null) {
            numbers += result.toString();
        }

        let cultureCode = event.target.getAttribute('cultureCode');
        if (!cultureCode) {
            cultureCode = 'en-US';
        }

        let currencyCode = event.target.getAttribute('currencyCode');
        if (!currencyCode) {
            currencyCode = 'USD';
        }

        // Create our number formatter.
        const formatter = new Intl.NumberFormat(cultureCode, {
            style: 'currency',
            currency: currencyCode,
        });
        let formatted = formatter.format(numbers);
        if (!event.target.value.includes('.')) {
            formatted = formatted.substring(0, formatted.length - 3);
        }
        else {
            const cents = event.target.value.split('.')[1];
            formatted = formatted.substring(0, formatted.length - 3) + '.' + cents.substring(0, 2);
        }

        event.target.value = formatted;
    },
    formatToPhone: (event) => {
        if (isModifierKey(event)) { return; }
        event.target.value = formatPhoneValue(event.target.value, getPhoneMask(event.target));
    },
    getCurrentLocation: () => {
        return window.location.href;
    },
    updateUri: (newUri) => {
        history.replaceState(history.state, '', newUri);
    },
    setupRipple: () => {
        ensureRippleHandlers();

        document.querySelectorAll('.nt-button, .nt-icon-button').forEach((element) => {
            registerButtonInteraction(element);
        });
    },
    registerButtonInteraction,
    registerButtonInteractions,
    registerRippleHost,
    startButtonInteraction,
    startRippleHost,
    applyPhoneMaskInput,
    formatPhoneValue,
    normalizePhoneValue,
    prefixCurrencyInput,
    autoGrowTextArea,
    updateInputCounter,
    jumpToGridPage: (select) => {
        const href = select?.dataset?.ntDataGridPageHref;
        if (!href) {
            return;
        }

        const target = href.replace('{page}', encodeURIComponent(select.value));
        if (typeof window.Blazor?.navigateTo === 'function') {
            window.Blazor.navigateTo(target);
        }
        else {
            window.location.assign(target);
        }
    },
    stopEnter: (event) => {
        if (event.key === 'Enter') {
            // Prevent the default action (such as submitting a surrounding form)
            // in addition to stopping propagation so that selecting an item with Enter
            // in a typeahead does not cause the form to submit.
            if (typeof event.preventDefault === 'function') {
                event.preventDefault();
            }
            event.stopPropagation();
        }
    },
    stopEnterPropagation: (event) => {
        if (event.key === 'Enter') {
            // Preserve the default newline behavior while preventing parent handlers
            // from treating Enter inside multiline inputs as a submit/navigation key.
            event.stopPropagation();
        }
    },
    radioGroupKeyDownHandler: (event) => {
        const group = event.currentTarget;
        if (!group) return;

        const radios = Array.from(group.querySelectorAll('input[type="radio"]'));
        if (radios.length === 0) return;

        const isGroupDisabled = group.disabled || group.classList.contains('nt-disabled') || group.classList.contains('nt-radio-disabled') || !!group.closest?.('.nt-radio-disabled');
        const isGroupReadOnly = group.classList.contains('nt-readonly') || group.classList.contains('nt-radio-readonly') || !!group.closest?.('.nt-radio-readonly');
        const canUseRadio = (radio) => radio && !radio.disabled && !radio.readOnly && !isGroupDisabled && !isGroupReadOnly;
        const getOption = (radio) => radio?.closest?.('.nt-radio-option, .nt-radio-input');
        const clearHighlightedRadios = () => {
            group.querySelectorAll('.nt-radio-option-highlighted').forEach(option => {
                option.classList.remove('nt-radio-option-highlighted');
                option.removeAttribute('data-nt-radio-highlighted');
            });
        };
        const getHighlightedRadio = () => {
            const highlightedOption = group.querySelector('.nt-radio-option-highlighted');
            const highlightedRadio = highlightedOption?.querySelector?.('input[type="radio"]');
            if (highlightedRadio) return highlightedRadio;
            if (group.contains(document.activeElement) && document.activeElement?.matches?.('input[type="radio"]')) return document.activeElement;
            return radios.find(radio => radio.checked) ?? radios[0];
        };
        const highlightRadio = (radio) => {
            clearHighlightedRadios();

            const option = getOption(radio);
            option?.classList.add('nt-radio-option-highlighted');
            option?.setAttribute('data-nt-radio-highlighted', 'true');
            radio.focus?.();
        };
        const selectRadio = (radio) => {
            if (!canUseRadio(radio)) return false;
            radio.click();
            if (!radio.checked) {
                radio.checked = true;
                radio.dispatchEvent(new Event('change', { bubbles: true }));
            }
            clearHighlightedRadios();
            group.focus?.({ preventScroll: true });
            return true;
        };

        if (event.key >= '1' && event.key <= '9') {
            const radio = radios[parseInt(event.key, 10) - 1];
            if (selectRadio(radio)) {
                event.preventDefault();
            }
            return;
        }

        const navigationKeys = ['ArrowDown', 'ArrowRight', 'ArrowUp', 'ArrowLeft', 'Home', 'End'];
        if (navigationKeys.includes(event.key)) {
            const currentRadio = getHighlightedRadio();
            const currentIndex = Math.max(0, radios.indexOf(currentRadio));
            const nextIndex = event.key === 'Home'
                ? 0
                : event.key === 'End'
                    ? radios.length - 1
                    : event.key === 'ArrowDown' || event.key === 'ArrowRight'
                        ? (currentIndex + 1) % radios.length
                        : (currentIndex - 1 + radios.length) % radios.length;
            const nextRadio = radios[nextIndex];

            if (canUseRadio(nextRadio)) {
                highlightRadio(nextRadio);
                event.preventDefault();
            }
            return;
        }

        if (event.key === ' ' || event.key === 'Spacebar') {
            if (selectRadio(getHighlightedRadio())) {
                event.preventDefault();
            }
        }
    },
    onThemeChanged: (dotNetHelper) => {
        const callback = () => {
            dotNetHelper.invokeMethodAsync('OnThemeChanged');
        };
        document.addEventListener('nt-theme-changed', callback);
        return {
            dispose: () => document.removeEventListener('nt-theme-changed', callback)
        };
    }
});
