## Why

Emmz speaks at tech conferences and wants a live anonymous Q&A / feedback channel: share a link (and QR code) at the start of a talk, collect anonymous messages from the audience during the session, then reveal them at the end on a projector-friendly inbox screen. No existing self-hosted tool fits this exact workflow.

## What Changes

- New greenfield ASP.NET Core 10 Razor Pages application
- Dockerised, deployable to Railway with a Postgres database
- Multiple inboxes per deployment, defined via a single `INBOXES` env var (e.g. `cph/hitc,cph/rtbc`)
- Public submission page at `/{conf}/{talk}` with optional sender name and embedded QR code
- Private inbox at `/{conf}/{talk}/inbox`, protected by a magic-link email (Resend)
- One authenticated session unlocks all configured inboxes
- Inbox UI: message list on the left, detail panel on the right, keyboard navigation, 5-second auto-refresh

## Capabilities

### New Capabilities

- `inbox-config`: Define and resolve multiple inboxes from a single `INBOXES` env var; 404 for unconfigured paths
- `message-submission`: Public page where anyone can submit an anonymous message (with optional name) to a specific inbox; includes auto-generated QR code of the page URL
- `magic-link-auth`: Owner requests a magic link sent to a configured email via Resend; clicking it creates a session cookie that grants access to all inboxes
- `inbox-viewer`: Authenticated projector-friendly inbox page — list + detail layout, keyboard navigation (↑↓), 5-second polling refresh, subtle sender-name label

### Modified Capabilities

## Impact

- New project: no existing code affected
- External dependencies: Resend (transactional email), Railway (hosting), Postgres (storage)
- Environment variables required: `OWNER_NAME`, `OWNER_EMAIL`, `INBOXES`, `RESEND_API_KEY`, `DATABASE_URL`, `SESSION_SECRET`
