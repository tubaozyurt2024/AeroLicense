import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutlined';
import AssignmentIcon from '@mui/icons-material/Assignment';
import BadgeIcon from '@mui/icons-material/Badge';
import DashboardIcon from '@mui/icons-material/Dashboard';
import FactCheckIcon from '@mui/icons-material/FactCheck';
import FlightIcon from '@mui/icons-material/Flight';
import FlightTakeoffIcon from '@mui/icons-material/FlightTakeoff';
import HistoryIcon from '@mui/icons-material/History';
import QueryStatsIcon from '@mui/icons-material/QueryStats';
import SchoolIcon from '@mui/icons-material/School';
import type { ReactElement } from 'react';
import type { UserRole } from '@/api/types';
import { t } from '@/i18n';
import { canAccess, type AccessKey } from './access';

type NavItem = { key: AccessKey; path: string; label: (role: UserRole) => string; icon: ReactElement };

const items: NavItem[] = [
  { key: 'dashboard', path: '/dashboard', label: () => t.nav.dashboard, icon: <DashboardIcon /> },
  { key: 'myApplications', path: '/applications', label: () => t.nav.myApplications, icon: <AssignmentIcon /> },
  { key: 'newApplication', path: '/applications/new', label: () => t.nav.newApplication, icon: <AddCircleOutlineIcon /> },
  { key: 'myTrainings', path: '/trainings', label: () => t.nav.myTrainings, icon: <SchoolIcon /> },
  { key: 'flightSummary', path: '/flight-summary', label: () => t.nav.flightSummary, icon: <QueryStatsIcon /> },
  { key: 'myLicenses', path: '/licenses', label: () => t.nav.myLicenses, icon: <BadgeIcon /> },
  { key: 'trainingRecords', path: '/training-records', label: () => t.nav.trainingRecords, icon: <SchoolIcon /> },
  { key: 'newTrainingRecord', path: '/training-records/new', label: () => t.nav.newTrainingRecord, icon: <AddCircleOutlineIcon /> },
  { key: 'reviewQueue', path: '/review', label: () => t.nav.reviewQueue, icon: <FactCheckIcon /> },
  { key: 'submitFlightLog', path: '/flight-logs/new', label: () => t.nav.submitFlightLog, icon: <FlightTakeoffIcon /> },
  {
    key: 'flightLogs',
    path: '/flight-logs',
    label: (role) => (role === 'Airline' ? t.nav.sentFlightLogs : t.nav.flightLogs),
    icon: <FlightIcon />,
  },
  { key: 'audit', path: '/audit', label: () => t.nav.audit, icon: <HistoryIcon /> },
];

/** Menü, route guard'larla aynı erişim tablosundan (access.ts) süzülür. */
export const navigationFor = (role: UserRole) =>
  items.filter((item) => canAccess(role, item.key)).map((item) => ({ ...item, label: item.label(role) }));
