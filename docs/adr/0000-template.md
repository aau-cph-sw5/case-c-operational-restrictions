# ADR {NNNN}. {Short title in the imperative}

**Status.** Proposed | Accepted | Superseded by ADR {NNNN} | Deprecated
**Date.** {YYYY-MM-DD}
**Deciders.** {names, and which teams}
**Related backlog items.** {MET-X-000, ...}

## Context

What forces are at work. Be honest about the ones that are not technical: a
dataset that had not arrived, a sprint boundary, a skill nobody on the product
had. Those constraints are real and a successor who does not know about them will
misread the decision.

State what was true at the time, not what is true now.

## Decision

One or two sentences, active voice, present tense.

> We will use server-sent events to propagate scenario state to connected clients.

## Consequences

What becomes easier.

What becomes harder. Do not skip this. A record with no downsides reads as
advocacy rather than a decision, and the downsides are what a successor needs when
they hit one.

What this commits us to that would be expensive to reverse.

## Alternatives considered

**{Alternative}.** Why it was rejected, specifically. "Too complex" is not a
reason; "it would require every client to hold an open connection through a tunnel
where the network drops for minutes at a time" is.

**{Alternative}.** As above.

## Notes

Anything a successor would want: a link to the discussion, the measurement that
settled it, the Metro answer that constrained it, the date it should be revisited.
