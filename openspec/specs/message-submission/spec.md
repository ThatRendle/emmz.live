# message-submission Specification

## Purpose

Defines the public, unauthenticated submission page where audience members send anonymous messages: a mobile-friendly form with an optional name and a required message, an inline QR code encoding the page's own URL, validation of empty messages, and an in-place confirmation after a successful submission.

## Requirements

### Requirement: Public submission page is accessible without authentication
The system SHALL serve the submission page at `/{conf}/{talk}` to any visitor without requiring authentication.

#### Scenario: Anonymous visitor loads submission page
- **WHEN** a visitor navigates to a configured inbox URL
- **THEN** the system SHALL return HTTP 200 with the submission form

### Requirement: Submission form accepts an optional name and a required message
The system SHALL present a form with two fields: an optional "Name" text input and a required "Message" textarea.

#### Scenario: Visitor submits with name
- **WHEN** a visitor enters a name and a message and submits the form
- **THEN** the system SHALL store both the name and message associated with the inbox

#### Scenario: Visitor submits without name
- **WHEN** a visitor leaves the name field blank and submits a message
- **THEN** the system SHALL store the message with a null sender name

#### Scenario: Visitor submits empty message
- **WHEN** a visitor submits the form with an empty message body
- **THEN** the system SHALL reject the submission and display a validation error

### Requirement: Submission page displays a QR code of its own URL
The system SHALL render a QR code image on the submission page that encodes the full URL of that page (scheme + host + path).

#### Scenario: QR code renders inline
- **WHEN** the submission page is loaded
- **THEN** the system SHALL display a QR code as an inline base64-encoded PNG image

#### Scenario: QR code URL matches request URL
- **WHEN** the submission page is loaded at `https://emmz.live/cph/hitc`
- **THEN** the QR code SHALL encode `https://emmz.live/cph/hitc`

### Requirement: Successful submission shows a confirmation message
The system SHALL display a confirmation message after a successful submission without navigating away from the page.

#### Scenario: Successful submission feedback
- **WHEN** a visitor successfully submits a message
- **THEN** the system SHALL display a confirmation message and clear the form fields

### Requirement: Submission page has a minimal, fun visual design
The system SHALL render the submission page with a clean, minimal aesthetic suitable for display on a mobile device scanned from a conference slide.

#### Scenario: Mobile-friendly rendering
- **WHEN** the submission page is viewed on a mobile viewport
- **THEN** the layout SHALL be usable without horizontal scrolling
