import { useState, type FormEvent, type ReactNode } from 'react';
import { ApiError } from '../../../api/client';
import {
  useCreateDirectoryMutation,
  useTestDirectoryConnectionMutation,
  useUpdateDirectoryMutation,
} from '../../../hooks/useDirectoryMutations';
import type { DirectoryDto } from '../../../api/types';

interface DirectoryFormProps {
  directory: DirectoryDto | null;
  onClose: () => void;
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="border-t border-slate-100 pt-5 first:border-0 first:pt-0">
      <h3 className="mb-3 text-sm font-semibold text-slate-800">{title}</h3>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">{children}</div>
    </section>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-slate-400">{hint}</span>}
    </label>
  );
}

const inputClass =
  'w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

export function DirectoryForm({ directory, onClose }: DirectoryFormProps) {
  const isEdit = directory !== null;

  const [name, setName] = useState(directory?.name ?? '');
  const [source, setSource] = useState(directory?.source ?? 1);
  const [directoryType, setDirectoryType] = useState(
    directory?.directoryType ?? 'Microsoft Active Directory',
  );
  const [hostname, setHostname] = useState(directory?.hostname ?? '');
  const [port, setPort] = useState(String(directory?.port ?? 389));
  const [useSsl, setUseSsl] = useState(directory?.useSsl ?? false);
  const [bindUsername, setBindUsername] = useState(directory?.bindUsername ?? '');
  const [bindPassword, setBindPassword] = useState('');
  const [baseDn, setBaseDn] = useState(directory?.baseDn ?? '');
  const [additionalUserDn, setAdditionalUserDn] = useState(directory?.additionalUserDn ?? '');
  const [additionalGroupDn, setAdditionalGroupDn] = useState(directory?.additionalGroupDn ?? '');
  const [permission, setPermission] = useState(directory?.permission ?? 0);
  const [userObjectClass, setUserObjectClass] = useState(directory?.userObjectClass ?? 'user');
  const [userObjectFilter, setUserObjectFilter] = useState(
    directory?.userObjectFilter ?? '(&(objectCategory=Person)(sAMAccountName=*))',
  );
  const [usernameAttribute, setUsernameAttribute] = useState(
    directory?.usernameAttribute ?? 'sAMAccountName',
  );
  const [usernameRdnAttribute, setUsernameRdnAttribute] = useState(
    directory?.usernameRdnAttribute ?? 'cn',
  );
  const [firstNameAttribute, setFirstNameAttribute] = useState(
    directory?.firstNameAttribute ?? 'givenName',
  );
  const [lastNameAttribute, setLastNameAttribute] = useState(directory?.lastNameAttribute ?? 'sn');
  const [displayNameAttribute, setDisplayNameAttribute] = useState(
    directory?.displayNameAttribute ?? 'displayName',
  );
  const [emailAttribute, setEmailAttribute] = useState(directory?.emailAttribute ?? 'mail');
  const [uniqueIdAttribute, setUniqueIdAttribute] = useState(
    directory?.uniqueIdAttribute ?? 'objectGUID',
  );
  const [syncSchedule, setSyncSchedule] = useState(directory?.syncSchedule ?? 0);

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<{ text: string; isError: boolean } | null>(null);

  const createMutation = useCreateDirectoryMutation();
  const updateMutation = useUpdateDirectoryMutation();
  const testMutation = useTestDirectoryConnectionMutation();

  const isPending = createMutation.isPending || updateMutation.isPending;
  const isActiveDirectory = source === 1;

  const buildPayload = () => ({
    name: name.trim(),
    source,
    directoryType: isActiveDirectory ? directoryType.trim() : null,
    hostname: isActiveDirectory ? hostname.trim() : null,
    port: isActiveDirectory ? Number(port) || 0 : 0,
    useSsl: isActiveDirectory ? useSsl : false,
    bindUsername: isActiveDirectory ? bindUsername.trim() : null,
    bindPassword: bindPassword.length > 0 ? bindPassword : null,
    baseDn: isActiveDirectory ? baseDn.trim() : null,
    additionalUserDn: isActiveDirectory ? additionalUserDn.trim() || null : null,
    additionalGroupDn: isActiveDirectory ? additionalGroupDn.trim() || null : null,
    permission,
    userObjectClass: isActiveDirectory ? userObjectClass.trim() : null,
    userObjectFilter: isActiveDirectory ? userObjectFilter.trim() : null,
    usernameAttribute: isActiveDirectory ? usernameAttribute.trim() : null,
    usernameRdnAttribute: isActiveDirectory ? usernameRdnAttribute.trim() : null,
    firstNameAttribute: isActiveDirectory ? firstNameAttribute.trim() : null,
    lastNameAttribute: isActiveDirectory ? lastNameAttribute.trim() : null,
    displayNameAttribute: isActiveDirectory ? displayNameAttribute.trim() : null,
    emailAttribute: isActiveDirectory ? emailAttribute.trim() : null,
    uniqueIdAttribute: isActiveDirectory ? uniqueIdAttribute.trim() : null,
    syncSchedule: isActiveDirectory ? syncSchedule : 0,
    sortOrder: directory?.sortOrder ?? 0,
  });

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);

    try {
      if (isEdit && directory) {
        await updateMutation.mutateAsync({ id: directory.id, payload: buildPayload() });
      } else {
        await createMutation.mutateAsync(buildPayload());
      }
      onClose();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Dizin kaydedilemedi.');
    }
  };

  const handleTestConnection = async () => {
    if (!directory) return;
    setTestResult(null);
    try {
      const result = await testMutation.mutateAsync(directory.id);
      setTestResult({ text: result.message, isError: !result.success });
    } catch (error) {
      setTestResult({
        text: error instanceof ApiError ? error.message : 'Bağlantı test edilemedi.',
        isError: true,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">
          {isEdit ? 'Dizini Düzenle' : 'Yeni Dizin'}
        </h2>
        <button
          type="button"
          onClick={onClose}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Listeye dön
        </button>
      </div>

      <Section title="Sunucu Ayarları">
        <Field label="Ad">
          <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
        </Field>
        <Field label="Dizin Kaynağı">
          <select
            value={source}
            onChange={(e) => setSource(Number(e.target.value))}
            disabled={isEdit}
            className={inputClass}
          >
            <option value={1}>Active Directory</option>
            <option value={0}>Internal</option>
          </select>
        </Field>

        {isActiveDirectory && (
          <>
            <Field label="Dizin Tipi">
              <input
                value={directoryType}
                onChange={(e) => setDirectoryType(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Sunucu Adresi" hint="LDAP sunucusunun adresi. Örnek: kizilay.local">
              <input
                value={hostname}
                onChange={(e) => setHostname(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Port">
              <input
                type="number"
                value={port}
                onChange={(e) => setPort(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="SSL Kullan"
              hint="Üretimde şifrenin ağda düz metin gitmemesi için açık olmalıdır."
            >
              <label className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  checked={useSsl}
                  onChange={(e) => setUseSsl(e.target.checked)}
                  className="h-4 w-4"
                />
                <span className="text-sm text-slate-600">SSL (LDAPS)</span>
              </label>
            </Field>
            <Field label="Bağlantı Kullanıcısı" hint="Örnek: servis_hesabi@kizilay.org.tr">
              <input
                value={bindUsername}
                onChange={(e) => setBindUsername(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Bağlantı Şifresi"
              hint={isEdit ? 'Boş bırakılırsa mevcut şifre korunur.' : undefined}
            >
              <input
                type="password"
                value={bindPassword}
                onChange={(e) => setBindPassword(e.target.value)}
                autoComplete="new-password"
                className={inputClass}
              />
            </Field>
          </>
        )}
      </Section>

      {isActiveDirectory && (
        <>
          <Section title="LDAP Şeması">
            <Field label="Base DN" hint="Kullanıcı ve grupların arandığı kök düğüm.">
              <input
                value={baseDn}
                onChange={(e) => setBaseDn(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Ek Kullanıcı DN"
              hint="Kullanıcı aramasını daraltmak için Base DN'in önüne eklenir."
            >
              <input
                value={additionalUserDn}
                onChange={(e) => setAdditionalUserDn(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Ek Grup DN"
              hint="Grup aramasını daraltmak için Base DN'in önüne eklenir."
            >
              <input
                value={additionalGroupDn}
                onChange={(e) => setAdditionalGroupDn(e.target.value)}
                className={inputClass}
              />
            </Field>
          </Section>

          <section className="border-t border-slate-100 pt-5">
            <h3 className="mb-3 text-sm font-semibold text-slate-800">Dizin İzinleri</h3>
            <div className="space-y-2">
              {[
                {
                  value: 0,
                  label: 'Salt Okunur',
                  hint: 'Kullanıcılar dizinden okunur, sistemde değiştirilemez.',
                },
                {
                  value: 1,
                  label: 'Salt Okunur, Yerel Gruplarla',
                  hint: 'Dizinden okunur; sistem içindeki gruplara eklenebilir.',
                },
                {
                  value: 2,
                  label: 'Okuma/Yazma',
                  hint: 'Sistemdeki değişiklikler dizine de yazılır. Bağlantı kullanıcısının yetkisi olmalıdır.',
                },
              ].map((option) => (
                <label key={option.value} className="flex items-start gap-2">
                  <input
                    type="radio"
                    name="permission"
                    checked={permission === option.value}
                    onChange={() => setPermission(option.value)}
                    className="mt-1 h-4 w-4"
                  />
                  <span>
                    <span className="block text-sm text-slate-700">{option.label}</span>
                    <span className="block text-xs text-slate-400">{option.hint}</span>
                  </span>
                </label>
              ))}
            </div>
          </section>

          <Section title="Kullanıcı Şeması Ayarları">
            <Field label="Kullanıcı Nesne Sınıfı">
              <input
                value={userObjectClass}
                onChange={(e) => setUserObjectClass(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Nesne Filtresi">
              <input
                value={userObjectFilter}
                onChange={(e) => setUserObjectFilter(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Adı Attribute">
              <input
                value={usernameAttribute}
                onChange={(e) => setUsernameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Adı RDN Attribute">
              <input
                value={usernameRdnAttribute}
                onChange={(e) => setUsernameRdnAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Ad Attribute">
              <input
                value={firstNameAttribute}
                onChange={(e) => setFirstNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Soyad Attribute">
              <input
                value={lastNameAttribute}
                onChange={(e) => setLastNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Görünen Ad Attribute">
              <input
                value={displayNameAttribute}
                onChange={(e) => setDisplayNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="E-posta Attribute">
              <input
                value={emailAttribute}
                onChange={(e) => setEmailAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Benzersiz Kimlik Attribute"
              hint="Kullanıcı adı değişse bile kimliğin korunmasını sağlar."
            >
              <input
                value={uniqueIdAttribute}
                onChange={(e) => setUniqueIdAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
          </Section>

          <Section title="Senkronizasyon Zamanlaması">
            <Field label="Otomatik Senkronizasyon">
              <select
                value={syncSchedule}
                onChange={(e) => setSyncSchedule(Number(e.target.value))}
                className={inputClass}
              >
                <option value={0}>Kapalı</option>
                <option value={1}>Saatlik</option>
                <option value={2}>Günlük</option>
                <option value={3}>Haftalık</option>
              </select>
            </Field>
          </Section>
        </>
      )}

      {errorMessage && (
        <p role="alert" className="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {testResult && (
        <p
          role="status"
          className={
            'rounded-md px-3 py-2 text-sm ' +
            (testResult.isError ? 'bg-rose-50 text-rose-700' : 'bg-emerald-50 text-emerald-700')
          }
        >
          {testResult.text}
        </p>
      )}

      <div className="flex items-center justify-between border-t border-slate-100 pt-4">
        <div>
          {isEdit && isActiveDirectory && (
            <button
              type="button"
              onClick={handleTestConnection}
              disabled={testMutation.isPending}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:text-slate-300"
            >
              {testMutation.isPending ? 'Test ediliyor…' : 'Bağlantıyı Test Et'}
            </button>
          )}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
          >
            Vazgeç
          </button>
          <button
            type="submit"
            disabled={isPending || name.trim().length === 0}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
          >
            {isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </div>
    </form>
  );
}
