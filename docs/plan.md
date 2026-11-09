# Current status

- Milestones 1–4 are complete.
- Next: milestone 5, optional real AI provider.
- API: `GET /api/clients`, `GET /api/clients/{id}`,
  `GET`/`POST /api/clients/{clientId}/communications`, and
  assessments under `/api/clients/{clientId}/assessments`: `POST` (analyze),
  `GET` (history), `GET latest`, `GET {assessmentId}`, and
  `POST {assessmentId}/reviews`.
- Analysis: one `ICommunicationAnalyzer` interface; the only implementation
  is a deterministic fixture analyzer that returns predefined, clearly
  labelled results when a client's communications exactly match one of the
  synthetic seed scenarios (matched by content fingerprint, not ids).
- Review: append-only human reviews (Accepted, Adjusted, Rejected) on the
  latest assessment only; the original analyzer output is never changed.
  Reviewer names are self-reported and unverified. This is a review
  history, not a verified audit trail.
- Frontend: React with `react-router`: client list, client detail, an
  add-communication form, an assessment panel with evidence links and a
  review form, and a read-only assessment history.
- Database: EF Core migrations `InitialCreate`,
  `AddAccountOwnerAndCommunications`, `AddAssessments`, and
  `AddAssessmentReviews` with synthetic
  seed data. They are not applied on startup; run
  `dotnet ef database update --project backend/api`.
- Tests: validation and model unit tests, plus endpoint tests against a
  separate `clientpulse_tests` database on the Compose PostgreSQL (recreated
  on each run; requires `docker compose up -d postgres`).

# Milestones

1. Show seeded clients in React using a database-backed Minimal API. (complete)
2. Client details and communication entry. (complete)
3. Fixture-based analysis with supporting evidence. (complete)
4. Human assessment review and history. (complete)
5. Optional real AI provider.
6. Demo reliability, tests, and presentation preparation.
   - Consider isolating endpoint tests in a throwaway Postgres container
     (Testcontainers) instead of the shared Compose `clientpulse_tests`
     database; only `backend/tests/ApiFactory.cs` should need to change.
   - Clean up the UI appearance and styling (layout, spacing, typography,
     and consistent visual treatment across the list, detail, and form).
