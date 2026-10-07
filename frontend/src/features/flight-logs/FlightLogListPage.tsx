import { Box, Button, Chip, FormControlLabel, Paper, Stack, Switch, TextField, Tooltip } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router';
import type { FlightLogListItemDto } from '@/api/types';
import { DataTable, type Column } from '@/components/DataTable';
import { PageHeader } from '@/components/PageHeader';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { useUrlQueryState } from '@/hooks/useUrlQueryState';
import { t } from '@/i18n';
import { formatDateTime, formatDuration } from '@/utils/format';
import { useFlightLogs } from './api';

/**
 * Uçuş kayıtları. Denetçi: tüm havayolları + pilot adı + şüpheli filtresi. Havayolu: sadece kendi
 * gönderdikleri; pilot adı backend'den zaten gelmez (veri minimizasyonu), kolon da gösterilmez.
 */
export function FlightLogListPage() {
  const user = useCurrentUser();
  const isInspector = user.role === 'Inspector';
  const { page, pageSize, filters, setPage, setPageSize, setFilter } = useUrlQueryState(['suspicious', 'license']);
  const suspicious = filters.suspicious === 'true';
  const [licenseInput, setLicenseInput] = useState(filters.license);

  const query = useFlightLogs({
    Suspicious: suspicious,
    PilotLicenseNumber: filters.license || undefined,
    Page: page,
    PageSize: pageSize,
  });

  const columns: Column<FlightLogListItemDto>[] = [
    { id: 'date', header: t.flights.date, render: (f) => formatDateTime(f.departureAtUtc) },
    { id: 'flight', header: t.flights.flight, render: (f) => f.flightNumber },
    { id: 'route', header: t.flights.route, render: (f) => `${f.departureAirport} → ${f.arrivalAirport}` },
    { id: 'duration', header: t.flights.duration, render: (f) => formatDuration(f.durationMinutes), align: 'right' },
    ...(isInspector ? [{ id: 'airline', header: t.flights.airline, render: (f: FlightLogListItemDto) => f.airlineCode, hideOnMobile: true }] : []),
    {
      id: 'pilot',
      header: isInspector ? t.flights.pilot : t.flights.license,
      render: (f) => (isInspector ? `${f.pilotName ?? '—'} (${f.pilotLicenseNumber})` : f.pilotLicenseNumber),
      hideOnMobile: true,
    },
    { id: 'recorded', header: t.flights.recordedAt, render: (f) => formatDateTime(f.recordedAtUtc), hideOnMobile: true },
    {
      id: 'flags',
      header: t.flights.flags,
      render: (f) => (
        <Stack direction="row" spacing={0.5}>
          {f.suspicionReasons.map((reason) => (
            <Chip key={reason} size="small" color="warning" label={t.flights.reasons[reason]} />
          ))}
        </Stack>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title={isInspector ? t.flights.listTitle : t.flights.sentTitle}
        actions={!isInspector && <Button variant="contained" component={RouterLink} to="/flight-logs/new">{t.nav.submitFlightLog}</Button>}
      />
      <Paper sx={{ p: 2, mb: 2 }}>
        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            setFilter('license', licenseInput.trim().toUpperCase());
          }}
          sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}
        >
          <Tooltip title={t.flights.suspiciousHelp}>
            <FormControlLabel
              control={<Switch checked={suspicious} onChange={(e) => setFilter('suspicious', e.target.checked ? 'true' : '')} />}
              label={t.flights.suspiciousOnly}
            />
          </Tooltip>
          <TextField
            size="small"
            label={t.flights.license}
            value={licenseInput}
            onChange={(e) => setLicenseInput(e.target.value)}
            sx={{ maxWidth: 260 }}
          />
          <Button type="submit" variant="outlined">{t.common.filter}</Button>
        </Box>
      </Paper>
      <DataTable
        caption={isInspector ? t.flights.listTitle : t.flights.sentTitle}
        columns={columns}
        rows={query.data?.items}
        getRowId={(f) => f.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        pagination={{ page, pageSize, totalCount: query.data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: setPageSize }}
      />
    </>
  );
}
