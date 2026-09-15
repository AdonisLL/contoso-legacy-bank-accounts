using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace Contoso.LegacyBank.Accounts.Data
{
    public class Customer
    {
        public int Id { get; set; }
        [Required, StringLength(20), Index("UX_Customer_Number", IsUnique = true)] public string CustomerNumber { get; set; }
        [Required, StringLength(80)] public string FirstName { get; set; }
        [Required, StringLength(80)] public string LastName { get; set; }
        [Required, StringLength(200)] public string Email { get; set; }
        public DateTime CreatedUtc { get; set; }
        public virtual ICollection<Account> Accounts { get; set; } = new HashSet<Account>();
    }

    public class Account
    {
        public int Id { get; set; }
        [Required, StringLength(20), Index("UX_Account_Number", IsUnique = true)] public string AccountNumber { get; set; }
        public int CustomerId { get; set; }
        [Required, StringLength(20)] public string AccountType { get; set; }
        [Required, StringLength(3)] public string CurrencyCode { get; set; }
        public decimal Balance { get; set; }
        public DateTime OpenedUtc { get; set; }
        public bool IsActive { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual ICollection<AccountTransaction> Transactions { get; set; } = new HashSet<AccountTransaction>();
    }

    public class AccountTransaction
    {
        public int Id { get; set; }
        [Required, StringLength(64), Index("UX_Transaction_ExternalId", IsUnique = true)] public string ExternalId { get; set; }
        public int AccountId { get; set; }
        public DateTime PostedUtc { get; set; }
        public decimal Amount { get; set; }
        [Required, StringLength(200)] public string Description { get; set; }
        [Required, StringLength(20)] public string TransactionType { get; set; }
        public virtual Account Account { get; set; }
    }

    public sealed class AccountsDbContext : DbContext
    {
        public AccountsDbContext() : base("name=ContosoLegacyBank") { }
        public AccountsDbContext(string connectionString) : base(connectionString) { }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountTransaction> Transactions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().Property(x => x.Balance).HasPrecision(19, 4);
            modelBuilder.Entity<AccountTransaction>().Property(x => x.Amount).HasPrecision(19, 4);
            modelBuilder.Entity<Customer>().HasMany(x => x.Accounts).WithRequired(x => x.Customer).HasForeignKey(x => x.CustomerId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Account>().HasMany(x => x.Transactions).WithRequired(x => x.Account).HasForeignKey(x => x.AccountId).WillCascadeOnDelete(false);
            base.OnModelCreating(modelBuilder);
        }
    }

    public interface IAccountRepository : IDisposable
    {
        Customer FindCustomer(string customerNumber);
        IList<Account> FindAccounts(string customerNumber);
        Account FindAccount(string accountNumber);
        IList<AccountTransaction> FindTransactions(string accountNumber, DateTime fromUtc, DateTime toUtc);
        bool ExternalIdExists(string externalId);
        AccountTransaction AddTransaction(Account account, string externalId, DateTime postedUtc, decimal amount, string description, string transactionType);
    }

    public sealed class DuplicateTransactionException : Exception
    {
        public DuplicateTransactionException(Exception innerException) : base("The transaction external ID already exists.", innerException) { }
    }

    public sealed class EfAccountRepository : IAccountRepository
    {
        private readonly AccountsDbContext context;
        public EfAccountRepository(AccountsDbContext context) { this.context = context ?? throw new ArgumentNullException(nameof(context)); }

        public Customer FindCustomer(string customerNumber) => context.Customers.AsNoTracking().SingleOrDefault(x => x.CustomerNumber == customerNumber);
        public IList<Account> FindAccounts(string customerNumber) => context.Accounts.AsNoTracking().Include(x => x.Customer).Where(x => x.Customer.CustomerNumber == customerNumber).OrderBy(x => x.AccountNumber).ToList();
        public Account FindAccount(string accountNumber) => context.Accounts.SingleOrDefault(x => x.AccountNumber == accountNumber);
        public IList<AccountTransaction> FindTransactions(string accountNumber, DateTime fromUtc, DateTime toUtc) => context.Transactions.AsNoTracking().Include(x => x.Account).Where(x => x.Account.AccountNumber == accountNumber && x.PostedUtc >= fromUtc && x.PostedUtc <= toUtc).OrderByDescending(x => x.PostedUtc).ThenBy(x => x.ExternalId).ToList();
        public bool ExternalIdExists(string externalId) => context.Transactions.Any(x => x.ExternalId == externalId);

        public AccountTransaction AddTransaction(Account account, string externalId, DateTime postedUtc, decimal amount, string description, string transactionType)
        {
            var transaction = new AccountTransaction { AccountId = account.Id, Account = account, ExternalId = externalId, PostedUtc = postedUtc, Amount = amount, Description = description, TransactionType = transactionType };
            context.Transactions.Add(transaction);
            account.Balance += amount;
            try
            {
                context.SaveChanges();
                return transaction;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                throw new DuplicateTransactionException(ex);
            }
        }

        public void Dispose() => context.Dispose();

        private static bool IsUniqueConstraintViolation(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                var sqlException = current as SqlException;
                if (sqlException != null && (sqlException.Number == 2601 || sqlException.Number == 2627)) return true;
            }
            return false;
        }
    }

    public static class DatabaseManager
    {
        public static void Setup()
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<AccountsDbContext>());
            using (var context = new AccountsDbContext())
            {
                context.Database.Initialize(false);
                Seed(context);
            }
        }

        public static void Reset()
        {
            Database.SetInitializer<AccountsDbContext>(null);
            using (var context = new AccountsDbContext()) context.Database.Delete();
            Setup();
        }

        public static void Seed(AccountsDbContext context)
        {
            UpsertCustomer(context, "CUST-1001", "Avery", "Morgan", "avery.morgan@example.test", new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc));
            UpsertCustomer(context, "CUST-1002", "Jordan", "Lee", "jordan.lee@example.test", new DateTime(2021, 6, 10, 0, 0, 0, DateTimeKind.Utc));
            context.SaveChanges();
            UpsertAccount(context, "CHK-10010001", "CUST-1001", "Checking", 2480.25m, new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc));
            UpsertAccount(context, "SAV-10010002", "CUST-1001", "Savings", 10500m, new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc));
            UpsertAccount(context, "CHK-10020001", "CUST-1002", "Checking", 875.50m, new DateTime(2021, 6, 10, 0, 0, 0, DateTimeKind.Utc));
            context.SaveChanges();
            UpsertTransaction(context, "SEED-1001-001", "CHK-10010001", new DateTime(2024, 1, 5, 12, 0, 0, DateTimeKind.Utc), 2500m, "Payroll deposit", "Credit");
            UpsertTransaction(context, "SEED-1001-002", "CHK-10010001", new DateTime(2024, 1, 7, 12, 0, 0, DateTimeKind.Utc), -19.75m, "Card purchase", "Debit");
            UpsertTransaction(context, "SEED-1001-003", "SAV-10010002", new DateTime(2024, 1, 31, 12, 0, 0, DateTimeKind.Utc), 25m, "Interest", "Credit");
            UpsertTransaction(context, "SEED-1002-001", "CHK-10020001", new DateTime(2024, 1, 9, 12, 0, 0, DateTimeKind.Utc), 900m, "Payroll deposit", "Credit");
            UpsertTransaction(context, "SEED-1002-002", "CHK-10020001", new DateTime(2024, 1, 11, 12, 0, 0, DateTimeKind.Utc), -24.50m, "Utility payment", "Debit");
            context.SaveChanges();
        }

        private static void UpsertCustomer(AccountsDbContext context, string number, string first, string last, string email, DateTime created)
        {
            if (!context.Customers.Any(x => x.CustomerNumber == number)) context.Customers.Add(new Customer { CustomerNumber = number, FirstName = first, LastName = last, Email = email, CreatedUtc = created });
        }

        private static void UpsertAccount(AccountsDbContext context, string number, string customerNumber, string type, decimal balance, DateTime opened)
        {
            if (!context.Accounts.Any(x => x.AccountNumber == number))
            {
                var customer = context.Customers.Single(x => x.CustomerNumber == customerNumber);
                context.Accounts.Add(new Account { AccountNumber = number, CustomerId = customer.Id, AccountType = type, CurrencyCode = "USD", Balance = balance, OpenedUtc = opened, IsActive = true });
            }
        }

        private static void UpsertTransaction(AccountsDbContext context, string externalId, string accountNumber, DateTime posted, decimal amount, string description, string type)
        {
            if (!context.Transactions.Any(x => x.ExternalId == externalId))
            {
                var account = context.Accounts.Single(x => x.AccountNumber == accountNumber);
                context.Transactions.Add(new AccountTransaction { ExternalId = externalId, AccountId = account.Id, PostedUtc = posted, Amount = amount, Description = description, TransactionType = type });
            }
        }
    }
}
