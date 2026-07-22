import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createProjectRisk,
  deleteProjectRisk,
  updateProjectRisk,
  updateProjectRiskStatus,
  type SaveProjectRiskPayload,
} from '../api/projectRisks';
import type { ProjectRiskStatusValue } from '../api/types';

export function useCreateProjectRiskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createProjectRisk,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectRisks'] }),
  });
}

export function useUpdateProjectRiskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; payload: Omit<SaveProjectRiskPayload, 'projectId'> }) =>
      updateProjectRisk(variables.id, variables.payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectRisks'] }),
  });
}

export function useUpdateProjectRiskStatusMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; status: ProjectRiskStatusValue }) =>
      updateProjectRiskStatus(variables.id, variables.status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectRisks'] }),
  });
}

export function useDeleteProjectRiskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteProjectRisk,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectRisks'] }),
  });
}
