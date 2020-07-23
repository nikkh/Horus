using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Threading.Tasks;

using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Horus.Functions.Models;
using Horus.Functions.Data;

namespace Placebo.Functions
{
    public class ModelTrainer
    {
        private readonly IConfiguration config;
        private readonly TelemetryClient telemetryClient;
        private static readonly HttpClient client = new HttpClient();
        public static readonly string sqlConnectionString = Environment.GetEnvironmentVariable("SQLConnectionString");
        private static readonly string recognizerApiKey = Environment.GetEnvironmentVariable("RecognizerApiKey");
        private static readonly string recognizerServiceBaseUrl = Environment.GetEnvironmentVariable("RecognizerServiceBaseUrl");

        public ModelTrainer(IConfiguration config, TelemetryConfiguration telemetryConfig)
        {
            this.config = config;
            this.telemetryClient = new TelemetryClient(telemetryConfig);
        }

        [FunctionName("ModelTrainer")]
        public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var job = context.GetInput<ModelTrainingJob>();
            job.OrchestrationId = context.InstanceId;
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";

            int pollingInterval = 3;
            DateTime expiryTime = DateTime.Now.AddMinutes(3);
            try
            {
                job = await context.CallActivityAsync<ModelTrainingJob>("StartTraining", job);
                while (context.CurrentUtcDateTime < expiryTime)
                {
                    job = await context.CallActivityAsync<ModelTrainingJob>("CheckTrainingStatus", job);
                    if (job.LatestRecognizerStatus == TrainingStatus.Ready)
                    {
                        job = await context.CallActivityAsync<ModelTrainingJob>("TrainingCompleted", job);
                        break;
                    }
                    // Orchestration sleeps until this time.
                    var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
                    await context.CreateTimer(nextCheck, CancellationToken.None);
                }

            }
            catch (Exception ex)
            {
                log.LogError($"{snip} Exception detected in model training orchestration {ex}.  Other orchestrations will continue to run.");
            }
            finally
            {
                context.SetOutput(job);
            }
        }


        #region Training
        [FunctionName("StartTraining")]
        public async Task<ModelTrainingJob> StartTraining([ActivityTrigger] ModelTrainingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} - ";
            var uri = $"{recognizerServiceBaseUrl}{ParsingConstants.FormRecognizerApiPath}";

            JObject body = new JObject(
                new JProperty("source", job.BlobSasUrl),
                new JProperty("sourceFilter",
                    new JObject(
                        new JProperty("prefix", job.BlobFolderName),
                        new JProperty("includeSubFolders", job.IncludeSubFolders)
                    )
                ),
                new JProperty("useLabelFile", job.UseLabelFile)
            );
            string json = body.ToString();

            string getUrl = "";
            using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.Add(ParsingConstants.OcpApimSubscriptionKey, recognizerApiKey);
                HttpResponseMessage response = await client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    HttpHeaders headers = response.Headers;
                    if (headers.TryGetValues("location", out IEnumerable<string> values))
                    {
                        getUrl = values.First();

                    }
                }
                else
                {
                    var test = await response.Content.ReadAsStringAsync();
                    throw new Exception($"That didnt work.  Trying to submit model training request {test} request was {json} Response:{response.StatusCode.ToString()}");
                }
            }

            job.RecognizerStatusUrl = getUrl;
            log.LogInformation($"{snip} Completed successfully");
            return job;
        }

        [FunctionName("CheckTrainingStatus")]
        public async Task<ModelTrainingJob> CheckTrainingStatus([ActivityTrigger] ModelTrainingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            string responseBody;
            JObject jsonContent;
            string jobStatus;
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", recognizerApiKey);
            var response = await client.GetAsync(job.RecognizerStatusUrl);
            if (response.IsSuccessStatusCode)
            {
                responseBody = await response.Content.ReadAsStringAsync();
                jsonContent = JObject.Parse(responseBody);
                if (jsonContent["modelInfo"]["status"] != null)
                {
                    jobStatus = jsonContent["modelInfo"]["status"].ToString();
                    if (jobStatus.ToLower() == "ready")
                    {
                        job.ModelId = jsonContent["modelInfo"]["modelId"].ToString();
                        string dateAsString = DateTime.Now.ToString();
                        try
                        {
                            dateAsString = jsonContent["modelInfo"]["createdDateTime"].ToString();
                        }
                        catch { }

                        DateTime dateValue;
                        if (DateTime.TryParse(dateAsString, out dateValue)) job.CreatedDateTime = dateValue;

                        dateAsString = DateTime.Now.ToString();
                        try
                        {
                            dateAsString = jsonContent["modelInfo"]["lastUpdatedDateTime"].ToString();
                        }
                        catch { }

                        if (DateTime.TryParse(dateAsString, out dateValue)) job.UpdatedDateTime = dateValue;



                        string numberAsString = "0";
                        try
                        {
                            numberAsString = jsonContent["trainResult"]["averageModelAccuracy"].ToString();
                        }
                        catch { }

                        decimal numberValue = 0;
                        if (Decimal.TryParse(numberAsString, out numberValue))
                        {
                            job.AverageModelAccuracy = numberValue;
                        }
                        else { job.AverageModelAccuracy = 0; }

                        job.TrainingDocumentResults = jsonContent["trainResult"]["trainingDocuments"].ToString();
                    }
                    if (jobStatus.ToLower() == "invalid")
                    {
                        throw new Exception($" Training failed. Status={jobStatus} The response body was {responseBody}");
                    }
                    log.LogInformation($"{snip} - Job Status: {jobStatus}");
                    return job;
                }
                throw new Exception($" Training failed. Status=null The response body was {responseBody}");
            }
            throw new Exception($" Training failed. Http Status Code {response.StatusCode}");
        }

        [FunctionName("TrainingCompleted")]
        public async Task<ModelTrainingJob> TrainingCompleted([ActivityTrigger] ModelTrainingJob job, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext ec)
        {
            var snip = $"Orchestration { job.OrchestrationId}: { ec.FunctionName} -";
            HorusSql.UpdateModelTraining(job, log);
            log.LogInformation($"{snip} - Completed successfully");
            return job;
        }
        #endregion
        

                
          
        
      
    }
}

