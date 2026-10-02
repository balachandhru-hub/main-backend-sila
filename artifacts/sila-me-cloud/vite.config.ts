import path from 'path';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import type { ProxyOptions } from 'vite';

import runtimeErrorOverlay from '@replit/vite-plugin-runtime-error-modal';

const rawPort = process.env.PORT;

if (!rawPort) {
  throw new Error(
    'PORT environment variable is required but was not provided.',
  );
}

const port = Number(rawPort);

if (Number.isNaN(port) || port <= 0) {
  throw new Error(`Invalid PORT value: "${rawPort}"`);
}

const basePath = process.env.BASE_PATH;

if (!basePath) {
  throw new Error(
    'BASE_PATH environment variable is required but was not provided.',
  );
}

const apiProxyTarget = process.env.SILA_ME_API_PROXY ?? 'http://127.0.0.1:8080';

function firstPathSegment(value: string): string | null {
  const match = value.split(/[/?#]/).filter(Boolean)[0];
  return match ? match.toLowerCase() : null;
}

function customerSlugFromBase(value: string): string | null {
  const first = firstPathSegment(value);
  if (!first || first === 'api' || first === 'platform') return null;
  return first === 'five-test' ? 'five' : first;
}

function slugFromReferer(referer: string | undefined): string | null {
  if (!referer) return null;
  try {
    const path = new URL(referer).pathname;
    const first = firstPathSegment(path);
    if (!first || first === 'api' || first === 'platform' || first === 'login') {
      return null;
    }
    return first === 'five-test' ? 'five' : first;
  } catch {
    return null;
  }
}

function attachTenantProxy(proxy: ProxyOptions): ProxyOptions {
  const previous = proxy.configure;
  return {
    ...proxy,
    configure(proxyServer, options) {
      previous?.(proxyServer, options);
      proxyServer.on('proxyReq', (proxyReq, req) => {
        if (proxyReq.getHeader('x-sila-route-slug')) return;
        const incoming = req.headers['x-sila-route-slug'];
        if (typeof incoming === 'string' && incoming.trim()) {
          proxyReq.setHeader('X-Sila-Route-Slug', incoming.trim());
          return;
        }
        const fromUrl = firstPathSegment(req.url ?? '');
        const urlSlug =
          fromUrl && fromUrl !== 'api' && fromUrl !== 'platform'
            ? fromUrl === 'five-test'
              ? 'five'
              : fromUrl
            : null;
        const slug =
          urlSlug ??
          slugFromReferer(
            typeof req.headers.referer === 'string'
              ? req.headers.referer
              : undefined,
          ) ??
          customerSlugFromBase(basePath);
        if (slug) {
          proxyReq.setHeader('X-Sila-Route-Slug', slug);
        }
      });
    },
  };
}

export default defineConfig({
  base: basePath,
  plugins: [
    react(),
    tailwindcss(),
    runtimeErrorOverlay(),
    ...(process.env.NODE_ENV !== 'production' &&
    process.env.REPL_ID !== undefined
      ? [
          await import('@replit/vite-plugin-cartographer').then((m) =>
            m.cartographer({
              root: path.resolve(import.meta.dirname, '..'),
            }),
          ),
          await import('@replit/vite-plugin-dev-banner').then((m) =>
            m.devBanner(),
          ),
        ]
      : []),
  ],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, 'src'),
      '@assets': path.resolve(
        import.meta.dirname,
        '..',
        '..',
        'attached_assets',
      ),
    },
    dedupe: ['react', 'react-dom'],
  },
  root: path.resolve(import.meta.dirname),
  build: {
    outDir: path.resolve(import.meta.dirname, 'dist/public'),
    emptyOutDir: true,
  },
  server: {
    port,
    strictPort: true,
    // Dual-stack (::) so Cursor/browser localhost (IPv4 or IPv6) both reach /five/login.
    host: '::',
    allowedHosts: true,
    hmr: {
      clientPort: port,
    },
    fs: {
      strict: true,
    },
    proxy: {
      '/api': attachTenantProxy({
        target: apiProxyTarget,
        changeOrigin: false,
      }),
    },
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const raw = req.url ?? '';
        const pathOnly = raw.split('?')[0] ?? '';
        const query = raw.includes('?') ? raw.slice(raw.indexOf('?')) : '';

        if (pathOnly === '/five-test' || pathOnly.startsWith('/five-test/')) {
          const rest = pathOnly.slice('/five-test'.length) || '/';
          res.statusCode = 302;
          res.setHeader('Location', `/five${rest}${query}`);
          res.end();
          return;
        }

        const prefixedApi = pathOnly.match(/^\/(five|five-test)\/api(\/.*)?$/i);
        if (prefixedApi) {
          req.url = `/api${prefixedApi[2] || ''}${query}`;
          next();
          return;
        }

        const baseSlug = customerSlugFromBase(basePath);
        const first = firstPathSegment(pathOnly);
        if (
          baseSlug &&
          first &&
          ['login', 'dashboard'].includes(first) &&
          !pathOnly.startsWith(`/${baseSlug}/`)
        ) {
          res.statusCode = 302;
          res.setHeader('Location', `/${baseSlug}${pathOnly}${query}`);
          res.end();
          return;
        }

        next();
      });
    },
  },
  preview: {
    port,
    host: '::',
    allowedHosts: true,
  },
});
