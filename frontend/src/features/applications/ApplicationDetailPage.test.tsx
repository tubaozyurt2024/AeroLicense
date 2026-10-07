import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { LicenseApplicationDto } from '@/api/types';
import { API, server } from '../../../test/server';
import { renderApp, users } from '../../../test/render';

const draft: LicenseApplicationDto = {
  id: 'app-1', applicantId: users.applicant.userId, applicantName: 'Pilot Ada Demo', licenseType: 'CPL', status: 'Draft',
  createdAtUtc: '2026-09-20T08:00:00Z', submittedAtUtc: null, trainingRecordId: null, reviewerId: null, reviewerName: null,
  reviewStartedAtUtc: null, decidedAtUtc: null, rejectionReason: null, licenseNumber: null, licenseExpiresAtUtc: null,
};

describe('Başvuru detayı (pilot)', () => {
  // Regresyon: gönderimden sonra buton ekrandan kalktığı için başarı bildirimi hiç görünmüyordu.
  it('taslak gönderilince bildirim görünür, durum ve zaman çizelgesi güncellenir', async () => {
    // Durumlu sahte API: gönderimden sonraki GET (önbellek tazelenirken) güncel durumu döner, gerçek backend gibi.
    let current = draft;
    server.use(
      http.get(`${API}/license-applications/app-1`, () => HttpResponse.json(current)),
      http.get(`${API}/license-applications`, () => HttpResponse.json({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      http.post(`${API}/license-applications/app-1/submit`, () => {
        current = { ...draft, status: 'Submitted', submittedAtUtc: '2026-09-21T08:00:00Z', trainingRecordId: 'tr-1' };
        return HttpResponse.json(current);
      }),
    );
    renderApp('/applications/app-1', users.applicant);

    await userEvent.click(await screen.findByRole('button', { name: 'Gönder' }));
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Gönder' }));

    expect(await screen.findByText('Başvurunuz gönderildi.')).toBeInTheDocument();
    expect(await screen.findByText('Başvuru gönderildi')).toBeInTheDocument(); // zaman çizelgesi
    expect(screen.queryByRole('button', { name: 'Gönder' })).not.toBeInTheDocument();
  });

  it('reddedilen başvuruda gerekçe düz metin olarak gösterilir', async () => {
    server.use(http.get(`${API}/license-applications/app-1`, () =>
      HttpResponse.json({ ...draft, status: 'Rejected', decidedAtUtc: '2026-09-23T08:00:00Z', rejectionReason: '<b>Sağlık raporu</b> eksik' })));
    renderApp('/applications/app-1', users.applicant);

    // HTML olarak yorumlanmaz: etiketler metin olarak görünür (XSS'e karşı React'in varsayılan kaçışı).
    expect(await screen.findByText('<b>Sağlık raporu</b> eksik')).toBeInTheDocument();
  });
});
