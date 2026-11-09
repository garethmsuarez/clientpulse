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

- `backend/api/`: ASP.NET Core Minimal API targeting .NET 10, organized by feature (`Clients/`, `Communications/`, `Assessments/`, `Analysis/`), with EF Core mapping per feature and demo seed data in `Data/`.
- `backend/tests/`: xUnit tests for the backend.
- `frontend/`: React 19 + TypeScript + Vite application with `react-router`: client list, client detail, communication entry, assessment panel with review form, and assessment history.
- `compose.yaml`: local PostgreSQL 17 service, exposed on `localhost:5433`.

Do not assume product behavior or domain architecture that is not present in the code. Follow the established patterns in the files you touch, and keep changes focused.

## Running the application

### Prerequisites
- .NET 10 SDK
- Node.js 20.19+ or 22.12+ with npm (required by Vite 8)
- Docker and Docker Compose

### Start the application

1. Start the database (runs in the background):
   ```sh
   docker compose up -d postgres
   ```

2. First run only: set the local connection string (not committed) and apply
   the migrations. The credentials are the local-only values from `compose.yaml`.
   ```sh
   dotnet user-secrets set "ConnectionStrings:ClientPulse" "Host=localhost;Port=5433;Database=clientpulse;Username=clientpulse;Password=clientpulse_dev_only" --project backend/api
   dotnet tool restore
   dotnet ef database update --project backend/api
   ```
   Later, run `dotnet ef database update --project backend/api` again after
   pulling new migrations.

3. Start the backend API (from repository root):
   ```sh
   dotnet run --project backend/api
   ```
   This uses the `http` launch profile: the API listens on `http://localhost:5114`,
   with the OpenAPI document at `/openapi/v1.json`. The Vite dev server proxies
   `/api` to this address.

4. Start the frontend (in a separate terminal, from repository root):
   ```sh
   npm --prefix frontend run dev
   ```
   The React app opens on `http://localhost:5173`.

### Verify it's working
- Frontend: http://localhost:5173
- API: http://localhost:5114/openapi/v1.json (view available endpoints)

### Stop the application
- Press `Ctrl+C` in each terminal window.
- Stop the database: `docker compose down`

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
