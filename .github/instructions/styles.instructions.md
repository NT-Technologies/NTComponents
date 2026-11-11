---
applyTo: "**/*.razor.css,**/*.razor.scss"
---

# Styling Guidelines

## Core Rules
- **Format:** Use **SCSS** (`.razor.scss`) exclusively. Never check in compiled `.css` files.
- **Placement:** Create `[Component].razor.scss` as a sibling to the `.razor` file.
- **Shared Styles:** Place variables and mixins in a central `styles` folder (e.g., `ClientApp/styles/_variables.scss`). Use `@use` to import them.

## Scoping & Selectors
- **Root Class:** Wrap all component styles in a single root class named after the component in kebab-case (e.g., `.my-component` for `MyComponent.razor`).
- **Depth:** Keep selectors shallow (max 3 levels).
- **Naming:** Use a lightweight BEM-like pattern (e.g., `__element`, `--modifier`).
- **Globals:** Avoid global selectors or modifying element defaults within component styles.
- **!important:** Do not use `!important` unless absolutely necessary (requires comment).

## Libraries & Utilities
- **NTComponents:** Prefer existing NTComponents or utility classes for layout and spacing.
- **Custom UI:** Implement custom patterns locally only if not covered by the library.

## Accessibility & Responsiveness
- **Visibility:** Do not use `display: none` to hide accessible content; use ARIA attributes.
- **Breakpoints:** Support common responsive breakpoints using shared mixins/variables.