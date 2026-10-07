import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type { VerificationResultDto } from '@/api/types';

/** Backend: LicenseDocument.CodeLength ve alfabe (karışan karakterler yok). */
export const VERIFICATION_CODE_PATTERN = /^[A-HJ-NP-TV-Z2-9]{26}$/;

export function useVerification(code: string | undefined) {
  return useQuery({
    queryKey: queryKeys.verify(code ?? ''),
    queryFn: async ({ signal }) =>
      (await api.get<VerificationResultDto>(`/verify/${encodeURIComponent(code!)}`, { signal })).data,
    enabled: !!code && VERIFICATION_CODE_PATTERN.test(code),
    // Doğrulama sonucu önbelleğe alınmaz: lisans az önce iptal edildiyse bunu hemen göstermeli.
    staleTime: 0,
    gcTime: 0,
    retry: false,
  });
}
