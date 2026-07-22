import { ReportPage } from '../../../pages/ReportPage';

interface TimesheetTabProps {
  projectId: string;
}

/** "Gerçekleşen Efor" (ReportPage) sayfasının içeriğini AYNEN — aynı toolbar (ay gezinme,
 * Günlük/Haftalık/Aylık, MQL filtresi, Group By), özet kartlar, tablo lejantı, pivot tablo,
 * "+ Work Log Ekle"/"Onayla" ve hücre/onay modalları dahil — burada, sadece bu projeye
 * kilitlenmiş şekilde gösterir. ReportPage'in kendisi değişmez; sadece isteğe bağlı
 * `projectId` prop'uyla veri bu projeye filtrelenir (bkz. ReportPage.tsx). */
export function TimesheetTab({ projectId }: TimesheetTabProps) {
  return <ReportPage projectId={projectId} />;
}
