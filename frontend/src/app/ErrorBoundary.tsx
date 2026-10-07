import { Alert, AlertTitle, Box, Button } from '@mui/material';
import { Component, type ErrorInfo, type ReactNode } from 'react';
import { t } from '@/i18n';

/**
 * Global Error Boundary: render sırasında beklenmeyen bir hata tüm uygulamayı beyaz ekrana düşürmesin.
 * Kullanıcıya teknik detay (stack trace) gösterilmez; hata sadece konsola (ve üretimde izleme aracına) gider.
 * Hook karşılığı olmadığı için React'te hâlâ class bileşen gerekir.
 */
export class ErrorBoundary extends Component<{ children: ReactNode }, { hasError: boolean }> {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Üretimde burası Sentry/OpenTelemetry gibi bir izleme servisine gönderilir.
    console.error(error, info.componentStack);
  }

  render() {
    if (!this.state.hasError) return this.props.children;
    return (
      <Box role="alert" sx={{ maxWidth: 560, mx: 'auto', mt: 8, px: 2 }}>
        <Alert severity="error" action={<Button onClick={() => window.location.reload()}>{t.errors.reload}</Button>}>
          <AlertTitle>{t.errors.boundaryTitle}</AlertTitle>
          {t.errors.boundaryBody}
        </Alert>
      </Box>
    );
  }
}
