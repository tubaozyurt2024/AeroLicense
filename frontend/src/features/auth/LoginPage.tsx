import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Button, Link, List, ListItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, Link as RouterLink, useLocation, useNavigate } from 'react-router';
import { z } from 'zod';
import { applyFieldErrors, userMessage } from '@/api/problem';
import { homePath } from '@/app/access';
import { t } from '@/i18n';
import { useAuth } from './AuthContext';

// Kurallar backend'deki LoginRequestValidator ile aynı. İstemci validasyonu sadece hızlı geri bildirim içindir;
// asıl doğrulama sunucuda (istemci atlatılabilir).
export const loginSchema = z.object({
  email: z.string().trim().min(1, t.auth.emailRequired).pipe(z.email(t.auth.emailInvalid)),
  password: z.string().min(1, t.auth.passwordRequired).max(128, t.auth.passwordTooLong),
});

const DEMO_USERS = ['pilot1', 'inspector', 'egitim1', 'havayolu1'].map((u) => `${u}@aerolicense.test`);

type LocationState = { from?: string; expired?: boolean } | null;

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const state = useLocation().state as LocationState;
  const [formError, setFormError] = useState<string | null>(null);

  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  if (user) return <Navigate to={homePath(user.role)} replace />;

  const onSubmit = handleSubmit(async ({ email, password }) => {
    setFormError(null);
    try {
      const me = await login(email, password);
      navigate(state?.from && state.from !== '/login' ? state.from : homePath(me.role), { replace: true });
    } catch (error) {
      if (!applyFieldErrors(error, setError, ['email', 'password'])) setFormError(userMessage(error));
    }
  });

  return (
    <Paper sx={{ p: { xs: 3, sm: 4 } }}>
      <Typography variant="h1" gutterBottom>{t.auth.title}</Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>{t.auth.subtitle}</Typography>

      {state?.expired && <Alert severity="info" sx={{ mb: 2 }}>{t.auth.sessionExpired}</Alert>}
      {formError && <Alert severity="error" sx={{ mb: 2 }} role="alert">{formError}</Alert>}

      {/* noValidate: tarayıcının kendi (dile göre değişen) balonları yerine tutarlı Zod mesajları. */}
      <Box component="form" onSubmit={onSubmit} noValidate>
        <Stack spacing={2}>
          <TextField
            label={t.auth.email}
            type="email"
            autoComplete="username"
            autoFocus
            {...register('email')}
            error={!!errors.email}
            helperText={errors.email?.message}
          />
          <TextField
            label={t.auth.password}
            type="password"
            autoComplete="current-password"
            {...register('password')}
            error={!!errors.password}
            helperText={errors.password?.message}
          />
          <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
            {isSubmitting ? t.auth.submitting : t.auth.submit}
          </Button>
        </Stack>
      </Box>

      <Link component={RouterLink} to="/verify" sx={{ display: 'inline-block', mt: 3 }}>{t.auth.verifyLink}</Link>

      <Box component="details" sx={{ mt: 3, color: 'text.secondary' }}>
        <summary>{t.auth.demoUsers}</summary>
        <List dense>
          {DEMO_USERS.map((email) => <ListItem key={email} sx={{ fontFamily: 'monospace' }}>{email}</ListItem>)}
        </List>
        <Typography variant="caption">{t.auth.demoHint}</Typography>
      </Box>
    </Paper>
  );
}
