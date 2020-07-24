using Horus.Functions.Models;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Threading.Tasks;

namespace Placebo.Trainer
{
    class Program
    {
        static string serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
        static string trainingQueueName = Environment.GetEnvironmentVariable("TrainingQueue");

        static async Task<int> Main(string[] args)
        {

        Console.WriteLine($"Training Queue Name = {trainingQueueName}");

            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--documentFormat",
                    description: "The document format for which the training assets should be uploaded"),
                new Option<string>(
                    "--labellingContainerSasUrl",
                    description: "SAS Token for container that holds the folder with the labelling and training assets"),
                new Option<string>(
                    "--blobContainerFolder",
                    getDefaultValue: () => null,
                    description: "The name of a folder within the blob container where the assets from the labelling tool aare stored"),
            };

            rootCommand.Description = "This command triggers training of a forms recognizer model, based on assets produced by the labelling tool and stored in a folder in blob storage.";
            try
            {
                rootCommand.Handler = CommandHandler.Create<string, string, string>(async (documentFormat, labellingContainerSasUrl, blobContainerFolder) =>
                {
                    try
                    {
                        Console.WriteLine($"The value for --documentFormat is: {documentFormat}");
                        if (string.IsNullOrEmpty(documentFormat))
                        {
                            throw new Exception($"--documentFormat {documentFormat} must be provided");
                        }

                        
                        Console.WriteLine($"The value for --labellingContainerSasUrl is: {labellingContainerSasUrl}");
                        if (string.IsNullOrEmpty(labellingContainerSasUrl))
                        {
                            throw new Exception($"--blobContainer {labellingContainerSasUrl} must be provided");
                        }

                        Console.WriteLine($"The value for --blobContainerFolder is: {blobContainerFolder}");
                        if (string.IsNullOrEmpty(blobContainerFolder))
                        {
                            throw new Exception($"--blobContainerFolder {blobContainerFolder} must be provided");
                        }

                        TrainingRequestMessage trainingRequestMessage = new TrainingRequestMessage
                        {
                            BlobFolderName = blobContainerFolder,
                            BlobSasUrl = labellingContainerSasUrl,
                            DocumentFormat = documentFormat,
                            IncludeSubFolders = "false",
                            UseLabelFile = "true"
                        };

                        await SendMessage(trainingRequestMessage);


                        Console.WriteLine("done.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);

                    }
                }
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
            return rootCommand.InvokeAsync(args).Result;

        }

        private static async Task SendMessage(TrainingRequestMessage trm)
        {
            var csb = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            
            csb.EntityPath = trainingQueueName;
            var queueClient = new QueueClient(csb);
            string data = JsonConvert.SerializeObject(trm);
            Message message = new Message(Encoding.UTF8.GetBytes(data));
            await queueClient.SendAsync(message);
        }
    }
}
