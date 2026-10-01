import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const apiTarget = process.env.SILA_API_TARGET || 'https://sila-api.chervicaon.com'

/** Dev server proxies the SILA gateway and rewrites auth cookies so a local
 *  origin can hold the session the API sets for .chervicaon.com. */
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: true,
        configure: (proxy) => {
          proxy.on('proxyRes', (proxyRes) => {
            const raw = proxyRes.headers['set-cookie']
            if (!raw) return
            proxyRes.headers['set-cookie'] = raw.map((cookie) =>
              cookie
                .replace(/;\s*Domain=[^;]*/gi, '')
                .replace(/;\s*Secure/gi, '')
                .replace(/;\s*SameSite=None/gi, '; SameSite=Lax'),
            )
          })
        },
      },
    },
  },
})
