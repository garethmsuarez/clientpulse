import { toApiError } from './apiError.ts'

export const communicationTypes = ['Email', 'Call', 'Meeting', 'Note'] as const

export type CommunicationType = (typeof communicationTypes)[number]

export type Communication = {
  id: number
  clientId: number
  type: CommunicationType
  occurredAt: string
  subject: string
  body: string
  createdAt: string
}

export type CreateCommunicationRequest = {
  type: CommunicationType
  occurredAt: string
  subject: string
  body: string
}

// Keep in sync with CommunicationRules on the server (which is authoritative).
export const subjectMaxLength = 200
export const bodyMaxLength = 10_000

export async function fetchCommunications(
  clientId: number,
  signal?: AbortSignal,
): Promise<Communication[]> {
  const response = await fetch(`/api/clients/${clientId}/communications`, { signal })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load communications')
  }

  return (await response.json()) as Communication[]
}

export async function createCommunication(
  clientId: number,
  request: CreateCommunicationRequest,
): Promise<Communication> {
  const response = await fetch(`/api/clients/${clientId}/communications`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to save communication')
  }

  return (await response.json()) as Communication
}
