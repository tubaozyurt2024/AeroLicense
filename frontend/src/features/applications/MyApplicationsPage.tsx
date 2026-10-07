import { Button, Link, MenuItem, Paper, TextField } from '@mui/material';
import { Link as RouterLink } from 'react-router';
import type { LicenseApplicationDto } from '@/api/types';
import { DataTable, type Column } from '@/components/DataTable';
import { PageHeader } from '@/components/PageHeader';
import { StatusChip } from '@/components/StatusChip';
import { useUrlQueryState } from '@/hooks/useUrlQueryState';
import { t } from '@/i18n';
import { formatDateTime } from '@/utils/format';
import { useApplications } from './api';
import { APPLICATION_STATUSES, parseStatus } from './applicationFilters';

const columns: Column<LicenseApplicationDto>[] = [
  {
    id: 'type',
    header: t.applications.licenseType,
    render: (a) => <Link component={RouterLink} to={`/applications/${a.id}`}>{t.licenseTypes[a.licenseType]}</Link>,
  },
  { id: 'status', header: t.applications.status, render: (a) => <StatusChip kind="application" status={a.status} /> },
  { id: 'created', header: t.applications.createdAt, render: (a) => formatDateTime(a.createdAtUtc), hideOnMobile: true },
  { id: 'submitted', header: t.applications.submittedAt, render: (a) => formatDateTime(a.submittedAtUtc), hideOnMobile: true },
  { id: 'license', header: t.applications.licenseNumber, render: (a) => a.licenseNumber ?? '—', hideOnMobile: true },
];

export function MyApplicationsPage() {
  const { page, pageSize, filters, setPage, setPageSize, setFilter } = useUrlQueryState(['status']);
  const status = parseStatus(filters.status);
  const query = useApplications({ Status: status, Page: page, PageSize: pageSize });

  return (
    <>
      <PageHeader
        title={t.applications.title}
        actions={<Button variant="contained" component={RouterLink} to="/applications/new">{t.nav.newApplication}</Button>}
      />
      <Paper sx={{ p: 2, mb: 2 }}>
        <TextField select label={t.applications.status} value={status ?? ''} onChange={(e) => setFilter('status', e.target.value)} sx={{ maxWidth: 260 }}>
          <MenuItem value="">{t.common.all}</MenuItem>
          {APPLICATION_STATUSES.map((s) => <MenuItem key={s} value={s}>{t.status.application[s]}</MenuItem>)}
        </TextField>
      </Paper>
      <DataTable
        caption={t.applications.title}
        columns={columns}
        rows={query.data?.items}
        getRowId={(a) => a.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        emptyMessage={t.dashboard.noApplications}
        pagination={{ page, pageSize, totalCount: query.data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: setPageSize }}
      />
    </>
  );
}
