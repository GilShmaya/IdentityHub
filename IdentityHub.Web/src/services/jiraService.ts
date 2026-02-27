import api from './api';
import type { JiraConfigResponse, JiraProject, Ticket } from '../types';

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

  async createTicket(title: string, description: string, projectKey: string): Promise<Ticket> {
    const { data } = await api.post<Ticket>('/api/jira/tickets', { title, description, projectKey });
    return data;
  },

  async getRecentTickets(projectKey: string): Promise<Ticket[]> {
    const { data } = await api.get<Ticket[]>(`/api/jira/tickets/recent?projectKey=${projectKey}`);
    return data;
  },
};
