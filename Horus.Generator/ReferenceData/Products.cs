using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.ReferenceData
{
    public static class Products
    {
        private static Random r = new Random();
        private static Dictionary<int, Product> products = new Dictionary<int, Product>();
        private static char[] letters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f' };
        private static string[] sizes = new string[] { "XX-SMALL", "X-SMALL", "SMALL", "MEDIUM", "Large", "X-LARGE", "XX-LARGE" };
        private static string[] colours = new string[] { "Bright Red", "Cool Blue", "Dusky Yellow", "Midnight Black", "Deep Purple", "Arctic White", "Dark Grey" };
        private static string[] fits = new string[] { "Skinny", "Slim", "Classic", "Big and Tall"};
        private static string[] garments = new string[] { "T-SHIRT (crew neck)", "T-SHIRT (V-neck)", "Woven Long sleeved T", "Jacket", "Trousers", "Jeans", "Beanie" };


        static Products()
        {
            for (int i = 0; i < 1000; i++)
            {
                Product p = new Product();
                p.Taxable = false;
                p.Price = r.NextDouble() * 100 * r.NextDouble();
                p.Discount = r.Next(0, 8);
                if (r.Next(1, 10) > 5) p.Taxable = true;
                string prefix = "";
                for (int j = 0; j < 4; j++)
                {
                    prefix += letters[j];
                }
                p.Isbn = prefix + r.Next(10000, 99999);
                p.Title = GenerateTitle();
                products.Add(i, p);
            }
           
        }

        private static string GenerateTitle()
        {
            var retval = "";
            int rSize = r.Next(0, sizes.Count() - 1);
            int rColour = r.Next(0, colours.Count() - 1);
            int rFit = r.Next(0, fits.Count() - 1);
            int rGarment = r.Next(0, garments.Count() - 1);
            retval = $"{garments[rGarment]} {sizes[rSize]} {fits[rFit]} {colours[rColour]}";
            return retval.ToLower();
        }

        public static Product GetRandomProduct()
        {
            
            int i = r.Next(1, products.Count);
            return products[i];
        }
    }

    public class Product
    {
       
        public string Title { get; set; }
        public string Isbn { get; set; }

        public double Discount { get; set; }
        public double Price { get; set; }
        public bool Taxable { get; set; }

    }
}
