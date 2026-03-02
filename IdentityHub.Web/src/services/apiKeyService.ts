import api from './api';
import type { ApiKeyInfo, CreateApiKeyResponse } from '../types';

export const apiKeyService = {
  async create(name: string): Promise<CreateApiKeyResponse> {
    const { data } = await api.post<CreateApiKeyResponse>('/api/keys', { name });
    return data;
  },

  async list(): Promise<ApiKeyInfo[]> {
    const { data } = await api.get<ApiKeyInfo[]>('/api/keys');
    return data;
  },

  async revoke(id: number): Promise<void> {
    await api.delete(`/api/keys/${id}`);
  },
};
