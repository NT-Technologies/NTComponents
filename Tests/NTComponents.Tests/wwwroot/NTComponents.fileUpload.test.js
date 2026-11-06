// Importing the module attaches the static file-upload change listener.
import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

function renderUpload({ isStatic = true } = {}) {
    document.body.innerHTML = `
        <div class="nt-input nt-file-upload">
            <span class="nt-file-upload-value">No image selected</span>
            <input type="file" ${isStatic ? 'data-nt-file-upload-static="true"' : ''} />
        </div>`;
    return {
        input: document.querySelector('input'),
        display: document.querySelector('.nt-file-upload-value')
    };
}

function choose(input, names) {
    Object.defineProperty(input, 'files', { configurable: true, value: names.map((name) => new File(['x'], name)) });
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

describe('static NTFileUpload file names', () => {
    test('shows the chosen file names', () => {
        const { input, display } = renderUpload();

        choose(input, ['a.png', 'b.png']);

        expect(display.textContent).toBe('a.png, b.png');
    });

    test('restores the empty text when the selection is cleared', () => {
        const { input, display } = renderUpload();
        choose(input, ['a.png']);

        choose(input, []);

        expect(display.textContent).toBe('No image selected');
    });

    test('leaves interactive (Blazor-rendered) uploads alone', () => {
        const { input, display } = renderUpload({ isStatic: false });

        choose(input, ['a.png']);

        expect(display.textContent).toBe('No image selected');
    });
});
