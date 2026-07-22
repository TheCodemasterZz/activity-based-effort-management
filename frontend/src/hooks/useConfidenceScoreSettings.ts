import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getConfidenceScoreSettings, updateConfidenceScoreSettings } from '../api/confidenceScoreSettings';

export function useConfidenceScoreSettings() {
  return useQuery({ queryKey: ['confidenceScoreSettings'], queryFn: getConfidenceScoreSettings });
}

export function useUpdateConfidenceScoreSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateConfidenceScoreSettings,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['confidenceScoreSettings'] }),
  });
}
