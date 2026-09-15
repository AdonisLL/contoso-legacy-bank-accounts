using System;
using System.ServiceModel;
using Contoso.LegacyBank.Accounts.Contracts;
using Contoso.LegacyBank.Accounts.Data;
using Contoso.LegacyBank.Accounts.ServiceHost;
using NUnit.Framework;

namespace Contoso.LegacyBank.Accounts.Tests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class ServiceIntegrationTests
    {
        private System.ServiceModel.ServiceHost host;
        private ChannelFactory<IAccountService> factory;
        private IAccountService client;

        [OneTimeSetUp]
        public void StartService()
        {
            DatabaseManager.Reset();
            var address = new Uri("http://127.0.0.1:8091/AccountService");
            host = new System.ServiceModel.ServiceHost(typeof(AccountService), address);
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None) { HostNameComparisonMode = HostNameComparisonMode.Exact };
            host.AddServiceEndpoint(typeof(IAccountService), binding, "");
            host.Open();
            factory = new ChannelFactory<IAccountService>(binding, new EndpointAddress(address));
            client = factory.CreateChannel();
        }

        [OneTimeTearDown]
        public void StopService()
        {
            try { ((IClientChannel)client)?.Close(); } catch { ((IClientChannel)client)?.Abort(); }
            try { factory?.Close(); } catch { factory?.Abort(); }
            try { host?.Close(); } catch { host?.Abort(); }
        }

        [Test]
        public void Ping_and_seeded_queries_work_over_basic_http()
        {
            Assert.That(client.Ping(), Is.EqualTo("PONG"));
            Assert.That(client.GetCustomer("CUST-1001").LastName, Is.EqualTo("Morgan"));
            Assert.That(client.GetAccounts("CUST-1001"), Has.Count.EqualTo(2));
            Assert.That(client.GetTransactions("CHK-10010001", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc)), Has.Count.EqualTo(2));
        }

        [Test]
        public void Duplicate_import_is_returned_as_typed_fault()
        {
            var request = new ImportTransactionRequest { ExternalId = "INTEGRATION-DUPLICATE-001", AccountNumber = "CHK-10010001", PostedUtc = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc), Amount = 5m, Description = "Integration credit", TransactionType = "Credit" };
            client.ImportTransaction(request);
            var fault = Assert.Throws<FaultException<AccountFault>>(() => client.ImportTransaction(request));
            Assert.That(fault.Detail.Code, Is.EqualTo("DuplicateExternalId"));
        }
    }
}
