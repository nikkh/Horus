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
using System.Diagnostics;

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
            log.LogTrace($"Checking if processing database has been initialised");
            HorusSql.CheckAndCreateDatabaseIfNecessary(log);
            
            using (SqlConnection connection = new SqlConnection(scoresSQLConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader;
                command.Connection = connection;
                log.LogTrace($"Checking if scores database has been initialised");
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
            log.LogInformation($"Inspection started");
            records.AddRange(await InspectTrainingStorage());
            records.AddRange(await InspectModelRegistration());
            records.AddRange(await InspectProcessingOrchestrations());
            records.AddRange(await CountProcessedDocuments(log));
            records.AddRange(await CheckIndividualDocuments(log));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UpdateDatabase(records);
            log.LogInformation($"Database update for {records.Count} records took {stopwatch.ElapsedMilliseconds} ms");
            return records;
        }

        private void UpdateDatabase(List<ScoreRecord> records)
        {
            using (SqlConnection connection = new SqlConnection(scoresSQLConnectionString))
            {
                
                connection.Open();
                log.LogTrace($"Updating database {connection.Database} with scores");
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
            log.LogTrace($"Checking accuracy of document recognition");
            log.LogTrace($"Reading expected results from SQL Database");
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

            log.LogInformation($"We will be checking the actual vs expected results for {checks.Count} documents");
            foreach (var check in checks)
            {
                string fileName = $"{check.DocumentFormat}-{check.FileName}";
                log.LogDebug($"Loading document {fileName} from processing database");
                Document document = null;
                try
                {
                    log.LogTrace($"Checking {fileName}");
                    document = HorusSql.LoadDocument(fileName, log);
                }
                catch (Exception)
                {
                    log.LogWarning($"Unable to load document {fileName} from processing database.");
                    continue;
                }
                if (document == null) 
                {
                    log.LogTrace($"Document {check.DocumentNumber} has not been processed sucessfully and will be skipped");
                    continue; 
                }
                var checkResults = CompareActualWithExpectedResults(document, check, log);
                results.AddRange(checkResults);
            }
            return results;
        }

           
        private bool Compare<T>(string fileName, string field, T actual, T expected, ILogger log, bool round = false) 
        {
            Type theType = typeof(T);
            log.LogTrace($"{fileName}-{field} ({theType.FullName}): Expected={expected}, Actual={actual}");
            bool equal = false;
            switch (theType.FullName)
            {
                case "System.String":
                    if ((string)(object)actual == (string)(object)expected) equal = true;
                     break;
                case "System.DateTime":
                    var actualDateTime = (DateTime)(object)actual;
                    var expectedDateTime = (DateTime)(object)expected;
                    if (actualDateTime.Date == expectedDateTime.Date) equal = true;
                     break;
                case "System.Double":
                    var actualDouble = Math.Round((double)(object)actual, 2);
                    var expectedDouble = Math.Round((double)(object)expected, 2);
                    if (actualDouble == expectedDouble) equal = true;
                    break;
                case "System.Decimal":
                    var actualDecimal = Math.Round((decimal)(object)actual, 2);
                    var expectedDecimal = Math.Round((decimal)(object)expected, 2);
                    if (actualDecimal == expectedDecimal) equal = true;
                    break;
                case "System.Int32":
                    if ((Int32)(object)actual == (Int32)(object)expected) equal = true;
                    break;
                case "System.Int64":
                    if ((Int64)(object)actual == (Int64)(object)expected) equal = true;
                    break;
                case "System.Boolean":
                    if ((bool)(object)actual == (bool)(object)expected) equal = true;
                    break;
                default:
                    throw new Exception($"Attempt to compare unsupported type {theType.FullName}");
            }

            if (equal) 
            { 
                log.LogTrace($"{fileName}-{field}: matches");
                return true;
            }
            else 
            { 
                log.LogWarning($"{fileName}-{field} does not match.  You may wish to investigate?");
                return false;
            } 

        }

        private List<ScoreRecord> CompareActualWithExpectedResults(Document actual, DocumentCheckRequest expected, ILogger log)
        {
            int documentPoints = 0;

            var results = new List<ScoreRecord>();

            log.LogTrace($"Checking accuracy of document {actual.FileName} header");

            // There are 7 header fields to check.  We will identify those that match.
            Dictionary<string, bool> matches = new Dictionary<string, bool> { { "Account", false },{ "GrandTotal", false }, { "ShippingTotal", false }, { "NetTotal", false }, { "VatAmount", false }, { "PostCode", false }, { "TaxDate", false } };

            matches["Account"] = Compare<string>(actual.FileName, "Account", actual.Account, expected.Account, log);
            matches["GrandTotal"] = Compare<decimal>(actual.FileName, "GrandTotal", actual.GrandTotal, (decimal) expected.GrandTotalValue, log);
            matches["ShippingTotal"] = Compare<decimal>(actual.FileName, "ShippingTotal", actual.ShippingTotal, (decimal)expected.ShippingTotalValue, log);
            matches["NetTotal"] = Compare<decimal>(actual.FileName, "NetTotal", actual.NetTotal, (decimal)expected.PreTaxTotalValue, log);
            matches["VatAmount"] = Compare<decimal>(actual.FileName, "VatAmount", actual.VatAmount, (decimal)expected.TaxTotalValue, log);
            matches["PostCode"] = Compare<string>(actual.FileName, "PostCode", actual.PostCode, expected.PostalCode, log);
            matches["TaxDate"] = Compare<DateTime>(actual.FileName, "TaxDate", actual.TaxDate ?? DateTime.MinValue, expected.DocumentDate, log);

            // A fully matched header is worth 20 points - so apportion that
            int DOCUMENT_HEADER_POINTS = 20;
            decimal numPossibleMatches = matches.Count();
            decimal numMatches = matches.Where(m => m.Value == true).Count();
            var successRate = numMatches / numPossibleMatches;
            var points = (int) (DOCUMENT_HEADER_POINTS * successRate);
            documentPoints = points;
            string notes = $"Document {actual.FileName} Header matched {numMatches} of {numPossibleMatches} fields for a score of {points} / {DOCUMENT_HEADER_POINTS})";
            log.LogInformation(notes);
           
            log.LogTrace($"Checking accuracy of document {actual.DocumentNumber} lines");
            matches = new Dictionary<string, bool> { { "ItemDescription", false }, { "UnitPrice", false }, { "Taxableindicator", false }, { "LineQuantity", false }, { "NetAmount", false }, { "DiscountPercent", false }};

            // 100 points for a fully matched document leaves 80 up for grabs: pro rata that over the expected lines
            int DOCUMENT_LINES_POINTS = 80;
            decimal DOCUMENT_LINE_POINTS = DOCUMENT_LINES_POINTS / expected.Lines.Count();
            numPossibleMatches = matches.Count();
            
            foreach (var expLine in expected.Lines.OrderBy(o=>o.LineNumber))
            {
                var expLineNumber = expLine.LineNumber.PadLeft(2, '0');
                log.LogTrace($"Checking Line {expLineNumber}");
                DocumentLineItem actLine = null;
                try
                {
                    actLine = actual.LineItems.Where(l => l.DocumentLineNumber == expLineNumber).Single();
                }
                catch (Exception)
                {
                    log.LogTrace($"{actual.FileName} Actual line matching {expLineNumber} does not exist - you may want to retrain your model?");
                }
                if (actLine != null)
                {
                    matches["ItemDescription"] = Compare<string>(actual.FileName, "ItemDescription"+ expLineNumber, actLine.ItemDescription, $"{expLine.ProductCode}{expLine.ProductDescription}".Trim(), log);
                    matches["UnitPrice"] = Compare<decimal>(actual.FileName, "UnitPrice" + expLineNumber, actLine.UnitPrice, (decimal) expLine.Price, log);
                    
                    bool actTaxIndicator = false;
                    if (!String.IsNullOrEmpty(actLine.Taxableindicator)) actTaxIndicator = true;
                    matches["Taxableindicator"] = Compare<bool>(actual.FileName, "Taxableindicator" + expLineNumber, actTaxIndicator, expLine.Taxable, log);
                  
                    double actLineQuantity = 0;
                    if (Double.TryParse(actLine.LineQuantity, out double res)) actLineQuantity = res;
                    matches["LineQuantity"] = Compare<double>(actual.FileName, "LineQuantity" + expLineNumber, actLineQuantity, expLine.Quantity, log);
                    if (!matches["LineQuantity"])
                    {
                        matches["LineQuantity"] = Compare<double>(actual.FileName, "CalculatedLineQuantity" + expLineNumber, (double) actLine.CalculatedLineQuantity, expLine.Quantity, log);
                    }
                    matches["NetAmount"] = Compare<decimal>(actual.FileName, "NetAmount" + expLineNumber, actLine.NetAmount, (decimal) expLine.DiscountedGoodsValue, log);
                    matches["DiscountPercent"] = Compare<decimal>(actual.FileName, "DiscountPercent" + expLineNumber, actLine.DiscountPercent, (decimal)expLine.Discount, log);

                    numMatches = matches.Where(m => m.Value == true).Count();
                    successRate = (numMatches / numPossibleMatches);
                    points = (int) (DOCUMENT_LINE_POINTS * successRate);
                    documentPoints += points;
                    notes = $"Document {actual.FileName} line {expLineNumber} matched {numMatches} of {numPossibleMatches} fields for a score of {points} / {DOCUMENT_LINE_POINTS})";
                    log.LogInformation(notes);
                }
                else
                {
                    log.LogTrace($"{actual.FileName} Actual line matching {expLineNumber} does not exist - you may want to retrain your model?");
                }
            }

            notes = $"Document {actual.FileName} overall scored {documentPoints} / 100 points)";
            if (documentPoints < 50)
            {
                log.LogError(notes);
            }
            else { log.LogInformation(notes); }
            results.Add(new ScoreRecord { Type = $"Accuracy", Notes = notes, Score = documentPoints });
            return results;
        }

        private async Task<List<ScoreRecord>> CountProcessedDocuments(ILogger log)
        {
            var results = new List<ScoreRecord>();
            log.LogTrace($"Checking for documents in SQL database");
            int numDocs = HorusSql.GetDocumentCount(log);
            log.LogTrace($"{numDocs} documents have been analysed and saved to SQL");
            results.Add(new ScoreRecord { Type = $"Processing", Notes = $"{numDocs} documents were detected in SQL database (3 points each)", Score = numDocs * 3 });
            return results;
        }

        private async Task<List<ScoreRecord>> InspectProcessingOrchestrations()
        {
            var results = new List<ScoreRecord>();
            log.LogTrace($"Checking processing orchestrations");
            var containers = await orchestrationBlobClient.ListContainersAsync();
            int score = containers.Count();
            log.LogTrace($"{score} orchestration containers were present");
            if (score > 500) score = 500;
            results.Add(new ScoreRecord { Type = $"Processing", Notes = $"{containers.Count()} processing orchestration containers were detected (1 point each, max 500)", Score = score });
            return results;
        }

        private async Task<List<ScoreRecord>> InspectTrainingStorage() 
        {
            var results = new List<ScoreRecord>();

            // Check containers have been created for each document type where a model needs to be trained
            log.LogTrace($"Checking that processing containers have been created in training account");
            var containers = await trainingBlobClient.ListContainersAsync();
            var documentTypesForChallenge = Environment.GetEnvironmentVariable("DocumentTypesForChallenge").Split(',').ToList();
            foreach (var item in documentTypesForChallenge)
            {
                log.LogTrace($"Checking container {item}");
                if (containers.Where(c=>c.Name == item).Count() == 1)
                {
                    log.LogTrace($"Container {item} has been created");
                    results.Add(new ScoreRecord { Type = $"Training", Notes=$"Container for document type {item} was detected", Score = 150 / documentTypesForChallenge.Count }); 
                }

            }

            // Check that training documents have been uploaded
            log.LogTrace($"Checking that training documents have been uploaded to  containers");
            foreach (var item in containers)
            {
                log.LogTrace($"Container {item.Name}");
                int i = 0; int j = 0;
                var allBlobs = await item.ListBlobsAsync();
                foreach (IListBlobItem blob in allBlobs)
                {
                    string name = blob.Uri.Segments.Last();
                    if (name.ToLower().EndsWith(".pdf"))
                    {
                        log.LogTrace($"Document {name} detected");
                        if (i < 10)
                        {
                            i++;
                        }
                    }

                    if (name.ToLower().EndsWith(".pdf.labels.json"))
                    {
                        log.LogTrace($"Document {name} detected");
                        if (j < 10)
                        {
                            j++;
                        }
                    }

                    if (name.ToLower().EndsWith(".fott"))
                    {
                        log.LogTrace($"Document {name} detected");
                        results.Add(new ScoreRecord { Type = $"Training", Notes = $"Recognizer labelling project has been created {name}", Score = 500 });
                    }
                }
                if (i>0) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{i} raw documents for document type {item.Name} are present (10 points each)", Score = 10 * i});
                if (j> 0) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{j} labelled documents for document type {item.Name} are present (25 points each)", Score = 25 * j });

            }
            
            return results;

        }

        private async Task<List<ScoreRecord>> InspectModelRegistration()
        {
            var results = new List<ScoreRecord>();
            log.LogTrace($"Checking that a Model has been registered for each document type");
            var documentTypesForChallenge = Environment.GetEnvironmentVariable("DocumentTypesForChallenge").Split(',').ToList();
            foreach (var documentType in documentTypesForChallenge)
            {
                log.LogTrace($"Checking {documentType}");
                var mtr = HorusSql.GetModelIdByDocumentFormat(documentType);
                if (mtr.DocumentFormat != null)
                {
                    log.LogTrace($"Model {mtr.ModelId} has been registered for {documentType}");
                    results.Add(new ScoreRecord { Type = $"Training", Notes = $"{mtr.ModelId} has been registered for document type {documentType}", Score = 500 });
                }
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

