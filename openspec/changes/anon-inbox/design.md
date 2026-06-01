## Context

New greenfield ASP.NET Core 10 Razor Pages application. Emmz speaks at tech conferences and wants a tool to collect anonymous audience messages during a talk and display them dramatically at the end. The app must be self-hostable by others.

Stack: ASP.NET Core 10, Razor Pages, EF Core + Postgres, Resend for email, Docker, Railway.

## Goals / Non-Goals

**Goals:**
- Minimal, deployable-in-minutes self-hosted app
- Multiple inboxes per deployment via `INBOXES` env var
- Public submission page with QR code
- Projector-friendly inbox UI with keyboard navigation
- Magic-link authentication for inbox access

**Non-Goals:**
- Multi-user accounts or per-user registration
- Real-time WebSockets (5-second polling is sufficient)
- Message moderation or deletion
- Reply or share-to-social mechanics
- Admin UI for managing inboxes (config-only)

## Decisions

### Routing: Dynamic Razor Pages via route constraints

Inboxes are defined in config as `cph/hitc,cph/rtbc`. At startup, the app parses `INBOXES` and registers the valid set. All `/{conf}/{talk}` requests check against this set; unrecognised paths return 404.

Rather than code-generating pages, a single parameterised Razor Page (`/Pages/Submit.cshtml` at route `/{conf}/{talk}`) and a single inbox page (`/Pages/Inbox.cshtml` at route `/{conf}/{talk}/inbox`) handle all inboxes. A custom `IPageRouteModelConvention` or a catch-all route with a `ValidInboxFilter` attribute keeps routing clean.

**Alternative considered**: separate Razor Page per inbox — rejected because it requires code generation and doesn't scale.

### Auth: Cookie session + Resend magic link

The owner triggers a magic link by visiting `/auth/request`. The app sends an email via Resend containing a signed token (HMAC-SHA256, 15-minute expiry) as a query parameter to `/auth/verify?token=...`. On verification, a persistent `HttpOnly` secure cookie (`anon-inbox-session`) is set. One cookie grants access to all inboxes.

**Alternative considered**: JWT in localStorage — rejected because `HttpOnly` cookie is more secure for a single-owner tool.

**Alternative considered**: per-inbox passwords — rejected as too cumbersome for live conference use.

### Data model

```
Inbox
  Id          GUID PK
  Slug        TEXT UNIQUE  -- "cph/hitc"
  CreatedAt   TIMESTAMPTZ

Message
  Id          GUID PK
  InboxId     GUID FK → Inbox
  SenderName  TEXT NULL    -- optional
  Body        TEXT NOT NULL
  ReceivedAt  TIMESTAMPTZ
```

Inboxes are created automatically on first use if they match a configured slug. This avoids a separate migration/seed step.

### QR Code: server-side via QRCoder NuGet package

Generated as an inline base64 PNG on the submission page. No external service — keeps the page self-contained and offline-resilient.

### Auto-refresh: meta refresh + HTMX swap

The inbox list refreshes every 5 seconds. Options:
- `<meta http-equiv="refresh">` — simplest, reloads whole page (bad for UX while reading)
- HTMX `hx-trigger="every 5s"` on the message list — partial swap, preserves selected message state

**Decision**: HTMX for the list panel refresh. Keeps selected message stable while new messages appear. HTMX is the only JS dependency.

### Keyboard navigation

Handled via a small inline `<script>` on the inbox page: `keydown` listener maps ArrowUp/ArrowDown to click the prev/next list item. No framework needed.

## Risks / Trade-offs

- **Magic link expiry during talk**: 15-minute token expiry means the owner must click the link and open the inbox before the token expires. Mitigation: session cookie persists until browser close, so click once before the talk starts.
- **INBOXES misconfiguration**: A typo in `INBOXES` silently creates a dead path. Mitigation: app logs configured slugs at startup.
- **Postgres on Railway cold start**: First request after inactivity may be slow. Mitigation: Railway hobby plan keeps containers warm; acceptable for this use case.
- **QR code accuracy**: QRCoder generates correct QR codes but the URL must be correct (HTTPS, correct domain). Mitigation: URL is derived from `Request.Scheme + Host` at render time.

## Migration Plan

1. Deploy Docker image to Railway, set env vars
2. App runs EF Core migrations on startup (`Database.MigrateAsync()` in `Program.cs`)
3. No data migration needed (greenfield)
4. Rollback: redeploy previous image tag

## Open Questions

- None — all decisions resolved during discovery.
