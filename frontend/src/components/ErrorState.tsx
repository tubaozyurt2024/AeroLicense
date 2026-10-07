import { Alert, Button } from '@mui/material';
import { toApiError, userMessage } from '@/api/problem';
import { t } from '@/i18n';

/** Sorgu hatası. Kullanıcı dostu mesaj + (varsa) destek kodu; teknik detay yok. */
export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const correlationId = toApiError(error).correlationId;
  return (
    <Alert
      severity="error"
      action={onRetry && <Button color="inherit" size="small" onClick={onRetry}>{t.common.retry}</Button>}
    >
      {userMessage(error)}
      {correlationId && <div>{t.errors.supportCode(correlationId)}</div>}
    </Alert>
  );
}
