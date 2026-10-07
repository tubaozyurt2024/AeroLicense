import { Box, Button, Card, CardActions, CardContent, List, ListItem, ListItemText, Skeleton, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router';
import { ErrorState } from '@/components/ErrorState';
import { PageHeader } from '@/components/PageHeader';
import { StatusChip } from '@/components/StatusChip';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { useLicenses } from '@/features/documents/licensesApi';
import { usePilotSummary } from '@/features/flight-logs/api';
import { t } from '@/i18n';
import { formatDate, formatDateTime, formatHours } from '@/utils/format';
import { useApplications } from './api';

/** Pilotun özeti: üç bağımsız kart, her biri kendi sorgusunu yapar; biri hata verirse diğerleri çalışır. */
export function ApplicantDashboard() {
  const user = useCurrentUser();
  const licenses = useLicenses({ Status: 'Active' });
  const summary = usePilotSummary(user.userId);
  const applications = useApplications({ PageSize: 5 });

  return (
    <>
      <PageHeader title={t.dashboard.title(user.fullName)} />
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' } }}>
        <DashboardCard title={t.dashboard.activeLicenses} query={licenses} action={{ to: '/licenses', label: t.nav.myLicenses }}>
          {(data) =>
            data.items.length === 0 ? (
              <Typography color="text.secondary">{t.dashboard.noActiveLicense}</Typography>
            ) : (
              <List dense disablePadding>
                {data.items.map((l) => (
                  <ListItem key={l.id} disableGutters>
                    <ListItemText primary={`${l.type} · ${l.licenseNumber}`} secondary={`${t.licenses.expiresAt}: ${formatDate(l.expiresAtUtc)}`} />
                  </ListItem>
                ))}
              </List>
            )
          }
        </DashboardCard>

        <DashboardCard title={t.dashboard.totalHours} query={summary} action={{ to: '/flight-summary', label: t.nav.flightSummary }}>
          {(data) => (
            <>
              <Typography variant="h2" component="p" sx={{ fontSize: '2.25rem' }}>{t.common.hours(formatHours(data.totalHours))}</Typography>
              <Typography color="text.secondary">{t.dashboard.last30}: {t.common.hours(formatHours(data.last30DaysHours))}</Typography>
            </>
          )}
        </DashboardCard>

        <DashboardCard title={t.dashboard.recentApplications} query={applications} action={{ to: '/applications', label: t.dashboard.allApplications }}>
          {(data) =>
            data.items.length === 0 ? (
              <Typography color="text.secondary">{t.dashboard.noApplications}</Typography>
            ) : (
              <List dense disablePadding>
                {data.items.map((a) => (
                  <ListItem key={a.id} disableGutters secondaryAction={<StatusChip kind="application" status={a.status} />}>
                    <ListItemText
                      primary={<RouterLink to={`/applications/${a.id}`}>{a.licenseType}</RouterLink>}
                      secondary={formatDateTime(a.createdAtUtc)}
                    />
                  </ListItem>
                ))}
              </List>
            )
          }
        </DashboardCard>
      </Box>
    </>
  );
}

type QueryLike<T> = { data: T | undefined; isLoading: boolean; error: unknown; refetch: () => unknown };

function DashboardCard<T>({ title, query, action, children }: {
  title: string; query: QueryLike<T>; action: { to: string; label: string }; children: (data: T) => ReactNode;
}) {
  return (
    <Card component="section" aria-label={title} sx={{ display: 'flex', flexDirection: 'column' }}>
      <CardContent sx={{ flexGrow: 1 }}>
        <Typography variant="h2" gutterBottom>{title}</Typography>
        {query.isLoading && <Skeleton height={80} />}
        {!!query.error && <ErrorState error={query.error} onRetry={() => void query.refetch()} />}
        {query.data !== undefined && children(query.data)}
      </CardContent>
      <CardActions>
        <Button component={RouterLink} to={action.to} size="small">{action.label}</Button>
      </CardActions>
    </Card>
  );
}
