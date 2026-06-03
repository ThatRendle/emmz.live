# inbox-config Specification

## Purpose

Defines how a single deployment serves multiple inboxes: inbox slugs are configured via the `INBOXES` environment variable, parsed and logged at startup, validated per request so that only configured paths resolve (others return 404), and persisted to the database on first use.

## Requirements

### Requirement: Inboxes are defined by environment variable
The system SHALL read the `INBOXES` environment variable at startup and parse it as a comma-separated list of path slugs (e.g. `cph/hitc,cph/rtbc`). Each slug identifies a unique inbox and defines its public URL path.

#### Scenario: Valid INBOXES env var
- **WHEN** `INBOXES=cph/hitc,cph/rtbc` is set
- **THEN** the system recognises `cph/hitc` and `cph/rtbc` as valid inbox slugs

#### Scenario: Missing INBOXES env var
- **WHEN** `INBOXES` is not set or is empty
- **THEN** the application SHALL fail to start with a descriptive error message

### Requirement: Unconfigured paths return 404
The system SHALL return HTTP 404 for any `/{conf}/{talk}` or `/{conf}/{talk}/inbox` path that does not match a configured inbox slug.

#### Scenario: Request to unconfigured path
- **WHEN** a request is made to `/unknown/path`
- **THEN** the system SHALL return HTTP 404

#### Scenario: Request to configured path
- **WHEN** a request is made to `/cph/hitc` and `cph/hitc` is in `INBOXES`
- **THEN** the system SHALL return HTTP 200

### Requirement: Inboxes are auto-created in the database on first use
The system SHALL create the Inbox record in the database the first time a request is made to a configured slug, if it does not already exist.

#### Scenario: First submission to a new inbox
- **WHEN** a message is submitted to a configured inbox slug that has no database record yet
- **THEN** the system SHALL create the Inbox record and associate the message with it

### Requirement: Configured slugs are logged at startup
The system SHALL log the list of configured inbox slugs at application startup.

#### Scenario: Application starts with valid config
- **WHEN** the application starts successfully
- **THEN** the system SHALL log each configured slug at Information level
