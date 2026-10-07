import { GlobalStyles as MuiGlobalStyles } from '@mui/material';

/** Uygulama geneli: "içeriğe geç" bağlantısı (klavye kullanıcıları) ve yazdırma görünümü. */
export function AppGlobalStyles() {
  return (
    <MuiGlobalStyles
      styles={(theme) => ({
        '.skip-link': {
          position: 'absolute',
          left: 8,
          top: -48,
          zIndex: theme.zIndex.tooltip,
          padding: '8px 16px',
          background: theme.palette.background.paper,
          color: theme.palette.text.primary,
          border: `2px solid ${theme.palette.primary.main}`,
          borderRadius: 4,
          '&:focus': { top: 8 },
        },
        '@media print': {
          '.no-print': { display: 'none !important' },
          body: { background: '#fff !important' },
          main: { padding: '0 !important' },
        },
      })}
    />
  );
}
