import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createProjectTask,
  deleteProjectTask,
  updateProjectTask,
  updateProjectTaskStatus,
  type SaveProjectTaskPayload,
} from '../api/projectTasks';
import type { ProjectTaskStatusValue } from '../api/types';

/** Görev CQRS mutasyonları — hepsi aynı `projectTasks` sorgu anahtarını (proje bazlı liste)
 * geçersiz kılar, tek dosyada toplanarak tekrarı azaltır. */
export function useCreateProjectTaskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createProjectTask,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectTasks'] }),
  });
}

export function useUpdateProjectTaskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; payload: Omit<SaveProjectTaskPayload, 'projectId'> }) =>
      updateProjectTask(variables.id, variables.payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectTasks'] }),
  });
}

export function useUpdateProjectTaskStatusMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; status: ProjectTaskStatusValue }) =>
      updateProjectTaskStatus(variables.id, variables.status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectTasks'] }),
  });
}

export function useDeleteProjectTaskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteProjectTask,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projectTasks'] }),
  });
}
