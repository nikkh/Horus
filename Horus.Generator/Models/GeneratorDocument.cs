using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.Models
{
    public class GeneratorDocument
    {
        public static readonly string sqlConnectionString = ConfigurationManager.AppSettings["ResultsSqlConnectionString"];   

        public List<GeneratorDocumentLineItem> Lines { get; set; }
        public string Account { get; set; }
        public string SingleName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Notes { get; set; }
        public string DocumentNumber { get; set; }
       


        public DateTime? DocumentDate { get; set; }

        public double PreTaxTotalValue
        {
            get
            {
                return Lines.Sum(l => l.DiscountedGoodsValue);
            }
        }
        public double TaxTotalValue
        {
            get
            {
                return Lines.Where(l => l.Taxable).Sum(l => l.DiscountedGoodsValue) * .19;
            }
        }
        public double ShippingTotalValue
        {
            get
            {
                return PreTaxTotalValue * .15;
            }
        }
        public double GrandTotalValue 
        {
            get 
            {
                return PreTaxTotalValue + TaxTotalValue + ShippingTotalValue;
            } 
        }

        public string DocumentFormat { get; set; }
        public string FileName { get; set; }
        public void Save()
        {
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;
                transaction = connection.BeginTransaction("GeneratorResultsTransaction");
                command.Connection = connection;
                command.Transaction = transaction;

                try
                {
                   
                    foreach (var line in Lines)
                    {

                        string insertClause = $"Insert into [GeneratedDocuments] (Account, SingleName, AddressLine1, AddressLine2, PostalCode, City, Notes, DocumentNumber, DocumentFormat, FileName, PreTaxTotalValue, TaxTotalValue, ShippingTotalValue, GrandTotalValue, LineNumber, Title, Author, Isbn, Quantity, Discount, Price, Taxable, GoodsValue, DiscountValue, DiscountedGoodsValue, TaxableValue";
                        string valuesClause = $"VALUES ('{Account}', '{SingleName}', '{AddressLine1}','{AddressLine2}', '{PostalCode}', '{City}', '{Notes}', '{DocumentNumber}',  '{DocumentFormat}', '{FileName}','{PreTaxTotalValue}','{TaxTotalValue}', '{ShippingTotalValue}', '{GrandTotalValue}','{line.ItemNumber}', '{line.Title}','{line.Author}', '{line.Isbn}', '{line.Quantity}','{line.Discount}', '{line.Price}','{line.Taxable}', '{line.GoodsValue}', '{line.DiscountValue}','{line.DiscountedGoodsValue}', '{line.TaxableValue}'";

                        if (DocumentDate != null)
                        {
                            DateTime date = (DateTime) DocumentDate;
                            insertClause += ", DocumentDate";
                            valuesClause += $", '{date.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
                        }
                        
                        insertClause += ") ";
                        valuesClause += ")";
                        command.CommandText = insertClause + valuesClause;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception prevented writing Document {DocumentNumber} to database (transaction was rolled back).  Message is {e.Message}");
                    transaction.Rollback();
                    throw e;
                }
                Console.WriteLine($"Document {DocumentNumber} was written to SQL database {connection.Database}");
            }
        }
    }
}
