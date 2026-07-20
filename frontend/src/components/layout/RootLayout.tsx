import type { ReactNode } from 'react';
import type { AppPage } from '../../lib/navigation';
import { Header } from './Header';
import { Footer } from './Footer';

interface RootLayoutProps {
  children: ReactNode;
  activePage: AppPage;
  onNavigate: (page: AppPage) => void;
}

export function RootLayout({ children, activePage, onNavigate }: RootLayoutProps) {
  return (
    <div className="flex h-screen flex-col overflow-hidden bg-slate-50">
      <Header activePage={activePage} onNavigate={onNavigate} />
      <main className="flex flex-1 flex-col overflow-hidden">{children}</main>
      <Footer />
    </div>
  );
}
