let counter = 0;

export function pickerMarkup({ showAlpha = false, value = '', embedded = false, disabled = false } = {}) {
    const rgbFields = ['r', 'g', 'b'].map(key => `<label data-nt-color-fieldset="rgb"><span>${key.toUpperCase()}</span><input type="text" data-nt-color-field="${key}"></label>`).join('');
    const hslFields = ['h', 's', 'l'].map(key => `<label data-nt-color-fieldset="hsl"><span>${key.toUpperCase()}</span><input type="text" data-nt-color-field="${key}"></label>`).join('');
    return `
<div class="nt-color-picker" data-nt-color-picker="true" data-value="${value}" data-show-alpha="${showAlpha}"${disabled ? ' data-disabled="true"' : ''}${embedded ? ' data-nt-color-embedded="true"' : ''} role="group" aria-label="Color picker">
    <div class="nt-color-area" role="slider" tabindex="0" aria-label="Saturation and brightness" data-nt-color-area="true"><span class="nt-color-area-thumb"></span></div>
    <input type="range" min="0" max="360" step="1" value="0" aria-label="Hue" data-nt-color-hue="true">
    ${showAlpha ? '<input type="range" min="0" max="100" step="1" value="100" aria-label="Opacity" data-nt-color-alpha="true">' : ''}
    <button type="button" aria-pressed="true" data-nt-color-format="hex">Hex</button>
    <button type="button" aria-pressed="false" data-nt-color-format="rgb">RGB</button>
    <button type="button" aria-pressed="false" data-nt-color-format="hsl">HSL</button>
    <label data-nt-color-fieldset="hex"><span>Hex</span><input type="text" data-nt-color-field="hex"></label>
    ${rgbFields}${hslFields}
    ${showAlpha ? '<label data-nt-color-fieldset="rgb hsl"><span>A</span><input type="text" data-nt-color-field="a"></label>' : ''}
    <input type="hidden" name="color" value="${value}" data-nt-color-value="true">
</div>`;
}

export function mountPicker(options = {}) {
    const host = document.createElement('div');
    host.innerHTML = pickerMarkup(options);
    document.body.append(host);
    return host.firstElementChild;
}

export function inputColorMarkup({ value = '', showAlpha = false, disabled = false, readOnly = false } = {}) {
    const id = `nt-input-color-test-${++counter}`;
    return `
<div class="nt-input-color" data-nt-input-color="true" data-show-alpha="${showAlpha}">
    <label for="${id}"><span data-nt-input-color-swatch="true"></span><input id="${id}" type="text" value="${value}" data-nt-input-color-input="true"${disabled ? ' disabled' : ''}${readOnly ? ' readonly' : ''}></label>
    <button type="button" aria-label="Open color picker" aria-expanded="false" data-nt-input-color-trigger="true"${disabled || readOnly ? ' disabled' : ''}></button>
    <div popover="auto" data-nt-input-color-popover="true">
        ${pickerMarkup({ showAlpha, value, embedded: true })}
        <button type="button" data-nt-input-color-action="cancel">Cancel</button>
        <button type="button" data-nt-input-color-action="confirm">OK</button>
    </div>
    <nt-page-script></nt-page-script>
</div>`;
}

export function mountInputColor(options = {}) {
    const host = document.createElement('div');
    host.innerHTML = inputColorMarkup(options);
    document.body.append(host);
    return host.firstElementChild;
}
