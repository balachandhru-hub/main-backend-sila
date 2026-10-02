---
name: Manual EF migrations
description: Reliable discovery requirements for hand-written Entity Framework migrations in this project.
---

Hand-written Entity Framework migrations must carry explicit `DbContext` and `Migration` metadata, and the database history must be checked after restarting the API.

**Why:** A migration compiled successfully but was not discovered by startup until both metadata attributes were present, leaving the API running against an incomplete schema.

**How to apply:** When scaffolding tools are unavailable and a migration is written manually, mirror the generated migration metadata before relying on `Database.MigrateAsync`.

Hand-written `MigrationBuilder` foreign-key calls should use named `principalTable` and `principalColumn` arguments rather than positional overloads.

**Why:** The positional overload can interpret the principal key as a schema name, producing a runtime PostgreSQL migration error even though the project compiles.

**How to apply:** Prefer the named-argument form for every manually authored foreign key, then restart the API and run a migration-backed test before continuing.

Follow-up schema migrations that repair fields added after an earlier migration was already applied should be idempotent when test hosts can start concurrently.

**Why:** Parallel API test hosts can race on an older migration history; an ordinary `AddColumn` can fail with a duplicate-column error even when the schema is otherwise valid.

**How to apply:** Use guarded SQL such as `ADD COLUMN IF NOT EXISTS` for narrowly scoped repair migrations, while keeping explicit migration metadata.