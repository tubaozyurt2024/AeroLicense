/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    // Backend CORS ayarında izinli tek origin bu port (appsettings.Development.json).
    port: 5173,
    strictPort: true,
  },
  build: {
    // Kaynak haritası üretimde yayınlanmaz: kaynak kod ve yorumlar istemciye gitmez.
    sourcemap: false,
    rolldownOptions: {
      output: {
        // Nadiren değişen kütüphaneler ayrı dosyada: uygulama her yayınlandığında kullanıcı sadece
        // değişen uygulama kodunu indirir, React/MUI tarayıcı önbelleğinden gelir.
        codeSplitting: {
          groups: [
            { name: 'react', test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/ },
            { name: 'mui', test: /node_modules[\\/](@mui|@emotion)[\\/]/ },
            { name: 'data', test: /node_modules[\\/](@tanstack|axios|zod|react-hook-form|@hookform)[\\/]/ },
          ],
        },
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./test/setup.ts'],
    css: false,
    env: { VITE_API_BASE_URL: 'http://api.test' },
  },
});
