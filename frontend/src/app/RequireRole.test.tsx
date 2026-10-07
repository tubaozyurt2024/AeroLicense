import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { renderApp, users } from '../../test/render';

describe('Rol bazlı route guard', () => {
  it('giriş yapmamış kullanıcıyı login sayfasına yönlendirir', async () => {
    const { router } = renderApp('/review');

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'));
    // Girişten sonra dönülecek adres saklanır.
    expect(router.state.location.state).toMatchObject({ from: '/review' });
  });

  it('başvuru sahibi denetçi sayfasına girmeye çalışırsa "yetkisiz" sayfasına gider', async () => {
    const { router } = renderApp('/audit', users.applicant);

    await waitFor(() => expect(router.state.location.pathname).toBe('/yetkisiz'));
    expect(await screen.findByText('Bu sayfayı görüntüleme yetkiniz yok')).toBeInTheDocument();
  });

  it('menüde sadece kullanıcının rolüne ait sayfalar görünür', async () => {
    renderApp('/yetkisiz', users.applicant);
    // jsdom'da ekran dar (mobil): menü önce açılır. Bu, mobil menünün çalıştığını da doğrular.
    await userEvent.click(await screen.findByRole('button', { name: 'Menüyü aç' }));

    const menu = await screen.findByRole('navigation', { name: 'Ana menü' });
    expect(menu).toHaveTextContent('Başvurularım');
    expect(menu).not.toHaveTextContent('İnceleme kuyruğu');
    expect(menu).not.toHaveTextContent('İz kayıtları');
  });
});
