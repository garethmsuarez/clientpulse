import { useCallback, useEffect, useState } from 'react'
import { Link, NavLink, Outlet, useParams } from 'react-router'
import { ApiError } from '../../api/apiError.ts'
import { fetchClient, type ClientDetail } from '../../api/clients.ts'
import { fetchCommunications, type Communication } from '../../api/communications.ts'
import { newestFirst, type ClientContext } from './clientContext.ts'

type LayoutState =
  | { status: 'loading' }
  | { status: 'not-found' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; client: ClientDetail; communications: Communication[] }

export function ClientNotFound() {
  return (
    <>
      <h1>Client not found</h1>
      <p>This client does not exist.</p>
    </>
  )
}

// Shell for /clients/:clientId/*: back link, client header, and tabs. Loads
// the client and its communications once and shares them with the tabs.
export function ClientLayout() {
  const { clientId } = useParams()
  const id = clientId && /^\d+$/.test(clientId) ? Number(clientId) : null

  return (
    <main className="clients client-detail">
      <nav className="back-nav" aria-label="Breadcrumb">
        <Link to="/" className="back-link">
          <span aria-hidden="true">←</span> Back to clients
        </Link>
      </nav>
      {id === null ? <ClientNotFound /> : <ClientLayoutContent key={id} clientId={id} />}
    </main>
  )
}

function ClientLayoutContent({ clientId }: { clientId: number }) {
  const [state, setState] = useState<LayoutState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([fetchClient(clientId, controller.signal), fetchCommunications(clientId, controller.signal)])
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

  const addCommunication = useCallback((communication: Communication) => {
    setState((current) =>
      current.status === 'loaded'
        ? { ...current, communications: [communication, ...current.communications].sort(newestFirst) }
        : current,
    )
  }, [])

  if (state.status === 'loading') return <p>Loading client…</p>
  if (state.status === 'not-found') return <ClientNotFound />
  if (state.status === 'error') return <p role="alert">{state.message}</p>

  const { client, communications } = state
  const context: ClientContext = { client, communications, addCommunication }

  return (
    <>
      <header className="client-header">
        <h1>{client.name}</h1>
        <dl className="client-facts">
          <dt>Account owner</dt>
          <dd>{client.accountOwner}</dd>
          <dt>Industry</dt>
          <dd>{client.industry}</dd>
        </dl>
      </header>

      <nav className="client-tabs" aria-label="Client sections">
        <NavLink to="." end>
          Overview
        </NavLink>
        <NavLink to="assessment">Assessment</NavLink>
        <NavLink to="communications">Communications ({communications.length})</NavLink>
        <NavLink to="history">History</NavLink>
      </nav>

      <Outlet context={context} />
    </>
  )
}

export function ClientSectionNotFound() {
  return (
    <section>
      <h2>Page not found</h2>
      <p>
        <Link to=".." relative="path">
          Back to overview
        </Link>
      </p>
    </section>
  )
}
