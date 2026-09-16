# Security Assessment Report

**Generated:** 2026-09-16T04:26:43.425632Z

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

No CVE vulnerabilities met the high severity threshold.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-662: Improper Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Accounts.Data/AccountData.cs

EfAccountRepository.AddTransaction reads a tracked Account, updates Balance with a read-modify-write operation at line 97, and saves at line 100 without a concurrency token or synchronization. Concurrent requests can overwrite one another and lose a balance update.

### CWE-820: Missing Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Accounts.Data/AccountData.cs

EfAccountRepository.AddTransaction reads a tracked Account, updates Balance with a read-modify-write operation at line 97, and saves at line 100 without a concurrency token or synchronization. Concurrent requests can overwrite one another and lose a balance update.

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/Contoso.LegacyBank.Accounts.Business/AccountManager.cs, src/Contoso.LegacyBank.Accounts.ServiceHost/AccountService.cs

AccountManager.ImportTransaction (lines 55-79) processes and rejects financial transaction requests without recording an audit event, while AccountService.Execute (lines 22-36) converts failures to typed faults without logging them.

