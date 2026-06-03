## Context

The public submission page (`Submit.cshtml`) is a single, layout-less Razor Page with inline `<style>` and **no JavaScript** today. The Name input and Message textarea carry `maxlength="100"` / `maxlength="5000"` and matching `[MaxLength]` attributes on `SubmitModel`. There is no character-count feedback. This change tightens the limits (64 / 512) and adds a live `[n/max]` counter under each field. Per the project's frontend convention, any client behaviour must be **vanilla JS + CSS, no frameworks**.

## Goals / Non-Goals

**Goals:**
- Enforce Name ≤ 64 and Message ≤ 512 on both client (`maxlength`) and server (`[MaxLength]`).
- Show a live `[n/max]` counter beneath each field, right-aligned, that updates on input and turns the error colour at the limit.
- Keep the page self-contained (inline style + small inline script), matching the existing file's style.

**Non-Goals:**
- No database schema/migration changes (text columns are unconstrained; existing rows untouched).
- No change to the empty-message required validation, QR code, confirmation flow, or layout.
- No new dependencies; no shared JS asset or external script file.

## Decisions

**Decision: Counter placement — below the field, right-aligned (not overlaid).**
A block element under each input, right-aligned, avoids overlapping the textarea's text and scrollbar and reads cleanly on a mobile viewport. Rationale over an absolutely-positioned in-corner badge: the textarea is multi-line and resizable, so an overlay would collide with content. Confirmed with the user.

**Decision: Small inline vanilla-JS counter, progressive-enhancement.**
A short inline `<script>` wires each field to its counter via an `input` listener, updates `[n/max]`, and toggles an `at-limit` class when `value.length >= max`. The counters render server-side with the correct initial value (`[0/64]`, `[0/512]`), so with JS disabled the form still works and the static limits still display — the script only adds live updates. Rationale over a separate JS file: the page is deliberately self-contained and layout-less; a one-off ~15-line script belongs inline like the existing styles. Counts use JS string `.length` (UTF-16 code units), which matches the browser's own `maxlength` accounting, so the displayed count and the hard stop never disagree.

**Alternative considered: pure-CSS counter (rejected — not possible).**
A live `[n/max]` counter cannot be built in CSS. The denominator is reachable — `content: attr(maxlength)` pulls the max in — but the numerator is not: as the visitor types, the browser updates the input's IDL property (`el.value`), **not** the `value` content attribute, and CSS attribute selectors / `attr()` only ever see the (frozen) content attribute. CSS also has no character-counting primitive (its counters count elements, not characters in a field). Pure CSS can only detect empty-vs-not (`:placeholder-shown`) or an exact-length "full" state via `minlength == maxlength` + `:valid` (which hijacks form validity and yields a boolean, not a number). So the current count is the one value the platform keeps out of CSS's reach, and reading `el.value.length` in a tiny JS handler is unavoidable. This is why 1.4 uses inline JS rather than a CSS-only approach.

**Decision: Server limits via `[MaxLength]`, same as today.**
Lower the existing `[MaxLength(100)]`/`[MaxLength(5000)]` to `[MaxLength(64)]`/`[MaxLength(512)]`. The `maxlength` HTML attribute is the primary UX guard; `[MaxLength]` is the server-side backstop against crafted over-length posts. Rationale: reuses the existing validation mechanism and `validation-message` display; no new code path.

## Risks / Trade-offs

- **[Counts diverge for astral/emoji characters]** → JS `.length` and HTML `maxlength` both count UTF-16 code units, so they agree with each other; `[MaxLength]` on the server counts .NET `string` length (also UTF-16 code units), so all three agree. Accepted: a 4-byte emoji counts as 2 — consistent across client display, client stop, and server check.
- **[JS disabled → no live counter]** → Static `[0/64]`/`[0/512]` still render and `maxlength` still enforces the cap; only the live number is lost. Acceptable degradation.
- **[Tightening limits could reject longer drafts]** → Limits are enforced going forward only; no stored data is validated or migrated. The page clears fields after a successful post, so there is no long-lived draft to truncate.

## Migration Plan

Pure code change; deploy normally. Rollback is reverting the commit. No data migration, no env-var or config change.
