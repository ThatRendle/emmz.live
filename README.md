# emmz.live

A self-hostable anonymous-message inbox for conference talks. Share a link (and QR code) at the start of a session; the audience sends anonymous messages; reveal them on the projector-friendly inbox screen at the end. A single deployment serves multiple inboxes defined by the `INBOXES` env var.

Built with ASP.NET Core 10 Razor Pages, EF Core + PostgreSQL, and Resend for magic-link email.

---

## Environment variables

| Variable | Required | Description |
|---|---|---|
| `OWNER_NAME` | No | Display name shown as a greeting in magic-link emails (e.g. `Alice`). Optional — omitting it suppresses the greeting without failing startup. |
| `OWNER_EMAIL` | Yes | Email address that receives the magic link to sign in (e.g. `alice@example.com`). |
| `INBOXES` | Yes | Comma-separated list of inbox slugs in `{conf}/{talk}` form. Only these paths are served; anything else returns 404. Example: `cph/hitc,cph/rtbc`. |
| `RESEND_API_KEY` | Yes | Resend API key for sending magic-link emails. Get one at [resend.com](https://resend.com). |
| `MAIL_FROM` | Yes | Verified sender address for outbound email. Bare form (`noreply@yourdomain.com`) or display-name form (`emmz.live <noreply@yourdomain.com>`). The domain must be verified in your Resend account. |
| `DATABASE_URL` | Yes | PostgreSQL connection. Accepts Railway's URL form (`postgres://user:pass@host:port/db`) or Npgsql keyword form (`Host=...;Database=...;Username=...;Password=...`). |
| `SESSION_SECRET` | Yes | HMAC-SHA256 key for signing magic-link tokens. Must be a long random value. Generate with: `openssl rand -base64 32`. |

---

## Local development

**Prerequisites:** Docker and Docker Compose.

```bash
cp .env.example .env
# Edit .env and fill in real values (at minimum SESSION_SECRET, OWNER_EMAIL, RESEND_API_KEY, MAIL_FROM).
# INBOXES defaults to cph/hitc,cph/rtbc in docker-compose.yml — change it to match your talks.
docker-compose up
```

The app auto-runs EF Core migrations on startup. Once running:

| URL | Purpose |
|---|---|
| `/{conf}/{talk}` | Anonymous message submission form |
| `/auth/request` | Request a magic-link sign-in email |
| `/{conf}/{talk}/inbox` | Authenticated inbox view (projector-friendly) |

The Postgres data volume is persisted across restarts (`postgres_data`).

---

## Deploy to Railway

1. **Connect your repo** in the Railway dashboard.
2. Railway will detect `railway.json` and build using the `Dockerfile` automatically.
3. **Add a Postgres plugin** — Railway provides a `DATABASE_URL` env var in the `postgres://...` URL form, which the app accepts directly.
4. **Set the required env vars** listed in the table above (`OWNER_NAME`, `OWNER_EMAIL`, `INBOXES`, `RESEND_API_KEY`, `MAIL_FROM`, `SESSION_SECRET`). `DATABASE_URL` is provided by the Postgres plugin.
5. **PORT:** Railway injects a `$PORT` env var at runtime. The app honours it automatically (falling back to 8080 for local use). No manual port configuration is needed in Railway.

---

## Notes

- There is no moderation, message deletion, or admin UI. Messages are permanent.
- Authentication is a magic link delivered via Resend. One sign-in unlocks all configured inboxes.
- Inboxes are defined at deploy time via `INBOXES`; there is no runtime inbox creation.
