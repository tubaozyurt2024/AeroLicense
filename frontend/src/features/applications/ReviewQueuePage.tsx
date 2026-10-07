import { Link, MenuItem, Paper, TextField } from '@mui/material';
import { Link as RouterLink } from 'react-router';
import type { ApplicationSortField, LicenseApplicationDto, SortDirection } from '@/api/types';
import { DataTable, type Column } from '@/components/DataTable';
import { PageHeader } from '@/components/PageHeader';
import { StatusChip } from '@/components/StatusChip';
import { useUrlQueryState } from '@/hooks/useUrlQueryState';
import { t } from '@/i18n';
import { formatDateTime } from '@/utils/format';
import { useApplications } from './api';
import { APPLICATION_STATUSES, parseStatus } from './applicationFilters';

const SORT_FIELDS: readonly ApplicationSortField[] = ['CreatedAt', 'SubmittedAt'];

const columns: Column<LicenseApplicationDto>[] = [
  {
    id: 'applicant',
    header: t.applications.applicant,
    render: (a) => <Link component={RouterLink} to={`/review/${a.id}`}>{a.applicantName}</Link>,
  },
  { id: 'type', header: t.applications.licenseType, render: (a) => a.licenseType },
  { id: 'status', header: t.applications.status, render: (a) => <StatusChip kind="application" status={a.status} /> },
  { id: 'submitted', header: t.applications.submittedAt, render: (a) => formatDateTime(a.submittedAtUtc), sortField: 'SubmittedAt' },
  { id: 'created', header: t.applications.createdAt, render: (a) => formatDateTime(a.createdAtUtc), sortField: 'CreatedAt', hideOnMobile: true },
  { id: 'reviewer', header: t.applications.reviewer, render: (a) => a.reviewerName ?? '—', hideOnMobile: true },
];

/**
 * Denetçi kuyruğu. Varsayılan: "Gönderildi" durumundakiler, en eski gönderim önce (ilk gelen ilk incelenir).
 * Sıralama ve filtre sunucuda yapılır; istemcide yapılsaydı sadece mevcut sayfa sıralanırdı (yanlış sonuç).
 */
export function ReviewQueuePage() {
  const { page, pageSize, filters, setPage, setPageSize, setFilter, setFilters } = useUrlQueryState(['status', 'sortBy', 'sortDir']);
  // URL'de status yoksa varsayılan kuyruk; "all" açıkça tümünü ister.
  const status = filters.status === 'all' ? undefined : (parseStatus(filters.status) ?? 'Submitted');
  const sortBy: ApplicationSortField = SORT_FIELDS.find((f) => f === filters.sortBy) ?? 'SubmittedAt';
  const sortDir: SortDirection = filters.sortDir === 'Desc' ? 'Desc' : 'Asc';

  const query = useApplications({ Status: status, SortBy: sortBy, SortDir: sortDir, Page: page, PageSize: pageSize });

  return (
    <>
      <PageHeader title={t.review.title} />
      <Paper sx={{ p: 2, mb: 2 }}>
        <TextField
          select
          label={t.applications.status}
          value={status ?? 'all'}
          onChange={(e) => setFilter('status', e.target.value)}
          sx={{ maxWidth: 260 }}
        >
          <MenuItem value="all">{t.common.all}</MenuItem>
          {APPLICATION_STATUSES.map((s) => <MenuItem key={s} value={s}>{t.status.application[s]}</MenuItem>)}
        </TextField>
      </Paper>
      <DataTable
        caption={t.review.title}
        columns={columns}
        rows={query.data?.items}
        getRowId={(a) => a.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        sort={{
          field: sortBy,
          direction: sortDir,
          // Aynı başlığa tekrar tıklamak yönü çevirir; farklı başlık artan sırayla başlar.
          onChange: (field) =>
            setFilters({ sortBy: field, sortDir: field === sortBy && sortDir === 'Asc' ? 'Desc' : 'Asc' }),
        }}
        pagination={{ page, pageSize, totalCount: query.data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: setPageSize }}
      />
    </>
  );
}
