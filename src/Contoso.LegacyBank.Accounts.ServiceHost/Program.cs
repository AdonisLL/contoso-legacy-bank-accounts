using System;
using System.ServiceModel;
using Contoso.LegacyBank.Accounts.Data;

namespace Contoso.LegacyBank.Accounts.ServiceHost
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (HasArgument(args, "--reset")) { DatabaseManager.Reset(); Console.WriteLine("ContosoLegacyBank reset and seeded."); return 0; }
                DatabaseManager.Setup();
                if (HasArgument(args, "--setup")) { Console.WriteLine("ContosoLegacyBank created/updated and seeded."); return 0; }
                var host = new System.ServiceModel.ServiceHost(typeof(AccountService));
                try
                {
                    host.Open();
                    Console.WriteLine("AccountService listening at http://localhost:8090/AccountService");
                    Console.WriteLine("Press ENTER to stop.");
                    Console.ReadLine();
                    host.Close();
                }
                catch
                {
                    host.Abort();
                    throw;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Service failed: " + ex.Message);
                return 1;
            }
        }

        private static bool HasArgument(string[] args, string expected) => Array.Exists(args ?? new string[0], x => string.Equals(x, expected, StringComparison.OrdinalIgnoreCase));
    }
}
