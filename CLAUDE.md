# ClientPulse

An interview demo application for reviewing client communications,
identifying concerning signals, and recording human review.

## Stack and structure
- C#/.NET 10 Minimal API in backend/api.
- React + TypeScript + Vite in frontend.
- PostgreSQL through EF Core/Npgsql.
- Tests in backend/tests.
- PostgreSQL runs through the existing Docker Compose configuration.
- Lowercase directories; conventional PascalCase C# types and namespaces.

## Architecture
- One backend application and one database.
- Group Minimal API routes in feature-specific endpoint files.
- Use EF Core directly for simple queries and persistence.
- Introduce services for meaningful workflows, not CRUD forwarding.
- Put AI analysis behind one application-owned interface.
- No controllers, MediatR, generic repositories, message brokers,
  microservices, or additional production projects without approval.

## Development rules
- Inspect existing files before proposing or changing implementation.
- Do not overwrite or recreate existing scaffolding unnecessarily.
- Implement only the requested milestone.
- Ask before adding dependencies or changing architecture.
- Use synthetic client communications only.
- Never log secrets or communication bodies.
- Clearly label fixture analysis; do not present it as real inference.
- Do not commit, push, delete database volumes, or apply destructive
  database changes without explicit approval.

## Project layout

- `backend/api/`: ASP.NET Core Web API targeting .NET 10; currently a starter API.
- `backend/tests/`: xUnit tests for the backend.
- `frontend/`: React 19 + TypeScript + Vite application; currently a starter UI.
- `compose.yaml`: local PostgreSQL 17 service, exposed on `localhost:5433`.

Do not assume product behavior or domain architecture that is not present in the code. Follow the established patterns in the files you touch, and keep changes focused.

## Common commands

Run from the repository root unless noted:

```sh
dotnet test ClientPulse.slnx
```

Frontend commands run from `frontend/`:

```sh
npm run dev
npm run build
npm run lint
```

Start the local database with `docker compose up -d postgres`; stop it with `docker compose down`. The Compose database credentials are for local development only.

## Working practices

- Add or update tests for behavior changes and run the narrowest relevant checks, then broader checks when practical.
- Keep nullable reference types enabled in .NET code and use the repository's existing xUnit conventions.
- Keep TypeScript types explicit where useful and follow the existing React/Vite structure.
- Do not commit secrets, local environment files, build output, or generated dependency artifacts.
- Inspect `git diff` before finishing and preserve unrelated user changes.

## Verification
- Backend: dotnet build and dotnet test.
- Frontend: npm --prefix frontend run lint.
- Frontend: npm --prefix frontend run build.
- Report commands actually run, outcomes, and anything not verified.
- Explain important changes and trade-offs after each task.
