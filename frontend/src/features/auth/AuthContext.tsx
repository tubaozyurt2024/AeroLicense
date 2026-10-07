import { useQueryClient } from '@tanstack/react-query';
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router';
import { api, registerAuthHandlers, sessionStore } from '@/api/client';
import type { CurrentUser, LoginResponse } from '@/api/types';

type AuthContextValue = {
  user: CurrentUser | null;
  login: (email: string, password: string) => Promise<CurrentUser>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Oturum durumu. Token'lar React state'inde değil client.ts'teki bellek değişkeninde durur (interceptor'lar
 * React dışında çalışır); burada sadece "kim giriş yaptı" bilgisi (ad, rol) tutulur.
 * Router içinde render edilir çünkü oturum düşünce login sayfasına yönlendirmesi gerekir.
 */
export function AuthProvider({ children, initialUser = null }: { children: ReactNode; initialUser?: CurrentUser | null }) {
  const [user, setUser] = useState<CurrentUser | null>(initialUser);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const endSession = useCallback(() => {
    sessionStore.clear();
    setUser(null);
    // Önbellek temizlenir: aynı tarayıcıda sonraki kullanıcı öncekinin verisini (cache'ten) göremez.
    queryClient.clear();
  }, [queryClient]);

  useEffect(() => {
    registerAuthHandlers({
      onSessionExpired: () => {
        const from = window.location.pathname + window.location.search;
        endSession();
        navigate('/login', { replace: true, state: { from, expired: true } });
      },
      onForbidden: () => navigate('/yetkisiz', { replace: true }),
    });
  }, [endSession, navigate]);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await api.post<LoginResponse>('/auth/login', { email, password });
    sessionStore.set({ accessToken: data.accessToken, refreshToken: data.refreshToken });
    const { data: me } = await api.get<CurrentUser>('/auth/me');
    setUser(me);
    return me;
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = sessionStore.get()?.refreshToken;
    endSession();
    navigate('/login', { replace: true });
    // Sunucu tarafında da oturumu kapat (refresh token ailesi iptal). Başarısız olsa da kullanıcı çıkmış olur.
    if (refreshToken) await api.post('/auth/logout', { refreshToken }).catch(() => undefined);
  }, [endSession, navigate]);

  const value = useMemo(() => ({ user, login, logout }), [user, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth, AuthProvider içinde kullanılmalı');
  return value;
}

/** Giriş yapılmış sayfalarda kullanıcı her zaman vardır (RequireRole garanti eder). */
export function useCurrentUser(): CurrentUser {
  const { user } = useAuth();
  if (!user) throw new Error('Oturum yok');
  return user;
}
