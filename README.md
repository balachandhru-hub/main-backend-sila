# SILA Mobile

Phone layout for the SILA procurement portal. Buyers and suppliers sign in with the same accounts as the desktop app and use this system's API (`https://sila-api.chervicaon.com`, the `main-backend-sila` gateway).

This history is an orphan branch, `cursor/sila-mobile-9098`, on `balachandhru-hub/main-backend-sila`. It does not share files with the .NET backend. To give it a repository of its own:

```bash
git clone -b cursor/sila-mobile-9098 --single-branch https://github.com/balachandhru-hub/main-backend-sila.git sila-mobile
cd sila-mobile
gh repo create balachandhru-hub/sila-mobile --public --source=. --remote=origin --push
```

## What you can do

- Sign in and keep the session with the identity cookies
- Buyer home: live RFQs, deadlines, awards, contracts
- Supplier home: open bids, quotes, wins
- RFQ list and detail, including supplier quotations
- Submit a supplier quotation (email code, then prices)
- Search the buyer catalog, or review the supplier catalog
- Read the organization profile

Administrators can sign in and see their account. User admin, approvals and contract setup stay on the desktop portal.

## Run

```bash
npm install
npm run dev
```

Open `http://localhost:5173`. The dev server proxies `/api` to the SILA gateway and rewrites the auth cookie so the phone session works on localhost.

`VITE_API_KEY` is the same public client key the desktop portal already sends. Copy `.env.example` to `.env` if it is missing.
