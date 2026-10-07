import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import type { UserRole } from '@/api/types';
import { useAuth } from '@/features/auth/AuthContext';

/**
 * Route guard. Giriş yoksa login'e (dönüş adresiyle), rol uygun değilse "yetkisiz" sayfasına yönlendirir.
 * UX içindir; güvenlik sınırı backend'dir.
 */
export function RequireRole({ roles, children }: { roles?: readonly UserRole[]; children?: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  if (roles && !roles.includes(user.role)) return <Navigate to="/yetkisiz" replace />;
  return children ?? <Outlet />;
}
