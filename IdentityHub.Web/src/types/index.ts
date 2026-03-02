export interface AuthResponse {
  token: string;
  email: string;
}

export interface JiraConfigResponse {
  email: string;
  siteUrl: string;
  isConfigured: boolean;
}

export interface JiraProject {
  key: string;
  name: string;
}

export interface JiraUser {
  accountId: string;
  displayName: string;
  avatarUrl?: string;
}

export interface Ticket {
  jiraIssueKey: string;
  title: string;
  selfUrl: string;
  createdAt: string;
}

export interface TicketDetail {
  jiraIssueKey: string;
  title: string;
  description?: string;
  status: string;
  priority?: string;
  assigneeAccountId?: string;
  assigneeDisplayName?: string;
  selfUrl: string;
  comments: TicketComment[];
}

export interface TicketComment {
  id: string;
  authorDisplayName: string;
  body: string;
  created: string;
}

export interface JiraTransition {
  id: string;
  name: string;
}

export interface ApiError {
  error: string;
}

export interface ApiKeyInfo {
  id: number;
  name: string;
  prefix: string;
  createdAt: string;
  expiresAt: string;
  isRevoked: boolean;
  isExpired: boolean;
}

export interface CreateApiKeyResponse {
  id: number;
  name: string;
  key: string;
  prefix: string;
  createdAt: string;
  expiresAt: string;
}
