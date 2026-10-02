# SILA ME database snapshots

Application code lives on GitHub with EF Core migrations under `apps/api/Migrations`.

`dumps/snapshots/` holds restorable PostgreSQL custom dumps of the databases used by this Cloud environment:

| File | Database | Use |
| --- | --- | --- |
| `sila_platform.dump` | `sila_platform` | Platform control plane (tenants, routes, licenses) |
| `sila_me.dump` | `sila_me` | Legacy SILA-DEV operational database |
| `sila_five_test.dump` | `sila_five_test` | FIVE Hotels TEST (Cloud `/five`) |
| `sila_five_prod.dump` | `sila_five_prod` | FIVE Hotels PROD |

These dumps include schema and data (recipes, inventory, POS tracker, users). They do not include `.env` passwords.

## Refresh snapshots

```bash
./scripts/db.sh snapshot
```

## Restore on a new machine

```bash
./scripts/db.sh up
./scripts/db.sh restore-snapshots
```

Ad-hoc backups go to `dumps/local/` and stay gitignored.
