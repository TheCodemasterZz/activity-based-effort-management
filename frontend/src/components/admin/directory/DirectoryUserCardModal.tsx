import { DirectoryUserCard } from './DirectoryUserCard';

interface DirectoryUserCardModalProps {
  userId: string;
  onClose: () => void;
  onSelectUser: (userId: string) => void;
}

/**
 * Organizasyon şemasındaki bir düğüme tıklanınca kullanıcı kartını sayfadan ayrılmadan gösterir.
 * Modal içindeki "Yönetici" referansına tıklanınca `onSelectUser` ile içerik değişir, modal kapanmaz.
 */
export function DirectoryUserCardModal({ userId, onClose, onSelectUser }: DirectoryUserCardModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-2 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600"
            aria-label="Kapat"
          >
            ✕
          </button>
        </div>
        <DirectoryUserCard userId={userId} onSelectUser={onSelectUser} />
      </div>
    </div>
  );
}
