import api from './api';
import type { ApiKeyResponse, ApiKeyCreatedResponse } from '../types';

export const apiKeyService = {
  async createKey(name: string): Promise<ApiKeyCreatedResponse> {
    const { data } = await api.post<ApiKeyCreatedResponse>('/api/keys', { name });
    return data;
  },

  async getKeys(): Promise<ApiKeyResponse[]> {
    const { data } = await api.get<ApiKeyResponse[]>('/api/keys');
    return data;
  },

  async revokeKey(id: number): Promise<void> {
    await api.delete(`/api/keys/${id}`);
  },
};
