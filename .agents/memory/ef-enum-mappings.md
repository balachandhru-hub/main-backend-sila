---
name: EF enum storage alignment
description: Prevent runtime materialization failures when an enum-backed PostgreSQL column uses a different storage representation.
---

Enum properties must explicitly match the database representation already used by the schema; migrations may store an enum as text even when the CLR default is integer.

**Why:** A query that includes extraction history failed at runtime because one legacy enum column was varchar in PostgreSQL while EF still expected an integer. Builds and writes did not expose the mismatch.

**How to apply:** When adding or loading enum-backed entities, inspect the actual column type and configure `HasConversion<string>()` or the numeric mapping consistently before relying on Include/materialization tests.