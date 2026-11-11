import { join } from 'path';
import { pathToFileURL } from 'url';
import { TextDecoder, TextEncoder } from 'util';
import { jest } from '@jest/globals';

const repoRoot = process.cwd();

function moduleUrl(relativePath) {
  return pathToFileURL(join(repoRoot, relativePath)).href;
}

async function importModule(relativePath) {
  return import(moduleUrl(relativePath));
}

function createDotNetRef() {
  return {
    invokeMethodAsync: jest.fn(() => Promise.resolve())
  };
}

function getNodeIndex(node) {
  return Array.from(node.parentNode.childNodes).indexOf(node);
}

function collapseBeforeNode(node) {
  const range = document.createRange();
  range.setStart(node.parentNode, getNodeIndex(node));
  range.collapse(true);
  window.getSelection().removeAllRanges();
  window.getSelection().addRange(range);
  return range;
}

function selectText(node, startOffset, endOffset) {
  const range = document.createRange();
  range.setStart(node, startOffset);
  range.setEnd(node, endOffset);
  window.getSelection().removeAllRanges();
  window.getSelection().addRange(range);
  return range;
}

function click(element) {
  element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
}

function keydown(element, key, options = {}) {
  const event = new KeyboardEvent('keydown', {
    bubbles: true,
    cancelable: true,
    key,
    ...options
  });
  element.dispatchEvent(event);
}

function blur(element, relatedTarget = null) {
  element.dispatchEvent(new FocusEvent('focusout', { bubbles: true, cancelable: false, relatedTarget }));
}

async function settleAsync() {
  await Promise.resolve();
  await new Promise((resolve) => setTimeout(resolve, 0));
  await Promise.resolve();
}

function setSurfaceContent(surface, html) {
  surface.innerHTML = html;
}

function escapeFixtureAttribute(value) {
  return `${value ?? ''}`
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function escapeFixtureText(value) {
  return `${value ?? ''}`
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function tableGridMarkup() {
  return Array.from({ length: 80 }, (_, index) => `<button type="button" data-col="${(index % 8) + 1}" data-row="${Math.floor(index / 8) + 1}"></button>`).join('');
}

function gridCell(fixture, column, row) {
  return fixture.element.querySelector(`[data-role="table-grid"] button[data-col="${column}"][data-row="${row}"]`);
}

function createEditorFixture({
  bindOnInput = false,
  editable = true,
  value = '',
  maxLength = 200
} = {}) {
  const encodedValueAttribute = escapeFixtureAttribute(value);
  const encodedValueText = escapeFixtureText(value);
  document.body.innerHTML = `
    <div class="nt-input-container">
      <nt-rich-text-editor data-bind-on-input="${bindOnInput}" data-editable="${editable}">
        <div class="nt-rich-text-editor-shell">
          <input type="hidden" class="nt-rich-text-editor-hidden-input" value="${encodedValueAttribute}" />
          <div class="nt-rich-text-editor-value" hidden>${encodedValueText}</div>
          <div class="nt-rich-text-editor-toolbar" role="toolbar">
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="bold" aria-keyshortcuts="Control+B" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="unorderedList" aria-keyshortcuts="Control+Alt+7" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="image" aria-keyshortcuts="Control+Alt+M" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="table" aria-keyshortcuts="Control+Alt+T" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="textColor" aria-keyshortcuts="Control+Alt+X" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="link" aria-keyshortcuts="Control+K" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="ariaLabel" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="iframe" aria-keyshortcuts="Control+Alt+F" aria-pressed="false"></button>
            <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="editHtml" aria-pressed="false"></button>
          </div>

          <div data-tool-command="image" data-role="image-editor" hidden aria-hidden="true">
            <input data-role="image-url" type="url" />
            <input data-role="image-file" type="file" />
            <input data-role="image-alt" type="text" />
            <input data-role="image-title" type="text" />
            <input data-role="image-width" type="number" />
            <input data-role="image-height" type="number" />
            <button type="button" data-role="image-apply"></button>
            <button type="button" data-role="image-cancel"></button>
          </div>

          <div data-tool-command="table" data-role="table-editor" hidden aria-hidden="true">
            <div data-role="table-insert">
              <div data-role="table-grid">${tableGridMarkup()}</div>
              <span data-role="table-grid-label"></span>
            </div>
            <div data-role="table-edit" hidden>
              <input data-role="table-border-color" type="color" />
              <input data-role="table-caption" type="text" />
              <button type="button" data-role="table-apply"></button>
              <button type="button" data-role="table-cancel"></button>
            </div>
          </div>

          <div data-tool-command="textColor" data-role="text-color-editor" hidden aria-hidden="true">
            <input data-role="text-color-value" type="color" />
            <button type="button" data-role="text-color-apply"></button>
            <button type="button" data-role="text-color-cancel"></button>
          </div>

          <div data-tool-command="link" data-role="link-editor" hidden aria-hidden="true">
            <input data-role="link-url" type="url" />
            <input data-role="link-text" type="text" />
            <input data-role="link-aria-label" type="text" />
            <input data-role="link-title" type="text" />
            <button type="button" data-role="link-apply"></button>
            <button type="button" data-role="link-cancel"></button>
          </div>

          <div data-tool-command="iframe" data-role="iframe-editor" hidden aria-hidden="true">
            <input data-role="iframe-url" type="url" />
            <input data-role="iframe-title" type="text" />
            <input data-role="iframe-width" type="text" />
            <input data-role="iframe-height" type="text" />
            <button type="button" data-role="iframe-apply"></button>
            <button type="button" data-role="iframe-cancel"></button>
          </div>

          <div data-tool-command="ariaLabel" hidden aria-hidden="true">
            <input data-role="aria-label-value" type="text" />
            <button type="button" data-role="aria-label-apply"></button>
            <button type="button" data-role="aria-label-cancel"></button>
          </div>

          <div class="nt-rich-text-editor-surface" contenteditable="true" data-placeholder="Write" data-maxlength="${maxLength}" tabindex="0"></div>
          <span class="nt-rich-text-editor-source-container">
            <pre class="nt-rich-text-editor-source-highlight" aria-hidden="true"><code></code></pre>
            <textarea data-role="html-source" hidden></textarea>
          </span>
        </div>
      </nt-rich-text-editor>
      <span class="nt-input-length">0/${maxLength}</span>
    </div>
  `;

  return {
    container: document.querySelector('.nt-input-container'),
    element: document.querySelector('nt-rich-text-editor'),
    hiddenInput: document.querySelector('.nt-rich-text-editor-hidden-input'),
    sourceValue: document.querySelector('.nt-rich-text-editor-value'),
    surface: document.querySelector('.nt-rich-text-editor-surface'),
    htmlSource: document.querySelector('[data-role="html-source"]'),
    htmlSourceHighlight: document.querySelector('.nt-rich-text-editor-source-highlight code'),
    length: document.querySelector('.nt-input-length')
  };
}

describe('NTRichTextEditor runtime behavior', () => {
  let editorModule;
  let selectionState;
  let originalExecCommand;
  let originalFileReader;
  let originalFetch;
  let originalRequestAnimationFrame;
  let originalCancelAnimationFrame;

  beforeAll(async () => {
    editorModule = await importModule('NTComponents/Editors/NTRichTextEditor.razor.js');
  });

  beforeEach(() => {
    document.body.innerHTML = '';

    selectionState = { range: null };
    Object.defineProperty(window, 'getSelection', {
      configurable: true,
      value: () => ({
        get rangeCount() {
          return selectionState.range ? 1 : 0;
        },
        get anchorNode() {
          return selectionState.range?.startContainer ?? null;
        },
        getRangeAt(index) {
          if (index !== 0 || !selectionState.range) {
            throw new Error('No selection range available.');
          }

          return selectionState.range;
        },
        removeAllRanges() {
          selectionState.range = null;
        },
        addRange(range) {
          selectionState.range = range;
        },
        toString() {
          return selectionState.range?.toString() ?? '';
        }
      })
    });

    originalExecCommand = document.execCommand;
    document.execCommand = jest.fn((command, _showUi, value) => {
      if (command === 'createLink') {
        const linkSelection = window.getSelection();
        if (linkSelection.rangeCount) {
          const linkRange = linkSelection.getRangeAt(0);
          const anchor = document.createElement('a');
          anchor.setAttribute('href', value);
          anchor.appendChild(linkRange.extractContents());
          linkRange.insertNode(anchor);
          const anchorRange = document.createRange();
          anchorRange.selectNodeContents(anchor);
          linkSelection.removeAllRanges();
          linkSelection.addRange(anchorRange);
        }

        return true;
      }

      if (command !== 'insertHTML') {
        return true;
      }

      const selection = window.getSelection();
      if (!selection.rangeCount) {
        return true;
      }

      const range = selection.getRangeAt(0);
      const template = document.createElement('template');
      template.innerHTML = value;
      const fragment = template.content.cloneNode(true);
      const insertedNodes = Array.from(fragment.childNodes);
      range.deleteContents();
      range.insertNode(fragment);

      if (insertedNodes.length > 0) {
        const nextRange = document.createRange();
        nextRange.setStartAfter(insertedNodes[insertedNodes.length - 1]);
        nextRange.collapse(true);
        selection.removeAllRanges();
        selection.addRange(nextRange);
      }

      return true;
    });

    originalFileReader = global.FileReader;
    global.FileReader = class MockFileReader {
      constructor() {
        this.result = null;
        this.onload = null;
      }

      readAsDataURL(file) {
        this.result = `data:${file.type};base64,ZmFrZQ==`;
        this.onload?.({ target: this });
      }
    };

    originalFetch = global.fetch;
    global.fetch = jest.fn(async () => ({
      ok: true,
      headers: {
        get: (name) => name.toLowerCase() === 'content-type' ? 'image/png' : null
      },
      blob: async () => new Blob(['remote image'], { type: 'image/png' })
    }));

    originalRequestAnimationFrame = window.requestAnimationFrame;
    originalCancelAnimationFrame = window.cancelAnimationFrame;
    window.requestAnimationFrame = (callback) => setTimeout(() => callback(0), 0);
    window.cancelAnimationFrame = (handle) => clearTimeout(handle);
  });

  afterEach(() => {
    jest.useRealTimers();
    document.execCommand = originalExecCommand;
    global.FileReader = originalFileReader;
    global.fetch = originalFetch;
    window.requestAnimationFrame = originalRequestAnimationFrame;
    window.cancelAnimationFrame = originalCancelAnimationFrame;
    document.body.innerHTML = '';
  });

  test('onLoad renders html and syncs bind-on-input changes', async () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: '<p>Hello</p>' });
    const dotNetRef = createDotNetRef();

    editorModule.onLoad(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toContain('Hello');

    setSurfaceContent(fixture.surface, '<p>World</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    expect(fixture.hiddenInput.value).toBe('<p>World</p>');
    expect(fixture.hiddenInput.value).toBe('<p>World</p>');
    expect(fixture.sourceValue.textContent).toBe('<p>Hello</p>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
      'UpdateValueFromJs',
      expect.any(Number),
      '<p>World</p>'
    );
  });

  test('applies direct html source and sanitizes posted values', () => {
    const fixture = createEditorFixture({
      bindOnInput: true,
      value: '<p onclick="bad()">Hello <a href="javascript:alert(1)">bad</a><script>alert(1)</script></p><iframe src="https://example.com/embed"></iframe>'
    });
    const dotNetRef = createDotNetRef();

    editorModule.onLoad(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toContain('<p>Hello <a>bad</a></p>');
    expect(fixture.surface.querySelector('script')).toBeNull();
    expect(fixture.surface.querySelector('p').hasAttribute('onclick')).toBe(false);
    expect(fixture.surface.querySelector('iframe').getAttribute('title')).toBe('Embedded content');
    expect(fixture.hiddenInput.value).toBe(fixture.surface.innerHTML);

    setSurfaceContent(fixture.surface, '<p><img src="javascript:alert(1)" onerror="bad()" alt="Bad"><a href="https://example.com/docs" aria-label="Docs label" title="Docs title">Docs</a></p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    expect(fixture.hiddenInput.value).toBe('<p><a href="https://example.com/docs" aria-label="Docs label" title="Docs title">Docs</a></p>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
      'UpdateValueFromJs',
      expect.any(Number),
      '<p><a href="https://example.com/docs" aria-label="Docs label" title="Docs title">Docs</a></p>'
    );
  });

  test('HTML source mode edits markup, disables formatting, and sanitizes on return', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    const toggle = fixture.element.querySelector('[data-command="editHtml"]');
    const bold = fixture.element.querySelector('[data-command="bold"]');
    click(toggle);
    expect(fixture.surface.hidden).toBe(true);
    expect(fixture.htmlSource.hidden).toBe(false);
    expect(fixture.htmlSource.value).toBe('<p>Start</p>');
    expect(bold.disabled).toBe(true);
    expect(toggle.getAttribute('aria-pressed')).toBe('true');

    fixture.htmlSource.value = '<p onclick="bad()">Edited <strong>HTML</strong><script>bad()</script></p>';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    expect(fixture.hiddenInput.value).toBe('<p>Edited <strong>HTML</strong></p>');
    click(toggle);
    expect(fixture.surface.hidden).toBe(false);
    expect(fixture.htmlSource.hidden).toBe(true);
    expect(fixture.surface.innerHTML).toBe('<p>Edited <strong>HTML</strong></p>');
    expect(fixture.hiddenInput.value).toBe(fixture.surface.innerHTML);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(Number), fixture.surface.innerHTML);
    expect(bold.disabled).toBe(false);
    expect(toggle.getAttribute('aria-pressed')).toBe('false');
  });

  test('HTML source view collapses embedded image data but saves the full data URL', () => {
    const data = 'A'.repeat(4096);
    const fixture = createEditorFixture({ value: `<p>Pic <img src="data:image/png;base64,${data}" alt="Dot"></p>` });
    editorModule.onLoad(fixture.element, createDotNetRef());

    click(fixture.element.querySelector('[data-command="editHtml"]'));
    expect(fixture.htmlSource.value).toBe('<p>Pic <img src="data:image/png;base64,[image-data-1, 3 KB]" alt="Dot"></p>');

    fixture.htmlSource.value = fixture.htmlSource.value.replace('Pic', 'Edited');
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    click(fixture.element.querySelector('[data-command="editHtml"]'));

    expect(fixture.hiddenInput.value).toBe(`<p>Edited <img src="data:image/png;base64,${data}" alt="Dot"></p>`);
  });

  test('HTML source view indents nested blocks and saves one-line markup', () => {
    const fixture = createEditorFixture({ value: '<div><p>First</p><p>Second</p></div><ul><li>A</li><li>B</li></ul>' });
    editorModule.onLoad(fixture.element, createDotNetRef());
    const toggle = fixture.element.querySelector('[data-command="editHtml"]');

    click(toggle);
    expect(fixture.htmlSource.value).toBe('<div>\n  <p>First</p>\n  <p>Second</p>\n</div>\n<ul>\n  <li>A</li>\n  <li>B</li>\n</ul>');
    click(toggle);
    expect(fixture.hiddenInput.value).toBe('<div><p>First</p><p>Second</p></div><ul><li>A</li><li>B</li></ul>');
    expect(fixture.hiddenInput.value).not.toMatch(/[\r\n]/);
  });

  test('HTML source highlights editable markup without interpreting source as DOM', () => {
    const fixture = createEditorFixture({ value: '<p title="Greeting">Hello</p>' });
    editorModule.onLoad(fixture.element, createDotNetRef());
    click(fixture.element.querySelector('[data-command="editHtml"]'));

    expect(fixture.htmlSourceHighlight.textContent).toBe(fixture.htmlSource.value);
    expect(fixture.htmlSourceHighlight.querySelector('.syntax-tag').textContent).toBe('<');
    expect(fixture.htmlSourceHighlight.querySelector('.syntax-attribute').textContent).toBe('title');
    expect(fixture.htmlSourceHighlight.querySelector('.syntax-value').textContent).toBe('"Greeting"');

    fixture.htmlSource.value = '<!-- note -->\n<img src="x" onerror="alert(1)">';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    expect(fixture.htmlSourceHighlight.textContent).toBe(fixture.htmlSource.value);
    expect(fixture.htmlSourceHighlight.querySelector('.syntax-comment').textContent).toBe('<!-- note -->');
    expect(fixture.htmlSourceHighlight.querySelector('img')).toBeNull();
    expect(fixture.htmlSourceHighlight.querySelectorAll('[onerror]')).toHaveLength(0);
  });

  test('HTML source saves compact markup without changing inline spaces or preformatted lines', () => {
    jest.useFakeTimers();
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    click(fixture.element.querySelector('[data-command="editHtml"]'));

    fixture.htmlSource.value = '<div>\n  <p><em>A</em> <strong>B</strong></p>\n  <pre>line 1\n  line 2</pre>\n</div>';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    const expected = '<div><p><em>A</em> <strong>B</strong></p><pre>line 1&#10;  line 2</pre></div>';
    expect(fixture.hiddenInput.value).toBe(expected);
    expect(fixture.surface.querySelector('p').textContent).toBe('A B');
    expect(fixture.surface.querySelector('pre').textContent).toBe('line 1\n  line 2');

    const outside = document.createElement('button');
    document.body.appendChild(outside);
    outside.focus();
    blur(fixture.htmlSource);
    jest.runAllTimers();
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expected);
    expect(fixture.htmlSource.value).toContain('\n  <p><em>A</em> <strong>B</strong></p>\n');
    expect(fixture.hiddenInput.value).not.toMatch(/[\r\n]/);
  });

  test('a rerender with the original formatted parameter preserves an unfinished source draft', () => {
    const initial = '<div>\n  <p>Start</p>\n</div>';
    const fixture = createEditorFixture({ value: initial });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    click(fixture.element.querySelector('[data-command="editHtml"]'));

    fixture.htmlSource.value = '<div>\n  <p>Draft <strong>';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.sourceValue.textContent = initial;
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.htmlSource.value).toBe('<div>\n  <p>Draft <strong>');
    expect(fixture.hiddenInput.value).toBe('<div><p>Draft <strong></strong></p></div>');
  });

  test('HTML source mode commits a sanitized value when focus leaves the editor', () => {
    jest.useFakeTimers();
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    click(fixture.element.querySelector('[data-command="editHtml"]'));
    fixture.htmlSource.value = '<p>Saved <em>source</em></p><img src="javascript:bad()">';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    const outside = document.createElement('button');
    document.body.appendChild(outside);
    outside.focus();
    blur(fixture.htmlSource);
    jest.runAllTimers();

    expect(fixture.hiddenInput.value).toBe('<p>Saved <em>source</em></p>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), fixture.hiddenInput.value);
  });

  test('HTML source mode updates bound input without replacing an incomplete draft', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    click(fixture.element.querySelector('[data-command="editHtml"]'));
    fixture.htmlSource.value = '<p>Draft <strong>';
    fixture.htmlSource.dispatchEvent(new Event('input', { bubbles: true }));
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.htmlSource.value).toBe('<p>Draft <strong>');
    expect(fixture.hiddenInput.value).toBe('<p>Draft <strong></strong></p>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(Number), fixture.hiddenInput.value);
  });

  test('accessible label tool wraps selected text and edits its label', () => {
    const fixture = createEditorFixture();
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);

    click(fixture.element.querySelector('[data-command="ariaLabel"]'));
    const panel = fixture.element.querySelector('[data-tool-command="ariaLabel"]');
    const input = fixture.element.querySelector('[data-role="aria-label-value"]');
    expect(panel.hidden).toBe(false);
    input.value = 'Primary term';
    click(fixture.element.querySelector('[data-role="aria-label-apply"]'));

    const tag = fixture.surface.querySelector('span[aria-label]');
    expect(tag.textContent).toBe('Alpha');
    expect(tag.getAttribute('aria-label')).toBe('Primary term');
    expect(tag.getAttribute('role')).toBe('group');
    expect(fixture.hiddenInput.value).toContain('<span role="group" aria-label="Primary term">Alpha</span>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(Number), fixture.hiddenInput.value);

    selectText(tag.firstChild, 0, 5);
    click(fixture.element.querySelector('[data-command="ariaLabel"]'));
    expect(input.value).toBe('Primary term');
    input.value = 'Updated term';
    keydown(input, 'Enter');
    expect(fixture.surface.querySelectorAll('span[aria-label]')).toHaveLength(1);
    expect(tag.getAttribute('aria-label')).toBe('Updated term');
  });

  test('keyboard shortcuts execute commands and blur commits the editor value', () => {
    jest.useFakeTimers();

    const fixture = createEditorFixture();
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    keydown(fixture.surface, 'b', { ctrlKey: true });
    expect(document.execCommand).toHaveBeenCalledWith('bold', false);

    setSurfaceContent(fixture.surface, '<p>Committed</p>');
    const outsideButton = document.createElement('button');
    document.body.appendChild(outsideButton);
    outsideButton.focus();
    blur(fixture.surface);
    jest.runAllTimers();

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith(
      'CommitValueFromJs',
      expect.any(Number),
      '<p>Committed</p>'
    );
  });

  test('an interactive editor started by the page script stays read-only until .NET connects', () => {
    const fixture = createEditorFixture({ value: '<p>Bound</p>' });
    fixture.element.dataset.interactive = 'true';
    fixture.surface.setAttribute('contenteditable', 'false');
    const pageScript = document.createElement('nt-page-script');
    document.body.appendChild(pageScript);

    editorModule.onLoad(pageScript);

    expect(fixture.surface.textContent).toBe('Bound');
    expect(fixture.surface.getAttribute('contenteditable')).toBe('false');
    expect(fixture.element.querySelector('[data-command="bold"]').disabled).toBe(true);

    editorModule.onLoad(fixture.element, createDotNetRef());

    expect(fixture.surface.getAttribute('contenteditable')).toBe('true');
    expect(fixture.element.querySelector('[data-command="bold"]').disabled).toBe(false);
  });

  test('a static editor becomes editable without a .NET reference', () => {
    const fixture = createEditorFixture({ value: '<p>Bound</p>' });
    fixture.element.dataset.interactive = 'false';
    fixture.surface.setAttribute('contenteditable', 'false');

    editorModule.onLoad(document.createElement('nt-page-script'));

    expect(fixture.surface.getAttribute('contenteditable')).toBe('true');
  });

  test('blur commits before the event returns so a following click cannot remove the editor first', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');

    blur(fixture.surface);

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Typed</p>');
  });

  test('blur without an edit reports the blur without sending the value', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    blur(fixture.surface);

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('BlurFromJs');
    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  test('blur does not commit when focus moves inside the editor', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    blur(fixture.surface, fixture.element.querySelector('[data-command="bold"]'));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  test('blur does not commit when only the browser window loses focus', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    fixture.surface.focus();
    const hasFocus = jest.spyOn(document, 'hasFocus').mockReturnValue(false);

    blur(fixture.surface);
    hasFocus.mockRestore();

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  test('onDispose flushes an edit that has not reached .NET', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Unsaved</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    editorModule.onDispose(fixture.element);

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Unsaved</p>');
  });

  test('onDispose does not rewrite the value of an untouched editor', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    editorModule.onDispose(fixture.element);

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  test('onDispose removes the focus listener with the others', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    editorModule.onDispose(fixture.element);
    dotNetRef.invokeMethodAsync.mockClear();

    blur(fixture.surface);

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
  });

  test('onDispose does not repeat a value already committed on blur', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    blur(fixture.surface);

    editorModule.onDispose(fixture.element);

    expect(dotNetRef.invokeMethodAsync.mock.calls.filter(([method]) => method === 'CommitValueFromJs')).toHaveLength(1);
  });

  test('a render older than the last value sent to .NET does not reset the surface', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    const typeText = (html) => {
      setSurfaceContent(fixture.surface, html);
      fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    };
    typeText('<p>A</p>');
    typeText('<p>AB</p>');

    fixture.element.dataset.receivedValueCount = '1';
    fixture.sourceValue.textContent = '<p>A</p>';
    fixture.hiddenInput.value = '<p>A</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>AB</p>');
    expect(fixture.hiddenInput.value).toBe('<p>AB</p>');
  });

  test('a committed edit lets a render that has seen every sent value replace the surface', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    blur(fixture.surface);

    fixture.element.dataset.receivedValueCount = '2';
    fixture.sourceValue.textContent = '<p>Normalized</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>Normalized</p>');
  });

  test('a commit that never reached .NET does not make later renders look stale', async () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('Disconnected'))) };
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    blur(fixture.surface);
    await settleAsync();

    fixture.sourceValue.textContent = '<p>Set from .NET</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>Set from .NET</p>');
  });

  describe('value transport', () => {
    const largeValue = `<p>${'é'.repeat(6000)}</p>`;

    beforeEach(() => {
      // jsdom does not provide TextEncoder; browsers and Blazor's DotNet global do.
      window.TextEncoder = TextEncoder;
      window.DotNet = { createJSStreamReference: jest.fn((data) => ({ streamed: data })) };
    });

    afterEach(() => {
      delete window.DotNet;
      delete window.TextEncoder;
    });

    function commitTyped(html) {
      const fixture = createEditorFixture({ value: '<p>Start</p>' });
      const dotNetRef = createDotNetRef();
      editorModule.onLoad(fixture.element, dotNetRef);
      dotNetRef.invokeMethodAsync.mockClear();
      setSurfaceContent(fixture.surface, html);
      fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
      blur(fixture.surface);
      return { fixture, dotNetRef };
    }

    test('a small value is sent in the call itself', () => {
      const { dotNetRef } = commitTyped('<p>Small</p>');

      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Small</p>');
      expect(window.DotNet.createJSStreamReference).not.toHaveBeenCalled();
    });

    test('a large value is streamed as UTF-8 to the same method', () => {
      const { dotNetRef } = commitTyped(largeValue);

      const [data] = window.DotNet.createJSStreamReference.mock.calls[0];
      expect(new TextDecoder().decode(data)).toBe(largeValue);
      expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('ReceiveValueStreamFromJs', 'CommitValueFromJs', expect.any(Number), { streamed: data });
      expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.anything(), expect.anything());
    });

    test('every send carries a higher sequence than the one before, small or streamed', () => {
      const hooks = editorModule.__testHooks;
      const dotNetRef = createDotNetRef();
      const state = { dotNetRef, sentValueCount: 0 };

      hooks.sendValue(state, 'UpdateValueFromJs', largeValue);
      hooks.sendValue(state, 'UpdateMarkupValueFromJs', '<p>Markup</p>');
      hooks.sendValue(state, 'CommitValueFromJs', '<p>Small</p>');

      const sequences = dotNetRef.invokeMethodAsync.mock.calls.map((call) => (call[0] === 'ReceiveValueStreamFromJs' ? call[2] : call[1]));
      expect(sequences[1]).toBeGreaterThan(sequences[0]);
      expect(sequences[2]).toBeGreaterThan(sequences[1]);
    });

    test('a streamed value counts as sent, so a render that has not seen it keeps the surface', () => {
      const { fixture } = commitTyped(largeValue);

      fixture.element.dataset.receivedValueCount = '0';
      fixture.sourceValue.textContent = '<p>Start</p>';
      editorModule.onUpdate(fixture.element, null);
      const keptWhileStale = fixture.surface.innerHTML;
      fixture.element.dataset.receivedValueCount = '1';
      fixture.sourceValue.textContent = '<p>Caught up</p>';
      editorModule.onUpdate(fixture.element, null);

      expect(keptWhileStale).toBe(largeValue);
      expect(fixture.surface.innerHTML).toBe('<p>Caught up</p>');
    });

    test('a stream that cannot be created does not make later renders look stale', async () => {
      window.DotNet.createJSStreamReference.mockImplementation(() => {
        throw new Error('Blazor is gone');
      });
      const { fixture } = commitTyped(largeValue);
      await settleAsync();

      fixture.sourceValue.textContent = '<p>Set from .NET</p>';
      editorModule.onUpdate(fixture.element, null);

      expect(fixture.surface.innerHTML).toBe('<p>Set from .NET</p>');
    });
  });

  test.each([false, true])('a caught-up render with a new value keeps an uncommitted edit (bindOnInput %s)', (bindOnInput) => {
    const fixture = createEditorFixture({ bindOnInput, value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    const typedParagraph = fixture.surface.firstChild;
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    fixture.element.dataset.receivedValueCount = bindOnInput ? '1' : '0';
    fixture.sourceValue.textContent = '<p>Pushed</p>';
    fixture.hiddenInput.value = '<p>Pushed</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.firstChild).toBe(typedParagraph);
    expect(fixture.hiddenInput.value).toBe(bindOnInput ? '<p>Typed</p>' : '<p>Start</p>');
  });

  test('a late debounced value does not revert newer text while editing', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    const typeText = (html) => {
      setSurfaceContent(fixture.surface, html);
      fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    };
    typeText('<p>A</p>');
    typeText('<p>AB</p>');

    fixture.element.dataset.receivedValueCount = '2';
    fixture.sourceValue.textContent = '<p>A</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>AB</p>');
  });

  test('an edited value commits over a value ignored during the edit', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.sourceValue.textContent = '<p>Pushed</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    blur(fixture.surface);

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Typed</p>');
  });

  test('a focused editor without edits accepts a new value', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    fixture.surface.focus();

    fixture.sourceValue.textContent = '<p>Another item</p>';
    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>Another item</p>');
  });

  test('focus leaving through a tool panel commits the edit', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    const panelInput = fixture.element.querySelector('[data-role="link-url"]');

    blur(fixture.surface, panelInput);
    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());

    blur(panelInput);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Typed</p>');
  });

  test('a rerender with an unchanged bound value keeps an uncommitted edit', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    editorModule.__testHooks.syncValueFromSurface(fixture.element, false);

    editorModule.onUpdate(fixture.element, dotNetRef);

    expect(fixture.surface.innerHTML).toBe('<p>Typed</p>');
  });

  test('aria-readonly follows whether the surface can be edited', () => {
    const fixture = createEditorFixture({ value: '<p>Bound</p>' });
    fixture.element.dataset.interactive = 'true';
    fixture.surface.setAttribute('contenteditable', 'false');
    fixture.surface.setAttribute('aria-readonly', 'true');

    editorModule.onLoad(document.createElement('nt-page-script'));
    expect(fixture.surface.getAttribute('aria-readonly')).toBe('true');

    editorModule.onLoad(fixture.element, createDotNetRef());
    expect(fixture.surface.getAttribute('aria-readonly')).toBe('false');
  });

  test('a click outside that moves no focus commits before app handlers run', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    fixture.surface.focus();
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    const outside = document.createElement('button');
    document.body.appendChild(outside);
    let commitsSeenByApp = -1;
    outside.addEventListener('click', () => {
      commitsSeenByApp = dotNetRef.invokeMethodAsync.mock.calls.filter(([method]) => method === 'CommitValueFromJs').length;
    });

    outside.click();

    expect(commitsSeenByApp).toBe(1);
  });

  test('a click inside the editor does not commit', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    fixture.surface.focus();
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    fixture.element.querySelector('[data-role="link-url"]').click();

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  test.each(['beforeunload', 'pagehide'])('%s commits an uncommitted edit', (eventName) => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));

    window.dispatchEvent(new Event(eventName));

    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Typed</p>');
  });

  test('hiding the page commits an uncommitted edit but showing it does not', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<p>Typed</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    const visibilityState = jest.spyOn(document, 'visibilityState', 'get');

    visibilityState.mockReturnValue('visible');
    document.dispatchEvent(new Event('visibilitychange'));
    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());

    visibilityState.mockReturnValue('hidden');
    document.dispatchEvent(new Event('visibilitychange'));
    visibilityState.mockRestore();
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Typed</p>');
  });

  test('pagehide leaves an untouched editor alone', () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    window.dispatchEvent(new Event('pagehide'));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), expect.anything());
  });

  const createPrerenderedEditor = () => {
    const fixture = createEditorFixture({ value: '<p>Start</p>' });
    fixture.element.dataset.interactive = 'true';
    fixture.surface.setAttribute('contenteditable', 'false');
    return fixture;
  };

  test('focus taken while prerendered moves to the surface that replaces it', () => {
    const prerendered = createPrerenderedEditor();
    editorModule.onLoad(document.createElement('nt-page-script'));
    prerendered.surface.focus();

    const interactive = createPrerenderedEditor();
    editorModule.onLoad(interactive.element, createDotNetRef());

    expect(document.activeElement).toBe(interactive.surface);
  });

  test('focus taken before the script loaded moves to the surface that replaces it', () => {
    const prerendered = createPrerenderedEditor();
    prerendered.surface.focus();
    editorModule.onLoad(document.createElement('nt-page-script'));

    const interactive = createPrerenderedEditor();
    editorModule.onLoad(interactive.element, createDotNetRef());

    expect(document.activeElement).toBe(interactive.surface);
  });

  test('prerender focus is not taken back from a control the user moved to', () => {
    const prerendered = createPrerenderedEditor();
    editorModule.onLoad(document.createElement('nt-page-script'));
    prerendered.surface.focus();

    const interactive = createPrerenderedEditor();
    const other = document.createElement('button');
    document.body.appendChild(other);
    other.focus();
    editorModule.onLoad(interactive.element, createDotNetRef());

    expect(document.activeElement).toBe(other);
  });

  test('disabled editors disable toolbar and tool controls and block toolbar execution', () => {
    const fixture = createEditorFixture({ editable: false });
    editorModule.onLoad(fixture.element, createDotNetRef());

    const boldButton = fixture.element.querySelector('[data-command="bold"]');
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    const imageApplyButton = fixture.element.querySelector('[data-role="image-apply"]');

    expect(boldButton.disabled).toBe(true);
    expect(imageUrlInput.disabled).toBe(true);
    expect(imageApplyButton.disabled).toBe(true);

    click(boldButton);

    expect(document.execCommand).not.toHaveBeenCalled();
  });

  test('image tool edits an existing image and populates uploaded file details', async () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><img src="https://old.example/image.png" alt="Old alt" width="320" height="180" /></p>');
    const image = fixture.surface.querySelector('img');
    collapseBeforeNode(image);

    click(fixture.element.querySelector('[data-command="image"]'));

    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    const urlInput = fixture.element.querySelector('[data-role="image-url"]');
    const fileInput = fixture.element.querySelector('[data-role="image-file"]');
    const altInput = fixture.element.querySelector('[data-role="image-alt"]');
    const widthInput = fixture.element.querySelector('[data-role="image-width"]');
    const heightInput = fixture.element.querySelector('[data-role="image-height"]');

    expect(imagePanel.hidden).toBe(false);
    expect(urlInput.value).toBe('https://old.example/image.png');
    expect(altInput.value).toBe('Old alt');
    expect(widthInput.value).toBe('320');
    expect(heightInput.value).toBe('180');

    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new File(['image'], 'updated-image.png', { type: 'image/png' })]
    });
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));
    await settleAsync();

    // The picked file waits for Insert: the URL field is cleared rather than filled with base64 data.
    expect(urlInput.value).toBe('');
    expect(altInput.value).toBe('Old alt');
    expect(image.getAttribute('src')).toBe('https://old.example/image.png');

    collapseBeforeNode(image);
    click(fixture.element.querySelector('[data-command="image"]'));
    urlInput.value = 'https://new.example/image.png';
    altInput.value = 'Updated alt';
    widthInput.value = '640';
    heightInput.value = '360';
    click(fixture.element.querySelector('[data-role="image-apply"]'));
    await settleAsync();

    expect(image.getAttribute('src')).toBe('https://new.example/image.png');
    expect(image.getAttribute('alt')).toBe('Updated alt');
    expect(image.getAttribute('width')).toBe('640');
    expect(image.getAttribute('height')).toBe('360');
    expect(imagePanel.hidden).toBe(true);
  });

  test('table tool inserts a table sized from the grid with the first row as the header', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    fixture.surface.textContent = '';
    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(fixture.element.querySelector('[data-command="table"]'));
    const panel = fixture.element.querySelector('[data-tool-command="table"]');
    expect(panel.hidden).toBe(false);
    expect(fixture.element.querySelector('[data-role="table-insert"]').hidden).toBe(false);
    expect(fixture.element.querySelector('[data-role="table-edit"]').hidden).toBe(true);

    gridCell(fixture, 3, 2).dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
    expect(fixture.element.querySelector('[data-role="table-grid-label"]').textContent).toBe('3 × 2');
    expect(fixture.element.querySelectorAll('[data-role="table-grid"] [data-selected]')).toHaveLength(6);
    expect(gridCell(fixture, 4, 2).hasAttribute('data-selected')).toBe(false);

    click(gridCell(fixture, 3, 2));
    const table = fixture.surface.querySelector('table');
    expect(table.querySelectorAll('thead th')).toHaveLength(3);
    expect(table.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(table.querySelectorAll('tbody td')).toHaveLength(3);
    expect(table.getAttribute('data-border-color')).toBe(editorModule.__testHooks.defaultTableBorderColor);
    expect(table.style.getPropertyValue('--nt-rich-text-table-border-color')).toBe(editorModule.__testHooks.defaultTableBorderColor);
    expect(fixture.surface.lastElementChild.tagName).toBe('P');
    expect(panel.hidden).toBe(true);
  });

  test('table grid supports arrow-key focus movement, Escape, and a header-only 1-row table', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());
    fixture.surface.textContent = '';
    click(fixture.element.querySelector('[data-command="table"]'));
    const panel = fixture.element.querySelector('[data-tool-command="table"]');

    const first = gridCell(fixture, 1, 1);
    expect(document.activeElement).toBe(first);
    keydown(first, 'ArrowRight');
    expect(document.activeElement).toBe(gridCell(fixture, 2, 1));
    keydown(document.activeElement, 'ArrowDown');
    expect(document.activeElement).toBe(gridCell(fixture, 2, 2));
    keydown(document.activeElement, 'ArrowLeft');
    expect(document.activeElement).toBe(gridCell(fixture, 1, 2));
    keydown(document.activeElement, 'Escape');
    expect(panel.hidden).toBe(true);
    expect(fixture.surface.querySelector('table')).toBeNull();

    click(fixture.element.querySelector('[data-command="table"]'));
    click(gridCell(fixture, 4, 1));
    const table = fixture.surface.querySelector('table');
    expect(table.querySelectorAll('thead th')).toHaveLength(4);
    expect(table.querySelectorAll('tbody')).toHaveLength(0);
    expect(editorModule.__testHooks.sanitizeEditorHtml(table.outerHTML)).toContain('<th>Header 4</th>');
  });

  test('buildTableHtml makes the first row the header and the remaining rows body rows', () => {
    const template = document.createElement('template');
    template.innerHTML = editorModule.__testHooks.buildTableHtml(2, 4);
    const table = template.content.querySelector('table');
    expect(Array.from(table.querySelectorAll('thead th')).map((cell) => cell.textContent)).toEqual(['Header 1', 'Header 2']);
    expect(table.querySelectorAll('tbody tr')).toHaveLength(3);
    expect(table.querySelectorAll('tbody td br')).toHaveLength(6);
  });

  test('table context menu actions apply to the cell that opened the menu and sync the value', () => {
    const fixture = createEditorFixture();
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    setSurfaceContent(fixture.surface, '<table><tbody><tr><td>A</td><td>B</td></tr></tbody></table>');
    const items = ['row-below', 'column-right', 'delete-table'].map((action) => {
      const item = document.createElement('button');
      item.dataset.tableAction = action;
      fixture.element.append(item);
      return item;
    });
    const secondCell = fixture.surface.querySelectorAll('td')[1];

    click(items[0]);
    expect(fixture.surface.querySelectorAll('tr')).toHaveLength(1);

    secondCell.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
    click(items[0]);
    expect(fixture.surface.querySelectorAll('tr')).toHaveLength(2);
    expect(window.getSelection().anchorNode.closest ? window.getSelection().anchorNode.closest('td') : window.getSelection().anchorNode).toBe(fixture.surface.querySelectorAll('tr')[1].cells[1]);

    click(items[1]);
    expect(fixture.surface.querySelectorAll('tr')[0].cells).toHaveLength(2);

    fixture.element.dataset.editable = 'false';
    fixture.surface.querySelector('td').dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
    click(items[2]);
    expect(fixture.surface.querySelector('table')).not.toBeNull();
    fixture.element.dataset.editable = 'true';

    fixture.surface.querySelector('td').dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
    click(items[2]);
    expect(fixture.surface.querySelector('table')).toBeNull();
    expect(fixture.element.querySelector('.nt-rich-text-editor-hidden-input').value).not.toContain('<table');
  });

  describe('table row and column actions', () => {
    const act = (cell, action) => editorModule.__testHooks.applyTableAction(cell, action);

    function buildTable() {
      document.body.innerHTML = '<div><table><thead><tr><th>H1</th><th>H2</th></tr></thead><tbody><tr><td>A1</td><td>A2</td></tr><tr><td>B1</td><td>B2</td></tr></tbody></table></div>';
      return document.querySelector('table');
    }

    const cellTexts = (table) => Array.from(table.rows).map((row) => Array.from(row.cells).map((cell) => `${cell.tagName.toLowerCase()}:${cell.textContent}`));

    test('inserts rows above and below with the clicked row shape', () => {
      const table = buildTable();
      const caret = act(table.querySelector('td'), 'row-below');
      expect(cellTexts(table)).toEqual([['th:H1', 'th:H2'], ['td:A1', 'td:A2'], ['td:', 'td:'], ['td:B1', 'td:B2']]);
      expect(caret).toBe(table.rows[2].cells[0]);
      expect(table.rows[2].cells[0].querySelector('br')).not.toBeNull();

      act(table.querySelector('td'), 'row-above');
      expect(table.tBodies[0].rows).toHaveLength(4);
      expect(table.tBodies[0].rows[0].cells[0].textContent).toBe('');
    });

    test.each(['row-above', 'row-below'])('%s on a header row starts the body with a td row', (action) => {
      const table = buildTable();
      const caret = act(table.querySelector('th'), action);
      expect(table.tHead.rows).toHaveLength(1);
      expect(cellTexts(table)).toEqual([['th:H1', 'th:H2'], ['td:', 'td:'], ['td:A1', 'td:A2'], ['td:B1', 'td:B2']]);
      expect(caret).toBe(table.tBodies[0].rows[0].cells[0]);
    });

    test('inserts columns left and right in every row, using th in header rows', () => {
      const table = buildTable();
      const caret = act(table.querySelectorAll('td')[1], 'column-right');
      expect(cellTexts(table)).toEqual([['th:H1', 'th:H2', 'th:'], ['td:A1', 'td:A2', 'td:'], ['td:B1', 'td:B2', 'td:']]);
      expect(caret).toBe(table.rows[1].cells[2]);

      act(table.querySelector('th'), 'column-left');
      expect(cellTexts(table)[0]).toEqual(['th:', 'th:H1', 'th:H2', 'th:']);
      expect(cellTexts(table)[1]).toEqual(['td:', 'td:A1', 'td:A2', 'td:']);
    });

    test('deletes rows and columns and removes the table when the last one goes', () => {
      const table = buildTable();
      let caret = act(table.querySelectorAll('td')[0], 'delete-row');
      expect(cellTexts(table)).toEqual([['th:H1', 'th:H2'], ['td:B1', 'td:B2']]);
      expect(caret).toBe(table.rows[1].cells[0]);

      caret = act(table.querySelectorAll('td')[1], 'delete-column');
      expect(cellTexts(table)).toEqual([['th:H1'], ['td:B1']]);
      expect(caret).toBe(table.rows[1].cells[0]);

      act(table.querySelector('td'), 'delete-row');
      expect(table.tBodies).toHaveLength(0);
      expect(table.rows).toHaveLength(1);
      expect(act(table.querySelector('th'), 'delete-column')).toBeNull();
      expect(document.querySelector('table')).toBeNull();
    });

    test('deleting the only row removes the table, and delete-table removes it outright', () => {
      const table = buildTable();
      table.tBodies[0].remove();
      expect(act(table.querySelector('th'), 'delete-row')).toBeNull();
      expect(document.querySelector('table')).toBeNull();

      const second = buildTable();
      expect(act(second.querySelector('td'), 'delete-table')).toBeNull();
      expect(document.querySelector('table')).toBeNull();
    });
  });

  test('text color tool applies the color to the selection through native commands', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    const textNode = fixture.surface.querySelector('p').firstChild;
    selectText(textNode, 0, 5);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    fixture.element.querySelector('[data-role="text-color-value"]').value = '#ff0000';
    document.execCommand.mockClear();
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    const colorCalls = document.execCommand.mock.calls.filter(([command]) => ['styleWithCSS', 'foreColor'].includes(command));
    expect(colorCalls).toEqual([
      ['styleWithCSS', false, 'true'],
      ['foreColor', false, '#ff0000'],
      ['styleWithCSS', false, 'false']
    ]);
  });

  test('link tool inserts links from the current selection and rejects unsafe URLs', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Docs</p>');
    const textNode = fixture.surface.querySelector('p').firstChild;
    selectText(textNode, 0, 4);

    click(fixture.element.querySelector('[data-command="link"]'));

    const linkUrlInput = fixture.element.querySelector('[data-role="link-url"]');
    const linkTextInput = fixture.element.querySelector('[data-role="link-text"]');
    expect(linkTextInput.value).toBe('Docs');

    linkUrlInput.value = 'javascript:alert(1)';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(fixture.surface.querySelector('a')).toBeNull();
    expect(document.activeElement).toBe(linkUrlInput);

    linkUrlInput.value = 'https://example.com/docs';
    linkTextInput.value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));

    const anchor = fixture.surface.querySelector('a');
    expect(anchor).not.toBeNull();
    expect(anchor.getAttribute('href')).toBe('https://example.com/docs');
    expect(anchor.textContent).toBe('Docs');
  });

  test('iframe tool inserts lazy-loaded embeds with default metadata', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(fixture.element.querySelector('[data-command="iframe"]'));

    fixture.element.querySelector('[data-role="iframe-url"]').value = 'https://example.com/embed';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));

    const iframe = fixture.surface.querySelector('iframe');
    expect(iframe).not.toBeNull();
    expect(iframe.getAttribute('src')).toBe('https://example.com/embed');
    expect(iframe.getAttribute('title')).toBe('Embedded content');
    expect(iframe.getAttribute('width')).toBe('100%');
    expect(iframe.getAttribute('height')).toBe('315');
    expect(iframe.getAttribute('loading')).toBe('lazy');
  });

  test('onUpdate binds newly added tool controls on the first sync', async () => {
    const fixture = createEditorFixture();
    const imageButton = fixture.element.querySelector('[data-command="image"]');
    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    imageButton.remove();
    imagePanel.remove();

    editorModule.onLoad(fixture.element, createDotNetRef());

    fixture.element.querySelector('.nt-rich-text-editor-toolbar').appendChild(imageButton);
    fixture.element.querySelector('.nt-rich-text-editor-shell').insertBefore(imagePanel, fixture.surface);
    editorModule.onUpdate(fixture.element, createDotNetRef());

    const range = document.createRange();
    range.selectNodeContents(fixture.surface);
    range.collapse(false);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    click(imageButton);
    expect(imagePanel.hidden).toBe(false);

    fixture.element.querySelector('[data-role="image-url"]').value = 'https://example.com/runtime-image.png';
    click(fixture.element.querySelector('[data-role="image-apply"]'));
    await settleAsync();

    const image = fixture.surface.querySelector('img');
    expect(image).not.toBeNull();
    expect(image.getAttribute('src')).toBe('https://example.com/runtime-image.png');
  });

  test('onDispose detaches editor listeners so later events do not resync state', () => {
    const fixture = createEditorFixture({ bindOnInput: true, value: 'Before' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);
    dotNetRef.invokeMethodAsync.mockClear();

    editorModule.onDispose(fixture.element);

    setSurfaceContent(fixture.surface, '<p>After</p>');
    fixture.surface.dispatchEvent(new Event('input', { bubbles: true }));
    click(fixture.element.querySelector('[data-command="bold"]'));

    expect(dotNetRef.invokeMethodAsync).not.toHaveBeenCalled();
    expect(document.execCommand).not.toHaveBeenCalled();
  });

  test('test hooks cover utility and normalization helpers', async () => {
    const hooks = editorModule.__testHooks;
    const root = document.createElement('div');
    root.innerHTML = '<div class="foo"><button type="button"></button><span style="color: rgb(1, 2, 3);">text</span><table><tbody><tr><td>A</td></tr></tbody></table></div>';
    const button = root.querySelector('button');
    const span = root.querySelector('span');
    const row = root.querySelector('tr');
    const cell = root.querySelector('td');

    expect(hooks.toArray({ 0: 'x', 1: 'y', length: 2 })).toEqual(['x', 'y']);
    expect(hooks.qs(root, '.foo')).toBe(root.firstElementChild);
    expect(hooks.isElement(root.firstElementChild)).toBe(true);
    expect(hooks.isHtmlButtonElement(button)).toBe(true);
    expect(hooks.isHtmlElement(root.firstElementChild)).toBe(true);
    expect(hooks.isHtmlInputElement(document.createElement('input'))).toBe(true);
    expect(hooks.isHtmlSpanElement(span)).toBe(true);
    expect(hooks.isTableCellElement(cell)).toBe(true);
    expect(hooks.isTableRowElement(row)).toBe(true);
    expect(hooks.getTableCells(row)).toHaveLength(1);


    const pendingState = { pendingSyncFrameId: setTimeout(() => {}, 50) };
    hooks.clearPendingSync(pendingState);
    expect(pendingState.pendingSyncFrameId).toBeNull();
    hooks.clearPendingSync(null);

    const caretSurface = document.createElement('div');
    caretSurface.innerHTML = '<p>Alpha</p><p>Beta</p>';
    document.body.appendChild(caretSurface);
    hooks.placeCaretAtEnd(caretSurface);
    expect(window.getSelection().anchorNode).not.toBeNull();

    expect(hooks.extractUrlScheme('https://example.com')).toBe('https');
    expect(hooks.normalizeSafeUrl('', 'link')).toBe('');
    expect(hooks.normalizeSafeUrl('#anchor', 'link')).toBe('#anchor');
    expect(hooks.normalizeSafeUrl('/relative', 'link')).toBe('/relative');
    expect(hooks.normalizeSafeUrl('mailto:test@example.com', 'link')).toBe('mailto:test@example.com');
    expect(hooks.normalizeSafeUrl('tel:555', 'link')).toBe('tel:555');
    expect(hooks.normalizeSafeUrl('blob:https://example.com/x', 'image')).toBe('blob:https://example.com/x');
    expect(hooks.normalizeSafeUrl('data:image/png;base64,abc', 'image')).toBe('data:image/png;base64,abc');
    expect(hooks.normalizeSafeUrl('javascript:alert(1)', 'link')).toBe('');
    expect(hooks.normalizeSafeUrl('ftp://example.com/file', 'iframe')).toBe('');
    expect(hooks.normalizeSafeUrl('example.com/path', 'iframe')).toBe('https://example.com/path');
    expect(hooks.getChildNodeIndex(cell)).toBe(0);
    expect(hooks.getChildNodeIndex(document.createTextNode('orphan'))).toBe(-1);

    const boundarySurface = document.createElement('div');
    boundarySurface.innerHTML = '<p><img src="https://example.com/a.png" alt="A" />Text</p>';
    document.body.appendChild(boundarySurface);
    const paragraph = boundarySurface.querySelector('p');
    const image = boundarySurface.querySelector('img');
    const textNode = paragraph.lastChild;
    expect(hooks.getBoundaryAdjacentElement(boundarySurface, textNode, 0, 'before')).toBe(image);
    expect(hooks.getBoundaryAdjacentElement(boundarySurface, textNode, textNode.textContent.length, 'after')).toBeNull();

    collapseBeforeNode(image);
    expect(hooks.getSelectionElement(boundarySurface)).toBe(image);
    expect(hooks.getCurrentBlockElement(boundarySurface)).toBe(paragraph);
    paragraph.setAttribute('align', 'center');
    expect(hooks.getCurrentAlignment(boundarySurface, paragraph)).toBe('center');
    expect(hooks.hasAncestorTag(boundarySurface, 'p')).toBe(true);
    expect(hooks.getElementTextColor(span)).toBe('rgb(1, 2, 3)');
    expect(hooks.getElementTextColor({})).toBe('');
    expect(hooks.normalizeHexColor('#abc')).toBe('#aabbcc');
    expect(hooks.normalizeHexColor('#aabbcc')).toBe('#aabbcc');
    expect(hooks.normalizeHexColor('red')).toBe('');
    expect(hooks.convertRgbChannelToHex('16')).toBe('10');
    expect(hooks.convertRgbChannelToHex('bad')).toBe('');
    expect(hooks.normalizeTextColorValue('rgb(255, 0, 128)')).toBe('#ff0080');
    expect(hooks.normalizeTextColorValue('rgba(255, 0, 128, 0.5)')).toBe('#ff0080');
    expect(hooks.normalizeTextColorValue('invalid')).toBe('');
    expect(hooks.escapeHtml(`<&>"'`)).toBe('&lt;&amp;&gt;&quot;&#39;');

    expect(hooks.normalizeImageDimension('320')).toBe('320');
    expect(hooks.normalizeImageDimension('50%')).toBe('');
    expect(hooks.normalizeTableBorderColor('#112233')).toBe('#112233');
    expect(hooks.normalizeTableBorderColor('#123')).toBe('');
    expect(hooks.buildTableStyleAttribute('#112233')).toContain('data-border-color="#112233"');
    expect(hooks.buildTableStyleAttribute('bad')).toBe('');
    expect(hooks.buildImageHtml({ src: 'https://example.com/a.png', alt: 'A', width: '100', height: '200' })).toContain('width="100"');
    expect(hooks.buildImageHtml({ src: 'javascript:bad', alt: 'A', width: '', height: '' })).toBe('');
    expect(hooks.renderHtmlImage('', 'https://example.com/a.png', 'Alt', '', '50', '60')).toContain('height="60"');
    expect(hooks.getImageDetails({})).toEqual({ src: '', alt: '', title: '', width: '', height: '' });

    const rejectedRef = { invokeMethodAsync: jest.fn(() => Promise.reject(new Error('disconnect'))) };
    const rejectedState = { dotNetRef: rejectedRef, sentValueCount: 0 };
    hooks.sendValue(rejectedState, 'UpdateMarkupValueFromJs', '<p>x</p>');
    hooks.sendValue(rejectedState, 'UpdateValueFromJs', '<p>x</p>');
    await settleAsync();
    expect(rejectedState.sentValueCount).toBe(0);
  });

  test('sanitizer drops iframes whose source is this origin', () => {
    const hooks = editorModule.__testHooks;

    const sanitized = hooks.sanitizeEditorHtml([
      '<iframe src="/upload/page.html"></iframe>',
      `<iframe src="${window.location.origin}/page"></iframe>`,
      '<iframe src="https://example.com/embed"></iframe>'
    ].join(''));

    expect(sanitized.match(/<iframe/g)).toHaveLength(1);
    expect(sanitized).toContain('src="https://example.com/embed"');
  });

  test('test hooks sanitize complex html content', () => {
    const hooks = editorModule.__testHooks;

    const surface = document.createElement('div');
    surface.innerHTML = [
      '<div onclick="bad()" align="center"><p><strong>Bold</strong> <em>Italic</em> <u>Underline</u> <s>Strike</s> <span style="color:#123456">Color</span><br><img src="https://example.com/image.png" alt="Alt" width="50" height="60" onerror="bad()" /><a href="https://example.com/docs">Docs</a></p></div>',
      '<pre data-language="ts"><code data-language="ts">const value = 1;\n</code></pre>',
      '<blockquote><p>Quote</p><p>More</p></blockquote>',
      '<ul><li>Item 1<ul><li>Nested</li></ul></li><li>Item 2</li></ul>',
      '<table data-border-color="#112233"><thead><tr><th style="text-align:center;">Name</th><th>Role</th></tr></thead><tbody><tr><td>Avery</td><td>Host</td></tr></tbody></table>',
      '<iframe src="https://example.com/embed" title="Embed" width="100%" height="315" loading="eager"></iframe>',
      '<script>alert(1)</script>'
    ].join('');

    const sanitized = hooks.sanitizeEditorHtml(surface.innerHTML);
    expect(sanitized).toContain('<strong>Bold</strong>');
    expect(sanitized).toContain('<img src="https://example.com/image.png" alt="Alt" width="50" height="60">');
    expect(sanitized).toContain('<a href="https://example.com/docs">Docs</a>');
    expect(sanitized).toContain('<table data-border-color="#112233"');
    expect(sanitized).toContain('<iframe src="https://example.com/embed" title="Embed" width="100%" height="315" loading="lazy" sandbox="allow-scripts allow-same-origin allow-popups allow-presentation"></iframe>');
    expect(sanitized).not.toContain('onclick');
    expect(sanitized).not.toContain('onerror');
    expect(sanitized).not.toContain('<script');

    expect(hooks.normalizeAlignment('start')).toBe('left');
    expect(hooks.normalizeAlignment('end')).toBe('right');
    expect(hooks.normalizeAlignment('weird')).toBe('');
    expect(hooks.getElementAlignment(surface.firstElementChild)).toBe('center');
  });

  test('test hooks cover runtime synchronization and toolbar state helpers', () => {
    const hooks = editorModule.__testHooks;
    const fixture = createEditorFixture({ bindOnInput: false, value: 'Start', maxLength: 25 });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    hooks.updateSourceValue(fixture.element, 'Updated');
    expect(fixture.hiddenInput.value).toBe('Updated');
    expect(fixture.hiddenInput.value).toBe('Updated');
    expect(fixture.sourceValue.textContent).toBe('Start');

    hooks.updateEmptyState(fixture.surface);
    expect(fixture.surface.dataset.empty).toBe('false');
    fixture.surface.innerHTML = '';
    hooks.updateEmptyState(fixture.surface);
    expect(fixture.surface.dataset.empty).toBe('true');

    fixture.surface.innerHTML = '<p><br></p>';
    hooks.updateEmptyState(fixture.surface);
    expect(fixture.surface.dataset.empty).toBe('true');
    expect(hooks.surfaceToValue(fixture.surface)).toBe('');

    hooks.applyHtmlToSurface(fixture.element, '<p><strong>Bold</strong></p>');
    expect(fixture.surface.innerHTML).toContain('<strong>Bold</strong>');

    setSurfaceContent(fixture.surface, '<p>Sync</p>');
    hooks.syncValueFromSurface(fixture.element, true);
    expect(fixture.hiddenInput.value).toBe('<p>Sync</p>');
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(Number), '<p>Sync</p>');

    setSurfaceContent(fixture.surface, '<p>Commit</p>');
    hooks.commitValue(fixture.element);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('CommitValueFromJs', expect.any(Number), '<p>Commit</p>');

    setSurfaceContent(fixture.surface, '<blockquote>Quote</blockquote>');
    selectText(fixture.surface.querySelector('blockquote').firstChild, 0, 2);
    fixture.element.querySelector('.nt-rich-text-editor-toolbar').innerHTML = `
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="paragraph" data-value="" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="heading" data-value="1" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="alignLeft" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="alignCenter" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="alignRight" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="alignJustify" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="unorderedList" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="orderedList" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="blockquote" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="codeBlock" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="bold" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="italic" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="underline" aria-pressed="false"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="strikeThrough" aria-pressed="false"></button>
    `;
    editorModule.onUpdate(fixture.element, dotNetRef);
    hooks.updateToolbarState(fixture.element);
    expect(fixture.element.querySelector('[data-command="blockquote"]').getAttribute('aria-pressed')).toBe('true');

    setSurfaceContent(fixture.surface, '<ul><li><strong><em><u><s>Alpha</s></u></em></strong></li></ul>');
    selectText(fixture.surface.querySelector('s').firstChild, 0, 5);
    hooks.updateToolbarState(fixture.element);
    expect(fixture.element.querySelector('[data-command="unorderedList"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="bold"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="italic"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="underline"]').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.element.querySelector('[data-command="strikeThrough"]').getAttribute('aria-pressed')).toBe('true');

    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B', ctrlKey: true }), 'Control+B')).toBe(true);
    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B' }), '')).toBe(false);
    expect(hooks.eventMatchesShortcut(new KeyboardEvent('keydown', { key: 'B', shiftKey: true }), 'Control+B')).toBe(false);
  });

  test('selection hooks clear invalid ranges and fall back to a caret inside the surface', () => {
    const hooks = editorModule.__testHooks;
    const surface = document.createElement('div');
    surface.innerHTML = '<p>Alpha</p><p>Beta</p>';
    document.body.appendChild(surface);
    const state = { selectionRange: 'sentinel' };

    window.getSelection().removeAllRanges();
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();

    const outside = document.createElement('div');
    outside.textContent = 'Outside';
    document.body.appendChild(outside);
    selectText(outside.firstChild, 0, 3);
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();

    const insideText = surface.querySelector('p').firstChild;
    selectText(insideText, 0, 5);
    hooks.saveSelectionRange(surface, state);
    expect(state.selectionRange).not.toBeNull();

    const detachedHost = document.createElement('div');
    detachedHost.innerHTML = '<p>Detached</p>';
    const detachedRange = document.createRange();
    detachedRange.setStart(detachedHost.querySelector('p').firstChild, 0);
    detachedRange.setEnd(detachedHost.querySelector('p').firstChild, 3);
    state.selectionRange = detachedRange;
    hooks.restoreSelectionRange(surface, state);
    expect(state.selectionRange).toBeNull();
    expect(window.getSelection().rangeCount).toBe(1);
    expect(surface.contains(window.getSelection().anchorNode)).toBe(true);

    hooks.restoreSelectionRange(surface, null);
    expect(window.getSelection().rangeCount).toBe(1);
  });

  test('image tool updates existing images, clears dimensions, autofills alt text, and rejects unsafe urls', async () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><img src="https://example.com/old.png" alt="" width="40" height="50" /></p>');
    const image = fixture.surface.querySelector('img');
    collapseBeforeNode(image);
    click(fixture.element.querySelector('[data-command="image"]'));

    const fileInput = fixture.element.querySelector('[data-role="image-file"]');
    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new File(['x'], 'photo.png', { type: 'image/png' })]
    });
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));
    await settleAsync();

    expect(fixture.element.querySelector('[data-role="image-url"]').value).toBe('');
    expect(fixture.element.querySelector('[data-role="image-alt"]').value).toBe('photo');
    click(fixture.element.querySelector('[data-role="image-apply"]'));
    await settleAsync();
    expect(image.getAttribute('src')).toContain('data:image/png;base64');

    collapseBeforeNode(image);
    click(fixture.element.querySelector('[data-command="image"]'));
    fixture.element.querySelector('[data-role="image-url"]').value = 'https://example.com/new.png';
    fixture.element.querySelector('[data-role="image-alt"]').value = 'Updated';
    fixture.element.querySelector('[data-role="image-width"]').value = '';
    fixture.element.querySelector('[data-role="image-height"]').value = '';
    click(fixture.element.querySelector('[data-role="image-apply"]'));
    await settleAsync();

    const updatedImage = fixture.surface.querySelector('img');
    expect(updatedImage.getAttribute('src')).toBe('https://example.com/new.png');
    expect(updatedImage.getAttribute('alt')).toBe('Updated');
    expect(updatedImage.hasAttribute('width')).toBe(false);
    expect(updatedImage.hasAttribute('height')).toBe(false);

    collapseBeforeNode(updatedImage);
    click(fixture.element.querySelector('[data-command="image"]'));
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    imageUrlInput.value = 'javascript:alert(1)';
    click(fixture.element.querySelector('[data-role="image-apply"]'));

    expect(document.activeElement).toBe(imageUrlInput);
    expect(fixture.surface.querySelector('img').getAttribute('src')).toBe('https://example.com/new.png');
  });

  test('link tool updates existing anchors and falls back to selected text or url when inserting', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p><a href="https://example.com/old">Old</a></p>');
    const anchor = fixture.surface.querySelector('a');
    selectText(anchor.firstChild, 0, 3);
    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/new';
    fixture.element.querySelector('[data-role="link-text"]').value = 'New';
    click(fixture.element.querySelector('[data-role="link-apply"]'));

    expect(anchor.getAttribute('href')).toBe('https://example.com/new');
    expect(anchor.textContent).toBe('New');

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/selected';
    fixture.element.querySelector('[data-role="link-text"]').value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(fixture.surface.querySelector('a').textContent).toBe('Alpha');

    setSurfaceContent(fixture.surface, '<p>Tail</p>');
    const collapsedRange = document.createRange();
    collapsedRange.setStart(fixture.surface.querySelector('p').firstChild, 4);
    collapsedRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(collapsedRange);

    click(fixture.element.querySelector('[data-command="link"]'));
    fixture.element.querySelector('[data-role="link-url"]').value = 'https://example.com/url';
    fixture.element.querySelector('[data-role="link-text"]').value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    const links = fixture.surface.querySelectorAll('a');
    expect(links[links.length - 1].textContent).toBe('https://example.com/url');
  });

  test('table and iframe tools update existing nodes in place with preserved content and defaults', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(
      fixture.surface,
      '<table data-border-color="#112233"><thead><tr><th>H1</th><th>H2</th></tr></thead><tbody><tr><td>A1</td><td>A2</td></tr></tbody></table>'
    );
    const table = fixture.surface.querySelector('table');
    selectText(table.querySelector('td').firstChild, 0, 1);
    click(fixture.element.querySelector('[data-command="table"]'));
    fixture.element.querySelector('[data-role="table-border-color"]').value = '#123456';
    fixture.element.querySelector('[data-role="table-caption"]').value = 'Totals';
    click(fixture.element.querySelector('[data-role="table-apply"]'));

    const updatedTable = fixture.surface.querySelector('table');
    expect(updatedTable).toBe(table);
    expect(Array.from(updatedTable.querySelectorAll('th, td')).map((cell) => cell.textContent)).toEqual(['H1', 'H2', 'A1', 'A2']);
    expect(updatedTable.getAttribute('data-border-color')).toBe('#123456');
    expect(updatedTable.style.getPropertyValue('--nt-rich-text-table-border-color')).toBe('#123456');
    expect(updatedTable.querySelector(':scope > caption').textContent).toBe('Totals');
    expect(updatedTable.firstElementChild.tagName).toBe('CAPTION');

    click(fixture.element.querySelector('[data-command="table"]'));
    fixture.element.querySelector('[data-role="table-caption"]').value = '';
    click(fixture.element.querySelector('[data-role="table-apply"]'));
    expect(updatedTable.querySelector('caption')).toBeNull();

    setSurfaceContent(fixture.surface, '<p><iframe src="https://example.com/old" title="Old" width="640" height="480"></iframe></p>');
    const iframe = fixture.surface.querySelector('iframe');
    collapseBeforeNode(iframe);
    click(fixture.element.querySelector('[data-command="iframe"]'));
    fixture.element.querySelector('[data-role="iframe-url"]').value = 'https://example.com/new';
    fixture.element.querySelector('[data-role="iframe-title"]').value = '';
    fixture.element.querySelector('[data-role="iframe-width"]').value = '';
    fixture.element.querySelector('[data-role="iframe-height"]').value = '';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));

    expect(iframe.getAttribute('src')).toBe('https://example.com/new');
    expect(iframe.getAttribute('title')).toBe('Embedded content');
    expect(iframe.getAttribute('width')).toBe('100%');
    expect(iframe.getAttribute('height')).toBe('315');
    expect(iframe.getAttribute('loading')).toBe('lazy');
  });

  test('text color tool blocks collapsed-range applies', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Plain</p>');
    const plainText = fixture.surface.querySelector('p').firstChild;
    const collapsedRange = document.createRange();
    collapsedRange.setStart(plainText, 5);
    collapsedRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(collapsedRange);
    click(fixture.element.querySelector('[data-command="textColor"]'));
    const textColorInput = fixture.element.querySelector('[data-role="text-color-value"]');
    textColorInput.value = '#654321';
    click(fixture.element.querySelector('[data-role="text-color-apply"]'));

    expect(document.activeElement).toBe(textColorInput);
    expect(fixture.surface.innerHTML).toBe('<p>Plain</p>');
    expect(document.execCommand).not.toHaveBeenCalledWith('foreColor', false, '#654321');
  });

  test('command routing handles toggles, multiple shortcut expressions, and tab list indentation', () => {
    const hooks = editorModule.__testHooks;
    const fixture = createEditorFixture({ bindOnInput: true, value: 'Start' });
    const dotNetRef = createDotNetRef();
    editorModule.onLoad(fixture.element, dotNetRef);

    setSurfaceContent(fixture.surface, '<blockquote><p>Quote</p></blockquote>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    document.execCommand.mockClear();
    hooks.executeEditorCommand(fixture.element, 'blockquote');
    expect(document.execCommand).toHaveBeenCalledWith('outdent', false);

    setSurfaceContent(fixture.surface, '<pre><code>const x = 1;</code></pre>');
    selectText(fixture.surface.querySelector('code').firstChild, 0, 5);
    hooks.executeEditorCommand(fixture.element, 'codeBlock');
    expect(fixture.surface.querySelector('pre')).toBeNull();
    expect(fixture.surface.innerHTML).toBe('<p>const x = 1;</p>');

    hooks.executeEditorCommand(fixture.element, 'heading', '2');
    expect(document.execCommand).toHaveBeenCalledWith('formatBlock', false, 'H2');
    hooks.executeEditorCommand(fixture.element, 'alignCenter');
    expect(document.execCommand).toHaveBeenCalledWith('justifyCenter', false);
    hooks.executeEditorCommand(fixture.element, 'orderedList');
    expect(document.execCommand).toHaveBeenCalledWith('insertOrderedList', false);

    fixture.element.querySelector('.nt-rich-text-editor-toolbar').innerHTML = `
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="bold" aria-keyshortcuts="Control+B" disabled></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="italic" aria-keyshortcuts="Control+Alt+9 Control+Shift+9"></button>
      <button type="button" class="nt-rich-text-editor-toolbar-button" data-command="underline"></button>
    `;
    editorModule.onUpdate(fixture.element, dotNetRef);
    fixture.element.querySelector('[data-command="bold"]').disabled = true;

    document.execCommand.mockClear();
    const handledShortcut = hooks.tryHandleShortcut(
      fixture.element,
      new KeyboardEvent('keydown', { key: '9', ctrlKey: true, altKey: true, bubbles: true, cancelable: true })
    );
    expect(handledShortcut).toBe(true);
    expect(document.execCommand).toHaveBeenCalledWith('italic', false);

    document.execCommand.mockClear();
    const ignoredShortcut = hooks.tryHandleShortcut(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'B', ctrlKey: true, bubbles: true, cancelable: true })
    );
    expect(ignoredShortcut).toBe(false);
    expect(document.execCommand).not.toHaveBeenCalled();

    setSurfaceContent(fixture.surface, '<ul><li>Item</li></ul>');
    selectText(fixture.surface.querySelector('li').firstChild, 0, 4);
    document.execCommand.mockClear();
    hooks.handleEditorKeyDown(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true })
    );
    expect(document.execCommand).toHaveBeenCalledWith('indent', false);
    expect(dotNetRef.invokeMethodAsync).toHaveBeenCalledWith('UpdateValueFromJs', expect.any(Number), '<ul><li>Item</li></ul>');

    document.execCommand.mockClear();
    const topLevelShiftTab = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true });
    hooks.handleEditorKeyDown(fixture.element, topLevelShiftTab);
    expect(document.execCommand).not.toHaveBeenCalledWith('outdent', false);
    expect(topLevelShiftTab.defaultPrevented).toBe(false);

    setSurfaceContent(fixture.surface, '<ul><li>Parent<ul><li>Child</li></ul></li></ul>');
    selectText(fixture.surface.querySelector('ul ul li').firstChild, 0, 5);
    document.execCommand.mockClear();
    hooks.handleEditorKeyDown(
      fixture.element,
      new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true })
    );
    expect(document.execCommand).toHaveBeenCalledWith('outdent', false);
  });

  test('image and table tools support cancel, escape, enter, and invalid-input paths', async () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());
    const imageToolbarButton = fixture.element.querySelector('[data-command="image"]');

    setSurfaceContent(fixture.surface, '<p>Image host</p>');
    const imageText = fixture.surface.querySelector('p').firstChild;
    const imageRange = document.createRange();
    imageRange.setStart(imageText, imageText.textContent.length);
    imageRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(imageRange);

    click(fixture.element.querySelector('[data-command="image"]'));
    const imagePanel = fixture.element.querySelector('[data-tool-command="image"]');
    const imageUrlInput = fixture.element.querySelector('[data-role="image-url"]');
    const imageAltInput = fixture.element.querySelector('[data-role="image-alt"]');
    expect(imagePanel.hidden).toBe(false);
    expect(imageToolbarButton.getAttribute('aria-expanded')).toBe('true');

    click(fixture.element.querySelector('[data-role="image-cancel"]'));
    expect(imagePanel.hidden).toBe(true);
    expect(imageToolbarButton.getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="image"]'));
    keydown(imageUrlInput, 'Escape');
    expect(imagePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="image"]'));
    imageUrlInput.value = 'https://example.com/enter.png';
    imageAltInput.value = 'Enter image';
    keydown(imageAltInput, 'Enter');
    await settleAsync();
    expect(fixture.surface.querySelector('img').getAttribute('src')).toBe('https://example.com/enter.png');

    setSurfaceContent(fixture.surface, '<p>Table host</p>');
    const tableText = fixture.surface.querySelector('p').firstChild;
    const tableRange = document.createRange();
    tableRange.setStart(tableText, tableText.textContent.length);
    tableRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(tableRange);

    click(fixture.element.querySelector('[data-command="table"]'));
    const tablePanel = fixture.element.querySelector('[data-tool-command="table"]');
    const captionInput = fixture.element.querySelector('[data-role="table-caption"]');
    expect(tablePanel.hidden).toBe(false);
    expect(fixture.element.querySelector('[data-role="table-insert"]').hidden).toBe(false);

    keydown(gridCell(fixture, 1, 1), 'Escape');
    expect(tablePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    setSurfaceContent(fixture.surface, '<table><tbody><tr><td>Cell</td></tr></tbody></table>');
    selectText(fixture.surface.querySelector('td').firstChild, 0, 1);
    click(fixture.element.querySelector('[data-command="table"]'));
    expect(fixture.element.querySelector('[data-role="table-edit"]').hidden).toBe(false);
    expect(fixture.element.querySelector('[data-role="table-insert"]').hidden).toBe(true);

    click(fixture.element.querySelector('[data-role="table-cancel"]'));
    expect(tablePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="table"]'));
    keydown(captionInput, 'Escape');
    expect(tablePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="table"]'));
    captionInput.value = 'Enter caption';
    keydown(captionInput, 'Enter');
    expect(fixture.surface.querySelectorAll('table')).toHaveLength(1);
    expect(fixture.surface.querySelector('caption').textContent).toBe('Enter caption');
  });

  test('link, iframe, and text-color tools support escape, cancel, enter, and invalid apply branches', () => {
    const fixture = createEditorFixture();
    editorModule.onLoad(fixture.element, createDotNetRef());

    setSurfaceContent(fixture.surface, '<p>Alpha Beta</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);

    click(fixture.element.querySelector('[data-command="link"]'));
    const linkPanel = fixture.element.querySelector('[data-tool-command="link"]');
    const linkUrlInput = fixture.element.querySelector('[data-role="link-url"]');
    const linkTextInput = fixture.element.querySelector('[data-role="link-text"]');
    keydown(linkUrlInput, 'Escape');
    expect(linkPanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="link"]'));
    linkUrlInput.value = '';
    click(fixture.element.querySelector('[data-role="link-apply"]'));
    expect(document.activeElement).toBe(linkUrlInput);

    linkUrlInput.value = 'https://example.com/enter-link';
    linkTextInput.value = 'Entered';
    keydown(linkTextInput, 'Enter');
    expect(fixture.surface.querySelector('a').textContent).toBe('Entered');

    setSurfaceContent(fixture.surface, '<p>Iframe host</p>');
    const iframeText = fixture.surface.querySelector('p').firstChild;
    const iframeRange = document.createRange();
    iframeRange.setStart(iframeText, iframeText.textContent.length);
    iframeRange.collapse(true);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(iframeRange);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    const iframePanel = fixture.element.querySelector('[data-tool-command="iframe"]');
    const iframeUrlInput = fixture.element.querySelector('[data-role="iframe-url"]');
    const iframeHeightInput = fixture.element.querySelector('[data-role="iframe-height"]');
    click(fixture.element.querySelector('[data-role="iframe-cancel"]'));
    expect(iframePanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    keydown(iframeUrlInput, 'Escape');
    expect(iframePanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="iframe"]'));
    iframeUrlInput.value = '';
    click(fixture.element.querySelector('[data-role="iframe-apply"]'));
    expect(document.activeElement).toBe(iframeUrlInput);

    iframeUrlInput.value = 'https://example.com/embed-enter';
    iframeHeightInput.value = '320';
    keydown(iframeHeightInput, 'Enter');
    expect(fixture.surface.querySelector('iframe').getAttribute('src')).toBe('https://example.com/embed-enter');

    setSurfaceContent(fixture.surface, '<p>Color me</p>');
    selectText(fixture.surface.querySelector('p').firstChild, 0, 5);
    click(fixture.element.querySelector('[data-command="textColor"]'));
    const textColorPanel = fixture.element.querySelector('[data-tool-command="textColor"]');
    const textColorInput = fixture.element.querySelector('[data-role="text-color-value"]');
    click(fixture.element.querySelector('[data-role="text-color-cancel"]'));
    expect(textColorPanel.hidden).toBe(true);
    expect(document.activeElement).toBe(fixture.surface);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    keydown(textColorInput, 'Escape');
    expect(textColorPanel.hidden).toBe(true);

    click(fixture.element.querySelector('[data-command="textColor"]'));
    textColorInput.value = '#ff0000';
    document.execCommand.mockClear();
    keydown(textColorInput, 'Enter');
    expect(document.execCommand).toHaveBeenCalledWith('foreColor', false, '#ff0000');
  });
});
