import { apiClient } from './client';
import type { PagedResult } from './types';

export interface NotificationDto {
  id: string;
  message: string;
  createdAtUtc: string;
  isRead: boolean;
}

export function getNotifications() {
  return apiClient.get<PagedResult<NotificationDto>>('/api/v1/notifications', { pageSize: 50 });
}

export function markNotificationRead(id: string) {
  return apiClient.patch<void>(`/api/v1/notifications/${id}/read`);
}
