# Lab3.2-ProvidedAlgorithm

The "provided algorithm" for Lab 3.2 — a deliberately naive re-implementation
of Lab 3.1's in-memory confidential/public data store, written as a developer
unfamiliar with Lab 3.1's findings might reasonably write one. It exists
**only** as a comparison baseline for `Lab3.2-Report`'s code-security
methodology — do not use any of its patterns as an example to follow.

Known, intentional issues (analyzed in the report):

- No input validation: malformed/mistyped/missing JSON fields all fall
  through to a single top-level `catch` that returns the raw exception
  message with HTTP 500.
- No request size limit at all.
- "Encryption" is XOR with a key hardcoded in source (`Program.cs`) — this
  is included specifically because it is a common shape of real-world
  mistake — deterministic, unauthenticated, and instantly reversible by
  anyone who can read the source.
- A plaintext `activityLog` that is appended to on every operation and
  never cleared, permanently retaining confidential values in memory for
  the life of the process.
- A plain `Dictionary` and a non-atomic `id++` counter shared across
  concurrent requests, with no locking.

Run with `dotnet run` (listens on `http://localhost:5090/`).
