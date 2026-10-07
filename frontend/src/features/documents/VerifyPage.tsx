import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorIcon from '@mui/icons-material/Error';
import HelpIcon from '@mui/icons-material/Help';
import WarningIcon from '@mui/icons-material/Warning';
import { Alert, AlertTitle, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState, type FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router';
import { toApiError } from '@/api/problem';
import type { VerificationResultDto, VerificationStatus } from '@/api/types';
import { ErrorState } from '@/components/ErrorState';
import { LoadingState } from '@/components/LoadingState';
import { t } from '@/i18n';
import { formatDate, formatDateTime } from '@/utils/format';
import { VERIFICATION_CODE_PATTERN, useVerification } from './api';

const severity: Record<VerificationStatus, 'success' | 'warning' | 'error'> = {
  Valid: 'success',
  Expired: 'warning',
  Revoked: 'error',
  Tampered: 'error',
};
const icons: Record<VerificationStatus, React.ReactNode> = {
  Valid: <CheckCircleIcon fontSize="large" />,
  Expired: <WarningIcon fontSize="large" />,
  Revoked: <ErrorIcon fontSize="large" />,
  Tampered: <ErrorIcon fontSize="large" />,
};

/**
 * Girişsiz belge doğrulama (QR'daki adres). Sadece backend'in döndüğü minimum bilgi gösterilir:
 * durum, lisans no, tür, maskeli ad, bitiş tarihi. "Değiştirilmiş" durumunda içerik alanları gelmez.
 */
export function VerifyPage() {
  const { code: rawCode } = useParams();
  const code = rawCode?.trim().toUpperCase();
  const navigate = useNavigate();
  const [input, setInput] = useState(code ?? '');
  const [inputError, setInputError] = useState<string | null>(null);
  const query = useVerification(code);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    const value = input.trim().toUpperCase();
    if (!VERIFICATION_CODE_PATTERN.test(value)) {
      setInputError(t.verify.invalidFormat);
      return;
    }
    setInputError(null);
    navigate(`/verify/${value}`);
  };

  const formatInvalid = !!code && !VERIFICATION_CODE_PATTERN.test(code);
  const notFound = formatInvalid || (query.isError && toApiError(query.error).status === 404);

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h1" gutterBottom>{t.verify.title}</Typography>
        <Typography color="text.secondary">{t.verify.subtitle}</Typography>
      </Box>

      {/* aria-live: sonuç geldiğinde ekran okuyucu duyurur. */}
      <Box aria-live="polite">
        {query.isFetching && <LoadingState />}
        {notFound && (
          <Alert severity="error" icon={<HelpIcon fontSize="large" />}>
            <AlertTitle>{t.verify.notFoundTitle}</AlertTitle>
            {t.verify.notFoundBody}
          </Alert>
        )}
        {query.isError && !notFound && <ErrorState error={query.error} onRetry={() => void query.refetch()} />}
        {query.data && <VerificationResult result={query.data} />}
      </Box>

      <Paper component="form" onSubmit={onSubmit} noValidate sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ alignItems: { sm: 'flex-start' } }}>
          <TextField
            label={t.verify.codeLabel}
            value={input}
            onChange={(e) => setInput(e.target.value.toUpperCase())}
            error={!!inputError}
            helperText={inputError ?? ' '}
            slotProps={{ htmlInput: { maxLength: 26, autoComplete: 'off', spellCheck: false, style: { fontFamily: 'monospace' } } }}
          />
          <Button type="submit" variant="contained" sx={{ height: 56, flexShrink: 0 }}>{t.verify.submit}</Button>
        </Stack>
      </Paper>

      <Typography variant="body2" color="text.secondary">{t.verify.privacy}</Typography>
    </Stack>
  );
}

function VerificationResult({ result }: { result: VerificationResultDto }) {
  const rows: [string, string][] = result.licenseNumber
    ? [
        [t.verify.licenseNumber, result.licenseNumber],
        [t.verify.licenseType, result.licenseType ? t.licenseTypes[result.licenseType] : '—'],
        [t.verify.holder, result.holderNameMasked ?? '—'],
        [t.verify.expiresAt, formatDate(result.expiresAtUtc)],
      ]
    : [];

  return (
    <Alert severity={severity[result.status]} icon={icons[result.status]} role="status">
      <AlertTitle sx={{ fontSize: '1.25rem' }}>{t.status.verification[result.status]}</AlertTitle>
      <Typography>{t.verify.explanation[result.status]}</Typography>
      <Box component="dl" sx={{ display: 'grid', gridTemplateColumns: 'max-content 1fr', columnGap: 2, rowGap: 0.5, mt: 2, mb: 0 }}>
        {rows.map(([label, value]) => (
          <Box key={label} sx={{ display: 'contents' }}>
            <Box component="dt" sx={{ color: 'text.secondary' }}>{label}</Box>
            <Box component="dd" sx={{ m: 0, wordBreak: 'break-word' }}>{value}</Box>
          </Box>
        ))}
        <Box component="dt" sx={{ color: 'text.secondary' }}>{t.verify.checkedAt}</Box>
        <Box component="dd" sx={{ m: 0 }}>{formatDateTime(result.checkedAtUtc)}</Box>
      </Box>
    </Alert>
  );
}
