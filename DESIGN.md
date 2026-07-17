# Branchline Design Contract

## 1. Product and Audience

Branchline is a desktop Git client for developers who scan commit graphs, branches, authors, times, and file changes for long sessions. The interface must optimize for dense comparison, quick navigation, and predictable keyboard-driven work rather than presentation or decoration.

## 2. Visual Direction

Use a quiet macOS-native graphite workspace with a restrained Liquid Glass-inspired chrome layer. Glass belongs to navigation and command surfaces; repository history, diff, and code content remain opaque and high-contrast. Color comes from graph lanes, status semantics, and a single system-blue interaction accent.

### Apple Design Principles

- Purpose: optimize the product around understanding repository history and acting on Git state.
- Familiarity: use standard desktop patterns, recognizable symbols, menu commands, tooltips, and predictable selection behavior.
- Flexibility: preserve resizable windows, sidebars, inspectors, and table columns plus keyboard-first workflows.
- Simplicity: keep primary sync actions close and group secondary tools without removing capability.
- Craft: align labels with their data, keep hit targets usable, and handle focus, inactive, loading, empty, and fallback states deliberately.
- Delight: let graph topology, author avatars, responsive hover feedback, and subtle material depth provide character without distracting from work.

## 3. Information Architecture

- Top chrome: repository tabs, global commands, current branch, sync state.
- Left navigation: history/worktree modes, search, branch groups, remotes, tags, submodules, worktrees.
- Primary workspace: resizable branch, graph, commit message, author, and time columns.
- Inspector or focused file view: commit information, changed files, and full-size diff/content.

## 4. Layout and Spacing

- Preserve compact desktop density: 24-32 px controls and 28 px list rows.
- Use 4 px as the base spacing unit; common gaps are 4, 8, 12, and 16 px.
- Keep major regions edge-to-edge and separated by 1 px dividers or splitters.
- Reserve corner radii for controls and overlays: 4 px for dense inputs, 6 px for grouped controls, 8 px maximum for popovers.
- Keep all table columns resizable and horizontally scrollable without moving the vertical scrollbar away from the content edge.

## 5. Typography

- Inter is the primary interface family; JetBrains Mono is used for SHA and code.
- Default working text remains compact. Use weight and contrast, not oversized type, to establish hierarchy.
- Column headers are uppercase, muted, and concise. Commit subjects remain the dominant text in each row.

## 6. Color and Material

- Base workspace: neutral near-black graphite, without a purple or blue cast.
- Content surfaces: opaque for history, diff, code, and long-form text.
- Chrome surfaces: translucent neutral material with a light upper highlight and subtle border.
- On macOS, chrome surfaces use the window blur backdrop with adaptive tint and restrained edge highlights; avoid painted reflections that make material look plastic.
- Accent: macOS system blue for focus, primary actions, and narrow active indicators.
- Graph colors remain saturated and varied because they encode topology.
- Selection uses a neutral translucent fill with an accent indicator where extra emphasis is needed.

## 7. Components

- Toolbars use translucent material and compact icon buttons with hover feedback.
- High-frequency sync commands use familiar symbols in one shared platter; the action that publishes local work receives the strongest tint inside that group.
- Sidebars use regular, text-safe material and retain strong group labels.
- Segmented controls use a shared container and a selected fill rather than standalone pills.
- Popovers use a stronger translucent surface, 8 px radius, 1 px highlight border, and restrained shadow.
- Buttons use rounded-rectangle geometry and distinct hover, pressed, focus, primary, secondary, and disabled states without adding a separate glass layer per button.
- Inputs use opaque text-safe fills with a visible focus ring; glass is reserved for navigation and command layers.
- Commit inspection is organized as Overview, Diff, and Tree; the Diff segment exposes its changed-file count without requiring a tab switch.
- History and diff tables use subtle row hover/selection fills and stable column geometry.

## 8. Interaction and Motion

- Hover clarifies clickability through luminance and border changes, not layout movement.
- Selection, focus, and active state use the system-blue accent consistently.
- SHA remains copyable by click and visible in tooltips on graph nodes.
- Escape exits focused file/diff mode and returns to the graph.
- Avoid decorative animation. Any transition must be short, reversible, and disabled when reduced motion is requested.

## 9. Accessibility

- Opaque fallbacks are mandatory when transparency is unavailable or reduced.
- Keep body text and graph data at high contrast over every material.
- Do not communicate status by color alone; retain icons, labels, or topology.
- Focus indicators must remain visible on keyboard navigation.
- Resizable panes and columns must retain practical minimum widths.

## 10. Responsive Desktop Behavior

- The app is desktop-first and supports narrow windows through resizable sidebars and horizontal table scrolling.
- At constrained widths, preserve graph and commit subject before secondary author/time metadata.
- The focused file view replaces the central graph workspace instead of opening in a narrow inspector.

## 11. Do Not

- Do not apply glass behind commit text, code, diff lines, or dense data tables.
- Do not tint every surface or use purple as the dominant selection color.
- Do not turn sections into floating cards or nest cards.
- Do not use oversized headings, decorative gradients, glowing blobs, or excessive pill controls.
- Do not trade information density for visual novelty.

## 12. Acceptance Checklist

- Top commands and navigation read as a distinct translucent chrome layer.
- Fetch, Pull, and Push remain immediately distinguishable by symbol and tooltip, and Push has clear primary emphasis.
- The left sidebar has material depth without reducing branch-name readability.
- The graph remains the strongest visual and its lines/nodes retain contrast.
- Selected rows use one consistent neutral translucent fill; focused controls retain the system-blue accent.
- Diff/code/history content remains opaque and readable.
- Inspector segments, buttons, inputs, menus, and push dialogs share one interaction-state vocabulary while using material only at the appropriate navigation or overlay layer.
- macOS window resizing, column resizing, scrolling, Escape behavior, SHA copy, and tooltips still work.
