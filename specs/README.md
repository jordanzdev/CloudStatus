# Specs

CloudStatus follows Spec Driven Development: **no production code is written without an approved
spec**. A spec says what must be true and how we will know; the code follows it, never the other
way around.

## Process

1. The author drafts the spec from the [template](#template), in state **Draft**.
2. The maintainer reviews it. Approval moves it to **Approved**.
3. Implementation happens on a branch named `spec/NNN-name`, with tests.
4. Before the pull request:
   - `build.ps1` finishes with no warnings.
   - Every `[auto]` criterion passes.
   - The spec's status and `CHANGELOG.md` `[Unreleased]` are updated.
5. The pull request lists every `[hw]` criterion the maintainer must test with the headset.
6. Merging moves the spec to **Implemented**.
7. Once the maintainer has passed every `[hw]` criterion, the spec moves to **Verified**.

**If the implementation contradicts an approved spec, stop.** Propose the change to the spec and
wait for approval. Code is never changed first with the spec "fixed" afterwards.

## States

| State | Meaning | Who sets it |
|---|---|---|
| **Draft** | Being written. May change freely | Author |
| **Approved** | Agreed. Implementation may start. Changes need new approval | Maintainer |
| **Implemented** | Merged, and every `[auto]` criterion passes | Author, in the pull request |
| **Verified** | Every `[hw]` criterion passed on real hardware | Maintainer |

A spec with no `[hw]` criteria goes straight from Implemented to Verified.

## Files

| File | Contents |
|---|---|
| [ROADMAP.md](ROADMAP.md) | Order of the specs and their dependencies |
| [000-vision.md](000-vision.md) | Problem, users, scope, non-goals, success metrics |
| `NNN-name.md` | One spec per feature |
| [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md) | Numbered design decisions (D1, D2, …) |
| [../docs/PERFORMANCE.md](../docs/PERFORMANCE.md) | Budgets, measurement method, results per version |
| [../docs/protocol.md](../docs/protocol.md) | HID packets, sources, licenses, open questions |

## Template

```markdown
# NNN: Title

**Status:** Draft

## Context
Why this exists and what it builds on. Link decisions (D1) and open questions (OQ-01).

## Goals
## Non-goals

## Requirements
- **REQ-NNN-01** The app MUST …
- **REQ-NNN-02** The app SHOULD …

## Performance budget
Which metrics from docs/PERFORMANCE.md this spec affects, and how each one is verified.

## Acceptance criteria
- **AC-NNN-01** `[auto]` Given … when … then …
- **AC-NNN-02** `[hw]` Given … when … then …

## Open questions
## Status history
```

### Writing requirements

- **MUST** is mandatory; **SHOULD** may be skipped only with a reason written in the spec.
- Every requirement has at least one acceptance criterion.
- `[auto]` means a test or CI covers it. `[hw]` means a person must check it by hand, usually with
  the headset. A criterion that needs no headset but cannot be automated is still `[hw]`.
- Open questions that block implementation must be resolved before the spec is approved.
