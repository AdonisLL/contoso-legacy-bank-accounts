# Contoso Legacy Bank Accounts

A deliberately traditional, but secure-by-default, .NET Framework 4.8 accounts service. It uses classic non-SDK projects, `packages.config`, WCF self-hosting, `BasicHttpBinding`, Entity Framework 6 code-first, and SQL Server LocalDB.

## Prerequisites

- Windows
- Visual Studio 2022 Build Tools with the .NET Framework 4.8 developer pack
- SQL Server Express LocalDB (`MSSQLLocalDB`)
- NuGet CLI

No database password or application credential is required. LocalDB uses Windows integrated security.

## Restore, build, and test

```powershell
nuget restore .\Contoso.LegacyBank.Accounts.sln -PackagesDirectory .\packages
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" .\Contoso.LegacyBank.Accounts.sln /m /p:Configuration=Release
.\packages\NUnit.ConsoleRunner.3.18.3\tools\nunit3-console.exe .\tests\Contoso.LegacyBank.Accounts.Tests\bin\Release\Contoso.LegacyBank.Accounts.Tests.dll
```

## Database setup and reset

Both commands create deterministic sample data and are safe to run repeatedly:

```powershell
.\scripts\setup.ps1
.\scripts\reset.ps1
```

Setup preserves imported transactions and inserts only missing seed records. Reset deletes and recreates the `ContosoLegacyBank` LocalDB database.

Seed identifiers include customers `CUST-1001` and `CUST-1002`, and accounts `CHK-10010001`, `SAV-10010002`, and `CHK-10020001`.

## Run the service

```powershell
.\src\Contoso.LegacyBank.Accounts.ServiceHost\bin\Release\Contoso.LegacyBank.Accounts.ServiceHost.exe
```

The SOAP endpoint is `http://localhost:8090/AccountService` and metadata is available at `http://localhost:8090/AccountService?wsdl`. Press Enter to stop.
Use `--noninteractive` when a script or process supervisor starts the host; it remains active until Ctrl+C or process termination.

Operations:

- `GetCustomer(customerNumber)`
- `GetAccounts(customerNumber)`
- `GetTransactions(accountNumber, fromDate, toDate)` (maximum 366-day range)
- `ImportTransaction(request)`
- `Ping()`

All service DTOs use explicit `DataContract`/`DataMember` declarations. Business and validation errors are returned as typed `AccountFault` SOAP faults. `ImportTransaction` enforces a database-level unique external ID and reports `DuplicateExternalId` for repeated imports.

## Repository structure

- `src/...Contracts`: WCF service contract, DTOs, and typed fault
- `src/...Data`: EF6 entities, context, repository, setup, and deterministic seed
- `src/...Business`: validation, mapping, use cases, and dependency seams
- `src/...ServiceHost`: console host and WCF adapter
- `tests`: unit tests plus LocalDB/WCF integration tests
- `docs`: current architecture and modernization guidance

The integration suite resets the local test database and binds port 8091. Do not run it against a LocalDB catalog containing data you need to preserve.
