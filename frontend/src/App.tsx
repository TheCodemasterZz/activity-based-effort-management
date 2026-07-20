import { useState } from 'react';
import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RootLayout } from './components/layout/RootLayout';
import { NotificationHost } from './components/common/NotificationHost';
import { HomePage } from './pages/HomePage';
import { PlanWorkPage } from './pages/PlanWorkPage';
import { ReportPage } from './pages/ReportPage';
import { ProjectsPage } from './pages/ProjectsPage';
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

function App() {
  const [activePage, setActivePage] = useState<AppPage>('workLog');

  return (
    <QueryClientProvider client={queryClient}>
      <NotificationHost />
      <RootLayout activePage={activePage} onNavigate={setActivePage}>
        {activePage === 'home' && <HomePage />}
        {activePage === 'planWork' && <PlanWorkPage />}
        {activePage === 'workLog' && <ReportPage />}
        {activePage === 'projects' && <ProjectsPage />}
      </RootLayout>
    </QueryClientProvider>
  );
}

export default App;
