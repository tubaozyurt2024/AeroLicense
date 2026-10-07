import { AppBar, Box, Button, Container, Toolbar, Typography } from '@mui/material';
import { Suspense } from 'react';
import { Link as RouterLink, Outlet } from 'react-router';
import { LoadingState } from '@/components/LoadingState';
import { useAuth } from '@/features/auth/AuthContext';
import { t } from '@/i18n';
import { homePath } from './access';
import { ThemeToggle } from './ThemeToggle';

/** Girişsiz sayfalar (login, belge doğrulama): menü yok, sade üst çubuk. */
export function PublicLayout() {
  const { user } = useAuth();
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <a href="#main" className="skip-link">{t.app.skipToContent}</a>
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar sx={{ gap: 1 }}>
          <Box sx={{ flexGrow: 1 }}>
            <Typography component="div" sx={{ fontWeight: 700, color: 'primary.main' }}>{t.app.name}</Typography>
            <Typography variant="caption" color="text.secondary" component="div">{t.app.authority}</Typography>
          </Box>
          <ThemeToggle />
          {user && (
            <Button component={RouterLink} to={homePath(user.role)}>{t.errors.goHome}</Button>
          )}
        </Toolbar>
      </AppBar>
      <Container component="main" id="main" tabIndex={-1} maxWidth="sm" sx={{ py: { xs: 3, md: 6 }, outline: 'none' }}>
        <Suspense fallback={<LoadingState />}>
          <Outlet />
        </Suspense>
      </Container>
    </Box>
  );
}
