# NTComponents.Analyzers

Roslyn analyzers for NTComponents applications. The package reports invalid component configurations, inaccessible icon-only controls, conflicting data-grid options, and other mistakes that NTComponents can identify at build time.

## Install

```xml
<PackageReference Include="NTComponents.Analyzers" Version="VERSION">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Diagnostics use the `NTC` prefix and run during IDE analysis and builds. They check NT component parameters written in Razor markup or hand-written `RenderTreeBuilder` code, and point at the `.razor` line when Razor maps one.

- **Warnings** predict a runtime exception or broken rendering, such as an empty required label, a background that does not match the variant, or a component rendered outside the parent it needs.
- **Accessibility** rules report controls that would render without an accessible name.
- **Info** suggestions report values a component ignores or adjusts, such as a parameter that only applies in another mode.

The analyzers only report what they can prove from constant values; anything computed at runtime is left alone. See [RULES.md](https://github.com/NT-Technologies/NTComponents/blob/main/NTComponents.Analyzers/RULES.md) for every rule, and tune one with `dotnet_diagnostic.NTC1003.severity` or a category with `dotnet_analyzer_diagnostic.category-Accessibility.severity` in `.editorconfig`.

## Color Migration

`NTC1077` migrates `TnTColor` references to `NTColor`; `NTC1079` migrates `ToCssTnTColorVariable()` calls to `ToCssNTColorVariable()`. Both diagnostics provide code fixes and Fix All support. The package includes `NTComponents.Analyzers.CodeFixes.dll` alongside the analyzer.

After updating NTComponents, analyze and apply supported C# fixes from your application directory:

```shell
dotnet format analyzers --diagnostics NTC1077 NTC1079 --severity warn
```

Pass a project or solution path after `analyzers` when needed. The CLI covers C# and `.razor.cs` files; update `.razor` expressions through your editor. `NTC1078` reports removed `None`, `Black`, and `White` values for manual replacement. Use an omitted/null optional color for defaults or choose a semantic role; those members have no safe automatic replacement.

Fix All converts color references throughout the selected C# scope.

Source and issue tracking: <https://github.com/NT-Technologies/NTComponents>
