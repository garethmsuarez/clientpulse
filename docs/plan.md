# Current status

- Solution, API, test, and React projects exist.
- EF Core dependencies are installed.
- PostgreSQL Docker Compose configuration exists.
- Milestone 1 (client list) is complete.
- Milestone 2 (client details and communication entry) is implemented;
  its migration `AddAccountOwnerAndCommunications` must be applied to the
  development database with `dotnet ef database update --project backend/api`.
- API endpoint tests use a separate `clientpulse_tests` database on the
  Compose PostgreSQL, recreated on each test run.

# First milestone

Show seeded clients in React using a database-backed Minimal API.

# Later milestones

1. Client details and communication entry.
2. Fixture-based analysis with supporting evidence.
3. Human assessment review and history.
4. Optional real AI provider.
5. Demo reliability, tests, and presentation preparation.
   - Consider isolating endpoint tests in a throwaway Postgres container
     (Testcontainers) instead of the shared Compose `clientpulse_tests`
     database; only `backend/tests/ApiFactory.cs` should need to change.
   - Clean up the UI appearance and styling (layout, spacing, typography,
     and consistent visual treatment across the list, detail, and form).
