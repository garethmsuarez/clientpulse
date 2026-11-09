import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '../App.tsx'
import type { AssessmentSummary, AttentionLevel, ReviewStatus } from '../api/assessments.ts'
import type { ClientSummary } from '../api/clients.ts'
import { installFakeApi, json } from '../test/fakeApi.ts'

function latest(
  level: AttentionLevel,
  createdAt: string,
  review: ReviewStatus = 'Unreviewed',
  approved: AttentionLevel | null = null,
  extra: Partial<AssessmentSummary> = {},
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
    ...extra,
  }
}

const clients: ClientSummary[] = [
  { id: 1, name: 'Synthetic Alpha', industry: 'Retail', latestAssessment: null },
  { id: 2, name: 'Synthetic Bravo', industry: 'Health', latestAssessment: latest('Low', '2026-11-03T00:00:00Z') },
  {
    id: 3,
    name: 'Synthetic Charlie',
    industry: 'Logistics',
    latestAssessment: latest('High', '2026-11-01T00:00:00Z', 'Adjusted', 'Medium', { isCurrent: false }),
  },
  {
    id: 4,
    name: 'Synthetic Delta',
    industry: 'Finance',
    latestAssessment: latest('High', '2026-11-02T00:00:00Z', 'Unreviewed', null, {
      isFixture: false,
      source: 'Anthropic',
    }),
  },
]

function CurrentSearch() {
  return <output data-testid="search">{useLocation().search}</output>
}

function renderList(path = '/') {
  installFakeApi({ 'GET /api/clients': () => json(clients) })
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AppRoutes />
      <CurrentSearch />
    </MemoryRouter>,
  )
}

async function rowNames() {
  const rows = await screen.findAllByRole('row')
  return rows.slice(1).map((row) => within(row).getAllByRole('cell')[0].textContent)
}

describe('client list', () => {
  it('shows assessment columns and defaults to highest severity first', async () => {
    renderList()

    expect(await rowNames()).toEqual(['Synthetic Delta', 'Synthetic Charlie', 'Synthetic Bravo', 'Synthetic Alpha'])
    expect(screen.getByRole('columnheader', { name: /Severity/ })).toHaveAttribute('aria-sort', 'descending')

    const charlie = screen.getByRole('row', { name: /Synthetic Charlie/ })
    expect(within(charlie).getByText('Medium')).toBeInTheDocument()
    expect(within(charlie).getByText('Adjusted from High')).toBeInTheDocument()
    expect(within(charlie).getByText('Out of date')).toBeInTheDocument()

    const delta = screen.getByRole('row', { name: /Synthetic Delta/ })
    expect(within(delta).getByText('AI')).toBeInTheDocument()
    expect(within(delta).getByText('Unreviewed')).toBeInTheDocument()

    expect(within(screen.getByRole('row', { name: /Synthetic Alpha/ })).getByText('No assessment')).toBeInTheDocument()
  })

  it('re-sorts from the headers and records the sort in the URL', async () => {
    const user = userEvent.setup()
    renderList()
    await rowNames()

    await user.click(screen.getByRole('button', { name: /Assessed/ }))

    expect(await rowNames()).toEqual(['Synthetic Bravo', 'Synthetic Delta', 'Synthetic Charlie', 'Synthetic Alpha'])
    expect(screen.getByRole('columnheader', { name: /Assessed/ })).toHaveAttribute('aria-sort', 'descending')
    expect(screen.getByRole('columnheader', { name: /Severity/ })).toHaveAttribute('aria-sort', 'none')
    expect(screen.getByTestId('search')).toHaveTextContent('?sort=assessed&dir=desc')

    await user.click(screen.getByRole('button', { name: /Assessed/ }))

    expect(await rowNames()).toEqual(['Synthetic Charlie', 'Synthetic Delta', 'Synthetic Bravo', 'Synthetic Alpha'])
    expect(screen.getByTestId('search')).toHaveTextContent('?sort=assessed&dir=asc')
  })

  it('restores the sort from the URL', async () => {
    renderList('/?sort=name&dir=desc')

    expect(await rowNames()).toEqual(['Synthetic Delta', 'Synthetic Charlie', 'Synthetic Bravo', 'Synthetic Alpha'])
    expect(screen.getByRole('columnheader', { name: /Client/ })).toHaveAttribute('aria-sort', 'descending')
  })
})
