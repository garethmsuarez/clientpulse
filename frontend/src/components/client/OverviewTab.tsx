import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { fetchLatestAssessment, type Assessment } from '../../api/assessments.ts'
import { dateTimeFormat } from '../../format.ts'
import { ReviewStatusLine } from '../AssessmentView.tsx'
import { useClientContext } from './clientContext.ts'

type LatestState =
  | { status: 'loading' }
  | { status: 'none' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessment: Assessment }

// Landing tab: a short summary of the latest assessment and of the
// communications, with links to the detailed tabs.
export function OverviewTab() {
  const { client, communications } = useClientContext()
  const [latest, setLatest] = useState<LatestState>({ status: 'loading' })
  const mostRecent = communications[0]

  useEffect(() => {
    const controller = new AbortController()

    fetchLatestAssessment(client.id, controller.signal)
      .then((assessment) => setLatest(assessment ? { status: 'loaded', assessment } : { status: 'none' }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load assessment'
        setLatest({ status: 'error', message })
      })

    return () => controller.abort()
  }, [client.id])

  return (
    <>
      <section aria-labelledby="overview-assessment-heading">
        <h2 id="overview-assessment-heading">Latest assessment</h2>
        {latest.status === 'loading' && <p>Loading assessment…</p>}
        {latest.status === 'error' && <p role="alert">{latest.message}</p>}
        {latest.status === 'none' && (
          <p>
            No assessment yet. <Link to="assessment">Analyze communications</Link>
          </p>
        )}
        {latest.status === 'loaded' && (
          <>
            <p className="assessment-meta">
              <span className={`attention attention-${latest.assessment.attentionLevel.toLowerCase()}`}>
                Analyzer attention: {latest.assessment.attentionLevel}
              </span>
              <span className="tag">{latest.assessment.isFixture ? 'Fixture' : 'AI'}</span>
              <span>
                Saved{' '}
                <time dateTime={latest.assessment.createdAt}>
                  {dateTimeFormat.format(new Date(latest.assessment.createdAt))}
                </time>
              </span>
            </p>
            {!latest.assessment.isCurrent && (
              <p className="assessment-stale" role="note">
                Communications have changed since this assessment was saved.
              </p>
            )}
            <ReviewStatusLine assessment={latest.assessment} />
            <p className="overview-summary">{latest.assessment.summary}</p>
            <p className="overview-links">
              <Link to="assessment">Open assessment</Link>
              <Link to="history">View history</Link>
            </p>
          </>
        )}
      </section>

      <section aria-labelledby="overview-communications-heading">
        <h2 id="overview-communications-heading">Communications</h2>
        {mostRecent ? (
          <p>
            {communications.length} recorded. Most recent: <strong>{mostRecent.subject}</strong> ({mostRecent.type},{' '}
            <time dateTime={mostRecent.occurredAt}>{dateTimeFormat.format(new Date(mostRecent.occurredAt))}</time>)
          </p>
        ) : (
          <p>No communications recorded yet.</p>
        )}
        <p className="overview-links">
          <Link to="communications">View all</Link>
          <Link to="communications/new">Add communication</Link>
        </p>
      </section>
    </>
  )
}
