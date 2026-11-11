import { jest } from '@jest/globals';

function moduleUrl(relativePath) {
  return new URL(`../../../${relativePath}`, import.meta.url).href;
}

async function waitForAnimationFrame(ms = 580) {
  await jest.advanceTimersByTimeAsync(ms);
}

let moduleInstance = 0;

describe('NTNavigationRail module', () => {
  let module;
  let originalMatchMedia;

  beforeEach(async () => {
    jest.useFakeTimers();
    document.body.innerHTML = '';
    window.NTComponents = undefined;
    originalMatchMedia = window.matchMedia;
    // The module keeps expanded state per rail id in a module-level map; load a fresh instance per test so state cannot leak between tests.
    module = await import(`${moduleUrl('NTComponents/NavRail/NTNavigationRail.razor.js')}?test=${++moduleInstance}`);
  });

  afterEach(() => {
    module.onDispose();
    window.NTComponents = undefined;
    window.matchMedia = originalMatchMedia;
    document.body.innerHTML = '';
    jest.useRealTimers();
  });

  test('menu button toggles collapsed and expanded rail state in place', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <a class="nt-navigation-rail-item" href="/">
          <span class="nt-navigation-rail-item-body">
            <span class="nt-navigation-rail-item-content">
              <span class="nt-navigation-rail-item-label">Home</span>
            </span>
          </span>
        </a>
      </nav>`;
    const registeredElements = [];
    window.NTComponents = {
      registerButtonInteraction: element => registeredElements.push(element)
    };

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');
    const icon = document.querySelector('.nt-navigation-rail-menu-icon .tnt-icon');
    const item = document.querySelector('.nt-navigation-rail-item');

    expect(registeredElements).toEqual([button, item]);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-expanding')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsing')).toBe(false);

    button.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-expanding')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsing')).toBe(false);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(button.getAttribute('aria-label')).toBe('Collapse navigation rail');
    expect(icon.textContent).toBe('menu_open');

    button.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-expanding')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsing')).toBe(true);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
    expect(button.getAttribute('aria-expanded')).toBe('false');
    expect(button.getAttribute('aria-label')).toBe('Expand navigation rail');
    expect(icon.textContent).toBe('menu');
  });

  test('repeated module loads do not register duplicate menu button handlers', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <a class="nt-navigation-rail-item" href="/">Home</a>
      </nav>`;

    module.onLoad();
    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');

    button.click();
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);

    module.onLoad();
    button.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  test('disposing a page-script marker does not unregister other active rails', () => {
    document.body.innerHTML = `
      <nav id="primary-rail" class="nt-navigation-rail nt-navigation-rail-expanded">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Collapse navigation rail"
                aria-expanded="true"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="primary-buttons-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Buttons
          </button>
          <div id="primary-buttons-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/buttons">Buttons</a>
            </div>
          </div>
        </div>
      </nav>
      <tnt-page-script src="./_content/NTComponents/NavRail/NTNavigationRail.razor.js"></tnt-page-script>`;

    module.onLoad();

    const marker = document.querySelector('tnt-page-script');
    const rail = document.querySelector('#primary-rail');
    const menuButton = rail.querySelector('.nt-navigation-rail-menu-button');
    const groupTrigger = rail.querySelector('.nt-navigation-rail-group-trigger');

    module.onDispose(marker);
    groupTrigger.click();

    expect(groupTrigger.getAttribute('aria-expanded')).toBe('false');
    expect(groupTrigger.closest('.nt-navigation-rail-group').classList.contains('nt-navigation-rail-group-open')).toBe(false);

    menuButton.click();

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(menuButton.getAttribute('aria-expanded')).toBe('false');
  });

  test('open by default expands collapsed rail on initial load for medium and larger screens', () => {
    window.matchMedia = jest.fn(query => ({
      matches: query === '(min-width: 840px)',
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed"
           data-nt-navigation-rail-open-by-default="true">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <a class="nt-navigation-rail-item" href="/">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');
    const icon = document.querySelector('.nt-navigation-rail-menu-icon .tnt-icon');
    const item = document.querySelector('.nt-navigation-rail-item');

    expect(window.matchMedia).toHaveBeenCalledWith('(min-width: 840px)');
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(false);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(button.getAttribute('aria-label')).toBe('Collapse navigation rail');
    expect(icon.textContent).toBe('menu_open');
  });

  test('open by default does not expand on screens below medium', () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed"
           data-nt-navigation-rail-open-by-default="true">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');
    const item = document.querySelector('.nt-navigation-rail-item');

    expect(window.matchMedia).toHaveBeenCalledWith('(min-width: 840px)');
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  test('open by default collapses server-expanded rail below medium', () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-expanded"
           data-nt-navigation-rail-open-by-default="true">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Collapse navigation rail"
                aria-expanded="true"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item nt-navigation-rail-item-expanded" href="/">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');
    const item = document.querySelector('.nt-navigation-rail-item');

    expect(window.matchMedia).toHaveBeenCalledWith('(min-width: 840px)');
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(item.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  test('open by default is applied only once and does not reopen after user collapse', () => {
    window.matchMedia = jest.fn(query => ({
      matches: query === '(min-width: 840px)',
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed"
           data-nt-navigation-rail-open-by-default="true">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);

    button.click();
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);

    module.onUpdate();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  test('expanded state survives navigation updates that rerender collapsed classes', () => {
    document.body.innerHTML = `
      <nav id="persistent-rail" class="nt-navigation-rail nt-navigation-rail-collapsed" data-permanent>
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const button = document.querySelector('.nt-navigation-rail-menu-button');

    button.click();
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);

    rail.classList.remove('nt-navigation-rail-expanded');
    rail.classList.add('nt-navigation-rail-collapsed');
    button.setAttribute('aria-expanded', 'false');

    module.onUpdate();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(false);
    expect(button.getAttribute('aria-expanded')).toBe('true');
  });

  test('expanded state survives replacement with a rail that has the same stable id', () => {
    document.body.innerHTML = `
      <nav id="replaced-rail" class="nt-navigation-rail nt-navigation-rail-collapsed" data-permanent>
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const originalRail = document.querySelector('.nt-navigation-rail');
    originalRail.querySelector('.nt-navigation-rail-menu-button').click();
    expect(originalRail.classList.contains('nt-navigation-rail-expanded')).toBe(true);

    originalRail.outerHTML = `
      <nav id="replaced-rail" class="nt-navigation-rail nt-navigation-rail-collapsed" data-permanent>
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const replacementRail = document.querySelector('.nt-navigation-rail');
    const replacementButton = replacementRail.querySelector('.nt-navigation-rail-menu-button');

    expect(replacementRail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(replacementRail.classList.contains('nt-navigation-rail-collapsed')).toBe(false);
    expect(replacementButton.getAttribute('aria-expanded')).toBe('true');
  });

  test('expanded rail module applies item and group presentation from declarative state', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-expanded nt-navigation-rail-indicator-full">
        <div class="nt-navigation-rail-section-header" role="heading" aria-level="2">Primary</div>
        <a class="nt-navigation-rail-item" href="/reports">Reports</a>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            <span class="nt-navigation-rail-item-icon" aria-hidden="true">
              <span class="tnt-icon material-symbols-outlined">workspaces</span>
            </span>
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <div class="nt-navigation-rail-section-header" role="heading" aria-level="3">Workspace</div>
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
            </div>
          </div>
        </div>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="false">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="admin-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Admin
          </button>
          <div id="admin-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/users">Users</a>
            </div>
          </div>
        </div>
      </nav>`;

    module.onLoad();

    const items = Array.from(document.querySelectorAll('.nt-navigation-rail-item'));
    const openGroup = document.querySelector('[aria-controls="workspace-panel"]').closest('.nt-navigation-rail-group');
    const collapsedGroup = document.querySelector('[aria-controls="admin-panel"]').closest('.nt-navigation-rail-group');
    const openPanel = document.querySelector('#workspace-panel');
    const collapsedPanel = document.querySelector('#admin-panel');
    const headers = Array.from(document.querySelectorAll('.nt-navigation-rail-section-header'));

    expect(items.every(item => item.classList.contains('nt-navigation-rail-item-expanded'))).toBe(true);
    expect(items.every(item => item.classList.contains('nt-navigation-rail-item-indicator-full'))).toBe(true);
    expect(headers.every(header => header.classList.contains('nt-navigation-rail-section-header-expanded'))).toBe(true);
    expect(openGroup.classList.contains('nt-navigation-rail-group-rail-expanded')).toBe(true);
    expect(openGroup.classList.contains('nt-navigation-rail-group-open')).toBe(true);
    expect(openGroup.querySelector(':scope > .nt-navigation-rail-group-trigger').getAttribute('aria-expanded')).toBe('true');
    expect(openPanel.hasAttribute('popover')).toBe(false);
    expect(collapsedGroup.classList.contains('nt-navigation-rail-group-rail-expanded')).toBe(true);
    expect(collapsedGroup.classList.contains('nt-navigation-rail-group-open')).toBe(false);
    expect(collapsedGroup.querySelector(':scope > .nt-navigation-rail-group-trigger').getAttribute('aria-expanded')).toBe('false');
    expect(collapsedPanel.hasAttribute('popover')).toBe(false);
  });

  test('group trigger opens a collapsed popover and toggles inline when rail is expanded', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            <span class="nt-navigation-rail-item-icon" aria-hidden="true">
              <span class="tnt-icon material-symbols-outlined">workspaces</span>
            </span>
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
            </div>
          </div>
        </div>
      </nav>`;

    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const group = document.querySelector('.nt-navigation-rail-group');
    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    const childItem = panel.querySelector('.nt-navigation-rail-item');
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({
      top: 24,
      left: 20,
      right: 76,
      bottom: 80,
      width: 56,
      height: 56
    }));
    panel.getBoundingClientRect = jest.fn(() => ({
      top: 0,
      left: 0,
      right: 200,
      bottom: 160,
      width: 200,
      height: 160
    }));
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    panel.hidePopover = jest.fn(() => {
      popoverOpen = false;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);

    const registeredElements = [];
    window.NTComponents = {
      registerButtonInteraction: element => registeredElements.push(element)
    };

    module.onLoad();

    expect(registeredElements).toEqual([menuButton, trigger, childItem]);
    expect(childItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);

    trigger.click();

    expect(panel.showPopover).toHaveBeenCalledTimes(1);
    expect(panel.style.left).toBe('80px');
    expect(panel.style.top).toBe('24px');
    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(childItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);

    menuButton.click();

    expect(panel.hidePopover).toHaveBeenCalledTimes(1);
    expect(panel.hasAttribute('popover')).toBe(false);
    expect(trigger.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
    expect(group.classList.contains('nt-navigation-rail-group-open')).toBe(true);
    expect(trigger.getAttribute('aria-expanded')).toBe('true');

    trigger.click();

    expect(group.classList.contains('nt-navigation-rail-group-open')).toBe(false);
    expect(trigger.getAttribute('aria-expanded')).toBe('false');

    trigger.click();

    expect(group.classList.contains('nt-navigation-rail-group-open')).toBe(true);
    expect(trigger.getAttribute('aria-expanded')).toBe('true');

    menuButton.click();

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(group.classList.contains('nt-navigation-rail-group-open')).toBe(false);
    expect(group.dataset.ntNavigationRailGroupOpen).toBe('true');
    expect(panel.hasAttribute('popover')).toBe(true);
    expect(panel.classList.contains('nt-navigation-rail-group-panel-converting')).toBe(true);
    expect(panel.showPopover).toHaveBeenCalledTimes(1);
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
  });

  test('open group popover follows its trigger when the navigation items scroll', async () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-items">
          <div class="nt-navigation-rail-group">
            <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                    type="button"
                    aria-expanded="false"
                    aria-controls="workspace-panel"
                    data-nt-navigation-rail-group-trigger="true">
              Workspace
            </button>
            <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
              <div class="nt-navigation-rail-group-items">
                <a class="nt-navigation-rail-item" href="/projects">Projects</a>
              </div>
            </div>
          </div>
        </div>
      </nav>`;

    const items = document.querySelector('.nt-navigation-rail-items');
    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    let triggerTop = 96;
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({
      top: triggerTop,
      left: 20,
      right: 76,
      bottom: triggerTop + 56,
      width: 56,
      height: 56
    }));
    panel.getBoundingClientRect = jest.fn(() => ({
      top: 0,
      left: 0,
      right: 200,
      bottom: 160,
      width: 200,
      height: 160
    }));
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    panel.hidePopover = jest.fn(() => {
      popoverOpen = false;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);

    module.onLoad();

    trigger.click();
    expect(panel.style.top).toBe('96px');

    triggerTop = 42;
    items.dispatchEvent(new Event('scroll'));
    await waitForAnimationFrame();

    expect(panel.style.top).toBe('42px');
  });

  test('group popover opens upward when it would overflow the viewport bottom', () => {
    const originalInnerHeight = window.innerHeight;
    const originalInnerWidth = window.innerWidth;
    Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 });

    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
            </div>
          </div>
        </div>
      </nav>`;

    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({
      top: 520,
      left: 20,
      right: 76,
      bottom: 576,
      width: 56,
      height: 56
    }));
    panel.getBoundingClientRect = jest.fn(() => ({
      top: 0,
      left: 0,
      right: 200,
      bottom: 180,
      width: 200,
      height: 180
    }));
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);

    module.onLoad();

    trigger.click();

    expect(panel.style.top).toBe('396px');
    expect(panel.style.transformOrigin).toBe('bottom left');
    expect(panel.classList.contains('nt-navigation-rail-group-panel-open-upward')).toBe(true);

    Object.defineProperty(window, 'innerHeight', { configurable: true, value: originalInnerHeight });
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalInnerWidth });
  });

  test('group popover uses fallback dimensions for initial upward placement', () => {
    const originalInnerHeight = window.innerHeight;
    const originalInnerWidth = window.innerWidth;
    Object.defineProperty(window, 'innerHeight', { configurable: true, value: 600 });
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1024 });

    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
            </div>
          </div>
        </div>
      </nav>`;

    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({
      top: 520,
      left: 20,
      right: 76,
      bottom: 576,
      width: 56,
      height: 56
    }));
    panel.getBoundingClientRect = jest.fn(() => ({
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 0
    }));
    Object.defineProperty(panel, 'scrollWidth', { configurable: true, value: 200 });
    Object.defineProperty(panel, 'scrollHeight', { configurable: true, value: 180 });
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);

    module.onLoad();

    trigger.click();

    expect(panel.style.top).toBe('396px');
    expect(panel.style.left).toBe('80px');
    expect(panel.style.transformOrigin).toBe('bottom left');
    expect(panel.classList.contains('nt-navigation-rail-group-panel-open-upward')).toBe(true);

    Object.defineProperty(window, 'innerHeight', { configurable: true, value: originalInnerHeight });
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalInnerWidth });
  });

  test('arrow keys move focus among enabled visible destinations without wrapping', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-expanded">
        <button class="nt-navigation-rail-menu-button" type="button" aria-expanded="true">Menu</button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
        <a class="nt-navigation-rail-item tnt-disabled" href="/disabled" aria-disabled="true" tabindex="-1">Disabled</a>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="false">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="admin-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Admin
          </button>
          <div id="admin-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/hidden">Hidden</a>
            </div>
          </div>
        </div>
        <a class="nt-navigation-rail-item" href="/reports">Reports</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const home = document.querySelector('[href="/home"]');
    const admin = document.querySelector('.nt-navigation-rail-group-trigger');
    const reports = document.querySelector('[href="/reports"]');

    home.focus();
    home.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(admin);

    admin.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    expect(document.activeElement).toBe(reports);

    reports.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(reports);

    reports.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true }));
    expect(document.activeElement).toBe(home);

    rail.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true }));
    expect(document.activeElement).toBe(reports);
  });

  test('collapsed inline group panels stay out of sequential focus order', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-expanded">
        <a class="nt-navigation-rail-item" href="/home">Home</a>
        <div class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="false">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="admin-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Admin
          </button>
          <div id="admin-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/users">Users</a>
            </div>
          </div>
        </div>
        <a class="nt-navigation-rail-item" href="/reports">Reports</a>
      </nav>`;

    module.onLoad();

    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    const users = document.querySelector('[href="/users"]');
    const reports = document.querySelector('[href="/reports"]');

    expect(panel.hasAttribute('popover')).toBe(false);
    expect(panel.hasAttribute('inert')).toBe(true);
    expect(panel.getAttribute('aria-hidden')).toBe('');

    trigger.focus();
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(reports);

    trigger.click();
    expect(panel.hasAttribute('inert')).toBe(false);
    expect(panel.hasAttribute('aria-hidden')).toBe(false);

    trigger.focus();
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(users);

    trigger.click();
    expect(panel.hasAttribute('inert')).toBe(true);
    expect(document.activeElement).toBe(trigger);
  });

  test('limit-to-one-expanded closes other top-level groups without closing nested groups', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-expanded"
           data-nt-navigation-rail-limit-to-one-expanded="true">
        <div id="workspace-group" class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="true"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel">
            <div class="nt-navigation-rail-group-items">
              <div id="nested-group" class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
                <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                        type="button"
                        aria-expanded="true"
                        aria-controls="nested-panel"
                        data-nt-navigation-rail-group-trigger="true">
                  Workspace tools
                </button>
                <div id="nested-panel" class="nt-navigation-rail-group-panel">
                  <div class="nt-navigation-rail-group-items">
                    <a class="nt-navigation-rail-item" href="/projects">Projects</a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div id="admin-group" class="nt-navigation-rail-group" data-nt-navigation-rail-group-expanded="true">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="true"
                  aria-controls="admin-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Administration
          </button>
          <div id="admin-panel" class="nt-navigation-rail-group-panel">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/users">Users</a>
            </div>
          </div>
        </div>
      </nav>`;

    module.onLoad();

    const workspaceGroup = document.querySelector('#workspace-group');
    const nestedGroup = document.querySelector('#nested-group');
    const adminGroup = document.querySelector('#admin-group');
    const workspaceTrigger = workspaceGroup.querySelector(':scope > .nt-navigation-rail-group-trigger');
    const nestedTrigger = nestedGroup.querySelector(':scope > .nt-navigation-rail-group-trigger');
    const adminTrigger = adminGroup.querySelector(':scope > .nt-navigation-rail-group-trigger');

    expect(workspaceTrigger.getAttribute('aria-expanded')).toBe('true');
    expect(nestedTrigger.getAttribute('aria-expanded')).toBe('true');
    expect(adminTrigger.getAttribute('aria-expanded')).toBe('false');

    adminTrigger.click();

    expect(workspaceTrigger.getAttribute('aria-expanded')).toBe('false');
    expect(nestedTrigger.getAttribute('aria-expanded')).toBe('true');
    expect(adminTrigger.getAttribute('aria-expanded')).toBe('true');
  });

  test('escape closes a collapsed group popover and restores trigger focus', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
            </div>
          </div>
        </div>
      </nav>`;

    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({ top: 24, left: 20, right: 76, bottom: 80, width: 56, height: 56 }));
    panel.getBoundingClientRect = jest.fn(() => ({ top: 0, left: 0, right: 200, bottom: 160, width: 200, height: 160 }));
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    panel.hidePopover = jest.fn(() => {
      popoverOpen = false;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);

    module.onLoad();
    trigger.click();
    expect(trigger.getAttribute('aria-expanded')).toBe('true');

    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(panel.hidePopover).toHaveBeenCalledTimes(1);
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(trigger);
  });

  test('data-nt-close-on-navigation closes the destination link popover when enabled', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <a class="nt-navigation-rail-item" href="/projects"><span>Projects</span></a>
            </div>
          </div>
        </div>
      </nav>`;

    const rail = document.querySelector('.nt-navigation-rail');
    const trigger = document.querySelector('.nt-navigation-rail-group-trigger');
    const panel = document.querySelector('.nt-navigation-rail-group-panel');
    const item = document.querySelector('a.nt-navigation-rail-item');
    let popoverOpen = false;

    trigger.getBoundingClientRect = jest.fn(() => ({ top: 24, left: 20, right: 76, bottom: 80, width: 56, height: 56 }));
    panel.getBoundingClientRect = jest.fn(() => ({ top: 0, left: 0, right: 200, bottom: 160, width: 200, height: 160 }));
    panel.showPopover = jest.fn(() => {
      popoverOpen = true;
      panel.dispatchEvent(new Event('toggle'));
    });
    panel.hidePopover = jest.fn(() => {
      popoverOpen = false;
      panel.dispatchEvent(new Event('toggle'));
    });
    const matches = panel.matches.bind(panel);
    panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);
    item.addEventListener('click', event => event.preventDefault());

    module.onLoad();
    trigger.click();
    item.querySelector('span').click();

    expect(panel.hidePopover).not.toHaveBeenCalled();

    rail.setAttribute('data-nt-close-on-navigation', 'true');
    item.querySelector('span').click();

    expect(panel.hidePopover).toHaveBeenCalledTimes(1);
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
  });

  test.each([
    { outerEnabled: true, innerEnabled: true, outerCloses: true, innerCloses: true },
    { outerEnabled: true, innerEnabled: false, outerCloses: true, innerCloses: false },
    { outerEnabled: false, innerEnabled: true, outerCloses: false, innerCloses: true },
    { outerEnabled: false, innerEnabled: false, outerCloses: false, innerCloses: false }
  ])('nested rails close only opted-in owned popovers when outer=$outerEnabled and inner=$innerEnabled', ({ outerEnabled, innerEnabled, outerCloses, innerCloses }) => {
    document.body.innerHTML = `
      <nav id="outer-rail" class="nt-navigation-rail nt-navigation-rail-collapsed" ${outerEnabled ? 'data-nt-close-on-navigation="true"' : ''}>
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="outer-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Outer
          </button>
          <div id="outer-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <nav id="inner-rail" class="nt-navigation-rail nt-navigation-rail-collapsed" ${innerEnabled ? 'data-nt-close-on-navigation="true"' : ''}>
              <div class="nt-navigation-rail-group">
                <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                        type="button"
                        aria-expanded="false"
                        aria-controls="inner-panel"
                        data-nt-navigation-rail-group-trigger="true">
                  Inner
                </button>
                <div id="inner-panel" class="nt-navigation-rail-group-panel" popover="auto">
                  <a class="nt-navigation-rail-item" href="/nested"><span>Nested</span></a>
                </div>
              </div>
            </nav>
          </div>
        </div>
      </nav>`;

    const outerTrigger = document.querySelector('#outer-rail > .nt-navigation-rail-group > .nt-navigation-rail-group-trigger');
    const outerPanel = document.querySelector('#outer-panel');
    const innerTrigger = document.querySelector('#inner-rail .nt-navigation-rail-group-trigger');
    const innerPanel = document.querySelector('#inner-panel');
    const item = document.querySelector('a.nt-navigation-rail-item');

    const configurePopover = panel => {
      let popoverOpen = false;
      const matches = panel.matches.bind(panel);
      panel.matches = selector => selector === ':popover-open' ? popoverOpen : matches(selector);
      panel.hidePopover = jest.fn(() => {
        popoverOpen = false;
        panel.dispatchEvent(new Event('toggle'));
      });

      return () => {
        popoverOpen = true;
        panel.dispatchEvent(new Event('toggle'));
      };
    };

    const openOuter = configurePopover(outerPanel);
    const openInner = configurePopover(innerPanel);
    item.addEventListener('click', event => event.preventDefault());

    module.onLoad();
    openOuter();
    openInner();
    item.querySelector('span').click();

    expect(outerPanel.hidePopover).toHaveBeenCalledTimes(outerCloses ? 1 : 0);
    expect(innerPanel.hidePopover).toHaveBeenCalledTimes(innerCloses ? 1 : 0);
    expect(outerTrigger.getAttribute('aria-expanded')).toBe(outerCloses ? 'false' : 'true');
    expect(innerTrigger.getAttribute('aria-expanded')).toBe(innerCloses ? 'false' : 'true');
  });

  test('modal rail expansion isolates background focus and escape restores the menu button', async () => {
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <nav class="nt-navigation-rail nt-navigation-rail-modal nt-navigation-rail-collapsed"
           b-testscope=""
           style="--nt-navigation-rail-scrim-color: var(--nt-color-primary);">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
        <a class="nt-navigation-rail-item" href="/reports">Reports</a>
      </nav>`;

    module.onLoad();

    const main = document.querySelector('#content');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const firstItem = document.querySelector('.nt-navigation-rail-item');

    menuButton.focus();
    menuButton.click();

    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog).not.toBeNull();
    expect(dialog.open).toBe(true);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-entering')).toBe(true);
    expect(dialog.contains(rail)).toBe(true);
    expect(dialog.hasAttribute('b-testscope')).toBe(true);
    expect(dialog.style.getPropertyValue('--nt-navigation-rail-scrim-color')).toBe('var(--nt-color-primary)');
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder').hasAttribute('b-testscope')).toBe(true);
    expect(main.inert).toBe(true);
    expect(main.getAttribute('aria-hidden')).toBe('true');
    expect(document.activeElement).toBe(firstItem);

    firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(menuButton.getAttribute('aria-expanded')).toBe('false');
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-exiting')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).not.toBeNull();
    expect(main.inert).toBe(true);

    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder')).toBeNull();
    expect(document.body.contains(rail)).toBe(true);
    expect(main.inert).not.toBe(true);
    expect(main.hasAttribute('aria-hidden')).toBe(false);
    expect(document.activeElement).toBe(menuButton);
  });

  test('authored modal dialog handlers are removed on dispose', () => {
    document.body.innerHTML = `
      <dialog class="nt-navigation-rail-modal-dialog">
        <nav class="nt-navigation-rail nt-navigation-rail-modal nt-navigation-rail-collapsed">
          <button class="nt-navigation-rail-menu-button"
                  type="button"
                  aria-label="Expand navigation rail"
                  aria-expanded="false"
                  data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                  data-nt-navigation-rail-collapsed-label="Expand navigation rail">
            Menu
          </button>
          <a class="nt-navigation-rail-item" href="/home">Home</a>
        </nav>
      </dialog>`;

    module.onLoad();

    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');

    menuButton.click();
    expect(dialog.hasAttribute('open')).toBe(true);

    module.onDispose(rail);
    rail.classList.add('nt-navigation-rail-expanded');
    rail.classList.remove('nt-navigation-rail-collapsed');
    menuButton.setAttribute('aria-expanded', 'true');

    dialog.dispatchEvent(new Event('close'));

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(false);
    expect(menuButton.getAttribute('aria-expanded')).toBe('true');
  });

  test('standard rail expansion becomes modal below medium screens', async () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
        <a class="nt-navigation-rail-item" href="/reports">Reports</a>
      </nav>`;

    module.onLoad();

    const main = document.querySelector('#content');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const firstItem = document.querySelector('.nt-navigation-rail-item');

    expect(rail.classList.contains('nt-navigation-rail-responsive-modal')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(main.inert).not.toBe(true);

    menuButton.focus();
    menuButton.click();

    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog).not.toBeNull();
    expect(dialog.open).toBe(true);
    expect(dialog.contains(rail)).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder')).not.toBeNull();
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(main.inert).toBe(true);
    expect(main.getAttribute('aria-hidden')).toBe('true');
    expect(document.activeElement).toBe(firstItem);

    firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-collapsing')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-responsive-modal')).toBe(true);
    expect(firstItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-exiting')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).not.toBeNull();
    expect(main.inert).toBe(true);

    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder')).toBeNull();
    expect(document.body.contains(rail)).toBe(true);
    expect(main.inert).not.toBe(true);
    expect(main.hasAttribute('aria-hidden')).toBe(false);
    expect(document.activeElement).toBe(menuButton);
    expect(rail.classList.contains('nt-navigation-rail-collapsing')).toBe(false);
    expect(firstItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(false);
  });

  test('nested rail expansion stays within its layout from small screens upward', () => {
    window.matchMedia = jest.fn(query => ({
      matches: query === '(min-width: 840px), (min-width: 600px) and (orientation: landscape)',
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <section class="nt-layout nt-layout-nested">
        <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
          <button class="nt-navigation-rail-menu-button"
                  type="button"
                  aria-label="Expand nested navigation rail"
                  aria-expanded="false"
                  data-nt-navigation-rail-expanded-label="Collapse nested navigation rail"
                  data-nt-navigation-rail-collapsed-label="Expand nested navigation rail">
            Menu
          </button>
          <a class="nt-navigation-rail-item" href="/home">Home</a>
        </nav>
        <section class="nt-body"><button id="nested-content">Nested content</button></section>
      </section>`;

    module.onLoad();

    const layout = document.querySelector('.nt-layout-nested');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const nestedContent = document.querySelector('#nested-content');

    menuButton.click();

    expect(rail.classList.contains('nt-navigation-rail-responsive-modal')).toBe(true);
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(rail.parentElement).toBe(layout);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(nestedContent.inert).not.toBe(true);

    nestedContent.dispatchEvent(new Event('pointerdown', { bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(document.activeElement).toBe(menuButton);
  });

  test('nested rail expansion becomes modal on extra-small screens', async () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <section class="nt-layout nt-layout-nested">
        <nav class="nt-navigation-rail nt-navigation-rail-hide-on-xs nt-navigation-rail-collapsed">
          <button class="nt-navigation-rail-menu-button"
                  type="button"
                  aria-label="Expand nested navigation rail"
                  aria-expanded="false"
                  data-nt-navigation-rail-expanded-label="Collapse nested navigation rail"
                  data-nt-navigation-rail-collapsed-label="Expand nested navigation rail">
            Menu
          </button>
          <a class="nt-navigation-rail-item" href="/home">Home</a>
        </nav>
        <section class="nt-body"><button id="nested-content">Nested content</button></section>
      </section>`;

    module.onLoad();

    const layout = document.querySelector('.nt-layout-nested');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const firstItem = document.querySelector('.nt-navigation-rail-item');
    const nestedContent = document.querySelector('#nested-content');
    const nestedBody = nestedContent.closest('.nt-body');

    menuButton.click();

    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog).not.toBeNull();
    expect(dialog.open).toBe(true);
    expect(dialog.contains(rail)).toBe(true);
    expect(nestedBody.inert).toBe(true);

    firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(rail.parentElement).toBe(layout);
    expect(nestedBody.inert).not.toBe(true);
    expect(document.activeElement).toBe(menuButton);
  });

  test('expanded modal rail does not restart enter animation on update', () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');

    menuButton.click();

    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog).not.toBeNull();
    expect(dialog.open).toBe(true);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-entering')).toBe(true);

    dialog.classList.remove('nt-navigation-rail-modal-dialog-entering');
    module.onUpdate();

    expect(dialog.open).toBe(true);
    expect(dialog.contains(rail)).toBe(true);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-entering')).toBe(false);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-exiting')).toBe(false);
  });

  test('xs-only external trigger opens the hidden rail as a modal', async () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <button class="nt-navigation-rail-xs-menu-button nt-navigation-rail-menu-button"
              type="button"
              aria-label="Expand navigation rail"
              aria-expanded="false"
              aria-controls="primary-rail"
              data-nt-navigation-rail-external-trigger="true"
              data-nt-navigation-rail-expanded-label="Collapse navigation rail"
              data-nt-navigation-rail-collapsed-label="Expand navigation rail"
              data-nt-navigation-rail-expanded-icon="menu_open"
              data-nt-navigation-rail-collapsed-icon="menu">
        <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
          <span class="tnt-icon">menu</span>
        </span>
      </button>
      <nav id="primary-rail" class="nt-navigation-rail nt-navigation-rail-hide-on-xs nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const externalButton = document.querySelector('.nt-navigation-rail-xs-menu-button');
    const internalButton = rail.querySelector('.nt-navigation-rail-menu-button');
    const firstItem = document.querySelector('.nt-navigation-rail-item');

    externalButton.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog?.open).toBe(true);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-hide-on-xs')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder').classList.contains('nt-navigation-rail-modal-placeholder-hide-on-xs')).toBe(true);
    expect(externalButton.getAttribute('aria-expanded')).toBe('true');
    expect(internalButton.getAttribute('aria-expanded')).toBe('true');
    expect(externalButton.getAttribute('aria-label')).toBe('Collapse navigation rail');
    expect(externalButton.querySelector('.tnt-icon').textContent).toBe('menu_open');

    firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(externalButton.getAttribute('aria-expanded')).toBe('false');
    expect(internalButton.getAttribute('aria-expanded')).toBe('false');
    expect(firstItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);

    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(document.activeElement).toBe(externalButton);
  });

  test('hide collapse behavior external trigger opens the hidden rail as a modal', async () => {
    window.matchMedia = jest.fn(query => ({
      matches: true,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <button class="nt-navigation-rail-xs-menu-button nt-navigation-rail-hide-menu-button nt-navigation-rail-menu-button"
              type="button"
              aria-label="Expand navigation rail"
              aria-expanded="false"
              aria-controls="primary-rail"
              data-nt-navigation-rail-external-trigger="true"
              data-nt-navigation-rail-expanded-label="Collapse navigation rail"
              data-nt-navigation-rail-collapsed-label="Expand navigation rail"
              data-nt-navigation-rail-expanded-icon="menu_open"
              data-nt-navigation-rail-collapsed-icon="menu">
        <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
          <span class="tnt-icon">menu</span>
        </span>
      </button>
      <nav id="primary-rail" class="nt-navigation-rail nt-navigation-rail-hide-when-collapsed nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail"
                data-nt-navigation-rail-expanded-icon="menu_open"
                data-nt-navigation-rail-collapsed-icon="menu">
          <span class="nt-navigation-rail-menu-icon" aria-hidden="true">
            <span class="tnt-icon">menu</span>
          </span>
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const externalButton = document.querySelector('.nt-navigation-rail-hide-menu-button');
    const internalButton = rail.querySelector('.nt-navigation-rail-menu-button');
    const firstItem = document.querySelector('.nt-navigation-rail-item');

    externalButton.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    const dialog = document.querySelector('.nt-navigation-rail-modal-dialog');
    expect(dialog?.open).toBe(true);
    expect(dialog.classList.contains('nt-navigation-rail-modal-dialog-hide-when-collapsed')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-placeholder').classList.contains('nt-navigation-rail-modal-placeholder-hide-when-collapsed')).toBe(true);
    expect(externalButton.getAttribute('aria-expanded')).toBe('true');
    expect(internalButton.getAttribute('aria-expanded')).toBe('true');

    firstItem.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(externalButton.getAttribute('aria-expanded')).toBe('false');
    expect(internalButton.getAttribute('aria-expanded')).toBe('false');
    expect(firstItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);

    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(document.activeElement).toBe(externalButton);
  });

  test('external trigger is rebound when Blazor replaces the xs menu button', () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <button id="primary-rail-xs-menu-button"
              class="nt-navigation-rail-xs-menu-button nt-navigation-rail-menu-button"
              type="button"
              aria-label="Expand navigation rail"
              aria-expanded="false"
              aria-controls="primary-rail"
              data-nt-navigation-rail-external-trigger="true"
              data-nt-navigation-rail-expanded-label="Collapse navigation rail"
              data-nt-navigation-rail-collapsed-label="Expand navigation rail">
        Menu
      </button>
      <nav id="primary-rail" class="nt-navigation-rail nt-navigation-rail-hide-on-xs nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const originalExternalButton = document.querySelector('#primary-rail-xs-menu-button');
    originalExternalButton.replaceWith(originalExternalButton.cloneNode(true));
    const replacementExternalButton = document.querySelector('#primary-rail-xs-menu-button');

    module.onUpdate();
    originalExternalButton.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(false);

    replacementExternalButton.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(replacementExternalButton.getAttribute('aria-expanded')).toBe('true');
    expect(rail.querySelector('.nt-navigation-rail-menu-button').getAttribute('aria-expanded')).toBe('true');
  });

  test('responsive modal rail closes on outside pointer interaction only', async () => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');
    const outside = document.querySelector('#outside');

    menuButton.click();
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')?.open).toBe(true);

    rail.dispatchEvent(new Event('pointerdown', { bubbles: true }));
    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);

    outside.dispatchEvent(new Event('pointerdown', { bubbles: true }));

    expect(rail.classList.contains('nt-navigation-rail-collapsed')).toBe(true);
    expect(menuButton.getAttribute('aria-expanded')).toBe('false');
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).not.toBeNull();

    await waitForAnimationFrame();

    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(document.activeElement).toBe(menuButton);
  });

  test.each([
    ['closes', true],
    ['stays open', false]
  ])('modal rail %s when a destination is selected with close-on-navigation %s', async (_, closeOnNavigation) => {
    window.matchMedia = jest.fn(query => ({
      matches: false,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"></main>
      <nav class="nt-navigation-rail nt-navigation-rail-hide-on-xs nt-navigation-rail-collapsed" ${closeOnNavigation ? 'data-nt-close-on-navigation="true"' : ''}>
        <button class="nt-navigation-rail-menu-button" type="button" aria-expanded="false">Menu</button>
        <a class="nt-navigation-rail-item" href="#reports">Reports</a>
      </nav>`;

    module.onLoad();

    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');

    menuButton.click();
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')?.open).toBe(true);

    document.querySelector('.nt-navigation-rail-item').click();
    await waitForAnimationFrame();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(!closeOnNavigation);
    expect(menuButton.getAttribute('aria-expanded')).toBe(String(!closeOnNavigation));
    expect(document.querySelector('.nt-navigation-rail-modal-dialog') !== null).toBe(!closeOnNavigation);
  });

  test.each([
    ['small landscape or wider shows the collapsed rail', true, false],
    ['extra-small or small portrait uses the hidden modal layout', false, true]
  ])('hide-on-xs rail: %s', (_, railScreen, expandedItemLayout) => {
    const railScreenQuery = '(min-width: 840px), (min-width: 600px) and (orientation: landscape)';
    window.matchMedia = jest.fn(query => ({
      matches: railScreen && query === railScreenQuery,
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-hide-on-xs nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button" type="button" aria-expanded="false">Menu</button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    expect(window.matchMedia).toHaveBeenCalledWith(railScreenQuery);
    expect(document.querySelector('.nt-navigation-rail-item').classList.contains('nt-navigation-rail-item-expanded')).toBe(expandedItemLayout);
  });

  test('standard rail expansion stays nonmodal on medium and larger screens', () => {
    window.matchMedia = jest.fn(query => ({
      matches: query === '(min-width: 840px)',
      media: query,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn()
    }));
    document.body.innerHTML = `
      <main id="content"><button id="outside">Outside</button></main>
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <button class="nt-navigation-rail-menu-button"
                type="button"
                aria-label="Expand navigation rail"
                aria-expanded="false"
                data-nt-navigation-rail-expanded-label="Collapse navigation rail"
                data-nt-navigation-rail-collapsed-label="Expand navigation rail">
          Menu
        </button>
        <a class="nt-navigation-rail-item" href="/home">Home</a>
      </nav>`;

    module.onLoad();

    const main = document.querySelector('#content');
    const rail = document.querySelector('.nt-navigation-rail');
    const menuButton = document.querySelector('.nt-navigation-rail-menu-button');

    expect(rail.classList.contains('nt-navigation-rail-responsive-modal')).toBe(false);

    menuButton.click();

    expect(rail.classList.contains('nt-navigation-rail-expanded')).toBe(true);
    expect(document.querySelector('.nt-navigation-rail-modal-dialog')).toBeNull();
    expect(main.inert).not.toBe(true);
    expect(main.hasAttribute('aria-hidden')).toBe(false);
  });

  test('collapsed popover group items keep stable expanded presentation across updates', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-section-header" role="heading" aria-level="2">Primary</div>
        <div class="nt-navigation-rail-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <div class="nt-navigation-rail-section-header" role="heading" aria-level="3">Workspace</div>
              <a class="nt-navigation-rail-item" href="/projects">Projects</a>
              <div class="nt-navigation-rail-group">
                <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                        type="button"
                        aria-expanded="false"
                        aria-controls="nested-panel"
                        data-nt-navigation-rail-group-trigger="true">
                  Admin
                </button>
                <div id="nested-panel" class="nt-navigation-rail-group-panel" popover="auto">
                  <div class="nt-navigation-rail-group-items">
                    <a class="nt-navigation-rail-item" href="/teams">Teams</a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </nav>`;

    const panel = document.querySelector('#workspace-panel');
    const railHeader = document.querySelector('nav > .nt-navigation-rail-section-header');
    const popoverHeader = panel.querySelector('.nt-navigation-rail-section-header');
    const popoverItem = panel.querySelector('a.nt-navigation-rail-item');
    const nestedTrigger = panel.querySelector('.nt-navigation-rail-group-trigger');

    module.onLoad();

    expect(railHeader.classList.contains('nt-navigation-rail-section-header-expanded')).toBe(false);
    expect(popoverHeader.classList.contains('nt-navigation-rail-section-header-expanded')).toBe(true);
    expect(popoverItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
    expect(nestedTrigger.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);

    popoverHeader.classList.remove('nt-navigation-rail-section-header-expanded');
    popoverItem.classList.remove('nt-navigation-rail-item-expanded');
    nestedTrigger.classList.remove('nt-navigation-rail-item-expanded');

    module.onUpdate();

    expect(popoverHeader.classList.contains('nt-navigation-rail-section-header-expanded')).toBe(true);
    expect(popoverItem.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
    expect(nestedTrigger.classList.contains('nt-navigation-rail-item-expanded')).toBe(true);
  });

  test('route matching uses base-relative case-insensitive paths', () => {
    const base = document.createElement('base');
    base.href = 'http://localhost/app/';
    document.head.append(base);
    window.history.pushState({}, '', '/app/Reports');

    try {
      document.body.innerHTML = `
        <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
          <a class="nt-navigation-rail-item"
             href=""
             data-nt-navigation-rail-match="Prefix">Home</a>
          <a class="nt-navigation-rail-item"
             href="reports"
             data-nt-navigation-rail-match="Prefix">Reports</a>
        </nav>`;

      module.onLoad();

      const home = document.querySelector('.nt-navigation-rail-item[href=""]');
      const reports = document.querySelector('.nt-navigation-rail-item[href="reports"]');

      expect(home.classList.contains('nt-navigation-rail-item-selected')).toBe(false);
      expect(home.hasAttribute('aria-current')).toBe(false);
      expect(reports.classList.contains('nt-navigation-rail-item-selected')).toBe(true);
      expect(reports.getAttribute('aria-current')).toBe('page');
    } finally {
      base.remove();
      window.history.pushState({}, '', '/');
    }
  });

  test('route changes reverse the previous selection animation while selecting the new destination', async () => {
    const base = document.createElement('base');
    base.href = 'http://localhost/app/';
    document.head.append(base);
    window.history.pushState({}, '', '/app/home');

    try {
      document.body.innerHTML = `
        <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
          <a class="nt-navigation-rail-item"
             href="home"
             data-nt-navigation-rail-match="All">Home</a>
          <a class="nt-navigation-rail-item"
             href="reports"
             data-nt-navigation-rail-match="All">Reports</a>
        </nav>`;

      module.onLoad();

      const home = document.querySelector('.nt-navigation-rail-item[href="home"]');
      const reports = document.querySelector('.nt-navigation-rail-item[href="reports"]');
      let finishExitAnimation;
      const exitAnimationFinished = new Promise(resolve => {
        finishExitAnimation = resolve;
      });
      home.getAnimations = jest.fn(() => [{
        animationName: 'nt-navigation-rail-indicator-exit-b-test',
        finished: exitAnimationFinished
      }]);

      window.history.pushState({}, '', '/app/reports');
      module.onUpdate();

      expect(home.classList.contains('nt-navigation-rail-item-selected')).toBe(false);
      expect(home.classList.contains('nt-navigation-rail-item-deselecting')).toBe(true);
      expect(home.getAnimations).toHaveBeenCalledWith({ subtree: true });
      expect(reports.classList.contains('nt-navigation-rail-item-selected')).toBe(true);

      finishExitAnimation();
      await Promise.resolve();

      expect(home.classList.contains('nt-navigation-rail-item-deselecting')).toBe(false);
    } finally {
      base.remove();
      window.history.pushState({}, '', '/');
    }
  });

  test('selected destination marks every ancestor group trigger selected', () => {
    document.body.innerHTML = `
      <nav class="nt-navigation-rail nt-navigation-rail-collapsed">
        <div class="nt-navigation-rail-group" id="workspace-group">
          <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                  type="button"
                  aria-expanded="false"
                  aria-controls="workspace-panel"
                  data-nt-navigation-rail-group-trigger="true">
            <span class="nt-navigation-rail-item-icon" aria-hidden="true">
              <span class="tnt-icon material-symbols-outlined">workspaces</span>
            </span>
            Workspace
          </button>
          <div id="workspace-panel" class="nt-navigation-rail-group-panel" popover="auto">
            <div class="nt-navigation-rail-group-items">
              <div class="nt-navigation-rail-group" id="admin-group">
                <button class="nt-navigation-rail-group-trigger nt-navigation-rail-item"
                        type="button"
                        aria-expanded="false"
                        aria-controls="admin-panel"
                        data-nt-navigation-rail-group-trigger="true">
                  <span class="nt-navigation-rail-item-icon" aria-hidden="true">
                    <span class="tnt-icon material-symbols-outlined">admin_panel_settings</span>
                  </span>
                  Admin
                </button>
                <div id="admin-panel" class="nt-navigation-rail-group-panel" popover="auto">
                  <div class="nt-navigation-rail-group-items">
                    <a class="nt-navigation-rail-item nt-navigation-rail-item-selected" href="/teams">Teams</a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </nav>`;

    module.onLoad();

    const workspaceGroup = document.querySelector('#workspace-group');
    const adminGroup = document.querySelector('#admin-group');
    const workspaceTrigger = workspaceGroup.querySelector(':scope > .nt-navigation-rail-group-trigger');
    const adminTrigger = adminGroup.querySelector(':scope > .nt-navigation-rail-group-trigger');
    const workspaceIcon = workspaceTrigger.querySelector('.nt-navigation-rail-item-icon .tnt-icon');
    const adminIcon = adminTrigger.querySelector('.nt-navigation-rail-item-icon .tnt-icon');

    expect(adminGroup.classList.contains('nt-navigation-rail-group-selected')).toBe(true);
    expect(workspaceGroup.classList.contains('nt-navigation-rail-group-selected')).toBe(true);
    expect(adminTrigger.classList.contains('nt-navigation-rail-item-selected')).toBe(true);
    expect(workspaceTrigger.classList.contains('nt-navigation-rail-item-selected')).toBe(true);
    expect(adminIcon.classList.contains('nt-nav-rail-selected-icon')).toBe(true);
    expect(workspaceIcon.classList.contains('nt-nav-rail-selected-icon')).toBe(true);

    document.querySelector('.nt-navigation-rail-item-selected[href="/teams"]').classList.remove('nt-navigation-rail-item-selected');
    module.onUpdate();

    expect(adminTrigger.classList.contains('nt-navigation-rail-item-selected')).toBe(false);
    expect(workspaceTrigger.classList.contains('nt-navigation-rail-item-selected')).toBe(false);
    expect(adminIcon.classList.contains('nt-nav-rail-selected-icon')).toBe(false);
    expect(workspaceIcon.classList.contains('nt-nav-rail-selected-icon')).toBe(false);
  });
});
