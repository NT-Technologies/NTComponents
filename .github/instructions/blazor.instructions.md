---
applyTo: "**/*.razor"
---

# Blazor Component Guidelines

## Core Principles
- **Single Responsibility:** Keep components small and focused. Split large components into smaller child components. Components should do one thing and one thing only.
- **Logic:** **NO** inline `@code` blocks. All logic must reside in a code-behind file (`[Component].razor.cs`).
- **State:** Avoid hidden global state. Pass data via parameters or explicitly injected services.
- **Regions:** Do not use `#region` blocks.

## File Structure
Follow this layout for every component:
1. **Markup:** `MyComponent.razor` (HTML/Razor only).
2. **Logic:** `MyComponent.razor.cs` (Partial class, logic, DI).
3. **Styles:** `MyComponent.razor.scss` (SCSS styles).
4. **Tests:** Located in the appropriate test project.

## Dependency Injection
- **Method:** Use **Constructor Injection** in the code-behind file.
- **Forbidden:** Do NOT use `@inject` directives in the `.razor` file.

## Parameters & API
- **Surface Area:** Keep public API minimal. Use `[Parameter]` only for values the parent *must* control.
- **Events:** Expose actions as `EventCallback` or `EventCallback<T>`.
- **Documentation:** Add XML documentation (`/// <summary>`) to all public members in the code-behind.

## Styling & UI
- **Format:** Use **SCSS** (`.razor.scss`) exclusively. No `.css` files.
- **Library:** Prefer [**NTComponents**](https://github.com/NT-Technologies/NTComponents) for UI elements and consistent styling.
- **Accessibility:** Use semantic HTML and ARIA attributes. Ensure responsiveness.

## Testing
- **Framework:** Use [**bUnit**](https://bunit.dev/docs/getting-started/index.html) for component testing.
- **Tools:** Use `xUnit`, `NSubstitute`, `AutoFixture`, and `AwesomeAssertions`.
- **Requirement:** Every component must have corresponding unit tests.

## AI Workflow
1. **Create:** Generate `.razor`, `.razor.cs`, and `.razor.scss` files simultaneously.
2. **Refactor:** If a component grows too large, suggest splitting it.
3. **Verify:** Ensure tests pass locally (`dotnet test`) before finalizing.
4. **Clarify:** If [**NTComponents**](https://github.com/NT-Technologies/NTComponents) usage or folder structure is unclear, ask before generating code.