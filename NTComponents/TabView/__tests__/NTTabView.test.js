/**
 * @jest-environment jsdom
 */
import { jest } from '@jest/globals';
import { __testHooks, onDispose, onLoad, onUpdate, selectTabByValue } from '../NTTabView.razor.js';

const originalLocation = window.location.href;

class ResizeObserverMock {
  constructor(callback) {
    this.callback = callback;
    ResizeObserverMock.instances.push(this);
  }

  observe = jest.fn();
  disconnect = jest.fn();

  static instances = [];
}

describe('NTTabView page-script module', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
    window.history.replaceState(null, '', originalLocation);
    global.ResizeObserver = ResizeObserverMock;
    ResizeObserverMock.instances = [];
    global.NTComponents = { registerButtonInteraction: jest.fn() };
    global.matchMedia = jest.fn().mockReturnValue({ matches: false });
    global.requestAnimationFrame = callback => {
      callback();
      return 1;
    };
    global.cancelAnimationFrame = jest.fn();
  });

  function createTabView() {
    document.body.innerHTML = `
      <nt-tab-view class="nt-tab-view" data-nt-tab-view="true" data-nt-tab-view-name="details" data-nt-tab-query-parameter="details" data-nt-tab-update-query="true">
        <div class="nt-tab-view-header">
          <div class="nt-tab-view-tablist" role="tablist">
            <button type="button" class="nt-tab-view-tab nt-tab-view-tab-selected" id="tab-overview" role="tab" aria-selected="true" aria-controls="panel-overview" tabindex="0" data-nt-tab-button="true" data-nt-tab-value="overview">
              <span class="nt-tab-view-content nt-tab-view-indicator-target">Overview</span>
            </button>
            <button type="button" class="nt-tab-view-tab" id="tab-specs" role="tab" aria-selected="false" aria-controls="panel-specs" tabindex="-1" data-nt-tab-button="true" data-nt-tab-value="specs">
              <span class="nt-tab-view-content nt-tab-view-indicator-target">Specs</span>
            </button>
            <span class="nt-tab-view-active-indicator" data-nt-tab-indicator></span>
          </div>
        </div>
        <div class="nt-tab-view-panels">
          <section class="nt-tab-view-panel nt-tab-view-panel-selected" id="panel-overview" role="tabpanel" data-nt-tab-panel="true" data-nt-tab-value="overview">Overview content</section>
          <section class="nt-tab-view-panel" id="panel-specs" role="tabpanel" data-nt-tab-panel="true" data-nt-tab-value="specs" hidden>Specs content</section>
        </div>
      </nt-tab-view>
      <nt-page-script></nt-page-script>`;

    return document.querySelector('nt-tab-view');
  }

  test('programmatic selection overrides the initial query and updates panels without moving focus', () => {
    const tabView = createTabView();
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}?details=overview`);
    const overview = tabView.querySelector('#tab-overview');
    overview.focus();

    expect(selectTabByValue(tabView, 'SPECS')).toBe(true);
    onUpdate(tabView);

    expect(overview.getAttribute('aria-selected')).toBe('false');
    expect(overview.tabIndex).toBe(-1);
    expect(tabView.querySelector('#tab-specs').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#tab-specs').tabIndex).toBe(0);
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(new URL(window.location.href).searchParams.get('details')).toBe('specs');
    expect(document.activeElement).toBe(overview);
  });

  // Contract: SelectTabAsync returns whether a matching enabled tab was found and selected.
  test.each(['disabled', 'missing'])('programmatic selection rejects a %s tab without changing selection', condition => {
    const tabView = createTabView();
    tabView.querySelector('#tab-specs').disabled = condition === 'disabled';

    expect(selectTabByValue(tabView, condition === 'disabled' ? 'specs' : 'missing')).toBe(false);
    expect(tabView.querySelector('#tab-overview').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-overview').hidden).toBe(false);
    expect(tabView.querySelector('#panel-specs').hidden).toBe(true);
    expect(window.location.href).toBe(originalLocation);
  });

  // Contract: SelectTabAsync remarks promise click-equivalent selection and query behavior;
  // existing 'updates preserve current selection instead of reapplying query value' is the update parity target.
  test('repeated programmatic selection survives page-script load and updates with query writes disabled', () => {
    const tabView = createTabView();
    tabView.dataset.ntTabUpdateQuery = 'false';
    const initialUrl = `${originalLocation.split('?')[0]}?details=overview&other=keep#anchor`;
    window.history.replaceState(null, '', initialUrl);

    expect(selectTabByValue(tabView, 'specs')).toBe(true);
    onLoad(document.querySelector('nt-page-script'));
    expect(selectTabByValue(tabView, 'specs')).toBe(true);
    onUpdate(tabView);

    expect(tabView.querySelector('#tab-specs').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
    expect(tabView.querySelectorAll('[role="tab"][aria-selected="true"]')).toHaveLength(1);
    expect(window.location.href).toBe(initialUrl);
  });

  // Contract: an ordinary render update must preserve the user's viewport while they
  // interact with panel content; explicit selection retains documented click parity.
  test('render updates do not scroll back to the tab after programmatic selection', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const overview = tabView.querySelector('#tab-overview');
    overview.scrollIntoView = jest.fn();
    const specs = tabView.querySelector('#tab-specs');
    specs.scrollIntoView = jest.fn();

    expect(selectTabByValue(tabView, 'specs')).toBe(true);
    expect(specs.scrollIntoView).toHaveBeenCalledTimes(1);
    expect(overview.scrollIntoView).not.toHaveBeenCalled();
    specs.scrollIntoView.mockClear();

    onUpdate(tabView);

    expect(specs.scrollIntoView).not.toHaveBeenCalled();
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });

  // Contract: SelectTabAsync selects a tab through that component's reference;
  // selecting one instance must leave another instance's content and selection intact.
  test.each(['sibling', 'nested'])('programmatic selection is scoped to its view with a %s view', placement => {
    const tabView = createTabView();
    const otherView = tabView.cloneNode(true);
    otherView.dataset.ntTabQueryParameter = 'other';
    otherView.querySelectorAll('[id]').forEach(element => { element.id = `other-${element.id}`; });
    otherView.querySelectorAll('[aria-controls]').forEach(element => {
      element.setAttribute('aria-controls', `other-${element.getAttribute('aria-controls')}`);
    });
    if (placement === 'nested') {
      tabView.querySelector('#panel-overview').append(otherView);
    } else {
      tabView.after(otherView);
    }
    onLoad(otherView);

    expect(selectTabByValue(tabView, 'specs')).toBe(true);

    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(otherView.querySelector('#other-tab-overview').getAttribute('aria-selected')).toBe('true');
    expect(otherView.querySelector('#other-panel-overview').hidden).toBe(false);
    expect(otherView.querySelector('#other-panel-specs').hidden).toBe(true);
    expect(new URL(window.location.href).searchParams.get('other')).toBeNull();
  });

  // Contract: disposing a removed component must not disable a still-mounted sibling.
  // A removed ElementReference can arrive as null at the JavaScript interop boundary.
  test('disposing a missing element leaves mounted tab navigation working', () => {
    const tabView = createTabView();
    onLoad(tabView);

    onDispose(null);
    tabView.querySelector('#tab-specs').click();

    expect(tabView.querySelector('#tab-specs').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
  });

  // Compatibility contract: omitting the scope retains document-wide cleanup.
  test('disposing without a scope removes navigation listeners from mounted views', () => {
    const tabView = createTabView();
    onLoad();

    onDispose();
    tabView.querySelector('#tab-specs').click();

    expect(tabView.querySelector('#tab-overview').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-overview').hidden).toBe(false);
    expect(tabView.querySelector('#panel-specs').hidden).toBe(true);
  });

  test('onLoad resolves a page-script element to the preceding tab view', () => {
    const tabView = createTabView();
    const pageScript = document.querySelector('nt-page-script');

    onLoad(pageScript);

    expect(tabView.__ntTabViewState).toBeDefined();
    expect(global.NTComponents.registerButtonInteraction).toHaveBeenCalledTimes(2);
  });

  test('query parameter selects matching tab on load', () => {
    const tabView = createTabView();
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}?details=specs`);

    onLoad(tabView);

    expect(tabView.querySelector('[data-nt-tab-value="overview"]').getAttribute('aria-selected')).toBe('false');
    expect(tabView.querySelector('[data-nt-tab-value="specs"]').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });

  test('click selects tab and writes named query parameter', () => {
    const tabView = createTabView();
    onLoad(tabView);

    tabView.querySelector('[data-nt-tab-value="specs"]').click();

    expect(new URL(window.location.href).searchParams.get('details')).toBe('specs');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });

  test('keyboard arrows move focus without wrapping and enter selects focused tab', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    overview.focus();
    overview.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
    expect(specs.getAttribute('aria-selected')).toBe('false');
    expect(overview.tabIndex).toBe(-1);
    expect(specs.tabIndex).toBe(0);

    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    expect(specs.getAttribute('aria-selected')).toBe('true');

    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
  });

  test('home and end keep a single roving tab stop', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    specs.focus();
    specs.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(overview);
    expect(overview.tabIndex).toBe(0);
    expect(specs.tabIndex).toBe(-1);

    overview.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(specs);
    expect(overview.tabIndex).toBe(-1);
    expect(specs.tabIndex).toBe(0);
  });

  test('reduced motion uses auto scroll behavior', () => {
    const tabView = createTabView();
    global.matchMedia = jest.fn().mockReturnValue({ matches: true });
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');
    specs.scrollIntoView = jest.fn();

    onLoad(tabView);
    specs.click();

    expect(specs.scrollIntoView).toHaveBeenCalledWith({ behavior: 'auto', block: 'nearest', inline: 'nearest' });
  });

  // Regression: a tab view below the fold must not scroll the page to its tab strip on first load.
  test.each(['', '?details=specs'])('initial load does not scroll the selected tab into view (query "%s")', query => {
    const tabView = createTabView();
    tabView.dataset.ntTabUpdateQuery = 'false';
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}${query}`);
    const tabs = Array.from(tabView.querySelectorAll('[role="tab"]'));
    tabs.forEach(tab => { tab.scrollIntoView = jest.fn(); });

    onLoad(document.querySelector('nt-page-script'));

    tabs.forEach(tab => expect(tab.scrollIntoView).not.toHaveBeenCalled());
    expect(tabView.querySelector(`#tab-${query ? 'specs' : 'overview'}`).getAttribute('aria-selected')).toBe('true');
    expect(window.scrollY).toBe(0);
  });

  test.each([
    ['click', (tabView, specs) => specs.click()],
    ['ArrowRight', tabView => tabView.querySelector('#tab-overview').dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }))]
  ])('%s scrolls the target tab into view within the tab strip', (_, act) => {
    const tabView = createTabView();
    onLoad(tabView);
    const specs = tabView.querySelector('#tab-specs');
    specs.scrollIntoView = jest.fn();

    act(tabView, specs);

    expect(specs.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
  });

  test('updates preserve current selection instead of reapplying query value', () => {
    const tabView = createTabView();
    window.history.replaceState(null, '', `${originalLocation.split('?')[0]}?details=specs`);
    onLoad(tabView);
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    overview.click();
    // A re-render that reapplies the server's initial selection must not override the user's choice.
    specs.setAttribute('aria-selected', 'true');

    onUpdate(tabView);

    expect(overview.getAttribute('aria-selected')).toBe('true');
    expect(specs.getAttribute('aria-selected')).toBe('false');
  });

  test('updates rebind tablist listeners when the tablist element is replaced', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const originalTabList = tabView.querySelector('.nt-tab-view-tablist');
    const replacementTabList = originalTabList.cloneNode(true);
    originalTabList.replaceWith(replacementTabList);
    const specs = replacementTabList.querySelector('[data-nt-tab-value="specs"]');

    onUpdate(tabView);
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('true');
    expect(tabView.__ntTabViewState.tabList).toBe(replacementTabList);
  });

  test('mutation observation releases a removed tablist and binds one added later', async () => {
    const tabView = createTabView();
    const header = tabView.querySelector('.nt-tab-view-header');
    const originalTabList = tabView.querySelector('.nt-tab-view-tablist');
    const removeEventListener = jest.spyOn(originalTabList, 'removeEventListener');
    const replacementTabList = originalTabList.cloneNode(true);
    onLoad(tabView);

    originalTabList.remove();
    await new Promise(resolve => queueMicrotask(resolve));

    expect(removeEventListener).toHaveBeenCalledWith('click', expect.any(Function));
    expect(removeEventListener).toHaveBeenCalledWith('scroll', expect.any(Function));

    header.append(replacementTabList);
    await new Promise(resolve => queueMicrotask(resolve));
    const specs = replacementTabList.querySelector('[data-nt-tab-value="specs"]');
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(new URL(window.location.href).searchParams.get('details')).toBe('specs');
  });

  test('mutation observation migrates listeners when the tablist is replaced in place', async () => {
    const tabView = createTabView();
    const originalTabList = tabView.querySelector('.nt-tab-view-tablist');
    const removeEventListener = jest.spyOn(originalTabList, 'removeEventListener');
    const replacementTabList = originalTabList.cloneNode(true);
    onLoad(tabView);
    const originalResizeObserver = ResizeObserverMock.instances[0];

    originalTabList.replaceWith(replacementTabList);
    await new Promise(resolve => queueMicrotask(resolve));
    const specs = replacementTabList.querySelector('[data-nt-tab-value="specs"]');
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(removeEventListener).toHaveBeenCalledWith('click', expect.any(Function));
    expect(removeEventListener).toHaveBeenCalledWith('scroll', expect.any(Function));
    expect(originalResizeObserver.disconnect).toHaveBeenCalled();
  });

  test('rebinds tablist listeners when interactive rendering replaces the header', async () => {
    const tabView = createTabView();
    const originalTabList = tabView.querySelector('.nt-tab-view-tablist');
    const removeEventListener = jest.spyOn(originalTabList, 'removeEventListener');
    onLoad(tabView);
    const replacementHeader = tabView.querySelector('.nt-tab-view-header').cloneNode(true);

    tabView.querySelector('.nt-tab-view-header').replaceWith(replacementHeader);
    await new Promise(resolve => queueMicrotask(resolve));
    const specs = replacementHeader.querySelector('[data-nt-tab-value="specs"]');
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('true');
    expect(tabView.__ntTabViewState.tabList).toBe(replacementHeader.querySelector('.nt-tab-view-tablist'));
    expect(removeEventListener).toHaveBeenCalledWith('click', expect.any(Function));
    expect(removeEventListener).toHaveBeenCalledWith('scroll', expect.any(Function));
  });

  test('indicator uses active target geometry relative to the scroll container', () => {
    const tabView = createTabView();
    const tabList = tabView.querySelector('.nt-tab-view-tablist');
    const target = tabView.querySelector('[data-nt-tab-value="overview"] .nt-tab-view-indicator-target');

    Object.defineProperty(tabList, 'scrollLeft', { value: 12, configurable: true });
    tabList.getBoundingClientRect = () => ({ left: 10, width: 300 });
    target.getBoundingClientRect = () => ({ left: 42, width: 56 });

    __testHooks.updateIndicator(tabView);

    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-x')).toBe('44px');
    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-width')).toBe('56px');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.transform).toBe('');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.inlineSize).toBe('');
  });

  test('secondary indicator uses the full selected tab width', () => {
    const tabView = createTabView();
    tabView.classList.add('nt-tab-view-secondary');
    const tabList = tabView.querySelector('.nt-tab-view-tablist');
    const tab = tabView.querySelector('[data-nt-tab-value="overview"]');
    const target = tab.querySelector('.nt-tab-view-indicator-target');

    Object.defineProperty(tabList, 'scrollLeft', { value: 4, configurable: true });
    tabList.getBoundingClientRect = () => ({ left: 10, width: 300 });
    tab.getBoundingClientRect = () => ({ left: 30, width: 120 });
    target.getBoundingClientRect = () => ({ left: 60, width: 48 });

    __testHooks.updateIndicator(tabView);

    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-x')).toBe('24px');
    expect(tabView.style.getPropertyValue('--nt-tab-view-active-indicator-width')).toBe('120px');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.transform).toBe('');
    expect(tabView.querySelector('[data-nt-tab-indicator]').style.inlineSize).toBe('');
  });

  test('onUpdate keeps existing state and onDispose removes listeners and observers', () => {
    const tabView = createTabView();
    onLoad(tabView);
    const state = tabView.__ntTabViewState;
    const overview = tabView.querySelector('[data-nt-tab-value="overview"]');
    const specs = tabView.querySelector('[data-nt-tab-value="specs"]');

    onUpdate(tabView);
    onDispose(tabView);
    specs.click();
    overview.focus();
    overview.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));

    expect(state.resizeObserver.disconnect).toHaveBeenCalled();
    expect(tabView.__ntTabViewState).toBeUndefined();
    expect(specs.getAttribute('aria-selected')).toBe('false');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(true);
    expect(new URL(window.location.href).searchParams.get('details')).toBeNull();
    expect(document.activeElement).toBe(overview);
  });

  test('onDispose prevents later header mutations from reactivating tab navigation', async () => {
    const tabView = createTabView();
    onLoad(tabView);
    onDispose(tabView);
    const replacementHeader = tabView.querySelector('.nt-tab-view-header').cloneNode(true);

    tabView.querySelector('.nt-tab-view-header').replaceWith(replacementHeader);
    await new Promise(resolve => queueMicrotask(resolve));
    const specs = replacementHeader.querySelector('[data-nt-tab-value="specs"]');
    specs.click();

    expect(specs.getAttribute('aria-selected')).toBe('false');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(true);
    expect(new URL(window.location.href).searchParams.get('details')).toBeNull();
  });

  test('onDispose removes the window resize fallback when ResizeObserver is unavailable', () => {
    const tabView = createTabView();
    const removeEventListener = jest.spyOn(window, 'removeEventListener');
    global.ResizeObserver = undefined;
    onLoad(tabView);
    const onScroll = tabView.__ntTabViewState.onScroll;

    onDispose(tabView);

    expect(removeEventListener).toHaveBeenCalledWith('resize', onScroll);
    expect(tabView.__ntTabViewState).toBeUndefined();
    removeEventListener.mockRestore();
  });

  test('dispose can resolve the tab view after page script element is detached', () => {
    const tabView = createTabView();
    const pageScript = document.querySelector('nt-page-script');
    onLoad(pageScript);
    const state = tabView.__ntTabViewState;

    pageScript.remove();
    onDispose(pageScript);

    expect(state.resizeObserver.disconnect).toHaveBeenCalled();
    expect(tabView.__ntTabViewState).toBeUndefined();
  });

  function addHistoryTab(tabView) {
    tabView.querySelector('[data-nt-tab-indicator]').insertAdjacentHTML('beforebegin', `
      <button type="button" class="nt-tab-view-tab" id="tab-history" role="tab" aria-selected="false" aria-controls="panel-history" tabindex="-1" data-nt-tab-button="true" data-nt-tab-value="history">
        <span class="nt-tab-view-content nt-tab-view-indicator-target">History</span>
      </button>`);
    tabView.querySelector('.nt-tab-view-panels').insertAdjacentHTML('beforeend',
      '<section class="nt-tab-view-panel" id="panel-history" role="tabpanel" data-nt-tab-panel="true" data-nt-tab-value="history" hidden>History content</section>');
  }

  test('a detached tab keeps the user selection when the re-render marks another tab selected', async () => {
    const tabView = createTabView();
    addHistoryTab(tabView);
    onLoad(tabView);
    tabView.querySelector('#tab-specs').click();

    // A popped-out overview tab loses its header while the re-render marks history as the initial selection.
    tabView.querySelector('#tab-overview').remove();
    tabView.querySelector('#tab-history').setAttribute('aria-selected', 'true');
    tabView.querySelector('#panel-history').hidden = false;
    await new Promise(resolve => queueMicrotask(resolve));

    expect(tabView.querySelector('#tab-specs').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#tab-history').getAttribute('aria-selected')).toBe('false');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
    expect(tabView.querySelector('#panel-history').hidden).toBe(true);
  });

  test('detaching the selected tab selects its neighbour and releases the detached element from hidden', async () => {
    const tabView = createTabView();
    addHistoryTab(tabView);
    onLoad(tabView);
    tabView.querySelector('#tab-specs').click();
    const detached = tabView.querySelector('#panel-specs');

    tabView.querySelector('#tab-specs').remove();
    detached.removeAttribute('data-nt-tab-panel');
    detached.setAttribute('data-nt-tab-detached', 'true');
    detached.hidden = true;
    await new Promise(resolve => queueMicrotask(resolve));

    expect(tabView.querySelector('#tab-history').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-history').hidden).toBe(false);
    expect(tabView.querySelector('#panel-overview').hidden).toBe(true);
    expect(detached.hidden).toBe(false);
  });

  test('detaching the last selected tab selects the previous tab', async () => {
    const tabView = createTabView();
    addHistoryTab(tabView);
    onLoad(tabView);
    tabView.querySelector('#tab-history').click();

    tabView.querySelector('#tab-history').remove();
    await new Promise(resolve => queueMicrotask(resolve));

    expect(tabView.querySelector('#tab-specs').getAttribute('aria-selected')).toBe('true');
    expect(tabView.querySelector('#panel-specs').hidden).toBe(false);
  });
});
