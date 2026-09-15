using System;
using System.Collections.Generic;
using System.ServiceModel;
using Contoso.LegacyBank.Accounts.Business;
using Contoso.LegacyBank.Accounts.Contracts;
using Contoso.LegacyBank.Accounts.Data;

namespace Contoso.LegacyBank.Accounts.ServiceHost
{
    public sealed class AccountService : IAccountService
    {
        private readonly Func<IAccountManager> managerFactory;
        public AccountService() : this(() => new AccountManager(new EfAccountRepository(new AccountsDbContext()))) { }
        public AccountService(Func<IAccountManager> managerFactory) { this.managerFactory = managerFactory ?? throw new ArgumentNullException(nameof(managerFactory)); }

        public CustomerDto GetCustomer(string customerNumber) => Execute(x => x.GetCustomer(customerNumber));
        public IList<AccountDto> GetAccounts(string customerNumber) => Execute(x => x.GetAccounts(customerNumber));
        public IList<TransactionDto> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate) => Execute(x => x.GetTransactions(accountNumber, fromDate, toDate));
        public TransactionDto ImportTransaction(ImportTransactionRequest request) => Execute(x => x.ImportTransaction(request));
        public string Ping() => "PONG";

        private T Execute<T>(Func<IAccountManager, T> action)
        {
            try
            {
                using (var manager = managerFactory()) return action(manager);
            }
            catch (AccountBusinessException ex)
            {
                throw new FaultException<AccountFault>(new AccountFault { Code = ex.Code, Message = ex.Message, Field = ex.Field }, new FaultReason(ex.Message));
            }
            catch (FaultException) { throw; }
            catch (Exception)
            {
                throw new FaultException<AccountFault>(new AccountFault { Code = "InternalError", Message = "The service could not complete the request." }, new FaultReason("The service could not complete the request."));
            }
        }
    }
}
