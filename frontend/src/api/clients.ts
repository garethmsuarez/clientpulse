export type ClientSummary = {
  id: number
  name: string
  industry: string
}

export async function fetchClients(signal?: AbortSignal): Promise<ClientSummary[]> {
  const response = await fetch('/api/clients', { signal })

  if (!response.ok) {
    throw new Error(`Failed to load clients (HTTP ${response.status})`)
  }

  return (await response.json()) as ClientSummary[]
}
