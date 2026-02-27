import api from './api';
import type { AuthResponse } from '../types';

export const authService = {
  async register(email: string, password: string): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/api/auth/register', { email, password });
    localStorage.setItem('token', data.token);
    localStorage.setItem('email', data.email);
    return data;
  },

  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await api.post<AuthResponse>('/api/auth/login', { email, password });
    localStorage.setItem('token', data.token);
    localStorage.setItem('email', data.email);
    return data;
  },

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('email');
  },

  isAuthenticated(): boolean {
    return !!localStorage.getItem('token');
  },

  getEmail(): string | null {
    return localStorage.getItem('email');
  },
};
