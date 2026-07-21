import { useDirectoryUser } from '../../../hooks/useDirectoryUsers';

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
        </div>
      )}
    </div>
  );
}
