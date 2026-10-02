# ADR 0006. Containerize environments using Docker, Docker Compose, and Nginx

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

The application spans a .NET backend, a React frontend, and a PostgreSQL database.

Differences in developer host operating systems, installed SDK versions, and local system packages frequently cause "works on my machine" bugs. Starting each service manually with individual terminal commands or manual database setups adds onboarding friction.

In addition, the production frontend build outputs static HTML, CSS, and JavaScript files. Running a Node.js runtime simply to serve static files wastes memory and CPU. We need a consistent runtime environment across workstations, CI checks, and production deployments.

## Decision

We will containerize application services with Docker, orchestrate multi-container local environments with Docker Compose, and use Nginx (`nginx:alpine`) to serve production frontend static assets and proxy API traffic.

## Consequences

### What becomes easier

- Environments are reproducible: the exact same container images run in local development, CI checks, and production deployments.
- The entire multi-service stack starts with a single command: `docker compose up --build`.
- Inter-service networking, port mappings, and build contexts are version-controlled in `docker-compose.yml`.
- New developers can run the entire application stack without manually installing PostgreSQL or .NET SDKs on their host OS.
- Static assets are served with minimal CPU and memory overhead, with built-in gzip compression and SPA fallback routing.

### What becomes harder

- Building and running containers introduces local build time and volume mount overhead compared to bare-metal execution.
- Debugging processes and inspecting network requests inside containers requires Docker knowledge.
- Docker Compose networking and volume behaviors differ from production orchestrators (such as Kubernetes), so Compose files are not a direct 1:1 match for cloud clusters.
- Configuration requires Nginx-specific syntax (`nginx.conf`), which can introduce subtle bugs if unfamiliar.

### What this commits us to

Our build, packaging, and local multi-service workflows are centered on Docker container images and Docker Compose.

## Alternatives considered

**Bare-metal host installation only.** Requiring every developer to manually install PostgreSQL, .NET 10, and Node.js directly on their machine. We rejected this because local system differences inevitably cause setup failures and untracked drift.

**Virtual machines (e.g. Vagrant).** Provides full isolation, but VMs are significantly heavier to download, slower to boot, and consume far more memory than lightweight Docker containers.

**Local Kubernetes (Minikube / Kind).** We rejected this as excessive complexity and resource overhead for local development when production orchestration has not yet been established.

**Node.js static server for frontend production.** We rejected this because running a full Node.js process to serve static files consumes more memory and bloats the production Docker image compared to `nginx:alpine`.

## Notes

Run `docker compose down -v` to reset database volumes when testing schema migrations from scratch. Native host development remains supported for developers who prefer running processes directly on their machine (Option 2 in `README.md`).
