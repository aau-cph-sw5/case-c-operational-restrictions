## Context

- A digital replacement for the paper-based Operational Restriction workflow: authoring, multi-party approval and signing, operator read-and-sign, cancellation, a register of restrictions in force, automatic notification, and a five-year archive, usable remotely.

## Folder Structure
Always keep files in an appropriate folder. Models for databases (Dal) should be placed in Models/ModelDal´ Same for DTos Models/ModelsDto´.

## Classes
Never Add classes in controllers or sevices unless told to do so.

## LLMs usage

- Mistral
- ChatGPT
- Claude
- Copilot
- Gemini

## Usage Principles

- Use AI as a development assistant, not as a replacement for critical thinking
- Always review and validate AI-generated code before implementation
- Maintain human oversight for all automated decisions

## Backend

- All backend code must follow the MVCS architecture
- Database: Postgres
- Node.js

## Backend commands

- dotnet build
- dotnet run
- dotnet test

## Frontend 

- Typescript React 
- tailwind CSS

## Frontend commands

- pnpm dev
- pnpm build
- pnpm test
- pnpm lint

## Architectural Guardrails

- Never expose domain entities, EF Core entities, database models, or persistence models directly through API endpoints.
- API request and response contracts must use dedicated DTOs.
- Keep DTOs separate from domain and persistence models.
- Use standard HTTP status codes consistently.

- Keep domain/business logic independent from persistence concerns where practical.
- Controllers/endpoints should not contain business logic.
- Controllers/endpoints are responsible primarily for HTTP concerns, validation orchestration, and translating application results into HTTP responses.

## Security

- Authentication must use JWT.

## Code Style

- Use camelCase for variable and function names.
- Use descriptive variable names and function names.
- It must follow the standard conventions for the specified languages (c# and typescript)

## Model Tone

- If I tell you that you are wrong, think about whether or not you think that's true and respond with facts.
- Avoid apologizing or making conciliatory statements.
- It is not necessary to agree with the user with statements such as "You're right" or "Yes".
- Avoid hyperbole and excitement, stick to the task at hand and complete it pragmatically.

## Boundaries

**Ask First:** Before modifying existing documents in a major way
**Ask First:** When unsure between approaches, explain both and let me choose
**Never Do:** edit config files or commit secrets
**Never Do:** Edit the AIAgentGuideline.md

