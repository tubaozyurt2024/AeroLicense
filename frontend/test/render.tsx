import { CssBaseline } from '@mui/material';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { RouterProvider, createMemoryRouter } from 'react-router';
import type { CurrentUser, UserRole } from '@/api/types';
import { NotificationProvider } from '@/app/Notifications';
import { createRoutes } from '@/app/router';
import { theme } from '@/app/theme';

export const users = {
  applicant: user('Applicant', 'Pilot Ada Demo'),
  inspector: user('Inspector', 'Denetçi Demo'),
  trainingOrg: user('TrainingOrg', 'Eğitim Sorumlusu', 'Anadolu Uçuş Akademisi (Demo)'),
  airline: user('Airline', 'Entegrasyon XDA', 'Demo Hava Yolları'),
};

function user(role: UserRole, fullName: string, organizationName?: string): CurrentUser {
  return {
    userId: `00000000-0000-7000-8000-00000000000${['Applicant', 'Inspector', 'TrainingOrg', 'Airline'].indexOf(role) + 1}`,
    role,
    fullName,
    organizationId: organizationName ? '00000000-0000-7000-8000-0000000000aa' : null,
    organizationName: organizationName ?? null,
  };
}

/** Uygulamanın gerçek route ağacını, verilen adresten ve (isteğe bağlı) oturumla render eder. */
export function renderApp(path: string, currentUser: CurrentUser | null = null) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  const router = createMemoryRouter(createRoutes(currentUser), { initialEntries: [path] });
  const result = render(
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <RouterProvider router={router} />
        </NotificationProvider>
      </QueryClientProvider>
    </ThemeProvider>,
  );
  return { ...result, router, queryClient };
}
