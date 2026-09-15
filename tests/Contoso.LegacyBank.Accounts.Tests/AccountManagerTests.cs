using System;
using System.Collections.Generic;
using Contoso.LegacyBank.Accounts.Business;
using Contoso.LegacyBank.Accounts.Contracts;
using Contoso.LegacyBank.Accounts.Data;
using NUnit.Framework;

namespace Contoso.LegacyBank.Accounts.Tests
{
    [TestFixture]
    public sealed class AccountManagerTests
    {
        [Test]
        public void GetCustomer_maps_known_customer()
        {
            using (var manager = new AccountManager(new FakeRepository()))
            {
                var result = manager.GetCustomer(" CUST-TEST ");
                Assert.That(result.CustomerNumber, Is.EqualTo("CUST-TEST"));
                Assert.That(result.FirstName, Is.EqualTo("Test"));
            }
        }

        [Test]
        public void GetTransactions_rejects_reversed_range()
        {
            using (var manager = new AccountManager(new FakeRepository()))
            {
                var error = Assert.Throws<AccountBusinessException>(() => manager.GetTransactions("ACC-TEST", new DateTime(2024, 2, 1), new DateTime(2024, 1, 1)));
                Assert.That(error.Code, Is.EqualTo("InvalidDateRange"));
            }
        }

        [Test]
        public void ImportTransaction_rejects_duplicate_external_id()
        {
            using (var manager = new AccountManager(new FakeRepository { DuplicateExists = true }))
            {
                var error = Assert.Throws<AccountBusinessException>(() => manager.ImportTransaction(ValidRequest()));
                Assert.That(error.Code, Is.EqualTo("DuplicateExternalId"));
                Assert.That(error.Field, Is.EqualTo("externalId"));
            }
        }

        [Test]
        public void ImportTransaction_updates_balance_and_returns_transaction()
        {
            var repository = new FakeRepository();
            using (var manager = new AccountManager(repository))
            {
                var result = manager.ImportTransaction(ValidRequest());
                Assert.That(result.ExternalId, Is.EqualTo("EXT-TEST-001"));
                Assert.That(repository.Account.Balance, Is.EqualTo(110m));
            }
        }

        private static ImportTransactionRequest ValidRequest() => new ImportTransactionRequest { ExternalId = "EXT-TEST-001", AccountNumber = "ACC-TEST", PostedUtc = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc), Amount = 10m, Description = "Test credit", TransactionType = "Credit" };

        private sealed class FakeRepository : IAccountRepository
        {
            public readonly Customer Customer = new Customer { CustomerNumber = "CUST-TEST", FirstName = "Test", LastName = "Customer", Email = "test@example.test", CreatedUtc = new DateTime(2020, 1, 1) };
            public readonly Account Account;
            public bool DuplicateExists { get; set; }
            public FakeRepository() { Account = new Account { Id = 1, AccountNumber = "ACC-TEST", Customer = Customer, AccountType = "Checking", CurrencyCode = "USD", Balance = 100m, OpenedUtc = new DateTime(2020, 1, 1), IsActive = true }; }
            public Customer FindCustomer(string customerNumber) => customerNumber == Customer.CustomerNumber ? Customer : null;
            public IList<Account> FindAccounts(string customerNumber) => new List<Account> { Account };
            public Account FindAccount(string accountNumber) => accountNumber == Account.AccountNumber ? Account : null;
            public IList<AccountTransaction> FindTransactions(string accountNumber, DateTime fromUtc, DateTime toUtc) => new List<AccountTransaction>();
            public bool ExternalIdExists(string externalId) => DuplicateExists;
            public AccountTransaction AddTransaction(Account account, string externalId, DateTime postedUtc, decimal amount, string description, string transactionType) { account.Balance += amount; return new AccountTransaction { ExternalId = externalId, Account = account, PostedUtc = postedUtc, Amount = amount, Description = description, TransactionType = transactionType }; }
            public void Dispose() { }
        }
    }
}
