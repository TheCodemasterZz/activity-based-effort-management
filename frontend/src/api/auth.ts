import { apiClient } from './client';
import type { LoginResultDto } from './types';

export function login(username: string, password: string) {
  return apiClient.post<LoginResultDto>('/api/v1/auth/login', { username, password });
}
