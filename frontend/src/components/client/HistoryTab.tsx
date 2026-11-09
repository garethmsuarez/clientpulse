import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { fetchAssessments, type AssessmentSummary } from '../../api/assessments.ts'
import { dateTimeFormat } from '../../format.ts'
import { ReviewStatusLine } from '../AssessmentView.tsx'
import { useClientContext } from './clientContext.ts'

type HistoryState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessments: AssessmentSummary[] }

// Read-only list of every saved assessment, newest first. Each links to its
// own detail page.
export function HistoryTab() {
  const { client } = useClientContext()
  const [state, setState] = useState<HistoryState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetchAssessments(client.id, controller.signal)
      .then((assessments) => setState({ status: 'loaded', assessments }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load assessment history'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [client.id])

  return (
    <section aria-labelledby="history-heading">
      <h2 id="history-heading">Assessment history</h2>
      {state.status === 'loading' && <p>Loading history…</p>}
      {state.status === 'error' && <p role="alert">{state.message}</p>}
      {state.status === 'loaded' && state.assessments.length === 0 && <p>No assessments yet.</p>}
      {state.status === 'loaded' && state.assessments.length > 0 && (
        <ul className="history">
          {state.assessments.map((summary) => (
            <li key={summary.id}>
              <Link to={String(summary.id)} className="history-link">
                <span className="history-date">{dateTimeFormat.format(new Date(summary.createdAt))}</span>
                <span className={`attention attention-${summary.attentionLevel.toLowerCase()}`}>
                  Analyzer: {summary.attentionLevel}
                </span>
                {summary.isFixture ? (
                  <span className="tag">Fixture</span>
                ) : (
                  <span className="tag tag-ai">AI{summary.responseModel ? ` · ${summary.responseModel}` : ''}</span>
                )}
                {summary.isLatest && <span className="tag">Latest</span>}
                {!summary.isCurrent && <span className="tag">Out of date</span>}
              </Link>
              <ReviewStatusLine assessment={summary} />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
