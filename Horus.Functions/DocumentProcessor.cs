using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Horus.Functions.Data;
using Horus.Functions.Engines;
using Horus.Functions.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
//using Microsoft.Azure.WebJobs.Extensions.DurableTask.Correlation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Horus.Functions
{
    public class DocumentProcessor
    {
        private static readonly CloudBlobClient stagingBlobClient = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StagingStorageAccountConnectionString")).CreateCloudBlobClient();
        private static readonly CloudBlobClient orchestrationBlobClient = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("OrchestrationStorageAccountConnectionString")).CreateCloudBlobClient();
        private static readonly HttpClient client = new HttpClient();
        
        private static readonly string recognizerApiKey = Environment.GetEnvironmentVariable("RecognizerApiKey");
        private static readonly string recognizerServiceBaseUrl = Environment.GetEnvironmentVariable("RecognizerServiceBaseUrl");
        private static readonly string processingEngineAssembly = Environment.GetEnvironmentVariable("ProcessingEngineAssembly");
        private static readonly string processingEngineType = Environment.GetEnvironmentVariable("ProcessingEngineType");
        private static readonly string persistenceEngineAssembly = Environment.GetEnvironmentVariable("PersistenceEngineAssembly");
        private static readonly string persistenceEngineType = Environment.GetEnvironmentVariable("PersistenceEngineType");
        private static readonly string integrationEngineAssembly = Environment.GetEnvironmentVariable("IntegrationEngineAssembly");
        private static readonly string integrationEngineType = Environment.GetEnvironmentVariable("IntegrationEngineType");

        private readonly IConfiguration config;
        private readonly TelemetryClient telemetryClient;

        public DocumentProcessor(IConfiguration config, TelemetryConfiguration telemetryConfig)
        {
            this.config = config;
            this.telemetryClient = new TelemetryClient(telemetryConfig);
        }

        [FunctionName("DocumentProcessor")]
        public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var job = context.GetInput<DocumentProcessingJob>();
            job.OrchestrationId = context.InstanceId;
            job.Ticks = context.CurrentUtcDateTime.Ticks;
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";

            int pollingInterval = 3;
            DateTime expiryTime = DateTime.Now.AddMinutes(3);

            try
            {
                job = await context.CallActivityAsync<DocumentProcessingJob>("StartPreprocessor", job);
                while (context.CurrentUtcDateTime < expiryTime)
                {
                    var jobStatus = await context.CallActivityAsync<string>("CheckPreprocessorStatus", job);
                    if (jobStatus == PreprocessorStatus.Completed)
                    {
                        job = await context.CallActivityAsync<DocumentProcessingJob>("PreprocessorCompleted", job);
                        break;
                    }
                    // Orchestration sleeps until this time.
                    var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
                    await context.CreateTimer(nextCheck, CancellationToken.None);
                }
                job = await context.CallActivityAsync<DocumentProcessingJob>("StartRecognizer", job);
                expiryTime = DateTime.Now.AddMinutes(3);
                while (context.CurrentUtcDateTime < expiryTime)
                {
                    job = await context.CallActivityAsync<DocumentProcessingJob>("CheckRecognizerStatus", job);
                    if (job.LatestRecognizerStatus == RecognizerStatus.Succeeded)
                    {
                        job = await context.CallActivityAsync<DocumentProcessingJob>("RecognizerCompleted", job);
                        break;
                    }
                    if (job.LatestRecognizerStatus == RecognizerStatus.Failed)
                    {
                        throw new Exception($"Recognizer Status is failed - need to investigate!!!.  RecognizerResponse = {job.RecognizerResponse}, orchestrationId = {job.OrchestrationId}");
                    }
                    // Orchestration sleeps until this time.
                    var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
                    await context.CreateTimer(nextCheck, CancellationToken.None);
                }
                job = await context.CallActivityAsync<DocumentProcessingJob>("Processor", job);
                job = await context.CallActivityAsync<DocumentProcessingJob>("Persistor", job);
                job = await context.CallActivityAsync<DocumentProcessingJob>("Integrator", job);
                job = await context.CallActivityAsync<DocumentProcessingJob>("Finalizer", job);
            }
            catch(Exception ex)
            {
                log.LogError($"{snip} Exception detected in document processing orchestration {ex}.  Other orchestrations will continue to run.");
                job.Exception = ex;
                job = await context.CallActivityAsync<DocumentProcessingJob>("ProcessingErrorHandler", job);
            }
            finally
            {
                context.SetOutput(job);
            }
        }

        [FunctionName("ProcessingErrorHandler")]
        public async Task<DocumentProcessingJob> ProcessingErrorHandler([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            try
            {
                var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
                var orchestrationContainer = orchestrationBlobClient.GetContainerReference(job.OrchestrationContainerName);
                await orchestrationContainer.CreateIfNotExistsAsync();
                var jobBlobName = $"{job.DocumentFormat}{BaseConstants.TrainingJobFileExtension}";
                var jobBlob = orchestrationContainer.GetBlockBlobReference(jobBlobName);
                await jobBlob.UploadTextAsync(JsonConvert.SerializeObject(job));
                var exceptionBlobName = $"{job.DocumentFormat}{BaseConstants.ExceptionExtension}";
                var exceptionBlob = orchestrationContainer.GetBlockBlobReference(exceptionBlobName);
                await exceptionBlob.UploadTextAsync(JsonConvert.SerializeObject(job.Exception));
                log.LogInformation($"{snip} - Exception Handled - Exception of Type {job.Exception.GetType()} added to blob {job.JobBlobName} was uploaded to container{job.OrchestrationContainerName}");
                return job;
            }
            catch (Exception ex)
            {
                throw new HorusTerminalException(ex);
            }
        }

        #region Preprocessing
        [FunctionName("StartPreprocessor")]
        public async Task<DocumentProcessingJob> StartPreprocessor([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            var orchestrationContainer = orchestrationBlobClient.GetContainerReference($"{job.OrchestrationId}");
            job.OrchestrationContainerName = orchestrationContainer.Name;
            await orchestrationContainer.CreateIfNotExistsAsync();
            
            log.LogTrace($"orchestrationContainerName={job.OrchestrationContainerName}");

            var stagingBlobUri = new Uri(job.StagingBlobUrl);
            var stagingContainerName = stagingBlobUri.Segments[1].Split('/')[0];
            job.DocumentFormat = stagingContainerName;
            var stagingContainer = stagingBlobClient.GetContainerReference(stagingContainerName);
            string orchestrationBlobName = $"{stagingContainerName}-{Uri.UnescapeDataString(stagingBlobUri.Segments.Last())}";
            job.OrchestrationBlobName = orchestrationBlobName;
            log.LogTrace($"orchestrationBlobName={job.OrchestrationBlobName}");
            var orchestrationBlob = orchestrationContainer.GetBlockBlobReference(orchestrationBlobName);
            job.OrchestrationBlobUrl = orchestrationBlob.Uri.ToString();
            var stagingBlob = await stagingBlobClient.GetBlobReferenceFromServerAsync(stagingBlobUri);
            await orchestrationBlob.StartCopyAsync(new Uri(GetSharedAccessUri(stagingBlob.Name, stagingContainer)));
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }

        [FunctionName("CheckPreprocessorStatus")]
        public async Task<string> CheckPreprocessorStatus([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            string jobStatus = "Pending";
            var orchestrationBlob = await orchestrationBlobClient.GetBlobReferenceFromServerAsync(new Uri(job.OrchestrationBlobUrl));
            await orchestrationBlob.FetchAttributesAsync();

            if (orchestrationBlob.CopyState.Status == CopyStatus.Pending)
            {
                jobStatus = "Pending";
            }
            if (orchestrationBlob.CopyState.Status == CopyStatus.Success)
            {
                jobStatus = "Completed";
            }
            log.LogInformation($"{snip} Job Status: {jobStatus}");
            return jobStatus;
        }

        [FunctionName("PreprocessorCompleted")]
        public async Task<DocumentProcessingJob> PreprocessorCompleted([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            var stagingBlob = await stagingBlobClient.GetBlobReferenceFromServerAsync(new Uri(job.StagingBlobUrl));
            await stagingBlob.DeleteAsync();
            log.LogDebug($"{snip} staging blob with Uri={job.StagingBlobUrl} was deleted.");
            log.LogInformation($"{snip} Completed successfully");
            return job;
        }
        #endregion

        #region Recognition
        [FunctionName("StartRecognizer")]
        public async Task<DocumentProcessingJob> StartRecognizer([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} - ";
            var model = HorusSql.GetModelIdByDocumentFormat(job.DocumentFormat);
            job.Model = model;
            log.LogTrace($"{snip} Document Name={job.DocumentName}, Format={job.DocumentFormat}, Model={model.ModelId}, Version={model.ModelVersion}");

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["includeTextDetails"] = "True";
            var uri = $"{recognizerServiceBaseUrl}{BaseConstants.FormRecognizerApiPath}/{model.ModelId}/{BaseConstants.FormRecognizerAnalyzeVerb}?{queryString}";
            log.LogTrace($"{snip} Recognizer Uri={uri}");

            HttpResponseMessage response;
            byte[] image = null;
            byte[] md5hash = null;
            var orchestrationBlob = await orchestrationBlobClient.GetBlobReferenceFromServerAsync(new Uri(job.OrchestrationBlobUrl));
            using (var memoryStream = new MemoryStream())
            {
                await orchestrationBlob.DownloadToStreamAsync(memoryStream);
                image = memoryStream.ToArray();
                using (var md5 = MD5.Create())
                {
                        memoryStream.Position = 0;
                        md5hash = md5.ComputeHash(memoryStream);
                }
            }
            job.Thumbprint = BitConverter.ToString(md5hash).Replace("-", " ");
            log.LogTrace($"{snip} Orchestration Blob={job.OrchestrationBlobName} downloaded.  Thumbprint={job.Thumbprint}");

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", recognizerApiKey);
            using (var postContent = new ByteArrayContent(image))
            {
                postContent.Headers.ContentType = new MediaTypeHeaderValue(job.ContentType);
                response = await client.PostAsync(uri, postContent);
            }

            string getUrl = "";
            if (response.IsSuccessStatusCode)
            {
                log.LogTrace($"{snip} Recognition request successful.");
                HttpHeaders headers = response.Headers;
                if (headers.TryGetValues("operation-location", out IEnumerable<string> values))
                {
                    getUrl = values.First();
                    log.LogTrace($"{snip} Recognition progress can be tracked at {getUrl}");
                }
            }
            else
            {
                log.LogTrace($"{snip} Recognition request unsuccessful.");
                throw new Exception($"{snip} That didnt work.  Trying to submit image for analysis {uri} Content:{response.Content.ReadAsStringAsync().Result}");
            }

            job.RecognizerStatusUrl = getUrl;
            log.LogInformation($"{snip} Completed successfully");
            return job;
        }

        [FunctionName("CheckRecognizerStatus")]
        public async Task<DocumentProcessingJob> CheckRecognizerStatus([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            string responseBody;
            JObject jsonContent;
            string jobStatus;
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", recognizerApiKey);
            var response = await client.GetAsync(job.RecognizerStatusUrl);
            if (response.IsSuccessStatusCode)
            {
                responseBody = response.Content.ReadAsStringAsync().Result;
                jsonContent = JObject.Parse(responseBody);
                if (jsonContent["status"] != null)
                {
                    jobStatus = jsonContent["status"].ToString();
                    if (jobStatus == "succeeded")
                    {
                        job.RecognizerResponse = responseBody;
                    }
                    job.LatestRecognizerStatus = jobStatus;
                }
                else
                {
                    throw new Exception($"{snip} Hmmmmmnn?  Checking analysis progress.  Get request was sucessful, but status element is null?");
                }
            }
            else
            {
                throw new Exception($"{snip} That didnt work.  Trying to submit image for analysis {job.RecognizerStatusUrl} Response:{response.StatusCode.ToString()}");
            }
            log.LogInformation($"{snip} - Job Status: {jobStatus}");
            return job;
        }

        [FunctionName("RecognizerCompleted")]
        public async Task<DocumentProcessingJob> RecognizerCompleted([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            var orchestrationContainer = orchestrationBlobClient.GetContainerReference(job.OrchestrationContainerName);
            var recognizedBlobName = $"{job.OrchestrationBlobName}{BaseConstants.RecognizedExtension}";
            job.RecognizedBlobName = recognizedBlobName;
            var recognizedBlob = orchestrationContainer.GetBlockBlobReference(recognizedBlobName);
            await recognizedBlob.UploadTextAsync(job.RecognizerResponse);
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }
        #endregion

        #region Processing
        [FunctionName("Processor")]
        public async Task<DocumentProcessingJob> Processor([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            var engine = (IProcessingEngine) EngineFactory.GetEngine(processingEngineAssembly, processingEngineType);
            log.LogDebug($"{snip} Processing engine: {engine.GetType()}");
            var document = engine.Process(job, log, snip);
              string documentForOutput = "********";
            if (!string.IsNullOrEmpty(document.DocumentNumber)) documentForOutput = document.DocumentNumber;
            log.LogDebug($"Orchestration {job.OrchestrationId}: {ec.FunctionName} - Document {documentForOutput} was parsed form recognizer output in {document.TimeToShred} ms");
            var orchestrationContainer = orchestrationBlobClient.GetContainerReference(job.OrchestrationContainerName);
            var documentBlobName = $"{job.OrchestrationBlobName}{BaseConstants.DocumentExtension}";
            job.DocumentName = documentBlobName;
            var documentBlob = orchestrationContainer.GetBlockBlobReference(documentBlobName);
            await documentBlob.UploadTextAsync(JsonConvert.SerializeObject(document));
            job.DocumentBlobUrl = documentBlob.Uri.ToString();
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }
        #endregion

        
        [FunctionName("Persistor")]
        public async Task<DocumentProcessingJob> Persistor([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} - ";
            var engine = (IPersistenceEngine) EngineFactory.GetEngine(persistenceEngineAssembly, persistenceEngineType);
            log.LogDebug($"{snip} Persistence engine: {engine.GetType()}");
            await engine.Save(job, log, snip);
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }


        [FunctionName("Integrator")]
        public async Task<DocumentProcessingJob> Integrator([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} - ";
            var engine = (IIntegrationEngine)EngineFactory.GetEngine(integrationEngineAssembly, integrationEngineType);
            log.LogDebug($"{snip} Integration engine: {engine.GetType()}");
            var retval = await engine.Integrate(job, log, snip);
            log.LogTrace(retval.ToString());
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }

        [FunctionName("Finalizer")]
        public async Task<DocumentProcessingJob> Finalizer([ActivityTrigger] DocumentProcessingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} - ";
            var orchestrationContainer = orchestrationBlobClient.GetContainerReference(job.OrchestrationContainerName);
            var jobBlobName = $"{job.OrchestrationBlobName}{BaseConstants.ProcessingJobFileExtension}";
            job.JobBlobName = jobBlobName;
            var jobBlob = orchestrationContainer.GetBlockBlobReference(jobBlobName);
            await jobBlob.UploadTextAsync(JsonConvert.SerializeObject(job));
            log.LogInformation($"{snip} Completed successfully");
            return job;
        }

        #region helpers

        private static string GetSharedAccessUri(string blobName, CloudBlobContainer container)
        {
            DateTime toDateTime = DateTime.Now.AddMinutes(60);

            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = null,
                SharedAccessExpiryTime = new DateTimeOffset(toDateTime)
            };

            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            string sas = blob.GetSharedAccessSignature(policy);

            return blob.Uri.AbsoluteUri + sas;
        }

       
        #endregion
    }
}