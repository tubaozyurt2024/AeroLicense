import { Alert, AlertTitle, Box, Button } from '@mui/material';
import { Navigate, Link as RouterLink, isRouteErrorResponse, useRouteError } from 'react-router';
import { useAuth } from '@/features/auth/AuthContext';
import { t } from '@/i18n';
import { homePath } from './access';

export function ForbiddenPage() {
  return (
    <Alert severity="warning" action={<Button component={RouterLink} to="/">{t.errors.goHome}</Button>}>
      <AlertTitle>{t.errors.forbiddenTitle}</AlertTitle>
      {t.errors.forbiddenBody}
    </Alert>
  );
}

export function NotFoundPage() {
  return (
    <Box sx={{ maxWidth: 560, mx: 'auto', mt: 8, px: 2 }}>
      <Alert severity="info" action={<Button component={RouterLink} to="/">{t.errors.goHome}</Button>}>
        {t.errors.notFoundTitle}
      </Alert>
    </Box>
  );
}

/** "/" adresi: kullanıcıyı rolünün açılış sayfasına yönlendirir. */
export function HomeRedirect() {
  const { user } = useAuth();
  return <Navigate to={user ? homePath(user.role) : '/login'} replace />;
}

/**
 * Router seviyesinde yakalanan hatalar (ör. yeni sürüm yayınlandıktan sonra eski bir lazy chunk'ın
 * bulunamaması). Teknik detay gösterilmez; kullanıcıya yenileme önerilir.
 */
export function RouteErrorPage() {
  const error = useRouteError();
  if (isRouteErrorResponse(error) && error.status === 404) return <NotFoundPage />;
  console.error(error);
  return (
    <Box role="alert" sx={{ maxWidth: 560, mx: 'auto', mt: 8, px: 2 }}>
      <Alert severity="error" action={<Button onClick={() => window.location.reload()}>{t.errors.reload}</Button>}>
        <AlertTitle>{t.errors.boundaryTitle}</AlertTitle>
        {t.errors.boundaryBody}
      </Alert>
    </Box>
  );
}
