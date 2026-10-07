import { describe, expect, it } from 'vitest';
import { formatDateTime, formatDuration, istanbulDateToUtcIso, istanbulLocalToUtcIso } from './format';

describe('Tarih/saat', () => {
  it('UTC değer Türkiye saatiyle ve TR formatında gösterilir', () => {
    // 11:30 UTC = 14:30 Türkiye (UTC+3); test ortamının saat diliminden bağımsız.
    expect(formatDateTime('2026-09-29T11:30:00Z')).toBe('29 Eyl 2026 14:30');
  });

  it('Türkiye saatiyle girilen zaman UTC\'ye çevrilir', () => {
    expect(istanbulLocalToUtcIso('2026-09-27T09:00')).toBe('2026-09-27T06:00:00.000Z');
    expect(istanbulDateToUtcIso('2026-09-28')).toBe('2026-09-27T21:00:00.000Z');
  });

  it.each([[45, '45 dk'], [60, '1 sa'], [85, '1 sa 25 dk']])('%i dk → %s', (minutes, text) => {
    expect(formatDuration(minutes)).toBe(text);
  });
});
