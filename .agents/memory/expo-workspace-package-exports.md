---
name: Expo workspace package exports
description: Metro resolution behavior for local TypeScript workspace packages used by the Expo artifact.
---

Local packages consumed by the Expo app should use a simple package export such as `"." : "./src/index.ts"`. Conditional `types`/`default` export objects can typecheck in TypeScript but fail to resolve in Metro even when pnpm has created the workspace symlink.

**Why:** The Expo web bundler uses Metro's package resolver, which is stricter than TypeScript's bundler resolution for workspace package export maps.

**How to apply:** When adding a shared workspace package to the mobile app, keep the root export string-based and point it directly to source; run the Expo workflow after linking the package to catch resolver issues.