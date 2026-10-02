# ADR 0004. Decouple client and server via REST API, OpenAPI, and reverse proxy

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

The application consists of a React single-page frontend and a .NET backend API. The two parts have distinct responsibilities: the frontend handles user interaction and presentation, while the backend handles business rules, validation, security, and persistence.

Developing and testing these parts independently requires a clearly defined interface. Both teams need an accurate, machine-readable description of endpoints, request bodies, and response models.

Additionally, in development the frontend runs on port 5173 (Vite) while the backend runs on port 8080. In production containers, frontend assets and backend API are separate network services. Calling across different origins triggers browser CORS restrictions, preflight latency, and environment-specific URL configurations in frontend code.

## Decision

We will decouple the React frontend and ASP.NET Core backend into separate applications communicating exclusively over a REST API.

The REST API is documented via OpenAPI using `Microsoft.AspNetCore.OpenApi`. Both Vite (in development) and Nginx (in production) will reverse-proxy requests matching `/api` to the backend, presenting frontend and API under a single origin.

## Consequences

### What becomes easier

- Frontend and backend can be developed, tested, and deployed independently.
- The REST API can support additional clients in the future (such as mobile apps or third-party integrations).
- OpenAPI generates machine-readable documentation directly from C# endpoint models, providing a verifiable contract.
- Frontend code uses relative URLs (`/api/...`) in both development and production, eliminating CORS configuration and preflight request latency.

### What becomes harder

- Communication happens over HTTP, introducing network latency and potential connection failures.
- Changes to API contracts require coordinated updates across both codebases.
- Proxy routing rules must be maintained in two places: `frontend/vite.config.ts` for local development and `frontend/nginx.conf` for production containers.

### What this commits us to

The REST API is the primary boundary between frontend and backend. All backend endpoints consumed by the frontend must be routed under the `/api` prefix.

## Alternatives considered

**Monolithic server-rendered application (e.g., Razor Pages or MVC).** Renders HTML on the server. We rejected this because we need a rich, interactive single-page application and independent frontend tooling.

**Direct database access from the frontend.** We rejected this because exposing database credentials and persistence concerns to the browser creates serious security flaws and bypasses business logic validation.

**Undocumented REST API without OpenAPI.** Relying on source code or chat messages to discover endpoints. We rejected this because a formal, machine-readable schema prevents integration guesswork.

**Enabling CORS on the backend instead of reverse-proxying.** Allowing cross-origin requests from `http://localhost:5173`. We rejected this because it requires maintaining origin allowlists across environments, introduces preflight roundtrip latency, and requires frontend code to switch base URLs between environments.

## Notes

In development, Vite proxies `/api` to `http://localhost:8080` in `frontend/vite.config.ts`. In production containers, Nginx proxies `/api` to `http://backend:8080` in `frontend/nginx.conf`.
