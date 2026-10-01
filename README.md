# SILA Store mobile

Phone view of the Silame store work that this backend actually serves: invoice upload, invoice review, purchase orders, and goods receipt (GRN).

It talks only to the SILA gateway in this system (`http://127.0.0.1:8000` by default). It does not call `https://sila-api.chervicaon.com`.

```bash
npm install
npm run dev
```

Open `http://localhost:5173` after the gateway is up. The dev server proxies `/api` and rewrites the auth cookie so the session stays on this origin.

Override the gateway with `SILA_API_TARGET` if it is not on port 8000.

Stock transfer, goods issue, and inventory count exist on the older Silame store client. This .NET operations API does not expose those routes, so they are not in this app.
