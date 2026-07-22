import { useState, type FormEvent } from 'react';
import { login } from '../api/auth';
import { ApiError } from '../api/client';
import { setSession } from '../lib/auth';

function EyeIcon({ off }: { off: boolean }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4.5 w-4.5">
      <path
        d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="12" cy="12" r="3" />
      {off && <path d="M3.5 3.5 20.5 20.5" strokeLinecap="round" />}
    </svg>
  );
}

/** Arkaplanda yavaşça sürüklenen, bulanıklaştırılmış "bulut" şekilleri — dış kütüphane/görsel
 * kullanmadan, saf CSS keyframe animasyonlarıyla. Her bulut farklı süre/gecikme ile hareket
 * ettiği için organik, senkronize olmayan bir sürüklenme hissi verir. Kart üzerindeki içerikle
 * etkileşime girmemesi için pointer-events-none ve düşük opaklık kullanılır. */
function CloudBackground() {
  return (
    <div className="pointer-events-none absolute inset-0 overflow-hidden">
      <style>{`
        @keyframes cloud-sweep-right {
          from { left: -35%; }
          to { left: 115%; }
        }
        @keyframes cloud-sweep-left {
          from { left: 115%; }
          to { left: -35%; }
        }
        @keyframes cloud-bob {
          0%, 100% { transform: translateY(0); }
          50% { transform: translateY(-22px); }
        }
      `}</style>

      {/* Her bulut iki iç içe katman: dış katman sabit hızda tek yönde yatayda süzülür (left),
          iç katman bağımsız olarak hafifçe yukarı-aşağı süzülür (transform) — gerçek bulutların
          rüzgarda hem ilerleyip hem hafifçe süzülme hissini taklit eder. */}
      <div className="absolute top-[2%] h-[26rem] w-[42rem]" style={{ animation: 'cloud-sweep-right 38s linear infinite' }}>
        <div className="h-full w-full rounded-full bg-sky-300/80 blur-2xl" style={{ animation: 'cloud-bob 7s ease-in-out infinite' }} />
      </div>
      <div className="absolute top-[14%] h-[30rem] w-[48rem]" style={{ animation: 'cloud-sweep-left 52s linear infinite', animationDelay: '-14s' }}>
        <div className="h-full w-full rounded-full bg-white blur-2xl" style={{ animation: 'cloud-bob 9s ease-in-out infinite' }} />
      </div>
      <div className="absolute top-[38%] h-[28rem] w-[50rem]" style={{ animation: 'cloud-sweep-right 46s linear infinite', animationDelay: '-6s' }}>
        <div className="h-full w-full rounded-full bg-indigo-200/70 blur-2xl" style={{ animation: 'cloud-bob 8s ease-in-out infinite' }} />
      </div>
      <div className="absolute top-[55%] h-[24rem] w-[44rem]" style={{ animation: 'cloud-sweep-left 34s linear infinite', animationDelay: '-20s' }}>
        <div className="h-full w-full rounded-full bg-sky-200/90 blur-2xl" style={{ animation: 'cloud-bob 6s ease-in-out infinite' }} />
      </div>
      <div className="absolute top-[28%] h-64 w-[30rem]" style={{ animation: 'cloud-sweep-right 22s linear infinite', animationDelay: '-9s' }}>
        <div className="h-full w-full rounded-full bg-white/90 blur-xl" style={{ animation: 'cloud-bob 5s ease-in-out infinite' }} />
      </div>
      <div className="absolute top-[70%] h-56 w-[26rem]" style={{ animation: 'cloud-sweep-left 27s linear infinite', animationDelay: '-3s' }}>
        <div className="h-full w-full rounded-full bg-sky-300/70 blur-xl" style={{ animation: 'cloud-bob 6.5s ease-in-out infinite' }} />
      </div>
    </div>
  );
}

/** Sağ paneldeki dekoratif "erişim onaylandı" grafiği — dış varlık kullanmadan (kütüphane/
 * görsel dosyası yok), telefon + kilit + kalkan ikonlarından oluşan sade, düz vektör bir
 * kompozisyon. */
function AccessIllustration() {
  return (
    <div className="relative flex h-full w-full items-center justify-center">
      <div className="flex h-48 w-32 flex-col items-center justify-center gap-3 rounded-[2rem] border-8 border-white bg-indigo-600 shadow-lg">
        <svg viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="1.8" className="h-9 w-9">
          <rect x="5" y="10.5" width="14" height="9" rx="2" />
          <path d="M8 10.5V7.5a4 4 0 0 1 8 0v3" strokeLinecap="round" />
        </svg>
        <div className="flex gap-1.5">
          {[0, 1, 2, 3].map((i) => (
            <span key={i} className="h-1.5 w-1.5 rounded-full bg-white/80" />
          ))}
        </div>
      </div>

      <div className="absolute right-8 top-6 flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-500 shadow-lg ring-4 ring-white">
        <svg viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.2" className="h-7 w-7">
          <path d="M12 3.5 5 6.5v5c0 4.5 3 7.6 7 9 4-1.4 7-4.5 7-9v-5l-7-3Z" strokeLinejoin="round" />
          <path d="m9 12 2 2 4-4" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
    </div>
  );
}

export function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSubmit = username.trim().length > 0 && password.length > 0 && !isSubmitting;

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!canSubmit) return;

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const result = await login(username.trim(), password);
      setSession(result);
    } catch (error) {
      setErrorMessage(
        error instanceof ApiError
          ? error.message
          : 'Giriş yapılamadı. Lütfen daha sonra tekrar deneyin.',
      );
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-gradient-to-b from-sky-500 via-sky-400 to-sky-200 p-4">
      <CloudBackground />

      <div className="relative z-10 flex w-full max-w-3xl overflow-hidden rounded-3xl bg-white shadow-xl">
        {/* Form paneli */}
        <div className="w-full p-8 sm:p-10 md:w-[55%]">
          <div className="mb-8 flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <span className="text-sm font-bold">M</span>
            </div>
            <span className="text-sm font-semibold text-slate-700">Mesainâme</span>
          </div>

          <h1 className="text-2xl font-bold text-slate-800">Giriş Yap</h1>
          <p className="mt-1.5 text-sm text-slate-500">
            Mesainâme hesabınıza erişmek için giriş yapın.
          </p>

          <form onSubmit={handleSubmit} className="mt-7 space-y-4">
            <label className="block">
              <span className="mb-1.5 block text-xs font-medium text-slate-600">Kullanıcı Adı</span>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                autoFocus
                placeholder="kullanici.adi"
                className="w-full rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-sm text-slate-700 outline-none placeholder:text-slate-400 focus:border-indigo-500 focus:bg-white focus:ring-1 focus:ring-indigo-500"
              />
            </label>

            <label className="block">
              <span className="mb-1.5 block text-xs font-medium text-slate-600">Şifre</span>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  className="w-full rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 pr-10 text-sm text-slate-700 outline-none placeholder:text-slate-400 focus:border-indigo-500 focus:bg-white focus:ring-1 focus:ring-indigo-500"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  <EyeIcon off={showPassword} />
                </button>
              </div>
            </label>

            {errorMessage && (
              <p role="alert" className="rounded-lg bg-rose-50 px-3.5 py-2.5 text-sm text-rose-700">
                {errorMessage}
              </p>
            )}

            <button
              type="submit"
              disabled={!canSubmit}
              className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300"
            >
              {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş Yap'}
            </button>
          </form>

          <p className="mt-8 text-center text-[11px] text-slate-400">
            Copyright © {new Date().getFullYear()} Yönetişim Müdürlüğü. Tüm hakları saklıdır.
          </p>
        </div>

        {/* Dekoratif panel */}
        <div className="hidden w-[45%] items-center justify-center bg-slate-50 p-8 md:flex">
          <AccessIllustration />
        </div>
      </div>
    </div>
  );
}
