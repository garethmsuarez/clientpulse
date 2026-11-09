import { useEffect, useState } from 'react'
import { ApiError } from '../api/apiError.ts'
import { analyzeCommunications, fetchLatestAssessment, type Assessment } from '../api/assessments.ts'
import { AssessmentView } from './AssessmentView.tsx'
import { ReviewForm } from './ReviewForm.tsx'

type LatestState =
  | { status: 'loading' }
  | { status: 'none' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessment: Assessment }

type AssessmentPanelProps = {
  clientId: number
  hasCommunications: boolean
  refreshKey: number
  // Called after a new assessment or review is saved, so history can reload.
  onChanged: () => void
}

function analysisErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case 'unsupported-input':
        return `Unsupported fixture input. ${error.message}`
      case 'no-communications':
        return error.message
      case 'invalid-analyzer-output':
        return `Analysis failed. ${error.message}`
    }
    return error.message
  }
  return error instanceof Error ? error.message : 'Analysis failed.'
}

export function AssessmentPanel({ clientId, hasCommunications, refreshKey, onChanged }: AssessmentPanelProps) {
  const [latest, setLatest] = useState<LatestState>({ status: 'loading' })
  const [analyzing, setAnalyzing] = useState(false)
  const [analysisError, setAnalysisError] = useState<string | null>(null)
  const [reviewSaved, setReviewSaved] = useState(false)

  // Loads the saved assessment only; analysis runs solely from the button.
  useEffect(() => {
    const controller = new AbortController()

    fetchLatestAssessment(clientId, controller.signal)
      .then((assessment) =>
        setLatest(assessment ? { status: 'loaded', assessment } : { status: 'none' }),
      )
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const message = error instanceof Error ? error.message : 'Failed to load assessment'
        setLatest({ status: 'error', message })
      })

    return () => controller.abort()
  }, [clientId, refreshKey])

  async function handleAnalyze() {
    setAnalyzing(true)
    setAnalysisError(null)
    setReviewSaved(false)
    try {
      const assessment = await analyzeCommunications(clientId)
      setLatest({ status: 'loaded', assessment })
      onChanged()
    } catch (error: unknown) {
      // Any previously loaded assessment stays on screen unchanged.
      setAnalysisError(analysisErrorMessage(error))
    } finally {
      setAnalyzing(false)
    }
  }

  function handleReviewCreated() {
    setReviewSaved(true)
    // Reloads the latest assessment (with its review status) and history.
    onChanged()
  }

  return (
    <section className="assessment" aria-labelledby="assessment-heading" aria-busy={analyzing}>
      <h2 id="assessment-heading">Latest assessment</h2>

      <div className="assessment-actions">
        <button type="button" onClick={handleAnalyze} disabled={analyzing || !hasCommunications}>
          {analyzing ? 'Analyzing…' : 'Analyze communications'}
        </button>
        {!hasCommunications && <span>Add a communication before analyzing.</span>}
      </div>

      {analysisError && (
        <p role="alert" className="assessment-error">
          {analysisError}
          {latest.status === 'loaded' && ' The previous assessment below is unchanged.'}
        </p>
      )}

      {latest.status === 'loading' && <p>Loading assessment…</p>}
      {latest.status === 'error' && <p role="alert">{latest.message}</p>}
      {latest.status === 'none' && <p>No assessment yet.</p>}
      {latest.status === 'loaded' && (
        <>
          <AssessmentView assessment={latest.assessment} />
          <h3>Add a review</h3>
          {!latest.assessment.isCurrent && (
            <p className="assessment-stale" role="note">
              You are reviewing an assessment of an earlier set of communications.
            </p>
          )}
          {reviewSaved && <p role="status">Review saved.</p>}
          <ReviewForm
            key={latest.assessment.id}
            clientId={clientId}
            assessment={latest.assessment}
            onCreated={handleReviewCreated}
          />
        </>
      )}
    </section>
  )
}
