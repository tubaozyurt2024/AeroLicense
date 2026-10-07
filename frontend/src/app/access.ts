import type { UserRole } from '@/api/types';

/**
 * Hangi rol hangi sayfayı görür: route guard'ları ve menü AYNI tablodan beslenir, ikisi ayrışamaz.
 * Not: bu sadece kullanıcı deneyimi içindir. Asıl yetki kontrolü backend'de; buradaki bir hata en fazla
 * kullanıcının 403 alacağı bir sayfaya gitmesine yol açar, veriye erişim sağlamaz.
 */
export const access = {
  dashboard: ['Applicant'],
  myTrainings: ['Applicant'],
  myApplications: ['Applicant'],
  newApplication: ['Applicant'],
  flightSummary: ['Applicant'],
  myLicenses: ['Applicant'],
  trainingRecords: ['TrainingOrg'],
  newTrainingRecord: ['TrainingOrg'],
  reviewQueue: ['Inspector'],
  flightLogs: ['Inspector', 'Airline'],
  submitFlightLog: ['Airline'],
  audit: ['Inspector'],
} as const satisfies Record<string, readonly UserRole[]>;

export type AccessKey = keyof typeof access;

export const canAccess = (role: UserRole, key: AccessKey): boolean => (access[key] as readonly UserRole[]).includes(role);

/** Girişten sonra rolün açılış sayfası. */
export function homePath(role: UserRole): string {
  switch (role) {
    case 'Applicant':
      return '/dashboard';
    case 'TrainingOrg':
      return '/training-records';
    case 'Inspector':
      return '/review';
    case 'Airline':
      return '/flight-logs/new';
  }
}
