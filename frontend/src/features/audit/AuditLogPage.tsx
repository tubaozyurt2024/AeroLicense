import { Box, Button, MenuItem, Paper, TextField, Tooltip, Typography } from '@mui/material';
import { useState } from 'react';
import type { AuditLogDto } from '@/api/types';
import { DataTable, type Column } from '@/components/DataTable';
import { PageHeader } from '@/components/PageHeader';
import { useUrlQueryState } from '@/hooks/useUrlQueryState';
import { t } from '@/i18n';
import { formatDateTime, istanbulDateToUtcIso, shortId } from '@/utils/format';
import { useAuditLogs } from './api';

const ENTITY_TYPES = Object.keys(t.audit.entityTypes) as (keyof typeof t.audit.entityTypes)[];

const columns: Column<AuditLogDto>[] = [
  { id: 'at', header: t.audit.occurredAt, render: (a) => formatDateTime(a.occurredAtUtc) },
  {
    id: 'actor',
    header: t.audit.actor,
    // E-posta backend'den maskeli gelir (KVKK); önyüz açık halini hiç görmez.
    render: (a) => (a.actorEmailMasked ? `${a.actorEmailMasked} (${a.actorRole ? t.roles[a.actorRole] : '—'})` : t.audit.system),
  },
  { id: 'action', header: t.audit.action, render: (a) => t.audit.actions[a.action] ?? a.action },
  {
    id: 'entity',
    header: t.audit.entity,
    render: (a) => (
      <Tooltip title={a.entityId}>
        <span>{(t.audit.entityTypes as Record<string, string>)[a.entityType] ?? a.entityType} · {shortId(a.entityId)}</span>
      </Tooltip>
    ),
    hideOnMobile: true,
  },
  { id: 'details', header: t.audit.details, render: (a) => a.details ?? '—', hideOnMobile: true },
  {
    id: 'correlation',
    header: t.audit.correlationId,
    render: (a) => <code style={{ fontSize: '0.75rem' }}>{a.correlationId ?? '—'}</code>,
    hideOnMobile: true,
  },
];

/** İz kayıtları (salt okunur). Filtreler URL'de; tarih aralığı Türkiye saatiyle gün başlangıcı/sonu. */
export function AuditLogPage() {
  const { page, pageSize, filters, setPage, setPageSize, setFilters } = useUrlQueryState(['entityType', 'from', 'to']);
  const [draft, setDraft] = useState(filters);

  const query = useAuditLogs({
    EntityType: filters.entityType || undefined,
    FromUtc: filters.from ? istanbulDateToUtcIso(filters.from) : undefined,
    // "Bitiş" günü dahil: ertesi günün başlangıcından 1 ms öncesi.
    ToUtc: filters.to ? new Date(new Date(istanbulDateToUtcIso(filters.to)).getTime() + 86_400_000 - 1).toISOString() : undefined,
    Page: page,
    PageSize: pageSize,
  });

  return (
    <>
      <PageHeader title={t.audit.title} subtitle={t.audit.maskedNote} />
      <Paper sx={{ p: 2, mb: 2 }}>
        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            setFilters(draft);
          }}
          sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '2fr 1fr 1fr auto auto' }, alignItems: 'center' }}
        >
          <TextField select size="small" label={t.audit.entityType} value={draft.entityType} onChange={(e) => setDraft({ ...draft, entityType: e.target.value })}>
            <MenuItem value="">{t.common.all}</MenuItem>
            {ENTITY_TYPES.map((type) => <MenuItem key={type} value={type}>{t.audit.entityTypes[type]}</MenuItem>)}
          </TextField>
          <TextField size="small" type="date" label={t.audit.from} value={draft.from} onChange={(e) => setDraft({ ...draft, from: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" label={t.audit.to} value={draft.to} onChange={(e) => setDraft({ ...draft, to: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
          <Button type="submit" variant="contained">{t.common.filter}</Button>
          <Button
            onClick={() => {
              const empty = { entityType: '', from: '', to: '' };
              setDraft(empty);
              setFilters(empty);
            }}
          >
            {t.common.clear}
          </Button>
        </Box>
      </Paper>
      <DataTable
        caption={t.audit.title}
        columns={columns}
        rows={query.data?.items}
        getRowId={(a) => a.id}
        isLoading={query.isLoading}
        error={query.error}
        onRetry={() => void query.refetch()}
        pagination={{ page, pageSize, totalCount: query.data?.totalCount ?? 0, onPageChange: setPage, onPageSizeChange: setPageSize }}
      />
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>{t.audit.maskedNote}</Typography>
    </>
  );
}
