## Context

`emmz.live` is an ASP.NET Core 10 Razor Pages app. After the `anon-inbox` change, the public submission page (`/{conf}/{talk}`) is the only page that looks designed — it sets `Layout = null` and carries its own inline `<style>` (palette: system-ui, `#f5f5f5` background, white rounded card with soft shadow, `#4f6ef7` accent, `#1a1a1a` text, `#c0392b` error). Every other page renders through `Pages/Shared/_Layout.cshtml`, whose only stylesheet is `wwwroot/css/site.css` — leftover Bootstrap-template cruft that styles classes the app never uses. The auth pages and the default Index/Privacy/Error pages are therefore unstyled. The inbox page (`Pages/Inbox.cshtml`) is the exception: it carries a large projector `<style>` block inline in its body.

This is a presentation-only pass. No data, auth, routing, or behaviour changes.

## Goals / Non-Goals

**Goals:**
- One shared, vanilla-CSS design system in `site.css` defining a distinctive **violet + teal** brand palette and the **Outfit** typeface, applied to all `_Layout` pages.
- A `Styles` head section on `_Layout` for page-scoped CSS.
- Styled auth (Request/Verify), Index, and Error pages (centered card on the standard background).
- A full-bleed, chrome-free inbox shell for projection in a dark violet/teal variant, preserving all existing inbox behaviour.
- The submission page brought into the shared palette + font for a cohesive brand, with its layout/structure unchanged.
- Removal of scaffold remnants (Privacy page, dead/Bootstrap CSS).

**Non-Goals:**
- No structural redesign of the submission page — only its palette and font change to match the design system (its layout, fields, QR placement, and handlers stay).
- No data model, auth/token/cookie, routing, validation, QR-generation, or HTMX-endpoint changes.
- No new JS; HTMX stays the only JS dependency. No CSS framework.
- No theming/dark-mode toggle (the dark inbox is a fixed projector variant, not user-switchable), no design-token build tooling — just plain CSS custom properties.

## Decisions

### Decision: A shared `site.css` design system, not per-page inline styles

Rewrite `wwwroot/css/site.css` as a small design system: a `:root` block of CSS custom properties (single source of truth) for the brand palette/typography/spacing, then base rules for `body`, headings, links, `button`/submit controls, form inputs/textareas, validation messages, and a reusable `.card` container. `_Layout` pages get the look for free.

**Brand tokens** (define as custom properties; exact values are binding):
- Font: `--font: "Outfit", system-ui, -apple-system, sans-serif`.
- Light: `--bg #f7f5fc`, `--surface #ffffff`, `--text #1d1b2e`, `--muted #6b6880`, `--border #e4e0ee`, `--primary #7c3aed`, `--primary-hover #6d28d9`, `--accent #14b8a6`, `--error #dc2626`, plus a soft shadow `0 2px 10px rgba(29,27,46,.08)`.
- Dark projector: `--proj-bg #17132a`, `--proj-panel #1f1838`, `--proj-text #f4f1fb`, selected = violet `#7c3aed` glow + teal `#14b8a6` accent bar.

- **Why:** one source of truth; the auth/Index/Error pages need only semantic markup + minimal classes. Custom properties let the dark projector variant reuse the same accent hues.
- **Alternative considered — per-page inline `<style>`:** rejected; that is today's inconsistency (submission + inbox each inline their own) and duplicates the palette.
- **Submission page:** bring it into the shared tokens (palette + font) but keep its existing structure/inline layout rules; swap its hardcoded colors (`#4f6ef7`, `#f5f5f5`, system-ui) for the brand tokens/Outfit. Low risk (presentation only; behaviour is covered by `SubmitPageModelTests`, which don't assert CSS).

### Decision: Load Outfit from Google Fonts via `<link>`, with a system fallback

Load Outfit in the `<head>` of both layouts with `preconnect` to `fonts.googleapis.com` + `fonts.gstatic.com` (crossorigin) and a stylesheet link `…/css2?family=Outfit:wght@400;500;600;700&display=swap`. The font stack ends in `system-ui, -apple-system, sans-serif` so text renders immediately and the app stays usable if the font service is unreachable.

- **Why:** simplest faithful read of "use Outfit from Google Fonts"; `display=swap` avoids invisible text; the fallback keeps the self-hostable app functional offline.
- **Alternative considered — self-host the Outfit `.woff2` files** under `wwwroot`: more privacy-friendly / no third-party request and better aligns with the app's offline-resilient ethos, but adds binary assets and `@font-face` plumbing. Deferred — the `<link>` approach is a one-line swap to self-hosting later if desired. Flagged so the reviewer doesn't treat the CDN dependency as an oversight.

### Decision: Add a `Styles` head section to `_Layout`

Add `@await RenderSectionAsync("Styles", required: false)` inside `_Layout`'s `<head>`. Page-specific CSS (e.g. anything the inbox still needs beyond the shared sheet) goes here rather than in the body.

- **Why:** head is the correct place for CSS; keeps page bodies clean; `required: false` means pages that don't need it are unaffected.
- **Alternative — keep inline body `<style>`:** works in browsers but is poor practice and is what the spec explicitly moves away from.

### Decision: A dedicated minimal layout for the full-bleed inbox

Add a second layout, e.g. `Pages/Shared/_ProjectorLayout.cshtml` — a minimal HTML document (head with charset/viewport/title, the Outfit font links, the shared stylesheet link, `Styles` section, `@RenderBody()`, HTMX script, `Scripts` section) with **no** site header/nav/footer. `Inbox.cshtml` sets `Layout = "_ProjectorLayout"`. The inbox's projector CSS moves into the shared sheet (scoped under an `.inbox-*`/`.projector` namespace) or the inbox's `Styles` section, and adopts the dark violet/teal projector tokens.

- **Why:** cleanest way to drop site chrome for one page while keeping the normal shell everywhere else; the inbox owns the full viewport for projection. Keeps HTMX available (required for the 5s refresh).
- **Alternative — conditionally hide header/footer in `_Layout` via a ViewData flag:** rejected as muddier (`_Layout` grows branching for one page); a separate layout is clearer and isolates the chrome-free contract.
- **Alternative — keep `Layout = null` and a fully self-contained page (like Submit):** workable, but a named projector layout is reusable and keeps the HTMX script wiring in one place.

### Decision: Remove the scaffold Privacy page and Bootstrap remnants

Delete `Pages/Privacy.cshtml(.cs)` (unused scaffold) and ensure no Bootstrap selectors/classes survive in `site.css`, `_Layout`, or the styled pages. Verify no remaining markup references removed classes.

- **Why:** the proposal calls for no template remnants; a stray Privacy link/page and dead CSS are exactly that.
- **Note:** confirm nothing links to `/Privacy` before deleting (the scaffold footer sometimes does) and remove such links.

## Risks / Trade-offs

- **Inbox regression while moving CSS / switching layout** → The inbox has subtle, behaviour-coupled CSS (full-height two-panel via `100dvh`, the OOB count span, selection highlight). Mitigation: move CSS verbatim first (just relocate from body to head/shared sheet, unchanged), switch the layout, then verify the projector behaviours (keyboard nav, 5s refresh, selection preserved, ≥2rem detail) still work; the existing 106 tests cover the server-side behaviour, and the XSS-safety is in the unchanged JS (`textContent`).
- **Accidentally changing behaviour, not just presentation** → Keep edits to markup/CSS/layout only; do not touch PageModel handlers, `Program.cs`, filters, or the inbox JS logic (only its surrounding markup/classes). The spec's "existing tests still pass" + "anonymous input stays inert" scenarios gate this.
- **Full-bleed inbox hides the only nav** → Acceptable and intended; the inbox is a terminal projector screen. The owner reaches it via URL/sign-in, not nav.
- **Losing the submission page's good look** → Don't refactor it; leave its inline styles. Lowest-risk path.
- **Contrast/accessibility regressions** → Preserve focus-visible states and the dark/high-contrast projector palette; verify text contrast meets WCAG AA: `#1d1b2e` on `#f7f5fc` and `#f4f1fb` on `#17132a` are both high-contrast; check `#7c3aed`/`#14b8a6` only where used for large text or non-text UI, not small body copy on white.
- **Google Fonts dependency (privacy/offline)** → Loading Outfit from a third party adds an external request and a minor privacy consideration for a self-hostable app. Mitigation: `display=swap` + `system-ui` fallback means text always renders and the app works offline; self-hosting remains an easy future swap (noted in the font decision). Not a blocker for this pass.

## Migration Plan

Presentation-only; no data or deploy migration. Standard flow: implement on the change branch, keep `dotnet build`/`test`/`format` green, manual visual verification (HITL) of each page, then merge. Rollback = revert the change's commits (no schema/runtime state involved).

## Open Questions

- None blocking. Palette (violet + teal), typeface (Outfit via Google Fonts), full-bleed inbox, and including the submission page in the shared tokens are all decided. Self-hosting the font is a deferred, non-blocking future option.
