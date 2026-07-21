// 'admin' bilinçli olarak NAV_ITEMS'e eklenmiyor — Jira'daki gibi ana navigasyonun dışında,
// sadece header'daki ⚙️ ikonundan erişilebilir bir sayfa.
export type AppPage =
  | 'home'
  | 'planWork'
  | 'workLog'
  | 'capacityManagement'
  | 'planningAccuracy'
  | 'projects'
  | 'employees'
  | 'widgets'
  | 'admin';

export const NAV_ITEMS: { page: AppPage; label: string }[] = [
  { page: 'home', label: 'Ana Sayfa' },
  { page: 'planWork', label: 'Planlanan Efor' },
  { page: 'workLog', label: 'Gerçekleşen Efor' },
  { page: 'capacityManagement', label: 'Kapasite Yönetimi' },
  { page: 'planningAccuracy', label: 'Planlama Doğruluğu' },
  { page: 'projects', label: 'Projeler' },
  { page: 'employees', label: 'Çalışanlar' },
  { page: 'widgets', label: 'Widgets' },
];
