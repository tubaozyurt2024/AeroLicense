import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { sessionStore } from '@/api/client';
import { server } from './server';

// Tanımlanmamış bir isteğe gidilirse test başarısız olur: sessizce gerçek ağa çıkılmaz.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  server.resetHandlers();
  sessionStore.clear();
  cleanup();
});
afterAll(() => server.close());
