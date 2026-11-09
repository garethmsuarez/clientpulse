import type { ClientSummary } from '../api/clients.ts'
import type { AssessmentSummary, AttentionLevel } from '../api/assessments.ts'

export type SortKey = 'name' | 'severity' | 'assessed'
export type SortDirection = 'asc' | 'desc'
export type ClientSort = { key: SortKey; direction: SortDirection }

export const defaultSort: ClientSort = { key: 'severity', direction: 'desc' }

// Direction used when a column is first selected.
export const initialDirection: Record<SortKey, SortDirection> = {
  name: 'asc',
  severity: 'desc',
  assessed: 'desc',
}

const levelRank: Record<AttentionLevel, number> = { Low: 1, Medium: 2, High: 3 }

// The severity a client sorts by: the reviewer-approved level when the latest
// assessment was accepted or adjusted, the analyzer's level when it is still
// unreviewed, and none when it was rejected (rejected does not mean low).
export function effectiveSeverity(assessment: AssessmentSummary): AttentionLevel | null {
  if (assessment.reviewStatus === 'Unreviewed') return assessment.attentionLevel
  return assessment.reviewerApprovedAttentionLevel
}

// Buckets that keep their place whichever direction is chosen: clients with
// a severity first, then rejected assessments, then clients never assessed.
function bucket(client: ClientSummary, key: SortKey): number {
  if (key === 'name') return 0
  if (!client.latestAssessment) return 2
  if (key === 'severity' && effectiveSeverity(client.latestAssessment) === null) return 1
  return 0
}

function byName(a: ClientSummary, b: ClientSummary): number {
  return a.name.localeCompare(b.name)
}

function assessedAt(client: ClientSummary): number {
  return client.latestAssessment ? Date.parse(client.latestAssessment.createdAt) : 0
}

export function sortClients(clients: ClientSummary[], sort: ClientSort): ClientSummary[] {
  const sign = sort.direction === 'asc' ? 1 : -1

  return [...clients].sort((a, b) => {
    const bucketDifference = bucket(a, sort.key) - bucket(b, sort.key)
    if (bucketDifference !== 0) return bucketDifference

    let primary = 0
    if (sort.key === 'name') {
      primary = byName(a, b)
    } else if (sort.key === 'severity') {
      const severityA = a.latestAssessment && effectiveSeverity(a.latestAssessment)
      const severityB = b.latestAssessment && effectiveSeverity(b.latestAssessment)
      primary = (severityA ? levelRank[severityA] : 0) - (severityB ? levelRank[severityB] : 0)
    } else {
      primary = assessedAt(a) - assessedAt(b)
    }
    if (primary !== 0) return sign * primary

    // Ties: most recently assessed first, then by name.
    return assessedAt(b) - assessedAt(a) || byName(a, b)
  })
}

export function parseSort(params: URLSearchParams): ClientSort {
  const key = params.get('sort')
  const direction = params.get('dir')
  if ((key === 'name' || key === 'severity' || key === 'assessed') && (direction === 'asc' || direction === 'desc')) {
    return { key, direction }
  }
  return defaultSort
}
