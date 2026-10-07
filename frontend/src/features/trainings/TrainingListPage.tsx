import { Button, Chip, FormControlLabel, Paper, Switch } from '@mui/material';
import { Link as RouterLink } from 'react-router';
import type { TrainingRecordDto } from '@/api/types';
import { DataTable, type Column } from '@/components/DataTable';
import { PageHeader } from '@/components/PageHeader';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { useUrlQueryState } from '@/hooks/useUrlQueryState';
import { t } from '@/i18n';
import { formatDate } from '@/utils/format';
import { useTrainingRecords } from './api';

const resultChip = (r: TrainingRecordDto) => (
  <Chip size="small" color={r.isPassed ? 'success' : 'error'} label={r.isPassed ? t.trainings.passed : t.trainings.failed} />
);

/**
 * Pilot: tamamladığı eğitimler. Eğitim kuruluşu: kendi girdiği kayıtlar (backend kapsamı zaten daraltır).
 * TC kimlik no backend'den maskeli gelir; önyüz tam değeri hiç görmez.
 */
export function TrainingListPage() {
  const user = useCurrentUser();
  const isOrg = user.role === 'TrainingOrg';
  const { page, pageSize, filters, setPage, setPageSize, setFilter } = useUrlQueryState(['passedOnly']);
  const passedOnly = filters.passedOnly === 'true';
  const query = useTrainingRecords({ PassedOnly: passedOnly, Page: page, PageSize: pageSize });

  const columns: Column<TrainingRecordDto>[] = [
    ...(isOrg
      ? [
          { id: 'applicant', header: t.trainings.applicant, render: (r: TrainingRecordDto) => r.applicantName },
          {
            id: 'nid',
            header: t.trainings.nationalId,
            render: (r: TrainingRecordDto) => <span style={{ fontFamily: 'monospace' }}>{r.applicantNationalIdMasked ?? '—'}</span>,
            hideOnMobile: true,
          },
        ]
      : [{ id: 'org', header: t.trainings.org, render: (r: TrainingRecordDto) => r.trainingOrgName }]),
    { id: 'type', header: t.trainings.licenseType, render: (r) => r.licenseType },
    { id: 'date', header: t.trainings.completedAt, render: (r) => formatDate(r.completedAtUtc), hideOnMobile: true },
    { id: 'score', header: t.trainings.score, render: (r) => r.examScore, align: 'right' },
    { id: 'result', header: t.trainings.result, render: resultChip },
  ];

  return (
    <>
      <PageHeader
        title={isOrg ? t.trainings.title : t.trainings.myTitle}
        actions={isOrg && <Button variant="contained" component={RouterLink} to="/training-records/new">{t.nav.newTrainingRecord}</Button>}
      />
      <Paper sx={{ px: 2, py: 1, mb: 2 }}>
        <FormControlLabel
          control={<Switch checked={passedOnly} onChange={(e) => setFilter('passedOnly', e.target.checked ? 'true' : '')} />}
          label={t.trainings.passedOnly}
        />
      </Paper>
      <DataTable
        caption={isOrg ? t.trainings.title : t.trainings.myTitle}
        columns={columns}
        rows={query.data?.items}
        getRowId={(r) => r.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        pagination={{ page, pageSize, totalCount: query.data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: setPageSize }}
      />
    </>
  );
}
