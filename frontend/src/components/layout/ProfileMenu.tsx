import { useEffect, useRef, useState } from 'react';

const MENU_ITEMS = ['Profili Düzenle', 'Dil Değiştir', 'Çıkış'];

/** Sadece UI amaçlı profil menüsü — herhangi bir gerçek işlevi yok, sadece açılıp kapanır. */
export function ProfileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

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

  return (
    <div ref={containerRef} className="relative">
      <button type="button" onClick={() => setIsOpen((v) => !v)} className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700">
          BK
        </div>
        <div className="text-left">
          <div className="text-sm font-medium text-slate-800">Barış Kalaycıoğlu</div>
          <div className="text-xs text-slate-400">Solution Architect</div>
        </div>
        <span className="text-slate-300">▾</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-48 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">
          {MENU_ITEMS.map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => setIsOpen(false)}
              className="block w-full rounded-md px-3 py-2 text-left text-sm text-slate-600 hover:bg-slate-50"
            >
              {item}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
