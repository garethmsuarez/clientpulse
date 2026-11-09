import { useEffect, useState } from 'react'
import {
  fetchAssessment,
  fetchAssessments,
  type Assessment,
  type AssessmentSummary,
} from '../api/assessments.ts'
import { dateTimeFormat } from '../format.ts'
import { AssessmentView, ReviewStatusLine } from './AssessmentView.tsx'

type HistoryState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessments: AssessmentSummary[] }

type AssessmentHistoryProps = {
  clientId: number
  refreshKey: number
}

// Read-only list of every saved assessment for the client, newest first.
export function AssessmentHistory({ clientId, refreshKey }: AssessmentHistoryProps) {
  const [state, setState] = useState<HistoryState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetchAssessments(clientId, controller.signal)
      .then((assessments) => setState({ status: 'loaded', assessments }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load assessment history'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [clientId, refreshKey])

  return (
    <section aria-labelledby="history-heading">
      <h2 id="history-heading">Assessment history</h2>
      {state.status === 'loading' && <p>Loading history…</p>}
      {state.status === 'error' && <p role="alert">{state.message}</p>}
      {state.status === 'loaded' && state.assessments.length === 0 && <p>No assessments yet.</p>}
      {state.status === 'loaded' && state.assessments.length > 0 && (
        <ul className="history">
          {state.assessments.map((summary) => (
            <HistoryItem key={summary.id} clientId={clientId} summary={summary} refreshKey={refreshKey} />
          ))}
        </ul>
      )}
    </section>
  )
}

type DetailState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessment: Assessment }

function HistoryItem({
  clientId,
  summary,
  refreshKey,
}: {
  clientId: number
  summary: AssessmentSummary
  refreshKey: number
}) {
  const [open, setOpen] = useState(false)
  const [detail, setDetail] = useState<DetailState>({ status: 'idle' })

  // Details load only when expanded, and reload when history refreshes.
  useEffect(() => {
    if (!open) return
    const controller = new AbortController()

    fetchAssessment(clientId, summary.id, controller.signal)
      .then((assessment) => setDetail({ status: 'loaded', assessment }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load assessment'
        setDetail({ status: 'error', message })
      })

    return () => controller.abort()
  }, [open, clientId, summary.id, refreshKey])

  function toggle() {
    if (!open && detail.status !== 'loaded') setDetail({ status: 'loading' })
    setOpen((value) => !value)
  }

  return (
    <li>
      <button type="button" className="history-toggle" aria-expanded={open} onClick={toggle}>
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
      </button>
      <ReviewStatusLine assessment={summary} />
      {open && (
        <div className="history-detail">
          {detail.status === 'loading' && <p>Loading assessment…</p>}
          {detail.status === 'error' && <p role="alert">{detail.message}</p>}
          {detail.status === 'loaded' && <AssessmentView assessment={detail.assessment} />}
        </div>
      )}
    </li>
  )
}
