import { Alert, Snackbar } from '@mui/material';
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

type Severity = 'success' | 'info' | 'warning' | 'error';
type Notify = (message: string, severity?: Severity) => void;

const NotifyContext = createContext<Notify | null>(null);

/** İşlem sonucu bildirimi (snackbar). role="status"/"alert" ile ekran okuyuculara da duyurulur. */
export function NotificationProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ message: string; severity: Severity; key: number } | null>(null);
  const notify = useCallback<Notify>((message, severity = 'success') => {
    setState({ message, severity, key: Date.now() });
  }, []);
  const value = useMemo(() => notify, [notify]);

  return (
    <NotifyContext.Provider value={value}>
      {children}
      <Snackbar
        key={state?.key}
        open={state !== null}
        autoHideDuration={6000}
        onClose={(_, reason) => reason !== 'clickaway' && setState(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {state ? (
          <Alert severity={state.severity} variant="filled" onClose={() => setState(null)}>
            {state.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </NotifyContext.Provider>
  );
}

export function useNotify(): Notify {
  const notify = useContext(NotifyContext);
  if (!notify) throw new Error('useNotify, NotificationProvider içinde kullanılmalı');
  return notify;
}
