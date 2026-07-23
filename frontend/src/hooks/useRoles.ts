import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignUserToRole,
  createRole,
  deleteRole,
  getPermissionCatalog,
  getRoleById,
  getRoles,
  grantPermission,
  removeUserFromRole,
  revokePermission,
  updateRole,
  type SaveRolePayload,
} from '../api/roles';

export function useRoles() {
  return useQuery({ queryKey: ['roles'], queryFn: getRoles });
}

export function useRole(id: string | null) {
  return useQuery({
    queryKey: ['roles', id],
    queryFn: () => getRoleById(id!),
    enabled: id !== null,
  });
}

export function usePermissionCatalog() {
  return useQuery({ queryKey: ['roles', 'permission-catalog'], queryFn: getPermissionCatalog });
}

export function useCreateRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveRolePayload) => createRole(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles'] }),
  });
}

export function useUpdateRoleMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveRolePayload) => updateRole(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      queryClient.invalidateQueries({ queryKey: ['roles', id] });
    },
  });
}

export function useDeleteRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles'] }),
  });
}

export function useGrantPermissionMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (permissionKey: string) => grantPermission(roleId, permissionKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useRevokePermissionMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (permissionKey: string) => revokePermission(roleId, permissionKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useAssignUserToRoleMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => assignUserToRole(roleId, userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useRemoveUserFromRoleMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => removeUserFromRole(roleId, userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}
