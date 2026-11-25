# Contributing To NTComponents

Thanks for helping improve NTComponents. Bug reports, documentation fixes, examples, and pull requests are all welcome.

## Before You Start

- **Questions and ideas:** start a thread in [Discussions](https://github.com/NT-Technologies/NTComponents/discussions).
- **Bugs:** open an [issue](https://github.com/NT-Technologies/NTComponents/issues) with the component, render mode (static SSR, Server, WebAssembly, or Auto), .NET version, and a minimal repro.
- **Larger changes:** open an issue or discussion first so the approach can be agreed before you invest time in it.

## Setup

You need the .NET SDK version pinned in `global.json`, Node.js, and npm.

```powershell
git clone --recurse-submodules https://github.com/NT-Technologies/NTComponents.git
cd NTComponents
dotnet restore
npm ci
npm run build:ts:release
dotnet build NTComponents.slnx -c Release
```

## Tests

Run both suites before opening a pull request. CI runs them too.

```powershell
dotnet test --project Tests/NTComponents.Tests/NTComponents.Tests.csproj -c Release --no-build
npm test
```

Run `pwsh ./test-aot-compatibility.ps1` when you change trimming behavior, JavaScript interop, static assets, or public component APIs.

Use xUnit v3 and bUnit for C# tests (`*.Tests.cs`), Jest for JavaScript modules (`*.test.js`), and Playwright in `Tests/NTComponents.IntegrationTests` for browser end-to-end coverage.

## Component Conventions

- Components live in feature folders under `NTComponents/` and split into co-located `.razor`, `.razor.cs`, `.razor.scss`, and optional `.razor.ts` files with the same base name.
- Follow the [Material 3 Figma source](https://www.figma.com/design/R5ZmyUEcfQt9bp2piDlBIp/) for structure, states, spacing, and tokens.
- Follow `.editorconfig`: 4-space indentation, CRLF line endings, braces required, file-scoped namespaces.
- Do not use BEM-style (`__`) CSS class names. Nest classes under the component root instead.
- Document public components with XML comments and `NTDocumentationAttribute` so the documentation site and MCP server pick them up.

## Pull Requests

- Use [Conventional Commits](https://www.conventionalcommits.org/), with a scope where it helps: `feat(dialog): ...`, `fix(input): ...`, `refactor(grid): ...`. Releases and changelogs are generated from these messages.
- Keep each pull request focused on one change and call out breaking changes explicitly.
- Include screenshots or GIFs for visual changes.
- Do not create `chore(release)` commits. semantic-release generates them.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE.txt).
