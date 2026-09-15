# Modernization guidance

The current layering creates migration seams while preserving a classic .NET Framework/WCF implementation.

## Recommended incremental path

1. Characterize SOAP behavior with contract and integration tests, including fault codes and duplicate import semantics.
2. Replace database initialization with reviewed EF migrations and move SQL Server connection configuration to deployment-managed settings.
3. Add structured telemetry, correlation IDs, health checks, and explicit request limits.
4. Introduce authenticated HTTPS before any non-local exposure.
5. Host the existing business layer behind a parallel modern endpoint. Keep WCF and the new adapter active during consumer migration.
6. Move persistence and business assemblies to multi-targeted or .NET Standard-compatible boundaries, eliminating EF6 proxy assumptions.
7. Define a versioned gRPC/Protobuf or HTTP contract with explicit decimal, UTC timestamp, validation, and error mappings.
8. Cut consumers over in measured waves, preserving idempotency on `externalId`, then retire WCF only after traffic and rollback criteria are satisfied.

## Compatibility risks

- SOAP clients may depend on generated namespaces, member order, and typed-fault shapes.
- `decimal` precision and date-time kind must be specified in any new protocol.
- Account balance changes and imported transactions must remain atomic.
- Retry policies must not create duplicate imports; the external ID remains the idempotency key.
- LocalDB setup is not a production migration strategy.

No modernization step should silently change fault codes, seed identifiers, range limits, ordering, or balance semantics.
