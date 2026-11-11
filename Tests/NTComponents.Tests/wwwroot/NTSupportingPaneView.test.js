import { jest } from '@jest/globals';
import { onDispose, onLoad, onUpdate } from '../../../NTComponents/Layout/Views/NTSupportingPaneView.razor.js';

function renderSupportingPaneView() {
  document.body.innerHTML = `
    <section>
      <div aria-label="Supporting pane controls">
        <button type="button"
                data-nt-supporting-pane-mode-control="Auto"
                data-nt-supporting-pane-target="supporting-pane-view">Auto</button>
        <button type="button"
                data-nt-supporting-pane-mode-control="Stacked"
                data-nt-supporting-pane-target="supporting-pane-view">Stacked</button>
        <button type="button"
                data-nt-supporting-pane-mode-control="HideOnSmallScreens"
                data-nt-supporting-pane-target="supporting-pane-view">Hide on small screens</button>
      </div>
      <p data-nt-supporting-pane-mode-label data-nt-supporting-pane-target="supporting-pane-view">Auto</p>
      <div id="supporting-pane-view"
           class="nt-supporting-pane-view"
           data-nt-supporting-pane-view
           data-nt-supporting-pane-mode="Auto">
        <section class="nt-supporting-pane-view-primary">Primary</section>
        <aside class="nt-supporting-pane-view-supporting">Supporting</aside>
        <nt-page-script src="./_content/NTComponents/Layout/Views/NTSupportingPaneView.razor.js"></nt-page-script>
      </div>
    </section>`;

  return document.querySelector('[data-nt-supporting-pane-view]');
}

async function updatePageScript(element) {
  onUpdate(element);
  await Promise.resolve();
}

describe('NTSupportingPaneView page script', () => {
  beforeEach(() => {
    onDispose();
    document.body.innerHTML = '';
    jest.restoreAllMocks();
  });

  afterEach(() => {
    onDispose();
  });

  test('onLoad discovers and enhances the owning view from its page-script marker', () => {
    const view = renderSupportingPaneView();

    onLoad(view.querySelector('nt-page-script'));

    const activeControl = document.querySelector('[data-nt-supporting-pane-mode-control="Auto"]');
    expect(activeControl.dataset.ntSupportingPaneActive).toBe('true');
    expect(activeControl.getAttribute('aria-pressed')).toBe('true');
    expect(view.dataset.ntSupportingPaneMode).toBe('Auto');
  });

  test('onUpdate discovers the owning view from its page-script marker and updates mode controls', async () => {
    const view = renderSupportingPaneView();
    await updatePageScript(view.querySelector('nt-page-script'));

    const control = document.querySelector('[data-nt-supporting-pane-mode-control="HideOnSmallScreens"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });

    const dispatchResult = control.dispatchEvent(event);

    expect(dispatchResult).toBe(false);
    expect(view.dataset.ntSupportingPaneMode).toBe('HideOnSmallScreens');
    expect(view.classList.contains('nt-supporting-pane-view-hide-on-small-screens')).toBe(true);
    expect(control.dataset.ntSupportingPaneActive).toBe('true');
    expect(control.getAttribute('aria-pressed')).toBe('true');
    expect(document.querySelector('[data-nt-supporting-pane-mode-label]').textContent).toBe('HideOnSmallScreens');
  });

  test('keeps modified mode-control clicks native', async () => {
    const view = renderSupportingPaneView();
    await updatePageScript();

    const control = document.querySelector('[data-nt-supporting-pane-mode-control="Stacked"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true, ctrlKey: true });

    const dispatchResult = control.dispatchEvent(event);

    expect(dispatchResult).toBe(true);
    expect(view.dataset.ntSupportingPaneMode).toBe('Auto');
    expect(view.classList.contains('nt-supporting-pane-view-stacked')).toBe(false);
  });

  test('uses one document click listener for multiple views', async () => {
    const addEventListenerSpy = jest.spyOn(document, 'addEventListener');
    renderSupportingPaneView();
    const firstSection = document.body.firstElementChild;
    document.body.append(firstSection.cloneNode(true));

    await updatePageScript();

    expect(addEventListenerSpy.mock.calls.filter(call => call[0] === 'click')).toHaveLength(1);
  });

  test('dispose removes mode-control handlers', async () => {
    const view = renderSupportingPaneView();
    await updatePageScript();
    onDispose(view);

    const control = document.querySelector('[data-nt-supporting-pane-mode-control="Stacked"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });

    const dispatchResult = control.dispatchEvent(event);

    expect(dispatchResult).toBe(true);
    expect(view.dataset.ntSupportingPaneMode).toBe('Auto');
  });

  test('dispose prunes disconnected views', async () => {
    const removeEventListenerSpy = jest.spyOn(document, 'removeEventListener');
    const view = renderSupportingPaneView();
    await updatePageScript();

    view.remove();
    onDispose();

    expect(removeEventListenerSpy.mock.calls.some(call => call[0] === 'click')).toBe(true);
  });
});
