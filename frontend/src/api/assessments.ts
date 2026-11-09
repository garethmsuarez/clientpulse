import { toApiError } from './apiError.ts'
import type { CommunicationType } from './communications.ts'

export type AttentionLevel = 'Low' | 'Medium' | 'High'

export const attentionLevels: AttentionLevel[] = ['Low', 'Medium', 'High']

export type ReviewDecision = 'Accepted' | 'Adjusted' | 'Rejected'

export const reviewDecisions: ReviewDecision[] = ['Accepted', 'Adjusted', 'Rejected']

export type ReviewStatus = 'Unreviewed' | ReviewDecision

// reviewerName is self-reported and unverified.
export type Review = {
  id: number
  assessmentId: number
  createdAt: string
  decision: ReviewDecision
  reviewedAttentionLevel: AttentionLevel | null
  notes: string | null
  reviewerName: string
}

export type CreateReviewRequest = {
  decision: ReviewDecision
  reviewedAttentionLevel: AttentionLevel | null
  notes: string | null
  reviewerName: string
}

// Keep in sync with ReviewRules on the server (which is authoritative).
export const reviewNotesMaxLength = 2_000
export const reviewerNameMaxLength = 100

export type Evidence = {
  communicationId: number
  type: CommunicationType
  occurredAt: string
  subject: string
}

export type Signal = {
  description: string
  evidence: Evidence[]
}

export type Assessment = {
  id: number
  clientId: number
  createdAt: string
  source: 'Fixture'
  isFixture: boolean
  fixtureName: string | null
  disclaimer: string | null
  summary: string
  attentionLevel: AttentionLevel
  concerns: Signal[]
  positiveSignals: Signal[]
  analyzedCommunicationIds: number[]
  isCurrent: boolean
  isLatest: boolean
  reviewStatus: ReviewStatus
  reviewerApprovedAttentionLevel: AttentionLevel | null
  reviews: Review[]
}

export type AssessmentSummary = {
  id: number
  createdAt: string
  source: 'Fixture'
  isFixture: boolean
  fixtureName: string | null
  attentionLevel: AttentionLevel
  isCurrent: boolean
  isLatest: boolean
  reviewStatus: ReviewStatus
  reviewerApprovedAttentionLevel: AttentionLevel | null
  latestReview: Review | null
}

// Resolves to null when the client has no assessment yet (HTTP 204, no body).
export async function fetchLatestAssessment(
  clientId: number,
  signal?: AbortSignal,
): Promise<Assessment | null> {
  const response = await fetch(`/api/clients/${clientId}/assessments/latest`, { signal })

  if (response.status === 204) return null

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load assessment')
  }

  return (await response.json()) as Assessment
}

export async function analyzeCommunications(clientId: number): Promise<Assessment> {
  const response = await fetch(`/api/clients/${clientId}/assessments`, { method: 'POST' })

  if (!response.ok) {
    throw await toApiError(response, 'Analysis failed')
  }

  return (await response.json()) as Assessment
}

export async function fetchAssessments(
  clientId: number,
  signal?: AbortSignal,
): Promise<AssessmentSummary[]> {
  const response = await fetch(`/api/clients/${clientId}/assessments`, { signal })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load assessment history')
  }

  return (await response.json()) as AssessmentSummary[]
}

export async function fetchAssessment(
  clientId: number,
  assessmentId: number,
  signal?: AbortSignal,
): Promise<Assessment> {
  const response = await fetch(`/api/clients/${clientId}/assessments/${assessmentId}`, { signal })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load assessment')
  }

  return (await response.json()) as Assessment
}

export async function createReview(
  clientId: number,
  assessmentId: number,
  request: CreateReviewRequest,
): Promise<Review> {
  const response = await fetch(`/api/clients/${clientId}/assessments/${assessmentId}/reviews`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to save review')
  }

  return (await response.json()) as Review
}
