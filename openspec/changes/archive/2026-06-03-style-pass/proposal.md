## Why

The `anon-inbox` feature shipped functionally complete, but only the public submission page looks designed — it is self-contained (`Layout = null` with its own inline `<style>`). Every other page built on `_Layout` is effectively unstyled: the one stylesheet `_Layout` links (`wwwroot/css/site.css`) is leftover Bootstrap scaffold cruft targeting classes the app never uses, the magic-link auth pages and the default Index/Privacy/Error pages have no styling at all, and the inbox page carries its own projector CSS inline in its body. The app needs one cohesive look so the owner-facing pages (sign-in, inbox) are as polished as the audience-facing submission page.

## What Changes

- Replace the Bootstrap-cruft `wwwroot/css/site.css` with a real shared stylesheet (a small design system) defining a **distinctive violet + teal brand palette** and applied to all `_Layout`-based pages.
- Adopt the **Outfit** typeface (Google Fonts) app-wide, with a `system-ui` fallback, loaded in the layouts' `<head>`.
- Add a `Styles` render section to `_Layout`'s `<head>` so pages can contribute scoped head CSS cleanly.
- Style the magic-link auth pages (`Auth/Request`, `Auth/Verify`) as a centered card matching the shared look.
- Make the inbox viewer **full-bleed and chrome-free** for projection — no site header/nav/footer — via a dedicated minimal layout; restyle it in a dark violet/teal projector variant while keeping the existing projector behaviour (≥2rem high-contrast detail text, keyboard nav, 5s HTMX refresh, selection preservation) and relocating its CSS out of mid-body.
- Bring the public **submission page** into the shared design system — adopt Outfit + the violet/teal palette while keeping its existing structure/layout — so the whole app is visually cohesive.
- Give the landing (Index) and Error pages a clean look consistent with the design system.
- Remove leftover scaffold artifacts: the unused default Privacy page and any dead/Bootstrap CSS, so no template remnants remain.
- Presentation only — **no** changes to the data model, auth logic, routing, validation, or page behaviour.

## Brand tokens (single source of truth)

- **Font:** `Outfit` (Google Fonts, weights 400/500/600/700) → fallback `system-ui, -apple-system, sans-serif`.
- **Light (owner pages + submission):** background `#f7f5fc`, card `#ffffff`, text `#1d1b2e`, muted `#6b6880`, border `#e4e0ee`, primary (buttons/links/focus) `#7c3aed`, primary-hover `#6d28d9`, accent (highlights/sender tags) `#14b8a6`, error `#dc2626`.
- **Dark projector (inbox):** background `#17132a`, panel `#1f1838`, text `#f4f1fb`; selected message = violet (`#7c3aed`) glow with a teal (`#14b8a6`) accent bar.

## Capabilities

### New Capabilities

- `app-styling`: A cohesive presentation layer for the application — a shared CSS design system and layout shells that give every page a consistent, accessible look, including a full-bleed projector shell for the inbox and the removal of scaffold styling remnants.

### Modified Capabilities

<!-- None. The existing anon-inbox capabilities are not yet promoted to openspec/specs/
     (the change is unarchived), and this pass changes only presentation, not their
     spec-level behaviour/requirements. -->

## Impact

- **Affected files:** `wwwroot/css/site.css` (rewritten), `Pages/Shared/_Layout.cshtml` (Outfit font + Styles section, refine header/footer), a new minimal projector layout, `Pages/Inbox.cshtml` (use minimal layout, relocate CSS, dark variant), `Pages/Auth/Request.cshtml` + `Verify.cshtml` (markup/classes), `Pages/Index.cshtml` + `Error.cshtml` (styling), `Pages/Submit.cshtml` (palette + font, structure unchanged), removal of `Pages/Privacy.cshtml(.cs)`.
- **No** changes to: entities, EF/migrations, `Program.cs` behaviour, auth/token/cookie logic, `ValidInboxFilter`, QR generation, HTMX endpoints, or any page's **behaviour/handlers** (the submission page's markup structure and handlers stay; only its palette/font change).
- **Constraints:** vanilla CSS only (no CSS frameworks); HTMX remains the only **JS** dependency; the only new external dependency is the **Outfit web font via Google Fonts** (loaded by `<link>`, `display=swap`, with a `system-ui` fallback so the app degrades gracefully and could be self-hosted later). Must not regress the 106 existing tests, the inbox XSS-safety (anonymous input stays auto-encoded; detail panel via `textContent`, never `innerHTML`), antiforgery, or proxy-aware URLs; build stays warning-clean (`TreatWarningsAsErrors`) and `dotnet format` clean; preserve accessibility (focus states, contrast, semantic markup).
