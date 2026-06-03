## Why

The submission form's current length limits are arbitrarily loose (Name 100, Message 5000) and there is no feedback on how much room remains, so an audience member typing on a phone has no idea they are near a limit until input silently stops. Tighter, purposeful limits plus a live character counter make the form's expectations obvious and keep stored messages projector-friendly.

## What Changes

- Lower the Name field length limit from 100 to **64** characters (client `maxlength` and server `MaxLength`).
- Lower the Message field length limit from 5000 to **512** characters (client `maxlength` and server `MaxLength`).
- Add a live character counter below each field, right-aligned, in the form `[n/max]` (e.g. `[14/64]`), that updates as the visitor types.
- The counter turns the error colour when the field is at its maximum length.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `message-submission`: the form's Name and Message fields gain explicit maximum lengths (64 / 512) and a live character-count indicator; the empty-message validation behaviour is unchanged.

## Impact

- `src/EmmzLive/Pages/Submit.cshtml` — `maxlength` attributes on the Name input and Message textarea, counter markup/CSS, and a small inline vanilla-JS counter script.
- `src/EmmzLive/Pages/Submit.cshtml.cs` — `[MaxLength]` values on `SenderName` (64) and `Body` (512).
- `tests/EmmzLive.Tests/Pages/SubmitPageModelTests.cs` — adjust/extend validation tests for the new limits.
- No database migration: `SenderName`/`Body` columns are unconstrained text; existing stored messages are unaffected. No new dependencies.
