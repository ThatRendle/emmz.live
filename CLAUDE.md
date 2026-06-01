# emmz.live

emmz.live is a self-hostable anonymous-message inbox for conference talks: a speaker shares a link (and QR code) at the start of a session, the audience sends anonymous messages, and the speaker reveals them on a projector-friendly inbox screen at the end. ASP.NET Core 10 Razor Pages, EF Core + Postgres, Resend for email, Dockerised, deployed to Railway. A single deployment serves multiple inboxes defined by the `INBOXES` env var.

Spec-driven development is managed with **OpenSpec** (`openspec/`). All feature work flows through a
change in `openspec/changes/`.

### Where to look for historical context on a shipped change

The OpenSpec archive (`openspec/changes/archive/YYYY-MM-DD-<name>/`) contains the spec deltas for every
shipped change. Alongside `proposal.md` / `design.md` / `specs/**/*.md` / `tasks.md` there may also be a
**`DEVLOG.md`** — a working note the orchestrator kept while the change was applied: per-section status,
decisions made under uncertainty, deviations, bugs surfaced, and human-in-the-loop verifications. When a
session needs context on *how* a prior change was built (not just *what* it specified), read the archived
`DEVLOG.md`. Active changes keep a `DEVLOG.md` in the change directory while in-flight; it moves to the
archive with the change.

### Commands

- Build: `dotnet build` — must be clean; `TreatWarningsAsErrors` is on, so any warning fails the build.
- Test: `dotnet test` — all green.
- Format: `dotnet format --verify-no-changes` — clean.
- Validate a change: `openspec validate <change-name> --strict`.
- List changes: `openspec list` (or the directories under `openspec/changes/`, excluding `archive/`).

---

# OpenSpec Apply Workflow

**This section is authoritative.** `/opsx:apply` is the entry point for implementing a change; if the
skill's behavior ever conflicts with what's written here, **follow this document**.

## Roles — the main thread never writes feature code

- **Orchestrator** = the main thread (you). You read specs, select work, brief agents, run the gates,
  tick boxes, and commit. **You do not implement feature code directly.**
- **`worker`** agent — implements the tasks.
- **`reviewer`** agent — audits the worker's diff.

Both agents are defined for this repo. Delegate; don't shortcut by writing the implementation yourself.

## 1. Select the change

1. List active changes = directories in `openspec/changes/` **excluding `archive/`**.
2. **Always ask the user which change to apply**, even when there is exactly one. If there are none,
   say so and stop.
3. Resume point = the **first unticked `- [ ]` task** in that change's `tasks.md`.

## 2. Pre-flight (orchestrator, before any section)

1. Read `proposal.md`, `design.md`, and the relevant `specs/<capability>/spec.md` for the section(s)
   you're about to work.
2. **Working tree must be clean** (`git status`). If it's dirty, stop and ask.
3. **Change must validate**: `openspec validate <change-name> --strict`. If it doesn't, stop and ask.
4. **Be on the change branch** `change/<change-name>`. Create it from the default branch if missing:
   `git switch -c change/<change-name>`.

## 3. Implement — section by section

The unit of work is a **`## N.` section**. Walk sections in order from the resume point. For each:

1. **Brief the worker.** Hand it: the section's tasks (`N.1`…`N.k`), the relevant spec excerpts, the
   binding design decisions that bind them, and the done-gates below. The worker should not need to go
   hunting — give it what it needs to stay focused.
2. **Worker implements the whole section.** If it's large or complex, split it into sub-chunks across
   multiple `worker` calls — but it remains **one commit at section end**.
3. **Audit.** Spawn `reviewer` on the section diff (correctness, design-decision compliance,
   OpenSpec scope, C# idiom, web-app safety).
4. **Review loop.** Feed the reviewer's findings back to the `worker`; worker fixes; `reviewer`
   re-audits. **Repeat until the reviewer signs off.**
5. **Gates — all must pass before ticking any box:**
   - `dotnet build` clean (no errors, no warnings — `TreatWarningsAsErrors` is on)
   - `dotnet test` green — new tests for the section **and** all existing tests
   - `openspec validate <change-name> --strict`
   - `dotnet format --verify-no-changes` clean
   If a gate fails, it's back to step 4, not a commit.
6. **Tick the boxes.** Mark every `- [x] N.M` in the section in `tasks.md`.
7. **Commit — one conventional commit per section:**
   ```
   feat(<change-name>): <section title> (section N)

   - N.1 <task summary>
   - N.2 <task summary>
   ...

   Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
   ```

## 4. Stop and ask — do not push on

Stop **immediately** and ask the user (do not improvise a fix) when:

- a spec/design is **ambiguous**, or two specs **contradict** each other;
- doing the task properly needs changes **outside this change's scope** (its proposal/specs);
- a task is **blocked by an unresolved Open Question** in `design.md`;
- implementation or tests reveal the **spec itself is wrong** (not just the code);
- a task **requires human-in-the-loop verification** that can't be settled by automated gates — e.g.
  scanning the QR code with a real phone, confirming the magic-link email actually arrives via Resend,
  or checking the inbox renders legibly on a projector. Implement and self-test as far as possible, then
  hand the user a precise, copy-pasteable way to verify (exact command, what to do, what they should see)
  and **wait for their confirmation before ticking that task**.

**On stopping mid-section:** leave the WIP **uncommitted**, do **not** tick the section, do **not**
revert. Report the **exact task (`N.M`)** that stopped you and why. The WIP stays in the working tree
for the user to inspect.

## 5. Done

When every task in the change is ticked and the final review is clean:

1. Report status: sections completed, commits made, test summary.
2. **Propose archiving** — offer to run `/opsx:archive` and **wait for the user's confirmation**.
   Do not archive automatically.
