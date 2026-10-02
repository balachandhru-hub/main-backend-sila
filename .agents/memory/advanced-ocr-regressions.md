---
name: Advanced OCR regression boundaries
description: OCR parsing and reread persistence constraints that are easy to break when providers change.
---

OCR provider normalization must preserve line boundaries; collapsing all whitespace causes labeled fields to consume the following header and can overflow short database columns.

**Why:** Both embedded PDF text and scanned fallback initially flattened invoice labels into one line, producing incorrect values and exposing failures only when advanced extraction persisted them.

**How to apply:** Keep newline-preserving normalization covered by digital and scanned fixtures, and test rereads in separate request scopes so history, line replacement, and denormalized extraction rows reflect production behavior.

Advanced rereads replace current invoice lines and denormalized extraction rows; those current-state records must not accumulate across immutable extraction-history entries.

**Why:** Reprocessing with tracked replacement entities can emit updates for nonexistent rows or leave prior denormalized rows behind, creating concurrency errors or duplicate current-state data.

**How to apply:** Assert multiple history rows with stable hashes while asserting one current invoice, one current line set, and one current denormalized row per line.