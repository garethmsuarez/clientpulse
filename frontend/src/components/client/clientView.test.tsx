import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import App from '../../App.tsx'
import type { Communication } from '../../api/communications.ts'
import { installFakeApi, json, problem } from '../../test/fakeApi.ts'

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

function tab(name: RegExp) {
  return within(screen.getByRole('navigation', { name: 'Client sections' })).getByRole('link', { name })
}

describe('client view', () => {
  it('opens on the Overview tab with a summary and no analysis', async () => {
    const fetchMock = installFakeApi()

    renderAt('/clients/1')

    expect(await screen.findByRole('heading', { level: 1, name: 'Synthetic Logistics' })).toBeInTheDocument()
    expect(tab(/^Overview$/)).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByText('Synthetic latest summary.')).toBeInTheDocument()
    expect(screen.getByText(/3 recorded/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Analyze communications' })).not.toBeInTheDocument()
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('shows an empty overview when there is no assessment yet', async () => {
    installFakeApi({ 'GET /api/clients/1/assessments/latest': () => new Response(null, { status: 204 }) })

    renderAt('/clients/1')

    expect(await screen.findByText(/No assessment yet/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Analyze communications' })).toHaveAttribute(
      'href',
      '/clients/1/assessment',
    )
  })

  it('switches tabs through the tab bar', async () => {
    installFakeApi()
    const user = userEvent.setup()
    renderAt('/clients/1')
    await screen.findByText('Synthetic latest summary.')

    await user.click(tab(/^Communications \(3\)$/))

    expect(await screen.findByRole('heading', { level: 2, name: 'Communications' })).toBeInTheDocument()
    expect(screen.getByText('Late deliveries')).toBeInTheDocument()
    expect(tab(/^Communications/)).toHaveAttribute('aria-current', 'page')

    await user.click(tab(/^History$/))

    expect(await screen.findByRole('heading', { level: 2, name: 'Assessment history' })).toBeInTheDocument()
  })

  it('keeps the review form closed until it is asked for', async () => {
    installFakeApi()
    const user = userEvent.setup()
    renderAt('/clients/1/assessment')

    const toggle = await screen.findByRole('button', { name: 'Review this assessment' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByRole('group', { name: 'Decision' })).not.toBeInTheDocument()

    await user.click(toggle)

    expect(screen.getByRole('button', { name: 'Cancel review' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('group', { name: 'Decision' })).toBeInTheDocument()
  })

  it('opens evidence on the Communications tab with the cited communication highlighted', async () => {
    installFakeApi()
    const user = userEvent.setup()
    renderAt('/clients/1/assessment')

    await user.click(await screen.findByRole('link', { name: /Late deliveries/ }))

    const cited = (await screen.findByRole('heading', { level: 3, name: 'Late deliveries' })).closest('li')
    expect(cited).toHaveClass('is-highlighted')
    expect(cited).toHaveAttribute('aria-current', 'true')
    const other = screen.getByRole('heading', { level: 3, name: 'Volumes' }).closest('li')
    expect(other).not.toHaveClass('is-highlighted')
  })

  it('adds a communication on its own page and returns to the list with it highlighted', async () => {
    const saved: Communication = {
      id: 1001,
      clientId: 1,
      type: 'Meeting',
      occurredAt: '2026-11-05T15:00:00Z',
      subject: 'Synthetic check-in',
      body: 'Synthetic meeting notes.',
      createdAt: '2026-11-05T15:00:00Z',
    }
    const fetchMock = installFakeApi({ 'POST /api/clients/1/communications': () => json(saved, 201) })
    const user = userEvent.setup()
    renderAt('/clients/1/communications/new')

    await user.selectOptions(await screen.findByLabelText('Type'), 'Meeting')
    await user.type(screen.getByLabelText('Subject'), 'Synthetic check-in')
    await user.type(screen.getByLabelText('Body'), 'Synthetic meeting notes.')
    await user.click(screen.getByRole('button', { name: 'Add communication' }))

    const added = (await screen.findByRole('heading', { level: 3, name: 'Synthetic check-in' })).closest('li')
    expect(added).toHaveClass('is-highlighted')
    expect(tab(/^Communications \(4\)$/)).toBeInTheDocument()

    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
    expect(JSON.parse(String(post?.[1]?.body))).toMatchObject({
      type: 'Meeting',
      subject: 'Synthetic check-in',
      body: 'Synthetic meeting notes.',
    })
  })

  it('lists history and opens a past assessment on its own read-only page', async () => {
    installFakeApi()
    const user = userEvent.setup()
    renderAt('/clients/1/history')

    const links = await screen.findAllByRole('link', { name: /Analyzer:/ })
    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/clients/1/history/12',
      '/clients/1/history/11',
    ])

    await user.click(links[1])

    expect(await screen.findByText('Synthetic older summary.')).toBeInTheDocument()
    expect(screen.getByText(/A newer assessment exists, so this one is read-only/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Review this assessment' })).not.toBeInTheDocument()
  })

  it('shows not found for an unknown client, a bad id, and an unknown section', async () => {
    installFakeApi({ 'GET /api/clients/999': () => problem(404, 'Client not found') })

    const unknown = renderAt('/clients/999')
    expect(await screen.findByRole('heading', { name: 'Client not found' })).toBeInTheDocument()
    unknown.unmount()

    const fetchMock = installFakeApi()
    const bad = renderAt('/clients/abc')
    expect(screen.getByRole('heading', { name: 'Client not found' })).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
    bad.unmount()

    renderAt('/clients/1/nope')
    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
