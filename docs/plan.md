# Current status

- Milestones 1 and 2 are complete and merged to `main`.
- Next: milestone 3, fixture-based analysis with supporting evidence.
- API: `GET /api/clients`, `GET /api/clients/{id}`, and
  `GET`/`POST /api/clients/{clientId}/communications`.
- Frontend: React with `react-router`: client list, client detail, and an
  add-communication form.
- Database: EF Core migrations `InitialCreate` and
  `AddAccountOwnerAndCommunications` with synthetic seed data. They are not
  applied on startup; run `dotnet ef database update --project backend/api`.
- Tests: validation and model unit tests, plus endpoint tests against a
  separate `clientpulse_tests` database on the Compose PostgreSQL (recreated
  on each run; requires `docker compose up -d postgres`).

# Milestones

1. Show seeded clients in React using a database-backed Minimal API. (complete)
2. Client details and communication entry. (complete)
3. Fixture-based analysis with supporting evidence.
4. Human assessment review and history.
5. Optional real AI provider.
6. Demo reliability, tests, and presentation preparation.
   - Consider isolating endpoint tests in a throwaway Postgres container
     (Testcontainers) instead of the shared Compose `clientpulse_tests`
     database; only `backend/tests/ApiFactory.cs` should need to change.
   - Clean up the UI appearance and styling (layout, spacing, typography,
     and consistent visual treatment across the list, detail, and form).
