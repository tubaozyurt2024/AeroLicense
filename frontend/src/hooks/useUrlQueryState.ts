import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';

const MAX_PAGE_SIZE = 100; // backend üst sınırı

/**
 * Liste durumunu (sayfa, sayfa boyutu, filtreler) URL query string'inde tutar. Faydası: sayfa yenilenince,
 * geri tuşuna basılınca veya bağlantı paylaşılınca aynı liste görünür. Filtre değişince sayfa 1'e döner
 * (yoksa 5. sayfadayken filtreleyen kullanıcı boş sayfa görürdü).
 */
export function useUrlQueryState<F extends string>(filterKeys: readonly F[], defaultPageSize = 20) {
  const [params, setParams] = useSearchParams();

  const page = Math.max(1, Number(params.get('page')) || 1);
  const pageSize = Math.min(MAX_PAGE_SIZE, Math.max(1, Number(params.get('pageSize')) || defaultPageSize));

  const filterValues = filterKeys.map((key) => params.get(key) ?? '').join('\u0000');
  const filters = useMemo(
    () => Object.fromEntries(filterKeys.map((key) => [key, params.get(key) ?? ''])) as Record<F, string>,
    // eslint-disable-next-line react-hooks/exhaustive-deps -- filterValues, ilgili parametrelerin içeriğini temsil eder
    [filterValues],
  );

  const update = useCallback(
    (changes: Record<string, string | number | undefined>, resetPage: boolean) => {
      setParams((current) => {
        const next = new URLSearchParams(current);
        for (const [key, value] of Object.entries(changes)) {
          if (value === undefined || value === '') next.delete(key);
          else next.set(key, String(value));
        }
        if (resetPage) next.delete('page');
        return next;
      }, { replace: true });
    },
    [setParams],
  );

  return {
    page,
    pageSize,
    filters,
    setPage: (value: number) => update({ page: value === 1 ? undefined : value }, false),
    setPageSize: (value: number) => update({ pageSize: value }, true),
    setFilter: (key: F, value: string) => update({ [key]: value }, true),
    setFilters: (values: Partial<Record<F, string>>) => update(values, true),
  };
}
