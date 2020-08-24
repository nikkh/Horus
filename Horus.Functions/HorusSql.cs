using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Horus.Functions.Data
{
    public class HorusSql
    {
        public static readonly string sqlConnectionString = Environment.GetEnvironmentVariable("SQLConnectionString");
        public static void CheckAndCreateDatabaseIfNecessary(ILogger log)
        {

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader;
                command.Connection = connection;
                command.CommandText = "select name from sysobjects where name = 'ModelTraining'";
                using (reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        log.LogTrace("Table ModelTraining exists no need to create database tables");
                        return;
                    }
                }

                log.LogInformation($"Database tables will be created...");
                SqlTransaction transaction = connection.BeginTransaction("InitializeDatabase");
                command.Transaction = transaction;

                var commandStr = "If not exists (select name from sysobjects where name = 'ModelTraining')" +
                 "CREATE TABLE [dbo].[ModelTraining]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentFormat] [nvarchar](15) NOT NULL, [ModelVersion] [int] NOT NULL, [ModelId] [nvarchar](50) NOT NULL, [CreatedDateTime] [datetime2](7) NOT NULL," +
                 "[UpdatedDateTime] [datetime2](7) NOT NULL, [BlobSasUrl] [nvarchar](max)NOT NULL, [BlobFolderName] [nvarchar](50) NULL, [IncludeSubfolders] [bit] NOT NULL, [UseLabelFile] [bit] NOT NULL, [AverageModelAccuracy] [decimal](19, 5) NOT NULL," +
                 "[TrainingDocumentResults] [nvarchar](max) NOT NULL " +
                 "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table ModelTraining was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'Document')" +
                    "CREATE TABLE[dbo].[Document]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentNumber] [nvarchar](50) NOT NULL, [TaxDate] [datetime2](7) NULL, [OrderNumber] [nvarchar](50) NULL," +
                        "[OrderDate] [datetime2](7) NULL, [FileName] [nvarchar](50) NULL, [ShreddingUtcDateTime] [datetime2](7) NOT NULL, [TimeToShred] [bigint] NOT NULL, [RecognizerStatus] [nvarchar](50) NULL," +
                        "[RecognizerErrors] [nvarchar](50) NULL, [UniqueRunIdentifier] [nvarchar](50) NOT NULL, [TerminalErrorCount] [int] NOT NULL, [WarningErrorCount] [int] NOT NULL, [IsValid] [bit] NOT NULL," +
                        "[Account] [nvarchar](50) NULL,	[VatAmount] [decimal](19, 5) NULL,	[ShippingTotal] [decimal](19, 5) NULL, [NetTotal] [decimal](19, 5) NULL, [GrandTotal] [decimal](19, 5) NULL, [PostCode] [nvarchar](10) NULL, [Thumbprint] [nvarchar](50) NULL, " +
                        "[TaxPeriod] [nvarchar](6) NULL, [ModelId] [nvarchar](50) NULL, [ModelVersion] [nvarchar](50) NULL, [LatestVersionIndicator] [bit] NULL, [DocumentVersion] [int] NULL " +
                        "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table Document was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'DocumentError')" +
                    "CREATE TABLE [dbo].[DocumentError]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentId] [int] NOT NULL, [ErrorCode] [nvarchar](10) NULL, [ErrorSeverity] [nvarchar](10) NULL, [ErrorMessage] [nvarchar](max)NULL " +
                        "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table DocumentErrors was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'DocumentLineItem')" +
                  "CREATE TABLE[dbo].[DocumentLineItem]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentId] [int] NOT NULL, [DocumentLineNumber] [nvarchar](5) NOT NULL, [ItemDescription] [nvarchar](max)NULL, [LineQuantity] [nvarchar](50) NULL," +
                    "[UnitPrice] [decimal](19, 5) NULL, [VATCode] [nvarchar](50) NULL, [NetAmount] [decimal](19, 5) NULL, [CalculatedLineQuantity] [decimal](18, 0) NULL, [TaxableIndicator] [nvarchar](1) NULL, [DiscountPercent] [decimal](9,5) NULL " +
                    "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table DocumentLineItem was created.");

                commandStr = "IF OBJECT_ID('[dbo].[GetModelByDocumentFormat]', 'P') IS NOT NULL DROP PROCEDURE[dbo].[GetModelByDocumentFormat]; ";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();

                commandStr = "CREATE PROCEDURE [dbo].[GetModelByDocumentFormat]  @DocumentFormat VARCHAR(15) AS SET NOCOUNT ON; SELECT DocumentFormat, ModelId, ModelVersion, UpdatedDateTime, AverageModelAccuracy" +
                 " FROM ModelTraining WHERE ModelVersion = (SELECT max(ModelVersion) FROM ModelTraining WHERE DocumentFormat = @DocumentFormat)";

                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Stored Procedure GetModelByDocumentFormat was created.");

                transaction.Commit();
            }
        }

        public static int GetDocumentCount(ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                int rowCount = 0;
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.Connection = connection;
                try
                {
                    command.CommandText = $"SELECT count(*) FROM document";
                    rowCount = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.LogError($"Exception while counting number of processed documents.  Message is {e.Message}");
                    throw e;
                }
                return rowCount;
            }
        }

        public static ModelTrainingRecord GetModelIdByDocumentFormat(string documentFormat)
        {
            var mt = new ModelTrainingRecord();
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetModelByDocumentFormat";
                command.Parameters.Add(new SqlParameter("@DocumentFormat", documentFormat));
                command.Connection = connection;
                try
                {
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {
                            mt.DocumentFormat = Convert.ToString(reader["DocumentFormat"]);
                            mt.ModelId = Convert.ToString(reader["ModelId"]);
                            mt.ModelVersion = Convert.ToInt32(reader["ModelVersion"]);
                            mt.UpdatedDateTime = Convert.ToDateTime(reader["UpdatedDateTime"]);
                            mt.AverageModelAccuracy = Convert.ToDecimal(reader["AverageModelAccuracy"]);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                finally { }
                return mt;
            }
        }

        public static ModelTrainingRecord UpdateModelTraining(ModelTrainingJob m, ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;
                transaction = connection.BeginTransaction("TrainingRequestTransaction");
                command.Connection = connection;
                command.Transaction = transaction;
                int currentVersion = 0;
                int newVersion = 0;
                try
                {

                    command.CommandText = $"SELECT MAX(ModelVersion) AS Current_Version from ModelTraining WHERE DocumentFormat='{m.DocumentFormat}'";
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader["Current_Version"] != System.DBNull.Value)
                            {
                                currentVersion = Convert.ToInt32(reader["Current_Version"]);
                            }
                            else
                            {
                                currentVersion = 0;
                            }
                        }

                    }
                    finally
                    {
                        reader.Close();
                    }

                    newVersion = currentVersion + 1;

                    // Add the row 
                    string insertClause = $"Insert into ModelTraining (DocumentFormat, ModelVersion, ModelId, CreatedDateTime, UpdatedDateTime, BlobSasUrl, BlobfolderName, IncludeSubFolders, UseLabelFile, AverageModelAccuracy, TrainingDocumentResults";
                    string valuesClause = $" VALUES ('{m.DocumentFormat}', '{newVersion}','{m.ModelId}', '{m.CreatedDateTime:yyyy-MM-dd HH:mm:ss.fff}', '{m.UpdatedDateTime:yyyy-MM-dd HH:mm:ss.fff}', '{m.BlobSasUrl}', '{m.BlobFolderName}','{m.IncludeSubFolders}', '{m.UseLabelFile}','{m.AverageModelAccuracy}','{m.TrainingDocumentResults}'";
                    insertClause += ") ";
                    valuesClause += ")";
                    command.CommandText = insertClause + valuesClause;


                    command.ExecuteNonQuery();


                    transaction.Commit();
                }
                catch (Exception e)
                {
                    log.LogError($"Exception prevented writing training request for document format {m.DocumentFormat} model id={m.ModelId} to database (transaction was rolled back).  Message is {e.Message}");
                    transaction.Rollback();
                    throw e;
                }

                ModelTrainingRecord mtr = new ModelTrainingRecord { ModelId = m.ModelId, ModelVersion = newVersion, AverageModelAccuracy = m.AverageModelAccuracy, DocumentFormat = m.DocumentFormat, UpdatedDateTime = m.UpdatedDateTime };
                log.LogInformation($"Training request for document format {m.DocumentFormat}, version={newVersion}, model id={m.ModelId}  was written to the database");
                return mtr;
            }
        }

        public static void SaveDocument(Document document, ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;
                transaction = connection.BeginTransaction("DocumentTransaction");
                command.Connection = connection;
                command.Transaction = transaction;
                // remove flag for current latest version
                command.CommandText = $"UPDATE Document set LatestVersionIndicator = 'FALSE' where fileName='{document.FileName}' and LatestVersionIndicator='{true}'";
                command.ExecuteNonQuery();

                // get next version number
                command.CommandText = $"SELECT MAX(DocumentVersion) AS CurrentDocVersion from Document WHERE fileName='{document.FileName}'";
                int currentDocVersion = 0;

                var objResult = command.ExecuteScalar();
                if (objResult != DBNull.Value && objResult != null)
                {
                    currentDocVersion = (int)objResult;
                }
                int newDocVersion = currentDocVersion + 1;

                string safePostCode = document.PostCode;
                try
                {
                    if (document.PostCode.Length > 10)
                    {
                        safePostCode = document.PostCode.Substring(0, 10);
                    }
                    // Add the document 
                    string insertClause = $"Insert into Document (DocumentNumber, OrderNumber, FileName, ShreddingUtcDateTime, TimeToShred, RecognizerStatus, RecognizerErrors, UniqueRunIdentifier, TerminalErrorCount, WarningErrorCount, IsValid, Account, VatAmount, ShippingTotal, NetTotal, GrandTotal, PostCode, Thumbprint, TaxPeriod, ModelId, ModelVersion, LatestVersionIndicator, DocumentVersion";
                    string valuesClause = $" VALUES ('{document.DocumentNumber}', '{document.OrderNumber}','{document.FileName}', '{document.ShreddingUtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}', '{document.TimeToShred}', '{document.RecognizerStatus}', '{document.RecognizerErrors}','{document.UniqueRunIdentifier}', '{document.TerminalErrorCount}','{document.WarningErrorCount}', '{document.IsValid}', '{document.Account}', '{document.VatAmount}', '{document.ShippingTotal}', '{document.NetTotal}', '{document.GrandTotal}', '{safePostCode}', '{document.Thumbprint}','{document.TaxPeriod}','{document.ModelId}','{document.ModelVersion}','{true}','{newDocVersion}'";
                    if (document.TaxDate != null)
                    {
                        DateTime taxDate = (DateTime)document.TaxDate;
                        insertClause += ", TaxDate";
                        valuesClause += $", '{taxDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
                    }
                    if (document.OrderDate != null)
                    {
                        DateTime orderDate = (DateTime)document.OrderDate;
                        insertClause += ", OrderDate";
                        valuesClause += $", '{orderDate.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
                    }
                    insertClause += ") ";
                    valuesClause += ")";
                    command.CommandText = insertClause + valuesClause;


                    command.ExecuteNonQuery();
                    int currentIdentity = 0;
                    command.CommandText = "SELECT IDENT_CURRENT('[dbo].[Document]') AS Current_Identity";
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {
                            currentIdentity = Convert.ToInt32(reader["Current_Identity"]);
                        }

                    }
                    finally
                    {
                        reader.Close();
                    }

                    // Add the lines
                    foreach (var line in document.LineItems)
                    {
                        string safeDescription = null;
                        // ensure no single quotes in drug description
                        if (line.ItemDescription != null)
                        { safeDescription = line.ItemDescription.Replace("'", BaseConstants.IllegalCharacterMarker); }
                        command.CommandText =
                        $"Insert into DocumentLineItem (DocumentId, ItemDescription, LineQuantity, UnitPrice, VATCode, NetAmount, CalculatedLineQuantity, DocumentLineNumber, TaxableIndicator, DiscountPercent) " +
                        $"VALUES ('{currentIdentity}', '{safeDescription}', '{line.LineQuantity}','{line.UnitPrice}', '{line.VATCode}','{line.NetAmount}','{line.CalculatedLineQuantity}', '{line.DocumentLineNumber}', '{line.Taxableindicator}', '{line.DiscountPercent}')";
                        command.ExecuteNonQuery();
                    }

                    // Add the Errors
                    foreach (var error in document.Errors)
                    {
                        command.CommandText =
                        $"Insert into DocumentError (DocumentId, ErrorCode, ErrorSeverity, ErrorMessage) " +
                        $"VALUES ('{currentIdentity}', '{error.ErrorCode}', '{error.ErrorSeverity}','{error.ErrorMessage}' )";
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    log.LogError($"Exception prevented writing Document {document.DocumentNumber} to database (transaction was rolled back).  Message is {e.Message}");
                    transaction.Rollback();
                    throw e;
                }
                log.LogInformation($"Document {document.DocumentNumber} was written to SQL database {connection.Database}");
            }
        }
        public static Document LoadDocument(string fileName, ILogger log)
        {
            Document document = null;
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.Connection = connection;
                try
                {
                    int documentId = 0;
                    command.CommandText = $"select * from Document where FileName = '{fileName}' order by ShreddingUtcDateTime desc";
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {

                            documentId = (int)reader["Id"];
                            document = new Document
                            {
                                Account = reader.GetValue<string>("Account"),
                                DocumentNumber = reader.GetValue<string>("DocumentNumber"),
                                TaxDate = reader.GetValue<DateTime>("TaxDate"),
                                FileName = reader.GetValue<string>("FileName"),
                                GrandTotal = reader.GetValue<decimal>("GrandTotal"),
                                NetTotal = reader.GetValue<decimal>("NetTotal"),
                                PostCode = reader.GetValue<string>("PostCode"),
                                ShippingTotal = reader.GetValue<decimal>("ShippingTotal"),
                                VatAmount = reader.GetValue<decimal>("VatAmount"),
                                TaxPeriod = reader.GetValue<string>("TaxPeriod"),
                                OrderDate = reader.GetValue<DateTime>("OrderDate"),
                                OrderNumber = reader.GetValue<string>("OrderNumber")
                            };
                            break;

                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Exception prevented reading Document {document.DocumentNumber} from SQL database {connection.Database}.  Message is {e.Message}");
                        throw e;
                    }
                    finally
                    {
                        reader.Close();
                    }

                    if (document == null)
                    {
                        log.LogInformation($"Requested document {fileName} not found in database");
                        return null;
                    }

                    command.CommandText = $"select * from DocumentLineItem where DocumentId = '{documentId}' order by DocumentLineNumber";
                    reader = command.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {

                            var lineItem = new DocumentLineItem
                            {
                                DiscountPercent = reader.GetValue<decimal>("DiscountPercent"),
                                DocumentLineNumber = reader.GetValue<string>("DocumentLineNumber"),
                                ItemDescription = reader.GetValue<string>("ItemDescription"),
                                LineQuantity = reader.GetValue<string>("LineQuantity"),
                                VATCode = reader.GetValue<string>("VATCode"),
                                NetAmount = reader.GetValue<decimal>("NetAmount"),
                                Taxableindicator = reader.GetValue<string>("Taxableindicator"),
                                UnitPrice = reader.GetValue<decimal>("UnitPrice")
                            };
                            document.LineItems.Add(lineItem);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                catch (Exception e)
                {
                    log.LogError($"Exception prevented reading Document Lines for document {document.DocumentNumber} from SQL database {connection.Database}.  Message is {e.Message}");
                    throw e;
                }
                log.LogInformation($"Document {document.DocumentNumber} was read from SQL database {connection.Database}");
                return document;
            }
        }
    }

        /// <summary>
        /// Helper class for SqlDataReader, which allows for the calling code to retrieve a value in a generic fashion.
        /// </summary>
        public static class SqlReaderHelper
        {
            private static bool IsNullableType(Type theValueType)
            {
                return (theValueType.IsGenericType && theValueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
            }

            /// <summary>
            /// Returns the value, of type T, from the SqlDataReader, accounting for both generic and non-generic types.
            /// </summary>
            /// <typeparam name="T">T, type applied</typeparam>
            /// <param name="theReader">The SqlDataReader object that queried the database</param>
            /// <param name="theColumnName">The column of data to retrieve a value from</param>
            /// <returns>T, type applied; default value of type if database value is null</returns>
            public static T GetValue<T>(this SqlDataReader theReader, string theColumnName)
            {
                // Read the value out of the reader by string (column name); returns object
                object theValue = theReader[theColumnName];

                // Cast to the generic type applied to this method (i.e. int?)
                Type theValueType = typeof(T);

                // Check for null value from the database
                if (DBNull.Value != theValue)
                {
                    // We have a null, do we have a nullable type for T?
                    if (!IsNullableType(theValueType))
                    {
                        // No, this is not a nullable type so just change the value's type from object to T
                        return (T)Convert.ChangeType(theValue, theValueType);
                    }
                    else
                    {
                        // Yes, this is a nullable type so change the value's type from object to the underlying type of T
                        NullableConverter theNullableConverter = new NullableConverter(theValueType);

                        return (T)Convert.ChangeType(theValue, theNullableConverter.UnderlyingType);
                    }
                }

                // The value was null in the database, so return the default value for T; this will vary based on what T is (i.e. int has a default of 0)
                return default(T);
            }
        }

}
