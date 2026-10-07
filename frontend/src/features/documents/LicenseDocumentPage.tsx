import PrintIcon from '@mui/icons-material/Print';
import { Box, Button, Paper, Skeleton, Stack, Typography } from '@mui/material';
import type { LicenseDto } from '@/api/types';
import { DefinitionList } from '@/components/DefinitionList';
import { EmptyState } from '@/components/EmptyState';
import { ErrorState } from '@/components/ErrorState';
import { LoadingState } from '@/components/LoadingState';
import { PageHeader } from '@/components/PageHeader';
import { StatusChip } from '@/components/StatusChip';
import { t } from '@/i18n';
import { formatDate } from '@/utils/format';
import { useLicenseQr, useLicenses } from './licensesApi';

/** Lisans belgesi: QR kodlu, yazdırılabilir. Yazdırmada menü/butonlar gizlenir (.no-print). */
export function LicenseDocumentPage() {
  const query = useLicenses({ PageSize: 20 });

  if (query.isLoading) return <LoadingState />;
  if (query.error || !query.data) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;

  return (
    <>
      <PageHeader
        title={t.licenses.title}
        actions={query.data.items.length > 0 && (
          <Button variant="outlined" startIcon={<PrintIcon />} onClick={() => window.print()}>{t.common.print}</Button>
        )}
      />
      {query.data.items.length === 0 ? (
        <EmptyState message={t.licenses.none} />
      ) : (
        <Stack spacing={3}>{query.data.items.map((license) => <LicenseDocument key={license.id} license={license} />)}</Stack>
      )}
    </>
  );
}

function LicenseDocument({ license }: { license: LicenseDto }) {
  const qr = useLicenseQr(license.id);
  return (
    <Paper component="article" aria-label={`${t.licenses.documentTitle} ${license.licenseNumber}`} sx={{ p: { xs: 2, md: 4 }, breakInside: 'avoid' }}>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, justifyContent: 'space-between' }}>
        <Box sx={{ flex: '1 1 320px' }}>
          <Typography variant="overline" color="text.secondary">{t.app.authority}</Typography>
          <Typography variant="h2" component="h2" gutterBottom>{t.licenses.documentTitle}</Typography>
          <DefinitionList
            items={[
              [t.licenses.number, <strong key="n">{license.licenseNumber}</strong>],
              [t.licenses.type, t.licenseTypes[license.type]],
              [t.licenses.holder, license.holderName],
              [t.licenses.issuedAt, formatDate(license.issuedAtUtc)],
              [t.licenses.expiresAt, formatDate(license.expiresAtUtc)],
              [t.licenses.status, <StatusChip key="s" kind="license" status={license.status} />],
            ]}
          />
        </Box>
        <Box sx={{ textAlign: 'center', width: 200 }}>
          {qr.url ? (
            <img src={qr.url} alt={t.licenses.qrAlt(license.licenseNumber)} width={200} height={200} />
          ) : qr.error ? (
            <ErrorState error={qr.error} />
          ) : (
            <Skeleton variant="rectangular" width={200} height={200} />
          )}
          {license.verificationUrl && (
            <Typography variant="caption" component="p" sx={{ wordBreak: 'break-all', mt: 1 }}>
              {t.licenses.verifyAt}: {license.verificationUrl}
            </Typography>
          )}
        </Box>
      </Box>
    </Paper>
  );
}
