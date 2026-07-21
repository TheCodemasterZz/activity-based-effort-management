import { useEffect, useRef, useState } from 'react';
import { useNotifications } from '../../hooks/useNotifications';
import { useMarkNotificationReadMutation } from '../../hooks/useMarkNotificationReadMutation';

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
}

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const notifications = useNotifications();
  const markRead = useMarkNotificationReadMutation();

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

  const items = notifications.data?.items ?? [];
  const unreadCount = items.filter((n) => !n.isRead).length;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        className="relative text-slate-400 hover:text-slate-600"
        aria-label="Bildirimler"
      >
        🔔
        {unreadCount > 0 && (
          <span className="absolute -right-1.5 -top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-80 rounded-lg border border-slate-200 bg-white p-2 shadow-lg">
          <div className="mb-1 px-2 py-1 text-sm font-semibold text-slate-700">Bildirimler</div>

          {items.length === 0 && (
            <div className="px-2 py-4 text-center text-sm text-slate-400">Bildirim yok</div>
          )}

          <div className="max-h-80 space-y-1 overflow-y-auto">
            {items.map((notification) => (
              <div
                key={notification.id}
                className={`flex items-start justify-between gap-2 rounded-lg px-2 py-2 text-sm ${
                  notification.isRead ? 'text-slate-400' : 'bg-indigo-50/60 text-slate-700'
                }`}
              >
                <div>
                  <div>{notification.message}</div>
                  <div className="mt-0.5 text-xs text-slate-400">{formatDateTime(notification.createdAtUtc)}</div>
                </div>
                {!notification.isRead && (
                  <button
                    type="button"
                    onClick={() => markRead.mutate(notification.id)}
                    className="shrink-0 text-slate-400 hover:text-red-600"
                    aria-label="Okundu olarak işaretle"
                  >
                    ✕
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
