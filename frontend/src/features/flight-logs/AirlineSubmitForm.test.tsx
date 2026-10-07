import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { API, server } from '../../../test/server';
import { renderApp, users } from '../../../test/render';

describe('Havayolu uçuş gönderimi (idempotency)', () => {
  it('aynı istek tekrar gönderilince aynı anahtar kullanılır ve "yeni kayıt oluşmadı" gösterilir', async () => {
    const seenKeys: string[] = [];
    const flight = { id: 'f-1', pilotLicenseNumber: 'AL-PPL-2025-ABC234', flightNumber: 'XDA777', departureAirport: 'LTFM',
      arrivalAirport: 'LTAC', departureAtUtc: '2026-09-27T06:00:00Z', arrivalAtUtc: '2026-09-27T07:10:00Z', durationMinutes: 70, recordedAtUtc: '' };
    server.use(http.post(`${API}/flight-logs`, ({ request }) => {
      const key = request.headers.get('Idempotency-Key')!;
      const replay = seenKeys.includes(key);
      seenKeys.push(key);
      return HttpResponse.json(flight, replay ? { status: 200, headers: { 'Idempotent-Replayed': 'true' } } : { status: 201 });
    }));
    renderApp('/flight-logs/new', users.airline);

    await userEvent.type(await screen.findByLabelText(/Pilot lisans no/), 'AL-PPL-2025-ABC234');
    await userEvent.type(screen.getByLabelText(/Uçuş no/), 'xda777');
    await userEvent.type(screen.getByLabelText(/Kalkış \(ICAO\)/), 'ltfm');
    await userEvent.type(screen.getByLabelText(/Varış \(ICAO\)/), 'ltac');
    await userEvent.type(screen.getByLabelText(/Kalkış zamanı/), '2026-09-27T09:00');
    await userEvent.type(screen.getByLabelText(/Varış zamanı/), '2026-09-27T10:10');

    await userEvent.click(screen.getByRole('button', { name: 'Gönder' }));
    expect(await screen.findByText(/HTTP 201/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Aynı isteği tekrar gönder' }));
    expect(await screen.findByText(/yeni kayıt oluşmadı/)).toBeInTheDocument();

    expect(seenKeys).toHaveLength(2);
    expect(seenKeys[0]).toBe(seenKeys[1]); // anahtar işlem başına, tıklama başına değil
  });

  it('aynı havalimanı ve ters saat istemcide yakalanır', async () => {
    renderApp('/flight-logs/new', users.airline); // onUnhandledRequest: 'error' → istek atılırsa test düşer

    await userEvent.type(await screen.findByLabelText(/Pilot lisans no/), 'AL-PPL-2025-ABC234');
    await userEvent.type(screen.getByLabelText(/Uçuş no/), 'XDA777');
    await userEvent.type(screen.getByLabelText(/Kalkış \(ICAO\)/), 'LTFM');
    await userEvent.type(screen.getByLabelText(/Varış \(ICAO\)/), 'LTFM');
    await userEvent.type(screen.getByLabelText(/Kalkış zamanı/), '2026-09-27T10:00');
    await userEvent.type(screen.getByLabelText(/Varış zamanı/), '2026-09-27T09:00');
    await userEvent.click(screen.getByRole('button', { name: 'Gönder' }));

    expect(await screen.findByText('Kalkış ve varış havalimanı aynı olamaz.')).toBeInTheDocument();
  });
});
