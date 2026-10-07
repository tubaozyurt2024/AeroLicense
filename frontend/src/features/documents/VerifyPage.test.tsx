import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { VerificationResultDto } from '@/api/types';
import { API, server } from '../../../test/server';
import { renderApp } from '../../../test/render';

const CODE = 'ABCDEFGHJKMNPQRSTVWXYZ2345';

const respond = (body: Partial<VerificationResultDto>) =>
  server.use(http.get(`${API}/verify/${CODE}`, () =>
    HttpResponse.json({ checkedAtUtc: '2026-09-29T12:00:00Z', ...body })));

describe('Belge doğrulama sayfası (girişsiz)', () => {
  it('geçerli belgede durumu ve sadece minimum, maskeli bilgiyi gösterir', async () => {
    respond({
      status: 'Valid', licenseNumber: 'AL-CPL-2026-ABC234', licenseType: 'CPL',
      holderNameMasked: 'P**** A** D***', expiresAtUtc: '2028-09-29T11:30:00Z',
    });
    renderApp(`/verify/${CODE}`);

    const result = (await screen.findByText('Geçerli')).closest('[role="status"]');
    expect(result).not.toBeNull();
    expect(result).toHaveTextContent('AL-CPL-2026-ABC234');
    expect(result).toHaveTextContent('P**** A** D***');
    // UTC 11:30 → Türkiye 14:30; tarih TR formatında.
    expect(result).toHaveTextContent('29 Eylül 2028');
  });

  it('değiştirilmiş belgede uyarı verir ve hiçbir içerik alanı göstermez', async () => {
    respond({ status: 'Tampered' });
    renderApp(`/verify/${CODE}`);

    expect(await screen.findByText('Değiştirilmiş')).toBeInTheDocument();
    expect(screen.getByText(/Bu belgeye güvenmeyin/)).toBeInTheDocument();
    expect(screen.queryByText('Lisans no')).not.toBeInTheDocument();
  });

  it.each([
    ['Expired', 'Süresi dolmuş'],
    ['Revoked', 'İptal edilmiş'],
  ] as const)('%s durumunu gösterir', async (status, label) => {
    respond({ status, licenseNumber: 'AL-PPL-2024-XYZ234', licenseType: 'PPL', holderNameMasked: 'P****', expiresAtUtc: '2026-01-01T00:00:00Z' });
    renderApp(`/verify/${CODE}`);

    expect(await screen.findByText(label)).toBeInTheDocument();
  });

  it('bilinmeyen kodda (404) "bulunamadı" gösterir', async () => {
    server.use(http.get(`${API}/verify/${CODE}`, () => HttpResponse.json({ title: 'Kayıt bulunamadı' }, { status: 404 })));
    renderApp(`/verify/${CODE}`);

    expect(await screen.findByText('Belge bulunamadı')).toBeInTheDocument();
  });

  it('formatı bozuk kod için API çağrılmaz', async () => {
    renderApp('/verify/kisa-kod'); // onUnhandledRequest: 'error' → istek atılsaydı test düşerdi

    expect(await screen.findByText('Belge bulunamadı')).toBeInTheDocument();
  });
});
