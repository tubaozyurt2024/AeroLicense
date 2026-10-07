import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type {
  CreateLicenseApplicationRequest, LicenseApplicationDto, LicenseApplicationPage, LicenseApplicationQuery,
  RejectLicenseApplicationRequest,
} from '@/api/types';

export function useApplications(params: LicenseApplicationQuery) {
  return useQuery({
    queryKey: queryKeys.applications.list(params),
    queryFn: async ({ signal }) => (await api.get<LicenseApplicationPage>('/license-applications', { params, signal })).data,
    // Sayfa değişirken tablo boşalıp titremesin; yeni sayfa gelene kadar eskisi gösterilir.
    placeholderData: keepPreviousData,
  });
}

export function useApplication(id: string) {
  return useQuery({
    queryKey: queryKeys.applications.detail(id),
    queryFn: async ({ signal }) => (await api.get<LicenseApplicationDto>(`/license-applications/${id}`, { signal })).data,
  });
}

/**
 * Durum değiştiren her işlem sonrası: dönen güncel DTO detay önbelleğine yazılır (ekstra GET yok),
 * listeler/audit geçersiz kılınır. Onayda lisans da oluştuğu için lisans listesi de tazelenir.
 */
function useApplicationMutation<TVariables>(request: (variables: TVariables) => Promise<LicenseApplicationDto>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: request,
    onSuccess: async (application) => {
      queryClient.setQueryData(queryKeys.applications.detail(application.id), application);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.applications.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.audit.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.licenses.all }),
      ]);
    },
  });
}

const post = async (url: string, body?: unknown) => (await api.post<LicenseApplicationDto>(url, body)).data;

export const useCreateApplication = () =>
  useApplicationMutation((body: CreateLicenseApplicationRequest) => post('/license-applications', body));

export const useSubmitApplication = () => useApplicationMutation((id: string) => post(`/license-applications/${id}/submit`));

export const useStartReview = () => useApplicationMutation((id: string) => post(`/license-applications/${id}/start-review`));

export const useApproveApplication = () => useApplicationMutation((id: string) => post(`/license-applications/${id}/approve`));

export const useRejectApplication = () =>
  useApplicationMutation(({ id, ...body }: { id: string } & RejectLicenseApplicationRequest) =>
    post(`/license-applications/${id}/reject`, body));
