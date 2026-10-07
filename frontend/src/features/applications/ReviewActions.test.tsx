import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import type { LicenseApplicationDto } from '@/api/types';
import { API, server } from '../../../test/server';
import { renderApp, users } from '../../../test/render';
import { availableActions } from './reviewActions';

const OTHER_INSPECTOR = '00000000-0000-7000-8000-0000000000ff';

const application = (overrides: Partial<LicenseApplicationDto> = {}): LicenseApplicationDto => ({
  id: 'app-1', applicantId: 'pilot-1', applicantName: 'Pilot Ada Demo', licenseType: 'CPL', status: 'Submitted',
  createdAtUtc: '2026-09-20T08:00:00Z', submittedAtUtc: '2026-09-21T08:00:00Z', trainingRecordId: 'tr-1',
  reviewerId: null, reviewerName: null, reviewStartedAtUtc: null, decidedAtUtc: null, rejectionReason: null,
  licenseNumber: null, licenseExpiresAtUtc: null, ...overrides,
});

const underReviewByMe = () =>
  application({ status: 'UnderReview', reviewerId: users.inspector.userId, reviewerName: 'Denetçi Demo', reviewStartedAtUtc: '2026-09-22T08:00:00Z' });

describe('availableActions (saf kural)', () => {
  it.each([
    ['Draft', false, false],
    ['Submitted', true, false],
    ['Approved', false, false],
    ['Rejected', false, false],
  ] as const)('%s: incelemeye al=%s, karar=%s', (status, canStart, canDecide) => {
    const actions = availableActions(application({ status }), users.inspector.userId);
    expect(actions.startReview.enabled).toBe(canStart);
    expect(actions.approve.enabled).toBe(canDecide);
    expect(actions.reject.enabled).toBe(canDecide);
  });

  it('incelemede ve incelemeyi üstlenen benim: karar verebilirim', () => {
    const actions = availableActions(underReviewByMe(), users.inspector.userId);
    expect(actions.approve.enabled && actions.reject.enabled).toBe(true);
  });

  it('incelemede ama başka denetçide: karar veremem, nedeni belirtilir', () => {
    const actions = availableActions(application({ status: 'UnderReview', reviewerId: OTHER_INSPECTOR }), users.inspector.userId);
    expect(actions.approve).toEqual({ enabled: false, reason: expect.stringContaining('üstlenen denetçi') });
  });
});

/** Detay sayfasını gerçek route'uyla, verilen başvuru DTO'suyla açar. */
function openDetail(dto: LicenseApplicationDto) {
  server.use(
    http.get(`${API}/license-applications/${dto.id}`, () => HttpResponse.json(dto)),
    http.get(`${API}/training-records`, () => HttpResponse.json({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
    http.get(`${API}/audit-logs`, () => HttpResponse.json({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })),
  );
  return renderApp(`/review/${dto.id}`, users.inspector);
}

describe('Denetçi karar butonları (ekranda)', () => {
  it('Gönderildi durumunda: İncelemeye al aktif, Onayla/Reddet pasif ve nedeni yazılı', async () => {
    openDetail(application());

    expect(await screen.findByRole('button', { name: 'İncelemeye al' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Onayla' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Reddet' })).toBeDisabled();
    expect(screen.getByText(/Karar sadece "İncelemede" durumundaki/)).toBeInTheDocument();
  });

  it('incelemeyi ben üstlendiysem: Onayla/Reddet aktif, İncelemeye al pasif', async () => {
    openDetail(underReviewByMe());

    expect(await screen.findByRole('button', { name: 'Onayla' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Reddet' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'İncelemeye al' })).toBeDisabled();
  });
});

describe('Ret işlemi', () => {
  it('gerekçe boşsa veya 10 karakterden kısaysa API çağrılmaz', async () => {
    let called = false;
    server.use(http.post(`${API}/license-applications/app-1/reject`, () => { called = true; return HttpResponse.json({}); }));
    openDetail(underReviewByMe());

    await userEvent.click(await screen.findByRole('button', { name: 'Reddet' }));
    const dialog = await screen.findByRole('dialog');
    const confirm = () => userEvent.click(dialog.querySelector('button:last-of-type')!);

    await confirm();
    expect(await screen.findByText('Ret gerekçesi zorunludur.')).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText(/Ret gerekçesi/), 'kısa');
    await confirm();
    expect(await screen.findByText('Gerekçe en az 10 karakter olmalıdır.')).toBeInTheDocument();
    expect(called).toBe(false);
  });

  it('geçerli gerekçeyle reddedilir ve gerekçe backend\'e gönderilir', async () => {
    let body: unknown;
    server.use(http.post(`${API}/license-applications/app-1/reject`, async ({ request }) => {
      body = await request.json();
      return HttpResponse.json(application({ status: 'Rejected', rejectionReason: 'Sağlık raporu eksik.', decidedAtUtc: '2026-09-23T08:00:00Z' }));
    }));
    openDetail(underReviewByMe());

    await userEvent.click(await screen.findByRole('button', { name: 'Reddet' }));
    await userEvent.type(await screen.findByLabelText(/Ret gerekçesi/), 'Sağlık raporu eksik.');
    await userEvent.click((await screen.findByRole('dialog')).querySelector('button:last-of-type')!);

    await waitFor(() => expect(body).toEqual({ reason: 'Sağlık raporu eksik.' }));
    expect(await screen.findByText('Başvuru reddedildi.')).toBeInTheDocument();
  });

  it('backend alan hatası (400) ilgili form alanının altında gösterilir', async () => {
    server.use(http.post(`${API}/license-applications/app-1/reject`, () =>
      HttpResponse.json({ title: 'Doğrulama hatası', status: 400, errors: { Reason: ['Sunucu: gerekçe geçersiz.'] } }, { status: 400 })));
    openDetail(underReviewByMe());

    await userEvent.click(await screen.findByRole('button', { name: 'Reddet' }));
    await userEvent.type(await screen.findByLabelText(/Ret gerekçesi/), 'Yeterince uzun bir gerekçe');
    await userEvent.click((await screen.findByRole('dialog')).querySelector('button:last-of-type')!);

    expect(await screen.findByText('Sunucu: gerekçe geçersiz.')).toBeInTheDocument();
  });
});
