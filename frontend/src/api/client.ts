import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { toApiError } from './problem';
import type { LoginResponse } from './types';

/**
 * Uygulamanın TEK HTTP istemcisi. Kimlik başlığı, correlation ID, oturum yenileme ve hata dönüştürme
 * burada bir kez yapılır; bileşenler ve hook'lar bunları bilmez.
 */
const baseURL = `${import.meta.env.VITE_API_BASE_URL ?? ''}/api/v1`;

export const api = axios.create({ baseURL, timeout: 15_000, headers: { Accept: 'application/json' } });

// --- Oturum: SADECE bellekte (localStorage/sessionStorage değil) ---------------------------------------
// XSS ile çalışan bir script localStorage'ı okuyabilir; modül değişkenine erişemez (bkz. README).
// Bedeli: sayfa yenilenince oturum düşer (prototipte kabul edilen ödünleşim).
type Session = { accessToken: string; refreshToken: string };
let session: Session | null = null;

export const sessionStore = {
  get: () => session,
  set: (value: Session) => {
    session = value;
  },
  clear: () => {
    session = null;
  },
};

// React dışındaki interceptor'ın React tarafına (navigate, state) haber vermesi için kayıtlı geri çağrılar.
type AuthHandlers = { onSessionExpired: () => void; onForbidden: () => void };
let handlers: AuthHandlers = { onSessionExpired: () => {}, onForbidden: () => {} };
export const registerAuthHandlers = (value: AuthHandlers) => {
  handlers = value;
};

// Her isteğe benzersiz ID: hata ekranındaki "destek kodu" ile backend log satırı birebir eşleşir.
const newCorrelationId = () => crypto.randomUUID().replaceAll('-', '');

api.interceptors.request.use((config) => {
  if (session) config.headers.Authorization = `Bearer ${session.accessToken}`;
  config.headers['X-Correlation-ID'] = newCorrelationId();
  return config;
});

// --- 401: sessiz yenileme (single-flight) -----------------------------------------------------------
// Access token 15 dk. Süresi dolunca aynı anda 5 istek 401 alabilir; hepsi TEK bir refresh isteğini bekler.
// Aksi halde 5 paralel refresh olur ve backend'in "tekrar kullanım tespiti" oturumu tamamen kapatır.
let refreshing: Promise<void> | null = null;

async function refreshSession(): Promise<void> {
  const current = session;
  if (!current) throw new Error('Oturum yok');
  // Interceptor'sız ham axios: refresh'in kendisi 401 alırsa sonsuz döngüye girilmez.
  const { data } = await axios.post<LoginResponse>(`${baseURL}/auth/refresh`, { refreshToken: current.refreshToken });
  session = { accessToken: data.accessToken, refreshToken: data.refreshToken };
}

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriableConfig | undefined;
    const status = error.response?.status;
    const isAuthEndpoint = config?.url?.startsWith('/auth/') ?? false;

    if (status === 401 && config && !isAuthEndpoint && session) {
      if (!config._retried) {
        config._retried = true;
        try {
          refreshing ??= refreshSession().finally(() => {
            refreshing = null;
          });
          await refreshing;
          return api(config); // yeni token'la bir kez tekrar dene
        } catch {
          // yenileme başarısız: aşağıda oturum kapatılır
        }
      }
      session = null;
      handlers.onSessionExpired();
    } else if (status === 403) {
      handlers.onForbidden();
    }

    return Promise.reject(toApiError(error));
  },
);
