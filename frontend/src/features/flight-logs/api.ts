import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type { CreateFlightLogRequest, FlightLogDto, FlightLogPage, FlightLogQuery, PilotSummaryDto } from '@/api/types';

export function usePilotSummary(pilotId: string) {
  return useQuery({
    queryKey: queryKeys.pilotSummary(pilotId),
    queryFn: async ({ signal }) => (await api.get<PilotSummaryDto>(`/pilots/${pilotId}/summary`, { signal })).data,
  });
}

export function useFlightLogs(params: FlightLogQuery) {
  return useQuery({
    queryKey: queryKeys.flightLogs.list(params),
    queryFn: async ({ signal }) => (await api.get<FlightLogPage>('/flight-logs', { params, signal })).data,
    placeholderData: keepPreviousData,
  });
}

export type SubmitFlightLogResult = { flightLog: FlightLogDto; replayed: boolean; status: number };

/**
 * Idempotency anahtarı çağıran tarafından verilir (bu hook üretmez): anahtar "HTTP isteği" başına değil
 * "mantıksal işlem" başına olmalı. Ağ hatasında aynı işlem tekrar gönderilince aynı anahtar kullanılır.
 */
export function useSubmitFlightLog() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ body, idempotencyKey }: { body: CreateFlightLogRequest; idempotencyKey: string }) => {
      const response = await api.post<FlightLogDto>('/flight-logs', body, { headers: { 'Idempotency-Key': idempotencyKey } });
      // Başlık, backend CORS ayarında "exposed" olduğu için tarayıcıdan okunabilir.
      const replayed = response.headers['idempotent-replayed'] === 'true';
      return { flightLog: response.data, replayed, status: response.status } satisfies SubmitFlightLogResult;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.flightLogs.all }),
  });
}
