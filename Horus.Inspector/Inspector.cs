using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Horus.Functions.Data;
using System.Data.SqlClient;
using Horus.Functions.Models;

namespace Horus.Inspector
{
    public class Inspector
    {

        private readonly ILogger log;
        private readonly List<ScoreRecord> records;
        private static readonly CloudBlobClient trainingBlobClient = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TrainingStorageAccountConnectionString")).CreateCloudBlobClient();
        static readonly CloudBlobClient orchestrationBlobClient = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("OrchestrationStorageAccountConnectionString")).CreateCloudBlobClient();
        public static readonly string scoresSQLConnectionString = Environment.GetEnvironmentVariable("ScoresSQLConnectionString");
        public static readonly string teamName = Environment.GetEnvironmentVariable("TeamName");
        public Inspector(ILogger log)
        {
            this.log = log;
            this.records = new List<ScoreRecord>();
            CheckAndCreateDatabaseIfNecessary(log);
        }

        private void CheckAndCreateDatabaseIfNecessary(ILogger log)
        {
            HorusSql.CheckAndCreateDatabaseIfNecessary(log);
            using (SqlConnection connection = new SqlConnection(scoresSQLConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader;
                command.Connection = connection;
                command.CommandText = "select name from sysobjects where name = 'ScoreSummary'";
                using (reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        log.LogTrace("Table ScoreSummary exists no need to create database tables");
                        return;
                    }
                }

                log.LogInformation($"Creating tables in {connection.Database} database ..");
                SqlTransaction transaction = connection.BeginTransaction("InitializeDatabase");
                command.Transaction = transaction;

                var commandStr = "If not exists (select name from sysobjects where name = 'GeneratedDocuments')" +
                 "CREATE TABLE[dbo].[GeneratedDocuments]([Id][int] IDENTITY(1, 1) NOT NULL, [Account] [nvarchar](50) NULL, [SingleName] [nvarchar](50) NULL, [AddressLine1] [nvarchar](50) NULL, [AddressLine2] [nvarchar](50) NULL, " +
                 "[PostalCode] [nvarchar](50) NULL, [City] [nvarchar](50) NULL, [Notes] [nvarchar](50) NULL, [DocumentNumber] [nvarchar](50) NOT NULL, [FileName] [nvarchar](50) NULL, [DocumentFormat] [nvarchar](50) NULL, " +
                 "[DocumentDate] [datetime2](7) NULL, [PreTaxTotalValue] [decimal](19, 5) NULL, [TaxTotalValue] [decimal](19, 5) NULL, [ShippingTotalValue] [decimal](19, 5) NULL, [GrandTotalValue]  [decimal](19, 5) NULL, [LineNumber] [nvarchar](5) NOT NULL, " +
                 "[Title] [nvarchar](50) NULL, [Author] [nvarchar](50) NULL, [Isbn] [nvarchar](50) NULL, [Quantity] [decimal](19, 5) NULL, [Discount] [decimal](19, 5) NULL, [Price] [decimal](19, 5) NULL, [Taxable] [bit] NOT NULL, " +
                 "[GoodsValue] [decimal](19, 5) NULL, [DiscountValue] [decimal](19, 5) NULL,	[DiscountedGoodsValue] [decimal](19, 5) NULL, [TaxableValue] [decimal](19, 5) NULL " +
                 "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table GeneratedDocuments was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'ScoreSummary')" +
                 "CREATE TABLE[dbo].[ScoreSummary]([Id][int] IDENTITY(1, 1) NOT NULL, " +
                 "[Team] [nvarchar](50) NOT NULL, [TotalScore][int] NOT NULL, [InspectionTime] [datetime2](7) NOT NULL " +
                 "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table ScoreSummary was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'ScoreDetail')" +
                "CREATE TABLE[dbo].[ScoreDetail]([Id][int] IDENTITY(1, 1) NOT NULL, " +
                "[Team][nvarchar](50) NOT NULL, [InspectionTime] [datetime2](7) NOT NULL, [Type] [nvarchar](50) NOT NULL, [Notes] [nvarchar] (max)NULL, [Score] [int] NOT NULL, [Status] [nvarchar](15)  NOT NULL " +
                "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table ScoreDetail was created.");

               transaction.Commit();
            }
            
        }

        public async Task<List<ScoreRecord>> Inspect() 
        {
            records.AddRange(await InspectTrainingStorage());
            records.AddRange(await InspectModelRegistration());
            records.AddRange(await InspectProcessingOrchestrations());
            records.AddRange(await CountProcessedDocuments(log));
            records.AddRange(await CheckIndividualDocuments(log));
            UpdateDatabase(records);
            return records;

        }

        private void UpdateDatabase(List<ScoreRecord> records)
        {
            using (SqlConnection connection = new SqlConnection(scoresSQLConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;
                transaction = connection.BeginTransaction("ScoresTransaction");
                command.Connection = connection;
                command.Transaction = transaction;
                var inspectionTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                try
                {
                    // Set previous score detail records to inactive
                    command.CommandText = $"UPDATE ScoreDetail SET Status = 'PREVIOUS' WHERE Team = '{teamName}' AND Status = 'CURRENT'";
                    command.ExecuteNonQuery();

                    // write new detail records
                    foreach (var record in records)
                    {
                        command.CommandText = $"INSERT INTO ScoreDetail (Team, InspectionTime, Type, Notes, Score, Status) " +
                            $"VALUES('{teamName}','{inspectionTime}','{record.Type}','{record.Notes}','{record.Score}','CURRENT')";
                        command.ExecuteNonQuery();
                    }

                    // delete existing summary
                    command.CommandText = $"DELETE FROM ScoreSummary WHERE Team = '{teamName}'";
                    command.ExecuteNonQuery();

                    // create new summary
                    command.CommandText = $"INSERT INTO ScoreSummary (Team, TotalScore, InspectionTime) VALUES ('{teamName}', '{records.Sum(s => s.Score)}', '{inspectionTime}')";
                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    log.LogError($"Exception prevented writing inspection scores for team {teamName} to database {connection.Database} (transaction was rolled back).  Message is {e.Message}");
                    transaction.Rollback();
                    throw e;
                }
                log.LogInformation($"scores for team {teamName} were written to database {connection.Database}");
            }
        }

        private async Task<List<ScoreRecord>> CheckIndividualDocuments(ILogger log)
        {
            var results = new List<ScoreRecord>();
            var checks = new List<DocumentCheckRequest>();
            using (SqlConnection connection = new SqlConnection(scoresSQLConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.Connection = connection;
                try
                {
                    string previousDocumentFormat = "";
                    string previousDocumentNumber = "";

                    command.CommandText = "SELECT * FROM [dbo].[GeneratedDocuments] order by DocumentFormat, DocumentNumber, LineNumber";
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        DocumentCheckRequest checkRequest = null;
                        while (reader.Read())
                        {
                            bool newDocument = false;
                            string currentDocumentFormat = (string)reader["DocumentFormat"];
                            string currentDocumentNumber = (string)reader["DocumentNumber"];
                            
                            if (currentDocumentFormat != previousDocumentFormat)
                            {
                                newDocument = true;
                                previousDocumentFormat = currentDocumentFormat;
                            }
                            if (currentDocumentNumber != previousDocumentNumber)
                            {
                                newDocument = true;
                                previousDocumentNumber = currentDocumentNumber;
                            }

                            if (newDocument)
                            {
                                if (checkRequest != null) checks.Add(checkRequest);
                                checkRequest = new DocumentCheckRequest
                                {
                                    Account = (string)reader["Account"],
                                    DocumentNumber = (string)reader["DocumentNumber"],
                                    DocumentDate = (DateTime) reader["DocumentDate"],
                                    PostalCode = (string)reader["PostalCode"],
                                    GrandTotalValue = Convert.ToDouble(reader["GrandTotalValue"]),
                                    PreTaxTotalValue = Convert.ToDouble(reader["PreTaxTotalValue"]),
                                    ShippingTotalValue = Convert.ToDouble(reader["ShippingTotalValue"]),
                                    TaxTotalValue = Convert.ToDouble(reader["TaxTotalValue"]),
                                    FileName = (string)reader["FileName"],
                                    DocumentFormat = (string)reader["DocumentFormat"],
                                };
                            }

                            var line = new DocumentLineCheckRequest
                            {
                                Discount = Convert.ToDouble(reader["Discount"]),
                                LineNumber = (string)reader["LineNumber"],
                                ProductCode = (string)reader["Isbn"],
                                ProductDescription = (string)reader["Title"],
                                Price = Convert.ToDouble(reader["Price"]),
                                Quantity = Convert.ToDouble(reader["Quantity"]),
                                Taxable = (bool)reader["Taxable"],
                                DiscountedGoodsValue = Convert.ToDouble(reader["DiscountedGoodsValue"]),
                                DiscountValue = Convert.ToDouble(reader["DiscountValue"]),
                                GoodsValue = Convert.ToDouble(reader["GoodsValue"]),
                                TaxableValue = Convert.ToDouble(reader["TaxableValue"]),
                            };
                            checkRequest.Lines.Add(line);
                        }
                        if (checkRequest != null) checks.Add(checkRequest);

                    }
                    catch (Exception e) 
                    {
                        log.LogError(e.Message);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                catch (Exception e)
                {
                    log.LogError($"Exception prevented reading expected results from SQL database {connection.Database}  Message is {e.Message}");
                    throw e;
                }
                log.LogInformation($"Expected Results read from SQL database {connection.Database}");
            }

            
            foreach (var check in checks)
            {
                string fileName = $"{check.DocumentFormat}-{check.FileName}";
                Document document = HorusSql.LoadDocument(fileName, log);
                if (document == null) 
                {
                    log.LogTrace($"Document {check.DocumentNumber} has not been processed and will be skipped");
                    continue; 
                }
                var checkResults = CompareActualWithExpectedResults(document, check, log);
                results.AddRange(checkResults);
            }
            return results;
        }

        private List<ScoreRecord> CompareActualWithExpectedResults(Document actual, DocumentCheckRequest expected, ILogger log)
        {
            var results = new List<ScoreRecord>();
            if (actual.Account == expected.Account)
                results.Add(new ScoreRecord { Type = $"Processing", Notes = $"Account {actual.Account} was recognized correctly in document {expected.FileName} (5 points awarded)", Score = 5 });
            if (Math.Round((double)actual.GrandTotal, 2) == Math.Round(expected.GrandTotalValue,2))
                results.Add(new ScoreRecord { Type = $"Processing", Notes = $"Grand Total {actual.GrandTotal} was recognized correctly in document {expected.FileName} (15 points awarded)", Score = 15 });
            return results;
        }

        private async Task<List<ScoreRecord>> CountProcessedDocuments(ILogger log)
        {
            var results = new List<ScoreRecord>();
            int numDocs = HorusSql.GetDocumentCount(log);
            results.Add(new ScoreRecord { Type = $"Processing", Notes = $"{numDocs} documents were detected in SQL database (1 points each)", Score = numDocs * 3 });
            return results;
        }

        private async Task<List<ScoreRecord>> InspectProcessingOrchestrations()
        {
            var results = new List<ScoreRecord>();
            var containers = await orchestrationBlobClient.ListContainersAsync();
            int score = containers.Count();
            if (score > 100) score = 100;
            results.Add(new ScoreRecord { Type = $"Processing", Notes = $"{containers.Count()} processing orchestration containers were detected (1 point each, max 100)", Score = score });
            return results;
        }

        private async Task<List<ScoreRecord>> InspectTrainingStorage() 
        {
            var results = new List<ScoreRecord>();

            // Check containers have been created for each document type where a model needs to be trained
            var containers = await trainingBlobClient.ListContainersAsync();
            var documentTypesForChallenge = Environment.GetEnvironmentVariable("DocumentTypesForChallenge").Split(',').ToList();
            foreach (var item in documentTypesForChallenge)
            {
                if (containers.Where(c=>c.Name == item).Count() == 1)
                {
                    results.Add(new ScoreRecord { Type = $"Training", Notes=$"Container for document type {item} was detected", Score = 10 / documentTypesForChallenge.Count }); 
                }

            }

            // Check that training documents have been uploaded
            foreach (var item in containers)
            {
                int i = 0; int j = 0;
                var allBlobs = await item.ListBlobsAsync();
                foreach (IListBlobItem blob in allBlobs)
                {
                    string name = blob.Uri.Segments.Last();
                    if (name.ToLower().EndsWith(".pdf"))
                    {
                        if (i < 10)
                        {
                            i++;
                        }
                    }

                    if (name.ToLower().EndsWith(".pdf.labels.json"))
                    {
                        if (j < 10)
                        {
                            j++;
                        }
                    }

                    if (name.ToLower().EndsWith(".fott"))
                    {
                        results.Add(new ScoreRecord { Type = $"Training", Notes = $"Recognizer labelling project has been created {name}", Score = 50 });
                    }
                }
                if (i>0) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{i} raw documents for document type {item.Name} are present (2 points each)", Score = 2 * i});
                if (j> 0) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{j} labelled documents for document type {item.Name} are present (10 points each)", Score = 10 * j });

            }
            
            return results;

        }

        private async Task<List<ScoreRecord>> InspectModelRegistration()
        {
            var results = new List<ScoreRecord>();
            var documentTypesForChallenge = Environment.GetEnvironmentVariable("DocumentTypesForChallenge").Split(',').ToList();
            foreach (var documentType in documentTypesForChallenge)
            {
                var mtr = HorusSql.GetModelIdByDocumentFormat(documentType);
                if (mtr.DocumentFormat != null) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{mtr.ModelId} has been registered for document type {documentType}", Score = 100});
            }
            return results;
        }
    }

    static class Extensions
    {
        public static async Task<List<CloudBlobContainer>> ListContainersAsync(this CloudBlobClient client)
        {
            BlobContinuationToken continuationToken = null;
            List<CloudBlobContainer> results = new List<CloudBlobContainer>();
            do
            {
                var response = await client.ListContainersSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }

        public static async Task<List<IListBlobItem>> ListBlobsAsync(this CloudBlobContainer client)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            OperationContext context = new OperationContext();
            BlobRequestOptions options = new BlobRequestOptions();
            do
            {
                var response = await client.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, continuationToken, options, context);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }
    }

    public class ScoreRecord
    {
        public string Type { get; set; }
        public int Score { get; set; }
        public string Notes { get; set; }
    }

    public class DocumentCheckRequest
    {

        public DocumentCheckRequest() { Lines = new List<DocumentLineCheckRequest>(); }

        public List<DocumentLineCheckRequest> Lines { get; set; }
        public string Account { get; set; }
        public string PostalCode { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public double PreTaxTotalValue { get; set; }
        public double TaxTotalValue { get; set; }
        public double ShippingTotalValue { get; set; }
        public double GrandTotalValue { get; set; }
        public string FileName { get; set; }
        public string DocumentFormat { get; set; }
    }

    public class DocumentLineCheckRequest
    {
        public string LineNumber { get; set; }
        public string ProductDescription { get; set; }
        public string ProductCode { get; set; }
        public double Quantity { get; set; }
        public double Discount { get; set; }
        public double Price { get; set; }

        public bool Taxable { get; set; }

        public double GoodsValue { get; set; }
        public double DiscountValue { get; set; }
        public double DiscountedGoodsValue { get; set; }
        public double TaxableValue { get; set; }
    }

}

