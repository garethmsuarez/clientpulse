import { describe, expect, it } from 'vitest'
import type { AssessmentSummary, AttentionLevel, ReviewStatus } from '../api/assessments.ts'
import type { ClientSummary } from '../api/clients.ts'
import { defaultSort, effectiveSeverity, parseSort, sortClients } from './clientSort.ts'

function assessment(
  level: AttentionLevel,
  review: ReviewStatus = 'Unreviewed',
  approved: AttentionLevel | null = null,
  createdAt = '2026-11-01T00:00:00Z',
): AssessmentSummary {
  return {
    id: 1,
    createdAt,
    source: 'Fixture',
    isFixture: true,
    fixtureName: 'synthetic',
    responseModel: null,
    attentionLevel: level,
    isCurrent: true,
    isLatest: true,
    reviewStatus: review,
    reviewerApprovedAttentionLevel: approved,
    latestReview: null,
  }
}

function client(name: string, latest: AssessmentSummary | null): ClientSummary {
  return { id: name.length, name, industry: 'Synthetic', latestAssessment: latest }
}

const names = (clients: ClientSummary[]) => clients.map((c) => c.name)

describe('effectiveSeverity', () => {
  it('uses the analyzer level until reviewed, then the reviewer-approved level', () => {
    expect(effectiveSeverity(assessment('High'))).toBe('High')
    expect(effectiveSeverity(assessment('High', 'Accepted', 'High'))).toBe('High')
    expect(effectiveSeverity(assessment('High', 'Adjusted', 'Low'))).toBe('Low')
  })

  it('has no severity for a rejected assessment (rejected is not low)', () => {
    expect(effectiveSeverity(assessment('High', 'Rejected', null))).toBeNull()
  })
})

describe('sortClients', () => {
  const clients = [
    client('Alpha', null),
    client('Bravo', assessment('Low')),
    client('Charlie', assessment('High', 'Adjusted', 'Medium')),
    client('Delta', assessment('Medium', 'Rejected', null)),
    client('Echo', assessment('High')),
  ]

  it('sorts by reviewed severity, then rejected, then never assessed', () => {
    expect(names(sortClients(clients, { key: 'severity', direction: 'desc' }))).toEqual([
      'Echo',
      'Charlie',
      'Bravo',
      'Delta',
      'Alpha',
    ])
  })

  it('keeps rejected and never-assessed clients last when ascending', () => {
    expect(names(sortClients(clients, { key: 'severity', direction: 'asc' }))).toEqual([
      'Bravo',
      'Charlie',
      'Echo',
      'Delta',
      'Alpha',
    ])
  })

  it('breaks severity ties by most recent assessment, then name', () => {
    const tied = [
      client('Older', assessment('High', 'Unreviewed', null, '2026-10-01T00:00:00Z')),
      client('Newer', assessment('High', 'Unreviewed', null, '2026-11-01T00:00:00Z')),
      client('Same B', assessment('Low', 'Unreviewed', null, '2026-09-01T00:00:00Z')),
      client('Same A', assessment('Low', 'Unreviewed', null, '2026-09-01T00:00:00Z')),
    ]

    expect(names(sortClients(tied, { key: 'severity', direction: 'desc' }))).toEqual([
      'Newer',
      'Older',
      'Same A',
      'Same B',
    ])
  })

  it('sorts by assessed date with never-assessed clients last in both directions', () => {
    const dated = [
      client('None', null),
      client('Early', assessment('Low', 'Unreviewed', null, '2026-09-01T00:00:00Z')),
      client('Late', assessment('Low', 'Unreviewed', null, '2026-11-01T00:00:00Z')),
    ]

    expect(names(sortClients(dated, { key: 'assessed', direction: 'desc' }))).toEqual(['Late', 'Early', 'None'])
    expect(names(sortClients(dated, { key: 'assessed', direction: 'asc' }))).toEqual(['Early', 'Late', 'None'])
  })

  it('sorts by name in either direction and does not modify the input', () => {
    const input = [...clients]

    expect(names(sortClients(clients, { key: 'name', direction: 'desc' }))).toEqual([
      'Echo',
      'Delta',
      'Charlie',
      'Bravo',
      'Alpha',
    ])
    expect(clients).toEqual(input)
  })
})

describe('parseSort', () => {
  it('reads a valid sort and falls back to the default otherwise', () => {
    expect(parseSort(new URLSearchParams('sort=assessed&dir=asc'))).toEqual({ key: 'assessed', direction: 'asc' })
    expect(parseSort(new URLSearchParams(''))).toEqual(defaultSort)
    expect(parseSort(new URLSearchParams('sort=bogus&dir=asc'))).toEqual(defaultSort)
    expect(parseSort(new URLSearchParams('sort=name&dir=sideways'))).toEqual(defaultSort)
  })
})
