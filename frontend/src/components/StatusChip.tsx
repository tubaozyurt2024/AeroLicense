import { Chip, type ChipProps } from '@mui/material';
import type { ApplicationStatus, LicenseStatus, VerificationStatus } from '@/api/types';
import { t } from '@/i18n';

type Color = ChipProps['color'];

const applicationColors: Record<ApplicationStatus, Color> = {
  Draft: 'default',
  Submitted: 'info',
  UnderReview: 'warning',
  Approved: 'success',
  Rejected: 'error',
};
const licenseColors: Record<LicenseStatus, Color> = { Active: 'success', Expired: 'warning', Revoked: 'error' };
const verificationColors: Record<VerificationStatus, Color> = {
  Valid: 'success',
  Expired: 'warning',
  Revoked: 'error',
  Tampered: 'error',
};

type Props =
  | { kind: 'application'; status: ApplicationStatus }
  | { kind: 'license'; status: LicenseStatus }
  | { kind: 'verification'; status: VerificationStatus };

/**
 * Durum rozeti. Durum sadece renkle anlatılmaz, metin her zaman görünür (renk körlüğü / WCAG 1.4.1).
 * `Record<Status, …>` sayesinde backend yeni bir durum eklerse burası derleme hatası verir.
 */
export function StatusChip(props: Props & { size?: ChipProps['size'] }) {
  const { label, color } = (() => {
    switch (props.kind) {
      case 'application':
        return { label: t.status.application[props.status], color: applicationColors[props.status] };
      case 'license':
        return { label: t.status.license[props.status], color: licenseColors[props.status] };
      case 'verification':
        return { label: t.status.verification[props.status], color: verificationColors[props.status] };
    }
  })();
  return <Chip label={label} color={color} size={props.size ?? 'small'} variant={color === 'default' ? 'outlined' : 'filled'} />;
}
