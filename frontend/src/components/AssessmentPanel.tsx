import { useEffect, useState } from 'react'
import { ApiError } from '../api/apiError.ts'
import {
  analyzeCommunications,
  fetchLatestAssessment,
  type Assessment,
  type Signal,
} from '../api/assessments.ts'
import { dateTimeFormat } from '../format.ts'

type LatestState =
  | { status: 'loading' }
  | { status: 'none' }
  | { status: 'error'; message: string }
  | { status: 'loaded'; assessment: Assessment }

type AssessmentPanelProps = {
  clientId: number
  hasCommunications: boolean
  refreshKey: number
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

export function AssessmentPanel({ clientId, hasCommunications, refreshKey }: AssessmentPanelProps) {
  const [latest, setLatest] = useState<LatestState>({ status: 'loading' })
  const [analyzing, setAnalyzing] = useState(false)
  const [analysisError, setAnalysisError] = useState<string | null>(null)

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
    try {
      const assessment = await analyzeCommunications(clientId)
      setLatest({ status: 'loaded', assessment })
    } catch (error: unknown) {
      // Any previously loaded assessment stays on screen unchanged.
      setAnalysisError(analysisErrorMessage(error))
    } finally {
      setAnalyzing(false)
    }
  }

  return (
    <section className="assessment" aria-labelledby="assessment-heading" aria-busy={analyzing}>
      <h2 id="assessment-heading">Assessment</h2>

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
      {latest.status === 'loaded' && <AssessmentView assessment={latest.assessment} />}
    </section>
  )
}

function AssessmentView({ assessment }: { assessment: Assessment }) {
  return (
    <article className="assessment-result">
      {assessment.isFixture && (
        <p className="fixture-label" role="note">
          <strong>{assessment.disclaimer ?? 'Fixture result.'}</strong>
          {assessment.fixtureName && <> Scenario: {assessment.fixtureName}</>}
        </p>
      )}

      {!assessment.isCurrent && (
        <p className="assessment-stale" role="note">
          Communications have changed since this assessment was saved.
        </p>
      )}

      <p className="assessment-meta">
        <span className={`attention attention-${assessment.attentionLevel.toLowerCase()}`}>
          Attention: {assessment.attentionLevel}
        </span>
        <span>
          Saved{' '}
          <time dateTime={assessment.createdAt}>
            {dateTimeFormat.format(new Date(assessment.createdAt))}
          </time>{' '}
          from {assessment.analyzedCommunicationIds.length} communication
          {assessment.analyzedCommunicationIds.length === 1 ? '' : 's'}
        </span>
      </p>

      <p>{assessment.summary}</p>

      <SignalList title="Concerns" signals={assessment.concerns} empty="No concerns identified." />
      <SignalList
        title="Positive signals"
        signals={assessment.positiveSignals}
        empty="No positive signals identified."
      />
    </article>
  )
}

function SignalList({ title, signals, empty }: { title: string; signals: Signal[]; empty: string }) {
  return (
    <>
      <h3>{title}</h3>
      {signals.length === 0 ? (
        <p>{empty}</p>
      ) : (
        <ul className="signals">
          {signals.map((signal, index) => (
            <li key={index}>
              {signal.description}
              <ul className="evidence" aria-label="Supporting communications">
                {signal.evidence.map((item) => (
                  <li key={item.communicationId}>
                    <a href={`#communication-${item.communicationId}`}>
                      {item.type} · {dateTimeFormat.format(new Date(item.occurredAt))} — {item.subject}
                    </a>
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
