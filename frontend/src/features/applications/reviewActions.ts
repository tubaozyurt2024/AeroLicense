import type { LicenseApplicationDto } from '@/api/types';
import { t } from '@/i18n';

export type ReviewAction = 'startReview' | 'approve' | 'reject';
export type ActionState = { enabled: true } | { enabled: false; reason: string };

const enabled: ActionState = { enabled: true };

/**
 * Hangi denetçi aksiyonu şu an mümkün? Backend'deki durum makinesi ve "sadece incelemeyi üstlenen denetçi
 * karar verir" kuralının istemci tarafı yansıması. Amaç kullanıcıyı 422 almaktan korumak (UX);
 * kural yine de backend'de uygulanır, buradaki bir hata güvenlik açığı yaratmaz.
 */
export function availableActions(application: LicenseApplicationDto, currentUserId: string): Record<ReviewAction, ActionState> {
  const startReview: ActionState =
    application.status === 'Submitted' ? enabled : { enabled: false, reason: t.review.disabledReason.notSubmitted };

  const decide: ActionState =
    application.status !== 'UnderReview'
      ? { enabled: false, reason: t.review.disabledReason.notUnderReview }
      : application.reviewerId !== currentUserId
        ? { enabled: false, reason: t.review.disabledReason.notReviewer }
        : enabled;

  return { startReview, approve: decide, reject: decide };
}
