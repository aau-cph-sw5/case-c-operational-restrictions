// PAGE:
// Route-level screen component rendered by React Router for a specific URL
import { useState } from 'react';
import { authService } from '../services/authService';
import type { LoginResponse } from '../types/loginResponse';

export function LoginPage() {
  const [email, setEmail] = useState('placeholder@example.com');
  const [password, setPassword] = useState('Password123!');
  const [result, setResult] = useState<LoginResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      setResult(await authService.login({ email, password }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container">
      <h1>Login</h1>

      <form onSubmit={handleSubmit} className="backend-card">
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <label>
            Email
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>

          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>

          <button type="submit" disabled={loading}>
            {loading ? 'Logging in...' : 'Log in'}
          </button>
        </div>
      </form>

      {error && <p className="error">{error}</p>}

      {result && (
        <div className="backend-card success">
          <p><strong>Logged in as:</strong> {result.user.name} ({result.user.email})</p>
          <p><strong>Expires:</strong> {new Date(result.expiresAtUtc).toLocaleString()}</p>
          <p style={{ wordBreak: 'break-all' }}><strong>Token:</strong> {result.accessToken}</p>
          <p>Saved to localStorage under "accessToken".</p>
        </div>
      )}
    </div>
  );
}
