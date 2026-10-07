import type {
  AuditLogQuery,
  FlightLogQuery,
  LicenseApplicationQuery,
  LicenseQuery,
  TrainingRecordQuery,
} from './types';

/**
 * TanStack Query anahtarları tek yerde. Hiyerarşik yapı sayesinde bir mutation sonrası
 * `invalidateQueries({ queryKey: queryKeys.applications.all })` tüm başvuru listelerini ve detaylarını tazeler.
 */
export const queryKeys = {
  trainings: {
    all: ['trainings'] as const,
    list: (params: TrainingRecordQuery) => ['trainings', 'list', params] as const,
  },
  applications: {
    all: ['applications'] as const,
    list: (params: LicenseApplicationQuery) => ['applications', 'list', params] as const,
    detail: (id: string) => ['applications', 'detail', id] as const,
  },
  licenses: {
    all: ['licenses'] as const,
    list: (params: LicenseQuery) => ['licenses', 'list', params] as const,
    qr: (id: string) => ['licenses', 'qr', id] as const,
  },
  flightLogs: {
    all: ['flightLogs'] as const,
    list: (params: FlightLogQuery) => ['flightLogs', 'list', params] as const,
  },
  pilotSummary: (pilotId: string) => ['pilotSummary', pilotId] as const,
  audit: {
    all: ['audit'] as const,
    list: (params: AuditLogQuery) => ['audit', 'list', params] as const,
  },
  verify: (code: string) => ['verify', code] as const,
};
