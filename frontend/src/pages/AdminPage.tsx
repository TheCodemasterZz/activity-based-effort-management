import { useEffect, useState } from 'react';
import { useEmployees } from '../hooks/useEmployees';
import { useNotifications } from '../hooks/useNotifications';
import { useValueStreams } from '../hooks/useValueStreams';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import { UserDirectorySection } from '../components/admin/directory/UserDirectorySection';
import { UsersSection } from '../components/admin/directory/UsersSection';
import { OrgChartSection } from '../components/admin/directory/OrgChartSection';
import { useConfidenceScoreSettings, useUpdateConfidenceScoreSettingsMutation } from '../hooks/useConfidenceScoreSettings';
import { WidgetsPage } from './WidgetsPage';
import { ApiError } from '../api/client';
import type { ConfidenceScoreSettingsDto } from '../api/types';

type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'users'
  | 'orgChart'
  | 'confidenceScore'
  | 'placeholder';

interface AdminSection {
  key: string;
  label: string;
  kind: SectionKind;
}

interface AdminGroup {
  header: string;
  sections: AdminSection[];
}

interface AdminTab {
  key: string;
  label: string;
  // groups yoksa (ör. Widgets) bu tab sol menü/bölüm yapısını atlar, kendi tam sayfa
  // içeriğini (fullPage) doğrudan ana alanda render eder.
  groups?: AdminGroup[];
  fullPage?: React.ComponentType;
}

const ADMIN_TABS: AdminTab[] = [
  {
    key: 'general',
    label: 'Genel',
    groups: [
      {
        header: 'GENEL AYARLAR',
        sections: [
          { key: 'company', label: 'Şirket Bilgileri', kind: 'placeholder' },
          { key: 'notifications', label: 'Bildirimler', kind: 'notifications' },
        ],
      },
    ],
  },
  {
    key: 'users',
    label: 'Kullanıcı Yönetimi',
    groups: [
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'users', label: 'Kullanıcılar', kind: 'users' },
          { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
      {
        header: 'ACTIVE DIRECTORY',
        sections: [
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
        ],
      },
    ],
  },
  {
    key: 'system',
    label: 'Sistem',
    groups: [
      {
        header: 'KATALOG',
        sections: [
          { key: 'valueStreams', label: "Value Stream'ler", kind: 'valueStreams' },
          { key: 'activities', label: 'Aktivite Kataloğu', kind: 'activities' },
        ],
      },
      {
        header: 'TAKVİM',
        sections: [
          { key: 'holidays', label: 'Resmi Tatiller', kind: 'holidays' },
          { key: 'workCalendars', label: 'Mesai Takvimleri', kind: 'workCalendars' },
        ],
      },
      {
        header: 'GÜVENİLİRLİK SKORU',
        sections: [
          { key: 'confidenceScore', label: 'Güvenilirlik Skoru Ayarları', kind: 'confidenceScore' },
        ],
      },
    ],
  },
  {
    key: 'widgets',
    label: 'Widgets',
    fullPage: WidgetsPage,
  },
];

function Placeholder({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-200 py-16 text-center">
      <div className="mb-2 text-3xl">🚧</div>
      <p className="text-sm font-medium text-slate-500">{label} bölümü yakında eklenecek.</p>
    </div>
  );
}

function EmployeesSection() {
  const employees = useEmployees();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad Soyad</th>
          <th className="py-2 pr-4 font-medium">E-posta</th>
        </tr>
      </thead>
      <tbody>
        {employees.data?.items.map((e) => (
          <tr key={e.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{e.name}</td>
            <td className="py-2 pr-4 text-slate-500">{e.email ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function NotificationsSection() {
  const notifications = useNotifications();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Mesaj</th>
          <th className="py-2 pr-4 font-medium">Tarih</th>
          <th className="py-2 font-medium">Durum</th>
        </tr>
      </thead>
      <tbody>
        {notifications.data?.items.map((n) => (
          <tr key={n.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{n.message}</td>
            <td className="py-2 pr-4 text-slate-500">{new Date(n.createdAtUtc).toLocaleDateString('tr-TR')}</td>
            <td className="py-2">
              <span
                className={
                  'rounded-full px-2 py-0.5 text-xs font-medium ' +
                  (n.isRead ? 'bg-slate-100 text-slate-500' : 'bg-indigo-50 text-indigo-700')
                }
              >
                {n.isRead ? 'Okundu' : 'Okunmadı'}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ValueStreamsSection() {
  const valueStreams = useValueStreams();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad</th>
          <th className="py-2 font-medium">Açıklama</th>
        </tr>
      </thead>
      <tbody>
        {valueStreams.data?.items.map((v) => (
          <tr key={v.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{v.name}</td>
            <td className="py-2 text-slate-500">{v.description ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ActivitiesSection() {
  const activities = useAllActivities();
  const items = activities.data?.items ?? [];
  const topLevel = items.filter((a) => !a.parentActivityId);
  const subByParent = new Map<string, typeof items>();
  for (const a of items) {
    if (!a.parentActivityId) continue;
    const list = subByParent.get(a.parentActivityId) ?? [];
    list.push(a);
    subByParent.set(a.parentActivityId, list);
  }

  return (
    <div className="space-y-4">
      {topLevel.map((l1) => (
        <div key={l1.id}>
          <div className="text-sm font-semibold text-slate-700">{l1.name}</div>
          <ul className="mt-1 space-y-0.5 pl-4 text-sm text-slate-500">
            {(subByParent.get(l1.id) ?? []).map((l2) => (
              <li key={l2.id}>· {l2.name}</li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}

function HolidaysSection() {
  const holidays = useHolidays();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Tarih</th>
          <th className="py-2 font-medium">Ad</th>
        </tr>
      </thead>
      <tbody>
        {holidays.data?.items
          .slice()
          .sort((a, b) => a.date.localeCompare(b.date))
          .map((h) => (
            <tr key={h.id} className="border-b border-slate-50 last:border-0">
              <td className="py-2 pr-4 text-slate-700">
                {new Date(`${h.date}T00:00:00`).toLocaleDateString('tr-TR')}
              </td>
              <td className="py-2 text-slate-500">{h.name}</td>
            </tr>
          ))}
      </tbody>
    </table>
  );
}

function WorkCalendarsSection() {
  // Sistemde şu an için sabit iki mesai takvimi var (seed veride HasData ile tanımlı); ayrı bir
  // "tüm takvimleri listele" API uç noktası olmadığından bilgi amaçlı statik olarak gösteriliyor.
  const calendars = [
    { name: 'Standart Ofis Mesaisi', schedule: 'Pzt–Cum 09:00–18:00, Cmt–Paz kapalı' },
    { name: 'Esnek Vardiya', schedule: 'Pzt–Cum 09:00–17:00, Cmt 10:00–14:00, Paz kapalı' },
  ];
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad</th>
          <th className="py-2 font-medium">Program</th>
        </tr>
      </thead>
      <tbody>
        {calendars.map((c) => (
          <tr key={c.name} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{c.name}</td>
            <td className="py-2 text-slate-500">{c.schedule}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

type SettingsFormState = Omit<ConfidenceScoreSettingsDto, 'id'>;

function NumberField({
  label,
  value,
  onChange,
  step = 1,
  hint,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  step?: number;
  hint?: string;
}) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-slate-500">{label}</label>
      <input
        type="number"
        step={step}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-full rounded-lg border border-slate-200 px-3 py-1.5 text-sm"
      />
      {hint && <p className="mt-0.5 text-[11px] text-slate-400">{hint}</p>}
    </div>
  );
}

function FieldGroup({
  title,
  description,
  groupWeight,
  children,
}: {
  title: string;
  description: string;
  groupWeight?: { total: number; onChange: (newTotal: number) => void };
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-100 p-3">
      <div className="mb-1 flex items-start justify-between gap-3">
        <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">{title}</div>
        {groupWeight && (
          <div className="flex shrink-0 items-center gap-1.5">
            <label className="text-[11px] font-medium text-slate-400">Grup Ağırlığı</label>
            <input
              type="number"
              step={1}
              min={0}
              value={groupWeight.total}
              onChange={(e) => groupWeight.onChange(Number(e.target.value))}
              className="w-16 rounded border border-slate-200 px-1.5 py-0.5 text-right text-xs font-semibold text-slate-700"
            />
          </div>
        )}
      </div>
      <p className="mb-2 text-[11px] leading-snug text-slate-400">{description}</p>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">{children}</div>
    </div>
  );
}

/** Bir grubun içindeki bireysel ağırlıkları, aralarındaki ORANI koruyarak yeni bir grup
 * toplamına göre yeniden dağıtır (ör. grup toplamı 30'dan 40'a çıkarılırsa, 3 alt ağırlık kendi
 * aralarındaki oranı koruyarak büyür). Yuvarlama artığı en büyük değere eklenir ki toplam her
 * zaman tam olarak `newTotal`a eşit kalsın. */
function rescaleGroupWeights(currentValues: number[], newTotal: number): number[] {
  const safeTotal = Math.max(0, Math.round(newTotal));
  const currentSum = currentValues.reduce((a, b) => a + b, 0);

  if (currentSum <= 0) {
    const base = Math.floor(safeTotal / currentValues.length);
    const remainder = safeTotal - base * currentValues.length;
    return currentValues.map((_, i) => base + (i < remainder ? 1 : 0));
  }

  const rounded = currentValues.map((v) => Math.round((v / currentSum) * safeTotal));
  const diff = safeTotal - rounded.reduce((a, b) => a + b, 0);
  if (diff !== 0) {
    let maxIndex = 0;
    for (let i = 1; i < rounded.length; i++) {
      if (rounded[i] > rounded[maxIndex]) maxIndex = i;
    }
    rounded[maxIndex] = Math.max(0, rounded[maxIndex] + diff);
  }
  return rounded;
}

/** Güvenilirlik skoru motorunun (bkz. lib/confidenceScore.ts) tüm ağırlık/eşik/parametre
 * ayarları — Log Work ekleme ve Efor Onayı ekranlarındaki rozetlerin dayandığı tek kaynak. */
function ConfidenceScoreSettingsSection() {
  const settingsQuery = useConfidenceScoreSettings();
  const updateMutation = useUpdateConfidenceScoreSettingsMutation();
  const [form, setForm] = useState<SettingsFormState | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [savedAt, setSavedAt] = useState<number | null>(null);

  useEffect(() => {
    if (settingsQuery.data) setForm(settingsQuery.data);
  }, [settingsQuery.data]);

  const set = <K extends keyof SettingsFormState>(key: K, value: SettingsFormState[K]) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev));

  const setGroupWeights = (keys: Array<keyof SettingsFormState>, newTotal: number) =>
    setForm((prev) => {
      if (!prev) return prev;
      const currentValues = keys.map((k) => prev[k] as number);
      const rescaled = rescaleGroupWeights(currentValues, newTotal);
      const next = { ...prev } as unknown as Record<string, number>;
      keys.forEach((k, i) => {
        next[k as string] = rescaled[i];
      });
      return next as unknown as SettingsFormState;
    });

  if (settingsQuery.isLoading || !form) {
    return <p className="text-sm text-slate-400">Yükleniyor…</p>;
  }

  const totalWeight =
    form.weightDescriptionLength +
    form.weightSpecificity +
    form.weightGenericPenalty +
    form.weightDuplicateDetection +
    form.weightRoundHoursSingle +
    form.weightDurationDescriptionRatio +
    form.weightDailyRoundTotal +
    form.weightDailyTotalReasonableness +
    form.weightBaselineDeviation +
    form.weightWeekendHoliday;

  const handleSave = async () => {
    setErrorMessage(null);
    setSavedAt(null);
    try {
      await updateMutation.mutateAsync(form);
      setSavedAt(Date.now());
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  return (
    <div className="space-y-4">
      <p className="text-xs text-slate-400">
        Skor, kayıtları veritabanında SAKLAMAZ — Log Work ve Efor Onayı ekranları her görüntülemede
        bu ayarlarla anlık olarak hesaplar. Ağırlıkların toplamı 100 olmak zorunda değil (motor
        otomatik oranlar), ama karşılaştırılabilirlik için 100'e yakın tutulması önerilir.
        {' '}
        <span className={totalWeight === 100 ? 'font-semibold text-emerald-600' : 'font-semibold text-amber-600'}>
          Şu an toplam: {totalWeight}
        </span>
      </p>

      <FieldGroup
        title="A. Açıklama Kalitesi"
        description="Girilen açıklama metninin ne kadar bilgilendirici olduğunu ölçer: yeterince uzun ve detaylı mı (A1), proje/aktiviteye özgü somut kelimeler içeriyor mu (A2), 'genel işler', 'toplantı' gibi jenerik/boş ifadelerden mi ibaret (A3 — ceza)."
        groupWeight={{
          total: form.weightDescriptionLength + form.weightSpecificity + form.weightGenericPenalty,
          onChange: (v) =>
            setGroupWeights(['weightDescriptionLength', 'weightSpecificity', 'weightGenericPenalty'], v),
        }}
      >
        <NumberField label="Uzunluk & Detay (A1)" value={form.weightDescriptionLength} onChange={(v) => set('weightDescriptionLength', v)} />
        <NumberField label="Spesifiklik (A2)" value={form.weightSpecificity} onChange={(v) => set('weightSpecificity', v)} />
        <NumberField label="Jenerik İfade Cezası (A3)" value={form.weightGenericPenalty} onChange={(v) => set('weightGenericPenalty', v)} />
      </FieldGroup>

      <FieldGroup
        title="B. Orijinallik"
        description="Aynı çalışanın son günlerdeki diğer kayıtlarıyla neredeyse birebir aynı (kopyala-yapıştır) açıklama girilip girilmediğini, kelime benzerliğine bakarak tespit eder (B1)."
        groupWeight={{
          total: form.weightDuplicateDetection,
          onChange: (v) => setGroupWeights(['weightDuplicateDetection'], v),
        }}
      >
        <NumberField label="Tekrar Tespiti (B1)" value={form.weightDuplicateDetection} onChange={(v) => set('weightDuplicateDetection', v)} />
      </FieldGroup>

      <FieldGroup
        title="C. Süre Sinyalleri"
        description="Girilen saatlerin doğallığını değerlendirir: tek bir kayıt hep tam/yuvarlak saatler mi (C1 — ör. hep 1h, 2h), süre ile açıklama uzunluğu birbiriyle orantısız mı (C2 — çok kısa açıklamayla çok uzun süre vb.), aynı günün toplamı hep yuvarlak mı çıkıyor (C3)."
        groupWeight={{
          total: form.weightRoundHoursSingle + form.weightDurationDescriptionRatio + form.weightDailyRoundTotal,
          onChange: (v) =>
            setGroupWeights(
              ['weightRoundHoursSingle', 'weightDurationDescriptionRatio', 'weightDailyRoundTotal'],
              v,
            ),
        }}
      >
        <NumberField label="Yuvarlak Süre — Tekil (C1)" value={form.weightRoundHoursSingle} onChange={(v) => set('weightRoundHoursSingle', v)} />
        <NumberField label="Süre-Açıklama Orantısı (C2)" value={form.weightDurationDescriptionRatio} onChange={(v) => set('weightDurationDescriptionRatio', v)} />
        <NumberField label="Günlük Toplam Yuvarlaklık (C3)" value={form.weightDailyRoundTotal} onChange={(v) => set('weightDailyRoundTotal', v)} />
      </FieldGroup>

      <FieldGroup
        title="D. Günlük Makullük"
        description="Bir çalışanın bir gün içinde girdiği toplam saatin makul bir sınırı aşıp aşmadığını kontrol eder (D1 — ör. tek günde şüpheli derecede yüksek toplam saat)."
        groupWeight={{
          total: form.weightDailyTotalReasonableness,
          onChange: (v) => setGroupWeights(['weightDailyTotalReasonableness'], v),
        }}
      >
        <NumberField label="Günlük Toplam Saat (D1)" value={form.weightDailyTotalReasonableness} onChange={(v) => set('weightDailyTotalReasonableness', v)} />
      </FieldGroup>

      <FieldGroup
        title="E. Kişi Bazlı Baseline"
        description="Kaydı, aynı çalışanın kendi geçmiş ortalama davranışıyla (kişisel baseline) karşılaştırır — çalışanın kendi alışkanlığından belirgin şekilde sapan kayıtları işaretler (E1). Yeterli geçmiş kaydı (3'ten az) olan çalışanlar için nötr/tam puan verilir."
        groupWeight={{
          total: form.weightBaselineDeviation,
          onChange: (v) => setGroupWeights(['weightBaselineDeviation'], v),
        }}
      >
        <NumberField label="Baseline Sapması (E1)" value={form.weightBaselineDeviation} onChange={(v) => set('weightBaselineDeviation', v)} />
      </FieldGroup>

      <FieldGroup
        title="F. Zamanlama"
        description="Kaydın hafta sonuna veya resmi tatile denk gelip gelmediğini kontrol eder — bu, mesai dışı bir günde girilen efora dikkat çekmek için ayrı bir puanlanan kriterdir (F1)."
        groupWeight={{
          total: form.weightWeekendHoliday,
          onChange: (v) => setGroupWeights(['weightWeekendHoliday'], v),
        }}
      >
        <NumberField label="Hafta Sonu/Resmi Tatil (F1)" value={form.weightWeekendHoliday} onChange={(v) => set('weightWeekendHoliday', v)} />
      </FieldGroup>

      <FieldGroup
        title="5'li Skala Eşikleri (0-100)"
        description="Toplam 0-100 puanın hangi aralıkların hangi güven etiketine (Çok Düşük/Düşük/Orta/Yüksek/Çok Yüksek) karşılık geldiğini belirler. Her eşik, bir üstteki etiketin başladığı puanı ifade eder."
      >
        <NumberField label="Çok Düşük <" value={form.thresholdVeryLow} onChange={(v) => set('thresholdVeryLow', v)} />
        <NumberField label="Düşük <" value={form.thresholdLow} onChange={(v) => set('thresholdLow', v)} />
        <NumberField label="Orta <" value={form.thresholdMedium} onChange={(v) => set('thresholdMedium', v)} />
        <NumberField label="Yüksek < (üstü Çok Yüksek)" value={form.thresholdHigh} onChange={(v) => set('thresholdHigh', v)} />
      </FieldGroup>

      <FieldGroup
        title="Sinyal Parametreleri"
        description="Yukarıdaki A-F sinyallerinin hesaplamalarında kullanılan ham eşik/aralık değerleri (ör. kaç gün geriye bakılacak, kaç karakterden kısa açıklama 'kısa' sayılacak, hangi benzerlik oranı 'tekrar' sayılacak)."
      >
        <NumberField label="Baseline Bakış Aralığı (gün)" value={form.baselineLookbackDays} onChange={(v) => set('baselineLookbackDays', v)} />
        <NumberField label="Tekrar Bakış Aralığı (gün)" value={form.duplicateLookbackDays} onChange={(v) => set('duplicateLookbackDays', v)} />
        <NumberField label="Tekrar Benzerlik Eşiği (0-1)" value={form.duplicateSimilarityThreshold} step={0.05} onChange={(v) => set('duplicateSimilarityThreshold', v)} />
        <NumberField label="Kısa Açıklama Eşiği (karakter)" value={form.shortDescriptionCharThreshold} onChange={(v) => set('shortDescriptionCharThreshold', v)} />
        <NumberField label="Uzun Açıklama Eşiği (karakter)" value={form.longDescriptionCharThreshold} onChange={(v) => set('longDescriptionCharThreshold', v)} />
        <NumberField label="Uzun Süre Eşiği (saat)" value={form.longDurationHoursThreshold} step={0.5} onChange={(v) => set('longDurationHoursThreshold', v)} />
        <NumberField label="Kısa Süre Eşiği (saat)" value={form.shortDurationHoursThreshold} step={0.25} onChange={(v) => set('shortDurationHoursThreshold', v)} />
        <NumberField label="Şüpheli Günlük Toplam (saat)" value={form.dailyTotalSuspiciousHours} step={0.5} onChange={(v) => set('dailyTotalSuspiciousHours', v)} />
      </FieldGroup>

      <div>
        <label className="mb-1 block text-xs font-medium text-slate-500">
          Jenerik/Boilerplate İfadeler (virgülle ayrılmış)
        </label>
        <p className="mb-1 text-[11px] leading-snug text-slate-400">
          Açıklama metninde bu ifadelerden biri (veya tamamı) geçiyorsa A3 sinyali ceza uygular —
          ör. "genel işler", "toplantı" gibi hiçbir şey anlatmayan kalıp ifadeler.
        </p>
        <textarea
          value={form.genericPhrasesCsv}
          onChange={(e) => set('genericPhrasesCsv', e.target.value)}
          rows={3}
          className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
        />
      </div>

      {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}
      {savedAt && <p className="text-sm text-emerald-600">Kaydedildi.</p>}

      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleSave}
          disabled={updateMutation.isPending}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {updateMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
      </div>
    </div>
  );
}

function SectionContent({ section }: { section: AdminSection }) {
  switch (section.kind) {
    case 'employees':
      return <EmployeesSection />;
    case 'notifications':
      return <NotificationsSection />;
    case 'valueStreams':
      return <ValueStreamsSection />;
    case 'activities':
      return <ActivitiesSection />;
    case 'holidays':
      return <HolidaysSection />;
    case 'workCalendars':
      return <WorkCalendarsSection />;
    case 'userDirectory':
      return <UserDirectorySection />;
    case 'users':
      return <UsersSection />;
    case 'orgChart':
      return <OrgChartSection />;
    case 'confidenceScore':
      return <ConfidenceScoreSettingsSection />;
    case 'placeholder':
      return <Placeholder label={section.label} />;
  }
}

/**
 * Jira'nın Administration ekranındaki yapıya (üst kategori sekmeleri + sol tarafta başlıklarla
 * gruplanmış alt bölümler + sağda seçili bölümün içerik/tablo alanı) benzer şekilde tasarlanmış
 * admin sayfası — header'daki ⚙️ ikonu buraya yönlendirir. Backend'de zaten karşılığı olan
 * bölümler (Çalışanlar, Bildirimler, Value Stream'ler, Aktiviteler, Resmi Tatiller) gerçek veriyle
 * dolduruldu; henüz karşılığı olmayanlar (Şirket Bilgileri, Roller) "yakında" ile işaretlendi.
 */
export function AdminPage() {
  const [activeTabKey, setActiveTabKey] = useState(ADMIN_TABS[0].key);
  const activeTab = ADMIN_TABS.find((t) => t.key === activeTabKey) ?? ADMIN_TABS[0];

  const [activeSectionKey, setActiveSectionKey] = useState(activeTab.groups?.[0]?.sections[0]?.key ?? '');
  const activeSection = activeTab.groups
    ? activeTab.groups.flatMap((g) => g.sections).find((s) => s.key === activeSectionKey) ??
      activeTab.groups[0].sections[0]
    : null;

  const selectTab = (tabKey: string) => {
    const tab = ADMIN_TABS.find((t) => t.key === tabKey);
    if (!tab) return;
    setActiveTabKey(tabKey);
    setActiveSectionKey(tab.groups?.[0]?.sections[0]?.key ?? '');
  };

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-slate-200 bg-white px-6">
        <h1 className="pt-4 text-lg font-semibold text-slate-800">Mesainame Yönetim Paneli</h1>
        <nav className="mt-3 flex gap-1">
          {ADMIN_TABS.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => selectTab(tab.key)}
              className={
                'border-b-2 px-3 py-2 text-sm font-medium ' +
                (tab.key === activeTabKey
                  ? 'border-indigo-600 text-indigo-700'
                  : 'border-transparent text-slate-500 hover:text-slate-700')
              }
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {activeTab.fullPage ? (
        <div className="flex-1 overflow-y-auto">
          <activeTab.fullPage />
        </div>
      ) : (
        <div className="flex flex-1 overflow-hidden">
          <aside className="w-64 shrink-0 overflow-y-auto border-r border-slate-200 bg-slate-50 p-4">
            {activeTab.groups?.map((group) => (
              <div key={group.header} className="mb-5">
                <div className="mb-1.5 px-2 text-xs font-semibold tracking-wide text-slate-400">{group.header}</div>
                <div className="space-y-0.5">
                  {group.sections.map((section) => (
                    <button
                      key={section.key}
                      type="button"
                      onClick={() => setActiveSectionKey(section.key)}
                      className={
                        'block w-full rounded-md px-2 py-1.5 text-left text-sm ' +
                        (section.key === activeSectionKey
                          ? 'bg-indigo-100 font-medium text-indigo-700'
                          : 'text-slate-600 hover:bg-slate-100')
                      }
                    >
                      {section.label}
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </aside>

          <main className="flex-1 overflow-y-auto p-6">
            {activeSection && (
              <>
                <h2 className="mb-4 text-base font-semibold text-slate-800">{activeSection.label}</h2>
                <div className="rounded-xl border border-slate-200 bg-white p-4">
                  <SectionContent section={activeSection} />
                </div>
              </>
            )}
          </main>
        </div>
      )}
    </div>
  );
}
