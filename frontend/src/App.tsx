import { Link, Route, Routes } from 'react-router'
import { ClientList } from './components/ClientList.tsx'
import { AddCommunicationPage } from './components/client/AddCommunicationPage.tsx'
import { AssessmentDetailPage } from './components/client/AssessmentDetailPage.tsx'
import { AssessmentTab } from './components/client/AssessmentTab.tsx'
import { ClientLayout, ClientSectionNotFound } from './components/client/ClientLayout.tsx'
import { CommunicationsTab } from './components/client/CommunicationsTab.tsx'
import { HistoryTab } from './components/client/HistoryTab.tsx'
import { OverviewTab } from './components/client/OverviewTab.tsx'
import './App.css'

function NotFound() {
  return (
    <main className="clients">
      <h1>Page not found</h1>
      <nav className="back-nav" aria-label="Breadcrumb">
        <Link to="/" className="back-link">
          <span aria-hidden="true">←</span> Back to clients
        </Link>
      </nav>
    </main>
  )
}

// Routes only; the browser router is provided by main.tsx (or a memory router in tests).
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<ClientList />} />
      <Route path="/clients/:clientId" element={<ClientLayout />}>
        <Route index element={<OverviewTab />} />
        <Route path="assessment" element={<AssessmentTab />} />
        <Route path="communications" element={<CommunicationsTab />} />
        <Route path="communications/new" element={<AddCommunicationPage />} />
        <Route path="history" element={<HistoryTab />} />
        <Route path="history/:assessmentId" element={<AssessmentDetailPage />} />
        <Route path="*" element={<ClientSectionNotFound />} />
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}

function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/" className="app-wordmark">
          ClientPulse
        </Link>
        <span className="app-tagline">Client communication review</span>
      </header>
      <div className="app-content">
        <AppRoutes />
      </div>
    </div>
  )
}

export default App
