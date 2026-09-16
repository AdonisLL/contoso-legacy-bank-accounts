# Security Assessment Report

**Generated:** 2026-09-16T00:51:09.0635160Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 3 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 3 |
| Total Rules Assessed | 59 |
| Rules Passed | 56 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 0 |
| optional | 0 |
| potential | 3 |

## CVE Findings (Dependency Vulnerabilities)

No CVE findings met the configured severity threshold.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-662: Improper Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Accounts.Business/AccountManager.cs:68, src/Contoso.LegacyBank.Accounts.Data/AccountData.cs:93

AccountManager.ImportTransaction loads the target account at line 68 and delegates transaction persistence at line 74. EfAccountRepository.AddTransaction then inserts the transaction and updates the shared account balance with a read-modify-write operation at lines 93-100 without a row-version concurrency token, serializable transaction, or atomic database update. Concurrent imports with different externalId values for the same account can both read the same starting balance and overwrite one balance update.

### CWE-820: Missing Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Accounts.ServiceHost/AccountService.cs:19, src/Contoso.LegacyBank.Accounts.Data/AccountData.cs:97

AccountService exposes ImportTransaction as a service operation at line 19. The underlying repository changes the persisted Account.Balance at AccountData.cs line 97 with no synchronization or database-side atomic update guarding concurrent requests that target the same account.

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/Contoso.LegacyBank.Accounts.ServiceHost/AccountService.cs:16, src/Contoso.LegacyBank.Accounts.ServiceHost/AccountService.cs:28, src/Contoso.LegacyBank.Accounts.Business/AccountManager.cs:55

AccountService exposes customer, account, transaction history, and transaction import operations at lines 16-19 and converts validation/business failures to WCF faults at lines 28-35 without recording audit or security logs. AccountManager.ImportTransaction begins at line 55 and validates/rejects transaction requests, duplicate external IDs, inactive accounts, and missing accounts, but these security-sensitive access and mutation events are not logged.
