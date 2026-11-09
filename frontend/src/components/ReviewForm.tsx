import { useState, type FormEvent } from 'react'
import { ApiError, type FieldErrors } from '../api/apiError.ts'
import {
  attentionLevels,
  createReview,
  reviewDecisions,
  reviewerNameMaxLength,
  reviewNotesMaxLength,
  type Assessment,
  type AttentionLevel,
  type Review,
  type ReviewDecision,
} from '../api/assessments.ts'

type FormValues = {
  decision: ReviewDecision | ''
  reviewedAttentionLevel: AttentionLevel | ''
  notes: string
  reviewerName: string
}

type ReviewFormProps = {
  clientId: number
  assessment: Assessment
  onCreated: (review: Review) => void
}

const emptyForm: FormValues = { decision: '', reviewedAttentionLevel: '', notes: '', reviewerName: '' }

const decisionHelp: Record<ReviewDecision, string> = {
  Accepted: 'The assessment is right as recorded. Notes are optional.',
  Adjusted: 'Replace the attention level. Notes are required.',
  Rejected: 'The assessment should not be relied on. Notes are required.',
}

function validate(values: FormValues, original: AttentionLevel): FieldErrors {
  const errors: FieldErrors = {}

  if (!values.decision) errors.decision = ['Choose a decision.']

  if (values.decision === 'Adjusted') {
    if (!values.reviewedAttentionLevel) {
      errors.reviewedAttentionLevel = ['Choose a replacement attention level.']
    } else if (values.reviewedAttentionLevel === original) {
      errors.reviewedAttentionLevel = ['The replacement level must differ from the original level.']
    }
  }

  const notes = values.notes.trim()
  if (!notes && (values.decision === 'Adjusted' || values.decision === 'Rejected')) {
    errors.notes = [`Notes are required for ${values.decision} reviews.`]
  } else if (notes.length > reviewNotesMaxLength) {
    errors.notes = [`Notes must be ${reviewNotesMaxLength.toLocaleString()} characters or fewer.`]
  }

  const reviewerName = values.reviewerName.trim()
  if (!reviewerName) errors.reviewerName = ['Enter your name.']
  else if (reviewerName.length > reviewerNameMaxLength)
    errors.reviewerName = [`Name must be ${reviewerNameMaxLength} characters or fewer.`]

  return errors
}

export function ReviewForm({ clientId, assessment, onCreated }: ReviewFormProps) {
  const [values, setValues] = useState<FormValues>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  function update<K extends keyof FormValues>(field: K, value: FormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError(null)

    const errors = validate(values, assessment.attentionLevel)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0 || !values.decision) return

    setSaving(true)
    try {
      const review = await createReview(clientId, assessment.id, {
        decision: values.decision,
        reviewedAttentionLevel:
          values.decision === 'Adjusted' && values.reviewedAttentionLevel
            ? values.reviewedAttentionLevel
            : null,
        notes: values.notes.trim() || null,
        reviewerName: values.reviewerName.trim(),
      })
      setValues(emptyForm)
      onCreated(review)
    } catch (error: unknown) {
      if (error instanceof ApiError && error.status === 400) {
        setFieldErrors(error.fieldErrors)
        setFormError('Please correct the highlighted fields.')
      } else if (error instanceof ApiError && error.code === 'not-latest-assessment') {
        setFormError('A newer assessment exists. Only the latest assessment can be reviewed; reload the page.')
      } else {
        setFormError(error instanceof Error ? error.message : 'Failed to save review.')
      }
    } finally {
      setSaving(false)
    }
  }

  function fieldError(field: keyof FormValues) {
    const messages = fieldErrors[field]
    if (!messages?.length) return null
    return (
      <span id={`review-${field}-error`} className="field-error">
        {messages.join(' ')}
      </span>
    )
  }

  function describedBy(field: keyof FormValues) {
    return fieldErrors[field]?.length ? `review-${field}-error` : undefined
  }

  const replacementLevels = attentionLevels.filter((level) => level !== assessment.attentionLevel)

  return (
    <form className="communication-form review-form" onSubmit={handleSubmit} noValidate>
      <fieldset aria-describedby={describedBy('decision')}>
        <legend>Decision</legend>
        {reviewDecisions.map((decision) => (
          <label key={decision} className="radio">
            <input
              type="radio"
              name="decision"
              value={decision}
              checked={values.decision === decision}
              onChange={() => update('decision', decision)}
            />
            <span>
              {decision} <span className="hint">— {decisionHelp[decision]}</span>
            </span>
          </label>
        ))}
        {fieldError('decision')}
      </fieldset>

      {values.decision === 'Adjusted' && (
        <label>
          Replacement attention level (analyzer said {assessment.attentionLevel})
          <select
            value={values.reviewedAttentionLevel}
            onChange={(event) => update('reviewedAttentionLevel', event.target.value as AttentionLevel)}
            aria-invalid={Boolean(fieldErrors.reviewedAttentionLevel)}
            aria-describedby={describedBy('reviewedAttentionLevel')}
          >
            <option value="">Choose a level</option>
            {replacementLevels.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
          {fieldError('reviewedAttentionLevel')}
        </label>
      )}

      <label>
        Notes{values.decision === 'Accepted' || !values.decision ? ' (optional for Accepted)' : ''}
        <textarea
          rows={3}
          value={values.notes}
          maxLength={reviewNotesMaxLength}
          onChange={(event) => update('notes', event.target.value)}
          aria-invalid={Boolean(fieldErrors.notes)}
          aria-describedby={describedBy('notes')}
        />
        {fieldError('notes')}
      </label>

      <label>
        Your name <span className="hint">(self-reported and not verified)</span>
        <input
          type="text"
          value={values.reviewerName}
          maxLength={reviewerNameMaxLength}
          autoComplete="off"
          onChange={(event) => update('reviewerName', event.target.value)}
          aria-invalid={Boolean(fieldErrors.reviewerName)}
          aria-describedby={describedBy('reviewerName')}
        />
        {fieldError('reviewerName')}
      </label>

      {formError && <p role="alert">{formError}</p>}

      <button type="submit" disabled={saving}>
        {saving ? 'Saving…' : 'Save review'}
      </button>
    </form>
  )
}
