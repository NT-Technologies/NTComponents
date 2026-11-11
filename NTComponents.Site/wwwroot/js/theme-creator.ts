import {
  Blend,
  DynamicScheme,
  Hct,
  TonalPalette,
  Variant,
  argbFromHex,
  blueFromArgb,
  greenFromArgb,
  hexFromArgb,
  redFromArgb,
} from "@material/material-color-utilities";

interface ThemeGenerationRequest {
  primary: string;
  primaryOverride?: string;
  error?: string;
  secondary?: string;
  tertiary?: string;
  neutral?: string;
  neutralVariant?: string;
  variant: string;
  harmonizeExtendedColors: boolean;
  extendedColors: Record<string, string>;
}

interface GeneratedThemeFile {
  name: string;
  content: string;
  tokenCount: number;
  properties: Record<string, string>;
}

interface PreviewState {
  animationFrame?: number;
  // Set once the user edits a color, so merely opening the page never recolors the site.
  applyToSite: boolean;
  changeHandler: (event: Event) => void;
  // The picker whose native color dialog is open. The preview leaves its value alone, so the dialog does not jump while the user drags.
  editing?: HTMLInputElement;
  fileName?: string;
  inputHandler: (event: Event) => void;
  request?: ThemeGenerationRequest;
  root: HTMLElement;
  target: HTMLElement;
}

const systemColorRoles = [
  "primary", "surfaceTint", "onPrimary", "primaryContainer", "onPrimaryContainer",
  "secondary", "onSecondary", "secondaryContainer", "onSecondaryContainer",
  "tertiary", "onTertiary", "tertiaryContainer", "onTertiaryContainer",
  "error", "onError", "errorContainer", "onErrorContainer", "background", "onBackground",
  "surface", "onSurface", "surfaceVariant", "onSurfaceVariant", "outline", "outlineVariant",
  "shadow", "scrim", "inverseSurface", "inverseOnSurface", "inversePrimary", "primaryFixed",
  "onPrimaryFixed", "primaryFixedDim", "onPrimaryFixedVariant", "secondaryFixed", "onSecondaryFixed",
  "secondaryFixedDim", "onSecondaryFixedVariant", "tertiaryFixed", "onTertiaryFixed", "tertiaryFixedDim",
  "onTertiaryFixedVariant", "surfaceDim", "surfaceBright", "surfaceContainerLowest", "surfaceContainerLow",
  "surfaceContainer", "surfaceContainerHigh", "surfaceContainerHighest",
] as const;

const variants: Record<string, Variant> = {
  content: Variant.CONTENT,
  expressive: Variant.EXPRESSIVE,
  fidelity: Variant.FIDELITY,
  "fruit-salad": Variant.FRUIT_SALAD,
  monochrome: Variant.MONOCHROME,
  neutral: Variant.NEUTRAL,
  rainbow: Variant.RAINBOW,
  "tonal-spot": Variant.TONAL_SPOT,
  vibrant: Variant.VIBRANT,
};

const schemeDefinitions = [
  { name: "light.css", isDark: false, contrastLevel: 0 },
  { name: "light-mc.css", isDark: false, contrastLevel: 0.5 },
  { name: "light-hc.css", isDark: false, contrastLevel: 1 },
  { name: "dark.css", isDark: true, contrastLevel: 0 },
  { name: "dark-mc.css", isDark: true, contrastLevel: 0.5 },
  { name: "dark-hc.css", isDark: true, contrastLevel: 1 },
] as const;

// Role palettes show the role color rendered in the preview.
const palettePreviewRoles: Record<string, string> = {
  error: "--nt-color-error",
  primary: "--nt-color-primary",
  secondary: "--nt-color-secondary",
  tertiary: "--nt-color-tertiary",
};

// Surface tones are close to white or black, which hides the chosen tint and makes a poor starting point in the color dialog, so the
// surface palettes show their key color instead.
const paletteKeyColors = {
  neutral: "neutralPalette",
  "neutral-variant": "neutralVariantPalette",
} as const;

let previewState: PreviewState | undefined;
let originalPreviewProperties: Map<string, string> | undefined;
// The creator's colors also recolor the whole site for this page session; a hard reload or resetSiteTheme removes them.
let siteTheme: { request: ThemeGenerationRequest; style: HTMLStyleElement; observer: MutationObserver } | undefined;

export function generateTheme(request: ThemeGenerationRequest): GeneratedThemeFile[] {
  return schemeDefinitions.map((definition) => generateThemeFile(request, definition));
}

export function initializeThemePreview(rootId: string, targetId: string): void {
  clearThemePreview();
  const root = document.getElementById(rootId);
  const target = document.getElementById(targetId);
  if (!root || !target) {
    throw new Error("Could not initialize the Material theme preview.");
  }

  const inputHandler = (event: Event): void => {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || input.type !== "color" || !input.dataset.themeColorKey) {
      return;
    }

    updatePickerDisplay(input);
    if (!previewState?.request) {
      return;
    }

    previewState.editing = input;
    updateRequestColor(previewState.request, input.dataset.themeColorKey, input.value);
    previewState.applyToSite = true;
    schedulePreview();
  };
  // A color input raises change when its dialog closes; from then on the picker follows the preview again.
  const changeHandler = (event: Event): void => {
    if (previewState && event.target === previewState.editing) {
      previewState.editing = undefined;
      schedulePreview();
    }
  };
  root.addEventListener("input", inputHandler);
  root.addEventListener("change", changeHandler);
  previewState = { applyToSite: false, changeHandler, inputHandler, root, target };
}

export function updateThemePreview(request: ThemeGenerationRequest, fileName: string): void {
  if (!previewState) {
    throw new Error("The Material theme preview has not been initialized.");
  }

  // The first update is the page's initial generation; later ones come from the user's edits.
  previewState.applyToSite ||= previewState.request !== undefined;
  previewState.request = cloneRequest(request);
  previewState.fileName = fileName;
  applyPreview();
}

export function selectThemePreview(fileName: string): void {
  if (!previewState) {
    return;
  }

  previewState.fileName = fileName;
  applyPreview();
}

export function clearThemePreview(): void {
  if (!previewState) {
    return;
  }

  if (previewState.animationFrame !== undefined) {
    cancelAnimationFrame(previewState.animationFrame);
  }
  previewState.root.removeEventListener("input", previewState.inputHandler);
  previewState.root.removeEventListener("change", previewState.changeHandler);
  restorePreviewProperties(previewState.target);
  previewState = undefined;
}

function createScheme(request: ThemeGenerationRequest, definition: typeof schemeDefinitions[number]): DynamicScheme {
  const sourceColorHct = Hct.fromInt(parseColor(request.primary, "primary"));
  const variant = variants[request.variant];
  if (variant === undefined) {
    throw new Error(`Unsupported theme style: ${request.variant}.`);
  }

  const paletteOverrides = {
    ...optionalPalette("primaryPalette", request.primaryOverride),
    ...optionalPalette("secondaryPalette", request.secondary),
    ...optionalPalette("tertiaryPalette", request.tertiary),
    ...optionalPalette("neutralPalette", request.neutral),
    ...optionalPalette("neutralVariantPalette", request.neutralVariant),
    ...optionalPalette("errorPalette", request.error),
  };

  return new DynamicScheme({
    sourceColorHct,
    variant,
    contrastLevel: definition.contrastLevel,
    isDark: definition.isDark,
    ...paletteOverrides,
  });
}

function generateThemeFile(request: ThemeGenerationRequest, definition: typeof schemeDefinitions[number], scheme = createScheme(request, definition)): GeneratedThemeFile {
  const sourceArgb = scheme.sourceColorArgb;
  const properties: Record<string, string> = {};

  for (const role of systemColorRoles) {
    properties[`--nt-color-${toKebabCase(role)}`] = toRgb(scheme[role]);
  }

  for (const [name, color] of Object.entries(request.extendedColors)) {
    const extendedArgb = parseColor(color, name);
    const paletteArgb = request.harmonizeExtendedColors ? Blend.harmonize(extendedArgb, sourceArgb) : extendedArgb;
    const extendedScheme = new DynamicScheme({
      sourceColorHct: Hct.fromInt(paletteArgb),
      variant: Variant.TONAL_SPOT,
      contrastLevel: definition.contrastLevel,
      isDark: definition.isDark,
      primaryPalette: TonalPalette.fromInt(paletteArgb),
    });
    properties[`--nt-color-${name}`] = toRgb(extendedScheme.primary);
    properties[`--nt-color-on-${name}`] = toRgb(extendedScheme.onPrimary);
    properties[`--nt-color-${name}-container`] = toRgb(extendedScheme.primaryContainer);
    properties[`--nt-color-on-${name}-container`] = toRgb(extendedScheme.onPrimaryContainer);
  }

  return {
    name: definition.name,
    content: toCss(properties),
    tokenCount: Object.keys(properties).length,
    properties,
  };
}

function applyPreview(): void {
  if (!previewState?.request || !previewState.fileName) {
    return;
  }

  const definition = schemeDefinitions.find((candidate) => candidate.name === previewState?.fileName);
  if (!definition) {
    throw new Error(`Unsupported preview scheme: ${previewState.fileName}.`);
  }

  const scheme = createScheme(previewState.request, definition);
  const properties = generateThemeFile(previewState.request, definition, scheme).properties;
  const targetStyle = previewState.target.style;
  if (!originalPreviewProperties) {
    originalPreviewProperties = new Map(Object.keys(properties).map((name) => [name, targetStyle.getPropertyValue(name)]));
  }

  for (const [name, value] of Object.entries(properties)) {
    targetStyle.setProperty(name, value);
  }
  syncPalettePickerDisplays(properties, scheme);
  if (previewState.applyToSite) {
    setSiteTheme(previewState.request);
  }
}

function setSiteTheme(request: ThemeGenerationRequest): void {
  if (!siteTheme) {
    const style = document.createElement("style");
    style.id = "docs-site-theme";
    document.head.appendChild(style);
    // Follow the header's light/dark and contrast choice, which swaps the active theme stylesheet.
    const observer = new MutationObserver(renderSiteTheme);
    observer.observe(document.head, { subtree: true, childList: true, attributes: true, attributeFilter: ["href", "data-nt-theme-loaded"] });
    siteTheme = { request, style, observer };
  }

  siteTheme.request = request;
  renderSiteTheme();
}

function renderSiteTheme(): void {
  if (!siteTheme) {
    return;
  }

  const href = document.querySelector('link[data-nt-theme][data-nt-theme-loaded="true"]')?.getAttribute("href") ?? "";
  const activeFile = href.split("?")[0].split("/").pop();
  const definition = schemeDefinitions.find((candidate) => candidate.name === activeFile) ?? schemeDefinitions[0];
  const properties = generateThemeFile(siteTheme.request, definition).properties;
  // The doubled :root outranks the theme stylesheets regardless of load order.
  const css = `:root:root{${Object.entries(properties).map(([name, value]) => `${name}:${value}`).join(";")}}`;
  // Only write on change: the head observer would otherwise re-trigger on this element's own update.
  if (siteTheme.style.textContent !== css) {
    siteTheme.style.textContent = css;
  }
}

export function resetSiteTheme(): void {
  siteTheme?.observer.disconnect();
  siteTheme?.style.remove();
  siteTheme = undefined;
}

function restorePreviewProperties(target: HTMLElement): void {
  if (!originalPreviewProperties) {
    return;
  }

  for (const [name, value] of originalPreviewProperties) {
    if (value) {
      target.style.setProperty(name, value);
    } else {
      target.style.removeProperty(name);
    }
  }

  originalPreviewProperties = undefined;
}

function schedulePreview(): void {
  if (!previewState || previewState.animationFrame !== undefined) {
    return;
  }

  previewState.animationFrame = requestAnimationFrame(() => {
    if (previewState) {
      previewState.animationFrame = undefined;
      applyPreview();
    }
  });
}

function cloneRequest(request: ThemeGenerationRequest): ThemeGenerationRequest {
  return { ...request, extendedColors: { ...request.extendedColors } };
}

function updateRequestColor(request: ThemeGenerationRequest, key: string, color: string): void {
  if (key === "primary") {
    request.primary = color;
    return;
  }

  const [group, name] = key.split(".", 2);
  if (group === "extended" && name) {
    request.extendedColors[name] = color;
    return;
  }

  if (group !== "palette" || !name) {
    return;
  }

  const property = name === "primary" ? "primaryOverride" : name === "neutral-variant" ? "neutralVariant" : name;
  if (property === "primaryOverride" || property === "secondary" || property === "tertiary" || property === "neutral" || property === "neutralVariant" || property === "error") {
    request[property] = color;
  }
}

function updatePickerDisplay(input: HTMLInputElement): void {
  const picker = input.closest<HTMLElement>("[data-theme-color-picker]");
  picker?.style.setProperty("--theme-picker-color", input.value);
  const output = picker?.querySelector<HTMLOutputElement>("[data-theme-color-output]");
  if (output) {
    output.value = input.value.toUpperCase();
  }
}

function syncPalettePickerDisplays(properties: Record<string, string>, scheme: DynamicScheme): void {
  if (!previewState) {
    return;
  }

  for (const input of previewState.root.querySelectorAll<HTMLInputElement>('input[type="color"][data-theme-color-key^="palette."]')) {
    if (input === previewState.editing) {
      continue;
    }

    const palette = input.dataset.themeColorKey?.slice("palette.".length) ?? "";
    const keyPalette = paletteKeyColors[palette as keyof typeof paletteKeyColors];
    const color = keyPalette ? hexFromArgb(scheme[keyPalette].keyColor.toInt()) : toHex(properties[palettePreviewRoles[palette]]);
    if (color) {
      input.value = color;
      updatePickerDisplay(input);
    }
  }
}

function optionalPalette(name: string, color?: string): Record<string, TonalPalette> {
  return color ? { [name]: TonalPalette.fromInt(parseColor(color, name)) } : {};
}

function parseColor(color: string, name: string): number {
  if (!/^#[0-9a-f]{6}$/i.test(color)) {
    throw new Error(`${name} must be a six-digit hexadecimal color.`);
  }

  return argbFromHex(color);
}

function toRgb(argb: number): string {
  return `rgb(${redFromArgb(argb)} ${greenFromArgb(argb)} ${blueFromArgb(argb)})`;
}

function toHex(rgb: string | undefined): string | undefined {
  const channels = rgb?.match(/^rgb\((\d+) (\d+) (\d+)\)$/);
  return channels ? `#${channels.slice(1).map((channel) => Number(channel).toString(16).padStart(2, "0")).join("")}` : undefined;
}

function toKebabCase(value: string): string {
  return value.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`);
}

function toCss(properties: Record<string, string>): string {
  const declarations = Object.entries(properties).map(([name, value]) => `    ${name}: ${value};`).join("\r\n");
  return `:root {\r\n${declarations}\r\n}\r\n`;
}
