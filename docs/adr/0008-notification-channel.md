# ADR 0008. Use in-app notifications and Web Push, with email as a fallback

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
contract with an SMS provider, and we do not yet know whether Metro has a mail
relay we can use.

DDR 0001 (MET-C-037) designs the in-app inbox. It shows three kinds of
notification: a signature is required, changes are requested, and system
information that is not about any restriction. Users filter the inbox by all,
unread ("Ulæste") and action required ("Påkrævet"), and a header counter shows
unread items.

## Decision

Every notification is stored in the database and shown in an in-app inbox. That
stored record is the source of truth. We deliver it by Web Push first and fall
back to email when push is unavailable or fails permanently. Email is not part of
Metro's decision. We keep it as a fallback so that a signer without a working push
subscription is still reached, and we only enable it if Metro provides a mail
relay. Without one, an undeliverable push is surfaced on the restriction and in the
inbox instead. Push and email
messages contain only a generic Danish text and a link, never restriction details.
The inbox is shown only after login, so it may show who acted and why, as in the
DDR 0001 design.

Delivery uses the outbox pattern. The service that changes a restriction's state
saves the notification rows in the same `SaveChanges` as the state change. A
hosted `BackgroundService` then sends pending deliveries and retries failed ones.

Notifications are stored in two tables:

- `Notification`: one message to one recipient, about one restriction version or,
  for system information, about none (type, requires action, created, read,
  resolved). The type is `SignatureRequired`, `ChangesRequested` or `SystemInfo`.
- `NotificationDelivery`: one row per channel for a notification (channel, status,
  attempt count, next attempt, sent, last error).

A delivery's status is `Pending`, `Sent`, `Failed` (will be retried) or
`Undeliverable` (attempts exhausted or a permanent error). `Sent` means the push
service or mail server accepted the message, not that the recipient saw it. Only
`ReadAt` and the signature itself confirm that the recipient acted.

A notification that requires action keeps it until the action is done, not until
it is read. When the recipient signs or makes the requested changes, the service
sets `ResolvedAt` in the same `SaveChanges`, and the notification leaves the
"Påkrævet" filter.

## Consequences

**Easier.** Push has no per-message cost and needs no external contract. It reuses
the PWA from ADR 0007. Push payloads are encrypted end to end (RFC 8291). The inbox
matches the MET-C-037 design. Because notifications are saved before they are
sent, failures can be retried and diagnosed per restriction. Two tables let one
notification be tracked separately on each channel without the inbox showing
duplicates. `BackgroundService` is part of ASP.NET Core, so it adds no new
dependency.

**Harder.** Web Push only confirms that the push service accepted a message, not
that the phone showed it. Users can deny permission and subscriptions expire, so we
must handle `404`/`410` responses and remove dead subscriptions. Users on iOS
must install the PWA first; we test on Android, the reference platform. Email needs an SMTP provider and is less immediate for people
on shift. We must write the retry and backoff logic ourselves and keep the
background worker monitored. A single `BackgroundService` assumes one backend
instance. Running several instances would need row locking (for example
`FOR UPDATE SKIP LOCKED`).

**Commitments.** We store push subscriptions per device and manage VAPID keys as
secrets, outside config files. We commit to a "generic text plus link" content
rule, so the user has to log in to see anything specific.

## Alternatives considered

**Web Push only.** Rejected because a user without a valid subscription (iOS app not
installed, permission denied, new phone) would silently receive nothing. That
violates "surfaced rather than lost".

**Email only.** Rejected as the primary channel. Operators on shift do not watch
their inbox, and email is not fast enough for "it is your turn to sign". Mail also
passes through external servers, so it carries no detail. We keep it as the
fallback.

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

**A single notification table with one row per channel.** Simpler to build, but the
inbox would show one entry per channel unless rows are grouped. Read state would
also have to be kept in sync across those rows.

## Notes

Questions for Metro: whether a mail relay is available, and which devices
operators use. DDR 0001 is still awaiting Metro's feedback, so the notification
types may change. Revisit this decision after the first test period, using the
undeliverable counts from the restriction's notification diagnostics.
