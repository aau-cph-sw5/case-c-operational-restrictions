# ADR 0010. Record the device class with a signature and make signing attempts idempotent

**Status.** Proposed
**Date.** 2026-11-18
**Deciders.** Group 5
**Related backlog items.** MET-C-010 Remote signing from phone, tablet or remote desktop; MET-C-003 (ADR 0008)

## Context

Signers must be able to sign from a phone, a tablet or a desktop browser. ADR 0008 decided that a valid JWT is sufficient to sign. Remote signing and physical presence are not equivalent evidence, so the assurance level must not depend on the device, and a reader of a signature should be able to see which kind of device was used.

A mobile connection can drop between the request and the response. The signer must then learn whether the signature exists, and a retry must not create a second one.

## Decision

1. **Assurance is the same on every device.** The ADR 0008 rule (valid JWT, signer is the required signer) is the only check. The device class is never an input to an authorization decision. A device that cannot present a valid token cannot sign.
2. **The device class is recorded as evidence.** `Signature` gets `DeviceClass` (Unknown, Phone, Tablet, Desktop) and the raw `UserAgent`. The server derives the class from the User-Agent and the `Sec-CH-UA-Mobile` Client Hint (`DeviceClassResolver`). A value declared by the client in the body or in a custom header is ignored. Both fields are `init`-only and are copied to later restriction versions.
3. **A signing attempt carries a client-generated key** in the `Idempotency-Key` header. The key is stored on the signature. A request with a key that already has a committed signature returns that signature and creates nothing. The key is optional so existing callers keep working.
4. **A signature exists only if it was committed.** The signing flow is serialized from the first write until the transaction has committed or the in-memory signature has been discarded, so a status lookup or a retry never sees a signature in between. `GET api/signature/attempts/{attemptKey}` answers Signed (with the signature) or NotSigned, for the calling user's own attempts only.
5. **The client reports success only after the server confirms.** If the request gets no usable answer (no response, or a 5xx), the client asks the status endpoint. While the answer is unknown it shows that, and offers no sign action.

## Consequences

### What becomes easier

- An auditor can see which class of device each signature came from and re-check it against the raw user agent.
- A dropped connection ends in one of two states the signer can see, and a retry is safe.

### What becomes harder

- The device class is coarse and comes from headers the client controls, so it is evidence and not proof. iPadOS Safari in desktop mode is reported as Desktop. A remote desktop session is reported as the class of the session, not of the physical device.
- Signatures created before this change have DeviceClass Unknown and no user agent.
- Operator read-and-sign signatures are still held in memory only (see `docs/signature-scheme-limitations.md`), so their attempt keys do not survive a restart. Signatures stored with their restriction version do.
- Signing is serialized across the process. This is acceptable at the expected volume and relies on a single backend instance, as the in-memory signature lists already do.

### What this commits us to

No code path may use `DeviceClass` or `UserAgent` to allow, refuse or relax a signing action. If the required assurance changes (for example re-authentication, revisiting ADR 0008), it must change for all device classes at once.

## Alternatives considered

**Relax or tighten assurance per device.** Rejected: it would make the evidence behind two signatures of the same document different depending on where the signer was.

**Trust a device class sent by the client.** Rejected: the server can derive a coarse class itself, and a self-declared value adds nothing.

**Optimistic success in the UI.** Rejected: a signer who sees "Signed" for a signature that was never committed has a false record.

## Notes

The `sign-random` endpoint has no `[Authorize]` and therefore does not meet the ADR 0008 assurance level on any device. It should be removed before real restrictions are held.
