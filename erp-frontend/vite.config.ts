import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],
    optimizeDeps: {
      include: ['framer-motion'],
    },
    server: {
      host: '127.0.0.1',
      port: Number(env.ERP_FRONTEND_PORT || 5173),
      strictPort: true,
      proxy: {
        '/api': {
          target: env.ERP_API_PROXY_TARGET || 'http://localhost:5100',
          changeOrigin: true,
        },
      },
    },
  }
})