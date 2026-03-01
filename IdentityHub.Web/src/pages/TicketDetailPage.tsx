import { useState, useEffect, type FormEvent } from 'react';
import { useParams, Link } from 'react-router-dom';
import { jiraService } from '../services/jiraService';
import type { TicketDetail, JiraUser, JiraTransition } from '../types';
import type { AxiosError } from 'axios';
import type { ApiError } from '../types';
import './Page.css';

const PRIORITIES = ['Highest', 'High', 'Medium', 'Low', 'Lowest'];

export function TicketDetailPage() {
  const { issueKey } = useParams<{ issueKey: string }>();
  const [ticket, setTicket] = useState<TicketDetail | null>(null);
  const [assignableUsers, setAssignableUsers] = useState<JiraUser[]>([]);
  const [transitions, setTransitions] = useState<JiraTransition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');

  // Edit state
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editPriority, setEditPriority] = useState('');
  const [editAssignee, setEditAssignee] = useState('');

  // Comment state
  const [commentBody, setCommentBody] = useState('');
  const [addingComment, setAddingComment] = useState(false);

  useEffect(() => {
    if (issueKey) loadTicket(issueKey);
  }, [issueKey]);

  const loadTicket = async (key: string) => {
    setLoading(true);
    setError('');
    try {
      const data = await jiraService.getTicket(key);
      setTicket(data);
      setEditTitle(data.title);
      setEditDescription(data.description || '');
      setEditPriority(data.priority || '');
      setEditAssignee(data.assigneeAccountId || '');

      // Extract project key from issue key (e.g., "NHI-42" -> "NHI")
      const projectKey = key.split('-')[0];
      try {
        const users = await jiraService.getAssignableUsers(projectKey);
        setAssignableUsers(users);
      } catch {
        setAssignableUsers([]);
      }
      try {
        const t = await jiraService.getTransitions(key);
        setTransitions(t);
      } catch {
        setTransitions([]);
      }
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to load ticket.');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (e: FormEvent) => {
    e.preventDefault();
    if (!ticket || !issueKey) return;
    setSaving(true);
    setError('');
    setSuccessMsg('');
    try {
      const updates: Record<string, string> = {};
      if (editTitle !== ticket.title) updates.title = editTitle;
      if (editDescription !== (ticket.description || '')) updates.description = editDescription;
      if (editPriority !== (ticket.priority || '')) updates.priority = editPriority;
      if (editAssignee !== (ticket.assigneeAccountId || '')) updates.assigneeAccountId = editAssignee;

      if (Object.keys(updates).length === 0) {
        setEditing(false);
        return;
      }

      await jiraService.updateTicket(issueKey, updates);
      setSuccessMsg('Ticket updated successfully.');
      setEditing(false);
      await loadTicket(issueKey);
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to update ticket.');
    } finally {
      setSaving(false);
    }
  };

  const handleAddComment = async (e: FormEvent) => {
    e.preventDefault();
    if (!issueKey || !commentBody.trim()) return;
    setAddingComment(true);
    setError('');
    try {
      await jiraService.addComment(issueKey, commentBody);
      setCommentBody('');
      await loadTicket(issueKey);
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to add comment.');
    } finally {
      setAddingComment(false);
    }
  };

  const handleTransition = async (transitionId: string) => {
    if (!issueKey) return;
    setSaving(true);
    setError('');
    setSuccessMsg('');
    try {
      await jiraService.transitionTicket(issueKey, transitionId);
      setSuccessMsg('Status updated successfully.');
      await loadTicket(issueKey);
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(axiosError.response?.data?.error || 'Failed to update status.');
    } finally {
      setSaving(false);
    }
  };

  const formatDate = (dateStr: string) => new Date(dateStr).toLocaleString();

  if (loading) {
    return (
      <div className="page-container">
        <div className="page-card"><p>Loading ticket...</p></div>
      </div>
    );
  }

  if (!ticket) {
    return (
      <div className="page-container">
        <div className="page-card">
          <div className="alert alert-error">{error || 'Ticket not found.'}</div>
          <Link to="/" className="back-link">← Back to tickets</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="detail-layout">
        <div className="detail-header">
          <Link to="/" className="back-link">← Back to tickets</Link>
          <div className="detail-header-row">
            <span className="detail-issue-key">{ticket.jiraIssueKey}</span>
            <span className={`status-pill status-${ticket.status.toLowerCase().replace(/\s+/g, '-')}`}>
              {ticket.status}
            </span>
            <a href={ticket.selfUrl} target="_blank" rel="noreferrer" className="jira-link">
              Open in Jira ↗
            </a>
          </div>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {successMsg && <div className="alert alert-success">{successMsg}</div>}

        <div className="detail-content">
          <div className="page-card">
            <div className="card-header">
              <h2>{editing ? 'Edit Ticket' : 'Details'}</h2>
              {!editing && (
                <button className="btn-secondary" onClick={() => setEditing(true)}>Edit</button>
              )}
            </div>

            {editing ? (
              <form onSubmit={handleSave}>
                <div className="form-group">
                  <label htmlFor="editTitle">Title</label>
                  <input
                    id="editTitle"
                    value={editTitle}
                    onChange={(e) => setEditTitle(e.target.value)}
                    required
                    maxLength={512}
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="editDesc">Description</label>
                  <textarea
                    id="editDesc"
                    value={editDescription}
                    onChange={(e) => setEditDescription(e.target.value)}
                    maxLength={8000}
                  />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label htmlFor="editPriority">Priority</label>
                    <select
                      id="editPriority"
                      value={editPriority}
                      onChange={(e) => setEditPriority(e.target.value)}
                    >
                      <option value="">Default</option>
                      {PRIORITIES.map((p) => (
                        <option key={p} value={p}>{p}</option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label htmlFor="editAssignee">Assignee</label>
                    <select
                      id="editAssignee"
                      value={editAssignee}
                      onChange={(e) => setEditAssignee(e.target.value)}
                    >
                      <option value="">Unassigned</option>
                      {assignableUsers.map((u) => (
                        <option key={u.accountId} value={u.accountId}>{u.displayName}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <div className="btn-row">
                  <button type="submit" className="btn-primary" disabled={saving}>
                    {saving ? 'Saving...' : 'Save Changes'}
                  </button>
                  <button type="button" className="btn-secondary" onClick={() => {
                    setEditing(false);
                    setEditTitle(ticket.title);
                    setEditDescription(ticket.description || '');
                    setEditPriority(ticket.priority || '');
                    setEditAssignee(ticket.assigneeAccountId || '');
                  }}>Cancel</button>
                </div>
              </form>
            ) : (
              <div className="detail-fields">
                <div className="detail-field">
                  <span className="detail-label">Title</span>
                  <span className="detail-value">{ticket.title}</span>
                </div>
                <div className="detail-field">
                  <span className="detail-label">Description</span>
                  <span className="detail-value detail-description">
                    {ticket.description || '—'}
                  </span>
                </div>
                <div className="detail-field-row">
                  <div className="detail-field">
                    <span className="detail-label">Priority</span>
                    <span className="detail-value">{ticket.priority || 'Default'}</span>
                  </div>
                  <div className="detail-field">
                    <span className="detail-label">Assignee</span>
                    <span className="detail-value">{ticket.assigneeDisplayName || 'Unassigned'}</span>
                  </div>
                  <div className="detail-field">
                    <span className="detail-label">Status</span>
                    <span className="detail-value">{ticket.status}</span>
                  </div>
                </div>
                {transitions.length > 0 && (
                  <div className="detail-field">
                    <span className="detail-label">Move to</span>
                    <div className="transition-buttons">
                      {transitions.map((t) => (
                        <button
                          key={t.id}
                          className="btn-transition"
                          disabled={saving}
                          onClick={() => handleTransition(t.id)}
                        >
                          {t.name}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="page-card">
            <h2>Comments</h2>
            <form onSubmit={handleAddComment} className="comment-form">
              <div className="form-group">
                <textarea
                  value={commentBody}
                  onChange={(e) => setCommentBody(e.target.value)}
                  placeholder="Add a comment..."
                  required
                  maxLength={8000}
                />
              </div>
              <button type="submit" className="btn-primary" disabled={addingComment || !commentBody.trim()}>
                {addingComment ? 'Posting...' : 'Add Comment'}
              </button>
            </form>

            {ticket.comments.length === 0 ? (
              <p className="empty-state">No comments yet.</p>
            ) : (
              <div className="comments-list">
                {ticket.comments.map((c) => (
                  <div key={c.id} className="comment-item">
                    <div className="comment-header">
                      <span className="comment-author">{c.authorDisplayName}</span>
                      <span className="comment-date">{formatDate(c.created)}</span>
                    </div>
                    <div className="comment-body">{c.body}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
