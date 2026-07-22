import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createProjectIssue,
  deleteProjectIssue,
  updateProjectIssue,
  updateProjectIssueStatus,
  type SaveProjectIssuePayload,
} from '../api/projectIssues';
import type { ProjectIssueStatusValue } from '../api/types';

export function useCreateProjectIssueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createProjectIssue,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectIssues'] }),
  });
}

export function useUpdateProjectIssueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; payload: Omit<SaveProjectIssuePayload, 'projectId'> }) =>
      updateProjectIssue(variables.id, variables.payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectIssues'] }),
  });
}

export function useUpdateProjectIssueStatusMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; status: ProjectIssueStatusValue }) =>
      updateProjectIssueStatus(variables.id, variables.status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectIssues'] }),
  });
}

export function useDeleteProjectIssueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteProjectIssue,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectIssues'] }),
  });
}
