import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined';
import { Box, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import { t } from '@/i18n';

export function EmptyState({ message = t.errors.empty, action }: { message?: string; action?: ReactNode }) {
  return (
    <Box sx={{ textAlign: 'center', py: 5, color: 'text.secondary' }}>
      <InboxOutlinedIcon aria-hidden fontSize="large" />
      <Typography sx={{ mt: 1 }}>{message}</Typography>
      {action && <Box sx={{ mt: 2 }}>{action}</Box>}
    </Box>
  );
}
