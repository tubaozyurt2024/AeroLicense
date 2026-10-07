import { defineConfig } from 'vitest/config';
import viteConfig from './vite.config';

/**
 * Gerçek backend'e karşı uçtan uca duman testi (MSW yok). jsdom, sayfanın origin'i olarak Vite dev
 * sunucusunun adresini kullanır; böylece backend'in CORS ayarı da gerçekten sınanır.
 * Çalıştırma: backend açıkken `SMOKE_PASSWORD=... npm run test:smoke`
 * Backend, login rate limit'i yükseltilmiş başlatılmalı (RateLimiting__LoginPermitPerMinute=100): testler
 * aynı IP'den 5'ten fazla giriş yapar ve varsayılan limit (5/dk) bunu doğru şekilde engeller.
 * Not: ana test ayarları birleştirilmez (mergeConfig setupFiles'ı ekler, MSW de yüklenirdi); sadece
 * plugin ve alias alınır.
 */
export default defineConfig({
  plugins: viteConfig.plugins,
  resolve: viteConfig.resolve,
  test: {
    environment: 'jsdom',
    include: ['test/smoke/**/*.smoke.tsx'],
    setupFiles: ['./test/smoke/setup.ts'],
    environmentOptions: { jsdom: { url: 'http://localhost:5173' } },
    env: {
      VITE_API_BASE_URL: process.env.SMOKE_API_URL ?? 'http://localhost:5272',
      SMOKE_PASSWORD: process.env.SMOKE_PASSWORD ?? '',
    },
    testTimeout: 30_000,
    fileParallelism: false,
  },
});
