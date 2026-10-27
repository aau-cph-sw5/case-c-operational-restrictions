# ADR 0008. Use in-app notifications and Web Push

**Status.** Proposed
**Date.** 2026-10-15
**Deciders.** Group 5
**Related backlog items.** MET-C-018, MET-C-019, MET-C-020, MET-C-037-A/B, ADR 0007, DDR 0001, Metro decision of 28 August 2026

## Context

The paper workflow stalls when a signer does not know it is their turn. Signers
include originators, duty operations managers, control-room supervisors and
operators. They may be on shift, off shift, or underground where mobile coverage
drops. The system must notify them reliably. A failed send must be retried, a
notification that cannot be delivered must be surfaced rather than lost,
notification state must be visible on the restriction, and no message that leaves
the system may carry confidential restriction content.

On 28 August 2026 Metro decided that signers are notified by an application
notification: the product is a mobile app that notifies in-app, with Android as the
reference platform. This ADR records how we deliver that and what we guarantee.

ADR 0007 already chose a Progressive Web App, so Web Push is available without a
native app. Web Push works on Android without extra steps. On iOS, which is not the
reference platform, push only works after the user adds the app to the home screen
(iOS 16.4+). We are a student team with one semester. We have no budget or
contract with an SMS provider, and we do not know whether Metro has a mail relay
we can use.

DDR 0001 (MET-C-037) designs the in-app inbox. It shows three kinds of
notification: a signature is required, changes are requested, and system
information that is not about any restriction. Users filter the inbox by all,
unread ("Ulæste") and action required ("Påkrævet"), and a header counter shows
unread items.

## Decision

Every notification is stored in the database and shown in an in-app inbox. That
stored record is the source of truth. We deliver it to the phone by Web Push. We
do not use email or SMS. A signer whose push cannot be delivered still finds the
notification in the inbox, and the failed delivery is surfaced on the restriction.
Push messages contain only a generic Danish text and a link, never restriction
details. The inbox is shown only after login, so it may show who acted and why, as
in the DDR 0001 design.

Delivery uses the outbox pattern. The service that changes a restriction's state
saves the notification rows in the same `SaveChanges` as the state change. A
hosted `BackgroundService` then sends pending deliveries and retries failed ones.

Notifications are stored in two tables:

- `Notification`: one message to one recipient, about one restriction version or,
  for system information, about none (type, requires action, created, read,
  resolved). The type is `SignatureRequired`, `ChangesRequested` or `SystemInfo`.
- `NotificationDelivery`: the push delivery of a notification (status, attempt
  count, next attempt, sent, last error).

A delivery's status is `Pending`, `Sent`, `Failed` (will be retried) or
`Undeliverable` (attempts exhausted or a permanent error). `Sent` means the push
service accepted the message, not that the recipient saw it. Only `ReadAt` and the
signature itself confirm that the recipient acted.

A notification that requires action keeps it until the action is done, not until
it is read. When the recipient signs or makes the requested changes, the service
sets `ResolvedAt` in the same `SaveChanges`, and the notification leaves the
"Påkrævet" filter.

## Consequences

**Easier.** Push has no per-message cost and needs no external contract. It reuses
the PWA from ADR 0007. Push payloads are encrypted end to end (RFC 8291). The inbox
matches the MET-C-037 design. Because notifications are saved before they are
sent, failures can be retried and diagnosed per restriction. Keeping deliveries in
their own table separates send attempts from the inbox record, and lets a
notification later have one delivery per device. One channel means one sender to
build and test. `BackgroundService` is part of ASP.NET Core, so it adds no new
dependency.

**Harder.** Web Push only confirms that the push service accepted a message, not
that the phone showed it. Users can deny permission and subscriptions expire, so we
must handle `404`/`410` responses and remove dead subscriptions. A signer without a
working push subscription gets no push at all; they only see the notification when
they next open the app, and the restriction shows the delivery as `Undeliverable`.
Users on iOS must install the PWA first; we test on Android, the reference
platform. We must write the retry and backoff logic ourselves and keep the
background worker monitored. A single `BackgroundService` assumes one backend
instance. Running several instances would need row locking (for example
`FOR UPDATE SKIP LOCKED`).

**Commitments.** We store push subscriptions per device and manage VAPID keys as
secrets, outside config files. We commit to a "generic text plus link" content
rule, so the user has to log in to see anything specific.

## Alternatives considered

**Web Push with email as a fallback.** Email would reach a signer whose push
subscription is missing or expired. We set it aside for now because it is not part
of Metro's decision, it needs a mail relay from Metro that we do not have, and it
adds a second channel to build, test and track. An undeliverable push is still
surfaced, so nothing is lost without it. We will revisit it if Metro provides a mail
relay and the undeliverable counts show that push alone is not enough.

**Email only.** Rejected. Operators on shift do not watch their inbox, and email is
not fast enough for "it is your turn to sign". Mail also passes through external
servers, so it could carry no detail.

**SMS.** SMS works without installing anything or granting permissions, and
carriers provide delivery reports, which is its main advantage. We rejected it for
now because it needs a paid gateway and an agreement with Metro, requires storing
every signer's phone number (more personal data under GDPR), and sends text
unencrypted through carriers. We will revisit it if Metro already has an SMS
gateway, or if measured push delivery proves too unreliable.

**Hangfire for retries.** Hangfire gives retries and a dashboard out of the box. It
was rejected because it adds a dependency, creates its own database tables, and
adds more for the team to learn. Our retry needs are simple enough for a
`BackgroundService`.

**A single notification table with the delivery fields on it.** Simpler to build,
but it mixes the inbox record with send attempts, and a notification could not get
one delivery per device without changing the table.

## Notes

Questions for Metro: which devices operators use, and whether a mail relay is
available if the email fallback is revisited. DDR 0001 is still awaiting Metro's
feedback, so the notification types may change. Revisit this decision after the
first test period, using the undeliverable counts from the restriction's
notification diagnostics.
