import { Box, CircularProgress } from '@mui/material';
import { t } from '@/i18n';

export function LoadingState() {
  return (
    <Box role="status" aria-live="polite" sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
      <CircularProgress aria-label={t.app.loading} />
    </Box>
  );
}
