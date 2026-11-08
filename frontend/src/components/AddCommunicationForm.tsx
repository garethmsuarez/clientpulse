import { useState, type FormEvent } from 'react'
import { ApiError, type FieldErrors } from '../api/apiError.ts'
import {
  bodyMaxLength,
  communicationTypes,
  createCommunication,
  subjectMaxLength,
  type Communication,
  type CommunicationType,
} from '../api/communications.ts'

type FormValues = {
  type: CommunicationType
  occurredAt: string
  subject: string
  body: string
}

type AddCommunicationFormProps = {
  clientId: number
  onCreated: (communication: Communication) => void
}

// Value for <input type="datetime-local">, in the browser's local time.
function toLocalInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

function emptyForm(): FormValues {
  return { type: 'Email', occurredAt: toLocalInputValue(new Date()), subject: '', body: '' }
}

function validate(values: FormValues): FieldErrors {
  const errors: FieldErrors = {}
  const occurredAt = new Date(values.occurredAt)

  if (!values.occurredAt) errors.occurredAt = ['Occurred at is required.']
  else if (Number.isNaN(occurredAt.getTime())) errors.occurredAt = ['Enter a valid date and time.']
  else if (occurredAt.getTime() > Date.now() + 5 * 60_000)
    errors.occurredAt = ['Occurred at cannot be in the future.']

  const subject = values.subject.trim()
  if (!subject) errors.subject = ['Subject is required.']
  else if (subject.length > subjectMaxLength)
    errors.subject = [`Subject must be ${subjectMaxLength} characters or fewer.`]

  const body = values.body.trim()
  if (!body) errors.body = ['Body is required.']
  else if (body.length > bodyMaxLength)
    errors.body = [`Body must be ${bodyMaxLength.toLocaleString()} characters or fewer.`]

  return errors
}

export function AddCommunicationForm({ clientId, onCreated }: AddCommunicationFormProps) {
  const [values, setValues] = useState<FormValues>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  function update<K extends keyof FormValues>(field: K, value: FormValues[K]) {
    setValues((current) => ({ ...current, [field]: value }))
    setSaved(false)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError(null)
    setSaved(false)

    const errors = validate(values)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setSaving(true)
    try {
      const created = await createCommunication(clientId, {
        type: values.type,
        occurredAt: new Date(values.occurredAt).toISOString(),
        subject: values.subject.trim(),
        body: values.body.trim(),
      })
      onCreated(created)
      setValues(emptyForm())
      setSaved(true)
    } catch (error: unknown) {
      if (error instanceof ApiError && error.status === 400) {
        setFieldErrors(error.fieldErrors)
        setFormError('Please correct the highlighted fields.')
      } else if (error instanceof ApiError && error.status === 404) {
        setFormError('This client no longer exists.')
      } else {
        setFormError(error instanceof Error ? error.message : 'Failed to save communication.')
      }
    } finally {
      setSaving(false)
    }
  }

  function fieldError(field: keyof FormValues) {
    const messages = fieldErrors[field]
    if (!messages?.length) return null
    return (
      <span id={`${field}-error`} className="field-error">
        {messages.join(' ')}
      </span>
    )
  }

  function describedBy(field: keyof FormValues) {
    return fieldErrors[field]?.length ? `${field}-error` : undefined
  }

  return (
    <form className="communication-form" onSubmit={handleSubmit} noValidate>
      <label>
        Type
        <select
          value={values.type}
          onChange={(event) => update('type', event.target.value as CommunicationType)}
          aria-invalid={Boolean(fieldErrors.type)}
          aria-describedby={describedBy('type')}
        >
          {communicationTypes.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        {fieldError('type')}
      </label>

      <label>
        Occurred at
        <input
          type="datetime-local"
          value={values.occurredAt}
          onChange={(event) => update('occurredAt', event.target.value)}
          aria-invalid={Boolean(fieldErrors.occurredAt)}
          aria-describedby={describedBy('occurredAt')}
        />
        {fieldError('occurredAt')}
      </label>

      <label>
        Subject
        <input
          type="text"
          value={values.subject}
          maxLength={subjectMaxLength}
          onChange={(event) => update('subject', event.target.value)}
          aria-invalid={Boolean(fieldErrors.subject)}
          aria-describedby={describedBy('subject')}
        />
        {fieldError('subject')}
      </label>

      <label>
        Body
        <textarea
          rows={5}
          value={values.body}
          maxLength={bodyMaxLength}
          onChange={(event) => update('body', event.target.value)}
          aria-invalid={Boolean(fieldErrors.body)}
          aria-describedby={describedBy('body')}
        />
        {fieldError('body')}
      </label>

      {formError && <p role="alert">{formError}</p>}
      {saved && <p role="status">Communication saved.</p>}

      <button type="submit" disabled={saving}>
        {saving ? 'Saving…' : 'Add communication'}
      </button>
    </form>
  )
}
