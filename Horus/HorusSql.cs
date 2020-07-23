using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                bool modelTrainingTableExist = false;
                using (reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        modelTrainingTableExist = true;
                        log.LogTrace("Table ModelTraining exists no need to create database tables");
                    }
                }

                if (modelTrainingTableExist)
                {
                    command.CommandText = "select count(*) from ModelTraining as NumberOfTrainedModels";
                    Int32 numberOfTrainedModels = (Int32)command.ExecuteScalar();
                    if (numberOfTrainedModels == 0)
                    {
                        log.LogCritical($"There are no trained models registered in the database");
                        throw new Exception("There are no trained models registered in the database");
                    }
                    return;
                }

                log.LogInformation($"Database tables will be created...");
                SqlTransaction transaction = connection.BeginTransaction("InitializeDatabase");
                command.Transaction = transaction;

                var commandStr = "If not exists (select name from sysobjects where name = 'ModelTraining')" +
                 "CREATE TABLE [dbo].[ModelTraining]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentFormat] [nvarchar](15) NOT NULL, [ModelVersion] [int] NOT NULL, [ModelId] [nvarchar](50) NOT NULL, [CreatedDateTime] [datetime2](7) NOT NULL," +
                 "[UpdatedDateTime] [datetime2](7) NOT NULL, [BlobSasUrl] [nvarchar](max)NOT NULL, [BlobFolderName] [nvarchar](50) NULL, [IncludeSubfolders] [bit] NOT NULL, [UseLabelFile] [bit] NOT NULL, [AverageModelAccuracy] [decimal](19, 5) NOT NULL," +
                 "[TrainingDocumentResults] [nvarchar](max)NOT NULL," +
                 "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table ModelTraining was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'Document')" +
                    "CREATE TABLE[dbo].[Document]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentNumber] [nvarchar](50) NOT NULL, [TaxDate] [datetime2](7) NULL, [OrderNumber] [nvarchar](50) NULL," +
                        "[OrderDate] [datetime2](7) NULL, [FileName] [nvarchar](50) NULL, [ShreddingUtcDateTime] [datetime2](7) NOT NULL, [TimeToShred] [bigint] NOT NULL, [RecognizerStatus] [nvarchar](50) NULL," +
                        "[RecognizerErrors] [nvarchar](50) NULL, [UniqueRunIdentifier] [nvarchar](50) NOT NULL, [TerminalErrorCount] [int] NOT NULL, [WarningErrorCount] [int] NOT NULL, [IsValid] [bit] NOT NULL," +
                        "[Account] [nvarchar](50) NULL,	[VatAmount] [decimal](19, 5) NULL,	[NetTotal] [decimal](19, 5) NULL, [GrandTotal] [decimal](19, 5) NULL, [PostCode] [nvarchar](10) NULL, [Thumbprint] [nvarchar](50) NULL," +
                        "[TaxPeriod] [nvarchar](6) NULL, [ModelId] [nvarchar](50) NULL, [ModelVersion] [nvarchar](50) NULL," +
                        "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table Document was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'DocumentError')" +
                    "CREATE TABLE [dbo].[DocumentError]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentId] [int] NOT NULL, [ErrorCode] [nvarchar](10) NULL, [ErrorSeverity] [nvarchar](10) NULL, [ErrorMessage] [nvarchar](max)NULL, " +
                        "PRIMARY KEY CLUSTERED ([Id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON[PRIMARY]) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                command.CommandText = commandStr;
                command.ExecuteNonQuery();
                log.LogTrace($"Table DocumentErrors was created.");

                commandStr = "If not exists (select name from sysobjects where name = 'DocumentLineItem')" +
                  "CREATE TABLE[dbo].[DocumentLineItem]([Id][int] IDENTITY(1, 1) NOT NULL, [DocumentId] [int] NOT NULL, [DocumentLineNumber] [nvarchar](5) NOT NULL, [ItemDescription] [nvarchar](max)NULL, [LineQuantity] [nvarchar](50) NULL," +
                    "[UnitPrice] [decimal](19, 5) NULL, [VATCode] [nvarchar](50) NULL, [NetAmount] [decimal](19, 5) NULL, [CalculatedLineQuantity] [decimal](18, 0) NULL," +
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
            log.LogCritical($"There are no trained models registered in the database");
            throw new Exception("There are no trained models registered in the database");
        }

        public static ModelTraining GetModelIdByDocumentFormat(string documentFormat)
        {
            var mt = new ModelTraining();
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

        public static void UpdateModelTraining(ModelTrainingJob m, ILogger log)
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

                log.LogInformation($"Training request for document format {m.DocumentFormat}, version={newVersion}, model id={m.ModelId}  was written to the database");

            }
        }
    }
}
