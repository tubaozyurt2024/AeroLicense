import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, MenuItem, Paper, Stack, TextField } from '@mui/material';
import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { z } from 'zod';
import { applyFieldErrors, userMessage } from '@/api/problem';
import { LICENSE_TYPES, PASSING_SCORE } from '@/api/types';
import { useNotify } from '@/app/Notifications';
import { PageHeader } from '@/components/PageHeader';
import { t } from '@/i18n';
import { istanbulDateToUtcIso, todayInIstanbul } from '@/utils/format';
import { useCreateTrainingRecord } from './api';

// Backend CreateTrainingRecordRequestValidator ile aynı kurallar.
export const trainingRecordSchema = z.object({
  applicantNationalId: z.string().trim().regex(/^[1-9][0-9]{10}$/, t.trainings.nationalIdInvalid),
  licenseType: z.enum(LICENSE_TYPES),
  completedAt: z
    .string()
    .min(1, t.trainings.dateRequired)
    .refine((d) => d <= todayInIstanbul(), t.trainings.dateFuture),
  examScore: z.number({ error: t.trainings.scoreRange }).int(t.trainings.scoreRange).min(0, t.trainings.scoreRange).max(100, t.trainings.scoreRange),
});

type FormValues = z.input<typeof trainingRecordSchema>;

export function TrainingRecordForm() {
  const navigate = useNavigate();
  const notify = useNotify();
  const create = useCreateTrainingRecord();
  const [formError, setFormError] = useState<string | null>(null);

  const { register, control, handleSubmit, setError, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(trainingRecordSchema),
    defaultValues: { applicantNationalId: '', licenseType: 'PPL', completedAt: '' },
  });
  const score = useWatch({ control, name: 'examScore' });
  const failing = typeof score === 'number' && !Number.isNaN(score) && score >= 0 && score < PASSING_SCORE;

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    create.mutate(
      {
        applicantNationalId: values.applicantNationalId.trim(),
        licenseType: values.licenseType,
        completedAtUtc: istanbulDateToUtcIso(values.completedAt),
        examScore: values.examScore,
      },
      {
        onSuccess: () => {
          notify(t.trainings.saved);
          navigate('/training-records');
        },
        onError: (error) => {
          // Backend'in alan adı "CompletedAtUtc"; formdaki karşılığı "completedAt".
          if (!applyFieldErrors(error, setError, ['applicantNationalId', 'licenseType', 'examScore'])) setFormError(userMessage(error));
        },
      },
    );
  });

  return (
    <>
      <PageHeader title={t.trainings.newTitle} />
      <Paper sx={{ p: { xs: 2, md: 3 }, maxWidth: 640 }}>
        {formError && <Alert severity="error" sx={{ mb: 2 }} role="alert">{formError}</Alert>}
        <Box component="form" onSubmit={onSubmit} noValidate>
          <Stack spacing={2}>
            <TextField
              label={t.trainings.nationalId}
              required
              inputMode="numeric"
              autoComplete="off"
              {...register('applicantNationalId')}
              error={!!errors.applicantNationalId}
              helperText={errors.applicantNationalId?.message ?? t.trainings.nationalIdHelp}
              slotProps={{ htmlInput: { maxLength: 11 } }}
            />
            <Controller
              name="licenseType"
              control={control}
              render={({ field }) => (
                <TextField select label={t.trainings.licenseType} required {...field} error={!!errors.licenseType} helperText={errors.licenseType?.message}>
                  {LICENSE_TYPES.map((type) => <MenuItem key={type} value={type}>{t.licenseTypes[type]}</MenuItem>)}
                </TextField>
              )}
            />
            <TextField
              label={t.trainings.completedAt}
              type="date"
              required
              {...register('completedAt')}
              error={!!errors.completedAt}
              helperText={errors.completedAt?.message ?? ' '}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: todayInIstanbul() } }}
            />
            <TextField
              label={t.trainings.score}
              type="number"
              required
              {...register('examScore', { valueAsNumber: true })}
              error={!!errors.examScore}
              helperText={errors.examScore?.message ?? '0–100'}
              slotProps={{ htmlInput: { min: 0, max: 100, step: 1 } }}
            />
            {/* Uyarı, hata değil: 70 altı bir sonuç da kaydedilir, ama "başarısız" sayılır. */}
            {failing && <Alert severity="warning" role="status">{t.trainings.failingWarning(PASSING_SCORE)}</Alert>}
            <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
              <Button onClick={() => navigate('/training-records')} disabled={create.isPending}>{t.common.cancel}</Button>
              <Button type="submit" variant="contained" disabled={create.isPending}>{t.common.save}</Button>
            </Stack>
          </Stack>
        </Box>
      </Paper>
    </>
  );
}
