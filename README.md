# NTComponents

[![PR Build](https://github.com/NT-Technologies/NTComponents/actions/workflows/pr-build.yml/badge.svg)](https://github.com/NT-Technologies/NTComponents/actions/workflows/pr-build.yml)
[![AOT Compatibility](https://github.com/NT-Technologies/NTComponents/actions/workflows/ensure-aot-build.yml/badge.svg)](https://github.com/NT-Technologies/NTComponents/actions/workflows/ensure-aot-build.yml)

NTComponents is a Material 3 inspired Blazor component library for .NET 10 and .NET 11 applications. The current component model is `NT*` first, with source-generated documentation metadata, render-compatibility guidance, static web assets, scoped CSS, optional JavaScript enhancement, and analyzer coverage for component usage rules.

The package is designed for Blazor Web Apps, Blazor WebAssembly, interactive render modes, and static SSR scenarios where components can render useful HTML before browser enhancement.

## Documentation And MCP

- [NTComponents documentation](https://ntcomponents.nttechnologies.dev/) — generated component guides, live examples, API members, render-mode compatibility, enums, and constants.
- [NTComponents MCP server](https://mcp.ntcomponents.nttechnologies.dev/mcp) — read-only Streamable HTTP endpoint for discovering and querying the same component documentation from MCP clients.
- [MCP service discovery](https://mcp.ntcomponents.nttechnologies.dev/) — current catalog counts plus links to MCP, REST, OpenAPI, and health endpoints.
- [OpenAPI document](https://mcp.ntcomponents.nttechnologies.dev/openapi/v1.json) — machine-readable contract for the documentation REST API.

Connect an MCP client that supports Streamable HTTP to:

```text
https://mcp.ntcomponents.nttechnologies.dev/mcp
```

No API key, authorization header, or local server process is required.

### Connect From Codex

Add the server to your Codex user configuration:

```powershell
codex mcp add ntcomponents --url https://mcp.ntcomponents.nttechnologies.dev/mcp
codex mcp list
```

Start a new Codex session after adding it so the NTComponents tools are available.

### Connect From Claude Code

Add the server with the remote HTTP transport. Use `--scope user` to make it available in every project, or omit that option to keep it local to the current project:

```powershell
claude mcp add --transport http --scope user ntcomponents https://mcp.ntcomponents.nttechnologies.dev/mcp
claude mcp list
```

### Connect From Visual Studio Code

Run **MCP: Open User Configuration** from the Command Palette, or create `.vscode/mcp.json` to share the connection with a workspace, then add:

```json
{
  "servers": {
    "ntcomponents": {
      "type": "http",
      "url": "https://mcp.ntcomponents.nttechnologies.dev/mcp"
    }
  }
}
```

Start the server from **MCP: List Servers** if it does not start automatically. You can verify any connection by asking the client to search the NTComponents documentation for a component such as `NTDataGrid`.

The public MCP service is anonymous and read-only. It exposes tools for searching the NTComponents catalog, reading component details, and retrieving generated documentation without cloning this repository. Client requests are rate-limited; applications should cache stable documentation results and retry `429 Too Many Requests` responses using the returned `Retry-After` value.

## Quick Getting Started

### 1. Install

```powershell
dotnet add package NTComponents
```

### 2. Add the namespace

Add the component namespace to your app's `_Imports.razor`:

```razor
@using NTComponents
```

### 3. Register services

Register the library services in `Program.cs`:

```csharp
builder.Services.AddNTServices();
```

`AddNTServices()` registers the NT snackbar, toast, dialog, local storage, session storage, and default option services used by the component library.

### 4. Add head dependencies

Place `NTHeadDependencies` in the document head and include the package scoped CSS:

```razor
<head>
    <NTHeadDependencies />
</head>
```

`NTHeadDependencies` emits theme bootstrap scripts, first-paint theme links, measurement tokens, ripple styles, Roboto, Material Symbols fonts, and the anchor-positioning polyfill hook.

### 5. Add app-level hosts

Add app-level hosts once near the route or layout level when you use these services:

```razor
<NTToast />
<NTSnackbar />
```

`NTToast` and `NTSnackbar` register browser bridges so both interactive Blazor code and static markup can trigger feedback. `NTThemeToggle` works with the theme runtime and the theme CSS files under `/Themes` by default.

### 6. Use components

```razor
<NTCard>
    <h2>Account</h2>
    <p>Review the account settings before saving.</p>
    <NTButton Label="Save" Variant="NTButtonVariant.Filled" OnClickCallback="@(async _ => await SaveAsync())" />
</NTCard>

@code {
    private Task SaveAsync()
    {
        return Task.CompletedTask;
    }
}
```

## Component Model

NTComponents uses the `NT*` API surface for new component work. Some legacy `TnT*` components and services remain for compatibility, but new code should prefer `NTButton`, `NTCard`, `NTDialog`, `NTToast`, `NTSnackbar`, `NTThemeToggle`, `NTNavigationRail`, `NTProgress`, `NTLoader`, `NTCarousel`, and the `NTInput*` form components.

The library's component model is built around:

- Material 3 structure, spacing, state, and token alignment.
- Co-located component files: `.razor`, `.razor.cs`, `.razor.scss`, and `.razor.ts` when browser behavior is needed.
- Scoped CSS plus shared static assets under `_content/NTComponents`.
- Progressive enhancement where possible: useful static markup first, richer behavior after browser JavaScript or Blazor interactivity is available.
- Render compatibility metadata through `NTDocumentationAttribute`.
- Analyzer-backed guidance for required parameters, invalid combinations, and accessibility-sensitive component usage.

## Component Families

The library includes components for:

- Actions: `NTButton`, `NTIconButton`, `NTFabButton`, `NTFabMenu`, `NTSplitButton`, `NTButtonGroup`.
- Layout: `NTLayout`, `NTHeader`, `NTBody`, `NTFooter`, `NTContainerView`, `NTListDetailView`, `NTSupportingPaneView`, `NTMultiPaneView`, `NTFeedView`.
- Navigation: `NTNavLink`, `NTNavigationRail`, `NTNavigationRailItem`, `NTNavigationRailGroup`.
- Surfaces and content: `NTCard`, `NTDivider`, `NTTag`, `NTChip`, `NTSkeleton`, `NTShape`, `NTTooltip`.
- Feedback: `NTToast`, `NTSnackbar`, `NTProgress`, `NTLoader`.
- Forms: `NTForm`, `NTInputText`, `NTTextArea`, `NTInputCheckbox`, `NTInputSwitch`, `NTInputRadioGroup`, `NTSelect`, `NTCombobox`, `NTAutocomplete`, `NTTypeahead`, `NTInputSlider`, `NTInputRangeSlider`, `NTInputDateTime`, `NTFileUpload`.
- Overlays and menus: `NTDialog`, `NTMenu`, `NTContextMenu`.
- Rich components: `NTDataGrid`, `NTCarousel`, `NTCarouselItem`, `NTTabView`, `NTTab`, `NTWizard`, `NTRichTextEditor`, `NTVirtualize`.

## Render Modes And SSR

Many components render useful HTML in static SSR and enhance later. Components that depend on browser APIs, JS interop, callbacks, or measurement are marked as progressively enhanced or interactive required in their generated documentation metadata.

Use these rules when choosing render modes:

- Native HTML behavior and pass-through attributes can work in static SSR.
- Blazor callbacks such as `EventCallback` require an interactive render mode.
- Browser-enhanced components need their package scripts and static assets available.
- App-level feedback hosts such as `NTToast` and `NTSnackbar` should be rendered once near the layout or route shell.

Static markup can use guarded browser bridge calls after the host script is available:

```html
<button onclick="window.NTToast?.queueToast({ title: 'Saved', message: 'Changes stored.', variant: 'success' })">
    Save
</button>
```

Interactive Blazor code can use the registered services:

```razor
@inject INTToastService ToastService
@inject INTSnackbarService SnackbarService

<NTButton Label="Show toast" OnClickCallback="ShowToastAsync" />
<NTButton Label="Show snackbar" Variant="NTButtonVariant.Outlined" OnClickCallback="ShowSnackbarAsync" />

@code {
    private Task ShowToastAsync()
    {
        return ToastService.ShowSuccessAsync("Saved", "Changes stored.");
    }

    private Task ShowSnackbarAsync()
    {
        return SnackbarService.ShowAsync("Message sent");
    }
}
```

## Theming

`NTHeadDependencies` and `NTThemeToggle` use the NT theme runtime. The default theme root is `/Themes`, with these expected file names:

- `light.css`, `light-mc.css`, `light-hc.css`
- `dark.css`, `dark-mc.css`, `dark-hc.css`

You can override the theme root and file names on both `NTHeadDependencies` and `NTThemeToggle`:

```razor
<NTHeadDependencies ThemesRoot="/brand-themes" LightDefaultCss="light.css" DarkDefaultCss="dark.css" />
<NTThemeToggle ThemesRoot="/brand-themes" DefaultTheme="NTTheme.System" DefaultContrast="NTThemeContrast.Default" />
```

The runtime persists the selected theme in local storage using `NTComponentsStoredThemeKey` and supports light, dark, and system preferences with default, medium, and high contrast variants.

## Packages

The repository produces:

- `NTComponents`: the core Blazor component library.
- `NTComponents.AspNetCore`: ASP.NET Core support package.
- `NTComponents.Extensions`: supporting extensions package.

The component package includes analyzer projects during build so component rules stay close to the public API.

## Upgrading To 2.0

2.0 targets .NET 10 and .NET 11 only, and removes every API that 1.x marked `[Obsolete]`, along with the legacy `TnT*` components. Move to the `NT*` equivalents before upgrading:

- Components: `TnTButton` → `NTButton`, `TnTInput*` → `NTInput*`, `TnTDataGrid` → `NTDataGrid`, `TnTSideNav` → `NTNavigationRail`, `TnTWizard` → `NTWizard`, and so on. Replace `NTInputSelect` with `NTSelect` or `NTAutocomplete`, and `NTInputFile` with `NTFileUpload`.
- Services: `AddTnTServices()` → `AddNTServices()`. `ITnTDialogService` and `ITnTToastService` are gone; use owned `NTDialog` components and `INTToastService` with `NTToast`.
- Enums: `TnTColor` → `NTColor`, `Size` → `NTSize`, `TextAlign` → `NTTextAlign`, `LayoutDirection` → `NTLayoutDirection`.

To migrate colors, run the migration fixer while still on a 1.x or 2.0 preview package. It cannot run after upgrading, because it needs `TnTColor` to exist:

```shell
dotnet format analyzers --diagnostics NTC1077 NTC1079 --severity warn
```

`NTC1078` flags `None`, `Black`, and `White`, which have no NTColor equivalent. Use `null` or omit an optional color parameter to get the component default. For fixed black or white, pick an appropriate semantic role instead.

Existing themes that define `--tnt-color-*` on `:root` continue to work through the compatibility stylesheet loaded by `NTHeadDependencies`.

## Development

Restore, build, and test from the repository root:

```powershell
dotnet restore
npm ci
npm run build:ts:release
dotnet build NTComponents.slnx -c Release
dotnet test Tests/NTComponents.Tests/NTComponents.Tests.csproj -c Release --no-build
npm test
```

Run AOT compatibility validation when changing trimming, JavaScript interop, static assets, or component public APIs:

```powershell
pwsh ./test-aot-compatibility.ps1
```

## Design And Documentation

Component design work follows the Material 3 source file and repo-local Material 3 reference notes. Public components use XML documentation and `NTDocumentationAttribute` metadata so documentation pages can show summaries, API members, render compatibility, and related enum or constant references directly from source.

The sample and documentation hosts in this repository are development aids for validating the package surface. Consumer setup should start with the quick setup guide above.

## License

NTComponents is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt) for details.
