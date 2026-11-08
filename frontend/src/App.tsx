import { Link, Route, Routes } from 'react-router'
import { ClientDetail } from './components/ClientDetail.tsx'
import { ClientList } from './components/ClientList.tsx'
import './App.css'

function NotFound() {
  return (
    <main className="clients">
      <h1>Page not found</h1>
      <p>
        <Link to="/">Back to clients</Link>
      </p>
    </main>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<ClientList />} />
      <Route path="/clients/:clientId" element={<ClientDetail />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}

export default App
