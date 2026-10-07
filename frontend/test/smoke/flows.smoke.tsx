import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { renderApp } from '../render';

// Demo parolası ortam değişkeninden gelir; dosyaya yazılmaz.
const password = (import.meta.env as Record<string, string | undefined>).SMOKE_PASSWORD ?? '';

async function loginAs(user: string, startPath = '/login') {
  const app = renderApp(startPath);
  await userEvent.type(await screen.findByLabelText(/E-posta/), `${user}@aerolicense.test`);
  await userEvent.type(screen.getByLabelText(/Parola/), password);
  await userEvent.click(screen.getByRole('button', { name: 'Giriş yap' }));
  await waitFor(() => expect(app.router.state.location.pathname).not.toBe('/login'), { timeout: 10_000 });
  return app;
}

const go = (app: Awaited<ReturnType<typeof loginAs>>, path: string) => app.router.navigate(path);

describe.skipIf(!password)('Gerçek backend ile uçtan uca akış', () => {
  it('pilot: taslak başvuruyu gönderir', async () => {
    const app = await loginAs('pilot1');
    expect(await screen.findByText(/Hoş geldiniz, Pilot Ada Demo/)).toBeInTheDocument();

    await go(app, '/applications?status=Draft');
    await userEvent.click(await screen.findByRole('link', { name: /CPL/ }));
    await userEvent.click(await screen.findByRole('button', { name: 'Gönder' }));
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Gönder' }));

    expect(await screen.findByText('Başvurunuz gönderildi.')).toBeInTheDocument();
    expect(await screen.findByText('Başvuru gönderildi')).toBeInTheDocument(); // zaman çizelgesi
  });

  it('denetçi: kuyruktan alır, incelemeye alır ve onaylar', async () => {
    const app = await loginAs('inspector');
    await waitFor(() => expect(app.router.state.location.pathname).toBe('/review'));

    await userEvent.click(await screen.findByRole('link', { name: 'Pilot Ada Demo' }));
    await userEvent.click(await screen.findByRole('button', { name: 'İncelemeye al' }));
    expect(await screen.findByText('Başvuru incelemeye alındı.')).toBeInTheDocument();

    await waitFor(() => expect(screen.getByRole('button', { name: 'Onayla' })).toBeEnabled());
    await userEvent.click(screen.getByRole('button', { name: 'Onayla' }));
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Onayla' }));

    expect(await screen.findByText('Başvuru onaylandı, lisans düzenlendi.')).toBeInTheDocument();
    expect(await screen.findByText(/AL-CPL-/)).toBeInTheDocument();
    // Başvurunun audit geçmişi de doldu.
    expect(await screen.findByText('Başvuru onaylandı')).toBeInTheDocument();
  });

  it('pilot: QR kodlu belgeyi görür; kod girişsiz doğrulama sayfasında "Geçerli" döner', async () => {
    const app = await loginAs('pilot1');
    await go(app, '/licenses');

    const qr = await screen.findByAltText(/AL-CPL-.*doğrulama QR kodu/, {}, { timeout: 10_000 });
    expect(qr.getAttribute('src')).toMatch(/^data:image\/png;base64,/);
    const urlText = screen.getAllByText(/Doğrulama adresi/)[0]!.textContent!;
    const code = urlText.split('/verify/')[1]!.trim();

    await go(app, `/verify/${code}`);
    expect(await screen.findByText('Geçerli', {}, { timeout: 10_000 })).toBeInTheDocument();
    expect(screen.getByText('P**** A** D***')).toBeInTheDocument();
  });

  it('havayolu: aynı isteği tekrar gönderince "yeni kayıt oluşmadı" (CORS exposed header)', async () => {
    const inspector = await loginAs('inspector');
    await go(inspector, '/flight-logs?license=');
    const licenseCell = await screen.findAllByText(/\(AL-PPL-/);
    const licenseNumber = licenseCell[0]!.textContent!.match(/\((AL-[^)]+)\)/)![1]!;
    inspector.unmount();

    await loginAs('havayolu1');
    await userEvent.type(await screen.findByLabelText(/Pilot lisans no/), licenseNumber);
    await userEvent.type(screen.getByLabelText(/Uçuş no/), 'XDA555');
    await userEvent.type(screen.getByLabelText(/Kalkış \(ICAO\)/), 'LTFM');
    await userEvent.type(screen.getByLabelText(/Varış \(ICAO\)/), 'LTAC');
    const day = new Date(Date.now() - 2 * 86_400_000).toISOString().slice(0, 10);
    await userEvent.type(screen.getByLabelText(/Kalkış zamanı/), `${day}T18:00`);
    await userEvent.type(screen.getByLabelText(/Varış zamanı/), `${day}T19:05`);

    await userEvent.click(screen.getByRole('button', { name: 'Gönder' }));
    expect(await screen.findByText(/HTTP 201/, {}, { timeout: 10_000 })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Aynı isteği tekrar gönder' }));
    expect(await screen.findByText(/yeni kayıt oluşmadı/)).toBeInTheDocument();
  });

  it('denetçi: şüpheli uçuş filtresi ve audit ekranı çalışır', async () => {
    const app = await loginAs('inspector');
    await go(app, '/flight-logs?suspicious=true');
    expect(await screen.findByText('Uzun süre')).toBeInTheDocument();
    expect(await screen.findByText('Geç bildirim')).toBeInTheDocument();

    await go(app, '/audit');
    expect((await screen.findAllByText(/i\*\*\*@aerolicense\.test/)).length).toBeGreaterThan(0);
  });

  it('eğitim kuruluşu: 70 altı puanda uyarı görür ve kaydı girer', async () => {
    const app = await loginAs('egitim1');
    await go(app, '/training-records/new');
    await userEvent.type(await screen.findByLabelText(/TC kimlik no/), '99999999902');
    await userEvent.type(screen.getByLabelText(/Tamamlanma/), '2026-09-01');
    await userEvent.type(screen.getByLabelText(/Puan/), '65');
    expect(await screen.findByText(/"başarısız" sayılacak/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Kaydet' }));
    expect(await screen.findByText('Eğitim kaydı oluşturuldu.')).toBeInTheDocument();
    expect(await screen.findByText('999******02')).toBeInTheDocument(); // listede maskeli TC
  });
});
