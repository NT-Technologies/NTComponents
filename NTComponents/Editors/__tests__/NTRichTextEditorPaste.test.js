import { join } from 'path';
import { pathToFileURL } from 'url';
import { jest } from '@jest/globals';

const repoRoot = process.cwd();

async function importModule(relativePath) {
  return import(pathToFileURL(join(repoRoot, relativePath)).href);
}

const allCommands = ['heading:1', 'heading:2', 'alignCenter', 'unorderedList', 'orderedList', 'blockquote', 'codeBlock', 'table', 'bold', 'italic', 'underline', 'strikeThrough', 'textColor', 'link', 'ariaLabel', 'image', 'iframe'];

function createEditor(commands, editable = true) {
  const buttons = commands.map((entry) => {
    const [command, value] = entry.split(':');
    return `<button type="button" class="nt-rich-text-editor-toolbar-button" data-command="${command}"${value ? ` data-value="${value}"` : ''}></button>`;
  }).join('');
  document.body.innerHTML = `
    <nt-rich-text-editor data-editable="${editable}">
      <div class="nt-rich-text-editor-value" hidden></div>
      <div class="nt-rich-text-editor-toolbar">${buttons}</div>
      <div class="nt-rich-text-editor-surface" contenteditable="${editable}"></div>
      <textarea data-role="html-source" hidden></textarea>
    </nt-rich-text-editor>`;
  return document.querySelector('nt-rich-text-editor');
}

function transfer(element, type, property, data, files = []) {
  const event = new Event(type, { bubbles: true, cancelable: true });
  Object.defineProperty(event, property, {
    value: { types: [...Object.keys(data), ...(files.length ? ['Files'] : [])], getData: (format) => data[format] ?? '', files }
  });
  element.querySelector('.nt-rich-text-editor-surface').dispatchEvent(event);
  return event;
}

function paste(element, data, files) {
  return transfer(element, 'paste', 'clipboardData', data, files);
}

function drop(element, data, files) {
  return transfer(element, 'drop', 'dataTransfer', data, files);
}

function beforeInput(element, inputType) {
  const event = new InputEvent('beforeinput', { bubbles: true, cancelable: true, inputType });
  element.querySelector('.nt-rich-text-editor-surface').dispatchEvent(event);
  return event;
}

function insertedHtml() {
  const call = document.execCommand.mock.calls.find(([command]) => command === 'insertHTML');
  return call?.[2];
}

describe('NTRichTextEditor paste', () => {
  let editorModule;
  let originalExecCommand;

  beforeAll(async () => {
    editorModule = await importModule('NTComponents/Editors/NTRichTextEditor.razor.js');
  });

  beforeEach(() => {
    originalExecCommand = document.execCommand;
    document.execCommand = jest.fn(() => true);
  });

  afterEach(() => {
    const element = document.querySelector('nt-rich-text-editor');
    if (element) {
      editorModule.onDispose(element);
    }

    document.execCommand = originalExecCommand;
    document.body.innerHTML = '';
  });

  test('reduces pasted HTML to the formatting the toolbar can produce', () => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);

    const event = paste(element, {
      'text/html': '<h1 style="color:red;text-align:center">Title</h1><ul><li>Item</li></ul><table><tr><td>Cell</td></tr></table><p><b>Bold</b> <i>it</i> <a href="https://example.com">link</a> <span role="group" aria-label="x">grouped</span></p><img src="https://example.com/a.png"><script>alert(1)</script>'
    });

    expect(event.defaultPrevented).toBe(true);
    expect(insertedHtml()).toBe('<p>Title</p><p>Item</p><p>Cell</p><p><b>Bold</b> it link grouped</p>');
  });

  test('keeps pasted HTML the toolbar supports', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);

    paste(element, {
      'text/html': '<h2 style="text-align:center">Title</h2><h3>Too deep</h3><ul><li><a href="https://example.com" aria-label="Example">link</a></li></ul><p><span style="color:#ff0000">red</span> <s>gone</s></p><table><tbody><tr><td>Cell</td></tr></tbody></table>'
    });

    expect(insertedHtml()).toBe('<h2 style="text-align: center;">Title</h2><p>Too deep</p><ul><li><a href="https://example.com" aria-label="Example">link</a></li></ul><p><span style="color: rgb(255, 0, 0);">red</span> <s>gone</s></p><table><tbody><tr><td>Cell</td></tr></tbody></table>');
  });

  test('drops alignments and list types the toolbar lacks', () => {
    const element = createEditor(['alignCenter', 'unorderedList']);
    editorModule.onLoad(element, null);

    paste(element, { 'text/html': '<p style="text-align:right">Right</p><p style="text-align:center">Center</p><ol><li>One</li></ol><ul><li>Two</li></ul>' });

    expect(insertedHtml()).toBe('<p>Right</p><p style="text-align: center;">Center</p><p>One</p><ul><li>Two</li></ul>');
  });

  test('turns Google Docs inline styles into formatting tags', () => {
    const element = createEditor(['bold', 'italic', 'underline']);
    editorModule.onLoad(element, null);

    paste(element, {
      'text/html': '<b style="font-weight:normal" id="docs-internal-guid-1"><p><span style="font-weight:700">Bold</span> <span style="font-style:italic;text-decoration:underline">styled</span> plain</p></b>'
    });

    expect(insertedHtml()).toBe('<p><strong>Bold</strong> <u><em>styled</em></u> plain</p>');
  });

  test('drops the link color and underline Chrome copies from the page theme', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);

    paste(element, {
      'text/html': '<a href="https://example.com/" style="color: rgb(189, 194, 255); text-decoration: underline; text-underline-offset: 2px; font-family: Roboto; font-size: 16px; font-weight: 400; caret-color: rgb(189, 194, 255); white-space: normal; background-color: rgb(19, 19, 24);">reference link</a>',
      'text/plain': 'reference link'
    });

    expect(insertedHtml()).toBe('<a href="https://example.com/">reference link</a>');
  });

  test('keeps text color set inside a pasted link', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);

    paste(element, { 'text/html': '<a href="https://example.com/"><span style="color:#ff0000">red</span> link</a>' });

    expect(insertedHtml()).toBe('<a href="https://example.com/"><span style="color: rgb(255, 0, 0);">red</span> link</a>');
  });

  test('converts pasted markdown text and limits it to the toolbar', () => {
    const element = createEditor(['heading:1', 'bold', 'unorderedList']);
    editorModule.onLoad(element, null);

    paste(element, { 'text/plain': '# Title\n\n**Bold** and *italic*\n\n- one\n- two\n\n> quoted' });

    expect(insertedHtml()).toBe('<h1>Title</h1><p><strong>Bold</strong> and italic</p><ul><li>one</li><li>two</li></ul><p>quoted</p>');
  });

  test('reads VS Code copies as their plain text', () => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);

    paste(element, {
      'text/html': '<div style="color:#d4d4d4"><span>**bold**</span></div>',
      'text/plain': '**bold**',
      'vscode-editor-data': '{}'
    });

    expect(insertedHtml()).toBe('<p><strong>bold</strong></p>');
  });

  test('renders pasted HTML source but keeps other angle brackets as text', () => {
    const element = createEditor(['italic']);
    editorModule.onLoad(element, null);

    paste(element, { 'text/plain': '<p>Hello <em>there</em><script>alert(1)</script></p>' });
    expect(insertedHtml()).toBe('<p>Hello <em>there</em></p>');

    document.execCommand.mockClear();
    paste(element, { 'text/plain': 'List<int> uses <T>' });
    expect(insertedHtml()).toBe('<p>List&lt;int&gt; uses &lt;T&gt;</p>');
  });

  test('uses the markup of a copied image rather than its file', () => {
    const element = createEditor(['image']);
    editorModule.onLoad(element, null);

    paste(element, { 'text/html': '<img src="https://example.com/a.png" alt="a">' }, [new File(['png'], 'a.png', { type: 'image/png' })]);
    expect(insertedHtml()).toBe('<img src="https://example.com/a.png" alt="a">');
  });

  test('keeps the iframe sandbox when attribute values look like a comment', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);

    paste(element, { 'text/html': '<iframe src="https://example.com/" title="<!--"></iframe><a href="https://example.com/" title="-->">x</a><!-- note -->' });
    const template = document.createElement('template');
    template.innerHTML = insertedHtml();
    expect(template.content.querySelector('iframe').hasAttribute('sandbox')).toBe(true);
    expect(insertedHtml()).not.toContain('note');
  });

  test('keeps very long pasted text as plain text instead of converting it', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);

    const text = `${'*a '.repeat(20000)}\nnext`;
    paste(element, { 'text/plain': text });
    expect(insertedHtml()).toBe(`<p>${'*a '.repeat(20000)}<br>next</p>`);
  });

  test.each([['pasted', paste], ['dropped', drop]])('ignores %s files no toolbar tool takes', async (_, send) => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);
    document.execCommand.mockClear();
    expect(send(element, {}, [new File(['png'], 'diagram.png', { type: 'image/png' })]).defaultPrevented).toBe(true);
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(insertedHtml()).toBeUndefined();
  });

  test('reduces content dropped from outside the editor to the toolbar', () => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);

    const event = drop(element, { 'text/html': '<h1>Title</h1><p><b>Bold</b> <i>it</i></p>' });

    expect(event.defaultPrevented).toBe(true);
    expect(insertedHtml()).toBe('<p>Title</p><p><b>Bold</b> it</p>');
  });

  test('leaves a drag within the editor to the browser', () => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    const dragged = {};
    const start = new Event('dragstart', { bubbles: true });
    Object.defineProperty(start, 'dataTransfer', { value: { setData: (format, value) => { dragged[format] = value; } } });
    surface.dispatchEvent(start);

    const moved = drop(element, { 'text/html': '<b>moved</b>', ...dragged });
    // Content dragged from another editor, or from outside, is cleaned even if a drag here never ended.
    const fromOtherEditor = drop(element, { 'text/html': '<i>other</i>', 'application/x-nt-rich-text-editor': 'other' });
    const dropped = drop(element, { 'text/plain': 'outside' });

    expect(moved.defaultPrevented).toBe(false);
    expect(fromOtherEditor.defaultPrevented).toBe(true);
    expect(dropped.defaultPrevented).toBe(true);
    expect(document.execCommand.mock.calls.filter(([command]) => command === 'insertHTML')).toEqual([['insertHTML', false, 'other'], ['insertHTML', false, '<p>outside</p>']]);
  });

  test('refuses files dragged over a read-only editor so the browser does not open them', () => {
    const dragOver = (element) => {
      const event = new Event('dragover', { bubbles: true, cancelable: true });
      Object.defineProperty(event, 'dataTransfer', { value: { types: ['Files'], dropEffect: 'copy' } });
      element.querySelector('.nt-rich-text-editor-surface').dispatchEvent(event);
      return event;
    };
    const readOnly = createEditor(['image'], false);
    editorModule.onLoad(readOnly, null);
    const refused = dragOver(readOnly);
    expect(refused.defaultPrevented).toBe(true);
    expect(refused.dataTransfer.dropEffect).toBe('none');

    editorModule.onDispose(readOnly);
    const editable = createEditor(['image']);
    editorModule.onLoad(editable, null);
    expect(dragOver(editable).defaultPrevented).toBe(false);
  });

  test('blocks native formatting the toolbar does not offer', () => {
    const element = createEditor(['bold', 'alignCenter']);
    editorModule.onLoad(element, null);

    expect(beforeInput(element, 'formatBold').defaultPrevented).toBe(false);
    expect(beforeInput(element, 'formatJustifyCenter').defaultPrevented).toBe(false);
    expect(beforeInput(element, 'formatItalic').defaultPrevented).toBe(true);
    expect(beforeInput(element, 'formatUnderline').defaultPrevented).toBe(true);
    expect(beforeInput(element, 'formatSuperscript').defaultPrevented).toBe(true);
    expect(beforeInput(element, 'formatOutdent').defaultPrevented).toBe(false);
    expect(beforeInput(element, 'insertText').defaultPrevented).toBe(false);
  });

  test('allows native indenting only where it nests a list item or the toolbar offers quotes', () => {
    const element = createEditor(['unorderedList']);
    editorModule.onLoad(element, null);
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    surface.innerHTML = '<p>text</p><ul><li>item</li></ul>';
    const caretIn = (node) => {
      const range = document.createRange();
      range.setStart(node.firstChild, 1);
      window.getSelection().removeAllRanges();
      window.getSelection().addRange(range);
    };

    caretIn(surface.querySelector('p'));
    expect(beforeInput(element, 'formatIndent').defaultPrevented).toBe(true);
    caretIn(surface.querySelector('li'));
    expect(beforeInput(element, 'formatIndent').defaultPrevented).toBe(false);

    editorModule.onDispose(element);
    const quoteEditor = createEditor(['blockquote']);
    editorModule.onLoad(quoteEditor, null);
    expect(beforeInput(quoteEditor, 'formatIndent').defaultPrevented).toBe(false);
  });

  test('pastes text into a code block as it was copied', () => {
    const element = createEditor(allCommands);
    editorModule.onLoad(element, null);
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    surface.innerHTML = '<pre><code>x</code></pre>';
    const range = document.createRange();
    range.setStart(surface.querySelector('code').firstChild, 1);
    window.getSelection().removeAllRanges();
    window.getSelection().addRange(range);

    paste(element, { 'text/html': '<b>ignored</b>', 'text/plain': '# setup\r\n    def __init__(self):\r\n        x = a*b*c <T>' });
    expect(insertedHtml()).toBe('# setup\n    def __init__(self):\n        x = a*b*c &lt;T&gt;');
  });

  test('keeps the lines of code pasted into an editor without code blocks', () => {
    const element = createEditor(['bold']);
    editorModule.onLoad(element, null);

    paste(element, { 'text/plain': '```\nfirst()\nsecond()\n```' });

    expect(insertedHtml()).toBe('<p>first()<br>second()</p>');
  });

  test('holds HTML source edits to the toolbar', () => {
    const element = createEditor(['bold', 'editHtml']);
    editorModule.onLoad(element, null);
    element.querySelector('[data-command="editHtml"]').click();

    const source = element.querySelector('[data-role="html-source"]');
    source.value = '<h2>Title</h2><p><b>Bold</b> <u>under</u></p>';
    source.dispatchEvent(new Event('input', { bubbles: true }));

    expect(element.querySelector('.nt-rich-text-editor-surface').innerHTML).toBe('<p>Title</p><p><b>Bold</b> under</p>');
  });

  test('leaves the value alone when the HTML source is opened and closed without edits', () => {
    const element = createEditor(['bold', 'editHtml']);
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    element.querySelector('.nt-rich-text-editor-value').textContent = '<h3>Notes</h3><p><u>under</u></p>';
    editorModule.onLoad(element, null);
    expect(surface.innerHTML).toBe('<h3>Notes</h3><p><u>under</u></p>');
    const toggle = element.querySelector('[data-command="editHtml"]');

    toggle.click();
    toggle.click();

    expect(surface.innerHTML).toBe('<h3>Notes</h3><p><u>under</u></p>');
  });

  test('keeps formatting the value already had when its HTML source is edited', () => {
    const element = createEditor(['bold', 'editHtml']);
    const surface = element.querySelector('.nt-rich-text-editor-surface');
    element.querySelector('.nt-rich-text-editor-value').textContent = '<h3 style="text-align: center;">Notes</h3><p><u>under</u></p>';
    editorModule.onLoad(element, null);
    element.querySelector('[data-command="editHtml"]').click();

    const source = element.querySelector('[data-role="html-source"]');
    source.value = '<h3 style="text-align: center;">Notes</h3><p><u>under</u> <i>new</i></p><h2 style="text-align: right;">Added</h2>';
    source.dispatchEvent(new Event('input', { bubbles: true }));

    expect(surface.innerHTML).toBe('<h3 style="text-align: center;">Notes</h3><p><u>under</u> new</p><p>Added</p>');
  });

  test('keeps the tags a tool plugin allows while its button is on the toolbar', () => {
    editorModule.registerRichTextEditorTool({ command: 'inlineCode', allowedTags: ['code'], createState: () => ({}), bind: () => { }, unbind: () => { } });
    const element = createEditor(['inlineCode']);
    editorModule.onLoad(element, null);
    paste(element, { 'text/html': '<p><code>x</code> <b>y</b></p>' });
    expect(insertedHtml()).toBe('<p><code>x</code> y</p>');

    editorModule.onDispose(element);
    document.execCommand.mockClear();
    const withoutTool = createEditor(['bold']);
    editorModule.onLoad(withoutTool, null);
    paste(withoutTool, { 'text/html': '<p><code>x</code> <b>y</b></p>' });
    expect(insertedHtml()).toBe('<p>x <b>y</b></p>');
  });

  test('leaves a read-only editor alone', () => {
    const element = createEditor(['bold'], false);
    editorModule.onLoad(element, null);

    const event = paste(element, { 'text/plain': 'text' });

    expect(event.defaultPrevented).toBe(false);
    expect(document.execCommand).not.toHaveBeenCalledWith('insertHTML', false, expect.anything());
  });
});

describe('markdownToHtml', () => {
  let markdownToHtml;

  beforeAll(async () => {
    ({ markdownToHtml } = (await importModule('NTComponents/Editors/NTRichTextEditor.razor.js')).__testHooks);
  });

  test.each([
    ['# Title #', '<h1>Title</h1>'],
    ['Line one\nLine two\n\nNext', '<p>Line one<br>Line two</p><p>Next</p>'],
    ['**b** *i* _e_ ~~s~~ `c*d` snake_case_name', '<p><strong>b</strong> <em>i</em> <em>e</em> <del>s</del> <code>c*d</code> snake_case_name</p>'],
    ['\\*literal\\* 2 * 3', '<p>*literal* 2 * 3</p>'],
    ['__b__ my__var__name', '<p><strong>b</strong> my__var__name</p>'],
    ['[a](https://x.com/a_b_c "T") ![alt](https://x.com/i.png) <https://y.com>', '<p><a href="https://x.com/a_b_c" title="T">a</a> <img src="https://x.com/i.png" alt="alt"> <a href="https://y.com">https://y.com</a></p>'],
    ['<b>raw</b> & "quotes"', '<p>&lt;b&gt;raw&lt;/b&gt; &amp; &quot;quotes&quot;</p>'],
    ['- a\n- b\n  - c\n\n  more\n- d\n\n1. x\n2. y', '<ul><li>a</li><li>b<ul><li>c</li></ul><p>more</p></li><li>d</li></ul><ol><li>x</li><li>y</li></ol>'],
    ['Intro\n- item', '<p>Intro</p><ul><li>item</li></ul>'],
    ['> one\n> # two', '<blockquote><p>one</p><h1>two</h1></blockquote>'],
    ['```js\nif (a < b) {\n  go();\n}\n```\nafter', '<pre>if (a &lt; b) {\n  go();\n}</pre><p>after</p>'],
    ['```inline``` text', '<p><code>inline</code> text</p>'],
    ['| A | B |\n| --- | :-: |\n| 1 | 2 \\| 3 |\nafter', '<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>1</td><td>2 | 3</td></tr></tbody></table><p>after</p>'],
    ['a\n\n---\n\n* * *\nb', '<p>a</p><p>b</p>'],
    ['\r\n\r\n', '']
  ])('converts %j', (markdown, html) => {
    expect(markdownToHtml(markdown)).toBe(html);
  });
});
