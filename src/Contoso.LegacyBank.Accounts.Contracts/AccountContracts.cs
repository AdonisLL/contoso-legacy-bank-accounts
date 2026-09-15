using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Contoso.LegacyBank.Accounts.Contracts
{
    [ServiceContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public interface IAccountService
    {
        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        CustomerDto GetCustomer(string customerNumber);

        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        IList<AccountDto> GetAccounts(string customerNumber);

        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        IList<TransactionDto> GetTransactions(string accountNumber, DateTime fromDate, DateTime toDate);

        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        TransactionDto ImportTransaction(ImportTransactionRequest request);

        [OperationContract]
        [FaultContract(typeof(AccountFault))]
        string Ping();
    }

    [DataContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public sealed class CustomerDto
    {
        [DataMember(Order = 1)] public string CustomerNumber { get; set; }
        [DataMember(Order = 2)] public string FirstName { get; set; }
        [DataMember(Order = 3)] public string LastName { get; set; }
        [DataMember(Order = 4)] public string Email { get; set; }
        [DataMember(Order = 5)] public DateTime CreatedUtc { get; set; }
    }

    [DataContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public sealed class AccountDto
    {
        [DataMember(Order = 1)] public string AccountNumber { get; set; }
        [DataMember(Order = 2)] public string CustomerNumber { get; set; }
        [DataMember(Order = 3)] public string AccountType { get; set; }
        [DataMember(Order = 4)] public string CurrencyCode { get; set; }
        [DataMember(Order = 5)] public decimal Balance { get; set; }
        [DataMember(Order = 6)] public DateTime OpenedUtc { get; set; }
        [DataMember(Order = 7)] public bool IsActive { get; set; }
    }

    [DataContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public sealed class TransactionDto
    {
        [DataMember(Order = 1)] public string ExternalId { get; set; }
        [DataMember(Order = 2)] public string AccountNumber { get; set; }
        [DataMember(Order = 3)] public DateTime PostedUtc { get; set; }
        [DataMember(Order = 4)] public decimal Amount { get; set; }
        [DataMember(Order = 5)] public string Description { get; set; }
        [DataMember(Order = 6)] public string TransactionType { get; set; }
    }

    [DataContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public sealed class ImportTransactionRequest
    {
        [DataMember(Order = 1, IsRequired = true)] public string ExternalId { get; set; }
        [DataMember(Order = 2, IsRequired = true)] public string AccountNumber { get; set; }
        [DataMember(Order = 3)] public DateTime PostedUtc { get; set; }
        [DataMember(Order = 4)] public decimal Amount { get; set; }
        [DataMember(Order = 5)] public string Description { get; set; }
        [DataMember(Order = 6)] public string TransactionType { get; set; }
    }

    [DataContract(Namespace = "urn:contoso:legacy-bank:accounts:v1")]
    public sealed class AccountFault
    {
        [DataMember(Order = 1)] public string Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }
        [DataMember(Order = 3, EmitDefaultValue = false)] public string Field { get; set; }
    }
}
