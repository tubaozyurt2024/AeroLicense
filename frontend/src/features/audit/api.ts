import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { queryKeys } from '@/api/queryKeys';
import type { AuditLogPage, AuditLogQuery } from '@/api/types';

export function useAuditLogs(params: AuditLogQuery, options: { enabled?: boolean } = {}) {
  return useQuery({
    queryKey: queryKeys.audit.list(params),
    queryFn: async ({ signal }) => (await api.get<AuditLogPage>('/audit-logs', { params, signal })).data,
    placeholderData: keepPreviousData,
    enabled: options.enabled ?? true,
  });
}
