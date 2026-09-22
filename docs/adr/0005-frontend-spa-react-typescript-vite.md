# ADR 0005. Build frontend as a React and TypeScript SPA with Vite and React Router

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

The application requires an interactive web interface for authoring operational restrictions, managing multi-party signing workflows, and viewing restrictions in force without full page reloads.

We need a component-based architecture for reusable UI elements, client-side routing so users can bookmark and share distinct view URLs, and static type checking to catch API mismatch errors at build time.

We also need fast local development startup, instant hot-module replacement (HMR), and efficient dependency management across developer workstations and CI runners.

## Decision

We will build the frontend as a Single-Page Application using React 19 and TypeScript, using React Router v7 for client-side navigation, Vite for local development and bundling, and pnpm for package management.

## Consequences

### What becomes easier

- UI elements are written as isolated, reusable React components.
- TypeScript catches type mismatches and missing fields at build time before code reaches the browser.
- Backend DTO schemas can be mirrored cleanly in frontend TypeScript interfaces.
- React Router provides centralized route mapping and bookmarkable URLs without full browser reloads.
- Local server startup and hot module replacement are near-instant because Vite serves source files over native ES modules.
- pnpm's content-addressable store speeds up dependency installs and reduces disk usage across machines.

### What becomes harder

- Developers must understand React component lifecycles, hooks, TypeScript types, and React Router conventions.
- Frontend TypeScript types must be updated whenever backend API contracts change.
- Direct navigation to deep client-side routes requires web server fallback configuration (`try_files $uri $uri/ /index.html`) so refreshing deep links does not return 404 errors.
- Packages that rely on npm's flat, hoisted `node_modules` structure can break under pnpm's strict symlinking, requiring explicit dependency declarations.

### What this commits us to

The frontend UI is built on React 19, TypeScript, and Vite. Migrating to another frontend framework or build tool later would require restructuring the UI and build scripts.

## Alternatives considered

**JavaScript with React (without TypeScript).** Reduces type-checking overhead, but we rejected it because static typing is essential for catching contract mismatches with the backend early.

**Vue or Angular.** Vue is a capable alternative and Angular is a full-featured framework. We chose React because of team familiarity, broad ecosystem support, and alignment with modern component architectures.

**Manual browser API routing (`window.history`).** We rejected this because implementing route matching, nested layouts, and parameters manually reinvents what React Router already provides reliably.

**Webpack / Create React App (CRA).** CRA is officially deprecated, and Webpack startup and rebuild times are significantly slower than Vite's native ESM dev server.

**npm or Yarn.** Standard npm installs are slower and duplicate dependencies across projects. Yarn Berry's PnP mode causes compatibility issues with some tools. pnpm provides equivalent disk savings while preserving a symlinked `node_modules` structure that standard tools understand.

## Notes

Run `pnpm dev` from the repository root to start frontend and backend concurrently. The production Nginx server includes SPA fallback routing in `frontend/nginx.conf`.
