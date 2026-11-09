# Current status

- Milestones 1–3 are complete.
- Next: milestone 4, human assessment review and history.
- API: `GET /api/clients`, `GET /api/clients/{id}`,
  `GET`/`POST /api/clients/{clientId}/communications`, and
  `POST /api/clients/{clientId}/assessments` plus
  `GET /api/clients/{clientId}/assessments/latest`.
- Analysis: one `ICommunicationAnalyzer` interface; the only implementation
  is a deterministic fixture analyzer that returns predefined, clearly
  labelled results when a client's communications exactly match one of the
  synthetic seed scenarios (matched by content fingerprint, not ids).
- Frontend: React with `react-router`: client list, client detail, an
  add-communication form, and an assessment panel with evidence links.
- Database: EF Core migrations `InitialCreate`,
  `AddAccountOwnerAndCommunications`, and `AddAssessments` with synthetic
  seed data. They are not applied on startup; run
  `dotnet ef database update --project backend/api`.
- Tests: validation and model unit tests, plus endpoint tests against a
  separate `clientpulse_tests` database on the Compose PostgreSQL (recreated
  on each run; requires `docker compose up -d postgres`).

# Milestones

1. Show seeded clients in React using a database-backed Minimal API. (complete)
2. Client details and communication entry. (complete)
3. Fixture-based analysis with supporting evidence. (complete)
4. Human assessment review and history.
5. Optional real AI provider.
6. Demo reliability, tests, and presentation preparation.
   - Consider isolating endpoint tests in a throwaway Postgres container
     (Testcontainers) instead of the shared Compose `clientpulse_tests`
     database; only `backend/tests/ApiFactory.cs` should need to change.
   - Clean up the UI appearance and styling (layout, spacing, typography,
     and consistent visual treatment across the list, detail, and form).
