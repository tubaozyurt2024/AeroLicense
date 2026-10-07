import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '@/api/problem';

/**
 * Yeniden deneme politikası: 4xx istemci hatasıdır, tekrar denemek sonucu değiştirmez (401/403/404/422).
 * Sadece ağ hatası ve 5xx en fazla 2 kez denenir. Mutation'lar (POST) ASLA otomatik tekrar denenmez:
 * idempotent olmayan bir işlem iki kez yapılabilirdi.
 */
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          const status = error instanceof ApiError ? error.status : 0;
          return (status === 0 || status >= 500) && failureCount < 2;
        },
      },
      mutations: { retry: false },
    },
  });
}
