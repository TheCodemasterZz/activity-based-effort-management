import { ApiError } from '../api/client';

export interface AppNotification {
  id: string;
  message: string;
}

type Listener = () => void;

let notifications: AppNotification[] = [];
const listeners = new Set<Listener>();

const AUTO_DISMISS_MS = 8000;

function emit() {
  listeners.forEach((listener) => listener());
}

export function getNotifications(): AppNotification[] {
  return notifications;
}

export function subscribeToNotifications(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function dismissNotification(id: string): void {
  notifications = notifications.filter((n) => n.id !== id);
  emit();
}

export function pushErrorNotification(message: string): void {
  // Sunucu tamamen kapandığında sayfadaki birden çok paralel query aynı anda başarısız
  // olup aynı mesajı tetikleyebilir — aynı metne sahip aktif bir bildirim varsa tekrar eklenmez.
  if (notifications.some((n) => n.message === message)) return;

  const id = crypto.randomUUID();
  notifications = [...notifications, { id, message }];
  emit();
  setTimeout(() => dismissNotification(id), AUTO_DISMISS_MS);
}

/** Bilinmeyen bir hatayı kullanıcıya gösterilecek anlaşılır bir Türkçe mesaja çevirir. */
export function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof TypeError) {
    return 'Sunucudan yanıt alınamadı. Bağlantınızı kontrol edip tekrar deneyin.';
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'Beklenmeyen bir hata oluştu.';
}
