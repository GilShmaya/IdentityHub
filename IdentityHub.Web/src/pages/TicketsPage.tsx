import { useState, useEffect, type FormEvent } from 'react';
import { jiraService } from '../services/jiraService';
import type { JiraProject, Ticket } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';

export function TicketsPage() {
  const [projects, setProjects] = useState<JiraProject[]>([]);
  const [selectedProject, setSelectedProject] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [recentTickets, setRecentTickets] = useState<Ticket[]>([]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const [projectsLoading, setProjectsLoading] = useState(true);

  useEffect(() => {
    loadProjects();
  }, []);

  useEffect(() => {
    if (selectedProject) {
      loadRecentTickets(selectedProject);
    }
  }, [selectedProject]);

  const loadProjects = async () => {
    try {
      const data = await jiraService.getProjects();
      setProjects(data);
      if (data.length > 0) {
        setSelectedProject(data[0].key);
      }
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to load projects. Is Jira configured?');
    } finally {
      setProjectsLoading(false);
    }
  };

  const loadRecentTickets = async (projectKey: string) => {
    try {
      const data = await jiraService.getRecentTickets(projectKey);
      setRecentTickets(data);
    } catch {
      // Silently fail — tickets list is supplementary
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      const ticket = await jiraService.createTicket(title, description, selectedProject);
      setSuccess(`Ticket ${ticket.jiraIssueKey} created successfully!`);
      setTitle('');
      setDescription('');
      await loadRecentTickets(selectedProject);
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to create ticket.');
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleString();
  };

  if (projectsLoading) {
    return (
      <div className="page-container">
        <div className="page-card"><p>Loading projects...</p></div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="page-content-wide">
        <div className="page-card">
          <h2>Create NHI Finding Ticket</h2>
          <p className="page-description">
            Report an identity-related finding to your Jira workspace.
          </p>

          {error && <div className="alert alert-error">{error}</div>}
          {success && <div className="alert alert-success">{success}</div>}

          {projects.length === 0 ? (
            <div className="alert alert-error">
              No projects found. Please <a href="/jira-config">configure Jira</a> first.
            </div>
          ) : (
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label htmlFor="project">Project</label>
                <select
                  id="project"
                  value={selectedProject}
                  onChange={(e) => setSelectedProject(e.target.value)}
                  required
                >
                  {projects.map((p) => (
                    <option key={p.key} value={p.key}>
                      {p.name} ({p.key})
                    </option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="title">Title</label>
                <input
                  id="title"
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="e.g., Stale Service Account: svc-deploy-prod"
                  required
                  maxLength={512}
                />
              </div>
              <div className="form-group">
                <label htmlFor="description">Description</label>
                <textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Details about the NHI finding..."
                  required
                  maxLength={8000}
                />
              </div>
              <button type="submit" className="btn-primary" disabled={loading}>
                {loading ? 'Creating...' : 'Create Ticket'}
              </button>
            </form>
          )}
        </div>

        <div className="page-card">
          <h2>Recent Tickets</h2>
          <p className="page-description">
            Last 10 tickets created from this app
            {selectedProject && ` in ${selectedProject}`}.
          </p>

          {recentTickets.length === 0 ? (
            <p className="empty-state">No tickets created yet for this project.</p>
          ) : (
            <div className="tickets-list">
              {recentTickets.map((ticket) => (
                <a
                  key={ticket.jiraIssueKey}
                  href={ticket.selfUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="ticket-item"
                >
                  <div className="ticket-key">{ticket.jiraIssueKey}</div>
                  <div className="ticket-title">{ticket.title}</div>
                  <div className="ticket-date">{formatDate(ticket.createdAt)}</div>
                </a>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
