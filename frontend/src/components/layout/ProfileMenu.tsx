import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthSession } from '../../hooks/useAuthSession';
import { clearSession } from '../../lib/auth';

/** Görünen addan baş harfleri üretir: "Serkan Gültepe" → "SG". */
function toInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export function ProfileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const session = useAuthSession();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  if (!session) return null;

  const displayName = session.displayName?.trim() || session.username;

  const handleLogout = () => {
    setIsOpen(false);
    // Önbellek temizlenmezse sonraki kullanıcı öncekinin verisini görebilir.
    queryClient.clear();
    clearSession();
  };

  return (
    <div ref={containerRef} className="relative">
      <button type="button" onClick={() => setIsOpen((v) => !v)} className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700">
          {toInitials(displayName)}
        </div>
        <div className="text-left">
          <div className="text-sm font-medium text-slate-800">{displayName}</div>
          <div className="text-xs text-slate-400">{session.username}</div>
        </div>
        <span className="text-slate-300">▾</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-48 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">
          <button
            type="button"
            onClick={handleLogout}
            className="block w-full rounded-md px-3 py-2 text-left text-sm text-slate-600 hover:bg-slate-50"
          >
            Çıkış
          </button>
        </div>
      )}
    </div>
  );
}
