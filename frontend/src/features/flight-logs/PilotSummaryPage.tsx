import { Box, Paper, Typography } from '@mui/material';
import { ErrorState } from '@/components/ErrorState';
import { LoadingState } from '@/components/LoadingState';
import { PageHeader } from '@/components/PageHeader';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { t } from '@/i18n';
import { formatDateTime, formatHours } from '@/utils/format';
import { usePilotSummary } from './api';

/**
 * Uçuş özeti. Grafik için kütüphane eklenmedi: üç değerlik yatay çubuk CSS ile yeterli. Grafiğin tamamı
 * tek bir aria-label ile özetlenir; değerler çubuk üzerinde metin olarak da yazılı (sadece görsele güvenilmez).
 */
export function PilotSummaryPage() {
  const user = useCurrentUser();
  const query = usePilotSummary(user.userId);

  if (query.isLoading) return <LoadingState />;
  if (query.error || !query.data) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;

  const s = query.data;
  const bars = [
    { label: t.flights.total, value: s.totalHours },
    { label: t.flights.last90, value: s.last90DaysHours },
    { label: t.flights.last30, value: s.last30DaysHours },
  ];
  const max = Math.max(...bars.map((b) => b.value), 1);
  const hours = (value: number) => t.common.hours(formatHours(value));

  return (
    <>
      <PageHeader title={t.flights.summaryTitle} />
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' }, mb: 2 }}>
        {[
          [t.flights.total, hours(s.totalHours)],
          [t.flights.last90, hours(s.last90DaysHours)],
          [t.flights.last30, hours(s.last30DaysHours)],
          [t.flights.totalFlights, String(s.totalFlights)],
        ].map(([label, value]) => (
          <Paper key={label} sx={{ p: 2 }}>
            <Typography color="text.secondary" variant="body2">{label}</Typography>
            <Typography sx={{ fontSize: '1.75rem', fontWeight: 600 }}>{value}</Typography>
          </Paper>
        ))}
      </Box>

      <Paper sx={{ p: 2 }} component="figure" aria-labelledby="chart-title" style={{ margin: 0 }}>
        <Typography variant="h2" id="chart-title" component="figcaption" gutterBottom>{t.flights.chartTitle}</Typography>
        <Box role="img" aria-label={t.flights.chartLabel(hours(s.totalHours), hours(s.last90DaysHours), hours(s.last30DaysHours))}>
          {bars.map((bar) => (
            <Box key={bar.label} sx={{ display: 'grid', gridTemplateColumns: '100px 1fr', alignItems: 'center', gap: 1, mb: 1.5 }}>
              <Typography variant="body2" aria-hidden>{bar.label}</Typography>
              <Box sx={{ bgcolor: 'action.hover', borderRadius: 1 }} aria-hidden>
                <Box
                  sx={{
                    width: `${Math.max((bar.value / max) * 100, 2)}%`, bgcolor: 'primary.main', color: 'primary.contrastText',
                    borderRadius: 1, px: 1, py: 0.5, whiteSpace: 'nowrap', fontSize: '0.875rem', minWidth: 'fit-content',
                  }}
                >
                  {hours(bar.value)}
                </Box>
              </Box>
            </Box>
          ))}
        </Box>
        <Typography variant="body2" color="text.secondary">{t.flights.lastFlight}: {formatDateTime(s.lastFlightAtUtc)}</Typography>
      </Paper>
    </>
  );
}
