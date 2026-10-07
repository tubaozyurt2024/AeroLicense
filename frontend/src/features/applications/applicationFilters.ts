import type { ApplicationStatus } from '@/api/types';

export const APPLICATION_STATUSES: readonly ApplicationStatus[] = ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected'];

/** URL'den gelen değer güvenilmez (kullanıcı elle yazabilir): sadece bilinen durumlar kabul edilir. */
export const parseStatus = (value: string): ApplicationStatus | undefined =>
  (APPLICATION_STATUSES as readonly string[]).includes(value) ? (value as ApplicationStatus) : undefined;
