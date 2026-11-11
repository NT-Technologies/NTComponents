import { onDispose, onLoad, onUpdate } from '../../../NTComponents/Layout/Views/NTListDetailView.razor.js';

function renderListDetailView() {
  document.body.innerHTML = `
    <div class="nt-list-detail-view" data-detail-visible="false">
      <section class="nt-list-detail-view-list">
        <a href="/fallback-one" data-nt-list-detail-trigger="one" data-nt-list-detail-selected="true" aria-current="true">One</a>
        <a href="/fallback-two" data-nt-list-detail-trigger="two">Two</a>
      </section>
      <section class="nt-list-detail-view-detail">
        <form data-nt-list-detail-back>
          <button type="submit">Back</button>
        </form>
        <article data-nt-list-detail-panel="one">One detail</article>
        <article data-nt-list-detail-panel="two" hidden>Two detail</article>
      </section>
      <nt-page-script src="./_content/NTComponents/Layout/Views/NTListDetailView.razor.js"></nt-page-script>
    </div>`;

  return document.querySelector('.nt-list-detail-view');
}

async function updatePageScript(element) {
  onUpdate(element);
  await Promise.resolve();
}

describe('NTListDetailView page script', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
  });

  test('onLoad discovers and enhances the owning view from its page-script marker', () => {
    const view = renderListDetailView();

    onLoad(view.querySelector('nt-page-script'));

    expect(view.dataset.ntListDetailSelectedValue).toBe('one');
    expect(view.querySelector('[data-nt-list-detail-trigger="one"]').dataset.ntListDetailSelected).toBe('true');
    expect(view.querySelector('[data-nt-list-detail-panel="one"]').hidden).toBe(false);
  });

  test('onUpdate discovers the owning view from its page-script marker and selects matching detail', async () => {
    const view = renderListDetailView();
    await updatePageScript(view.querySelector('nt-page-script'));

    const trigger = view.querySelector('[data-nt-list-detail-trigger="two"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });

    const dispatchResult = trigger.dispatchEvent(event);

    expect(dispatchResult).toBe(false);
    expect(view.classList.contains('nt-list-detail-view-detail-visible')).toBe(true);
    expect(view.dataset.detailVisible).toBe('true');
    expect(trigger.dataset.ntListDetailSelected).toBe('true');
    expect(trigger.getAttribute('aria-current')).toBe('true');
    expect(view.querySelector('[data-nt-list-detail-trigger="one"]').dataset.ntListDetailSelected).toBe('false');
    expect(view.querySelector('[data-nt-list-detail-panel="one"]').hidden).toBe(true);
    expect(view.querySelector('[data-nt-list-detail-panel="two"]').hidden).toBe(false);
  });

  test('keeps modified link clicks native', async () => {
    const view = renderListDetailView();
    await updatePageScript();

    const trigger = view.querySelector('[data-nt-list-detail-trigger="two"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true, ctrlKey: true });

    const dispatchResult = trigger.dispatchEvent(event);

    expect(dispatchResult).toBe(true);
    expect(view.dataset.detailVisible).toBe('false');
    expect(view.querySelector('[data-nt-list-detail-panel="two"]').hidden).toBe(true);
  });

  test('back form closes detail without navigation', async () => {
    const view = renderListDetailView();
    view.classList.add('nt-list-detail-view-detail-visible');
    view.dataset.detailVisible = 'true';
    await updatePageScript();

    const form = view.querySelector('[data-nt-list-detail-back]');
    const event = new Event('submit', { bubbles: true, cancelable: true });

    const dispatchResult = form.dispatchEvent(event);

    expect(dispatchResult).toBe(false);
    expect(view.classList.contains('nt-list-detail-view-detail-visible')).toBe(false);
    expect(view.dataset.detailVisible).toBe('false');
  });

  test('falls back to visible panel when selected trigger has no matching panel', async () => {
    const view = renderListDetailView();
    view.querySelector('[data-nt-list-detail-trigger="one"]').setAttribute('data-nt-list-detail-trigger', 'missing');
    await updatePageScript();

    const trigger = view.querySelector('[data-nt-list-detail-trigger="two"]');
    const panel = view.querySelector('[data-nt-list-detail-panel="two"]');
    panel.hidden = false;
    view.querySelector('[data-nt-list-detail-panel="one"]').hidden = true;

    await updatePageScript();

    expect(trigger.dataset.ntListDetailSelected).toBe('true');
    expect(trigger.getAttribute('aria-current')).toBe('true');
    expect(view.dataset.ntListDetailSelectedValue).toBe('two');
  });

  test('dispose removes delegated handlers', async () => {
    const view = renderListDetailView();
    await updatePageScript();
    onDispose();

    const trigger = view.querySelector('[data-nt-list-detail-trigger="two"]');
    const event = new MouseEvent('click', { bubbles: true, cancelable: true });

    const dispatchResult = trigger.dispatchEvent(event);

    expect(dispatchResult).toBe(true);
    expect(view.dataset.detailVisible).toBe('false');
  });
});
