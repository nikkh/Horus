using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Horus.Generator.Builders;
using Horus.Generator.Models;
using Horus.Generator.ReferenceData;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Newtonsoft.Json;

namespace Horus.Generator
{
    static class Generator
    {
        static Random random = new Random();
        public static readonly string saveExpectedResultsToScoreDatabase = ConfigurationManager.AppSettings["SaveExpectedResultsToScoreDatabase"];

        public static GeneratorSpecification Generate(Supplier supplier, int numDocuments = 1, int baseDocumentNumber = 15000)
        {
            bool save = false;
            if (saveExpectedResultsToScoreDatabase.ToLower() == "true")
            {
                save = true;
                Console.WriteLine($"Expected results will be saved to scores database");
            }
            else
            {
                Console.WriteLine($"Warning!! Expected results are not being saved.");
            }
            var gs = new GeneratorSpecification();
            gs.Header = new GeneratorHeader
            {
                LogoFile = supplier.LogoFile,
                SupplierFullName = supplier.SupplierFullName,
                DocumentType = "INVOICE",
                SupplierKey = supplier.SupplierKey,
                SupplierName = supplier.SupplierName,
                BuilderAssembly = supplier.BuilderAssembly,
                BuilderType = supplier.BuilderType
            };

            gs.Documents = new List<GeneratorDocument>();
           
            for (int d = 0; d < numDocuments; d++)
            {
                GeneratorDocument gd = new GeneratorDocument();
                gd.DocumentFormat = supplier.SupplierKey;
                gd.DocumentNumber = (baseDocumentNumber + 1 + d).ToString();
                gd.FileName = $"{gs.Header.DocumentType}-{gd.DocumentNumber}.pdf";
                gd.DocumentDate = DateTime.Now.Subtract(new TimeSpan(random.Next(1, 180), 0, 0, 0));
                if (random.Next(1,10) <= 3) gd.Notes = "Need to do something with this";
                var account = Accounts.GetRandomAccount();
                gd.PostalCode = account.PostalCode;
                gd.SingleName = account.SingleName;
                gd.Account = account.AccountNumber;
                gd.AddressLine1 = account.AddressLine1;
                gd.AddressLine2 = account.AddressLine2;
                gd.City = account.City;
                gd.Lines = new List<GeneratorDocumentLineItem>();
                var numLines =  random.Next(1, supplier.MaxLines);
                for (int l = 0; l < numLines; l++)
                {
                    var gdli = new GeneratorDocumentLineItem();
                    var product = Products.GetRandomProduct();
                    gdli.Discount = product.Discount;
                    gdli.Isbn = product.Isbn;
                    gdli.ItemNumber = (l+1).ToString();
                    gdli.Price = product.Price;
                    gdli.Title = product.Title;
                    gdli.Quantity = random.Next(1, 100);
                    gdli.Taxable = product.Taxable;
                    gd.Lines.Add(gdli);

                }
                gs.Documents.Add(gd);
                if (save) gd.Save();
                
                

            }
            return gs;
        }

    }
}
