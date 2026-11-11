type Maybe<T> = T | null | undefined;

interface DotNetAutocompleteRef {
    invokeMethodAsync(methodName: 'NotifyAutocompleteValueChanged', value: string | null, closeMenu: boolean): Promise<unknown> | void;
    invokeMethodAsync(methodName: 'NotifyAutocompleteTouched'): Promise<unknown> | void;
}

interface AutocompleteOptionState {
    element: HTMLButtonElement;
    isCustom: boolean;
    labelElement: HTMLElement | null;
    listItem: HTMLElement | null;
    lowerLabel: string;
    lowerValue: string;
    originalIndex: number;
    trailing: HTMLElement | null;
    value: string;
}

interface AutocompleteIconDefinition {
    cssClass: string;
    icon: string;
    style?: string;
    title: string;
}

interface AutocompleteOptionDefinition {
    cssClass: string;
    customFormat?: string;
    disabled: boolean;
    group?: string;
    id?: string;
    isCustom: boolean;
    label: string;
    leadingIcon?: AutocompleteIconDefinition;
    selected: boolean;
    supportingText?: string;
    value: string;
}

interface AutocompleteState {
    activeElement: HTMLElement | null;
    activeIndex: number;
    documentListenersAttached: boolean;
    dotNetRef: DotNetAutocompleteRef | null;
    input: HTMLInputElement;
    isOpen: boolean;
    list: HTMLElement | null;
    menu: HTMLElement | null;
    onDocumentFocusIn: (event: FocusEvent) => void;
    onDocumentMouseDown: (event: MouseEvent) => void;
    onDocumentScroll: (event: Event) => void;
    onInputClick: (event: MouseEvent) => void;
    onInputBeforeInput: (event: InputEvent) => void;
    onInputInput: (event: Event) => void;
    onInputKeyDown: (event: KeyboardEvent) => void;
    onMenuClick: (event: MouseEvent) => void;
    onMenuMouseDown: (event: MouseEvent) => void;
    onWindowResize: (event: UIEvent) => void;
    options: AutocompleteOptionState[];
    optionSources: HTMLScriptElement[];
    query: string | null;
    root: HTMLElement;
    selectedValue: string | null;
}

const stateByInput = new Map<HTMLInputElement, AutocompleteState>();
const activeOptionClass = 'nt-combobox-option-active';
const menuLayerClass = 'nt-combobox-menu-layer';
const menuViewportMargin = 8;
const menuViewportOffset = 4;
const menuPreferredMaxHeight = 320;

function isDotNetObjectReference(value: unknown): value is DotNetAutocompleteRef {
    return typeof (value as DotNetAutocompleteRef | null)?.invokeMethodAsync === 'function';
}

function queryRoot(input: HTMLInputElement): HTMLElement {
    return input.closest<HTMLElement>('.nt-autocomplete') ?? input;
}

function queryOptions(root: HTMLElement): HTMLButtonElement[] {
    return Array.from(root.querySelectorAll<HTMLButtonElement>('.nt-combobox-list [data-nt-autocomplete-option="true"]'));
}

function getOptionState(option: HTMLButtonElement, index: number): AutocompleteOptionState {
    const label = option.dataset.ntAutocompleteLabel ?? option.textContent?.trim() ?? '';
    const value = option.dataset.ntAutocompleteValue ?? label;
    const lowerLabel = label.toLocaleLowerCase();
    const lowerValue = value.toLocaleLowerCase();
    const labelElement = option.querySelector<HTMLElement>('.nt-combobox-option-label');
    const isCustom = option.dataset.ntAutocompleteCustomOption === 'true';
    return {
        element: option,
        isCustom,
        labelElement,
        listItem: option.closest<HTMLElement>('.nt-combobox-list-item'),
        lowerLabel,
        lowerValue,
        originalIndex: index,
        trailing: option.querySelector<HTMLElement>('.nt-combobox-option-trailing'),
        value,
    };
}

function getOptionOrder(option: AutocompleteOptionState): number {
    const order = Number.parseInt(option.element.style.order || option.listItem?.style.order || '', 10);
    return Number.isFinite(order) ? order : option.originalIndex;
}

function getVisibleEnabledOptions(state: AutocompleteState): AutocompleteOptionState[] {
    return state.options
        .filter(option => option.listItem?.isConnected && !option.element.hidden && !option.element.disabled)
        .sort((left, right) => getOptionOrder(left) - getOptionOrder(right) || left.originalIndex - right.originalIndex);
}

function updateMenuPlacement(state: AutocompleteState): void {
    if (!state.menu || state.menu.hidden) {
        return;
    }

    const rootRect = state.root.getBoundingClientRect();
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    const maxViewportWidth = Math.max(0, viewportWidth - menuViewportMargin * 2);
    const width = Math.min(rootRect.width, maxViewportWidth);
    const left = Math.min(Math.max(rootRect.left, menuViewportMargin), Math.max(menuViewportMargin, viewportWidth - width - menuViewportMargin));
    const spaceBelow = Math.max(0, viewportHeight - rootRect.bottom - menuViewportMargin - menuViewportOffset);
    const spaceAbove = Math.max(0, rootRect.top - menuViewportMargin - menuViewportOffset);
    const openAbove = spaceBelow < menuPreferredMaxHeight && spaceAbove > spaceBelow;
    const availableSpace = openAbove ? spaceAbove : spaceBelow;
    const maxHeight = Math.min(menuPreferredMaxHeight, availableSpace);
    const height = Math.min(state.menu.scrollHeight || maxHeight, maxHeight);
    const top = openAbove ? rootRect.top - menuViewportOffset - height : rootRect.bottom + menuViewportOffset;

    state.menu.classList.toggle('nt-combobox-menu-above', openAbove);
    state.menu.classList.add(menuLayerClass);
    state.menu.style.left = `${left}px`;
    state.menu.style.maxHeight = `${maxHeight}px`;
    state.menu.style.top = `${Math.max(menuViewportMargin, top)}px`;
    state.menu.style.width = `${width}px`;
}

function showMenuSurface(state: AutocompleteState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.showPopover === 'function' && !isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.showPopover();
        }
        catch {
            // Already-open or unsupported popover transitions should not block the fallback positioned menu.
        }
    }
}

function hideMenuSurface(state: AutocompleteState): void {
    if (!state.menu) {
        return;
    }

    if (typeof state.menu.hidePopover === 'function' && isMenuPopoverOpen(state.menu)) {
        try {
            state.menu.hidePopover();
        }
        catch {
            // Best-effort cleanup only.
        }
    }

    state.menu.classList.remove(menuLayerClass, 'nt-combobox-menu-above');
    state.menu.style.removeProperty('left');
    state.menu.style.removeProperty('max-height');
    state.menu.style.removeProperty('top');
    state.menu.style.removeProperty('width');
}

function isMenuPopoverOpen(menu: HTMLElement): boolean {
    try {
        return menu.matches(':popover-open');
    }
    catch {
        return false;
    }
}

function scrollOptionIntoView(option: AutocompleteOptionState): void {
    option.element.scrollIntoView({ block: 'nearest' });
}

function invokeDotNetSafely(callback: () => Promise<unknown> | void): Promise<void> {
    try {
        return Promise.resolve(callback()).then(
            () => { },
            () => { }
        );
    }
    catch {
        return Promise.resolve();
    }
}

function updateSelectedOptionDom(state: AutocompleteState): void {
    for (const option of state.options) {
        const selected = !option.isCustom && option.value === state.selectedValue;
        option.element.classList.toggle('nt-combobox-option-selected', selected);
        option.element.setAttribute('aria-selected', selected ? 'true' : 'false');
        if (selected && !option.trailing) {
            option.trailing = createTrailingCheckElement();
            option.element.appendChild(option.trailing);
        }
        else if (!selected && option.trailing) {
            option.trailing.remove();
            option.trailing = null;
        }
    }
}

function clearActiveOption(state: AutocompleteState): void {
    state.activeIndex = -1;
    state.input.removeAttribute('aria-activedescendant');
    state.activeElement?.classList.remove(activeOptionClass);
    state.activeElement = null;
}

function updateActiveOption(state: AutocompleteState, nextIndex: number): void {
    if (!state.isOpen) {
        clearActiveOption(state);
        return;
    }

    const visibleOptions = getVisibleEnabledOptions(state);
    if (visibleOptions.length === 0) {
        clearActiveOption(state);
        return;
    }

    state.activeElement?.classList.remove(activeOptionClass);
    state.activeElement = null;

    state.activeIndex = ((nextIndex % visibleOptions.length) + visibleOptions.length) % visibleOptions.length;
    const activeOption = visibleOptions[state.activeIndex];
    activeOption.element.classList.add(activeOptionClass);
    state.activeElement = activeOption.element;
    state.input.setAttribute('aria-activedescendant', activeOption.element.id);
    scrollOptionIntoView(activeOption);
}

function rankOption(option: AutocompleteOptionState, query: string): number {
    if (option.isCustom) {
        return -1;
    }

    if (!query) {
        return 0;
    }

    if (option.lowerValue === query || option.lowerLabel === query) {
        return 0;
    }

    if (option.lowerValue.startsWith(query) || option.lowerLabel.startsWith(query)) {
        return 1;
    }

    return option.lowerValue.includes(query) || option.lowerLabel.includes(query) ? 2 : -1;
}

function collectRankedOptions(options: AutocompleteOptionState[], query: string): AutocompleteOptionState[] {
    const exactMatches: AutocompleteOptionState[] = [];
    const startsWithMatches: AutocompleteOptionState[] = [];
    const containsMatches: AutocompleteOptionState[] = [];
    for (const option of options) {
        const rank = rankOption(option, query);
        if (rank === 0) {
            exactMatches.push(option);
            continue;
        }

        if (rank === 1) {
            startsWithMatches.push(option);
            continue;
        }

        if (rank === 2) {
            containsMatches.push(option);
        }
    }

    return exactMatches.concat(startsWithMatches, containsMatches);
}

function formatCustomOptionText(option: AutocompleteOptionState, value: string): string {
    const format = option.element.dataset.ntAutocompleteCustomFormat ?? 'Use "{0}"';
    return format.replace('{0}', value);
}

function setOptionOrder(option: AutocompleteOptionState, order: number): void {
    const orderValue = order.toString();
    option.element.style.order = orderValue;
    if (option.listItem) {
        option.listItem.style.order = orderValue;
    }
}

function clearOptionOrder(option: AutocompleteOptionState): void {
    option.element.style.removeProperty('order');
    option.listItem?.style.removeProperty('order');
}

function orderVisibleOptions(state: AutocompleteState, visibleRankedOptions: AutocompleteOptionState[], customOption: AutocompleteOptionState | null): void {
    const headers = new Map<string, HTMLElement>();
    for (const header of Array.from(state.root.querySelectorAll<HTMLElement>('[data-nt-autocomplete-group-header]'))) {
        header.style.removeProperty('order');
        const groupId = header.dataset.ntAutocompleteGroupHeader;
        if (groupId) {
            headers.set(groupId, header);
        }
    }

    const groupOptions = new Map<string, AutocompleteOptionState[]>();
    const entries: { groupId: string | null; option: AutocompleteOptionState | null }[] = [];
    for (const option of visibleRankedOptions) {
        const groupId = option.listItem?.dataset.ntAutocompleteGroup;
        if (!groupId) {
            entries.push({ groupId: null, option });
            continue;
        }

        const options = groupOptions.get(groupId);
        if (options) {
            options.push(option);
            continue;
        }

        groupOptions.set(groupId, [option]);
        entries.push({ groupId, option: null });
    }

    let order = 0;
    for (const entry of entries) {
        if (entry.groupId) {
            const header = headers.get(entry.groupId);
            if (header) {
                header.style.order = order.toString();
            }

            order++;
            for (const option of groupOptions.get(entry.groupId) ?? []) {
                setOptionOrder(option, order++);
            }
        }
        else if (entry.option) {
            setOptionOrder(entry.option, order++);
        }
    }

    if (customOption) {
        setOptionOrder(customOption, order);
    }
}

function filterOptions(state: AutocompleteState, updateActive = state.isOpen): void {
    if (state.options.length === 0) {
        updateGroupVisibility(state);
        if (updateActive) {
            clearActiveOption(state);
        }
        return;
    }

    const typedValue = state.query?.trim() ?? '';
    const query = typedValue.toLocaleLowerCase();
    const normalOptions = state.options.filter(option => !option.isCustom);
    const customOption = state.options.find(option => option.isCustom);
    const selectedOption = normalOptions.find(option => option.value === state.selectedValue);
    const showCustomOption = Boolean(state.query !== null && customOption && typedValue);
    const visibleRankedOptions = state.query === null
        ? selectedOption ? [selectedOption, ...normalOptions.filter(option => option !== selectedOption)] : normalOptions
        : collectRankedOptions(normalOptions, query);
    const visibleOptions = new Set(visibleRankedOptions);
    if (customOption && showCustomOption) {
        visibleOptions.add(customOption);
        customOption.value = typedValue;
        customOption.lowerValue = typedValue.toLocaleLowerCase();
        customOption.lowerLabel = customOption.lowerValue;
        customOption.element.dataset.ntAutocompleteValue = typedValue;
        customOption.element.dataset.ntAutocompleteLabel = typedValue;
        if (customOption.labelElement) {
            customOption.labelElement.textContent = formatCustomOptionText(customOption, typedValue);
        }
    }

    for (const option of state.options) {
        const visible = visibleOptions.has(option);
        option.element.hidden = !visible;
        option.listItem?.toggleAttribute('hidden', !visible);
        if (!visible) {
            clearOptionOrder(option);
        }
    }

    orderVisibleOptions(state, visibleRankedOptions, showCustomOption && customOption ? customOption : null);
    updateGroupVisibility(state);

    const empty = state.root.querySelector<HTMLElement>('.nt-combobox-empty');
    if (empty) {
        empty.hidden = visibleRankedOptions.length > 0 || showCustomOption;
    }

    if (state.isOpen) {
        updateMenuPlacement(state);
    }

    if (updateActive) {
        updateActiveOption(state, 0);
    }
    else {
        clearActiveOption(state);
    }
}

function updateGroupVisibility(state: AutocompleteState): void {
    const visibleGroupIds = new Set<string>();
    for (const item of Array.from(state.root.querySelectorAll<HTMLElement>('[data-nt-autocomplete-group]'))) {
        const groupId = item.dataset.ntAutocompleteGroup;
        if (groupId && !item.hidden && item.querySelector<HTMLButtonElement>('[data-nt-autocomplete-option="true"]')?.hidden === false) {
            visibleGroupIds.add(groupId);
        }
    }

    for (const header of Array.from(state.root.querySelectorAll<HTMLElement>('[data-nt-autocomplete-group-header]'))) {
        const groupId = header.dataset.ntAutocompleteGroupHeader;
        header.hidden = !groupId || !visibleGroupIds.has(groupId);
    }
}

function attachDocumentListeners(state: AutocompleteState): void {
    if (state.documentListenersAttached) {
        return;
    }

    document.addEventListener('mousedown', state.onDocumentMouseDown);
    document.addEventListener('focusin', state.onDocumentFocusIn);
    document.addEventListener('scroll', state.onDocumentScroll, true);
    window.addEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = true;
}

function detachDocumentListeners(state: AutocompleteState): void {
    if (!state.documentListenersAttached) {
        return;
    }

    document.removeEventListener('mousedown', state.onDocumentMouseDown);
    document.removeEventListener('focusin', state.onDocumentFocusIn);
    document.removeEventListener('scroll', state.onDocumentScroll, true);
    window.removeEventListener('resize', state.onWindowResize);
    state.documentListenersAttached = false;
}

function applyOpenState(state: AutocompleteState, isOpen: boolean): void {
    state.isOpen = isOpen;
    state.root.classList.toggle('nt-autocomplete-open', isOpen);
    state.input.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    if (state.menu) {
        state.menu.hidden = !isOpen;
        state.menu.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        if (!isOpen) {
            hideMenuSurface(state);
        }
    }

    if (isOpen) {
        showMenuSurface(state);
        updateMenuPlacement(state);
        attachDocumentListeners(state);
    }
    else {
        detachDocumentListeners(state);
        clearActiveOption(state);
        clearMenuOptions(state);
    }
}

function setOpen(state: AutocompleteState, isOpen: boolean, notifyTouched = false): void {
    if (state.isOpen === isOpen) {
        return;
    }

    applyOpenState(state, isOpen);
    if (isOpen) {
        filterOptions(state);
        return;
    }

    filterOptions(state, false);

    if (notifyTouched && state.dotNetRef) {
        void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyAutocompleteTouched'));
    }
}

function openAutocomplete(state: AutocompleteState): boolean {
    if (state.input.disabled || state.input.readOnly) {
        return false;
    }

    if (state.isOpen) {
        return false;
    }

    renderMenuOptions(state);
    setOpen(state, true);
    return true;
}

function notifyValueChanged(state: AutocompleteState, closeMenu: boolean): void {
    const dotNetRef = state.dotNetRef;
    if (!dotNetRef) {
        return;
    }

    const value = state.input.value;
    void invokeDotNetSafely(() => dotNetRef.invokeMethodAsync('NotifyAutocompleteValueChanged', value, closeMenu));
}

function selectOption(state: AutocompleteState, option: AutocompleteOptionState): void {
    if (option.element.disabled || state.input.disabled || state.input.readOnly) {
        return;
    }

    state.input.value = option.value;
    state.query = null;
    state.selectedValue = option.value;
    setOpen(state, false);
    notifyValueChanged(state, true);
}

function updateElements(state: AutocompleteState): void {
    const query = state.query;
    if (query !== null && state.isOpen) {
        state.input.value = query;
    }
    else {
        state.query = null;
        state.selectedValue = state.input.value || null;
    }

    state.root = queryRoot(state.input);
    state.list = state.root.querySelector<HTMLElement>('.nt-combobox-list');
    state.menu = state.root.querySelector<HTMLElement>('[data-nt-autocomplete-menu="true"]');
    state.optionSources = Array.from(state.root.querySelectorAll<HTMLScriptElement>('script[data-nt-autocomplete-option-definition="true"], script[data-nt-autocomplete-options="true"]'));
    const shouldBeOpen = state.root.classList.contains('nt-autocomplete-open') || state.menu?.hidden === false;
    if (shouldBeOpen) {
        renderMenuOptions(state);
    }
    else {
        clearMenuOptions(state);
    }

    applyOpenState(state, shouldBeOpen);
    filterOptions(state, shouldBeOpen);
}

function hasLiveMenuOptions(state: AutocompleteState): boolean {
    return Array.from(state.list?.children ?? []).some(child => child.classList.contains('nt-combobox-list-item'));
}

function parseOptionDefinitions(state: AutocompleteState): AutocompleteOptionDefinition[] {
    const definitions: AutocompleteOptionDefinition[] = [];
    for (const source of state.optionSources) {
        if (!source.textContent) {
            continue;
        }

        try {
            const value = JSON.parse(source.textContent) as AutocompleteOptionDefinition | AutocompleteOptionDefinition[];
            if (Array.isArray(value)) {
                definitions.push(...value);
            }
            else if (value && typeof value === 'object') {
                definitions.push(value);
            }
        }
        catch {
            // Ignore malformed option metadata and keep the native input usable.
        }
    }

    return definitions;
}

function createIconElement(icon: AutocompleteIconDefinition): HTMLElement {
    const wrapper = document.createElement('span');
    wrapper.className = 'nt-combobox-option-leading';
    wrapper.setAttribute('aria-hidden', 'true');

    const iconElement = document.createElement('span');
    iconElement.className = icon.cssClass;
    iconElement.title = icon.title;
    if (icon.style) {
        iconElement.setAttribute('style', icon.style);
    }
    iconElement.textContent = icon.icon;
    wrapper.appendChild(iconElement);
    return wrapper;
}

function createTrailingCheckElement(): HTMLElement {
    const trailing = document.createElement('span');
    trailing.className = 'nt-combobox-option-trailing';
    trailing.setAttribute('aria-hidden', 'true');

    const check = document.createElement('span');
    check.className = 'nt-components nt-icon material-symbols-outlined mi-medium';
    check.title = 'check';
    check.textContent = 'check';
    trailing.appendChild(check);
    return trailing;
}

function createGroupElement(label: string, groupId: string): HTMLElement {
    const item = document.createElement('li');
    item.className = 'nt-combobox-list-item nt-combobox-list-group';
    item.dataset.ntAutocompleteGroupHeader = groupId;
    item.setAttribute('role', 'presentation');

    const labelElement = document.createElement('div');
    labelElement.className = 'nt-combobox-group-label';
    labelElement.textContent = label;
    item.appendChild(labelElement);
    return item;
}

function createOptionElement(definition: AutocompleteOptionDefinition, index: number, listboxId: string, groupId: string | null): HTMLElement {
    const item = document.createElement('li');
    item.className = 'nt-combobox-list-item';
    item.setAttribute('role', 'presentation');
    if (groupId) {
        item.dataset.ntAutocompleteGroup = groupId;
    }
    if (definition.isCustom) {
        item.hidden = true;
    }

    const option = document.createElement('button');
    option.className = definition.cssClass || 'nt-combobox-option';
    option.classList.toggle('nt-autocomplete-group-option', groupId !== null);
    option.id = definition.id || `${listboxId}-option-${index}`;
    option.type = 'button';
    option.setAttribute('role', 'option');
    option.setAttribute('aria-selected', definition.selected ? 'true' : 'false');
    option.dataset.ntAutocompleteOption = 'true';
    option.dataset.ntAutocompleteValue = definition.value;
    option.dataset.ntAutocompleteLabel = definition.label;
    if (definition.disabled) {
        option.disabled = true;
        option.setAttribute('aria-disabled', 'true');
    }
    if (definition.isCustom) {
        option.dataset.ntAutocompleteCustomOption = 'true';
        option.dataset.ntAutocompleteCustomFormat = definition.customFormat ?? 'Use "{0}"';
        option.hidden = true;
    }

    if (definition.leadingIcon) {
        option.appendChild(createIconElement(definition.leadingIcon));
    }

    const content = document.createElement('span');
    content.className = 'nt-combobox-option-content';
    const label = document.createElement('span');
    label.className = 'nt-combobox-option-label';
    label.textContent = definition.isCustom ? (definition.customFormat ?? 'Use "{0}"').replace('{0}', '') : definition.label;
    content.appendChild(label);
    if (definition.supportingText) {
        const supporting = document.createElement('span');
        supporting.className = 'nt-combobox-option-supporting';
        supporting.textContent = definition.supportingText;
        content.appendChild(supporting);
    }
    option.appendChild(content);

    if (!definition.isCustom && definition.selected) {
        option.appendChild(createTrailingCheckElement());
    }

    item.appendChild(option);
    return item;
}

function renderMenuOptions(state: AutocompleteState): void {
    if (!state.list || hasLiveMenuOptions(state)) {
        state.options = queryOptions(state.root).map(getOptionState);
        updateSelectedOptionDom(state);
        updateGroupVisibility(state);
        return;
    }

    const fragment = document.createDocumentFragment();
    const listboxId = state.list.id || state.input.id || 'nt-autocomplete-listbox';
    let currentGroup: string | null = null;
    let currentGroupId: string | null = null;
    let groupIndex = 0;
    const definitions = parseOptionDefinitions(state);
    for (let index = 0; index < definitions.length; index++) {
        const definition = definitions[index];
        const group = definition.isCustom ? null : definition.group?.trim() || null;
        if (group !== currentGroup) {
            currentGroup = group;
            currentGroupId = group ? `${listboxId}-group-${groupIndex++}` : null;
            if (group && currentGroupId) {
                fragment.appendChild(createGroupElement(group, currentGroupId));
            }
        }

        fragment.appendChild(createOptionElement(definition, index, listboxId, currentGroupId));
    }
    state.list.appendChild(fragment);
    state.options = queryOptions(state.root).map(getOptionState);
    updateSelectedOptionDom(state);
    updateGroupVisibility(state);
}

function clearMenuOptions(state: AutocompleteState): void {
    for (const child of Array.from(state.list?.children ?? [])) {
        if (child.classList.contains('nt-combobox-list-item')) {
            child.remove();
        }
    }

    state.options = [];
}

function createState(input: HTMLInputElement, dotNetRef: Maybe<unknown>): AutocompleteState {
    const state: AutocompleteState = {
        activeElement: null,
        activeIndex: -1,
        documentListenersAttached: false,
        dotNetRef: isDotNetObjectReference(dotNetRef) ? dotNetRef : null,
        input,
        isOpen: false,
        list: null,
        menu: null,
        onDocumentFocusIn: () => { },
        onDocumentMouseDown: () => { },
        onDocumentScroll: () => { },
        onInputClick: () => { },
        onInputBeforeInput: () => { },
        onInputInput: () => { },
        onInputKeyDown: () => { },
        onMenuClick: () => { },
        onMenuMouseDown: () => { },
        onWindowResize: () => { },
        options: [],
        optionSources: [],
        query: null,
        root: queryRoot(input),
        selectedValue: input.value || null,
    };

    state.onInputClick = () => {
        openAutocomplete(state);
    };

    state.onInputBeforeInput = () => {
        if (state.selectedValue === null || state.input.value !== state.selectedValue) {
            return;
        }

        state.input.value = '';
        state.query = '';
        state.selectedValue = null;
        if (state.dotNetRef) {
            void invokeDotNetSafely(() => state.dotNetRef?.invokeMethodAsync('NotifyAutocompleteValueChanged', null, false));
        }
    };

    state.onInputInput = () => {
        state.query = state.input.value;
        state.selectedValue = null;
        if (!openAutocomplete(state)) {
            filterOptions(state);
        }
        updateSelectedOptionDom(state);
    };

    state.onInputKeyDown = event => {
        if (state.input.disabled || state.input.readOnly) {
            return;
        }

        const visibleOptions = getVisibleEnabledOptions(state);
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                if (!openAutocomplete(state)) {
                    updateActiveOption(state, state.activeIndex + 1);
                }
                break;
            case 'ArrowUp':
                event.preventDefault();
                openAutocomplete(state);
                updateActiveOption(state, state.activeIndex - 1);
                break;
            case 'Enter':
                if (!state.isOpen) {
                    openAutocomplete(state);
                    return;
                }

                if (state.activeIndex >= 0 && state.activeIndex < visibleOptions.length) {
                    event.preventDefault();
                    selectOption(state, visibleOptions[state.activeIndex]);
                }
                break;
            case 'Escape':
                if (state.isOpen) {
                    event.preventDefault();
                    setOpen(state, false, true);
                }
                break;
            case 'Tab':
                setOpen(state, false, true);
                break;
        }
    };

    state.onMenuMouseDown = event => {
        event.preventDefault();
    };

    state.onMenuClick = event => {
        const target = event.target instanceof Element ? event.target : null;
        const optionElement = target?.closest<HTMLButtonElement>('[data-nt-autocomplete-option="true"]');
        const option = state.options.find(item => item.element === optionElement);
        if (!option) {
            return;
        }

        event.preventDefault();
        selectOption(state, option);
    };

    state.onDocumentMouseDown = event => {
        const target = event.target instanceof Node ? event.target : null;
        if (target && state.root.contains(target)) {
            return;
        }

        setOpen(state, false, true);
    };

    state.onDocumentFocusIn = event => {
        const target = event.target instanceof Node ? event.target : null;
        if (target && state.root.contains(target)) {
            return;
        }

        setOpen(state, false, true);
    };

    state.onDocumentScroll = () => {
        updateMenuPlacement(state);
    };

    state.onWindowResize = () => {
        updateMenuPlacement(state);
    };

    input.addEventListener('click', state.onInputClick);
    input.addEventListener('beforeinput', state.onInputBeforeInput);
    input.addEventListener('input', state.onInputInput);
    input.addEventListener('keydown', state.onInputKeyDown);
    updateElements(state);
    state.menu?.addEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.addEventListener('click', state.onMenuClick);
    return state;
}

function cleanupState(state: Maybe<AutocompleteState>): void {
    if (!state) {
        return;
    }

    state.input.removeEventListener('click', state.onInputClick);
    state.input.removeEventListener('beforeinput', state.onInputBeforeInput);
    state.input.removeEventListener('input', state.onInputInput);
    state.input.removeEventListener('keydown', state.onInputKeyDown);
    state.menu?.removeEventListener('mousedown', state.onMenuMouseDown);
    state.menu?.removeEventListener('click', state.onMenuClick);
    hideMenuSurface(state);
    detachDocumentListeners(state);
}

function synchronizeInput(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    if (!input) {
        return;
    }

    const existing = stateByInput.get(input);
    if (existing) {
        existing.dotNetRef = isDotNetObjectReference(dotNetRef) ? dotNetRef : existing.dotNetRef;
        const previousMenu = existing.menu;
        updateElements(existing);
        if (previousMenu !== existing.menu) {
            previousMenu?.removeEventListener('mousedown', existing.onMenuMouseDown);
            previousMenu?.removeEventListener('click', existing.onMenuClick);
            existing.menu?.addEventListener('mousedown', existing.onMenuMouseDown);
            existing.menu?.addEventListener('click', existing.onMenuClick);
        }
        return;
    }

    stateByInput.set(input, createState(input, dotNetRef));
}

function cleanupDisconnectedStates(): void {
    for (const [input, state] of stateByInput) {
        if (!input.isConnected) {
            cleanupState(state);
            stateByInput.delete(input);
        }
    }
}

export function onLoad(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    synchronizeAutocomplete(input, dotNetRef);
}

export function onUpdate(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    synchronizeAutocomplete(input, dotNetRef);
}

function synchronizeAutocomplete(input: Maybe<HTMLInputElement>, dotNetRef: Maybe<unknown>): void {
    cleanupDisconnectedStates();
    synchronizeInput(input, dotNetRef);
}

export function enhanceAll(root: ParentNode = document): void {
    cleanupDisconnectedStates();
    for (const input of Array.from(root.querySelectorAll<HTMLInputElement>('[data-nt-autocomplete-input="true"]'))) {
        synchronizeInput(input, null);
    }
}

export function onDispose(input: Maybe<HTMLInputElement>): void {
    if (!input) {
        for (const state of stateByInput.values()) {
            cleanupState(state);
        }
        stateByInput.clear();
        return;
    }

    const state = stateByInput.get(input);
    cleanupState(state);
    stateByInput.delete(input);
    cleanupDisconnectedStates();
}
