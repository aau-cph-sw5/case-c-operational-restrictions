# Limits of the signature scheme

This document describes what a signature in this system does and does not prove. It is written against what a regulator, auditor, or investigator might reasonably expect from a signed safety document, so that the gaps are known and stated rather than discovered later.

It describes the code as of 2026-10-11 (backlog item MET-C-003).

## What a signature is today

A `Signature` records:

- who signed (`SigneeId`) and in which role at the time (`SigneeRole`)
- which restriction version it belongs to (`OperationalRestrictionId`) and which required signature it fulfils (`RequiresSignatureId`)
- when it was created (`SignedAt`, server time in UTC)

It is created by `SignatureService.Sign` when an authenticated user, who must be the required signer, signs an unsigned and not-invalidated required signature. `Signature` has only `init` properties, and a test fails if a public setter is added.

In practice this is a click-to-sign by a logged-in user. The user's identity rests on a valid JWT. See ADR 0008 for why signing does not require re-authentication.

## Summary

| Expectation | Status | Detail |
|---|---|---|
| Signer identity is established | Partial | Rests on a JWT only. Login is currently a placeholder. |
| Signer is present at signing | Not met | No re-authentication, and tokens live for 14 days. |
| Signature cannot be altered | Partial | Enforced in code and by test, not by the database. |
| Alteration would be detectable | Not met | No hash, digital signature, or hash chain. |
| Signature is durable | Not met | Currently held in memory only. |
| Time of signing is trustworthy | Partial | Server clock, no external time source. |
| Signed content is bound to the signature | Partial | Linked by id only, content is not hashed. |
| Full history is kept | Not met | No signature audit trail in the code today. |

## Limits in detail

### Identity and presence

- A signature proves that a request arrived with a valid token for the signer's user id. It does not prove that the person was at the keyboard. Anyone with access to an unlocked device or a copied token can sign as that user.
- JWT lifetime is 14 days (`JwtOptions.LifetimeDays`). A signature can be made with a token that was issued two weeks earlier.
- There is no second factor. Login is by email and password.
- The login in the code today is a single hardcoded placeholder user, and the users used when signing are a hardcoded list. Real identity management is not in place.

### Immutability

- Immutability is a property of the C# type. `Signature` has no setters, and `SignatureService` has no method that updates or deletes a signature.
- Nothing prevents a change outside the application, for example direct database access, a migration, or a future code path that bypasses `SignatureService`. When signatures are stored in PostgreSQL, the database does not enforce this unless we add it (for example revoked UPDATE/DELETE rights or a trigger).
- There is no tamper evidence. If a stored signature were changed, nothing in the data would show it.
- The test checks the shape of the `Signature` type. It does not prove that no code path anywhere changes stored signatures.

### Validity versus existence

A signature is never changed, but it can stop being valid. When a restriction is revised, `InvalidateRequiredSignatures` sets the related required signatures to `Invalid`, and `IsSignatureValid` then returns false. `RequiresSignature` is mutable and is the source of truth for validity. Anyone reading the data has to look at both the signature and its required signature.

### Binding to the signed content

A signature points to a restriction version by id. It does not contain a hash of what was shown to the signer. If the restriction content for that id were changed, the signature would still point to it. The versioning of restrictions must therefore be append-only for this binding to mean anything.

### Time

`SignedAt` is `DateTime.UtcNow` on the application server. It is not from a trusted time-stamping service, and it is only as accurate as the server clock.

### Persistence and retention

Signatures and required signatures are currently held in static in-memory lists in `SignatureService` and are lost on restart. Persistence in PostgreSQL is still to be built. The five-year archive requirement is therefore not met by the current code.

### Audit trail

There is no record of attempted or refused signings, and no record of invalidations beyond the current status. A versioned audit trail is a separate backlog item (MET-C-021).

### Legal status

This is a simple electronic signature in the eIDAS sense: data attached to other data that the signer uses to sign. It is not an advanced or qualified electronic signature. If a regulator or a contract requires one of those, this scheme does not satisfy it.

## Temporary code that does not meet the scheme

`POST api/signature/sign-random` has no `[Authorize]`. It signs as the required signee without any logged-in user. It is marked temporary and must be removed or protected before the system holds real restrictions.

## What would close the gaps

Roughly in order of value:

1. Persist signatures in PostgreSQL and remove the in-memory lists and the `sign-random` endpoint.
2. Restrict UPDATE and DELETE on the signature table at the database level.
3. Store a hash of the restriction content on the signature, and a hash chain or a signature over each record, to make changes detectable.
4. Add an append-only audit log, including refused attempts and invalidations.
5. Add re-authentication or a short token lifetime for signing, if the requirement turns out to be that the signer is present (revisit ADR 0008).
6. Add real user management and a second factor.
7. Use a trusted time source if exact signing times are required.

## Questions to ask the customer

- Which regulation or internal safety rule governs signed operational restrictions, and does it name a signature type?
- Must the signer be demonstrably present at the moment of signing?
- How must a signed record be shown to be unchanged after five years?
- Who may inspect the audit trail, and in what form?
