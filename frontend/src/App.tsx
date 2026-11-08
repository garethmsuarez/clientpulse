import { useEffect, useState } from 'react'
import { fetchClients, type ClientSummary } from './api/clients.ts'
import './App.css'

type ClientsState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; clients: ClientSummary[] }

function App() {
  const [state, setState] = useState<ClientsState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetchClients(controller.signal)
      .then((clients) => setState({ status: 'loaded', clients }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load clients'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [])

  return (
    <main className="clients">
      <h1>Clients</h1>
      {state.status === 'loading' && <p>Loading clients…</p>}
      {state.status === 'error' && <p role="alert">{state.message}</p>}
      {state.status === 'loaded' && state.clients.length === 0 && <p>No clients found.</p>}
      {state.status === 'loaded' && state.clients.length > 0 && (
        <table>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Industry</th>
            </tr>
          </thead>
          <tbody>
            {state.clients.map((client) => (
              <tr key={client.id}>
                <td>{client.name}</td>
                <td>{client.industry}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}

export default App
