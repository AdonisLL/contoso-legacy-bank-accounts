using System;
using System.Collections.Generic;
using System.Linq;
using Contoso.LegacyBank.Accounts.Contracts;
using Contoso.LegacyBank.Accounts.Data;

namespace Contoso.LegacyBank.Accounts.Business
{
    public sealed class AccountBusinessException : Exception
    {
        public AccountBusinessException(string code, string message, string field = null) : base(message) { Code = code; Field = field; }
        public string Code { get; }
        public string Field { get; }
    }

    public interface IAccountManager : IDisposable
    {
        CustomerDto GetCustomer(string customerNumber);
        IList<AccountDto> GetAccounts(string customerNumber);
        IList<TransactionDto> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate);
        TransactionDto ImportTransaction(ImportTransactionRequest request);
    }

    public sealed class AccountManager : IAccountManager
    {
        private readonly IAccountRepository repository;
        public AccountManager(IAccountRepository repository) { this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); }

        public CustomerDto GetCustomer(string customerNumber)
        {
            customerNumber = Required(customerNumber, "customerNumber");
            var customer = repository.FindCustomer(customerNumber);
            if (customer == null) throw new AccountBusinessException("CustomerNotFound", "The customer was not found.", "customerNumber");
            return new CustomerDto { CustomerNumber = customer.CustomerNumber, FirstName = customer.FirstName, LastName = customer.LastName, Email = customer.Email, CreatedUtc = SpecifyUtc(customer.CreatedUtc) };
        }

        public IList<AccountDto> GetAccounts(string customerNumber)
        {
            customerNumber = Required(customerNumber, "customerNumber");
            if (repository.FindCustomer(customerNumber) == null) throw new AccountBusinessException("CustomerNotFound", "The customer was not found.", "customerNumber");
            return repository.FindAccounts(customerNumber).Select(MapAccount).ToList();
        }

        public IList<TransactionDto> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate)
        {
            accountNumber = Required(accountNumber, "accountNumber");
            var fromUtc = NormalizeUtc(fromDate, "fromDate");
            var toUtc = NormalizeUtc(toDate, "toDate");
            if (fromUtc > toUtc) throw new AccountBusinessException("InvalidDateRange", "fromDate must be on or before toDate.", "fromDate");
            if ((toUtc - fromUtc).TotalDays > 366) throw new AccountBusinessException("DateRangeTooLarge", "The transaction date range cannot exceed 366 days.", "toDate");
            if (repository.FindAccount(accountNumber) == null) throw new AccountBusinessException("AccountNotFound", "The account was not found.", "accountNumber");
            return repository.FindTransactions(accountNumber, fromUtc, toUtc).Select(MapTransaction).ToList();
        }

        public TransactionDto ImportTransaction(ImportTransactionRequest request)
        {
            if (request == null) throw new AccountBusinessException("ValidationError", "A request is required.", "request");
            var externalId = Required(request.ExternalId, "externalId");
            var accountNumber = Required(request.AccountNumber, "accountNumber");
            var description = Required(request.Description, "description");
            var type = Required(request.TransactionType, "transactionType");
            if (externalId.Length > 64) throw new AccountBusinessException("ValidationError", "externalId cannot exceed 64 characters.", "externalId");
            if (description.Length > 200) throw new AccountBusinessException("ValidationError", "description cannot exceed 200 characters.", "description");
            if (type != "Credit" && type != "Debit") throw new AccountBusinessException("ValidationError", "transactionType must be Credit or Debit.", "transactionType");
            if (request.Amount == 0) throw new AccountBusinessException("ValidationError", "amount cannot be zero.", "amount");
            if ((type == "Credit" && request.Amount < 0) || (type == "Debit" && request.Amount > 0)) throw new AccountBusinessException("ValidationError", "amount sign does not match transactionType.", "amount");
            var postedUtc = NormalizeUtc(request.PostedUtc, "postedUtc");
            var account = repository.FindAccount(accountNumber);
            if (account == null) throw new AccountBusinessException("AccountNotFound", "The account was not found.", "accountNumber");
            if (!account.IsActive) throw new AccountBusinessException("AccountInactive", "The account is inactive.", "accountNumber");
            if (repository.ExternalIdExists(externalId)) throw DuplicateExternalId();
            try
            {
                return MapTransaction(repository.AddTransaction(account, externalId, postedUtc, request.Amount, description, type));
            }
            catch (DuplicateTransactionException)
            {
                throw DuplicateExternalId();
            }
        }

        public void Dispose() => repository.Dispose();

        private static string Required(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new AccountBusinessException("ValidationError", field + " is required.", field);
            return value.Trim();
        }

        private static DateTime NormalizeUtc(DateTime value, string field)
        {
            if (value == default(DateTime)) throw new AccountBusinessException("ValidationError", field + " is required.", field);
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }

        private static DateTime SpecifyUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
        private static AccountBusinessException DuplicateExternalId() => new AccountBusinessException("DuplicateExternalId", "A transaction with this externalId already exists.", "externalId");
        private static AccountDto MapAccount(Account x) => new AccountDto { AccountNumber = x.AccountNumber, CustomerNumber = x.Customer?.CustomerNumber, AccountType = x.AccountType, CurrencyCode = x.CurrencyCode, Balance = x.Balance, OpenedUtc = SpecifyUtc(x.OpenedUtc), IsActive = x.IsActive };
        private static TransactionDto MapTransaction(AccountTransaction x) => new TransactionDto { ExternalId = x.ExternalId, AccountNumber = x.Account?.AccountNumber, PostedUtc = SpecifyUtc(x.PostedUtc), Amount = x.Amount, Description = x.Description, TransactionType = x.TransactionType };
    }
}
