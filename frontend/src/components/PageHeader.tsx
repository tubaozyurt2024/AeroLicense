import { Box, Typography } from '@mui/material';
import type { ReactNode } from 'react';

/** Her sayfada tek bir <h1>: ekran okuyucu kullanıcıları başlıklarla gezinir. */
export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: ReactNode; actions?: ReactNode }) {
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 2, mb: 3 }}>
      <Box sx={{ flex: '1 1 auto', minWidth: 0 }}>
        <Typography variant="h1">{title}</Typography>
        {subtitle && (
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {actions && <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }} className="no-print">{actions}</Box>}
    </Box>
  );
}
