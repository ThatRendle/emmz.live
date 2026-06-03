## 1. Shared design system

- [x] 1.1 Rewrite `wwwroot/css/site.css` as a vanilla-CSS design system: a `:root` block of custom properties for the brand tokens — font stack `"Outfit", system-ui, -apple-system, sans-serif`; light palette `--bg #f7f5fc`, `--surface #ffffff`, `--text #1d1b2e`, `--muted #6b6880`, `--border #e4e0ee`, `--primary #7c3aed`, `--primary-hover #6d28d9`, `--accent #14b8a6`, `--error #dc2626`, soft shadow `0 2px 10px rgba(29,27,46,.08)`, plus radius/spacing tokens; and dark projector tokens `--proj-bg #17132a`, `--proj-panel #1f1838`, `--proj-text #f4f1fb`. Then base rules for `body`, headings, links, `button`/submit controls, form `input`/`textarea` (incl. `:focus`/`focus-visible` rings in `--primary`), validation messages (`--error`), and a reusable `.card` container. Remove all Bootstrap-scaffold selectors (`.btn`, `.form-floating`, `.nav-link`, `.navbar-brand`, etc.).
- [x] 1.2 Add the Outfit font to `Pages/Shared/_Layout.cshtml`'s `<head>`: `preconnect` to `fonts.googleapis.com` and `fonts.gstatic.com` (crossorigin) + a stylesheet link `https://fonts.googleapis.com/css2?family=Outfit:wght@400;500;600;700&display=swap`. Also add `@await RenderSectionAsync("Styles", required: false)` after the shared stylesheet link, and give `_Layout`'s header/nav/footer a clean look using the design system.
- [x] 1.3 Verify a `_Layout`-based page (e.g. Index) now renders in Outfit with the brand palette, and that no markup references removed Bootstrap classes.

## 2. Owner-facing pages

- [x] 2.1 Style `Pages/Auth/Request.cshtml` as a centered `.card` using the shared system: styled heading, submit button, and the confirmation / send-failure messages.
- [x] 2.2 Style `Pages/Auth/Verify.cshtml` (invalid/expired states) as a centered `.card` with a styled "request a new sign-in link" action.
- [x] 2.3 Style `Pages/Index.cshtml` (landing) consistently with the design system — no raw-HTML appearance.
- [x] 2.4 Style `Pages/Error.cshtml` using the design system.
- [x] 2.5 Bring `Pages/Submit.cshtml` into the brand: swap its inline palette (`#4f6ef7`, `#f5f5f5`, `#1a1a1a`, system-ui) for the shared tokens/Outfit (use `var(--…)`), keeping its existing structure, fields, QR placement, and behaviour. Ensure it still works as a self-contained page (it may consume the shared tokens by linking `site.css` or by referencing the same hex values; keep it cohesive with the rest).

## 3. Full-bleed projector inbox

- [ ] 3.1 Create `Pages/Shared/_ProjectorLayout.cshtml`: a minimal HTML document (charset/viewport/title, the Outfit font links, shared stylesheet link, `Styles` section, `@RenderBody()`, the HTMX script, `Scripts` section) with NO site header/nav/footer.
- [ ] 3.2 Set `Layout = "_ProjectorLayout"` on `Pages/Inbox.cshtml`, move its projector CSS out of the body `<style>` into the shared stylesheet (namespaced) and/or the page's `Styles` section, and apply the dark violet/teal projector tokens (`--proj-bg`/`--proj-panel`/`--proj-text`; selected message = violet glow + teal accent bar).
- [ ] 3.3 Confirm the inbox now fills the viewport with no site chrome, and that the two-panel layout, ≥2rem high-contrast detail text, the message-count badge (incl. the OOB refresh), and the empty state all render correctly.
- [ ] 3.4 Self-verify (and flag for HITL) that the unchanged inbox JS still works: click-to-detail via `textContent`, ArrowUp/ArrowDown boundary-clamped nav, 5s HTMX refresh, and selected-message preservation across refresh.

## 4. Remove scaffold remnants

- [ ] 4.1 Delete `Pages/Privacy.cshtml` and `Pages/Privacy.cshtml.cs`; remove any link/reference to `/Privacy` (e.g. in `_Layout`).
- [ ] 4.2 Grep the project for leftover Bootstrap/scaffold class names and dead CSS and remove them; confirm `wwwroot/css/site.css` contains only used rules.

## 5. Verification

- [ ] 5.1 `dotnet build` clean (0 warnings, `TreatWarningsAsErrors`); `dotnet test` green (all 106 existing tests); `dotnet format --verify-no-changes` clean.
- [ ] 5.2 `openspec validate style-pass --strict` passes.
- [ ] 5.3 Manual (HITL) visual pass: every page renders in Outfit with the violet/teal brand; submission page rebranded but structurally unchanged; auth Request/Verify, Index, Error look cohesive; inbox is full-bleed/chrome-free, dark violet/teal, legible at projector distance with the selected message clearly highlighted; an HTML/script message body renders as inert text in list and detail.
