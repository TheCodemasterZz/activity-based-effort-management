import { NAV_ITEMS, type AppPage } from '../../lib/navigation';
import { NotificationBell } from './NotificationBell';
import { ProfileMenu } from './ProfileMenu';

interface HeaderProps {
  activePage: AppPage;
  onNavigate: (page: AppPage) => void;
}

export function Header({ activePage, onNavigate }: HeaderProps) {
  return (
    <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
      <div className="flex items-center gap-6">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-600 text-white">
            <span className="text-sm font-bold">M</span>
          </div>
          <div>
            <div className="text-sm font-semibold text-slate-800">Mesainâme</div>
            <div className="text-xs text-slate-400">Emeğin Defteri</div>
          </div>
        </div>

        <nav className="flex items-center gap-1">
          {NAV_ITEMS.map((item) => (
            <button
              key={item.page}
              type="button"
              onClick={() => onNavigate(item.page)}
              className={
                'rounded-md px-3 py-1.5 text-sm font-medium transition-colors ' +
                (activePage === item.page
                  ? 'bg-indigo-50 text-indigo-700'
                  : 'text-slate-500 hover:bg-slate-50 hover:text-slate-700')
              }
            >
              {item.label}
            </button>
          ))}
        </nav>
      </div>

      <div className="flex items-center gap-4">
        <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-amber-700">
          Test Mode
        </span>
        <NotificationBell />
        <button
          type="button"
          onClick={() => onNavigate('admin')}
          className={
            'text-slate-400 hover:text-slate-600' + (activePage === 'admin' ? ' text-indigo-600' : '')
          }
          aria-label="Yönetim"
        >
          ⚙️
        </button>
        <ProfileMenu />
      </div>
    </header>
  );
}
