import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { fetchClients, type ClientSummary } from '../api/clients.ts'
import type { AssessmentSummary } from '../api/assessments.ts'
import { dateFormat } from '../format.ts'
import {
  effectiveSeverity,
  initialDirection,
  parseSort,
  sortClients,
  type ClientSort,
  type SortKey,
} from './clientSort.ts'

type ClientsState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; clients: ClientSummary[] }

export function ClientList() {
  const [state, setState] = useState<ClientsState>({ status: 'loading' })
  const [searchParams, setSearchParams] = useSearchParams()
  const sort = parseSort(searchParams)

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

  // The sort lives in the URL so it survives refresh and returning from a client.
  function sortBy(key: SortKey) {
    const direction =
      sort.key === key ? (sort.direction === 'asc' ? 'desc' : 'asc') : initialDirection[key]
    setSearchParams({ sort: key, dir: direction }, { replace: true })
  }

  return (
    <main className="clients">
      <h1>Clients</h1>
      {state.status === 'loading' && <p>Loading clients…</p>}
      {state.status === 'error' && <p role="alert">{state.message}</p>}
      {state.status === 'loaded' && state.clients.length === 0 && <p>No clients found.</p>}
      {state.status === 'loaded' && state.clients.length > 0 && (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <SortableHeader label="Client" sortKey="name" sort={sort} onSort={sortBy} />
                <th scope="col">Industry</th>
                <SortableHeader label="Severity" sortKey="severity" sort={sort} onSort={sortBy} />
                <th scope="col">Review</th>
                <SortableHeader label="Assessed" sortKey="assessed" sort={sort} onSort={sortBy} />
              </tr>
            </thead>
            <tbody>
              {sortClients(state.clients, sort).map((client) => (
                <tr key={client.id}>
                  <td>
                    <Link to={`/clients/${client.id}`}>{client.name}</Link>
                  </td>
                  <td>{client.industry}</td>
                  <td>
                    <SeverityCell assessment={client.latestAssessment} />
                  </td>
                  <td>
                    <ReviewCell assessment={client.latestAssessment} />
                  </td>
                  <td>
                    {client.latestAssessment ? (
                      <time dateTime={client.latestAssessment.createdAt}>
                        {dateFormat.format(new Date(client.latestAssessment.createdAt))}
                      </time>
                    ) : (
                      <span className="muted">—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}

function SortableHeader({
  label,
  sortKey,
  sort,
  onSort,
}: {
  label: string
  sortKey: SortKey
  sort: ClientSort
  onSort: (key: SortKey) => void
}) {
  const active = sort.key === sortKey
  const ariaSort = active ? (sort.direction === 'asc' ? 'ascending' : 'descending') : 'none'

  return (
    <th scope="col" aria-sort={ariaSort}>
      <button type="button" className="sort-button" onClick={() => onSort(sortKey)}>
        {label}
        <span className="sort-indicator" aria-hidden="true">
          {active ? (sort.direction === 'asc' ? '▲' : '▼') : '↕'}
        </span>
      </button>
    </th>
  )
}

function SeverityCell({ assessment }: { assessment: AssessmentSummary | null }) {
  if (!assessment) return <span className="muted">No assessment</span>

  const severity = effectiveSeverity(assessment)

  return (
    <span className="severity-cell">
      {severity ? (
        <span className={`attention attention-${severity.toLowerCase()}`}>{severity}</span>
      ) : (
        <span className="attention attention-rejected">Rejected</span>
      )}
      <span className="tag">{assessment.isFixture ? 'Fixture' : 'AI'}</span>
      {!assessment.isCurrent && <span className="tag">Out of date</span>}
    </span>
  )
}

function ReviewCell({ assessment }: { assessment: AssessmentSummary | null }) {
  if (!assessment) return <span className="muted">—</span>

  switch (assessment.reviewStatus) {
    case 'Unreviewed':
      return <span className="muted">Unreviewed</span>
    case 'Accepted':
      return <>Accepted</>
    case 'Adjusted':
      return <>Adjusted from {assessment.attentionLevel}</>
    case 'Rejected':
      return <>Rejected (analyzer: {assessment.attentionLevel})</>
  }
}
