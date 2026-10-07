import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { API, server } from '../../../test/server';
import { renderApp, users } from '../../../test/render';

describe('Giriş formu', () => {
  it('boş gönderimde alan hatalarını gösterir ve API çağırmaz', async () => {
    let called = false;
    server.use(http.post(`${API}/auth/login`, () => { called = true; return HttpResponse.json({}); }));
    renderApp('/login');

    await userEvent.click(await screen.findByRole('button', { name: 'Giriş yap' }));

    expect(await screen.findByText('E-posta zorunludur.')).toBeInTheDocument();
    expect(screen.getByText('Parola zorunludur.')).toBeInTheDocument();
    expect(called).toBe(false);
  });

  it('geçersiz e-posta formatını reddeder', async () => {
    renderApp('/login');

    await userEvent.type(await screen.findByLabelText(/E-posta/), 'eposta-degil');
    await userEvent.type(screen.getByLabelText(/Parola/), 'x');
    await userEvent.click(screen.getByRole('button', { name: 'Giriş yap' }));

    expect(await screen.findByText('Geçerli bir e-posta adresi girin.')).toBeInTheDocument();
  });

  it('başarılı girişte token alınır, rolün açılış sayfasına gidilir', async () => {
    server.use(
      http.post(`${API}/auth/login`, () =>
        HttpResponse.json({ accessToken: 'a', expiresAtUtc: '', role: 'Inspector', refreshToken: 'r', refreshTokenExpiresAtUtc: '' })),
      http.get(`${API}/auth/me`, ({ request }) =>
        // Interceptor'ın Authorization başlığını eklediği de doğrulanır.
        request.headers.get('Authorization') === 'Bearer a'
          ? HttpResponse.json(users.inspector)
          : new HttpResponse(null, { status: 401 })),
      http.get(`${API}/license-applications`, () => HttpResponse.json({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
    );
    const { router } = renderApp('/login');

    await userEvent.type(await screen.findByLabelText(/E-posta/), 'inspector@aerolicense.test');
    await userEvent.type(screen.getByLabelText(/Parola/), 'Demo123!');
    await userEvent.click(screen.getByRole('button', { name: 'Giriş yap' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/review'));
  });

  it('401 yanıtında backend mesajını gösterir', async () => {
    server.use(http.post(`${API}/auth/login`, () =>
      HttpResponse.json({ title: 'Kimlik doğrulanamadı', detail: 'E-posta veya parola hatalı.', status: 401 }, { status: 401 })));
    renderApp('/login');

    await userEvent.type(await screen.findByLabelText(/E-posta/), 'pilot1@aerolicense.test');
    await userEvent.type(screen.getByLabelText(/Parola/), 'yanlis');
    await userEvent.click(screen.getByRole('button', { name: 'Giriş yap' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('E-posta veya parola hatalı.');
  });
});
