import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/apiError.ts'
import { fetchAssessment, type Assessment } from '../../api/assessments.ts'
import { dateTimeFormat } from '../../format.ts'
import { AssessmentView } from '../AssessmentView.tsx'
import { useClientContext } from './clientContext.ts'

type DetailState =
  | { status: 'loading' }
  | { status: 'not-found' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessment: Assessment }

// One saved assessment, read-only. Reviews are added from the Assessment tab
// (latest assessment only).
export function AssessmentDetailPage() {
  const { client } = useClientContext()
  const { assessmentId } = useParams()
  const id = assessmentId && /^\d+$/.test(assessmentId) ? Number(assessmentId) : null

  return (
    <section aria-labelledby="assessment-detail-heading">
      <div className="section-heading">
        <h2 id="assessment-detail-heading">Saved assessment</h2>
        <Link to=".." relative="path">
          Back to history
        </Link>
      </div>
      {id === null ? (
        <p>Assessment not found.</p>
      ) : (
        <AssessmentDetailContent key={id} clientId={client.id} assessmentId={id} />
      )}
    </section>
  )
}

function AssessmentDetailContent({ clientId, assessmentId }: { clientId: number; assessmentId: number }) {
  const [state, setState] = useState<DetailState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetchAssessment(clientId, assessmentId, controller.signal)
      .then((assessment) => setState({ status: 'loaded', assessment }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        if (error instanceof ApiError && error.status === 404) {
          setState({ status: 'not-found' })
          return
        }
        const message = error instanceof Error ? error.message : 'Failed to load assessment'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [clientId, assessmentId])

  if (state.status === 'loading') return <p>Loading assessment…</p>
  if (state.status === 'not-found') return <p>Assessment not found.</p>
  if (state.status === 'error') return <p role="alert">{state.message}</p>

  const { assessment } = state

  return (
    <>
      <p className="provider-note">
        Assessment from {dateTimeFormat.format(new Date(assessment.createdAt))}.{' '}
        {assessment.isLatest ? (
          <>
            This is the latest assessment; <Link to="../../assessment" relative="path">review it on the Assessment tab</Link>.
          </>
        ) : (
          'A newer assessment exists, so this one is read-only.'
        )}
      </p>
      <AssessmentView assessment={assessment} />
    </>
  )
}
