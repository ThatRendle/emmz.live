## 1. Project Scaffold

- [x] 1.1 Create ASP.NET Core 10 Razor Pages solution (`dotnet new webapp`)
- [x] 1.2 Add NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `QRCoder`, `Resend`
- [x] 1.3 Add HTMX via CDN script tag in `_Layout.cshtml`
- [x] 1.4 Create `Dockerfile` (multi-stage: build + runtime on `mcr.microsoft.com/dotnet/aspnet:10.0`)
- [x] 1.5 Create `docker-compose.yml` with app + postgres services for local development
- [x] 1.6 Create `.env.example` documenting all required env vars (`OWNER_NAME`, `OWNER_EMAIL`, `INBOXES`, `RESEND_API_KEY`, `DATABASE_URL`, `SESSION_SECRET`)

## 2. Data Model & Database

- [x] 2.1 Create `Inbox` entity with `Id` (GUID), `Slug` (string, unique), `CreatedAt`
- [x] 2.2 Create `Message` entity with `Id` (GUID), `InboxId` (FK), `SenderName` (nullable string), `Body` (string), `ReceivedAt`
- [x] 2.3 Create `AppDbContext` with both entities registered
- [x] 2.4 Add EF Core migration for initial schema
- [x] 2.5 Wire `Database.MigrateAsync()` in `Program.cs` to auto-migrate on startup

## 3. Inbox Configuration

- [x] 3.1 Create `InboxConfig` class that reads and parses `INBOXES` env var on startup; throw if missing/empty
- [x] 3.2 Register `InboxConfig` as a singleton in DI
- [x] 3.3 Log configured slugs at Information level in `Program.cs`
- [x] 3.4 Create `ValidInboxFilter` action filter that returns 404 for unrecognised `{conf}/{talk}` combinations

## 4. Message Submission Page

- [x] 4.1 Create `Pages/Submit.cshtml` + `Submit.cshtml.cs` with route `/{conf}/{talk}`
- [x] 4.2 Apply `ValidInboxFilter` to the page
- [x] 4.3 Implement `OnGetAsync`: resolve or create Inbox record, generate QR code (base64 PNG via QRCoder), pass to view
- [x] 4.4 Implement `OnPostAsync`: validate message body (required), save Message to DB, return page with confirmation flag
- [x] 4.5 Render form with optional Name input, required Message textarea, and submit button
- [x] 4.6 Render QR code as inline `<img src="data:image/png;base64,...">` below the form
- [x] 4.7 Show confirmation message on successful submission; clear form fields
- [x] 4.8 Apply minimal, mobile-friendly CSS to submission page

## 5. Magic Link Authentication

- [x] 5.1 Create `Pages/Auth/Request.cshtml` + `Request.cshtml.cs`
- [x] 5.2 Implement `OnPostAsync`: generate HMAC-SHA256 token (payload: email + expiry timestamp, key: `SESSION_SECRET`), send magic link email via Resend SDK, show confirmation
- [x] 5.3 Create `Pages/Auth/Verify.cshtml.cs` (page-less handler) at route `/auth/verify`
- [x] 5.4 Implement `OnGetAsync` on Verify: validate token signature and expiry; on success set `anon-inbox-session` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`); redirect to `/`; on failure return error page
- [x] 5.5 Create `AuthFilter` (or use ASP.NET Core cookie auth middleware) to protect inbox pages; redirect to `/auth/request` if no valid cookie
- [x] 5.6 Configure cookie authentication in `Program.cs` with scheme `anon-inbox-session`

## 6. Inbox Viewer Page

- [x] 6.1 Create `Pages/Inbox.cshtml` + `Inbox.cshtml.cs` with route `/{conf}/{talk}/inbox`
- [x] 6.2 Apply auth filter and `ValidInboxFilter` to the page
- [x] 6.3 Implement `OnGetAsync`: load all messages for the inbox ordered by `ReceivedAt` ascending, pass to view
- [x] 6.4 Create partial `Pages/Shared/_MessageList.cshtml` for the left panel (used by HTMX refresh)
- [x] 6.5 Implement HTMX partial endpoint `OnGetListAsync` returning only the `_MessageList` partial (for auto-refresh)
- [x] 6.6 Render two-panel layout: left panel with `_MessageList`, right panel with selected message detail
- [x] 6.7 Add HTMX attributes on list panel: `hx-get="?handler=List"`, `hx-trigger="every 5s"`, `hx-swap="innerHTML"`
- [x] 6.8 Add click handler on list items to update detail panel via JS (or HTMX `hx-target`)
- [x] 6.9 Add inline `<script>` for ArrowUp/ArrowDown keyboard navigation between messages
- [x] 6.10 Display total message count in the UI
- [x] 6.11 Apply projector-friendly CSS: large detail panel text (min 2rem), high contrast, readable from distance
- [x] 6.12 Style message list items with subtle sender-name label above preview text

## 7. Deployment

- [x] 7.1 Verify `Dockerfile` builds and runs correctly locally with `docker-compose up`
- [x] 7.2 Create `railway.json` or `railway.toml` with build and start configuration
- [x] 7.3 Document all required Railway env vars in `README.md`
