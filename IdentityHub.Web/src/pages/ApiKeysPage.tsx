import { useState, useEffect } from 'react';
import { apiKeyService } from '../services/apiKeyService';
import type { ApiKeyResponse, ApiKeyCreatedResponse } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';

export function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKeyResponse[]>([]);
  const [name, setName] = useState('');
  const [newKey, setNewKey] = useState<ApiKeyCreatedResponse | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadKeys();
  }, []);

  const loadKeys = async () => {
    try {
      const data = await apiKeyService.getKeys();
      setKeys(data);
    } catch {
      setError('Failed to load API keys.');
    }
  };

  const handleCreate = async () => {
    if (!name.trim()) return;
    setError('');
    setNewKey(null);
    setLoading(true);
    try {
      const created = await apiKeyService.createKey(name);
      setNewKey(created);
      setName('');
      await loadKeys();
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to create API key.');
    } finally {
      setLoading(false);
    }
  };

  const handleRevoke = async (id: number) => {
    if (!confirm('Are you sure you want to revoke this API key? This cannot be undone.')) return;
    try {
      await apiKeyService.revokeKey(id);
      await loadKeys();
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to revoke API key.');
    }
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleString();
  };

  return (
    <div className="page-container">
      <div className="page-card" style={{ maxWidth: 700 }}>
        <h2>API Keys</h2>
        <p className="page-description">
          Manage API keys for external systems to create NHI findings programmatically.
        </p>

        {error && <div className="alert alert-error">{error}</div>}

        {newKey && (
          <div className="alert alert-success">
            <strong>API Key created!</strong> Copy it now — it won't be shown again.
            <div className="api-key-display">{newKey.key}</div>
          </div>
        )}

        <div className="create-key-row">
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Key name (e.g., CI/CD Scanner)"
            className="create-key-input"
            maxLength={128}
          />
          <button onClick={handleCreate} className="btn-primary btn-inline" disabled={loading || !name.trim()}>
            {loading ? 'Creating...' : 'Create Key'}
          </button>
        </div>

        <div className="keys-list">
          {keys.length === 0 ? (
            <p className="empty-state">No API keys created yet.</p>
          ) : (
            keys.map((key) => (
              <div key={key.id} className={`key-item ${key.isRevoked ? 'key-revoked' : ''}`}>
                <div className="key-info">
                  <div className="key-name">{key.name}</div>
                  <div className="key-meta">
                    {key.keyPrefix}••• · Created {formatDate(key.createdAt)}
                    {key.isRevoked && <span className="badge-revoked">Revoked</span>}
                  </div>
                </div>
                {!key.isRevoked && (
                  <button onClick={() => handleRevoke(key.id)} className="btn-danger">
                    Revoke
                  </button>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
