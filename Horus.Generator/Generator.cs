using System;
using System.Collections.Generic;
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

        static int maxDocuments = 30;
        static int maxLines = 8;
        static int baseDocumentNumber = 15000;

       
        public static GeneratorSpecification Generate()
        {

           
            var gs = new GeneratorSpecification();
            var supplier = new Suppliers().GetRandomSupplier();

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
            var numDocuments = random.Next(1, maxDocuments);

            for (int d = 0; d < numDocuments; d++)
            {
                GeneratorDocument gd = new GeneratorDocument();
                gd.DocumentNumber = (baseDocumentNumber + 1 + d).ToString();
                gd.DocumentDate = DateTime.Now.Subtract(new TimeSpan(random.Next(1, 60), 0, 0, 0)).ToString("DD-MM-YYYY");
                if (random.Next(1,10) <= 3) gd.Notes = "Need to do something with this";
                var account = Accounts.GetRandomAccount();
                gd.PostalCode = account.PostalCode;
                gd.SingleName = account.SingleName;
                gd.Account = account.AccountNumber;
                gd.AddressLine1 = account.AddressLine1;
                gd.AddressLine2 = account.AddressLine2;
                gd.City = account.City;
                gd.Lines = new List<GeneratorDocumentLineItem>();
                var numLines =  random.Next(1, maxLines);

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
                    gdli.Taxable = product.Taxable.ToString();
                    gd.Lines.Add(gdli);

                }
                gs.Documents.Add(gd);


            }
            return gs;
        }

    }
}
