using Horus.Functions.Models;
using Horus.Trainer;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Placebo.Trainer
{
    class Program
    {
        static string serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
        static string trainingQueueName = Environment.GetEnvironmentVariable("TrainingQueue");

        static async Task Main(string[] args)
        {
            var directory = Directory.GetCurrentDirectory();
            var text = File.ReadAllText($"{directory}\\TrainingRequest.json");
            var request = JsonConvert.DeserializeObject<TrainingRequest>(text);
            var csb = new ServiceBusConnectionStringBuilder(request.ServiceBusConnectionString);
            csb.EntityPath = request.TrainingQueueName;
            var queueClient = new QueueClient(csb);
            foreach (var item in request.Items)
            {
                TrainingRequestMessage trainingRequestMessage = new TrainingRequestMessage
                {
                    BlobFolderName = item.BlobFolder,
                    BlobSasUrl = request.SasUrl,
                    DocumentFormat = item.DocumentFormat,
                    IncludeSubFolders = "false",
                    UseLabelFile = "true"
                };
                Console.WriteLine($"Sending Message for document format={item.DocumentFormat}");
                string data = JsonConvert.SerializeObject(trainingRequestMessage);
                Message message = new Message(Encoding.UTF8.GetBytes(data));
                await queueClient.SendAsync(message);
            }
        }


    }
}
