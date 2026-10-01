import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

/** The one SILA gateway (Identity, Buyer, Supplier, Master Data, Operations).
 *  The other environment brings this up on port 8000. Do not point at
 *  https://sila-api.chervicaon.com — that host is a different deployment. */
const apiTarget = process.env.SILA_API_TARGET || 'http://127.0.0.1:8000'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
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
