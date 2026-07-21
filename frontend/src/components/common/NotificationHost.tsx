import { useSyncExternalStore } from 'react';
import { dismissNotification, getNotifications, subscribeToNotifications } from '../../lib/notifications';

const STYLES = {
  error: {
    container: 'border-red-200 bg-red-50',
    icon: 'text-red-500',
    text: 'text-red-700',
    close: 'text-red-400 hover:text-red-600',
    symbol: '⚠',
  },
  success: {
    container: 'border-emerald-200 bg-emerald-50',
    icon: 'text-emerald-500',
    text: 'text-emerald-700',
    close: 'text-emerald-400 hover:text-emerald-600',
    symbol: '✓',
  },
} as const;

export function NotificationHost() {
  const notifications = useSyncExternalStore(subscribeToNotifications, getNotifications, getNotifications);

  if (notifications.length === 0) return null;

  return (
    <div className="pointer-events-none fixed right-4 top-4 z-[100] flex w-full max-w-sm flex-col gap-2">
      {notifications.map((notification) => {
        const style = STYLES[notification.type];
        return (
          <div
            key={notification.id}
            role="alert"
            className={`pointer-events-auto flex items-start gap-3 rounded-lg border p-3 shadow-lg ${style.container}`}
          >
            <span className={`mt-0.5 ${style.icon}`}>{style.symbol}</span>
            <p className={`flex-1 text-sm ${style.text}`}>{notification.message}</p>
            <button
              type="button"
              onClick={() => dismissNotification(notification.id)}
              aria-label="Bildirimi kapat"
              className={style.close}
            >
              ✕
            </button>
          </div>
        );
      })}
    </div>
  );
}
