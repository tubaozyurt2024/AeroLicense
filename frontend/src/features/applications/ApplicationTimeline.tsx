import { Step, StepLabel, Stepper, Typography } from '@mui/material';
import type { LicenseApplicationDto } from '@/api/types';
import { t } from '@/i18n';
import { formatDateTime } from '@/utils/format';

type TimelineEvent = { key: string; label: string; at: string; error?: boolean };

/**
 * Başvuru sahibi audit log'u göremez (bu doğru bir kısıt); zaman çizelgesi DTO'daki tarihlerden türetilir.
 * Saf fonksiyon olarak ayrı: test edilebilir, bileşen sadece çizer.
 */
export function applicationEvents(a: LicenseApplicationDto): TimelineEvent[] {
  const events: TimelineEvent[] = [{ key: 'created', label: t.applications.timelineEvents.created, at: a.createdAtUtc }];
  if (a.submittedAtUtc) events.push({ key: 'submitted', label: t.applications.timelineEvents.submitted, at: a.submittedAtUtc });
  if (a.reviewStartedAtUtc)
    events.push({
      key: 'review',
      label: t.applications.timelineEvents.reviewStarted(a.reviewerName ?? '—'),
      at: a.reviewStartedAtUtc,
    });
  if (a.decidedAtUtc)
    events.push(
      a.status === 'Rejected'
        ? { key: 'decided', label: t.applications.timelineEvents.rejected, at: a.decidedAtUtc, error: true }
        : { key: 'decided', label: t.applications.timelineEvents.approved, at: a.decidedAtUtc },
    );
  return events;
}

export function ApplicationTimeline({ application }: { application: LicenseApplicationDto }) {
  const events = applicationEvents(application);
  return (
    <Stepper orientation="vertical" activeStep={events.length} aria-label={t.applications.timeline}>
      {events.map((event) => (
        <Step key={event.key} completed>
          <StepLabel
            error={event.error}
            optional={<Typography variant="caption" color="text.secondary">{formatDateTime(event.at)}</Typography>}
          >
            {event.label}
          </StepLabel>
        </Step>
      ))}
    </Stepper>
  );
}
