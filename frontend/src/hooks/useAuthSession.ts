import { useSyncExternalStore } from 'react';
import { getSession, subscribeToAuth, type AuthSession } from '../lib/auth';

export function useAuthSession(): AuthSession | null {
  return useSyncExternalStore(subscribeToAuth, getSession, () => null);
}
