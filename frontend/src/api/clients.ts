import { toApiError } from './apiError.ts'
import type { AssessmentSummary } from './assessments.ts'

export type ClientSummary = {
  id: number
  name: string
  industry: string
  // Null when the client has not been assessed yet.
  latestAssessment: AssessmentSummary | null
}

export type ClientDetail = {
  id: number
  name: string
  industry: string
  accountOwner: string
}

export async function fetchClients(signal?: AbortSignal): Promise<ClientSummary[]> {
  const response = await fetch('/api/clients', { signal })

  if (!response.ok) {
    throw new Error(`Failed to load clients (HTTP ${response.status})`)
  }

  return (await response.json()) as ClientSummary[]
}

export async function fetchClient(clientId: number, signal?: AbortSignal): Promise<ClientDetail> {
  const response = await fetch(`/api/clients/${clientId}`, { signal })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load client')
  }

  return (await response.json()) as ClientDetail
}
