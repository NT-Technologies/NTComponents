import { jest } from '@jest/globals';

function moduleUrl(relativePath) {
  return new URL(`../../../${relativePath}`, import.meta.url).href;
}

// MutationObserver callbacks run as microtasks after the DOM change.
function flushMutations() {
  return new Promise(resolve => setTimeout(resolve, 0));
}

let moduleInstance = 0;

describe('NTBody module', () => {
  let module;
  let originalScrollIntoView;
  let scrollIntoView;

  beforeEach(async () => {
    document.body.innerHTML = '';
    window.history.pushState({}, '', '/start');
    originalScrollIntoView = HTMLElement.prototype.scrollIntoView;
    scrollIntoView = jest.fn();
    HTMLElement.prototype.scrollIntoView = scrollIntoView;
    module = await import(`${moduleUrl('NTComponents/Layout/NTBody.razor.js')}?test=${++moduleInstance}`);
  });

  afterEach(() => {
    module.onDispose();
    delete window.navigation;
    HTMLElement.prototype.scrollIntoView = originalScrollIntoView;
    window.history.pushState({}, '', '/');
    document.body.innerHTML = '';
  });

  // jsdom does not lay out, so the body's scroll offset is a plain value the test can read back.
  function renderBody(content = '<h1>Start</h1>') {
    document.body.innerHTML = `
      <main class="nt-body">
        <nt-page-script src="./_content/NTComponents/Layout/NTBody.razor.js"></nt-page-script>
        <div class="page">${content}</div>
      </main>`;
    const body = document.querySelector('.nt-body');
    let scrollTop = 0;
    let maxScrollTop = Infinity;
    Object.defineProperty(body, 'scrollTop', { configurable: true, get: () => scrollTop, set: value => { scrollTop = Math.min(value, maxScrollTop); } });
    // A real body clamps its offset to how far its content lets it scroll.
    const setMaxScrollTop = value => { maxScrollTop = value; };
    return { body, page: body.querySelector('.page'), script: body.querySelector('nt-page-script'), setMaxScrollTop };
  }

  // The user scrolling: the browser fires scroll after the offset changes.
  function scrollBy(body, top) {
    body.scrollTop = top;
    body.dispatchEvent(new Event('scroll'));
  }

  // jsdom has no Navigation API, so each test names the history entry it is on.
  function goTo(url, entryKey) {
    window.navigation = { currentEntry: { key: entryKey } };
    window.history.pushState({}, '', url);
  }

  test('rendering a new page after the path changes scrolls the body back to the top', async () => {
    const { body, page, script } = renderBody();
    module.onLoad(script);
    body.scrollTop = 900;

    window.history.pushState({}, '', '/next');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();

    expect(body.scrollTop).toBe(0);
  });

  test('a new path with a fragment scrolls its target into view', async () => {
    const { body, page, script } = renderBody();
    module.onLoad(script);
    body.scrollTop = 900;

    window.history.pushState({}, '', '/components#charts');
    page.innerHTML = '<h1>Components</h1><section id="charts">Charts</section>';
    await flushMutations();

    expect(scrollIntoView).toHaveBeenCalledTimes(1);
    expect(scrollIntoView.mock.contexts[0]).toBe(document.getElementById('charts'));
  });

  test('query-string and fragment changes on the same path keep the scroll position', async () => {
    const { body, page, script } = renderBody('<section id="api">API</section>');
    module.onLoad(script);
    body.scrollTop = 900;

    window.history.pushState({}, '', '/start?sort=name');
    page.append(document.createElement('p'));
    await flushMutations();
    window.history.pushState({}, '', '/start?sort=name#api');
    page.append(document.createElement('p'));
    await flushMutations();

    expect(body.scrollTop).toBe(900);
    expect(scrollIntoView).not.toHaveBeenCalled();
  });

  test('an update after the path changes resets the scroll even before new content renders', () => {
    const { body, script } = renderBody();
    module.onLoad(script);
    body.scrollTop = 900;

    window.history.pushState({}, '', '/next');
    module.onUpdate(script);

    expect(body.scrollTop).toBe(0);
  });

  test('loading a page with a fragment scrolls its target into view', () => {
    window.history.pushState({}, '', '/start#details');
    const { script } = renderBody('<section id="details">Details</section>');

    module.onLoad(script);

    expect(scrollIntoView.mock.contexts[0]).toBe(document.getElementById('details'));
  });

  test('a disposed body keeps its scroll position on navigation', async () => {
    const { body, page, script } = renderBody();
    module.onLoad(script);
    body.scrollTop = 900;

    module.onDispose(script);
    window.history.pushState({}, '', '/next');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();

    expect(body.scrollTop).toBe(900);
  });
  test('a fragment target that renders after the new page scrolls into view when it appears', async () => {
    const { body, page, script } = renderBody();
    module.onLoad(script);
    body.scrollTop = 900;

    window.history.pushState({}, '', '/components#charts');
    page.innerHTML = '<h1>Components</h1>';
    await flushMutations();
    expect(body.scrollTop).toBe(0);

    page.insertAdjacentHTML('beforeend', '<section id="charts">Charts</section>');
    await flushMutations();

    expect(scrollIntoView).toHaveBeenCalledTimes(1);
    expect(scrollIntoView.mock.contexts[0]).toBe(document.getElementById('charts'));
  });

  test('loading a page whose fragment target renders later scrolls to it when it appears', async () => {
    window.history.pushState({}, '', '/start#details');
    const { page, script } = renderBody();
    module.onLoad(script);

    page.insertAdjacentHTML('beforeend', '<section id="details">Details</section>');
    await flushMutations();

    expect(scrollIntoView.mock.contexts[0]).toBe(document.getElementById('details'));
  });

  test.each(['wheel', 'touchstart', 'keydown', 'pointerdown'])('a late fragment target does not move the page after the user %s', async eventName => {
    const { body, page, script } = renderBody();
    module.onLoad(script);

    window.history.pushState({}, '', '/components#charts');
    page.innerHTML = '<h1>Components</h1>';
    await flushMutations();
    body.dispatchEvent(new Event(eventName, { bubbles: true }));
    page.insertAdjacentHTML('beforeend', '<section id="charts">Charts</section>');
    await flushMutations();

    expect(scrollIntoView).not.toHaveBeenCalled();
  });

  test('going back to a page restores where the user had scrolled it', async () => {
    goTo('/start', 'start-entry');
    const { body, page, script } = renderBody();
    module.onLoad(script);
    scrollBy(body, 600);

    goTo('/next', 'next-entry');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();
    expect(body.scrollTop).toBe(0);
    scrollBy(body, 250);

    goTo('/start', 'start-entry');
    page.innerHTML = '<h1>Start</h1>';
    await flushMutations();
    expect(body.scrollTop).toBe(600);

    goTo('/next', 'next-entry');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();
    expect(body.scrollTop).toBe(250);
  });

  test('a restored position waits for the page content to grow tall enough', async () => {
    goTo('/start', 'start-entry');
    const { body, page, script, setMaxScrollTop } = renderBody();
    module.onLoad(script);
    scrollBy(body, 600);

    goTo('/next', 'next-entry');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();

    setMaxScrollTop(100);
    goTo('/start', 'start-entry');
    page.innerHTML = '<h1>Loading</h1>';
    await flushMutations();
    expect(body.scrollTop).toBe(100);

    setMaxScrollTop(Infinity);
    page.innerHTML = '<h1>Start</h1><p>Loaded</p>';
    await flushMutations();
    expect(body.scrollTop).toBe(600);
  });

  test('a restored position stops following the content once the user scrolls', async () => {
    goTo('/start', 'start-entry');
    const { body, page, script, setMaxScrollTop } = renderBody();
    module.onLoad(script);
    scrollBy(body, 600);

    goTo('/next', 'next-entry');
    page.innerHTML = '<h1>Next</h1>';
    await flushMutations();

    setMaxScrollTop(100);
    goTo('/start', 'start-entry');
    page.innerHTML = '<h1>Loading</h1>';
    await flushMutations();
    body.dispatchEvent(new Event('wheel'));
    scrollBy(body, 40);

    setMaxScrollTop(Infinity);
    page.innerHTML = '<h1>Start</h1><p>Loaded</p>';
    await flushMutations();
    expect(body.scrollTop).toBe(40);
  });

  test('replacing the current entry with another page starts that page at the top', async () => {
    goTo('/start', 'start-entry');
    const { body, page, script } = renderBody();
    module.onLoad(script);
    scrollBy(body, 600);

    window.history.replaceState({}, '', '/redirected');
    page.innerHTML = '<h1>Redirected</h1>';
    await flushMutations();

    expect(body.scrollTop).toBe(0);
  });
});
