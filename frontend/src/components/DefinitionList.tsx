import { Box } from '@mui/material';
import type { ReactNode } from 'react';

/** Etiket–değer listesi. Semantik <dl>: ekran okuyucular "terim / açıklama" olarak okur. */
export function DefinitionList({ items }: { items: [label: string, value: ReactNode][] }) {
  return (
    <Box
      component="dl"
      sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'max-content 1fr' }, columnGap: 3, rowGap: { xs: 0, sm: 1 }, m: 0 }}
    >
      {items.map(([label, value]) => (
        <Box key={label} sx={{ display: 'contents' }}>
          <Box component="dt" sx={{ color: 'text.secondary', mt: { xs: 1, sm: 0 } }}>{label}</Box>
          <Box component="dd" sx={{ m: 0, wordBreak: 'break-word' }}>{value}</Box>
        </Box>
      ))}
    </Box>
  );
}
