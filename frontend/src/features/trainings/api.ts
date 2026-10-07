import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type { CreateTrainingRecordRequest, TrainingRecordDto, TrainingRecordPage, TrainingRecordQuery } from '@/api/types';

export function useTrainingRecords(params: TrainingRecordQuery, options: { enabled?: boolean } = {}) {
  return useQuery({
    queryKey: queryKeys.trainings.list(params),
    queryFn: async ({ signal }) => (await api.get<TrainingRecordPage>('/training-records', { params, signal })).data,
    placeholderData: keepPreviousData,
    enabled: options.enabled ?? true,
  });
}

export function useCreateTrainingRecord() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateTrainingRecordRequest) => (await api.post<TrainingRecordDto>('/training-records', body)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.trainings.all }),
  });
}
