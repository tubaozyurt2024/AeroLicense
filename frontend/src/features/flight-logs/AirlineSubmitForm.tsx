import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, AlertTitle, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { applyFieldErrors, userMessage } from '@/api/problem';
import type { CreateFlightLogRequest } from '@/api/types';
import { PageHeader } from '@/components/PageHeader';
import { t } from '@/i18n';
import { formatDuration, istanbulLocalToUtcIso } from '@/utils/format';
import { useSubmitFlightLog, type SubmitFlightLogResult } from './api';

const icao = z.string().trim().toUpperCase().regex(/^[A-Z]{4}$/, t.flights.icaoInvalid);

// Backend CreateFlightLogRequestValidator ile aynı kurallar.
export const flightLogSchema = z
  .object({
    pilotLicenseNumber: z.string().trim().min(1, t.flights.required).max(32),
    flightNumber: z.string().trim().toUpperCase().regex(/^[A-Z0-9]{2,3}[0-9]{1,4}[A-Z]?$/, t.flights.flightNumberInvalid),
    departureAirport: icao,
    arrivalAirport: icao,
    departureAt: z.string().min(1, t.flights.required),
    arrivalAt: z.string().min(1, t.flights.required),
  })
  .refine((v) => v.departureAirport !== v.arrivalAirport, { path: ['arrivalAirport'], message: t.flights.sameAirport })
  .refine((v) => !v.departureAt || !v.arrivalAt || v.arrivalAt > v.departureAt, { path: ['arrivalAt'], message: t.flights.arrivalBeforeDeparture })
  .refine((v) => !v.arrivalAt || new Date(istanbulLocalToUtcIso(v.arrivalAt)) <= new Date(), { path: ['arrivalAt'], message: t.flights.future });

type FormValues = z.input<typeof flightLogSchema>;
const fields = ['pilotLicenseNumber', 'flightNumber', 'departureAirport', 'arrivalAirport', 'departureAt', 'arrivalAt'] as const;

/**
 * Test amaçlı gönderim formu. Idempotency-Key "mantıksal işlem" başına bir kez üretilir, her tıklamada değil:
 * ağ kopup kullanıcı tekrar gönderirse sunucu aynı anahtarı görür ve ikinci kayıt oluşturmaz.
 * "Yeni kayıt" yeni bir işlem başlatır → yeni anahtar.
 */
export function AirlineSubmitForm() {
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [lastBody, setLastBody] = useState<CreateFlightLogRequest | null>(null);
  const [result, setResult] = useState<SubmitFlightLogResult | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const submit = useSubmitFlightLog();

  const { register, handleSubmit, setError, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(flightLogSchema),
    defaultValues: { pilotLicenseNumber: '', flightNumber: '', departureAirport: '', arrivalAirport: '', departureAt: '', arrivalAt: '' },
  });

  const send = (body: CreateFlightLogRequest) => {
    setFormError(null);
    setLastBody(body);
    submit.mutate(
      { body, idempotencyKey },
      {
        onSuccess: setResult,
        onError: (error) => {
          setResult(null);
          if (!applyFieldErrors(error, setError, fields)) setFormError(userMessage(error));
        },
      },
    );
  };

  const onSubmit = handleSubmit((v) =>
    send({
      pilotLicenseNumber: v.pilotLicenseNumber.trim(),
      flightNumber: v.flightNumber.trim().toUpperCase(),
      departureAirport: v.departureAirport.trim().toUpperCase(),
      arrivalAirport: v.arrivalAirport.trim().toUpperCase(),
      departureAtUtc: istanbulLocalToUtcIso(v.departureAt),
      arrivalAtUtc: istanbulLocalToUtcIso(v.arrivalAt),
    }),
  );

  const startNew = () => {
    reset();
    setResult(null);
    setLastBody(null);
    setFormError(null);
    setIdempotencyKey(crypto.randomUUID());
  };

  return (
    <>
      <PageHeader title={t.flights.submitTitle} subtitle={t.flights.submitIntro} />
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', lg: '3fr 2fr' }, alignItems: 'start' }}>
        <Paper sx={{ p: { xs: 2, md: 3 } }}>
          {formError && <Alert severity="error" sx={{ mb: 2 }} role="alert">{formError}</Alert>}
          <Box component="form" onSubmit={onSubmit} noValidate>
            <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
              <TextField label={t.flights.pilotLicenseNumber} required {...register('pilotLicenseNumber')} error={!!errors.pilotLicenseNumber} helperText={errors.pilotLicenseNumber?.message ?? 'AL-CPL-2026-…'} />
              <TextField label={t.flights.flightNumber} required {...register('flightNumber')} error={!!errors.flightNumber} helperText={errors.flightNumber?.message ?? 'XDA123'} />
              <TextField label={t.flights.departureAirport} required {...register('departureAirport')} error={!!errors.departureAirport} helperText={errors.departureAirport?.message ?? 'LTFM'} slotProps={{ htmlInput: { maxLength: 4, style: { textTransform: 'uppercase' } } }} />
              <TextField label={t.flights.arrivalAirport} required {...register('arrivalAirport')} error={!!errors.arrivalAirport} helperText={errors.arrivalAirport?.message ?? 'LTAC'} slotProps={{ htmlInput: { maxLength: 4, style: { textTransform: 'uppercase' } } }} />
              <TextField label={t.flights.departureAt} type="datetime-local" required {...register('departureAt')} error={!!errors.departureAt} helperText={errors.departureAt?.message ?? ' '} slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label={t.flights.arrivalAt} type="datetime-local" required {...register('arrivalAt')} error={!!errors.arrivalAt} helperText={errors.arrivalAt?.message ?? ' '} slotProps={{ inputLabel: { shrink: true } }} />
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              {t.flights.idempotencyKey}: <code>{idempotencyKey}</code>
              <br />
              {t.flights.idempotencyHelp}
            </Typography>
            <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: 'wrap', gap: 1 }}>
              <Button type="submit" variant="contained" disabled={submit.isPending}>{t.flights.send}</Button>
              <Button variant="outlined" disabled={submit.isPending || !lastBody} onClick={() => lastBody && send(lastBody)}>
                {t.flights.resend}
              </Button>
              <Button onClick={startNew} disabled={submit.isPending}>{t.flights.newRecord}</Button>
            </Stack>
          </Box>
        </Paper>

        <Box aria-live="polite">
          {result && (
            <Alert severity={result.replayed ? 'info' : 'success'}>
              <AlertTitle>{t.flights.lastResult}: HTTP {result.status}</AlertTitle>
              {result.replayed ? t.flights.replayed : t.flights.created}
              <Box component="ul" sx={{ pl: 2, mb: 0 }}>
                <li>ID: <code>{result.flightLog.id}</code></li>
                <li>{result.flightLog.flightNumber} · {result.flightLog.departureAirport} → {result.flightLog.arrivalAirport}</li>
                <li>{t.flights.duration}: {formatDuration(result.flightLog.durationMinutes)}</li>
              </Box>
            </Alert>
          )}
        </Box>
      </Box>
    </>
  );
}
