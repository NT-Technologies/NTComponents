# NTCarousel Material 3 implementation specification

Status: Implemented as the hard replacement for the legacy carousel and validated against the repository, Material documentation, rendered media, and LiveTest.

Last reviewed: 2026-07-23

## Purpose

Replace the legacy `NTCarousel` and `NTCarouselItem` with a web adaptation of the current Material 3 carousel. The replacement must cover all six Material layouts, responsive item sizing, native scrolling, Material motion and interaction states, reduced motion, and an accessible keyboard and assistive-technology model. This is a hard replacement with no compatibility wrappers or parallel component identity.

Material does not publish a web implementation for this component. This specification therefore separates direct Material requirements from web-platform adaptations needed to express those requirements with valid HTML and ARIA.

## Sources

Material pages were freshly scraped on 2026-07-23 with the repository-approved `m3-docs-scraper` workflow:

- [Carousel overview](https://m3.material.io/components/carousel/overview)
- [Carousel specs](https://m3.material.io/components/carousel/specs)
- [Carousel guidelines](https://m3.material.io/components/carousel/guidelines)
- [Carousel accessibility](https://m3.material.io/components/carousel/accessibility)

The web accessibility mapping also follows the current [WAI-ARIA carousel pattern](https://www.w3.org/WAI/ARIA/apg/patterns/carousel/) and [WCAG 2.2 pause, stop, hide guidance](https://www.w3.org/WAI/WCAG22/Understanding/pause-stop-hide.html).

Downloaded figures were inspected for interaction states, dynamic size examples, responsive behavior, focus order, container labeling, and item position labeling. The rendered overview and accessibility videos were also sampled frame-by-frame. They confirm continuous large/medium/small mask interpolation, imagery moving more slowly than its mask, pressed ripple/state feedback, and the equal-width edge-reaching reduced-motion layouts. Authored item text remains rendered at every visual size and is naturally clipped by the morphing mask rather than hidden by the component. The temporary scrape and frame captures are under `.tmp/m3-carousel/` and are not implementation deliverables.

## Repository alignment

- Keep the implementation direct and defensive. Do not add a generic carousel framework, layout-provider abstraction, or duplicate state model.
- Keep the replacement `NTCarousel*` component files co-located in `NTComponents/Carousel/` and remove the legacy registered-child implementation without compatibility wrappers.
- Make `.razor.ts` the authored browser source and generate the served `.razor.js`; do not continue hand-authoring JavaScript only.
- Use existing color, shape, motion, elevation, and interaction tokens from `NTComponents/Styles/_Variables`.
- Use logical CSS properties and normal nested SCSS classes under the component root. Do not introduce BEM `__` names.
- Preserve static SSR content and native scrolling. JavaScript may enhance layout, input, focus, parallax, and callbacks after the browser module loads.
- Add bUnit, Jest, and LiveTest/Playwright coverage for every new observable contract. Use `[EditorRequired]` plus defensive runtime validation for invalid component parameters.
- Keep the root carousel free of overlaid or side-mounted previous/next controls. Material explicitly places alternate controls and “Show all” affordances above or below the carousel.

## Legacy implementation assessment

The replaced implementation had useful foundations that informed the replacement:

- `NTCarousel` owns child registration and stable `Order` sorting.
- The root renders a dedicated `.nt-carousel-viewport` native scroll surface.
- Pointer dragging can be enabled or disabled.
- CSS scroll snapping can be enabled, with centered snapping for center-aligned hero.
- Clickable items reuse the shared ripple/interactable system.
- The browser code throttles width recalculation with animation frames and observes viewport resize.
- Items and content are emitted during static SSR and enhanced after the custom elements load.
- No previous/next buttons are rendered into or over the carousel.

The implementation does not yet fulfill its public or Material-shaped surface:

- `CarouselAppearance` lists all six current Material layouts, but styling and layout behavior only distinguish hero and center-aligned hero. `nt-layout` is otherwise descriptive only.
- Every layout is a 700px-high horizontal flex row with 16px side padding. Full-screen is not vertical or edge-to-edge; uncontained variants do not have their specified overflow model.
- Item widths are driven primarily by `80%`, background-image pixel width, and viewport clipping. There is no large/medium/small keyline layout, 40–56px small-item range, responsive fitting model, or explicit maximum large-item width.
- Item resizing changes the content box at viewport edges, but there is no defined Material parallax model or reduced-motion alternative.
- The root and items have no carousel/slide semantics, accessible names, position labels, or roving keyboard focus.
- Clickable `div` content is mouse/click operable but has no button semantics or Space/Enter behavior.
- `OnIndexChanged` is public but is not connected to browser behavior.
- `AutoPlayInterval` is documented as milliseconds while the browser treats its numeric value as seconds. Autoplay has no pause control and does not implement the required focus/hover rules.
- `BackgroundColor` emits `--nt-carousel-bg-color`, but the component SCSS always uses transparent and never consumes the variable. Browser code similarly emits `--nt-carousel-item-gap` while SCSS uses a hardcoded 8px gap.
- Center alignment is conditional on `EnableSnapping`; disabling snapping removes the centered-layout class instead of changing only scroll settlement behavior.
- Browser source is hand-authored `.razor.js`, contrary to the repository’s TypeScript-source requirement.
- The LiveTest page repeats large item sets but does not identify layouts, demonstrate all six appearances, show accessible naming, or provide a “Show all” path.

## Material behavior contract

### Shared container measurements

Unless the layout overrides them:

| Attribute | Required value |
| --- | --- |
| Cross-axis alignment | Centered |
| Leading/trailing padding | 16px |
| Top/bottom padding | 8px |
| Gap | 8px |
| Item shape | 28px corner radius |
| Small item width | Dynamic within 40–56px |

Carousel height is content and product dependent; Material does not prescribe the current 700px fixed height. The component must have a compact usable default, accept consumer sizing through normal styles, and let the full-screen layout fill its containing content area.

### Layouts

| `CarouselAppearance` | Required layout | Default scrolling |
| --- | --- | --- |
| `MultiBrowse` | At least one large, one medium, and one small item when space permits. Additional large/medium items appear at wider sizes. All contained items remain fully visible. | Snap |
| `Uncontained` | Uniform-width items that can pass the trailing container edge. Leading padding is 16px; no contained trailing inset is imposed while scrolling. | Free; snap remains an allowed option |
| `UncontainedMultiAspectRatio` | Common-height items whose widths follow item aspect ratios from 9:16 through 16:9. Leading padding is 16px and gap is 8px. | Free; snap remains an allowed option |
| `Hero` | One dominant large item plus one 40–56px trailing preview in compact widths. Wider containers may show more large items. | Snap, one dominant item per step |
| `CenterAlignedHero` | One centered dominant large item with 40–56px previews on both leading and trailing sides. | Center snap, one dominant item per step |
| `FullScreen` | One edge-to-edge item per viewport, vertical scrolling, no outer padding, and 16px between items. Intended for portrait compact/medium experiences. | Mandatory vertical snap |

`EnableSnapping` remains for compatibility. `FullScreen` with snapping disabled is invalid and throws during parameter validation so a noncompliant vertical layout is never rendered. The uncontained layouts accept either value. The other contained layouts default to snapping.

The public default remains `true` for backward compatibility. Documentation recommends setting it to `false` for uncontained layouts when standard free scrolling is desired; both modes are permitted by Material for those layouts.

### Responsive sizing

- Use the carousel’s actual inline size, not global viewport breakpoints, so nested and pane layouts behave correctly.
- In compact widths, show no more than three text-heavy contained items at once. More items are acceptable only when their imagery remains recognizable.
- `PreferredItemWidth` is the target dominant-item width and defaults to 186px, matching the Android multi-browse strategy input. The engine searches complete arrangements and adjusts small items first, then medium items, then large items to fit the container.
- `MaxLargeItemWidth` controls the maximum dominant-item width. It must be optional; the layout engine chooses a readable default when omitted.
- Medium width consumes remaining layout space after large items, small items, gaps, and padding are accounted for.
- Small width is clamped to 40–56px. A configuration that would make content less than 40px must reduce the number of visible keylines rather than produce a thinner item.
- `NTCarouselItem.AspectRatio` supplies the SSR-safe ratio for `UncontainedMultiAspectRatio`. When omitted, the browser uses `1`.
- Resizing must preserve the active item and recompute keylines without jumping to a different logical item.

### Motion and parallax

- Native user scrolling is the source of truth. Do not replace it with a translated track.
- Snap settles to layout keylines after input ends. Hero variants advance one dominant item. Multi-browse uses discrete Android-style start/default/end keyline states: each end step moves exactly one trailing non-focal keyline ahead of the focal range (`L/M/S`, then `S/L/M`, then `S/M/L` at compact width).
- Multi-browse snap offsets come from the distance moved by each keyline state, not a uniform item-index multiplier. Multiple focal items may share the same initial offset.
- After enhancement, one JavaScript settlement path owns snapping for drag, wheel/touch scroll-end, keyboard, and autoplay. It is cancellable by new input and uses release velocity to choose the destination and duration.
- When reduced motion is not requested, programmatic settlement interpolates smoothly with continuous initial velocity and a zero-velocity finish. Reduced motion settles immediately.
- Autoplay uses the same smooth settlement between successive distinct keyline positions. When multiple focal items share an initial snap offset, autoplay skips those duplicate offsets so every interval produces visible movement. After the final physical position, the next timer tick smoothly returns to item 1; reduced motion returns immediately.
- Only item imagery receives parallax. Text and interactive child content must remain spatially stable and legible.
- Every contained item keeps a dominant-width canvas while a separate clipping mask morphs between large, medium, and small. A leading multi-browse mask moves normally until its leading edge reaches the viewport boundary. That edge then remains pinned to the boundary while the trailing edge continues its normal drag path, squashing the mask at the drag rate. After reaching the 40–56px small keyline, the complete fixed-size mask slides offscreen. Neither image nor text reflows when the mask changes.
- Background images move at a lower rate than the item container and remain covered without exposing empty space.
- Shape and size changes use existing Material motion tokens. Avoid per-item timers and layout-thrashing reads/writes.
- `prefers-reduced-motion: reduce` disables smooth programmatic scrolling, parallax, and in-scroll width/shape interpolation. Items use equal stable sizes; contained layouts extend to the content edges so visuals are not accidentally clipped. Hero retains a partial next-item preview.

## Public component contract

### `NTCarousel`

Provide:

- `AllowDragging`
- `Appearance`
- `BackgroundColor`
- `ChildContent`
- `EnableSnapping`
- `OnIndexChanged`

Clarify and complete:

- `AutoPlayInterval` is expressed in seconds. XML documentation, examples, and tests must use that unit consistently.
- `OnIndexChanged` fires only when the settled active item changes, including keyboard, drag, wheel/touch, and autoplay movement. It does not fire repeatedly during a single scroll gesture.

Add:

- `string AriaLabel`: required non-empty accessible name for the carousel. Because `aria-roledescription="carousel"` already announces the control type, examples should use content names such as “Featured destinations,” not “Featured destinations carousel.”
- `bool IsLandmark`: defaults to `false`, which renders `role="group"`. Set it only when the carousel is important enough to warrant a named `region` landmark.
- `int? MaxLargeItemWidth`: optional CSS-pixel maximum for dominant items. Reject non-positive values.
- `int PreferredItemWidth`: target multi-browse dominant width in CSS pixels, default `186`. Reject non-positive values.
- `int ItemHeight`: horizontal carousel height in CSS pixels, defaulting to `240` and rejecting non-positive values.

When `AutoPlayInterval` is set, the required control is always rendered before the viewport, never over it, and its accessible label changes between “Pause rotation” and “Start rotation.” There is intentionally no option to hide this accessibility requirement.

Do not add previous/next buttons or indicator dots in this phase. Native panning, scrollbars hidden only visually, keyboard focus, and the external “Show all” affordance are the Material-aligned navigation model.

### `NTCarouselItem`

Provide:

- `BackgroundImageSrc`
- `ChildContent`
- `EnableRipple`
- `OnClickCallback`

Add:

- `string AriaLabel`: a required content-specific item name. The browser appends “{position} of {count}” after enhancement.
- `double? AspectRatio`: width divided by height for the multi-aspect-ratio layout; valid range is 9/16 through 16/9.

Items remain in authored DOM order. `NTCarousel` deliberately does not borrow child render fragments or register/re-render descendants; direct child rendering is required for reliable static SSR and avoids renderer recursion with several carousels on one page.

Clickable items keep the current callback but gain focus, button semantics, and Space/Enter activation. Non-clickable items remain focusable slide groups so their name and position can be announced and arrow navigation can reach them.

### Configuration validation

`AriaLabel` and child content use Blazor's `[EditorRequired]` compile-time assistance. Runtime validation rejects missing or whitespace carousel/item labels, a non-positive autoplay interval, maximum large width or item height, an aspect ratio outside 9:16 through 16:9, an item outside `NTCarousel`, and `FullScreen` with snapping disabled. This keeps dynamic values safe without introducing a separate analyzer surface for the replacement component.

## Rendered DOM and accessibility

Target structure:

```html
<nt-carousel role="group" aria-roledescription="carousel" aria-label="Featured destinations" data-layout="multi-browse">
  <button type="button" class="nt-carousel-autoplay-control">Pause rotation</button>
  <div class="nt-carousel-viewport">
    <div class="nt-carousel-track">
      <nt-carousel-item role="group" aria-roledescription="slide" aria-label="Desert retreat, 1 of 8" tabindex="0">
        <div class="nt-carousel-item-mask">...</div>
      </nt-carousel-item>
    </div>
  </div>
</nt-carousel>
```

- Default to `role="group"`. `IsLandmark="true"` uses `role="region"` only when the carousel is important enough to appear among page landmarks.
- The root is not focusable. Initial `Tab` focus enters the current item, initially the first item.
- Before enhancement, every item remains reachable so static SSR does not hide content from keyboard users. After enhancement, use a roving `tabindex`: one item is in the page tab order; arrow navigation moves focus and the active tab stop between items; the next `Tab` leaves the carousel.
- Horizontal layouts use Left/Right arrows and respect RTL visual direction. Full-screen uses Up/Down arrows. Orthogonal arrows are not intercepted, allowing normal page movement.
- Space or Enter activates a clickable focused item. They do not synthesize activation for non-clickable items.
- Item labels include the logical position and total count. Consumer `AriaLabel` text is combined with position, not substituted for it.
- Focus movement scrolls the target to the appropriate layout keyline and does not focus the viewport or root.
- If autoplay is enabled, it stops when keyboard focus enters and does not restart until explicitly requested. It pauses while hovered, dragged, touched, natively scrolled, or hidden. Boundary input that produces no scroll restarts one fresh interval instead of disabling rotation. Reduced-motion mode starts with autoplay paused, and both visibility loss and enabling reduced motion cancel an in-flight settlement.
- Every autoplay instance provides a visible pause/start control. The control stays outside the scroll viewport and precedes rotating content in the tab order.
- On vertically scrolling pages, the consumer must provide a “Show all” button or a header-adjacent 48px arrow that opens a vertically scrolling view of all items. This requires application routing/content knowledge and therefore remains outside the reusable carousel API. Documentation and LiveTest must demonstrate it.

## Browser implementation

- Author `NTCarousel.razor.ts` as the single browser source and generate `NTCarousel.razor.js` with the existing TypeScript release pipeline. `NTCarouselItem` has no independent layout controller.
- Keep one owning carousel controller. Item elements expose only the minimal measured/media state the controller needs; do not maintain a second independent layout loop per item.
- Read the direct authored child item list during the page-script update lifecycle; do not register descendants or re-render their fragments from the parent.
- Use one `ResizeObserver` for the viewport and one animation-frame scroll scheduler. Batch geometry reads before style writes.
- Preserve authored DOM order and the active logical index across resize-driven keyline recalculation.
- Use logical scroll calculations that normalize RTL browser behavior.
- Use pointer capture for mouse/pen dragging. Preserve native touch scrolling and vertical page escape with appropriate `touch-action` values. Any pointer or wheel input cancels an in-flight programmatic settlement; native scroll events defer the next autoplay interval until settlement, while pointer release provides recovery when a boundary touch produces no scroll event.
- Retain CSS snap points only as the pre-enhancement fallback. Once enhanced, disable CSS snap so it cannot compete with the controller's velocity-aware settlement.
- Clean up observers, timers, animation frames, media-query listeners, pointer handlers, keyboard handlers, and autoplay handlers on disconnect/update.
- Use the existing page-script lifecycle and asynchronous JS interop. Do not require a Blazor interactive render mode for native scrolling, semantic labels, or CSS snapping.

## Styling implementation

- Replace the fixed 700px root height with a compact default block size and allow consumer `style` to override it. Full-screen uses the available content viewport block size.
- Apply shared 28px corner-radius, 8px gap, and 8px/16px padding tokens. Full-screen removes the radius only where edge-to-edge visuals require it.
- Select layout styles from the component's `data-layout` attribute instead of broad hero-only classes.
- Keep overflow scrolling and scrollbar suppression on `.nt-carousel-viewport` only.
- Keep clipping on the item visual/content layer, separate from any layer that may render focus indication or elevation.
- Use the shared interactable mixin for hover, focus, pressed, disabled, and ripple behavior. Preserve a visible focus indicator outside the clipped visual.
- Use CSS variables only for runtime layout values calculated by the controller or documented public overrides. Compile constant measurements from shared SCSS variables.

## LiveTest and documentation

Add a dedicated `/carousel` LiveTest page with labeled sections for:

1. Multi-browse
2. Uncontained
3. Uncontained multi-aspect ratio
4. Hero
5. Center-aligned hero
6. Full-screen
7. Reduced-motion verification guidance
8. Optional autoplay with visible pause/start control

Each horizontal example includes an accessible content label and a “Show all” affordance below or in its heading row. Items use varied, recognizable imagery and concise text that demonstrates large/medium/small adaptation instead of twelve copies of the same logo.

## Validation plan

### bUnit

- All six layouts render distinct layout contracts.
- Root and items render valid roles, roledescription, labels, positions, and initial roving tabindex in static output.
- Additional attributes still merge without duplicating class/style/ARIA values.
- Clickable and non-clickable item semantics differ correctly.
- Parameter validation covers invalid combinations.
- `OnIndexChanged` is wired through the lifecycle without firing during initial static render.

### Jest

- Keyline calculation at compact, medium, and expanded container widths.
- Discrete compact and expanded end-state order, nonuniform end offsets, minimum-size offscreen containers, and constant item/content canvases while masks interpolate.
- 40–56px small-item clamp and fallback when space is insufficient.
- Uniform, multi-aspect, hero, center-aligned, and vertical full-screen geometry.
- Stable active item through resize.
- Pointer threshold, click suppression after drag, touch-axis preservation, and cleanup.
- Roving focus, Enter/Space activation, RTL arrows, and full-screen Up/Down navigation.
- Settled index reporting once per logical change.
- Autoplay pause/start, focus, hover, drag, reduced-motion, and timer cleanup.
- Reduced-motion media-query changes after initialization.

### Playwright/LiveTest

- Inspect each layout at compact and desktop widths.
- Pan with mouse and native wheel/touch-equivalent input; verify the page does not acquire horizontal overflow.
- Verify snap destinations, active-item sizing, parallax, focus indicator, keyboard order, and callback output.
- Emulate reduced motion and verify equal stable item sizes, no parallax, no smooth scrolling, and paused autoplay.
- Check full-screen portrait vertical snapping and ensure landscape usage is documented as unsupported rather than silently broken.
- Run an accessibility snapshot to confirm carousel and slide names/positions.

## Acceptance criteria

- All six `CarouselAppearance` values produce the Material layout described in this document.
- Multi-browse and hero items visibly transition between Material size roles without becoming thinner than 40px.
- Uncontained multi-aspect items preserve declared aspect ratios; full-screen scrolls vertically one viewport at a time.
- Native scrolling works before browser enhancement; enhanced dragging, parallax, focus, callbacks, and autoplay work in Interactive Server, WebAssembly, and Auto contexts.
- Keyboard and screen-reader users can enter, traverse, activate, and leave the carousel without focusing the container.
- Every auto-rotating carousel can be paused and does not restart after focus without an explicit request.
- Reduced motion removes parallax and dynamic resizing.
- The component renders no overlaid or side-mounted previous/next controls.
- Focus indicators, item content, scroll clipping, and any elevation do not clip one another.
- Focused bUnit and Jest tests, a Release build, and the LiveTest browser matrix pass.

## LiveTest runtime observation

The existing host was built for `net10.0`, launched at `http://localhost:5185/carousel`, and inspected in Chromium at 1440×1000 and 390×844. The initial `--no-build` run served empty scoped CSS bundles, so it was discarded; all observations below come from a successful current rebuild. The browser console contained no component errors.

### Current behavior to preserve

- The carousel owns horizontal overflow. At 390px the document remained 390px wide while each carousel viewport held its own wider scroll track.
- Native scroll snapping works. On the first desktop carousel, a real mouse drag moved the viewport from 0px to 615px and settled at 520px, exactly one 512px item plus the 8px gap.
- The dynamic clipped preview works as a discoverability cue. Before the drag, visible content widths were 512px, 512px, and 78px; after snapping, they became 56px, 512px, and 598px as items crossed the viewport edges.
- Autoplay uses seconds in practice. The `AutoPlayInterval="1"` example advanced by one 520px item-plus-gap step over approximately 1.25 seconds at desktop width.
- Pointer dragging stops autoplay during the gesture, removes snap while the dragging class is present, suppresses the drag click, then restores snap/autoplay on release.
- The 28px item shape, shared ripple on the clickable item, hidden scrollbar, 8px gap, 16px outer inset, and contained horizontal track render without browser errors.

These are regression constraints. The Material keyline engine may change exact widths, but it must retain the native-scroll feel, clipped preview cue, drag threshold/click suppression, local overflow ownership, and deterministic snap settlement.

### Measured gap matrix

| Area | Current LiveTest result | Required result |
| --- | --- | --- |
| Demo coverage | Five instances: multi-browse, autoplay multi-browse, hero, and two center-aligned hero examples | One labeled example for every one of the six Material layouts, plus reduced motion and autoplay |
| Root size | Every instance is exactly 700px tall at desktop and compact widths | Content/product-sized default; full-screen alone fills its content viewport |
| Desktop multi-browse | 1118px viewport; first item slots are 512px, 512px, 894px, 512px while visible content widths are 512px, 512px, 78px, 56px | Stable large/medium/small keylines fitted to available width with small clamped to 40–56px |
| Compact multi-browse | 358px viewport; uniform 286.39px slots with one 286.39px item and a 63.22px clipped preview visible | Recognizable contained large/medium/small layout, reducing keyline count instead of producing accidental widths |
| Hero | Compact view shows a 286.39px leading item and a 63.61px trailing preview | Preserve this useful large-plus-preview behavior while using the defined 40–56px small range |
| Center-aligned hero | At initial compact position, item 1 begins at the leading inset and only a trailing preview is visible; geometry is identical to hero | Large item centered between leading and trailing previews from the initial state |
| Uncontained variants | Enum values exist, but there are no LiveTest instances and no layout-specific CSS/JS | Uniform uncontained and declared-ratio uncontained behavior |
| Full-screen | Enum value exists, but there is no LiveTest instance and all code is horizontal | Edge-to-edge vertical viewport with mandatory y-axis snap |
| Responsive behavior | Slot width changes from image-natural width/80% fallback, with content clipped to visible intersection | Container-based Material keylines, stable active identity, and explicit aspect ratios |
| Accessibility tree | Root and items expose no roles, names, roledescriptions, position labels, or current item | Named carousel region/group and named slide groups with position/total |
| Keyboard | The first carousel contains zero focusable elements; clickable content is an unfocusable `div` with no role | One roving item tab stop, arrow traversal, Space/Enter activation, and normal Tab exit |
| Reduced motion | Emulation leaves `scroll-behavior: smooth`, dynamic content widths, snap behavior, and autoplay running | No smooth motion, parallax, or dynamic resizing; autoplay initially paused |
| Autoplay control | `AutoPlayInterval="1"` moves content automatically with no visible or focusable control | Visible start/pause control plus focus, hover, drag, and reduced-motion safeguards |
| Show all | No headings or alternate vertical item path | Consumer-owned Show all/header arrow demonstrated outside the viewport |

### Specification changes caused by runtime review

- Native overflow, pointer dragging, snap restoration, click suppression, and the clipped preview are explicitly preservation requirements.
- The layout engine must not replace the scroll surface with a translated track.
- Center-aligned hero needs a defined initial keyline; `scroll-snap-align: center` alone is insufficient at the scroll boundary.
- Width calculations must stop using background pixel width as the primary layout contract. The current logo’s 512px natural width dominates desktop slots, while the no-background item falls back to 80%, producing unrelated slot sizes.
- The LiveTest redesign is required for validation, not merely documentation, because the present page cannot exercise half of the public appearance enum.

## Implementation conformance review

The implemented component was reviewed in the rebuilt `/carousel` LiveTest page at 1280×720 and 390×844:

- All seven examples enhanced successfully with no browser console errors or warnings, and the document never acquired horizontal overflow.
- Desktop multi-browse measured four 186px focal items, one 150px medium item, one 56px small item, 8px gaps, and 16px insets. Compact measured 186px, 116px, and 40px keylines.
- A real compact mouse fling produced successive intermediate scroll positions before settling at the final 754px snap. New touch input cancels an in-flight programmatic settlement, and the enhanced viewport has no competing CSS snap owner.
- Runtime selector verification found and corrected a specificity regression where the pre-enhancement `mandatory` CSS snap rule remained active after enhancement and quantized the controller's animation. The fallback now explicitly excludes `[data-enhanced='true']`; all enhanced layouts compute `scroll-snap-type: none` while the controller settles them.
- The primary multi-browse LiveTest sequence now contains 14 distinct items. At 390px it exposes a 2306px logical range and terminal snaps at 2182px and 2306px, so the demo exercises intermediate and end-state transitions instead of ending after the first few arrangements. Repeated real-pointer drags settled at 388px, 776px, 1164px, 1552px, 1940px, and finally 2306px with item 14 active, proving the terminal item is reachable.
- A deliberately slow Playwright drag (5px every 40ms) exposed an Interactive Auto hydration discontinuity: Blazor replaced the direct viewport and then the custom-element root during an active gesture. The controller now transfers only active interaction state across a stable carousel identity, restores direct-child viewport replacements from a mutation callback before paint, preserves the live logical scroll during relayout, and reacquires pointer capture on the replacement root.
- Repeating that test from first static paint produced the continuous sequence 5px, 10px, 15px through 250px while the root changed at 60px, with no reset or snap-position spike. With reduced motion disabled, release then produced per-frame values from 250px through 249px, 248px, 247px, and onward to the 194px destination over 191ms rather than jumping to the destination.
- The initial implementation used one direct interpolation into a mirrored terminal state. Review found that this skipped the Android intermediate end state and produced a medium/medium/medium penultimate stop. The corrected contract moves trailing keylines individually: compact `L/M/S → S/L/M → S/M/L`, and expanded `L/L/…/M/S → S/L/L/…/M → S/M/L/L/…`.
- The corrected snap range reaches the final item using per-state distances (`small + gap`, then `medium + gap`) instead of continuing one large-item step for every logical item.
- Fully outside multi-browse keylines retain the arrangement's 40–56px small size and are positioned completely beyond the viewport edge, eliminating slivers without collapsing a container toward zero.
- Leading-edge interpolation has three phases: free translation before contact, boundary-pinned squashing, and fixed-small translation offscreen. The interpolated trailing edge remains unchanged across all three phases, preserving the normal 8–16px neighbor spacing without opening an empty band. In the compact slow-drag trace, the mask measured `left 1.6px / width 148.4px`, then `left 0 / width 97.9px`, `left 0 / width 45.8px`, and finally `left -25.4px / width 40px`.
- Contained item hosts, media, and content now retain the dominant canvas width while only the mask width and offset change, preventing image scaling and text reflow.
- At compact width every item host and content canvas remained 186px and every media layer remained 234px while masks changed through 186px, 116px, and 40px; offscreen masks remain 40px.
- Hero and center-hero use shifted start/end states so boundary items preserve previews and trailing alignment.
- Arrow navigation moved focus and the roving tab stop together, reported the settled logical index once, respected RTL direction, and used Up/Down for full-screen.
- Focus changed autoplay to “Start rotation”; explicit resume advanced after the configured four seconds. Reduced motion started paused, used equal 174px edge-reaching items, set parallax to `0px`, and retained a 40px partial next hero item.
- Autoplay terminal wrapping smoothly returns the logical index, roving tab stop, and scroll position to item 1, then schedules the next normal interval. Reduced-motion mode performs the same state reset immediately.
- At 1022px, multi-browse produced four logical indexes at the initial `0px` snap position. Autoplay now selects the next greater snap position, visibly settling `0px → 64px → 222px` on consecutive intervals before the smooth `222px → 0px` terminal return.
- Full-screen measured 1022×640 with a 3920px vertical track, no horizontal overflow, 16px between pages, and a 656px ArrowDown snap step.
- The accessibility snapshot exposed named carousel groups, named slide groups with “position of total,” consumer-owned Show all buttons, and the visible autoplay control.

The first direct-child implementation attempt also received a static SSR stress check with all seven examples. Borrowing and re-rendering registered child fragments caused renderer recursion, so the approved implementation renders authored child DOM exactly once and lets the single browser controller enhance it. The corrected structure loads cleanly in static SSR and Interactive Auto.

## Approval decisions

The implemented contract intentionally makes these choices:

1. Require a non-empty `AriaLabel` on every `NTCarousel` and `NTCarouselItem`, default the root role to `group`, and enforce labels with `[EditorRequired]` plus runtime validation.
2. Preserve `AutoPlayInterval` as seconds to match current runtime behavior, then make autoplay accessible instead of silently changing its unit.
3. Add `ItemHeight`, `PreferredItemWidth`, `MaxLargeItemWidth`, and item `AspectRatio` as layout inputs; keep exact medium/small sizing internal so the component maintains a valid Material layout.
4. Keep the original `NTCarousel` untouched and give the new implementation its own component, item, styles, TypeScript controller, generated JavaScript, tests, and LiveTest route.
