import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type { LicensePage, LicenseQuery } from '@/api/types';

export function useLicenses(params: LicenseQuery) {
  return useQuery({
    queryKey: queryKeys.licenses.list(params),
    queryFn: async ({ signal }) => (await api.get<LicensePage>('/licenses', { params, signal })).data,
  });
}

/**
 * QR görseli yetkili bir istekle alınır (<img src> Authorization başlığı gönderemez). PNG küçük (~600 bayt)
 * olduğu için data: URL'e çevrilir: object URL gibi serbest bırakılması (revoke) gereken bir kaynak yok.
 */
export function useLicenseQr(licenseId: string) {
  const query = useQuery({
    queryKey: queryKeys.licenses.qr(licenseId),
    queryFn: async ({ signal }) => {
      const blob = (await api.get<Blob>(`/licenses/${licenseId}/qr`, { responseType: 'blob', signal })).data;
      return toDataUrl(blob);
    },
    staleTime: Infinity,
  });
  return { url: query.data, isLoading: query.isLoading, error: query.error };
}

const toDataUrl = (blob: Blob) =>
  new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error ?? new Error('QR okunamadı'));
    reader.readAsDataURL(blob);
  });
