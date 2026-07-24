import type { UserDto } from '../api/types';

/** Kullanıcının ekranda gösterilecek adı — displayName boşsa username'e düşer. */
export function userDisplayName(user: Pick<UserDto, 'displayName' | 'username'>): string {
  return user.displayName?.trim() || user.username;
}
