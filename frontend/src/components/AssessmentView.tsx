import type { Assessment, Review, Signal } from '../api/assessments.ts'
import { dateTimeFormat } from '../format.ts'

// Read-only display of one assessment: the original analyzer output, the
// current review status, and the review history.
export function AssessmentView({ assessment }: { assessment: Assessment }) {
  const count = assessment.analyzedCommunicationIds.length

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
          Analyzer attention: {assessment.attentionLevel}
        </span>
        <span>
          Saved{' '}
          <time dateTime={assessment.createdAt}>
            {dateTimeFormat.format(new Date(assessment.createdAt))}
          </time>{' '}
          from {count} communication{count === 1 ? '' : 's'}
        </span>
      </p>

      <ReviewStatusLine assessment={assessment} />

      <p>{assessment.summary}</p>

      <SignalList title="Concerns" signals={assessment.concerns} empty="No concerns identified." />
      <SignalList
        title="Positive signals"
        signals={assessment.positiveSignals}
        empty="No positive signals identified."
      />

      <h3>Review history</h3>
      {assessment.reviews.length === 0 ? (
        <p>No reviews yet.</p>
      ) : (
        <ul className="reviews">
          {assessment.reviews.map((review) => (
            <ReviewItem key={review.id} review={review} />
          ))}
        </ul>
      )}
    </article>
  )
}

export function ReviewStatusLine({
  assessment,
}: {
  assessment: Pick<Assessment, 'reviewStatus' | 'reviewerApprovedAttentionLevel'>
}) {
  const { reviewStatus, reviewerApprovedAttentionLevel } = assessment

  let text: string
  switch (reviewStatus) {
    case 'Unreviewed':
      text = 'Not yet reviewed.'
      break
    case 'Accepted':
      text = `Accepted by reviewer. Reviewer-approved attention: ${reviewerApprovedAttentionLevel}.`
      break
    case 'Adjusted':
      text = `Adjusted by reviewer. Reviewer-approved attention: ${reviewerApprovedAttentionLevel}.`
      break
    case 'Rejected':
      text = 'Rejected by reviewer. No reviewer-approved attention level.'
      break
  }

  return (
    <p className={`review-status review-status-${reviewStatus.toLowerCase()}`}>
      <strong>Review status:</strong> {text}
    </p>
  )
}

function ReviewItem({ review }: { review: Review }) {
  return (
    <li>
      <div className="review-meta">
        <strong>{review.decision}</strong>
        {review.reviewedAttentionLevel && <span>→ {review.reviewedAttentionLevel}</span>}
        <span>
          by {review.reviewerName} <span className="unverified">(self-reported, unverified)</span>
        </span>
        <time dateTime={review.createdAt}>{dateTimeFormat.format(new Date(review.createdAt))}</time>
      </div>
      {review.notes && <p className="review-notes">{review.notes}</p>}
    </li>
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
