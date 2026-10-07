/**
 * Backend DTO'ları: ELLE YAZILMAZ. schema.d.ts, `npm run gen:api` ile backend'in OpenAPI şemasından üretilir;
 * burada sadece okunabilir takma adlar var. Backend bir alanı değiştirirse tip üretimi sonrası derleme hatası alınır.
 */
import type { components, paths } from './schema';

type Schemas = components['schemas'];

export type UserRole = Schemas['UserRole'];
export type LicenseType = Schemas['LicenseType'];
export type ApplicationStatus = Schemas['ApplicationStatus'];
export type LicenseStatus = Schemas['LicenseStatus'];
export type VerificationStatus = Schemas['VerificationStatus'];
export type SuspicionReason = Schemas['SuspicionReason'];
export type ApplicationSortField = Schemas['ApplicationSortField'];
export type SortDirection = Schemas['SortDirection'];

export type LoginRequest = Schemas['LoginRequest'];
export type LoginResponse = Schemas['LoginResponse'];
export type CurrentUser = Schemas['CurrentUserResponse'];

export type TrainingRecordDto = Schemas['TrainingRecordDto'];
export type CreateTrainingRecordRequest = Schemas['CreateTrainingRecordRequest'];
export type LicenseApplicationDto = Schemas['LicenseApplicationDto'];
export type CreateLicenseApplicationRequest = Schemas['CreateLicenseApplicationRequest'];
export type RejectLicenseApplicationRequest = Schemas['RejectLicenseApplicationRequest'];
export type LicenseDto = Schemas['LicenseDto'];
export type VerificationResultDto = Schemas['VerificationResultDto'];
export type FlightLogDto = Schemas['FlightLogDto'];
export type FlightLogListItemDto = Schemas['FlightLogListItemDto'];
export type CreateFlightLogRequest = Schemas['CreateFlightLogRequest'];
export type PilotSummaryDto = Schemas['PilotSummaryDto'];
export type AuditLogDto = Schemas['AuditLogDto'];
export type ProblemDetails = Schemas['ProblemDetails'];

export type TrainingRecordPage = Schemas['TrainingRecordDtoPagedResult'];
export type LicenseApplicationPage = Schemas['LicenseApplicationDtoPagedResult'];
export type LicensePage = Schemas['LicenseDtoPagedResult'];
export type FlightLogPage = Schemas['FlightLogListItemDtoPagedResult'];
export type AuditLogPage = Schemas['AuditLogDtoPagedResult'];

// Sorgu parametreleri de şemadan (ASP.NET model binding büyük/küçük harfe duyarsız; şemadaki adlar kullanılır).
type QueryOf<P extends keyof paths> = paths[P] extends { get: { parameters: { query?: infer Q } } } ? NonNullable<Q> : never;
export type TrainingRecordQuery = QueryOf<'/api/v1/training-records'>;
export type LicenseApplicationQuery = QueryOf<'/api/v1/license-applications'>;
export type LicenseQuery = QueryOf<'/api/v1/licenses'>;
export type FlightLogQuery = QueryOf<'/api/v1/flight-logs'>;
export type AuditLogQuery = QueryOf<'/api/v1/audit-logs'>;

// `satisfies`: backend yeni bir tür eklerse ya da adı değişirse burada derleme hatası alınır.
export const LICENSE_TYPES = ['PPL', 'CPL', 'ATPL'] as const satisfies readonly LicenseType[];
export const PASSING_SCORE = 70; // backend: TrainingRecord.PassingScore
