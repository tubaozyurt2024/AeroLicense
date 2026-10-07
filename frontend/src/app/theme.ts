import { createTheme } from '@mui/material/styles';

/**
 * Kurumsal ama kurgusal renkler (gerçek bir kurumun renk kimliği değil). Metin/arka plan kontrastları
 * WCAG AA'yı (normal metin ≥ 4.5:1) sağlayacak koyulukta seçildi: primary #1d4e89 beyaz üzerinde ~8:1.
 * CSS değişkenleri + "data" seçicisi: tema değişince sayfa yeniden render edilmez, sadece değişkenler değişir.
 */
export const theme = createTheme({
  cssVariables: { colorSchemeSelector: 'data' },
  colorSchemes: {
    light: {
      palette: {
        primary: { main: '#1d4e89' },
        secondary: { main: '#5a6b7d' },
        background: { default: '#f4f6f9', paper: '#ffffff' },
      },
    },
    dark: {
      palette: {
        primary: { main: '#8fb8ec' },
        secondary: { main: '#a9b6c4' },
        background: { default: '#10151c', paper: '#18202a' },
      },
    },
  },
  shape: { borderRadius: 8 },
  typography: {
    // Harici font indirilmez (gizlilik + hız): sistem fontları.
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    h1: { fontSize: '1.75rem', fontWeight: 600 },
    h2: { fontSize: '1.35rem', fontWeight: 600 },
    h3: { fontSize: '1.1rem', fontWeight: 600 },
  },
  components: {
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiPaper: { defaultProps: { variant: 'outlined' } },
    MuiTextField: { defaultProps: { fullWidth: true } },
    // Klavye kullanıcıları için belirgin odak halkası (erişilebilirlik).
    MuiButtonBase: { styleOverrides: { root: { '&.Mui-focusVisible': { outline: '2px solid currentColor', outlineOffset: 2 } } } },
  },
});
