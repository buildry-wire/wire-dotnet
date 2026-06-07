# Changelog

Format: Keep a Changelog; semver.

## [Unreleased]

## [1.0.0] - 2026-06-07
First public release.

### Added
- `WireClient` with Bearer auth, idempotency keys, retry/backoff with jitter honoring `Retry-After`.
- Resources: `PaymentIntents`, `Charges`, `Events`, `WebhookEndpoints`.
- Cursor auto-pagination via `IAsyncEnumerable<T>`.
- Webhook signature verification (`Webhooks.Verify` / `VerifyAt`).
- Typed `WireException` (with `WireConnectionException` for network/timeout failures).
