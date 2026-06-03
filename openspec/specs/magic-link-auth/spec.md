# magic-link-auth Specification

## Purpose

Defines passwordless owner authentication: the owner requests a magic link sent to the configured `OWNER_EMAIL`, the link carries an HMAC-SHA256-signed, time-limited token, and successful verification establishes a persistent session cookie that grants access to all inboxes. Unauthenticated inbox requests are redirected to the request page.

## Requirements

### Requirement: Owner requests a magic link via a dedicated page
The system SHALL provide a page at `/auth/request` where the owner can request a magic link to be sent to the configured `OWNER_EMAIL` address.

#### Scenario: Owner requests magic link
- **WHEN** the owner submits the request form at `/auth/request`
- **THEN** the system SHALL send a magic link email to `OWNER_EMAIL` via Resend and display a confirmation message

#### Scenario: Request page shows no sensitive information
- **WHEN** the `/auth/request` page is loaded
- **THEN** the system SHALL NOT display the owner's email address in the page HTML

### Requirement: Magic link token is signed and time-limited
The system SHALL generate magic link tokens as HMAC-SHA256-signed values derived from `SESSION_SECRET` with a 15-minute expiry.

#### Scenario: Valid token verifies successfully
- **WHEN** the owner clicks a magic link within 15 minutes of generation
- **THEN** the system SHALL verify the token and create a session

#### Scenario: Expired token is rejected
- **WHEN** the owner clicks a magic link more than 15 minutes after generation
- **THEN** the system SHALL return an error page and SHALL NOT create a session

#### Scenario: Tampered token is rejected
- **WHEN** a request is made to `/auth/verify` with a token that fails HMAC verification
- **THEN** the system SHALL return HTTP 400 and SHALL NOT create a session

### Requirement: Successful verification creates a persistent session cookie
The system SHALL set an `HttpOnly`, `Secure`, `SameSite=Strict` session cookie named `anon-inbox-session` upon successful magic link verification. The cookie SHALL persist until the browser session ends.

#### Scenario: Cookie is set after verification
- **WHEN** the owner successfully verifies a magic link
- **THEN** the response SHALL include a `Set-Cookie` header for `anon-inbox-session`

#### Scenario: One session grants access to all inboxes
- **WHEN** the owner holds a valid session cookie
- **THEN** the owner SHALL be able to access all configured inbox URLs without re-authenticating

### Requirement: Unauthenticated inbox access redirects to auth request page
The system SHALL redirect any unauthenticated request to `/{conf}/{talk}/inbox` to `/auth/request`.

#### Scenario: Unauthenticated inbox access
- **WHEN** a visitor without a valid session cookie requests an inbox URL
- **THEN** the system SHALL redirect to `/auth/request`
