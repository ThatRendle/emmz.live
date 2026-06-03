# inbox-viewer Specification

## Purpose

Defines the authenticated, projector-friendly inbox screen where the owner reveals received messages: an authentication-gated two-panel layout with a scrollable message list and a large-text detail panel, sender-name labelling, click and keyboard navigation, a 5-second auto-refresh that preserves the selection, projector-optimised typography, and a visible message count.

## Requirements

### Requirement: Inbox page requires authentication
The system SHALL protect `/{conf}/{talk}/inbox` behind the session cookie. Unauthenticated requests SHALL be redirected to `/auth/request`.

#### Scenario: Authenticated owner accesses inbox
- **WHEN** the owner with a valid session cookie requests an inbox URL
- **THEN** the system SHALL return HTTP 200 with the inbox page

### Requirement: Inbox displays messages in a two-panel layout
The system SHALL render the inbox as a two-column layout: a scrollable message list on the left and a detail panel on the right.

#### Scenario: Multiple messages exist
- **WHEN** the inbox contains messages
- **THEN** the left panel SHALL list all messages and the right panel SHALL display the selected message in large text

#### Scenario: No messages exist
- **WHEN** the inbox contains no messages
- **THEN** the system SHALL display an empty state in the list panel

### Requirement: Message list shows sender name as a subtle label
The system SHALL display the sender name above the message preview in the list if present.

#### Scenario: Named message in list
- **WHEN** a message has a sender name
- **THEN** the list item SHALL display the name as a subtle label above the message text

#### Scenario: Anonymous message in list
- **WHEN** a message has no sender name
- **THEN** the list item SHALL display no name label

### Requirement: Clicking a message displays it in the detail panel
The system SHALL update the right-hand detail panel to show the full text of the selected message when a list item is clicked.

#### Scenario: Owner clicks a message
- **WHEN** the owner clicks a message in the list
- **THEN** the detail panel SHALL display the full message body in large, readable text, and the sender name if present

### Requirement: Keyboard navigation moves between messages
The system SHALL support ArrowUp and ArrowDown keys to move the selection through the message list.

#### Scenario: Arrow down
- **WHEN** the owner presses ArrowDown
- **THEN** the next message in the list SHALL become selected and displayed in the detail panel

#### Scenario: Arrow up
- **WHEN** the owner presses ArrowUp
- **THEN** the previous message in the list SHALL become selected and displayed in the detail panel

#### Scenario: Navigation at boundary
- **WHEN** the owner presses ArrowDown on the last message or ArrowUp on the first
- **THEN** the selection SHALL remain on the current message

### Requirement: Inbox list auto-refreshes every 5 seconds
The system SHALL refresh the message list panel every 5 seconds without reloading the full page or losing the currently selected message.

#### Scenario: New message arrives during session
- **WHEN** a new message is submitted while the inbox page is open
- **THEN** it SHALL appear in the list within 10 seconds without the owner taking any action

#### Scenario: Refresh preserves selected message
- **WHEN** the list refreshes and the currently selected message still exists
- **THEN** the detail panel SHALL continue to display the same message

### Requirement: Inbox is optimised for projector display
The system SHALL use large, high-contrast typography in the detail panel suitable for reading from the back of a conference room.

#### Scenario: Detail panel typography
- **WHEN** a message is displayed in the detail panel
- **THEN** the message text SHALL be rendered at a font size appropriate for projection (minimum 2rem)

### Requirement: Inbox displays total message count
The system SHALL display the total number of messages received in the inbox.

#### Scenario: Message count visible
- **WHEN** the inbox page is loaded
- **THEN** the total message count SHALL be visible in the UI
