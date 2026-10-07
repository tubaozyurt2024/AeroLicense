import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { applyFieldErrors, userMessage } from '@/api/problem';
import type { LicenseApplicationDto } from '@/api/types';
import { useNotify } from '@/app/Notifications';
import { ConfirmDialog } from '@/components/ConfirmDialog';
import { t } from '@/i18n';
import { useApproveApplication, useRejectApplication, useStartReview } from './api';
import { availableActions } from './reviewActions';

// Backend RejectLicenseApplicationRequestValidator ile aynı sınırlar.
export const rejectSchema = z.object({
  reason: z.string().trim().min(1, t.review.reasonRequired).min(10, t.review.reasonTooShort).max(1000, t.review.reasonTooLong),
});

export function ReviewActions({ application, currentUserId }: { application: LicenseApplicationDto; currentUserId: string }) {
  const notify = useNotify();
  const actions = availableActions(application, currentUserId);
  const startReview = useStartReview();
  const approve = useApproveApplication();
  const [dialog, setDialog] = useState<'approve' | 'reject' | null>(null);

  const disabledReasons = [...new Set(Object.values(actions).flatMap((a) => (a.enabled ? [] : [a.reason])))];

  const onStartReview = () =>
    startReview.mutate(application.id, {
      onSuccess: () => notify(t.review.done.startReview),
      onError: (error) => notify(userMessage(error), 'error'),
    });

  const onApprove = () =>
    approve.mutate(application.id, {
      onSuccess: () => {
        setDialog(null);
        notify(t.review.done.approve);
      },
      onError: (error) => notify(userMessage(error), 'error'),
    });

  return (
    <Box className="no-print">
      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
        {/* Gönderim sürerken buton pasif: çift tıklama iki istek göndermez. */}
        <Button variant="outlined" onClick={onStartReview} disabled={!actions.startReview.enabled || startReview.isPending}>
          {t.review.startReview}
        </Button>
        <Button variant="contained" color="success" onClick={() => setDialog('approve')} disabled={!actions.approve.enabled}>
          {t.review.approve}
        </Button>
        <Button variant="outlined" color="error" onClick={() => setDialog('reject')} disabled={!actions.reject.enabled}>
          {t.review.reject}
        </Button>
      </Stack>
      {/* Pasif butonun nedeni görünür metin olarak (tooltip, pasif butonda klavyeyle açılamaz). */}
      {disabledReasons.map((reason) => (
        <Typography key={reason} variant="body2" color="text.secondary" sx={{ mt: 1 }}>{reason}</Typography>
      ))}

      <ConfirmDialog
        open={dialog === 'approve'}
        title={t.review.approveConfirmTitle}
        description={t.review.approveConfirmBody}
        confirmLabel={t.review.approve}
        confirmColor="success"
        pending={approve.isPending}
        onConfirm={onApprove}
        onClose={() => setDialog(null)}
      />
      {dialog === 'reject' && (
        <RejectDialog
          applicationId={application.id}
          onClose={() => setDialog(null)}
          onDone={() => {
            setDialog(null);
            notify(t.review.done.reject);
          }}
        />
      )}
    </Box>
  );
}

export function RejectDialog({ applicationId, onClose, onDone }: { applicationId: string; onClose: () => void; onDone: () => void }) {
  const reject = useRejectApplication();
  const [formError, setFormError] = useState<string | null>(null);
  const { register, handleSubmit, setError, formState: { errors } } = useForm({
    resolver: zodResolver(rejectSchema),
    defaultValues: { reason: '' },
  });

  const onConfirm = handleSubmit(({ reason }) => {
    setFormError(null);
    reject.mutate(
      { id: applicationId, reason },
      {
        onSuccess: onDone,
        onError: (error) => {
          if (!applyFieldErrors(error, setError, ['reason'])) setFormError(userMessage(error));
        },
      },
    );
  });

  return (
    <ConfirmDialog
      open
      title={t.review.rejectTitle}
      confirmLabel={t.review.reject}
      confirmColor="error"
      pending={reject.isPending}
      onConfirm={() => void onConfirm()}
      onClose={onClose}
    >
      {formError && <Alert severity="error" sx={{ mb: 2 }}>{formError}</Alert>}
      <TextField
        label={t.review.reasonLabel}
        multiline
        minRows={3}
        autoFocus
        required
        {...register('reason')}
        error={!!errors.reason}
        helperText={errors.reason?.message ?? t.review.reasonHelp}
        slotProps={{ htmlInput: { maxLength: 1000 } }}
      />
    </ConfirmDialog>
  );
}
