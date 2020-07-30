using Horus.Functions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Functions.Engines
{
    public class CosmosPersistenceEngine : PersistenceEngine
    {
        private static readonly CloudBlobClient orchestrationBlobClient = CloudStorageAccount.Parse(
            Environment.GetEnvironmentVariable("OrchestrationStorageAccountConnectionString")).CreateCloudBlobClient();
        private static readonly string endpoint = Environment.GetEnvironmentVariable("CosmosEndPointUrl");
        private static readonly string authKey = Environment.GetEnvironmentVariable("CosmosAuthorizationKey");
        private static readonly CosmosClient cosmosClient = new CosmosClient(endpoint, authKey);
        private static readonly string cosmosDatabaseId = Environment.GetEnvironmentVariable("CosmosDatabaseId");
        private static readonly string cosmosContainerId = Environment.GetEnvironmentVariable("CosmosContainerId");

        public async override Task<DocumentProcessingJob> Save(DocumentProcessingJob job, ILogger log, string snip)
        {
            var documentBlob = await orchestrationBlobClient.GetBlobReferenceFromServerAsync(new Uri(job.DocumentBlobUrl));
            string documentBlobContents;
            using (var memoryStream = new MemoryStream())
            {
                await documentBlob.DownloadToStreamAsync(memoryStream);
                documentBlobContents = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            var document = JsonConvert.DeserializeObject<Document>(documentBlobContents);
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDatabaseId);
            ContainerProperties containerProperties = new ContainerProperties(cosmosContainerId, partitionKeyPath: "/Account");
            Container container = await database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400);
            _ = await container.CreateItemAsync(document, new PartitionKey(document.Account),
            new ItemRequestOptions()
            {
                EnableContentResponseOnWrite = false
            });
            log.LogDebug($"{snip} document {document.DocumentNumber} was saved to Cosmos - database={cosmosDatabaseId}, container={cosmosContainerId})");
            return job;
        }
    }
}
