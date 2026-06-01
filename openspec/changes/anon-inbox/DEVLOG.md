# DEVLOG — anon-inbox

Working notes kept by the orchestrator while applying the change. Per-section status,
decisions made under uncertainty, deviations, and human-in-the-loop verifications.

## Pinned facts / cross-section constraints

- **Project layout:** solution `EmmzLive.slnx` at root; web project `src/EmmzLive/`; tests `tests/EmmzLive.Tests/`.
- **Canonical slug:** `ValidInboxFilter` writes the *configured-form* slug to
  `HttpContext.Items[ValidInboxFilter.InboxSlugKey]` (`"InboxSlug"`). All DB work (get-or-create
  inbox, message insert, inbox lookup) MUST use that value, never the raw `{conf}`/`{talk}` route
  values — matching is case-insensitive, so raw values would split rows across casings.
- **Proxy-aware URLs:** `UseForwardedHeaders` (XForwardedFor|Proto, known proxies/networks cleared)
  is the first middleware so `Request.Scheme`/`Host` are correct behind Railway's TLS-terminating
  proxy. QR codes and the magic-link verify URL are built from the request.
- **Env vars:** `OWNER_NAME`, `OWNER_EMAIL`, `INBOXES`, `RESEND_API_KEY`, `DATABASE_URL`,
  `SESSION_SECRET`, plus **`MAIL_FROM`** (added section 5 — see below). Required vars fail fast at
  startup. `DATABASE_URL` accepts Railway URL form (`postgres://…`) or Npgsql keyword form.
- **Tests:** xUnit + EFCore.InMemory; EFCore packages pinned to 10.0.8 in the test project to avoid
  MSB3277 against Npgsql.EFCore.PostgreSQL 10.0.2's transitive 10.0.4. `[InternalsVisibleTo]`
  exposes internal handler methods.

## Section status

- **1. Project Scaffold** — done (`246161a`). Dropped jQuery/Bootstrap (HTMX-only); htmx 2.0.4 via
  unpkg with SRI. `.gitattributes` `eol=lf`.
- **2. Data Model & Database** — done (`354e7c1`). `DateTimeOffset`→timestamptz. Design-time
  migrations via `IDesignTimeDbContextFactory` (placeholder conn). `Program.cs` fails fast when
  `DATABASE_URL` unset (no silent localhost fallback). `ConnectionStringHelper` maps `sslmode`,
  scheme-based URL detection.
- **3. Inbox Configuration** — done (`3c76cfc`). Case-insensitive slug match + canonicalization
  (see pinned constraint). Fail-fast on missing/empty `INBOXES`.
- **4. Message Submission Page** — done (`c079db2`). `PngByteQRCode` (no System.Drawing). Get-or-
  create with DbUpdateException race fallback. Antiforgery on; anonymous input auto-encoded;
  body ≤5000 / name ≤100 chars.
  - HITL pending: QR scan with a real phone (deferred to deployment/manual verify).
- **5. Magic Link Authentication** — done (pending commit).
  - **Decision — MAIL_FROM env var (user-approved this session):** Resend requires a verified-domain
    sender, which none of the original 6 env vars provided. Added required `MAIL_FROM`
    (e.g. `emmz.live <noreply@emmz.live>`), fail-fast if unset, documented in `.env.example`.
    README env list to be completed in section 7.
  - **Decision — task 5.5 scope (orchestrator call):** 5.5 ("protect inbox pages; redirect to
    /auth/request") is split by the task decomposition: section 5 builds the *mechanism* (ASP.NET
    cookie auth, scheme/cookie `anon-inbox-session`, `LoginPath=/auth/request`, no global fallback
    policy so public pages stay anonymous); **task 6.2 applies `[Authorize]` to the inbox page**.
    The end-to-end unauthenticated-redirect is therefore verified in section 6, not here — a
    WebApplicationFactory test in section 5 would need a live Postgres (startup `MigrateAsync`),
    not worth it. 5.5 ticked as mechanism-complete; section-6 reviewer confirms the redirect.
  - Token: HMAC-SHA256 over base64url(`{email}:{unixExpiry}`), keyed by `SESSION_SECRET`;
    constant-time verify (`CryptographicOperations.FixedTimeEquals`); 15-min expiry; verified email
    must equal `OWNER_EMAIL` (OrdinalIgnoreCase). Bad signature/format → 400; expired → error page;
    neither creates a session. Session cookie (`IsPersistent=false`) HttpOnly/Secure/SameSite=Strict.
  - Email send wrapped behind `IMagicLinkEmailSender` (Resend impl); transient send failure renders
    a generic retry message (no email/token/secret leak), logged via ILogger.
  - **HITL pending — real email delivery:** cannot be tested automatically. Verify with real
    `RESEND_API_KEY` + verified `MAIL_FROM`: run the app, visit `/auth/request`, click send, confirm
    the email arrives at `OWNER_EMAIL` within 15 min and the verify link signs in + sets the cookie.
- **6. Inbox Viewer Page** — done (pending commit). `[Authorize(scheme anon-inbox-session)]` +
  `[ServiceFilter(ValidInboxFilter)]` on the page model cover both `OnGetAsync` and the
  `OnGetListAsync` poll handler — this closes 5.5's redirect end-to-end.
  - **XSS (highest-risk surface — audited clean):** anonymous Body/SenderName emitted only via
    auto-encoded Razor; click-to-detail JS carries body/name in Razor-encoded `data-*` attrs and
    writes the detail panel via `textContent` only (never `innerHTML`); newlines via CSS
    `white-space: pre-wrap`. Reviewer traced `<img onerror>`, `</script>`, `${}`/`{{}}` payloads
    through list/detail/post-swap — all inert.
  - Messages loaded by canonical slug (Items), ordered `ReceivedAt` ascending in both handlers;
    missing inbox row → empty, no 500. Selection preserved across the 5s swap via `htmx:afterSwap`
    re-select by id + event delegation. Keyboard ArrowUp/Down with boundary clamps.
  - **Count staleness fix:** count badge updated on each poll via `hx-swap-oob` span emitted ONLY
    in the `OnGetListAsync` response (gated by `ViewData["IncludeOobCount"]`) — initial inline
    render keeps a single shell `id="msg-count"` (avoided a duplicate-id / stray-span bug).
  - HITL pending (needs a running app + browser/projector): unauthenticated→/auth/request redirect,
    click + ArrowUp/Down nav, 5s auto-refresh visual, projector legibility.
- **7. Deployment** — not started (includes HITL: docker build, QR phone scan, projector legibility,
  real magic-link email).

## Consolidated human-in-the-loop verification (to run before archive)

All require a running app (`docker-compose up` with env vars set). To present to the user as one pass:
1. **Docker build/run** (7.1): `docker-compose up` builds and serves.
2. **Submission + QR** (4.x): visit `/cph/hitc`, submit a message; scan the QR with a phone → resolves
   to the same URL.
3. **Magic-link email** (5.2): `/auth/request` → real email arrives via Resend within 15 min; link
   signs in + sets cookie; expired/tampered link rejected.
4. **Auth redirect** (5.5/6.2): unauthenticated `/cph/hitc/inbox` → redirects to `/auth/request`.
5. **Inbox UX** (6.x): two-panel layout; click + ArrowUp/Down navigation; new submission appears
   within ~5s without reload and selection is preserved; count updates; legible on a projector.
