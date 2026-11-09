import { Link, useNavigate } from 'react-router'
import type { Communication } from '../../api/communications.ts'
import { AddCommunicationForm } from '../AddCommunicationForm.tsx'
import { useClientContext } from './clientContext.ts'

export function AddCommunicationPage() {
  const { client, addCommunication } = useClientContext()
  const navigate = useNavigate()

  function handleCreated(communication: Communication) {
    addCommunication(communication)
    navigate(`/clients/${client.id}/communications#communication-${communication.id}`)
  }

  return (
    <section aria-labelledby="add-communication-heading">
      <div className="section-heading">
        <h2 id="add-communication-heading">Add communication</h2>
        <Link to=".." relative="path">
          Cancel
        </Link>
      </div>
      <AddCommunicationForm clientId={client.id} onCreated={handleCreated} />
    </section>
  )
}
