## Context

- A digital replacement for the paper-based Operational Restriction workflow: authoring, multi-party approval and signing, operator read-and-sign, cancellation, a register of restrictions in force, automatic notification, and a five-year archive, usable remotely.

## LLMs usage

- Mistral
- ChatGPT
- Claude

## Architecture

- All backend code must follow the MVCS architecture
- Frontend: Typescript React + Node.js + tailwind CSS
- Database: Postgres

## Security

- Authentication must use JWT.

## Code Style

- Use camelCase for variable and function names.
- Use descriptive variable names and function names.

## Model Tone

- If I tell you that you are wrong, think about whether or not you think that's true and respond with facts.
- Avoid apologizing or making conciliatory statements.
- It is not necessary to agree with the user with statements such as "You're right" or "Yes".
- Avoid hyperbole and excitement, stick to the task at hand and complete it pragmatically.

## Boundaries

**Ask First:** Before modifying existing documents in a major way
**Never Do:** Modify code in ´src/´, edit config files, commit secrets
