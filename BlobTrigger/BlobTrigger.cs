using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SendGrid;
using System;

namespace BlobTrigger
{
    public static class BlobTrigger
    {
        [FunctionName("BlobTrigger")]
        public static async Task Run([BlobTrigger("userdocx/{name}", Connection = "anmanikin1_STORAGE")]Stream myBlob, string name, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Blob trigger function processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var azureConnectionString = config["anmanikin1_STORAGE"];
            var sendGridKey = config["SendGridKey"];
            var senderEmail = config["SenderEmail"];
            
            var blobServiseClient = new BlobServiceClient(azureConnectionString);
            var containerClient = blobServiseClient.GetBlobContainerClient("userdocx");
            var blobClient = containerClient.GetBlobClient(name);

            BlobProperties properties = await blobClient.GetPropertiesAsync();

            var metadata = properties.Metadata;

            try
            {
                var userEmail = metadata["userEmail"];
                // Create the SendGrid message
                var sendGridClient = new SendGridClient(sendGridKey);
                var message = new SendGridMessage();
                message.AddTo(userEmail);
                message.SetFrom(new EmailAddress(senderEmail, "Anton"));
                message.SetSubject("Your file has been successfully uploaded");
                message.AddContent("text/plain", $"A new file '{name}' has been uploaded to the Blob Storage container.");

                // Send the email using SendGrid
                var response = await sendGridClient.SendEmailAsync(message);
            }
            catch (Exception)
            {
                Console.WriteLine("Bad metadata value");
            }
        }
    }
}
