# Branchline Design

## Source of truth

- Status: Active
- Last refreshed: 2026-07-17
- Primary product surfaces: launcher, repository history, working copy, commit inspector, diff viewer, dialogs and popovers
- Evidence reviewed: current dark-theme launcher, history, working-copy, toolbar, sidebar, diff, and inspector captures; `src/Resources/Themes.axaml`; `src/Resources/Styles.axaml`; primary XAML views; existing product screenshots
- External references:
  - Apple Human Interface Guidelines: [Sidebars](https://developer.apple.com/design/human-interface-guidelines/sidebars), [Toolbars](https://developer.apple.com/design/human-interface-guidelines/toolbars), [Layout](https://developer.apple.com/design/human-interface-guidelines/layout), and [Icons](https://developer.apple.com/design/human-interface-guidelines/icons)
  - Apple WWDC25: [Get to know the new design system](https://developer.apple.com/videos/play/wwdc2025/356/)
  - Native macOS reference: [CodeEdit](https://github.com/CodeEditApp/CodeEdit)
  - Framework constraints: [Avalonia window transparency](https://docs.avaloniaui.net/api/avalonia/controls/toplevel) and [style selectors](https://docs.avaloniaui.net/docs/styling/style-selectors)

## Brand

- Personality: quiet, exact, technical, native to macOS, and confident under dense information.
- Trust signals: stable geometry, legible Git state, predictable selection, visible keyboard focus, and immediate command feedback.
- Avoid: ornamental glass, neon tinting, oversized empty states, pill-heavy controls, floating page cards, and visual novelty that competes with repository data.

## Product goals

- Goals: make repository state scannable, keep common Git actions one glance away, and support long keyboard-and-mouse sessions without visual fatigue.
- Non-goals: marketing-style presentation, hiding advanced Git capabilities, or making data panes translucent for effect.
- Success signals: users can distinguish navigation, commands, content, and inspection at a glance; labels do not clip at the supported minimum window size; changing views does not shift shared controls; no popup, tooltip, or material-related crashes.

## Personas and jobs

- Primary personas: developers reviewing history, preparing commits, resolving changes, and navigating branches across one or more repositories.
- User jobs: understand current Git state, locate a revision or ref, inspect a change, prepare a commit, and run sync or maintenance commands.
- Key contexts of use: wide desktop windows, long sessions, dark mode, keyboard navigation, and resizable multi-pane layouts.

## Information architecture

- Primary navigation: repository tabs in the title area; repository modes and refs in the leading sidebar.
- Core screens: Repositories, History, Local Changes, Stashes, commit Details, Changes, and Repository tree.
- Content hierarchy: window chrome -> repository command bar -> leading navigation -> primary data/content -> trailing inspector or focused diff.
- The leading pane owns navigation. The center owns the current task. The trailing pane owns contextual inspection. A command must not appear in more than one of these roles without a clear frequency reason.

## Design principles

1. Content first. Glass is a functional chrome layer above opaque data, never a background for code, diff lines, commit messages, or tables.
2. Hierarchy through placement. Use spacing, alignment, grouping, and typography before adding backgrounds, borders, or shadows.
3. Dense, not cramped. Keep 24-32 px controls and 28-32 px rows while preserving readable labels and stable hit areas.
4. One component anatomy. The same action keeps the same symbol, label, state feedback, and placement logic across screens.
5. Native restraint. Small and medium macOS controls use compact rounded rectangles; capsules are reserved for a genuinely prominent action or count.
6. Reliability is visual quality. Every translucent surface has an opaque fallback, and no experimental acrylic control is permitted.

## Visual language

- Color: neutral graphite in dark mode and neutral cool gray in light mode. System accent color is reserved for focus, active navigation, and primary actions. Git graph and status colors remain semantic.
- Typography: Inter for interface text and JetBrains Mono for code, paths, SHA, and fixed-width counters. Section labels are 11-12 px, semibold, and left aligned; working labels remain 12-13 px.
- Spacing/layout rhythm: 4 px base unit. Common insets are 6, 8, 12, and 16 px. Pane edges align vertically across split views.
- Shape/radius/elevation: 4 px for dense rows and inputs, 6 px for grouped controls, 8 px maximum for popovers. Page panes remain edge-to-edge. Shadows are limited to detached overlays.
- Motion: no decorative motion. Feedback is immediate and uses opacity or color changes without geometry shifts.
- Imagery/iconography: familiar single-color symbols, 12-14 px in compact bars. Use text when a symbol is ambiguous. Do not manually tint all navigation symbols with a brand color.
- Icon source policy: shared icons must be original geometry or come from a permissive pack with attribution. Apple SF Symbols must never be copied into the cross-platform resource dictionary; a future macOS-only provider may resolve them from AppKit at runtime and must always retain a permissive fallback.
- Icon naming policy: new workflow actions use semantic keys such as `Icon.StageSelected` and `Icon.NextChange`. Pack-specific or legacy `Icons.*` keys remain implementation details while high-frequency screens migrate.

## Components

- Existing components to reuse: `icon_button`, `flat`, `switch_button`, `toolbar_group`, sidebar list styles, inspector tabs, text inputs, and native splitters.
- New or changed components: semantic pane brushes; compact toolbar overflow menu; sidebar navigation rows; pane headers; restrained empty states; keyboard-visible focus states.
- Variants and states: rest, pointer-over, pressed, selected or checked, focus-visible, inactive window, disabled, loading, empty, and error.
- Token ownership: color and material in `src/Resources/Themes.axaml`; global component anatomy in `src/Resources/Styles.axaml`; screen composition in `src/Views/*.axaml`.
- Button contract: icon buttons have a stable 28 x 28 hit area, 12-14 px glyph, 6 px radius, no rest fill outside a group, subtle hover fill, darker pressed fill, and a visible focus ring.
- Pane contract: navigation and inspector may use translucent material; history, working copy, editors, and diff remain opaque. Pane separators are 1 px and splitters keep a larger invisible drag target.

## Accessibility

- Target standard: WCAG 2.2 AA where applicable to desktop controls.
- Keyboard/focus behavior: all interactive controls retain tab navigation and a 2 px system-accent focus indicator using `:focus-visible`; Escape returns from focused diff views where already supported.
- Contrast/readability: primary text is high contrast, secondary text remains legible, and transparency never carries body text without a text-safe fill.
- Screen-reader semantics: icon-only buttons require localized tooltips or accessible names. State is not communicated by color alone.
- Reduced motion and sensory considerations: avoid animated decoration and provide opaque material fallbacks.

## Responsive behavior

- Supported devices: desktop windows from the existing 1024 x 600 minimum upward; macOS is the primary visual target.
- Layout adaptations: leading repository navigation has a practical 280 px minimum so labels and search affordances do not clip; working-copy file lists have a practical 320 px minimum; graph and commit subject retain priority over author and time metadata at constrained widths.
- Touch/hover differences: desktop hover states clarify clickability; controls keep stable hit areas and do not resize on hover.

## Interaction states

- Loading: preserve layout and show progress near the affected command or pane.
- Empty: use a compact symbol and direct state label centered in the content pane; avoid oversized illustrations.
- Error: use semantic color plus icon and concise text; detailed logs stay available through existing actions.
- Success: avoid persistent celebratory UI; update state and use existing notifications when needed.
- Disabled: reduce contrast without removing labels or changing geometry.
- Transparency unavailable: fall back to the semantic opaque pane or popup brush.

## Content voice

- Tone: concise, factual, and action-oriented.
- Terminology: use established Git terms and preserve existing localization keys.
- Microcopy rules: commands use verbs; panes and modes use nouns; tooltips name the action and may include the shortcut. Do not explain visible styling or basic interface mechanics inside the product.

## Implementation constraints

- Framework/styling system: Avalonia XAML with shared dynamic resources and selector-based states.
- Design-token constraints: do not hard-code new surface colors in views when a semantic brush can own the role.
- Performance constraints: keep virtualized lists, avoid per-row effects, and do not add blur or shadow to scrolling data.
- Compatibility constraints: macOS supports only transparent Avalonia windows; native interop owns backdrop blur. `ExperimentalAcrylicBorder` and `ExperimentalAcrylicMaterial` are banned because they previously crashed during popup and tooltip updates.
- Asset constraints: Apple design resources are reference material, not distributable application assets. Do not export, trace, or vendor SF Symbols into `Icons.axaml`.
- Test/screenshot expectations: build Debug and self-contained macOS Release; capture launcher, history or diff, and working-copy states at 1280 x 720; inspect title bar, toolbar groups, sidebar, primary content, inspector, empty states, and longest visible labels; verify no new crash logs after interaction.

## Open questions

- [ ] Decide whether a future release should expose Small, Medium, and Large sidebar density as a user preference.
- [ ] Validate the light theme on a light macOS desktop after the dark-theme overhaul is accepted.
- [ ] Evaluate replacing custom title tabs with a more native tabbing model without changing multi-repository behavior.
- [ ] Audit the provenance and license of legacy geometries in `Icons.axaml` before replacing the remaining low-frequency icons.
