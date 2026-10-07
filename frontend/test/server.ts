import { setupServer } from 'msw/node';

/**
 * MSW: testlerde gerçek HTTP katmanı (axios + interceptor'lar) çalışır, sadece ağ yanıtı taklit edilir.
 * Axios'u mock'lamaktan farkı: istemcideki hata dönüştürme, başlıklar ve URL'ler de test edilmiş olur.
 */
export const server = setupServer();
export const API = 'http://api.test/api/v1';
