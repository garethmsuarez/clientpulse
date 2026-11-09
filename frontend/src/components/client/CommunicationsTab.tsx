import { useEffect } from 'react'
import { Link, useLocation } from 'react-router'
import { dateTimeFormat } from '../../format.ts'
import { useClientContext } from './clientContext.ts'

// The URL hash (#communication-{id}) marks a communication to highlight, for
// evidence links and newly added communications. CSS :target does not update
// on client-side navigation, so the highlight is driven from the hash here.
function highlightedIdFrom(hash: string): number | null {
  const match = /^#communication-(\d+)$/.exec(hash)
  return match ? Number(match[1]) : null
}

export function CommunicationsTab() {
  const { communications } = useClientContext()
  const { hash } = useLocation()
  const highlightedId = highlightedIdFrom(hash)

  useEffect(() => {
    if (highlightedId === null) return
    document.getElementById(`communication-${highlightedId}`)?.scrollIntoView?.({ block: 'center' })
  }, [highlightedId])

  return (
    <section aria-labelledby="communications-heading">
      <div className="section-heading">
        <h2 id="communications-heading">Communications</h2>
        <Link to="new" className="button-link">
          Add communication
        </Link>
      </div>
      {communications.length === 0 ? (
        <p>No communications recorded yet.</p>
      ) : (
        <ul className="communications">
          {communications.map((communication) => (
            <li
              key={communication.id}
              id={`communication-${communication.id}`}
              className={communication.id === highlightedId ? 'is-highlighted' : undefined}
              aria-current={communication.id === highlightedId ? 'true' : undefined}
            >
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
  )
}
