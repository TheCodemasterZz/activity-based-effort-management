export type AppPage = 'home' | 'planWork' | 'workLog' | 'projects';

export const NAV_ITEMS: { page: AppPage; label: string }[] = [
  { page: 'home', label: 'Ana Sayfa' },
  { page: 'planWork', label: 'Plan Work' },
  { page: 'workLog', label: 'Work Log' },
  { page: 'projects', label: 'Projeler' },
];
