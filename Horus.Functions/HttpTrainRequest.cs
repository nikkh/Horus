using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using Horus.Functions.Models;
using System.Text;

namespace Horus.Functions
{
    public static class HttpTrainRequest
    {
        private static readonly string serviceBusConnectionString = Environment.GetEnvironmentVariable("IncomingDocumentServiceBusConnectionString");
        private static readonly string trainingQueue = Environment.GetEnvironmentVariable("TrainingQueue");

        [FunctionName("HttpTrainRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            
            log.LogInformation($"HttpTrainRequest Function triggered by HttpRequest at: {DateTime.Now}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic payload = JsonConvert.DeserializeObject(requestBody);

            var csb = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            csb.EntityPath = trainingQueue;
            var queueClient = new QueueClient(csb);
            string formats = "";
            foreach (var item in payload.Items)
            {
                formats += item.DocumentFormat + ", ";
                TrainingRequestMessage trainingRequestMessage = new TrainingRequestMessage
                {
                    BlobFolderName = item.BlobFolder,
                    BlobSasUrl = payload.SasUrl,
                    DocumentFormat = item.DocumentFormat,
                    IncludeSubFolders = "false",
                    UseLabelFile = "true"
                };
                Console.WriteLine($"Sending Message for document format={item.DocumentFormat}");
                string data = JsonConvert.SerializeObject(trainingRequestMessage);
                Message message = new Message(Encoding.UTF8.GetBytes(data));
                await queueClient.SendAsync(message);
            }

            return new OkObjectResult($"HttpTrainRequest: your training request for document formats {formats.Substring(0, formats.Length-2)} submitted sucessfully.");
        }
    }
}
