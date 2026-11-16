import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server: {
      host: '127.0.0.1',
      port: Number(env.SUPPLIER_PORTAL_PORT || 5176),
      strictPort: true,
      strictPort: true,
      proxy: {
        '/api/supplier-portal': {
          target: env.SUPPLIER_PORTAL_API_PROXY_TARGET || 'http://localhost:5100',
          changeOrigin: true,
        },
      },
    },
  };
});
