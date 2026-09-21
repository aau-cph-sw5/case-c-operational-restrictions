# Case C. Operational Restrictions

*Driftsrestriktioner*  ·  5th Semester, BSc Software Engineering, AAU Copenhagen  ·  Autumn 2026

A digital replacement for the paper-based Operational Restriction workflow: authoring, multi-party approval and signing, operator read-and-sign, cancellation, a register of restrictions in force, automatic notification, and a five-year archive, usable remotely.

> **New here?** Read the [documentation hub](https://github.com/aau-cph-sw5/semester-docs) first,
> in particular [CONTRIBUTING](https://github.com/aau-cph-sw5/semester-docs/blob/main/CONTRIBUTING.md),
> [onboarding](https://github.com/aau-cph-sw5/semester-docs/blob/main/docs/01-onboarding.md) and
> [handling Metro material](https://github.com/aau-cph-sw5/semester-docs/blob/main/docs/10-data-handling.md).

> This case has the richest source material of the four, 21 stories across 10 epics, and the highest proportion of items carrying a single acceptance criterion or none. The detail sits in the workflow structure rather than in the individual stories, which is why the state machine of C-001 and C-002 is the first thing this backlog builds.

## The work

27 backlog items across 12 epics, one GitHub issue each. [Board](https://github.com/orgs/aau-cph-sw5/projects) · [Full backlog](https://github.com/aau-cph-sw5/semester-docs/blob/main/backlog/case-c-operational-restrictions.md)

**Start with the minimum demonstrable product**, the 7 items proposed for sprints 1 to 3:

- `MET-C-001` Document the Operational Restriction lifecycle as an explicit state model
- `MET-C-002` State machine implementation with guarded transitions
- `MET-C-003` Signature primitive: identity, role, timestamp, immutability
- `MET-C-004` Create a restriction with the required fields
- `MET-C-006` Originator signs the restriction
- `MET-C-021` Versioned audit trail of every restriction and signature
- `MET-C-022` Role-based access control across the workflow **(blocked)**

Items marked **(blocked)** are in this set because the product is incomplete without them, not because they can be pulled today: they need an answer from Metro first. The rule in CONTRIBUTING.md stands, and a blocked item that your team needs next is something to raise at the sprint review.


**7 items are blocked** on an answer from Metro Service, down from eleven before Metro's August answers. They are not dead: read them, and bring the question to the next sprint review. Filter the issues by `status:blocked` to see them, and by `needs:metro` for everything that still carries a question for Metro.

## Getting it running

### Option 1: Running with Docker (Recommended)

**Prerequisites:** Docker and Docker Compose.

```bash
# Build and start all services (Backend + Frontend)
docker compose up --build
```

* **Frontend:** Open [http://localhost:5173](http://localhost:5173) in your browser.
* **Backend API:** Access [http://localhost:8080/api/hello](http://localhost:8080/api/hello) (or via reverse-proxy at [http://localhost:5173/api/hello](http://localhost:5173/api/hello)).

To stop the containers:
```bash
docker compose down
```

### Option 2: Running Locally

**Prerequisites:** .NET 10.0 SDK and Node.js 22+ / pnpm.

 **1. Install Dependencies:**
   ```bash
   pnpm install
   ```

 **2. Start Backend & Frontend Concurrently:**
   ```bash
   pnpm dev
   ```
   
   (Or run individually in separate terminals: cd Backend && dotnet run and pnpm --filter frontend dev)
   
   *Frontend runs on [http://localhost:5173](http://localhost:5173).*

   *Backend runs on [http://localhost:8080](http://localhost:8080).*
   *Swagger UI (API documentation) is available at [http://localhost:8080/swagger](http://localhost:8080/swagger) when running in development.*

 **3. Format & Lint:**
   ```bash
   pnpm format    # Formats both backend (.NET) and frontend (Prettier)
   pnpm lint      # Lints both backend (Roslyn analyzers) and frontend (ESLint)
   ```

 **4. Run Tests:**
   ```bash
   pnpm test           # Runs both backend (.NET) and frontend (Vitest) tests
   pnpm test:backend   # Backend tests only (dotnet test)
   pnpm test:frontend  # Frontend tests only (Vitest)
   ```

## Layout

```
Backend/       .NET 10 Web API (MVCS architecture, EF Core, PostgreSQL provider)
Backend.Tests/ backend unit and integration test suite (xUnit)
frontend/      React + Vite SPA with React Router
contracts/     published interfaces other teams build against, versioned
docs/adr/      architecture decision records
fixtures/      synthetic test data. Never anything Metro supplied.
```

## Branches

`main` protected, only what has been demonstrated at a review. `staging` integration, should always run. `development` the shared working branch. One feature branch per item, named for it.

## AI assistants

> Record here which assistants this team used and for what, per [the semester policy](https://github.com/aau-cph-sw5/semester-docs/blob/main/docs/11-ai-use.md). Two lines is enough.

## Licence

MIT, per Section 7 of the AAU and Metro Service collaboration framework.
