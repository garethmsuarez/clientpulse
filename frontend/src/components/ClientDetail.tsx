import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiError.ts'
import { fetchClient, type ClientDetail as ClientDetailData } from '../api/clients.ts'
import { fetchCommunications, type Communication } from '../api/communications.ts'
import { AddCommunicationForm } from './AddCommunicationForm.tsx'

type DetailState =
  | { status: 'loading' }
  | { status: 'not-found' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; client: ClientDetailData; communications: Communication[] }

const dateTimeFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

function newestFirst(a: Communication, b: Communication): number {
  return Date.parse(b.occurredAt) - Date.parse(a.occurredAt) || b.id - a.id
}

export function ClientDetail() {
  const { clientId } = useParams()
  const id = clientId && /^\d+$/.test(clientId) ? Number(clientId) : null

  return (
    <main className="clients client-detail">
      <p>
        <Link to="/">← Back to clients</Link>
      </p>
      {id === null ? <ClientNotFound /> : <ClientDetailContent key={id} clientId={id} />}
    </main>
  )
}

function ClientNotFound() {
  return (
    <>
      <h1>Client not found</h1>
      <p>This client does not exist.</p>
    </>
  )
}

function ClientDetailContent({ clientId }: { clientId: number }) {
  const [state, setState] = useState<DetailState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchClient(clientId, controller.signal),
      fetchCommunications(clientId, controller.signal),
    ])
      .then(([client, communications]) => setState({ status: 'loaded', client, communications }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        if (error instanceof ApiError && error.status === 404) {
          setState({ status: 'not-found' })
          return
        }
        const message = error instanceof Error ? error.message : 'Failed to load client'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [clientId])

  if (state.status === 'loading') return <p>Loading client…</p>
  if (state.status === 'not-found') return <ClientNotFound />
  if (state.status === 'error') return <p role="alert">{state.message}</p>

  const { client, communications } = state

  function handleCreated(communication: Communication) {
    setState((current) =>
      current.status === 'loaded'
        ? { ...current, communications: [communication, ...current.communications].sort(newestFirst) }
        : current,
    )
  }

  return (
    <>
      <h1>{client.name}</h1>
      <dl className="client-facts">
        <dt>Account owner</dt>
        <dd>{client.accountOwner}</dd>
        <dt>Industry</dt>
        <dd>{client.industry}</dd>
      </dl>

      <section aria-labelledby="communications-heading">
        <h2 id="communications-heading">Communications</h2>
        {communications.length === 0 ? (
          <p>No communications recorded yet.</p>
        ) : (
          <ul className="communications">
            {communications.map((communication) => (
              <li key={communication.id}>
                <div className="communication-meta">
                  <span className="communication-type">{communication.type}</span>
                  <time dateTime={communication.occurredAt}>
                    {dateTimeFormat.format(new Date(communication.occurredAt))}
                  </time>
                </div>
                <h3>{communication.subject}</h3>
                <p className="communication-body">{communication.body}</p>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="add-communication-heading">
        <h2 id="add-communication-heading">Add communication</h2>
        <AddCommunicationForm clientId={client.id} onCreated={handleCreated} />
      </section>
    </>
  )
}
