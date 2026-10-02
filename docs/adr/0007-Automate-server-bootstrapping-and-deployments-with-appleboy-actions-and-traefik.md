# ADR 0007. Automate server bootstrapping and deployments with Appleboy actions and Traefik

**Status.** Proposed 
**Date.** 2026-09-29  
**Deciders.** Group 4, Artem Shevchenko, Daniel Bach Eriksen, Gabriella Blake, Joakim Boldt   
**Related backlog items.** MET-C-33  

## Context

Our infrastructure requires continuous deployment to a remote server across multiple environments (development, staging, and main). Deploying manually via SSH is error prone, untracked, and creates bottlenecks. We need a GitHub Actions CD pipeline to push repository changes to the server and spin up containerized environments automatically.

Additionally, because we host multiple environments on a single remote machine, we need a way to route traffic dynamically to the correct Docker stack (e.g., `dev.domain.com` vs `staging.domain.com`). Manually managing port allocations and reverse proxy configurations for every new branch or environment creates unnecessary maintenance overhead.

## Decision

We will automate server bootstrapping and continuous deployment pipelines using `appleboy/ssh-action` and `appleboy/scp-action`, and deploy Traefik as a global reverse proxy to dynamically route traffic to our Docker Compose environments.

## Consequences

**What becomes easier**
- Deployments are fully hands off and triggered directly by branch pushes.
- Traefik automatically detects new Docker containers via the Docker socket and provisions routing dynamically based on environment variables, eliminating the need to update reverse proxy configurations manually.
- The `appleboy/ssh-action` natively handles SSH socket connections and abstracts away the need to manually configure `~/.ssh/known_hosts`.
- Explicit GitHub Action parameters for source and target directories prevent us from having to write raw Bash scripts over SSH pipelines, reducing syntax errors.

**What becomes harder**
- We must securely manage SSH keys and domain variables within GitHub Secrets.
- Traefik introduces an additional layer of Docker networking complexity (requiring a shared external `traefik-net` network) compared to direct host port bindings.
- Troubleshooting deployment failures requires inspecting GitHub Action logs rather than direct local feedback.

**What this commits us to**
- Our deployment infrastructure is tightly coupled to GitHub Actions as a runner, Appleboy SSH actions for remote execution, and Traefik's label-based dynamic routing for traffic management.

## Alternatives considered

**Nginx as a global reverse proxy.** Nginx is currently used to serve our static frontend assets. However, we rejected using Nginx as the global reverse proxy for our multi-environment server because routing new environments requires manually updating `nginx.conf` and reloading the Nginx process. Traefik reads Docker events natively to handle this automatically without restarts.

**webfactory/ssh-agent.** This action is highly reputable, but its primary use case is checking out private submodules during the CI build step. We rejected it because, for remote deployments, it forces developers to write raw `scp` and `ssh` commands and manually handle SSH strict host checking (`ssh-keyscan`), adding boilerplate to our workflows.

**LuisEnMarroquin/setup-ssh-action.** We rejected this srmaller, community-built alternative because it similarly just drops keys onto the runner, requiring manual bash execution for transfers. It lacks the explicit declarative parameters, widespread enterprise adoption, and continuous maintenance of the appleboy actions.

## Notes

The Appleboy actions are backed by extensive use in the open-source community and provide dedicated parameters for deployment paths, which aligns perfectly with our `/opt/stacks/{environment}` server layout. Nginx will remain inside our individual environment stacks to serve static assets efficiently.