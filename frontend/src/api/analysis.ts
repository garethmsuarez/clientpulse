import { toApiError } from './apiError.ts'

export type ActiveAnalysisProvider = {
  provider: 'Fixture' | 'Anthropic'
  model: string | null
  effort: string | null
}

export async function fetchAnalysisProvider(signal?: AbortSignal): Promise<ActiveAnalysisProvider> {
  const response = await fetch('/api/analysis/provider', { signal })

  if (!response.ok) {
    throw await toApiError(response, 'Failed to load analysis provider')
  }

  return (await response.json()) as ActiveAnalysisProvider
}
