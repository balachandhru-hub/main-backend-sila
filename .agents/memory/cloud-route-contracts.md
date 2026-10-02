---
name: Cloud route contracts
description: Route naming discipline for the SILA ME Cloud frontend foundation.
---

Canonical Cloud route names should follow the product specification exactly, especially for module families such as Menu Engineering and Administration. Navigation links, router entries, and readiness documentation must use the same canonical paths.

**Why:** A delegated frontend implementation used shortened aliases that rendered correctly but did not match the named route contract, making direct links and acceptance checks inconsistent.

**How to apply:** When reviewing or extending Cloud navigation, compare every explicit route in the product brief against both the link configuration and the flat wouter route list before final verification.