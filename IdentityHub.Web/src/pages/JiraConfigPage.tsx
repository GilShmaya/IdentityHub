import { useState, useEffect, type FormEvent } from 'react';
import { jiraService } from '../services/jiraService';
import type { JiraConfigResponse } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';

export function JiraConfigPage() {
  const [email, setEmail] = useState('');
  const [apiToken, setApiToken] = useState('');
  const [siteUrl, setSiteUrl] = useState('');
  const [config, setConfig] = useState<JiraConfigResponse | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadConfig();
  }, []);

  const loadConfig = async () => {
    try {
      const data = await jiraService.getConfig();
      setConfig(data);
      if (data.isConfigured) {
        setEmail(data.email);
        setSiteUrl(data.siteUrl);
      }
    } catch {
      // Config not set yet — that's okay
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      await jiraService.saveConfig(email, apiToken, siteUrl);
      setSuccess('Jira configuration saved successfully!');
      setApiToken('');
      await loadConfig();
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to save configuration.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page-container">
      <div className="page-card">
        <h2>Jira Configuration</h2>
        <p className="page-description">
          Connect your Jira workspace to create NHI finding tickets.
        </p>

        {config?.isConfigured && (
          <div className="status-badge status-connected">
            ✅ Connected to {config.siteUrl}
          </div>
        )}

        {error && <div className="alert alert-error">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="jira-email">Jira Email</label>
            <input
              id="jira-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="your-email@company.com"
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="jira-token">API Token</label>
            <input
              id="jira-token"
              type="password"
              value={apiToken}
              onChange={(e) => setApiToken(e.target.value)}
              placeholder={config?.isConfigured ? '••••••••  (enter new token to update)' : 'Paste your Jira API token'}
              required={!config?.isConfigured}
            />
            <small className="form-hint">
              Generate at{' '}
              <a href="https://id.atlassian.com/manage-profile/security/api-tokens" target="_blank" rel="noreferrer">
                Atlassian API Tokens
              </a>
            </small>
          </div>
          <div className="form-group">
            <label htmlFor="jira-site">Jira Site URL</label>
            <input
              id="jira-site"
              type="url"
              value={siteUrl}
              onChange={(e) => setSiteUrl(e.target.value)}
              placeholder="https://your-org.atlassian.net"
              required
            />
          </div>
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Saving...' : config?.isConfigured ? 'Update Configuration' : 'Save Configuration'}
          </button>
        </form>
      </div>
    </div>
  );
}
