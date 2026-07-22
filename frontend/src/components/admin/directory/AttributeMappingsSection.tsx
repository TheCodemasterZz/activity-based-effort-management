import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import {
  useAttributeMappings,
  useCreateAttributeMappingMutation,
  useDeleteAttributeMappingMutation,
  useUpdateAttributeMappingMutation,
} from '../../../hooks/useAttributeMappings';
import type { DirectoryAttributeMappingDto, DirectoryDto } from '../../../api/types';

const inputClass =
  'w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

interface AttributeMappingsSectionProps {
  directory: DirectoryDto;
  onBack: () => void;
}

export function AttributeMappingsSection({ directory, onBack }: AttributeMappingsSectionProps) {
  const mappings = useAttributeMappings(directory.id);
  const createMutation = useCreateAttributeMappingMutation(directory.id);
  const updateMutation = useUpdateAttributeMappingMutation(directory.id);
  const deleteMutation = useDeleteAttributeMappingMutation(directory.id);

  const [adAttributeName, setAdAttributeName] = useState('');
  const [systemFieldName, setSystemFieldName] = useState('');
  const [fieldType, setFieldType] = useState('text');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const items = mappings.data ?? [];

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);

    try {
      await createMutation.mutateAsync({
        adAttributeName: adAttributeName.trim(),
        systemFieldName: systemFieldName.trim(),
        fieldType,
        isSynced: true,
        sortOrder: items.length,
      });
      setAdAttributeName('');
      setSystemFieldName('');
      setFieldType('text');
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute eklenemedi.');
    }
  };

  const handleToggleSynced = async (mapping: DirectoryAttributeMappingDto) => {
    setErrorMessage(null);
    try {
      await updateMutation.mutateAsync({
        id: mapping.id,
        payload: {
          adAttributeName: mapping.adAttributeName,
          systemFieldName: mapping.systemFieldName,
          fieldType: mapping.fieldType,
          isSynced: !mapping.isSynced,
          sortOrder: mapping.sortOrder,
        },
      });
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute güncellenemedi.');
    }
  };

  const handleDelete = async (mapping: DirectoryAttributeMappingDto) => {
    if (!window.confirm(`"${mapping.systemFieldName}" AD Attribute'unu silmek istediğinize emin misiniz?`))
      return;

    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(mapping.id);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute silinemedi.');
    }
  };

  const canCreate = adAttributeName.trim().length > 0 && systemFieldName.trim().length > 0;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — AD Attributes</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Listeye dön
        </button>
      </div>

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Dizindeki Alan</span>
          <input
            value={adAttributeName}
            onChange={(e) => setAdAttributeName(e.target.value)}
            placeholder="company"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Sistemdeki Ad</span>
          <input
            value={systemFieldName}
            onChange={(e) => setSystemFieldName(e.target.value)}
            placeholder="Kurum"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Tip</span>
          <select
            value={fieldType}
            onChange={(e) => setFieldType(e.target.value)}
            className={inputClass}
          >
            <option value="text">Metin</option>
            <option value="user">Kullanıcı</option>
            <option value="photo">Fotoğraf</option>
          </select>
        </label>
        <button
          type="submit"
          disabled={!canCreate || createMutation.isPending}
          className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
        >
          {createMutation.isPending ? 'Ekleniyor…' : 'Ekle'}
        </button>
      </form>

      {errorMessage && (
        <p role="alert" className="mb-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {mappings.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz AD Attribute tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Dizindeki Alan</th>
              <th className="py-2 pr-4 font-medium">Sistemdeki Ad</th>
              <th className="py-2 pr-4 font-medium">Tip</th>
              <th className="py-2 pr-4 font-medium">Senkronize</th>
              <th className="py-2 font-medium">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.map((mapping) => (
              <tr key={mapping.id} className="border-b border-slate-50 last:border-0">
                <td className="py-2 pr-4 font-mono text-xs text-slate-600">
                  {mapping.adAttributeName}
                </td>
                <td className="py-2 pr-4 text-slate-700">{mapping.systemFieldName}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {mapping.fieldType === 'user'
                    ? 'Kullanıcı'
                    : mapping.fieldType === 'photo'
                      ? 'Fotoğraf'
                      : 'Metin'}
                </td>
                <td className="py-2 pr-4">
                  <input
                    type="checkbox"
                    checked={mapping.isSynced}
                    onChange={() => handleToggleSynced(mapping)}
                    className="h-4 w-4"
                  />
                </td>
                <td className="py-2">
                  <button
                    type="button"
                    onClick={() => handleDelete(mapping)}
                    className="text-xs text-rose-600 hover:underline"
                  >
                    Sil
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
