import { Alert, AlertTitle, Box, Button, Chip, List, ListItem, ListItemText, Paper, Typography } from '@mui/material';
import { useState } from 'react';
import { useParams } from 'react-router';
import { userMessage } from '@/api/problem';
import type { LicenseApplicationDto } from '@/api/types';
import { useNotify } from '@/app/Notifications';
import { ConfirmDialog } from '@/components/ConfirmDialog';
import { DataTable } from '@/components/DataTable';
import { DefinitionList } from '@/components/DefinitionList';
import { ErrorState } from '@/components/ErrorState';
import { LoadingState } from '@/components/LoadingState';
import { PageHeader } from '@/components/PageHeader';
import { StatusChip } from '@/components/StatusChip';
import { useAuditLogs } from '@/features/audit/api';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { useTrainingRecords } from '@/features/trainings/api';
import { t } from '@/i18n';
import { formatDate, formatDateTime } from '@/utils/format';
import { useApplication, useSubmitApplication } from './api';
import { ApplicationTimeline } from './ApplicationTimeline';
import { ReviewActions } from './ReviewActions';

/** Başvuru detayı. Pilot ve denetçi aynı sayfayı görür; denetçiye ek olarak karar paneli ve geçmiş gösterilir. */
export function ApplicationDetailPage() {
  const { id = '' } = useParams();
  const user = useCurrentUser();
  const query = useApplication(id);

  if (query.isLoading) return <LoadingState />;
  if (query.error || !query.data) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;

  const a = query.data;
  const isInspector = user.role === 'Inspector';

  return (
    <>
      <PageHeader
        title={t.applications.detailTitle}
        subtitle={<StatusChip kind="application" status={a.status} size="medium" />}
        actions={a.status === 'Draft' && user.role === 'Applicant' ? <SubmitDraftButton id={a.id} /> : undefined}
      />

      {a.status === 'Rejected' && a.rejectionReason && (
        <Alert severity="error" sx={{ mb: 2 }}>
          <AlertTitle>{t.applications.rejectionReason}</AlertTitle>
          {/* Düz metin olarak basılır (React kaçışlar); HTML olarak yorumlanmaz. */}
          {a.rejectionReason}
        </Alert>
      )}

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' }, alignItems: 'start' }}>
        <Box sx={{ display: 'grid', gap: 2 }}>
          <Paper sx={{ p: 2 }}>
            {isInspector && (
              <Box sx={{ mb: 2 }}>
                <ReviewActions application={a} currentUserId={user.userId} />
              </Box>
            )}
            <DefinitionList
              items={[
                [t.applications.licenseType, t.licenseTypes[a.licenseType]],
                [t.applications.applicant, a.applicantName],
                [t.applications.createdAt, formatDateTime(a.createdAtUtc)],
                [t.applications.submittedAt, formatDateTime(a.submittedAtUtc)],
                [t.applications.reviewer, a.reviewerName ?? '—'],
                [t.applications.licenseNumber, a.licenseNumber ?? '—'],
                [t.licenses.expiresAt, formatDate(a.licenseExpiresAtUtc)],
              ]}
            />
          </Paper>
          {isInspector && <ApplicantTrainings application={a} />}
        </Box>

        <Box sx={{ display: 'grid', gap: 2 }}>
          <Paper sx={{ p: 2 }} component="section" aria-label={t.applications.timeline}>
            <Typography variant="h2" gutterBottom>{t.applications.timeline}</Typography>
            <ApplicationTimeline application={a} />
          </Paper>
          {isInspector && <AuditHistory applicationId={a.id} />}
        </Box>
      </Box>
    </>
  );
}

function SubmitDraftButton({ id }: { id: string }) {
  const submit = useSubmitApplication();
  const notify = useNotify();
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button variant="contained" onClick={() => setOpen(true)}>{t.applications.submit}</Button>
      <ConfirmDialog
        open={open}
        title={t.applications.submitConfirmTitle}
        description={t.applications.submitConfirmBody}
        confirmLabel={t.applications.submit}
        pending={submit.isPending}
        onClose={() => setOpen(false)}
        onConfirm={async () => {
          // mutate(..., { onSuccess }) DEĞİL: başarıdan sonra durum "Submitted" olur ve bu buton ekrandan
          // kalkar; TanStack Query, unmount olmuş bileşenin mutate callback'lerini çağırmaz ve bildirim
          // hiç görünmezdi. mutateAsync'in promise'i bileşenden bağımsız çözülür.
          try {
            await submit.mutateAsync(id);
            notify(t.applications.submitted);
          } catch (error) {
            notify(userMessage(error), 'error');
          } finally {
            setOpen(false);
          }
        }}
      />
    </>
  );
}

/** Denetçi: başvuranın tüm eğitimleri; başvuruya kanıt olarak bağlanan kayıt işaretlenir. */
function ApplicantTrainings({ application }: { application: LicenseApplicationDto }) {
  const query = useTrainingRecords({ ApplicantId: application.applicantId, PageSize: 50 });
  return (
    <Box component="section" aria-label={t.review.applicantTrainings}>
      <Typography variant="h2" sx={{ mb: 1 }}>{t.review.applicantTrainings}</Typography>
      <DataTable
        caption={t.review.applicantTrainings}
        rows={query.data?.items}
        getRowId={(r) => r.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        columns={[
          {
            id: 'type',
            header: t.trainings.licenseType,
            render: (r) => (
              <>
                {r.licenseType}{' '}
                {r.id === application.trainingRecordId && <Chip size="small" color="primary" label={t.applications.evidence} />}
              </>
            ),
          },
          { id: 'org', header: t.trainings.org, render: (r) => r.trainingOrgName, hideOnMobile: true },
          { id: 'date', header: t.trainings.completedAt, render: (r) => formatDate(r.completedAtUtc) },
          { id: 'score', header: t.trainings.score, render: (r) => r.examScore, align: 'right' },
          {
            id: 'result',
            header: t.trainings.result,
            render: (r) => <Chip size="small" color={r.isPassed ? 'success' : 'error'} label={r.isPassed ? t.trainings.passed : t.trainings.failed} />,
          },
        ]}
      />
    </Box>
  );
}

function AuditHistory({ applicationId }: { applicationId: string }) {
  const query = useAuditLogs({ EntityType: 'LicenseApplication', EntityId: applicationId, PageSize: 50 });
  return (
    <Paper sx={{ p: 2 }} component="section" aria-label={t.review.auditHistory}>
      <Typography variant="h2" gutterBottom>{t.review.auditHistory}</Typography>
      {query.isLoading && <LoadingState />}
      {!!query.error && <ErrorState error={query.error} onRetry={() => void query.refetch()} />}
      <List dense disablePadding>
        {query.data?.items.map((log) => (
          <ListItem key={log.id} disableGutters>
            <ListItemText
              primary={t.audit.actions[log.action] ?? log.action}
              secondary={`${formatDateTime(log.occurredAtUtc)} · ${log.actorEmailMasked ?? t.audit.system}`}
            />
          </ListItem>
        ))}
      </List>
    </Paper>
  );
}
