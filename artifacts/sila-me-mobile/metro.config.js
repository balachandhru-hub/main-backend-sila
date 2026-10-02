const http = require('http');
const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);
const previousEnhance = config.server?.enhanceMiddleware;
const apiPort = process.env.EXPO_PUBLIC_SILA_ME_API_PORT ?? process.env.SILA_ME_API_PORT ?? '8080';
const hopByHop = new Set([
  'connection',
  'keep-alive',
  'proxy-authenticate',
  'proxy-authorization',
  'te',
  'trailers',
  'transfer-encoding',
  'upgrade',
]);

config.server = {
  ...config.server,
  enhanceMiddleware: (metroMiddleware, server) => {
    const inner = previousEnhance ? previousEnhance(metroMiddleware, server) : metroMiddleware;
    return (req, res, next) => {
      const url = req.url ?? '';
      if (!url.startsWith('/api')) {
        return inner(req, res, next);
      }

      const headers = { ...req.headers };
      for (const name of hopByHop) {
        delete headers[name];
      }
      if (!headers['x-sila-route-slug']) {
        headers['x-sila-route-slug'] = process.env.EXPO_PUBLIC_SILA_ROUTE_SLUG || 'five';
      }

      const proxyReq = http.request(
        {
          hostname: '127.0.0.1',
          port: Number(apiPort),
          path: url,
          method: req.method,
          headers,
        },
        (proxyRes) => {
          const responseHeaders = { ...proxyRes.headers };
          for (const name of hopByHop) {
            delete responseHeaders[name];
          }
          res.writeHead(proxyRes.statusCode ?? 502, responseHeaders);
          proxyRes.pipe(res);
        },
      );
      proxyReq.on('error', () => {
        if (!res.headersSent) {
          res.statusCode = 502;
          res.setHeader('Content-Type', 'application/json');
        }
        res.end(
          JSON.stringify({
            code: 'API_UNREACHABLE',
            message: 'Cannot reach the SILA ME API. Confirm the API is running and port 8080 is forwarded.',
          }),
        );
      });
      req.pipe(proxyReq);
    };
  },
};

module.exports = config;
