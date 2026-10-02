# ADR 0001. Record architecture decisions

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

We need to record significant architectural and design choices made on this project, including what was chosen, what was rejected, and why.

A team forgets its own reasoning within a month. When people graduate or move to other tasks, inherited code looks arbitrary without written context. Multiple teams also share this codebase, so choices made by one team are encountered by others as constraints. A short record turns "why is it like this" into a two-minute read instead of an integration meeting.

Oral examinations also ask directly about design choices and trade-offs. Having written down what we rejected and why prepares us to answer.

## Decision

We will use Architecture Decision Records (ADRs) to document architecturally significant decisions.

Records are stored in `docs/adr/`, numbered sequentially starting with this document, and structured according to `0000-template.md`.

## Consequences

### What becomes easier

- Decisions, trade-offs, and rejected options are preserved for current team members, successors, and examiners.
- Cross-team coordination is faster because architectural constraints are documented in one place.
- Decisions go through pull request reviews, so teammates review choices before code locks them in.

### What becomes harder

- Writing and reviewing decision records takes time and discipline during feature development.
- When an earlier decision changes, we have to write a new record and mark the old one superseded with links in both directions.

### What this commits us to

- Every significant architectural decision must have a corresponding record in `docs/adr/`.
- Records are numbered sequentially and never renumbered or deleted.
- If a decision changes, a new record must be written and the old record marked as superseded with bidirectional links.

## Alternatives considered

**Informal notes or wiki pages.** We rejected this because separate documentation drifts out of sync with code, describes what was built rather than why, and skips peer review.

**Code comments and commit/PR descriptions.** We rejected this because architectural context ends up scattered across hundreds of files or buried in closed pull requests, making systemic review difficult.

## Notes

See `docs/adr/0000-template.md` for the template.
