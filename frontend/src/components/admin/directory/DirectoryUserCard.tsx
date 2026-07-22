import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import {
  useDirectoryUser,
  useResetInternalUserPasswordMutation,
} from '../../../hooks/useDirectoryUsers';

interface DirectoryUserCardProps {
  userId: string;
  onBack: () => void;
}

const SOURCE_LABEL: Record<number, string> = {
  0: 'Internal',
  1: 'Active Directory',
};

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex border-b border-slate-50 py-2 last:border-0">
      <div className="w-48 shrink-0 text-sm text-slate-500">{label}</div>
      <div className="text-sm text-slate-700">{value}</div>
    </div>
  );
}

/** Yalnızca internal kullanıcılar için; AD kullanıcısının şifresi dizinde yönetilir. */
function ResetPasswordPanel({ userId }: { userId: string }) {
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState<{ text: string; isError: boolean } | null>(null);
  const resetMutation = useResetInternalUserPasswordMutation();

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setMessage(null);

    try {
      await resetMutation.mutateAsync({ userId, newPassword });
      setNewPassword('');
      setMessage({ text: 'Şifre güncellendi.', isError: false });
    } catch (error) {
      setMessage({
        text: error instanceof ApiError ? error.message : 'Şifre güncellenemedi.',
        isError: true,
      });
    }
  };

  return (
    <div>
      <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Şifre</h4>
      <p className="mb-3 text-sm text-slate-500">
        Kullanıcının şifresini sıfırlayın. Yeni şifreyi kullanıcıya siz iletmelisiniz.
      </p>

      <form onSubmit={handleSubmit} className="flex flex-wrap items-end gap-2">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Yeni Şifre</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
            className="w-64 rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
          />
        </label>
        <button
          type="submit"
          disabled={newPassword.length < 8 || resetMutation.isPending}
          className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
        >
          {resetMutation.isPending ? 'Güncelleniyor…' : 'Şifreyi Sıfırla'}
        </button>
      </form>

      {newPassword.length > 0 && newPassword.length < 8 && (
        <p className="mt-2 text-xs text-slate-400">Şifre en az 8 karakter olmalıdır.</p>
      )}

      {message && (
        <p
          role="status"
          className={
            'mt-3 rounded-md px-3 py-2 text-sm ' +
            (message.isError ? 'bg-rose-50 text-rose-700' : 'bg-emerald-50 text-emerald-700')
          }
        >
          {message.text}
        </p>
      )}
    </div>
  );
}

export function DirectoryUserCard({ userId, onBack }: DirectoryUserCardProps) {
  const { data: user, isLoading } = useDirectoryUser(userId);

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Kullanıcı Kartı</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Kullanıcılara dön
        </button>
      </div>

      {isLoading || !user ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : (
        <div className="space-y-6">
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-semibold text-slate-800">
                {user.displayName ?? user.username}
              </h3>
              <span
                className={
                  'rounded-full px-2 py-0.5 text-xs font-medium ' +
                  (user.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500')
                }
              >
                {user.isActive ? 'Aktif' : 'Pasif'}
              </span>
            </div>
            <p className="mt-0.5 text-sm text-slate-500">{user.username}</p>
          </div>

          <div>
            <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
              Hesap
            </h4>
            <InfoRow label="Dizin" value={user.directoryName} />
            <InfoRow label="Kaynak" value={SOURCE_LABEL[user.source] ?? '—'} />
            <InfoRow label="Ad" value={user.firstName ?? '—'} />
            <InfoRow label="Soyad" value={user.lastName ?? '—'} />
            <InfoRow label="E-posta" value={user.email ?? '—'} />
            <InfoRow
              label="Son Senkron"
              value={user.lastSyncedUtc ? new Date(user.lastSyncedUtc).toLocaleString('tr-TR') : '—'}
            />
          </div>

          <div>
            <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
              Dizin Alanları
            </h4>
            {user.attributes.length === 0 ? (
              <p className="py-2 text-sm text-slate-400">
                Senkronize edilmiş alan yok. Alan Eşlemeleri bölümünden alan tanımlayıp dizini
                yeniden senkronize edin.
              </p>
            ) : (
              user.attributes.map((attribute) => (
                <InfoRow
                  key={attribute.adAttributeName}
                  label={attribute.systemFieldName}
                  value={attribute.value ?? '—'}
                />
              ))
            )}
          </div>

          {user.source === 0 && <ResetPasswordPanel userId={user.id} />}
        </div>
      )}
    </div>
  );
}
