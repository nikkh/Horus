using Horus.Functions.Data;
using Horus.Functions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Functions.Engines
{
    public class SqlPersistenceEngine : PersistenceEngine
    {
        private static readonly CloudBlobClient orchestrationBlobClient = CloudStorageAccount.Parse(
            Environment.GetEnvironmentVariable("OrchestrationStorageAccountConnectionString")).CreateCloudBlobClient();
       

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
            HorusSql.SaveDocument(document, log);
            var results = $"{snip} document {document.DocumentNumber} was saved to SQL";
            log.LogDebug(results);
            return job;
        }
    }
}
