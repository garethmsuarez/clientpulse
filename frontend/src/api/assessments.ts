import { toApiError } from './apiError.ts'
import type { CommunicationType } from './communications.ts'

export type AttentionLevel = 'Low' | 'Medium' | 'High'

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
