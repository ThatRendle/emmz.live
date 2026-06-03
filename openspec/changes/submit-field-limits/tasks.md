## 1. Field limits and live character counters

- [x] 1.1 In `Submit.cshtml.cs`, lower `[MaxLength(100)]` on `SenderName` to `[MaxLength(64)]` and `[MaxLength(5000)]` on `Body` to `[MaxLength(512)]`.
- [x] 1.2 In `Submit.cshtml`, change the Name input `maxlength` to `64` and the Message textarea `maxlength` to `512`.
- [x] 1.3 In `Submit.cshtml`, add a right-aligned counter element beneath each field, rendered server-side with its initial value (`[0/64]` for Name, `[0/512]` for Message), and add the CSS for the counter (muted colour, right-aligned, small) plus an `at-limit` state using the existing error colour token.
- [x] 1.4 In `Submit.cshtml`, add a small inline vanilla-JS script that, on `input`, updates each counter to `[n/max]` and toggles the `at-limit` class when the field is at its maximum length; ensure it sets correct counts on load (in case fields are pre-filled).
- [x] 1.5 Update `SubmitPageModelTests.cs`: adjust existing length expectations and add tests that a 64-char name and 512-char message are accepted while a 65-char name and 513-char message are rejected (ModelState invalid, no message stored).
- [x] 1.6 Run gates: `dotnet build` clean, `dotnet test` green, `dotnet format --verify-no-changes` clean, `openspec validate submit-field-limits --strict`.
