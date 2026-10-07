import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material';
import { useId, type ReactNode } from 'react';
import { t } from '@/i18n';

type Props = {
  open: boolean;
  title: string;
  description?: ReactNode;
  confirmLabel?: string;
  confirmColor?: 'primary' | 'error' | 'success';
  pending?: boolean;
  onConfirm: () => void | Promise<void>;
  onClose: () => void;
  children?: ReactNode;
};

/**
 * Geri alınamaz işlemler için onay. İşlem sürerken butonlar pasif ve diyalog kapatılamaz:
 * çift tıklama ile çift gönderim olmaz, kullanıcı sonucu görmeden pencere kapanmaz.
 */
export function ConfirmDialog({
  open, title, description, confirmLabel = t.common.confirm, confirmColor = 'primary', pending = false, onConfirm, onClose, children,
}: Props) {
  const titleId = useId();
  return (
    <Dialog open={open} onClose={pending ? undefined : onClose} aria-labelledby={titleId} fullWidth maxWidth="sm">
      <DialogTitle id={titleId}>{title}</DialogTitle>
      <DialogContent>
        {description && <DialogContentText sx={{ mb: children ? 2 : 0 }}>{description}</DialogContentText>}
        {children}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={pending}>{t.common.cancel}</Button>
        <Button variant="contained" color={confirmColor} onClick={() => void onConfirm()} disabled={pending}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
