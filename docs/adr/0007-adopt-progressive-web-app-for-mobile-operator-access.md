# ADR 0007. Build mobile experience as a Progressive Web App instead of React Native

**Status.** Accepted
**Date.** 2026-09-29
**Deciders.** Group 5
**Related backlog items.** MET-C-036 Convert Application to a PWA

## Context

Operators need to receive notifications and view operational restrictions on mobile devices.

Our supervisor suggested building a native mobile app with React Native to handle mobile push notifications. However, we are a big team on a single-semester deadline, and our web frontend is already being built with React 19, Vite, and TypeScript (ADR 0005).

Building a separate React Native app would mean maintaining two different frontends, duplicating domain types, and dealing with native build tools (e.g., Xcode and Android Studio). Distributing a native app to Metro operator phones would also require enterprise app store certificates or MDM rollout.

We need a way to support mobile devices, offline opening, and push notifications without splitting our team across two codebases.

## Decision

We will turn our existing React web application into a Progressive Web App (PWA) using `vite-plugin-pwa` instead of building a separate React Native app.

## Consequences

### What becomes easier

- We maintain a single frontend codebase for desktop, tablet, and mobile.
- Deployments are instant: pushing to the server updates all devices without app store reviews or device management profiles.
- Operators can install the app to their home screen directly from the browser URL.
- Static assets (HTML, CSS, JS, icons) are precached, so the app shell opens even in tunnels with no mobile connection.
- Standard Web Push APIs allow sending push notifications to mobile browsers.

### What becomes harder

- iOS users must open the app in Safari and tap "Add to Home Screen" before push notifications and standalone mode work (iOS 16.4+ requirement).
- Background work is limited to what the Service Worker can do, so we cannot run persistent background threads like a native app could.
- The site must be served over HTTPS in production for service workers to function.

### What this commits us to

Mobile operator support and future push notifications will rely on standard Web APIs (Web App Manifest, Service Workers, Web Push) rather than native iOS and Android SDKs.

## Alternatives considered

**React Native.** Suggested by our supervisor. We rejected this because building and testing two separate codebases would cut our development capacity in half, and dealing with app store distribution or enterprise sideloading adds needless friction for Metro's devices.

**Native iOS/Android (Swift/Kotlin).** Rejected for the same reasons as React Native, with the added problem that the team would have to learn two more mobile languages.

**Standard responsive website without PWA.** Rejected because regular browser tabs cannot be installed to the home screen, cannot load offline, and cannot receive background push notifications when the tab is closed.

## Notes

Initial PWA setup adds the web manifest, app icons, and offline shell caching via `vite-plugin-pwa`. Web Push subscription endpoints and backend VAPID keys will be added in the notification feature sprint.
