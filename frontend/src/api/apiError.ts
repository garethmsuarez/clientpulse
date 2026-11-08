export type FieldErrors = Record<string, string[]>

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(message: string, status: number, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: FieldErrors
}

export async function toApiError(response: Response, fallback: string): Promise<ApiError> {
  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // Non-JSON error body; fall back to the generic message.
  }

  const message = problem.detail ?? problem.title ?? `${fallback} (HTTP ${response.status})`
  return new ApiError(message, response.status, problem.errors ?? {})
}
