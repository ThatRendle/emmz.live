## MODIFIED Requirements

### Requirement: Submission form accepts an optional name and a required message
The system SHALL present a form with two fields: an optional "Name" text input limited to a maximum of 64 characters and a required "Message" textarea limited to a maximum of 512 characters. The system SHALL enforce these limits both in the browser (so input cannot exceed the maximum) and on the server (so an over-length submission is rejected).

#### Scenario: Visitor submits with name
- **WHEN** a visitor enters a name and a message and submits the form
- **THEN** the system SHALL store both the name and message associated with the inbox

#### Scenario: Visitor submits without name
- **WHEN** a visitor leaves the name field blank and submits a message
- **THEN** the system SHALL store the message with a null sender name

#### Scenario: Visitor submits empty message
- **WHEN** a visitor submits the form with an empty message body
- **THEN** the system SHALL reject the submission and display a validation error

#### Scenario: Name within limit is accepted
- **WHEN** a visitor submits a name of 64 characters or fewer
- **THEN** the system SHALL accept the name

#### Scenario: Over-length name is rejected on the server
- **WHEN** a submission reaches the server with a name longer than 64 characters
- **THEN** the system SHALL reject the submission as invalid and not store the message

#### Scenario: Message within limit is accepted
- **WHEN** a visitor submits a message of 512 characters or fewer
- **THEN** the system SHALL accept the message

#### Scenario: Over-length message is rejected on the server
- **WHEN** a submission reaches the server with a message body longer than 512 characters
- **THEN** the system SHALL reject the submission as invalid and not store the message

## ADDED Requirements

### Requirement: Each field shows a live character counter
The system SHALL display a live character counter beneath each of the Name and Message fields, right-aligned, in the form `[n/max]` where `n` is the current character count and `max` is that field's maximum length (64 for Name, 512 for Message). The counter SHALL update as the visitor types and SHALL turn the error colour when the field has reached its maximum length.

#### Scenario: Counter reflects current length
- **WHEN** a visitor has typed 14 characters into the Name field
- **THEN** the system SHALL display `[14/64]` beneath that field

#### Scenario: Counter starts at zero
- **WHEN** the submission page is first loaded with empty fields
- **THEN** the Name counter SHALL read `[0/64]` and the Message counter SHALL read `[0/512]`

#### Scenario: Counter signals the limit
- **WHEN** a field has reached its maximum length
- **THEN** the system SHALL render that field's counter in the error colour
