import { useSyncExternalStore } from 'react';
import { dismissNotification, getNotifications, subscribeToNotifications } from '../../lib/notifications';

export function NotificationHost() {
  const notifications = useSyncExternalStore(subscribeToNotifications, getNotifications, getNotifications);

  if (notifications.length === 0) return null;

  return (
    <div className="pointer-events-none fixed right-4 top-4 z-[100] flex w-full max-w-sm flex-col gap-2">
      {notifications.map((notification) => (
        <div
          key={notification.id}
          role="alert"
          className="pointer-events-auto flex items-start gap-3 rounded-lg border border-red-200 bg-red-50 p-3 shadow-lg"
        >
          <span className="mt-0.5 text-red-500">⚠</span>
          <p className="flex-1 text-sm text-red-700">{notification.message}</p>
          <button
            type="button"
            onClick={() => dismissNotification(notification.id)}
            aria-label="Bildirimi kapat"
            className="text-red-400 hover:text-red-600"
          >
            ✕
          </button>
        </div>
      ))}
    </div>
  );
}
