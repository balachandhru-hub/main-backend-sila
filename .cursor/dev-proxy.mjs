// Single-origin dev reverse proxy for the SILA ME Cloud web app.
//
// The Cloud SPA calls the API using same-origin `/api/*` paths (cookie-based
// sessions), matching the production single-domain router. In local dev the
// Vite server and the .NET API listen on separate ports, so this proxy fronts
// both: `/api/*` is forwarded to the API and everything else to Vite (including
// the HMR websocket).
//
// Ports are configurable via env vars:
//   PROXY_PORT (default 4000), API_PORT (default 8080), WEB_PORT (default 5173)
import http from 'node:http';

const API = { host: '127.0.0.1', port: Number(process.env.API_PORT) || 8080 };
const WEB = { host: '127.0.0.1', port: Number(process.env.WEB_PORT) || 5173 };
const LISTEN = Number(process.env.PROXY_PORT) || 4000;

const server = http.createServer((req, res) => {
  const target = req.url.startsWith('/api') ? API : WEB;
  const proxyReq = http.request(
    {
      host: target.host,
      port: target.port,
      method: req.method,
      path: req.url,
      headers: req.headers,
    },
    (proxyRes) => {
      res.writeHead(proxyRes.statusCode, proxyRes.headers);
      proxyRes.pipe(res, { end: true });
    },
  );
  proxyReq.on('error', (err) => {
    res.writeHead(502);
    res.end('proxy error: ' + err.message);
  });
  req.pipe(proxyReq, { end: true });
});

// Forward the Vite HMR websocket upgrade.
server.on('upgrade', (req, socket, head) => {
  const proxyReq = http.request({
    host: WEB.host,
    port: WEB.port,
    method: req.method,
    path: req.url,
    headers: req.headers,
  });
  proxyReq.on('upgrade', (proxyRes, proxySocket, proxyHead) => {
    socket.write(
      'HTTP/1.1 101 Switching Protocols\r\n' +
        Object.entries(proxyRes.headers)
          .map(([k, v]) => `${k}: ${v}`)
          .join('\r\n') +
        '\r\n\r\n',
    );
    if (proxyHead && proxyHead.length) proxySocket.unshift(proxyHead);
    proxySocket.pipe(socket);
    socket.pipe(proxySocket);
  });
  proxyReq.on('error', () => socket.destroy());
  proxyReq.end();
});

server.listen(LISTEN, '0.0.0.0', () =>
  console.log(
    `dev proxy listening on http://0.0.0.0:${LISTEN} -> /api:${API.port}, *:${WEB.port}`,
  ),
);
