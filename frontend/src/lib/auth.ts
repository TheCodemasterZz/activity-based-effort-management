export interface AuthSession {
  token: string;
  expiresAtUtc: string;
  userId: string;
  username: string;
  displayName: string | null;
  source: number;
}

const STORAGE_KEY = 'mesainame.auth';

type Listener = () => void;
const listeners = new Set<Listener>();

let cachedSession: AuthSession | null | undefined;

function emit() {
  listeners.forEach((listener) => listener());
}

function readFromStorage(): AuthSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    // Bozuk kayıt oturumsuz sayılır; kullanıcı yeniden giriş yapar.
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

function isExpired(session: AuthSession): boolean {
  return new Date(session.expiresAtUtc).getTime() <= Date.now();
}

export function getSession(): AuthSession | null {
  if (cachedSession === undefined) {
    cachedSession = readFromStorage();
  }

  if (cachedSession && isExpired(cachedSession)) {
    clearSession();
    return null;
  }

  return cachedSession;
}

export function setSession(session: AuthSession): void {
  cachedSession = session;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  emit();
}

export function clearSession(): void {
  cachedSession = null;
  localStorage.removeItem(STORAGE_KEY);
  emit();
}

export function subscribeToAuth(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
