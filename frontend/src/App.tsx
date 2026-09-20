import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import './App.css'
import { ExampleComponent } from './components/ExampleComponent'

interface HelloResponse {
  message: string
  timestamp: string
}

function App() {
  const [data, setData] = useState<HelloResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch('/api/hello')
      .then((res) => {
        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`)
        }
        return res.json()
      })
      .then((json: HelloResponse) => {
        setData(json)
        setLoading(false)
      })
      .catch((err: Error) => {
        setError(err.message)
        setLoading(false)
      })
  }, [])

  return (
    <div className="container">
      <h1>Hello World!</h1>
      <p>Frontend: React + Vite</p>

      <div className="backend-card">
        <h2>Backend Status (.NET 10)</h2>
        {loading && <p>Connecting to .NET backend...</p>}
        {error && <p className="error">Could not connect to backend: {error}</p>}
        {data && (
          <div className="success">
            <p><strong>Message:</strong> {data.message}</p>
            <p><strong>Server Time:</strong> {new Date(data.timestamp).toLocaleString()}</p>
          </div>
        )}
      </div>

      <ExampleComponent />

      <p><Link to="/login">Go to login page</Link></p>
    </div>
  )
}

export default App
