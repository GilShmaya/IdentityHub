import { useState, useEffect, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { jiraService } from '../services/jiraService';
import type { JiraProject, JiraUser, Ticket } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';

const PRIORITIES = ['Highest', 'High', 'Medium', 'Low', 'Lowest'];

export function TicketsPage() {
  const [projects, setProjects] = useState<JiraProject[]>([]);
  const [selectedProject, setSelectedProject] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState('');
  const [assignee, setAssignee] = useState('');
  const [assignableUsers, setAssignableUsers] = useState<JiraUser[]>([]);
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
      loadAssignableUsers(selectedProject);
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

  const loadAssignableUsers = async (projectKey: string) => {
    try {
      const users = await jiraService.getAssignableUsers(projectKey);
      setAssignableUsers(users);
    } catch {
      setAssignableUsers([]);
    }
  };

  const loadRecentTickets = async (projectKey: string) => {
    try {
      const data = await jiraService.getRecentTickets(projectKey);
      setRecentTickets(data);
    } catch {
      // Silently fail
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      const ticket = await jiraService.createTicket(title, description, selectedProject, assignee || undefined, priority || undefined);
      setSuccess(`Ticket ${ticket.jiraIssueKey} created successfully!`);
      setTitle('');
      setDescription('');
      setPriority('');
      setAssignee('');
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
            Report an identity-related finding to your Jira site.
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
              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="priority">Priority</label>
                  <select
                    id="priority"
                    value={priority}
                    onChange={(e) => setPriority(e.target.value)}
                  >
                    <option value="">Default</option>
                    {PRIORITIES.map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label htmlFor="assignee">Assignee</label>
                  <select
                    id="assignee"
                    value={assignee}
                    onChange={(e) => setAssignee(e.target.value)}
                  >
                    <option value="">Unassigned</option>
                    {assignableUsers.map((u) => (
                      <option key={u.accountId} value={u.accountId}>{u.displayName}</option>
                    ))}
                  </select>
                </div>
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
                <div key={ticket.jiraIssueKey} className="ticket-item">
                  <div className="ticket-key">{ticket.jiraIssueKey}</div>
                  <div className="ticket-title">{ticket.title}</div>
                  <div className="ticket-date">{formatDate(ticket.createdAt)}</div>
                  <div className="ticket-actions">
                    <Link to={`/tickets/${ticket.jiraIssueKey}`} className="ticket-action-link">
                      View
                    </Link>
                    <a
                      href={ticket.selfUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="ticket-action-link"
                    >
                      Jira ↗
                    </a>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
