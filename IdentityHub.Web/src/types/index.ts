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

export interface Ticket {
  jiraIssueKey: string;
  title: string;
  selfUrl: string;
  createdAt: string;
}

export interface ApiKeyResponse {
  id: number;
  name: string;
  keyPrefix: string;
  createdAt: string;
  isRevoked: boolean;
}

export interface ApiKeyCreatedResponse {
  id: number;
  name: string;
  key: string;
  createdAt: string;
}

export interface ApiError {
  error: string;
}
