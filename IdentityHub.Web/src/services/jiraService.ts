import api from './api';
import type { JiraConfigResponse, JiraProject, JiraUser, Ticket, TicketDetail, TicketComment, JiraTransition } from '../types';

export const jiraService = {
  async saveConfig(email: string, apiToken: string, siteUrl: string): Promise<void> {
    await api.post('/api/jira/config', { email, apiToken, siteUrl });
  },

  async getConfig(): Promise<JiraConfigResponse> {
    const { data } = await api.get<JiraConfigResponse>('/api/jira/config');
    return data;
  },

  async getProjects(): Promise<JiraProject[]> {
    const { data } = await api.get<JiraProject[]>('/api/jira/projects');
    return data;
  },

  async getAssignableUsers(projectKey: string): Promise<JiraUser[]> {
    const { data } = await api.get<JiraUser[]>(`/api/jira/projects/${projectKey}/users`);
    return data;
  },

  async createTicket(
    title: string,
    description: string,
    projectKey: string,
    assigneeAccountId?: string,
    priority?: string
  ): Promise<Ticket> {
    const { data } = await api.post<Ticket>('/api/jira/tickets', {
      title,
      description,
      projectKey,
      assigneeAccountId: assigneeAccountId || null,
      priority: priority || null,
    });
    return data;
  },

  async getRecentTickets(projectKey: string): Promise<Ticket[]> {
    const { data } = await api.get<Ticket[]>(`/api/jira/tickets/recent?projectKey=${projectKey}`);
    return data;
  },

  async getTicket(issueKey: string): Promise<TicketDetail> {
    const { data } = await api.get<TicketDetail>(`/api/jira/tickets/${issueKey}`);
    return data;
  },

  async updateTicket(issueKey: string, updates: {
    title?: string;
    description?: string;
    assigneeAccountId?: string;
    priority?: string;
  }): Promise<void> {
    await api.put(`/api/jira/tickets/${issueKey}`, updates);
  },

  async addComment(issueKey: string, body: string): Promise<TicketComment> {
    const { data } = await api.post<TicketComment>(`/api/jira/tickets/${issueKey}/comments`, { body });
    return data;
  },

  async getTransitions(issueKey: string): Promise<JiraTransition[]> {
    const { data } = await api.get<JiraTransition[]>(`/api/jira/tickets/${issueKey}/transitions`);
    return data;
  },

  async transitionTicket(issueKey: string, transitionId: string): Promise<void> {
    await api.post(`/api/jira/tickets/${issueKey}/transitions`, { transitionId });
  },
};
