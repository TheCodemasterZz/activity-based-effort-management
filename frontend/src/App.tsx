import { useState } from 'react';
import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RootLayout } from './components/layout/RootLayout';
import { NotificationHost } from './components/common/NotificationHost';
import { HomePage } from './pages/HomePage';
import { PlanWorkPage } from './pages/PlanWorkPage';
import { CapacityManagementPage } from './pages/CapacityManagementPage';
import { ReportPage } from './pages/ReportPage';
import { ProjectsPage } from './pages/ProjectsPage';
import { EmployeesPage } from './pages/EmployeesPage';
import { WidgetsPage } from './pages/WidgetsPage';
import { WidgetLogWorkPage } from './pages/WidgetLogWorkPage';
import { AdminPage } from './pages/AdminPage';
import { pushErrorNotification, toErrorMessage } from './lib/notifications';
import type { AppPage } from './lib/navigation';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1 },
  },
  queryCache: new QueryCache({
    onError: (error) => pushErrorNotification(toErrorMessage(error)),
  }),
  mutationCache: new MutationCache({
    onError: (error) => pushErrorNotification(toErrorMessage(error)),
  }),
});

// Uygulamada gerçek bir router yok (bkz. WidgetLogWorkPage.tsx dosya başı açıklaması) — widget
// modu, normal sayfa navigasyonunun tamamen dışında, doğrudan URL'deki `?widget=log-work`
// parametresine bakılarak tespit edilir.
function isWidgetMode(): boolean {
  return new URLSearchParams(window.location.search).get('widget') === 'log-work';
}

function App() {
  const [activePage, setActivePage] = useState<AppPage>('workLog');

  return (
    <QueryClientProvider client={queryClient}>
      <NotificationHost />
      {isWidgetMode() ? (
        <WidgetLogWorkPage />
      ) : (
        <RootLayout activePage={activePage} onNavigate={setActivePage}>
          {activePage === 'home' && <HomePage />}
          {activePage === 'planWork' && <PlanWorkPage />}
          {activePage === 'workLog' && <ReportPage />}
          {activePage === 'capacityManagement' && <CapacityManagementPage />}
          {activePage === 'projects' && <ProjectsPage />}
          {activePage === 'employees' && <EmployeesPage />}
          {activePage === 'widgets' && <WidgetsPage />}
          {activePage === 'admin' && <AdminPage />}
        </RootLayout>
      )}
    </QueryClientProvider>
  );
}

export default App;
