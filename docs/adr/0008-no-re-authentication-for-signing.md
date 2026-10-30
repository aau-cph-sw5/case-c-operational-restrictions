# ADR 0008. Do not require re-authentication when signing

**Status.** Accepted
**Date.** 2026-10-11
**Deciders.** Group 5
**Related backlog items.** MET-C-003, Signature primitive: identity, role, timestamp, immutability

## Context

A signature on an operational restriction records who signed, in which role, and when. The backlog item for the signature primitive asks that the assurance level, meaning whether signing requires re-authentication, is recorded as a decision with its reason.

Signing happens through the API. Every endpoint that creates a signature requires a valid JWT, issued by the backend at login and validated on each request (signature, issuer, audience, and lifetime). The signer is taken from the token and must match the user the signature is required from. A user without a valid token cannot reach the signing endpoint.

An additional re-authentication step (for example asking for the password again, or issuing a short-lived step-up token) would add a separate flow to the backend and frontend, and `AuthService` would need to support it.

## Decision

We will not require re-authentication when a user signs. A valid JWT is sufficient proof of identity for signing.

## Consequences

### What becomes easier

- Signing is a single authenticated request. There is no extra flow to build, test, or explain to operators.
- Nothing new is needed in `AuthService`, `JwtService`, or the frontend.

### What becomes harder

- A signature is only as strong as the session behind it. Anyone using an unlocked device with a still-valid token can sign as that user.
- The token lifetime is set by `JwtOptions.LifetimeDays`, so a signature can be created with a token that is days old. If the required assurance for signing turns out to be "the signer is present now", this decision does not give that.
- Anyone reading an old signature cannot tell from the signature itself that no re-authentication took place. This ADR is the record of that decision.

### What this commits us to

All signing endpoints must stay behind `[Authorize]`.

Adding re-authentication later means a new flow and a way to record the assurance level on `Signature`. Signatures created before that point would have no assurance level recorded.

## Alternatives considered

**Re-enter the password when signing.** Gives stronger evidence that the signer is present at the moment of signing. We rejected it because every request to the signing endpoint is already authenticated with a validated JWT, and the extra flow was judged not worth the added work for this project.

**Short-lived step-up token for signing.** Limits how old a session can be when it is used to sign. Rejected for the same reason, and because it requires changes to token issuing and the frontend.

**Shorter JWT lifetime for all endpoints.** Not chosen as part of this decision. It could be used later to reduce the risk described above without a separate re-authentication flow.

## Notes

Revisit this decision if the requirements specify that signers must be present at the time of signing, or if the JWT lifetime is kept at multiple days.
