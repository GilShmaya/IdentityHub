import { useState, useEffect, type FormEvent } from 'react';
import { apiKeyService } from '../services/apiKeyService';
import type { ApiKeyInfo } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';
import './ApiKeys.css';

export function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKeyInfo[]>([]);
  const [name, setName] = useState('');
  const [expiresInDays, setExpiresInDays] = useState(90);
  const [newKey, setNewKey] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadKeys();
  }, []);

  const loadKeys = async () => {
    try {
      const data = await apiKeyService.list();
      setKeys(data);
    } catch {
      // Ignore
    }
  };

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setNewKey(null);
    setCopied(false);
    setLoading(true);
    try {
      const result = await apiKeyService.create(name, expiresInDays);
      setNewKey(result.key);
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
    try {
      await apiKeyService.revoke(id);
      await loadKeys();
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to revoke API key.');
    }
  };

  const handleCopy = async () => {
    if (newKey) {
      await navigator.clipboard.writeText(newKey);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const activeKeys = keys.filter(k => !k.isRevoked && !k.isExpired);
  const expiredKeys = keys.filter(k => k.isExpired && !k.isRevoked);
  const revokedKeys = keys.filter(k => k.isRevoked);

  return (
    <div className="page-container">
      <div className="apikeys-layout">
        <div className="page-card">
          <h2>Create API Key</h2>
          <p className="page-description">
            Generate a key for programmatic access to the REST API.
          </p>

          {error && <div className="alert alert-error">{error}</div>}

          {newKey && (
            <div className="apikey-reveal">
              <div className="apikey-reveal-header">
                <span className="apikey-reveal-icon">◈</span>
                <strong>Your new API key</strong>
              </div>
              <p className="apikey-reveal-warning">
                Copy it now — you won't be able to see it again.
              </p>
              <div className="apikey-reveal-value">
                <code>{newKey}</code>
                <button
                  onClick={handleCopy}
                  className="btn-copy"
                  title="Copy to clipboard"
                >
                  {copied ? '✓ Copied' : 'Copy'}
                </button>
              </div>
            </div>
          )}

          <form onSubmit={handleCreate}>
            <div className="form-group">
              <label htmlFor="key-name">Key Name</label>
              <input
                id="key-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. CI/CD Pipeline, Security Scanner"
                required
                maxLength={128}
              />
              <small className="form-hint">
                A descriptive name to identify this key's purpose.
              </small>
            </div>
            <div className="form-group">
              <label htmlFor="key-expiry">Expiration</label>
              <select
                id="key-expiry"
                value={expiresInDays}
                onChange={(e) => setExpiresInDays(Number(e.target.value))}
                required
              >
                <option value={30}>30 days</option>
                <option value={60}>60 days</option>
                <option value={90}>90 days</option>
                <option value={180}>180 days</option>
                <option value={365}>365 days</option>
              </select>
            </div>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Generating...' : 'Generate Key'}
            </button>
          </form>
        </div>

        <div className="page-card">
          <h2>Your API Keys</h2>
          <p className="page-description">
            {activeKeys.length} active key{activeKeys.length !== 1 ? 's' : ''}
            {expiredKeys.length > 0 && `, ${expiredKeys.length} expired`}
          </p>

          {activeKeys.length === 0 && expiredKeys.length === 0 && revokedKeys.length === 0 && (
            <div className="empty-state">
              No API keys yet. Create one to get started.
            </div>
          )}

          {activeKeys.length > 0 && (
            <div className="apikey-list">
              {activeKeys.map(k => (
                <div key={k.id} className="apikey-item">
                  <div className="apikey-item-info">
                    <span className="apikey-item-name">{k.name}</span>
                    <span className="apikey-item-meta">
                      <code className="apikey-item-prefix">{k.prefix}••••</code>
                      <span className="apikey-item-date">
                        expires {new Date(k.expiresAt).toLocaleDateString()}
                      </span>
                    </span>
                  </div>
                  <button
                    onClick={() => handleRevoke(k.id)}
                    className="btn-revoke"
                  >
                    Revoke
                  </button>
                </div>
              ))}
            </div>
          )}

          {expiredKeys.length > 0 && (
            <>
              <div className="apikey-divider">Expired</div>
              <div className="apikey-list">
                {expiredKeys.map(k => (
                  <div key={k.id} className="apikey-item apikey-item-revoked">
                    <div className="apikey-item-info">
                      <span className="apikey-item-name">{k.name}</span>
                      <span className="apikey-item-meta">
                        <code className="apikey-item-prefix">{k.prefix}••••</code>
                        <span className="apikey-item-date">
                          expired {new Date(k.expiresAt).toLocaleDateString()}
                        </span>
                      </span>
                    </div>
                    <span className="apikey-badge-revoked">Expired</span>
                  </div>
                ))}
              </div>
            </>
          )}

          {revokedKeys.length > 0 && (
            <>
              <div className="apikey-divider">Revoked</div>
              <div className="apikey-list">
                {revokedKeys.map(k => (
                  <div key={k.id} className="apikey-item apikey-item-revoked">
                    <div className="apikey-item-info">
                      <span className="apikey-item-name">{k.name}</span>
                      <span className="apikey-item-meta">
                        <code className="apikey-item-prefix">{k.prefix}••••</code>
                        <span className="apikey-item-date">
                          {new Date(k.createdAt).toLocaleDateString()}
                        </span>
                      </span>
                    </div>
                    <span className="apikey-badge-revoked">Revoked</span>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
