import { vi } from 'vitest'
import type { Assessment, AssessmentSummary } from '../api/assessments.ts'
import type { ClientDetail } from '../api/clients.ts'
import type { Communication } from '../api/communications.ts'

// Synthetic test data only.
export const client: ClientDetail = {
  id: 1,
  name: 'Synthetic Logistics',
  industry: 'Logistics',
  accountOwner: 'Synthetic Owner',
}

export const communications: Communication[] = [
  {
    id: 3,
    clientId: 1,
    type: 'Note',
    occurredAt: '2026-10-28T09:30:00Z',
    subject: 'Plan sent',
    body: 'Synthetic note body.',
    createdAt: '2026-10-28T09:30:00Z',
  },
  {
    id: 2,
    clientId: 1,
    type: 'Call',
    occurredAt: '2026-10-21T16:00:00Z',
    subject: 'Late deliveries',
    body: 'Synthetic call body.',
    createdAt: '2026-10-21T16:00:00Z',
  },
  {
    id: 1,
    clientId: 1,
    type: 'Email',
    occurredAt: '2026-10-06T14:20:00Z',
    subject: 'Volumes',
    body: 'Synthetic email body.',
    createdAt: '2026-10-06T14:20:00Z',
  },
]

export const latestAssessment: Assessment = {
  id: 12,
  clientId: 1,
  createdAt: '2026-11-08T10:00:00Z',
  source: 'Fixture',
  isFixture: true,
  fixtureName: 'synthetic-scenario',
  requestedModel: null,
  responseModel: null,
  effort: null,
  disclaimer: 'Fixture result: predefined for a synthetic demo scenario.',
  summary: 'Synthetic latest summary.',
  attentionLevel: 'Medium',
  concerns: [
    {
      description: 'Synthetic concern.',
      evidence: [{ communicationId: 2, type: 'Call', occurredAt: '2026-10-21T16:00:00Z', subject: 'Late deliveries' }],
    },
  ],
  positiveSignals: [],
  analyzedCommunicationIds: [1, 2, 3],
  isCurrent: true,
  isLatest: true,
  reviewStatus: 'Unreviewed',
  reviewerApprovedAttentionLevel: null,
  reviews: [],
}

export const olderAssessment: Assessment = {
  ...latestAssessment,
  id: 11,
  createdAt: '2026-11-01T10:00:00Z',
  summary: 'Synthetic older summary.',
  attentionLevel: 'Low',
  isLatest: false,
  reviewStatus: 'Accepted',
  reviewerApprovedAttentionLevel: 'Low',
}

function summaryOf(assessment: Assessment): AssessmentSummary {
  return {
    id: assessment.id,
    createdAt: assessment.createdAt,
    source: assessment.source,
    isFixture: assessment.isFixture,
    fixtureName: assessment.fixtureName,
    responseModel: assessment.responseModel,
    attentionLevel: assessment.attentionLevel,
    isCurrent: assessment.isCurrent,
    isLatest: assessment.isLatest,
    reviewStatus: assessment.reviewStatus,
    reviewerApprovedAttentionLevel: assessment.reviewerApprovedAttentionLevel,
    latestReview: null,
  }
}

type Handler = (body: unknown) => Response

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

export function problem(status: number, title: string): Response {
  return new Response(JSON.stringify({ title, status }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

// Stubs fetch with the default API for client 1. Overrides replace or add
// routes, keyed as "METHOD /path". Returns the mock so tests can inspect calls.
export function installFakeApi(overrides: Record<string, Handler> = {}) {
  const routes: Record<string, Handler> = {
    'GET /api/analysis/provider': () => json({ provider: 'Fixture', model: null, effort: null }),
    'GET /api/clients/1': () => json(client),
    'GET /api/clients/1/communications': () => json(communications),
    'GET /api/clients/1/assessments/latest': () => json(latestAssessment),
    'GET /api/clients/1/assessments': () => json([latestAssessment, olderAssessment].map(summaryOf)),
    'GET /api/clients/1/assessments/12': () => json(latestAssessment),
    'GET /api/clients/1/assessments/11': () => json(olderAssessment),
    ...overrides,
  }

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.pathname : input.url
    const key = `${init?.method ?? 'GET'} ${url}`
    const handler = routes[key]
    if (!handler) return problem(404, `No fake route for ${key}`)
    return handler(init?.body ? JSON.parse(String(init.body)) : undefined)
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export { json }
