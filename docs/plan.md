# Current status

- Milestones 1–7 are complete.
- Next: see Future (demo reliability, tests, and presentation preparation).
- API: `GET /api/clients`, `GET /api/clients/{id}`,
  `GET`/`POST /api/clients/{clientId}/communications`, and
  assessments under `/api/clients/{clientId}/assessments`: `POST` (analyze),
  `GET` (history), `GET latest`, `GET {assessmentId}`, and
  `POST {assessmentId}/reviews`; plus `GET /api/analysis/provider`.
- Analysis: one `ICommunicationAnalyzer` interface, chosen by server
  configuration (`Analysis:Provider`):
  - `Fixture` (default): a deterministic analyzer that returns predefined,
    clearly labelled results when a client's communications exactly match
    one of the synthetic seed scenarios (matched by content fingerprint).
  - `Anthropic`: Claude via the official `Anthropic` NuGet package
    (SDK types confined to `Analysis/Claude/`). Structured JSON output,
    validated before saving; evidence must cite the analyzed communications.
    Provenance (requested model, response model, effort) is stored.
- **Synthetic data only.** In Anthropic mode, communication text is sent to
  Anthropic's API. Do not enable it with real client communications.
- Review: append-only human reviews (Accepted, Adjusted, Rejected) on the
  latest assessment only; the original analyzer output is never changed.
  Reviewer names are self-reported and unverified. This is a review
  history, not a verified audit trail.
- Frontend: React with `react-router`, styled with a phase2online.com-inspired
  palette (light theme). The client view is split into nested routes under
  `/clients/:clientId`: Overview (landing summary), Assessment (analyze,
  latest result, expandable review form), Communications (list; evidence
  links highlight the cited item), Add communication (own page), History
  (list), and a read-only page per past assessment.
- Database: EF Core migrations `InitialCreate`,
  `AddAccountOwnerAndCommunications`, `AddAssessments`,
  `AddAssessmentReviews`, and `AddAssessmentProvenance` with synthetic
  seed data. They are not applied on startup; run
  `dotnet ef database update --project backend/api`.
- Tests: validation and model unit tests, plus endpoint tests against a
  separate `clientpulse_tests` database on the Compose PostgreSQL (recreated
  on each run; requires `docker compose up -d postgres`). Tests always use
  the fixture analyzer or fakes and never call the live API. Frontend tests
  use Vitest and React Testing Library with a stubbed `fetch`
  (`npm --prefix frontend run test`); they never call the API.

# Using the Anthropic analyzer (optional)

Synthetic data only. The key stays on the backend and is never logged.

```sh
dotnet user-secrets set "Anthropic:ApiKey" "<your key>" --project backend/api
dotnet user-secrets set "Analysis:Provider" "Anthropic" --project backend/api
```

Model and effort default to `claude-sonnet-5-5` at `medium` and can be
changed under `Analysis:Anthropic` (see `appsettings.json`), along with the
output-token limit, timeouts, and input limits. If the key is missing in
Anthropic mode the API fails at startup; it never falls back to fixture
results. Remove `Analysis:Provider` (or set it to `Fixture`) to switch back.

Calls are bounded by the SDK's per-attempt timeout (40 s) and its built-in
retries (2), plus a 120 s overall deadline. Failures return 502
(`provider_invalid_output`, `provider_output_truncated`, `provider_refused`,
`provider_configuration_error` for rejected credentials or requests),
503 (`provider_unavailable`), or 504 (`provider_timeout`) and save nothing.

# Milestones

1. Show seeded clients in React using a database-backed Minimal API. (complete)
2. Client details and communication entry. (complete)
3. Fixture-based analysis with supporting evidence. (complete)
4. Human assessment review and history. (complete)
5. Optional real AI provider. (complete)
   - Evaluate Claude Sonnet 5.5 against the synthetic scenarios before
     considering Opus.
6. Clean up the styling of the frontend, ideally with a color scheme similar
   to https://phase2online.com. (complete)
7. Break up the client view into smaller pieces. It currently shows a lot of
   information on one page, with multiple sections for entering different
   things. (complete)

# Future

1. Demo reliability, tests, and presentation preparation.
   - Consider isolating endpoint tests in a throwaway Postgres container
     (Testcontainers) instead of the shared Compose `clientpulse_tests`
     database; only `backend/tests/ApiFactory.cs` should need to change.
