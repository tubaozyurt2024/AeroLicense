/**
 * Backend tüm zamanları UTC (ISO 8601, "Z") döner. Gösterimde tarayıcının saat dilimine güvenilmez:
 * kullanıcı yurt dışındayken de resmi kayıt Türkiye saatiyle görünmeli. Bu yüzden saat dilimi sabit.
 */
export const TIME_ZONE = 'Europe/Istanbul';
const LOCALE = 'tr-TR';

const dateTimeFormat = new Intl.DateTimeFormat(LOCALE, { timeZone: TIME_ZONE, dateStyle: 'medium', timeStyle: 'short' });
const dateFormat = new Intl.DateTimeFormat(LOCALE, { timeZone: TIME_ZONE, dateStyle: 'long' });
const hoursFormat = new Intl.NumberFormat(LOCALE, { minimumFractionDigits: 1, maximumFractionDigits: 1 });

export const formatDateTime = (iso: string | null | undefined): string => (iso ? dateTimeFormat.format(new Date(iso)) : '—');

export const formatDate = (iso: string | null | undefined): string => (iso ? dateFormat.format(new Date(iso)) : '—');

export const formatHours = (hours: number): string => hoursFormat.format(hours);

export function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h === 0 ? `${m} dk` : m === 0 ? `${h} sa` : `${h} sa ${m} dk`;
}

/**
 * <input type="datetime-local"> değeri ("2026-09-28T14:30") saat dilimi taşımaz. Kullanıcıya "Türkiye saati"
 * diye sorulduğu için UTC+3 olarak yorumlanır. Türkiye 2016'dan beri yaz saati uygulamıyor (sabit +03:00),
 * bu yüzden tarih kütüphanesi gerekmez.
 */
export const istanbulLocalToUtcIso = (local: string): string => new Date(`${local}:00+03:00`).toISOString();

/** "2026-09-28" → o günün Türkiye saatiyle başlangıcı, UTC ISO. */
export const istanbulDateToUtcIso = (date: string): string => new Date(`${date}T00:00:00+03:00`).toISOString();

/** Türkiye saatiyle bugünün tarihi (YYYY-MM-DD), <input type="date" max> için. */
export const todayInIstanbul = (): string =>
  new Intl.DateTimeFormat('en-CA', { timeZone: TIME_ZONE }).format(new Date());

/** UUID'nin kısa gösterimi (tablolarda). Tam değer title/tooltip ile verilir. */
export const shortId = (id: string): string => id.slice(0, 8);
