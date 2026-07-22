import { apiClient } from './client';
import type { ConfidenceScoreSettingsDto } from './types';

export function getConfidenceScoreSettings() {
  return apiClient.get<ConfidenceScoreSettingsDto>('/api/v1/confidencescoresettings');
}

export type SaveConfidenceScoreSettingsPayload = Omit<ConfidenceScoreSettingsDto, 'id'>;

export function updateConfidenceScoreSettings(payload: SaveConfidenceScoreSettingsPayload) {
  return apiClient.put<void>('/api/v1/confidencescoresettings', payload);
}
