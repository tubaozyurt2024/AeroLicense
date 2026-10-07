import {
  Box, Paper, Skeleton, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow,
  TableSortLabel, type TableCellProps,
} from '@mui/material';
import type { ReactNode } from 'react';
import { t } from '@/i18n';
import { EmptyState } from './EmptyState';
import { ErrorState } from './ErrorState';

export type Column<T> = {
  id: string;
  header: string;
  render: (row: T) => ReactNode;
  align?: TableCellProps['align'];
  /** Sunucu tarafı sıralama alanı; verilirse başlık tıklanabilir olur. */
  sortField?: string;
  /** Dar ekranda gizlenir (mobilde yatay kaydırmayı azaltır). */
  hideOnMobile?: boolean;
};

type Props<T> = {
  caption: string;
  columns: Column<T>[];
  rows: readonly T[] | undefined;
  getRowId: (row: T) => string;
  isLoading: boolean;
  error: unknown;
  onRetry?: () => void;
  emptyMessage?: string;
  pagination?: {
    page: number; // 1 tabanlı (backend ile aynı)
    pageSize: number;
    totalCount: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (pageSize: number) => void;
  };
  sort?: { field: string; direction: 'Asc' | 'Desc'; onChange: (field: string) => void };
};

const PAGE_SIZES = [10, 20, 50];

/**
 * Sunucu tarafı sayfalanan tablo. Veri bu bileşende filtrelenmez/sıralanmaz: 10.000 kayıtlık bir listeyi
 * tarayıcıya indirmek yerine her sayfa backend'den istenir. Yükleniyor / hata / boş durumları tek yerde.
 */
export function DataTable<T>({
  caption, columns, rows, getRowId, isLoading, error, onRetry, emptyMessage, pagination, sort,
}: Props<T>) {
  if (error) return <ErrorState error={error} onRetry={onRetry} />;

  const cellSx = (column: Column<T>) => (column.hideOnMobile ? { display: { xs: 'none', md: 'table-cell' } } : undefined);

  return (
    <Paper>
      <TableContainer>
        <Table size="small" aria-busy={isLoading}>
          {/* Görsel olarak gizli ama ekran okuyucuya tablonun ne olduğunu söyler. */}
          <caption style={{ position: 'absolute', width: 1, height: 1, overflow: 'hidden', clip: 'rect(0 0 0 0)' }}>{caption}</caption>
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align}
                  sx={cellSx(column)}
                  sortDirection={sort && column.sortField === sort.field ? (sort.direction === 'Asc' ? 'asc' : 'desc') : false}
                >
                  {sort && column.sortField ? (
                    <TableSortLabel
                      active={sort.field === column.sortField}
                      direction={sort.field === column.sortField && sort.direction === 'Asc' ? 'asc' : 'desc'}
                      onClick={() => sort.onChange(column.sortField!)}
                    >
                      {column.header}
                    </TableSortLabel>
                  ) : (
                    column.header
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading
              ? Array.from({ length: 5 }, (_, i) => (
                  <TableRow key={i}>
                    {columns.map((column) => (
                      <TableCell key={column.id} sx={cellSx(column)}>
                        <Skeleton />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              : rows?.map((row) => (
                  <TableRow key={getRowId(row)} hover>
                    {columns.map((column) => (
                      <TableCell key={column.id} align={column.align} sx={cellSx(column)}>
                        {column.render(row)}
                      </TableCell>
                    ))}
                  </TableRow>
                ))}
          </TableBody>
        </Table>
      </TableContainer>
      {!isLoading && rows?.length === 0 && <EmptyState message={emptyMessage} />}
      {pagination && (
        <Box className="no-print">
          <TablePagination
            component="div"
            count={pagination.totalCount}
            page={pagination.page - 1}
            rowsPerPage={pagination.pageSize}
            rowsPerPageOptions={PAGE_SIZES}
            onPageChange={(_, page) => pagination.onPageChange(page + 1)}
            onRowsPerPageChange={(e) => pagination.onPageSizeChange(Number(e.target.value))}
            labelRowsPerPage={t.common.rowsPerPage}
            labelDisplayedRows={({ from, to, count }) => t.common.pageInfo(from, to, count)}
          />
        </Box>
      )}
    </Paper>
  );
}
