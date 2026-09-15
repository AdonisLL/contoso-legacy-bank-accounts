# Architecture

## Runtime view

```text
SOAP client
  -> BasicHttpBinding / AccountService (console self-host)
  -> IAccountManager / AccountManager
  -> IAccountRepository / EfAccountRepository
  -> EF6 AccountsDbContext
  -> SQL LocalDB: ContosoLegacyBank
```

The service boundary owns transport concerns and converts known business exceptions to typed `AccountFault` values. It does not expose internal exceptions. The business layer owns input validation, date-range policy, duplicate-import semantics, DTO mapping, and account balance updates. The data layer owns persistence and the unique external transaction ID constraint.

## Dependency seams

- `IAccountManager` separates WCF hosting from business behavior.
- `IAccountRepository` separates use cases from EF6 and enables fast unit tests.
- `Func<IAccountManager>` permits service-level composition without a container.
- DTOs and service contracts live in a dependency-light assembly.

Each request creates and disposes its own EF context. WCF's default per-call instancing therefore has no shared mutable context. Import updates the transaction and account balance in one EF `SaveChanges` transaction. The pre-check gives a friendly duplicate response while the unique index provides the authoritative race-safe guard.

## Data model

- Customer: unique customer number and profile
- Account: unique account number, owner, type, currency, balance, status
- AccountTransaction: globally unique external ID, account, UTC post time, amount, description, type

Seed data is deterministic and idempotent. Setup inserts missing known rows; reset drops and rebuilds the local catalog.

## Operational boundaries

This sample is local/development software. `BasicHttpBinding` uses HTTP without transport authentication to meet legacy compatibility requirements; it must not be exposed beyond localhost or a trusted development environment. Production deployment would require HTTPS, authentication/authorization, centralized telemetry, managed database resilience, and formal schema migrations.
