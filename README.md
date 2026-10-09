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

## Server and Autonomous Deployment

Our CD pipeline pushes branch changes to a remote remote environment automatically, but requires a one time sequential setup for new instances.

**1. Create a Linux Server**
Provision a fresh, blank Linux server to host the application environments.

**2. Set Up GitHub Secrets**
The GitHub Actions runner requires credentials to access and configure the remote server. Add the following repository secrets:

* `SERVER_HOST`: The IP address of your remote Linux server.
* `SERVER_USER`: The Linux username used to execute commands on the machine.
* `SSH_PRIVATE_KEY`: The SSH private key that has authorized access to the server.
* `SERVER_FINGERPRINT`: The servers fingerprint identifies a server's public key, needed for secure connection.
* `DOMAIN`: The base domain or subdomain (e.g., via DuckDNS) that Traefik will use for dynamic web routing.
* `ACME_EMAIL`: The email address used for Let's Encrypt ACME registration and SSL/TLS certificate renewal notifications.
* `JWT_SIGNING_KEY`: A secure random string (minimum 32 characters) used by the ASP.NET Core backend to sign and validate JWT authentication tokens.

**3. Run the Server Bootstrap Workflow**
You **must** run the "Server Bootstrap" GitHub Action (`bootstrap.yml`) *first*, before triggering any autonomous deployment workflows. This bootstrap script is entirely server-agnostic. It logs into the blank server to automatically install Docker, set up Docker Compose, configure the external networks, and deploy the global Traefik reverse proxy needed to route traffic to your future containers.
**4. Example of autonomous deployment**
Once the autonomous deployment workflows run, they will push the branch to the subdomain with the branch prefix, below is the current example, which might be deprecated;

*Development branch runs on [http://dev.metro-operational-restrictions.duckdns.org](http://dev.metro-operational-restrictions.duckdns.org).*

*Staging branch runs on  [http://staging.metro-operational-restrictions.duckdns.org](http://staging.metro-operational-restrictions.duckdns.org).*

*Main branch runs on [http://main.metro-operational-restrictions.duckdns.org](http://main.metro-operational-restrictions.duckdns.org).*

**4. SERVER_FINGERPRINT Debuging**

The server fingerprint is something you yourself have to procure, one of the ways to do so is to use the following command in CMD: 
```bash 
ssh-keyscan -p 22 SERVER_IP 2>nul | ssh-keygen -lf -
```
Example 
```bash 
ssh-keyscan -p 22 150.230.151.68 2>nul | ssh-keygen -lf -

```
The `appleboy` plugin typically uses (ECDSA), but if it dosent work, you will have to test the other two formats (RSA) and (ED25519). 

You can also isntead do the following:
1. Go to your repository **Settings** -> **Secrets and variables** -> **Actions**.
2. Create a new **Variable**.
3. Name it `ACTIONS_STEP_DEBUG` and set the value to `true`.

With this enabled, the next time your pipeline runs and the SSH handshake fails, the `appleboy` plugin will print the verbose Go SSH logs directly into your GitHub Actions console. Allowing you to see which algorithm it's using.

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
