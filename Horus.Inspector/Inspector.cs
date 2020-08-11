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

namespace Horus.Inspector
{
    public class Inspector
    {

        private readonly ILogger log;
        private readonly List<ScoreRecord> records;
        private static readonly CloudBlobClient trainingBlobClient = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TrainingStorageAccountConnectionString")).CreateCloudBlobClient();

        public Inspector(ILogger log)
        {
            this.log = log;
            this.records = new List<ScoreRecord>();
        }


        public async Task<List<ScoreRecord>> Inspect() 
        {
            records.AddRange(await InspectTrainingStorage());
            records.AddRange(await InspectModelRegistration());
            return records;

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
                if (mtr != null) results.Add(new ScoreRecord { Type = $"Training", Notes = $"{mtr.ModelId} has been registered for document type {documentType}", Score = 100 * j });
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

}

