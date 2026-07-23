import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import type { PermissionDescriptorDto } from '../../../api/types';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import {
  useAssignUserToRoleMutation,
  useCreateRoleMutation,
  useDeleteRoleMutation,
  useGrantPermissionMutation,
  usePermissionCatalog,
  useRemoveUserFromRoleMutation,
  useRevokePermissionMutation,
  useRole,
  useRoles,
} from '../../../hooks/useRoles';

const inputClass =
  'w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

function groupByModule(descriptors: PermissionDescriptorDto[]): Record<string, PermissionDescriptorDto[]> {
  const groups: Record<string, PermissionDescriptorDto[]> = {};
  for (const descriptor of descriptors) {
    (groups[descriptor.moduleLabel] ??= []).push(descriptor);
  }
  return groups;
}

function RoleForm({
  initialName,
  initialDescription,
  submitLabel,
  onSubmit,
  onCancel,
}: {
  initialName: string;
  initialDescription: string;
  submitLabel: string;
  onSubmit: (name: string, description: string) => Promise<void>;
  onCancel: () => void;
}) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      await onSubmit(name.trim(), description.trim());
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Rol kaydedilemedi.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="mb-5 flex flex-wrap items-end gap-2">
      <label className="block">
        <span className="mb-1 block text-xs font-medium text-slate-600">Rol Adı</span>
        <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
      </label>
      <label className="block">
        <span className="mb-1 block text-xs font-medium text-slate-600">Açıklama</span>
        <input value={description} onChange={(e) => setDescription(e.target.value)} className={inputClass} />
      </label>
      <button
        type="submit"
        disabled={name.trim().length === 0 || isSubmitting}
        className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
      >
        {isSubmitting ? 'Kaydediliyor…' : submitLabel}
      </button>
      <button type="button" onClick={onCancel} className="text-sm text-slate-500 hover:text-slate-700">
        Vazgeç
      </button>
      {errorMessage && <p role="alert" className="w-full text-sm text-rose-700">{errorMessage}</p>}
    </form>
  );
}

function RoleDetail({ roleId, onBack }: { roleId: string; onBack: () => void }) {
  const role = useRole(roleId);
  const catalog = usePermissionCatalog();
  const grantMutation = useGrantPermissionMutation(roleId);
  const revokeMutation = useRevokePermissionMutation(roleId);
  const assignMutation = useAssignUserToRoleMutation(roleId);
  const removeMutation = useRemoveUserFromRoleMutation(roleId);
  const [userSearch, setUserSearch] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const users = useDirectoryUsers({ searchTerm: userSearch, pageSize: 10 });

  if (role.isLoading || !role.data) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  const grantedSet = new Set(role.data.permissions);
  const modules = groupByModule(catalog.data ?? []);
  const assignedUserIds = new Set(role.data.assignedUsers.map((u) => u.id));

  const togglePermission = async (key: string) => {
    setErrorMessage(null);
    try {
      if (grantedSet.has(key)) {
        await revokeMutation.mutateAsync(key);
      } else {
        await grantMutation.mutateAsync(key);
      }
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'İzin güncellenemedi.');
    }
  };

  const handleAssignUser = async (userId: string) => {
    setErrorMessage(null);
    try {
      await assignMutation.mutateAsync(userId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Kullanıcı atanamadı.');
    }
  };

  const handleRemoveUser = async (userId: string) => {
    setErrorMessage(null);
    try {
      await removeMutation.mutateAsync(userId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Kullanıcı kaldırılamadı.');
    }
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">
          {role.data.name}
          {role.data.isSystemAdmin && (
            <span className="ml-2 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
              Sistem Yöneticisi
            </span>
          )}
        </h2>
        <button type="button" onClick={onBack} className="text-sm text-slate-500 hover:text-slate-700">
          ← Listeye dön
        </button>
      </div>

      {errorMessage && (
        <p role="alert" className="mb-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {role.data.isSystemAdmin ? (
        <p className="mb-6 text-sm text-slate-500">
          Bu rol sistemdeki tüm işlemlere erişebilir; izin listesi ayrıca yönetilmez.
        </p>
      ) : (
        <div className="mb-6">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">İzinler</h3>
          <div className="space-y-3">
            {Object.entries(modules).map(([moduleLabel, descriptors]) => (
              <div key={moduleLabel}>
                <p className="mb-1 text-xs font-medium uppercase tracking-wide text-slate-400">{moduleLabel}</p>
                <div className="flex flex-wrap gap-3">
                  {descriptors.map((descriptor) => (
                    <label key={descriptor.key} className="flex items-center gap-1.5 text-sm text-slate-700">
                      <input
                        type="checkbox"
                        checked={grantedSet.has(descriptor.key)}
                        onChange={() => togglePermission(descriptor.key)}
                        className="h-4 w-4"
                      />
                      {descriptor.label}
                    </label>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div>
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Atanmış Kullanıcılar</h3>
        {role.data.assignedUsers.length === 0 ? (
          <p className="mb-3 text-sm text-slate-400">Bu role henüz kimse atanmamış.</p>
        ) : (
          <ul className="mb-3 space-y-1">
            {role.data.assignedUsers.map((user) => (
              <li key={user.id} className="flex items-center justify-between text-sm">
                <span className="text-slate-700">{user.displayName ?? user.username}</span>
                <button
                  type="button"
                  onClick={() => handleRemoveUser(user.id)}
                  className="text-xs text-rose-600 hover:underline"
                >
                  Kaldır
                </button>
              </li>
            ))}
          </ul>
        )}

        <input
          value={userSearch}
          onChange={(e) => setUserSearch(e.target.value)}
          placeholder="Kullanıcı adı ile ara ve ekle"
          className={inputClass}
        />
        {userSearch.trim().length > 0 && (
          <ul className="mt-2 space-y-1">
            {(users.data?.items ?? [])
              .filter((user) => !assignedUserIds.has(user.id))
              .map((user) => (
                <li key={user.id} className="flex items-center justify-between text-sm">
                  <span className="text-slate-700">{user.displayName ?? user.username}</span>
                  <button
                    type="button"
                    onClick={() => handleAssignUser(user.id)}
                    className="text-xs text-indigo-600 hover:underline"
                  >
                    Ekle
                  </button>
                </li>
              ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export function RolesSection() {
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const roles = useRoles();
  const createMutation = useCreateRoleMutation();
  const deleteMutation = useDeleteRoleMutation();

  if (selectedRoleId) {
    return <RoleDetail roleId={selectedRoleId} onBack={() => setSelectedRoleId(null)} />;
  }

  const items = roles.data ?? [];

  const handleDelete = async (roleId: string, roleName: string) => {
    if (!window.confirm(`"${roleName}" rolünü silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(roleId);
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Roller ve İzinler</h2>
        {!isCreating && (
          <button
            type="button"
            onClick={() => setIsCreating(true)}
            className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            Yeni Rol
          </button>
        )}
      </div>

      {isCreating && (
        <RoleForm
          initialName=""
          initialDescription=""
          submitLabel="Oluştur"
          onCancel={() => setIsCreating(false)}
          onSubmit={async (name, description) => {
            await createMutation.mutateAsync({ name, description: description || null });
            setIsCreating(false);
          }}
        />
      )}

      {roles.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz rol tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Ad</th>
              <th className="py-2 pr-4 font-medium">Açıklama</th>
              <th className="py-2 pr-4 font-medium">İzin Sayısı</th>
              <th className="py-2 font-medium">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.map((role) => (
              <tr key={role.id} className="border-b border-slate-50 last:border-0">
                <td
                  onClick={() => setSelectedRoleId(role.id)}
                  className="cursor-pointer py-2 pr-4 text-indigo-600 hover:underline"
                >
                  {role.name}
                  {role.isSystemAdmin && (
                    <span className="ml-2 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
                      Sistem Yöneticisi
                    </span>
                  )}
                </td>
                <td className="py-2 pr-4 text-slate-500">{role.description ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {role.isSystemAdmin ? 'Tümü' : role.permissionCount}
                </td>
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    <button
                      type="button"
                      onClick={() => setSelectedRoleId(role.id)}
                      className="text-indigo-600 hover:underline"
                    >
                      Düzenle
                    </button>
                    {!role.isSystemAdmin && (
                      <button
                        type="button"
                        onClick={() => handleDelete(role.id, role.name)}
                        className="text-rose-600 hover:underline"
                      >
                        Sil
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
