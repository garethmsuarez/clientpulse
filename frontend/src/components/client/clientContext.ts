import { useOutletContext } from 'react-router'
import type { ClientDetail } from '../../api/clients.ts'
import type { Communication } from '../../api/communications.ts'

// Shared by ClientLayout with its tab routes through the router outlet.
export type ClientContext = {
  client: ClientDetail
  communications: Communication[]
  addCommunication: (communication: Communication) => void
}

export function useClientContext(): ClientContext {
  return useOutletContext<ClientContext>()
}

export function newestFirst(a: Communication, b: Communication): number {
  return Date.parse(b.occurredAt) - Date.parse(a.occurredAt) || b.id - a.id
}
