using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.ReferenceData
{
    public static class Accounts
    {
        private static Dictionary<int, Account> accounts = new Dictionary<int, Account>();
        static Accounts()
        {
            JObject ds = JObject.Parse(File.ReadAllText($"{Directory.GetCurrentDirectory()}\\ReferenceData\\Accounts.json"));
            foreach (var item in ds["Accounts"])
            {
                Account a = new Account();
                
                int i = Int32.Parse(item["seq"].ToString());
                a.AccountNumber = item["accno"].ToString().ToUpper();
                a.AddressLine1 = item["street"].ToString();
                a.AddressLine2 = String.Empty;
                a.City = item["city"].ToString();
                a.PostalCode = $"{item["zip"].ToString()} {item["state"].ToString()}";
                a.SingleName = $"{item["name"]["first"]} {item["name"]["last"]}";


                accounts.Add(i, a);
            }
           
        }
        public static Account GetRandomAccount()
        {
            Random r = new Random();
            int i = r.Next(1, accounts.Count);
            return accounts[i];
        }
    }

    public class Account
    {
        public string AccountNumber { get; set; }
        public string SingleName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        
    }
}
