///
/// File: AzureImageAnalyser.cs
/// 
/// These are the main entry points to 'functions' that are triggers or perform general operations.
///

using System.IO;
using Microsoft.Azure; // Name space for CloudConfigurationManager
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage; // Name space for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Name space for Queue storage types
using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

using Google.Cloud.Vision.V1;

namespace VisionProcessor
{
    /// <summary>
    /// This class was initially intended to be solely for an upload POC.
    /// However the scope of the code has increased and therefore the name of the class
    /// is now inappropriate.  It will be changed at a later date.
    /// </summary>
    public class AzureImageAnalyser 
    {
        // attributes
        TraceWriter _log = null;
        protected internal string _jsonData { get; set; } = "";
        public static IConfigurationRoot _config { get; set; }
        public static string _apiKey = "";

        // methods
        protected internal AzureImageAnalyser( TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
        {
            _log = log;
        }

        public static AzureImageAnalyser Create(TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
        {
            return new AzureImageAnalyser( log, imageURL, name, description, hash );
        }

        /// <summary>
        /// AZURE BLOB Trigger function that is triggered by certain file types placed in a queue.
        /// This needs expanding to become a template method that can be turn into a specialised method
        /// for assorted types of file input.  Also a blob trigger is the wrong approach to address
        /// this as there is a long delay, up to 10 minutes for the trigger to be fired after the blob has 
        /// hit the corresponding queue.  The ideal mechanism is to use a message queue trigger that responds to 
        /// a message written to it from a blob upload and that message queue trigger is what causes the functionality here
        /// to be invoked. This will improve throughput and decrease startup time.
        /// </summary>
        /// <param name="myBlob"></param>
        /// <param name="myBlob2"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        [FunctionName("VisionAnalyser")]
        public static async Task Run(
            [BlobTrigger("vision/{name}", Connection = "functionsfactory")]Stream myBlob,
            [Blob("vision/{name}", FileAccess.ReadWrite, Connection = "functionsfactory")] CloudBlockBlob myBlob2, string name, TraceWriter log, ExecutionContext context)
        {
            try
            {
                log.Info("Creating configuration");

                var builder = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();

                _config = builder.Build();
                AzureImageAnalyser._apiKey = _config["GoogleAPIKey"];

                log.Info($"Retrieved GoogleAPIKey: { _apiKey }");

                log.Info($"FileUpload:BlobTrigger processing Name:{name} \n Size: {myBlob.Length} Bytes");

                log.Info($"FileUpload:BlobTrigger Passing blob Name:{ myBlob2.Uri.ToString() } to Vision API.");
                GCVision imageJob = GCVision.Create(log, myBlob2.Uri.ToString(), myBlob2.Name, myBlob2.Name + "_description", myBlob2.Properties.ContentMD5);
                imageJob.DetectLabels();

                log.Info($"FileUpload:BlobTrigger Placing JSON data for blob Name: {name} in queue for analysis.");

                // place JSON data in Azure storage queue for further processing
                await AddToQueue("functionsfactory", "blob-trigger", imageJob._jsonData, log);
            }

            catch (Exception ex)
            {
                log.Info($"Trigger Exception found: {ex.Message}");
                throw ex;
            }
            return;

        }


        /// <summary>
        /// Add a message to a queue for processing in some other part of the system.
        /// Currently the values for queues and so on are hard coded here but need to be removed and 
        /// placed in the application configuration file.
        /// </summary>
        /// <param name="ocrResult"></param>
        public static async Task AddToQueue(string storageAccountConnectionSetting, string queueName, string messageData, TraceWriter log)
        {

/*
            COMMENTED OUT FOR DEBUG PURPOSES ONLY - THIS IS ACTIVE CODE
            try
            {
                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting(storageAccountConnectionSetting));

                // Create the queue client.
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

                // Retrieve a reference to a queue for this storage account.
                CloudQueue queue = queueClient.GetQueueReference(queueName);

                // Create the queue if it doesn't already exist.
                await queue.CreateIfNotExistsAsync();

                // Create a message and add it to the queue.
                CloudQueueMessage message = new CloudQueueMessage(messageData);
                log.Info($"Adding message: {messageData} to queue: {queueName}");
                await queue.AddMessageAsync(message);
            }

            catch (StorageException se)
            {
                log.Error($"Exception occurred creating message queue: {queueName} {se.ToString()}");
                log.Info("Ensure the storage account is specified correctly");
            }*/
        }

        /// <summary>
        /// Extract only elements of interest from OCR JSON data.
        /// Currently this is the description and locale.
        /// This needs to be rewritten to permit a configurable list of values 
        /// to be pulled in from a configuration file.  Also the matching loop will
        /// need amending to fit this approach.
        /// </summary>
        /// <param name="rawJSON"></param>
        /// <returns></returns>
        private static string TrimJson(string rawJSON)
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(rawJSON));

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonWriter writer = new JsonTextWriter(sw);

            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();

            while (reader.Read())
            {
                if ((reader.TokenType == JsonToken.PropertyName) &&
                    ((string.Equals("description", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase)) ||
                    (string.Equals("locale", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase))))
                {
                    // Add property name and value to output JSON.
                    writer.WritePropertyName(reader.Value.ToString());
                    reader.Read();
                    writer.WriteValue(reader.Value);
                }
            }
            writer.WriteEndObject();
            return sw.ToString();
        }
    }
}
