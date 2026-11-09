import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/apiError.ts'
import { fetchClient, type ClientDetail as ClientDetailData } from '../api/clients.ts'
import { fetchCommunications, type Communication } from '../api/communications.ts'
import { dateTimeFormat } from '../format.ts'
import { AddCommunicationForm } from './AddCommunicationForm.tsx'
import { AssessmentHistory } from './AssessmentHistory.tsx'
import { AssessmentPanel } from './AssessmentPanel.tsx'

type DetailState =
  | { status: 'loading' }
  | { status: 'not-found' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; client: ClientDetailData; communications: Communication[] }

function newestFirst(a: Communication, b: Communication): number {
  return Date.parse(b.occurredAt) - Date.parse(a.occurredAt) || b.id - a.id
}

export function ClientDetail() {
  const { clientId } = useParams()
  const id = clientId && /^\d+$/.test(clientId) ? Number(clientId) : null

  return (
    <main className="clients client-detail">
      <nav className="back-nav" aria-label="Breadcrumb">
        <Link to="/" className="back-link">
          <span aria-hidden="true">←</span> Back to clients
        </Link>
      </nav>
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
  // Bumped after a communication, assessment, or review is saved so the
  // assessment panel and history re-read saved data. It never re-analyzes.
  const [assessmentRefresh, setAssessmentRefresh] = useState(0)

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
    setAssessmentRefresh((n) => n + 1)
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

      <AssessmentPanel
        clientId={client.id}
        hasCommunications={communications.length > 0}
        refreshKey={assessmentRefresh}
        onChanged={() => setAssessmentRefresh((n) => n + 1)}
      />

      <section aria-labelledby="communications-heading">
        <h2 id="communications-heading">Communications</h2>
        {communications.length === 0 ? (
          <p>No communications recorded yet.</p>
        ) : (
          <ul className="communications">
            {communications.map((communication) => (
              <li key={communication.id} id={`communication-${communication.id}`}>
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

      <AssessmentHistory clientId={client.id} refreshKey={assessmentRefresh} />
    </>
  )
}
