import { lazy, type ComponentType, type ReactNode } from 'react';
import { Outlet, createBrowserRouter, type RouteObject } from 'react-router';
import type { CurrentUser, UserRole } from '@/api/types';
import { AuthProvider } from '@/features/auth/AuthContext';
import { LoginPage } from '@/features/auth/LoginPage';
import { access } from './access';
import { AppLayout } from './AppLayout';
import { PublicLayout } from './PublicLayout';
import { RequireRole } from './RequireRole';
import { ForbiddenPage, HomeRedirect, NotFoundPage, RouteErrorPage } from './StatusPages';

/**
 * Rol sayfaları lazy yüklenir: bir pilot, denetçi ekranlarının kodunu hiç indirmez (daha küçük ilk yük).
 * Login ve doğrulama sayfası ana pakette: QR'dan gelen kişi en hızlı şekilde sonucu görmeli.
 */
const named = <K extends string>(loader: () => Promise<Record<K, ComponentType>>, name: K) =>
  lazy(() => loader().then((module) => ({ default: module[name] })));

const VerifyPage = named(() => import('@/features/documents/VerifyPage'), 'VerifyPage');

// Applicant
const ApplicantDashboard = named(() => import('@/features/applications/ApplicantDashboard'), 'ApplicantDashboard');
const MyApplicationsPage = named(() => import('@/features/applications/MyApplicationsPage'), 'MyApplicationsPage');
const NewApplicationWizard = named(() => import('@/features/applications/NewApplicationWizard'), 'NewApplicationWizard');
const ApplicationDetailPage = named(() => import('@/features/applications/ApplicationDetailPage'), 'ApplicationDetailPage');
const TrainingListPage = named(() => import('@/features/trainings/TrainingListPage'), 'TrainingListPage');
const PilotSummaryPage = named(() => import('@/features/flight-logs/PilotSummaryPage'), 'PilotSummaryPage');
const LicenseDocumentPage = named(() => import('@/features/documents/LicenseDocumentPage'), 'LicenseDocumentPage');

// Inspector (+ Airline: uçuş listesi)
const ReviewQueuePage = named(() => import('@/features/applications/ReviewQueuePage'), 'ReviewQueuePage');
const FlightLogListPage = named(() => import('@/features/flight-logs/FlightLogListPage'), 'FlightLogListPage');
const AuditLogPage = named(() => import('@/features/audit/AuditLogPage'), 'AuditLogPage');

// TrainingOrg
const TrainingRecordForm = named(() => import('@/features/trainings/TrainingRecordForm'), 'TrainingRecordForm');

// Airline
const AirlineSubmitForm = named(() => import('@/features/flight-logs/AirlineSubmitForm'), 'AirlineSubmitForm');

/**
 * Route ağacı fonksiyonla üretilir: testler aynı ağacı createMemoryRouter ile, hazır bir oturumla
 * (initialUser) kurar. Böylece test edilen şey gerçek route/guard yapılandırmasıdır.
 */
export const createRoutes = (initialUser: CurrentUser | null = null): RouteObject[] => [
  {
    element: (
      <AuthProvider initialUser={initialUser}>
        <Outlet />
      </AuthProvider>
    ),
    errorElement: <RouteErrorPage />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/verify/:code?', element: <VerifyPage /> },
        ],
      },
      {
        element: <RequireRole />,
        children: [
          {
            element: <AppLayout />,
            children: [
              { index: true, element: <HomeRedirect /> },
              { path: '/yetkisiz', element: <ForbiddenPage /> },
              ...protectedRoutes(),
            ],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];

/** Rol sayfaları: her biri erişim tablosundaki (access.ts) rollerle korunur. Modüller eklendikçe büyür. */
function protectedRoutes(): RouteObject[] {
  const guarded = (path: string, roles: readonly UserRole[], element: ReactNode): RouteObject => ({
    path,
    element: <RequireRole roles={roles}>{element}</RequireRole>,
  });
  return [
    guarded('/dashboard', access.dashboard, <ApplicantDashboard />),
    guarded('/applications', access.myApplications, <MyApplicationsPage />),
    guarded('/applications/new', access.newApplication, <NewApplicationWizard />),
    guarded('/applications/:id', access.myApplications, <ApplicationDetailPage />),
    guarded('/trainings', access.myTrainings, <TrainingListPage />),
    guarded('/flight-summary', access.flightSummary, <PilotSummaryPage />),
    guarded('/licenses', access.myLicenses, <LicenseDocumentPage />),

    guarded('/review', access.reviewQueue, <ReviewQueuePage />),
    guarded('/review/:id', access.reviewQueue, <ApplicationDetailPage />),
    guarded('/flight-logs', access.flightLogs, <FlightLogListPage />),
    guarded('/audit', access.audit, <AuditLogPage />),

    guarded('/training-records', access.trainingRecords, <TrainingListPage />),
    guarded('/training-records/new', access.newTrainingRecord, <TrainingRecordForm />),

    guarded('/flight-logs/new', access.submitFlightLog, <AirlineSubmitForm />),
  ];
}

export const createAppRouter = () => createBrowserRouter(createRoutes());
