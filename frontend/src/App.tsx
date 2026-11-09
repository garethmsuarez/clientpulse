import { Link, Route, Routes } from 'react-router'
import { ClientDetail } from './components/ClientDetail.tsx'
import { ClientList } from './components/ClientList.tsx'
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
        <Routes>
          <Route path="/" element={<ClientList />} />
          <Route path="/clients/:clientId" element={<ClientDetail />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </div>
    </div>
  )
}

export default App
