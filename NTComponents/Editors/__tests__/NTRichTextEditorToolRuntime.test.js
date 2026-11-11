import { join } from 'path';
import { pathToFileURL } from 'url';
import { jest } from '@jest/globals';

const repoRoot = process.cwd();

function moduleUrl(relativePath) {
  return pathToFileURL(join(repoRoot, relativePath)).href;
}

async function importModule(relativePath) {
  return import(moduleUrl(relativePath));
}

function click(element) {
  element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
}

function keydown(element, key) {
  element.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
}

async function settleAsync() {
  await Promise.resolve();
  await new Promise((resolve) => setTimeout(resolve, 0));
  await Promise.resolve();
}

function selectText(node, startOffset, endOffset) {
  const range = document.createRange();
  range.setStart(node, startOffset);
  range.setEnd(node, endOffset);
  const selection = window.getSelection();
  selection.removeAllRanges();
  selection.addRange(range);
  return range;
}

function collapseAtEnd(node) {
  const range = document.createRange();
  range.setStart(node, node.textContent.length);
  range.collapse(true);
  const selection = window.getSelection();
  selection.removeAllRanges();
  selection.addRange(range);
  return range;
}

function createEditorState() {
  return {
    selectionRange: null,
    toolStates: new Map()
  };
}

function createHost(element, surface, options = {}) {
  let selectionElement = options.selectionElement ?? surface;
  let focusedToolCommand = options.focusedToolCommand ?? null;
  let restoredRange = options.restoredRange ?? null;

  return {
    getSurface: jest.fn(() => (Object.hasOwn(options, 'surfaceOverride') ? options.surfaceOverride : surface)),
    closeOtherTools: jest.fn(),
    getSelectionElement: jest.fn(() => selectionElement),
    saveSelectionRange: jest.fn((_surface, editorState) => {
      const selection = window.getSelection();
      editorState.selectionRange = selection.rangeCount ? selection.getRangeAt(0).cloneRange() : null;
    }),
    restoreSelectionRange: jest.fn((_surface, editorState) => {
      const selection = window.getSelection();
      selection.removeAllRanges();
      if (restoredRange) {
        selection.addRange(restoredRange);
        return;
      }

      if (editorState.selectionRange) {
        selection.addRange(editorState.selectionRange);
      }
    }),
    getToolPanel: (editorElement, command) => editorElement.querySelector(`[data-tool-command="${command}"]`),
    setToolPanelOpen: jest.fn((editorElement, command, isOpen) => {
      const panel = editorElement.querySelector(`[data-tool-command="${command}"]`);
      if (panel) {
        panel.hidden = !isOpen;
        panel.setAttribute('aria-hidden', `${!isOpen}`);
      }
    }),
    updateToolbarState: jest.fn(),
    syncValueFromSurface: jest.fn(),
    setToolbarButtonPressed: jest.fn(),
    getFocusedToolCommand: jest.fn(() => focusedToolCommand),
    setSelectionElement(value) {
      selectionElement = value;
    },
    setFocusedToolCommand(value) {
      focusedToolCommand = value;
    },
    setRestoredRange(value) {
      restoredRange = value;
    }
  };
}

describe('NTRichTextEditor registered tool runtimes', () => {
  let editorModule;
  let originalExecCommand;
  let originalFileReader;
  let originalFetch;

  beforeAll(async () => {
    editorModule = await importModule('NTComponents/Editors/NTRichTextEditor.razor.js');
  });

  beforeEach(() => {
    document.body.innerHTML = '';

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
  });

  afterEach(() => {
    document.execCommand = originalExecCommand;
    global.FileReader = originalFileReader;
    global.fetch = originalFetch;
    document.body.innerHTML = '';
  });

  test('image and table tools cover direct runtime branches', async () => {
    document.body.innerHTML = `
      <nt-rich-text-editor data-editable="true">
        <div data-tool-command="image" data-role="image-editor" hidden aria-hidden="true">
          <input data-role="image-url" type="text" />
          <input data-role="image-file" type="file" />
          <input data-role="image-alt" type="text" />
          <input data-role="image-title" type="text" />
          <input data-role="image-width" type="text" />
          <input data-role="image-height" type="text" />
          <button type="button" data-role="image-apply"></button>
          <button type="button" data-role="image-cancel"></button>
        </div>
        <div data-tool-command="table" data-role="table-editor" hidden aria-hidden="true">
          <div data-role="table-insert">
            <div data-role="table-grid">
              <button type="button" data-col="1" data-row="1"></button>
              <button type="button" data-col="2" data-row="1"></button>
            </div>
            <span data-role="table-grid-label"></span>
          </div>
          <div data-role="table-edit" hidden>
            <input data-role="table-border-color" type="text" />
            <input data-role="table-caption" type="text" />
            <button type="button" data-role="table-apply"></button>
            <button type="button" data-role="table-cancel"></button>
          </div>
        </div>
        <div class="nt-rich-text-editor-surface" contenteditable="true"><p>Host</p></div>
      </nt-rich-text-editor>
    `;

    const element = document.querySelector('nt-rich-text-editor');
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    const imageTool = editorModule.getRichTextEditorTool('image');
    const tableTool = editorModule.getRichTextEditorTool('table');

    const imageState = createEditorState();
    const imageHost = createHost(element, surface, { selectionElement: surface.querySelector('p') });
    const imageContext = editorModule.createRichTextEditorToolContext(element, imageState, imageHost, imageTool);
    imageTool.bind(imageContext);
    imageTool.setDisabled(imageContext, true);
    imageTool.setDisabled(imageContext, false);

    collapseAtEnd(surface.querySelector('p').firstChild);
    imageTool.execute(imageContext);
    click(element.querySelector('[data-role="image-cancel"]'));
    expect(imageHost.updateToolbarState).toHaveBeenCalled();

    imageTool.execute(imageContext);
    element.querySelector('[data-role="image-url"]').value = '../images/direct.png';
    element.querySelector('[data-role="image-alt"]').value = 'Relative';
    keydown(element.querySelector('[data-role="image-alt"]'), 'Enter');
    await settleAsync();
    expect(surface.querySelector('img').getAttribute('src')).toBe('../images/direct.png');

    surface.innerHTML = '<p>Plain</p>';
    imageHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p').firstChild);
    imageTool.execute(imageContext);
    element.querySelector('[data-role="image-url"]').value = 'example.com/plain.png';
    element.querySelector('[data-role="image-alt"]').value = '';
    click(element.querySelector('[data-role="image-apply"]'));
    await settleAsync();
    const plainImage = surface.querySelectorAll('img')[0];
    expect(plainImage.getAttribute('src')).toBe('example.com/plain.png');

    surface.innerHTML = '<p>Remote</p>';
    imageHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p').firstChild);
    imageTool.execute(imageContext);
    element.querySelector('[data-role="image-url"]').value = 'https://example.com/remote.png';
    element.querySelector('[data-role="image-title"]').value = 'Remote title';
    click(element.querySelector('[data-role="image-apply"]'));
    await settleAsync();
    const remoteImage = surface.querySelector('img');
    expect(remoteImage.getAttribute('src')).toBe('https://example.com/remote.png');
    expect(remoteImage.getAttribute('title')).toBe('Remote title');
    expect(global.fetch).not.toHaveBeenCalled();

    surface.innerHTML = '<p>Upload host</p>';
    imageHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p').firstChild);
    imageHost.syncValueFromSurface.mockClear();
    imageHost.updateToolbarState.mockClear();
    imageTool.execute(imageContext);
    const fileInput = element.querySelector('[data-role="image-file"]');
    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new File(['fake'], 'uploaded-image.png', { type: 'image/png' })]
    });
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));
    await settleAsync();
    // Choosing a file only stages it; nothing is inserted until Insert is clicked.
    expect(surface.querySelector('img')).toBeNull();
    expect(element.querySelector('[data-role="image-url"]').value).toBe('');
    click(element.querySelector('[data-role="image-apply"]'));
    await settleAsync();
    const uploadedImage = surface.querySelector('img');
    expect(uploadedImage.getAttribute('src')).toBe('data:image/png;base64,ZmFrZQ==');
    expect(uploadedImage.getAttribute('alt')).toBe('uploaded-image');
    expect(imageHost.syncValueFromSurface).toHaveBeenCalledWith(element, true);
    expect(imageHost.updateToolbarState).toHaveBeenCalled();

    surface.innerHTML = '<p>Locked</p>';
    collapseAtEnd(surface.querySelector('p').firstChild);
    imageTool.execute(imageContext);
    element.dataset.editable = 'false';
    fileInput.dispatchEvent(new Event('change', { bubbles: true }));
    await settleAsync();
    expect(surface.querySelector('img')).toBeNull();
    element.dataset.editable = 'true';

    imageContext.toolState.onImageFileChange({ target: null });
    imageTool.close(imageContext);
    expect(imageState.selectionRange).toBeNull();
    imageTool.unbind(imageContext);

    const imageNoSurfaceState = createEditorState();
    const imageNoSurfaceHost = createHost(element, surface, { surfaceOverride: null });
    const imageNoSurfaceContext = editorModule.createRichTextEditorToolContext(element, imageNoSurfaceState, imageNoSurfaceHost, imageTool);
    imageTool.bind(imageNoSurfaceContext);
    expect(imageTool.execute(imageNoSurfaceContext)).toBe(false);
    click(element.querySelector('[data-role="image-apply"]'));
    await settleAsync();

    surface.innerHTML = '<table style="--nt-rich-text-table-border-color:#445566;"><tbody><tr><td>Only</td></tr></tbody></table>';
    const existingTable = surface.querySelector('table');
    imageHost.setSelectionElement(existingTable);
    const tableState = createEditorState();
    const tableHost = createHost(element, surface, { selectionElement: existingTable.querySelector('td') });
    const tableContext = editorModule.createRichTextEditorToolContext(element, tableState, tableHost, tableTool);
    tableTool.bind(tableContext);
    selectText(existingTable.querySelector('td').firstChild, 0, 1);
    tableTool.execute(tableContext);
    expect(element.querySelector('[data-role="table-border-color"]').value).toBe('#445566');
    expect(element.querySelector('[data-role="table-caption"]').value).toBe('');
    click(element.querySelector('[data-role="table-cancel"]'));
    expect(tableHost.updateToolbarState).toHaveBeenCalled();

    selectText(existingTable.querySelector('td').firstChild, 0, 1);
    tableTool.execute(tableContext);
    element.querySelector('[data-role="table-border-color"]').value = '#112233';
    element.querySelector('[data-role="table-caption"]').value = 'Kept';
    click(element.querySelector('[data-role="table-apply"]'));
    expect(surface.querySelector('table')).toBe(existingTable);
    expect(existingTable.querySelector('td').textContent).toBe('Only');
    expect(existingTable.dataset.borderColor).toBe('#112233');
    expect(existingTable.querySelector('caption').textContent).toBe('Kept');

    surface.innerHTML = '<p>Table host</p>';
    tableHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p').firstChild);
    tableTool.execute(tableContext);
    click(element.querySelector('[data-role="table-grid"] button[data-col="2"]'));
    const insertedTable = surface.querySelector('table');
    expect(insertedTable.dataset.borderColor).toBe('#94a3b8');
    expect(insertedTable.querySelectorAll('thead th')).toHaveLength(2);
    expect(insertedTable.querySelector('tbody')).toBeNull();

    tableTool.close(tableContext);
    expect(tableState.selectionRange).toBeNull();
    tableTool.unbind(tableContext);

    const tableNoSurfaceState = createEditorState();
    const tableNoSurfaceHost = createHost(element, surface, { surfaceOverride: null });
    const tableNoSurfaceContext = editorModule.createRichTextEditorToolContext(element, tableNoSurfaceState, tableNoSurfaceHost, tableTool);
    tableTool.bind(tableNoSurfaceContext);
    expect(tableTool.execute(tableNoSurfaceContext)).toBe(false);
    click(element.querySelector('[data-role="table-apply"]'));
  });

  test('link and iframe tools cover direct runtime branches', () => {
    document.body.innerHTML = `
      <nt-rich-text-editor>
        <div data-tool-command="link" data-role="link-editor" hidden aria-hidden="true">
          <input data-role="link-url" type="text" />
          <input data-role="link-text" type="text" />
          <input data-role="link-aria-label" type="text" />
          <input data-role="link-title" type="text" />
          <button type="button" data-role="link-apply"></button>
          <button type="button" data-role="link-cancel"></button>
        </div>
        <div data-tool-command="iframe" data-role="iframe-editor" hidden aria-hidden="true">
          <input data-role="iframe-url" type="text" />
          <input data-role="iframe-title" type="text" />
          <input data-role="iframe-width" type="text" />
          <input data-role="iframe-height" type="text" />
          <button type="button" data-role="iframe-apply"></button>
          <button type="button" data-role="iframe-cancel"></button>
        </div>
        <div class="nt-rich-text-editor-surface" contenteditable="true"><p>Docs</p></div>
      </nt-rich-text-editor>
    `;

    const element = document.querySelector('nt-rich-text-editor');
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    const linkTool = editorModule.getRichTextEditorTool('link');
    const iframeTool = editorModule.getRichTextEditorTool('iframe');

    const linkState = createEditorState();
    const linkHost = createHost(element, surface, { selectionElement: surface.querySelector('p') });
    const linkContext = editorModule.createRichTextEditorToolContext(element, linkState, linkHost, linkTool);
    linkTool.bind(linkContext);

    selectText(surface.querySelector('p').firstChild, 0, 4);
    linkTool.execute(linkContext);
    click(element.querySelector('[data-role="link-cancel"]'));
    expect(linkHost.updateToolbarState).toHaveBeenCalled();

    selectText(surface.querySelector('p').firstChild, 0, 4);
    linkTool.execute(linkContext);
    element.querySelector('[data-role="link-url"]').value = '../docs';
    element.querySelector('[data-role="link-aria-label"]').value = 'Documentation';
    element.querySelector('[data-role="link-title"]').value = 'Read documentation';
    keydown(element.querySelector('[data-role="link-text"]'), 'Enter');
    expect(surface.querySelector('a').getAttribute('href')).toBe('../docs');
    expect(surface.querySelector('a').getAttribute('aria-label')).toBe('Documentation');
    expect(surface.querySelector('a').getAttribute('title')).toBe('Read documentation');

    surface.innerHTML = '<p></p>';
    linkHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p'));
    linkTool.execute(linkContext);
    element.querySelector('[data-role="link-url"]').value = 'example.com/plain';
    element.querySelector('[data-role="link-text"]').value = '';
    click(element.querySelector('[data-role="link-apply"]'));
    const insertedLink = surface.querySelector('a');
    expect(insertedLink.getAttribute('href')).toBe('https://example.com/plain');
    expect(insertedLink.textContent).toBe('https://example.com/plain');

    const detachedHost = document.createElement('div');
    detachedHost.innerHTML = '<p>Outside</p>';
    document.body.appendChild(detachedHost);
    const detachedRange = document.createRange();
    detachedRange.setStart(detachedHost.querySelector('p').firstChild, 0);
    detachedRange.setEnd(detachedHost.querySelector('p').firstChild, 3);
    linkHost.setRestoredRange(detachedRange);
    linkContext.toolState.linkTarget = null;
    element.querySelector('[data-role="link-url"]').value = 'https://example.com/fallback';
    click(element.querySelector('[data-role="link-apply"]'));
    expect(document.activeElement).toBe(element.querySelector('[data-role="link-url"]'));

    linkHost.setFocusedToolCommand('link');
    linkTool.syncState(linkContext);
    linkHost.setFocusedToolCommand('other');
    linkTool.syncState(linkContext);
    linkHost.setFocusedToolCommand(null);
    linkHost.setSelectionElement(insertedLink);
    linkTool.syncState(linkContext);
    expect(element.querySelector('[data-role="link-aria-label"]').value).toBe('');
    expect(element.querySelector('[data-role="link-title"]').value).toBe('');
    expect(linkHost.setToolbarButtonPressed).toHaveBeenCalled();

    linkTool.close(linkContext);
    expect(linkState.selectionRange).toBeNull();
    linkTool.unbind(linkContext);

    const linkNoSurfaceState = createEditorState();
    const linkNoSurfaceHost = createHost(element, surface, { surfaceOverride: null });
    const linkNoSurfaceContext = editorModule.createRichTextEditorToolContext(element, linkNoSurfaceState, linkNoSurfaceHost, linkTool);
    linkTool.bind(linkNoSurfaceContext);
    expect(linkTool.execute(linkNoSurfaceContext)).toBe(false);
    click(element.querySelector('[data-role="link-apply"]'));

    surface.innerHTML = '<p>Embed</p>';
    const iframeState = createEditorState();
    const iframeHost = createHost(element, surface, { selectionElement: surface.querySelector('p') });
    const iframeContext = editorModule.createRichTextEditorToolContext(element, iframeState, iframeHost, iframeTool);
    iframeTool.bind(iframeContext);

    collapseAtEnd(surface.querySelector('p').firstChild);
    iframeTool.execute(iframeContext);
    click(element.querySelector('[data-role="iframe-cancel"]'));
    expect(iframeHost.updateToolbarState).toHaveBeenCalled();

    collapseAtEnd(surface.querySelector('p').firstChild);
    iframeTool.execute(iframeContext);
    // A same-origin source would make allow-same-origin a sandbox escape, so it is refused and nothing is inserted.
    element.querySelector('[data-role="iframe-url"]').value = '../embed';
    click(element.querySelector('[data-role="iframe-apply"]'));
    expect(surface.querySelector('iframe')).toBeNull();

    element.querySelector('[data-role="iframe-url"]').value = 'https://example.com/embed';
    click(element.querySelector('[data-role="iframe-apply"]'));
    expect(surface.querySelector('iframe').getAttribute('src')).toBe('https://example.com/embed');
    expect(surface.querySelector('iframe').getAttribute('sandbox')).toBe('allow-scripts allow-same-origin allow-popups allow-presentation');

    surface.innerHTML = '<p>Embed again</p>';
    iframeHost.setSelectionElement(surface.querySelector('p'));
    collapseAtEnd(surface.querySelector('p').firstChild);
    iframeTool.execute(iframeContext);
    element.querySelector('[data-role="iframe-url"]').value = 'example.com/embed';
    element.querySelector('[data-role="iframe-title"]').value = '';
    element.querySelector('[data-role="iframe-width"]').value = '';
    element.querySelector('[data-role="iframe-height"]').value = '';
    keydown(element.querySelector('[data-role="iframe-height"]'), 'Enter');
    const insertedIframe = surface.querySelector('iframe');
    expect(insertedIframe.getAttribute('src')).toBe('https://example.com/embed');
    expect(insertedIframe.getAttribute('title')).toBe('Embedded content');

    iframeTool.close(iframeContext);
    expect(iframeState.selectionRange).toBeNull();
    iframeTool.unbind(iframeContext);

    const iframeNoSurfaceState = createEditorState();
    const iframeNoSurfaceHost = createHost(element, surface, { surfaceOverride: null });
    const iframeNoSurfaceContext = editorModule.createRichTextEditorToolContext(element, iframeNoSurfaceState, iframeNoSurfaceHost, iframeTool);
    iframeTool.bind(iframeNoSurfaceContext);
    expect(iframeTool.execute(iframeNoSurfaceContext)).toBe(false);
    click(element.querySelector('[data-role="iframe-apply"]'));
  });

  test('text color tool covers direct runtime branches', () => {
    document.body.innerHTML = `
      <nt-rich-text-editor>
        <div data-tool-command="textColor" data-role="text-color-editor" hidden aria-hidden="true">
          <input data-role="text-color-value" type="text" />
          <button type="button" data-role="text-color-apply"></button>
          <button type="button" data-role="text-color-cancel"></button>
        </div>
        <div class="nt-rich-text-editor-surface" contenteditable="true">
          <p><span id="outer" style="color: rgb(170, 187, 204);"><span style="color:#aabbcc">Blue</span></span></p>
        </div>
      </nt-rich-text-editor>
    `;

    const element = document.querySelector('nt-rich-text-editor');
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    const outer = element.querySelector('#outer');
    const textColorTool = editorModule.getRichTextEditorTool('textColor');
    const textColorState = createEditorState();
    const textColorHost = createHost(element, surface, { selectionElement: outer });
    const textColorContext = editorModule.createRichTextEditorToolContext(element, textColorState, textColorHost, textColorTool);
    textColorTool.bind(textColorContext);
    textColorTool.setDisabled(textColorContext, true);
    textColorTool.setDisabled(textColorContext, false);

    selectText(outer.firstChild.firstChild, 0, 4);
    textColorTool.execute(textColorContext);
    expect(element.querySelector('[data-role="text-color-value"]').value).toBe('#aabbcc');
    click(element.querySelector('[data-role="text-color-cancel"]'));
    expect(textColorHost.updateToolbarState).toHaveBeenCalled();

    selectText(outer.firstChild.firstChild, 0, 4);
    textColorTool.execute(textColorContext);
    element.querySelector('[data-role="text-color-value"]').value = '#abc';
    click(element.querySelector('[data-role="text-color-apply"]'));
    expect(document.execCommand).toHaveBeenCalledWith('styleWithCSS', false, 'true');
    expect(document.execCommand).toHaveBeenCalledWith('foreColor', false, '#aabbcc');
    expect(document.execCommand).toHaveBeenLastCalledWith('styleWithCSS', false, 'false');

    surface.innerHTML = '<p>Plain text</p>';
    const detachedHost = document.createElement('div');
    detachedHost.innerHTML = '<p>Elsewhere</p>';
    document.body.appendChild(detachedHost);
    const detachedRange = document.createRange();
    detachedRange.setStart(detachedHost.querySelector('p').firstChild, 0);
    detachedRange.setEnd(detachedHost.querySelector('p').firstChild, 4);
    textColorHost.setSelectionElement(surface.querySelector('p'));
    textColorHost.setRestoredRange(detachedRange);
    textColorContext.toolState.textColorTarget = null;
    element.querySelector('[data-role="text-color-value"]').value = '#654321';
    click(element.querySelector('[data-role="text-color-apply"]'));
    expect(document.activeElement).toBe(element.querySelector('[data-role="text-color-value"]'));

    textColorTool.close(textColorContext);
    expect(textColorState.selectionRange).toBeNull();
    textColorTool.unbind(textColorContext);

    const textColorNoSurfaceState = createEditorState();
    const textColorNoSurfaceHost = createHost(element, surface, { surfaceOverride: null });
    const textColorNoSurfaceContext = editorModule.createRichTextEditorToolContext(element, textColorNoSurfaceState, textColorNoSurfaceHost, textColorTool);
    textColorTool.bind(textColorNoSurfaceContext);
    expect(textColorTool.execute(textColorNoSurfaceContext)).toBe(false);
    click(element.querySelector('[data-role="text-color-apply"]'));
  });
});
