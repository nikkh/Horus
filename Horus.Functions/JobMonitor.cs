using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Dynamitey.DynamicObjects;
using Horus.Functions.Data;
using Horus.Functions.Models;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Horus.Functions
{
    public class JobMonitor
    {

        public JobMonitor(TelemetryConfiguration telemetryConfig) 
        {
            
        }

        [FunctionName("DocumentMonitor")]
        public async Task TriggerProcessDocument([ServiceBusTrigger("%IncomingDocumentsQueue%", Connection = "IncomingDocumentServiceBusConnectionString")] Message message, [DurableClient] IDurableOrchestrationClient starter, ILogger log, ExecutionContext ec)
        {
            log.LogInformation($"{ec.FunctionName} function was triggered by receipt of service bus message {message.MessageId}");
            var activity = message.ExtractActivity();
            string payload = System.Text.Encoding.UTF8.GetString(message.Body);
            var body = JObject.Parse(payload);
            if (!CanProcessMessage(message.MessageId, body, log))
            {
                log.LogWarning($"Message {message.MessageId} was ignored!!  Please see previous log items for reasons.");
                return;
            }

            HorusSql.CheckAndCreateDatabaseIfNecessary(log);

            string blobUrl = body["data"]["url"].ToString();
            string contentType = body["data"]["contentType"].ToString();
           
            var job = new DocumentProcessingJob { StagingBlobUrl = blobUrl, ContentType = contentType };
            string orchestrationId = await starter.StartNewAsync("DocumentProcessor",null, job);
            log.LogInformation($"{ec.FunctionName} processed message {message.MessageId}.  Orchestration {orchestrationId} will process document: {blobUrl}");
        }

        [FunctionName("TrainingMonitor")]
        public async Task TriggerTrainModel([ServiceBusTrigger("%TrainingQueue%", Connection = "IncomingDocumentServiceBusConnectionString")] Message message, [DurableClient] IDurableOrchestrationClient starter, ILogger log, ExecutionContext ec)
        {
            log.LogInformation($"{ec.FunctionName} function was triggered by receipt of service bus message {message.MessageId}");
            string payload = System.Text.Encoding.UTF8.GetString(message.Body);
            var trm = JsonConvert.DeserializeObject<TrainingRequestMessage>(payload);

            HorusSql.CheckAndCreateDatabaseIfNecessary(log);

            var job = new ModelTrainingJob { 
                BlobFolderName = trm.BlobFolderName,
                BlobSasUrl = trm.BlobSasUrl,
                DocumentFormat = trm.DocumentFormat,
                IncludeSubFolders = "false",
                UseLabelFile = "true"
            };
            string orchestrationId = await starter.StartNewAsync("ModelTrainer", null, job);
            log.LogInformation($"{ec.FunctionName} processed message {message.MessageId}.  Orchestration {orchestrationId}");
        }

        private static bool CanProcessMessage(string messageId, JObject body, ILogger log)
        {
            string eventType = body["eventType"].ToString();
            if (eventType != "Microsoft.Storage.BlobCreated")
            {
                log.LogInformation($"Message {messageId} was ignored due to event type ({eventType})");
                return false;
            }
            string blobType = body["data"]["blobType"].ToString();
            if (blobType != "BlockBlob")
            {
                log.LogInformation($"Message {messageId} was ignored due to blob type ({blobType})");
                return false;
            }
            string contentType= body["data"]["contentType"].ToString();
            if (!ParsingConstants.AllowedContentTypes.Contains(contentType))
            {
                log.LogInformation($"Message {messageId} was ignored due to content type ({contentType}).  Valid content types are {string.Join(",", ParsingConstants.AllowedContentTypes.ToArray())}.");
                return false;
            }

            return true;
        }


    }
}
