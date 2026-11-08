import { toApiError } from './apiError.ts'

export type ClientSummary = {
  id: number
  name: string
  industry: string
}

export type ClientDetail = ClientSummary & {
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
