///
/// File: AzureImageAnalyser.cs
/// 
/// These are the main entry points to 'functions' that are triggers or perform general operations.
///

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VisionProcessor
{
    /// <summary>
    /// This class was initially intended to be solely for an upload POC.
    /// </summary>
    public class AzureImageAnalyser
    {
        // attributes
        TraceWriter _log = null;
        protected internal string _jsonData { get; set; } = "";
        public static IConfigurationRoot _config { get; set; }
        public static string _apiKey = "";
        public static string _queueConnection = "";
        public static string _queueName = "";

        // methods
        protected internal AzureImageAnalyser( TraceWriter log, string etag, string imageURL, string hash, string name = "", string description = "" )
        {
            _log = log;
        }

        public static AzureImageAnalyser Create(TraceWriter log, string etag, string imageURL, string hash, string name = "", string description = "" )
        {
            return new AzureImageAnalyser( log, etag, imageURL, hash, name, description );
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
                log.Info($"Blob analysis started, processing BLOB Name:{name} \n Size: {myBlob.Length} Bytes");
                log.Info("Retrieving configuration");
                var builder = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables();

                _config = builder.Build();
                AzureImageAnalyser._apiKey = _config["GoogleAPIKey"];
                AzureImageAnalyser._queueConnection = _config["MSGQ_CONSTR_VISION_ANALYSER"];
                AzureImageAnalyser._queueName = _config["MSGQ_NAME_VISION_ANALYSER"];

//                log.Info($"Retrieved GoogleAPIKey: { _apiKey }");                     // retained for debug purposes only
                log.Info($"Retrieved Queue Connection String: { _queueConnection }");
                log.Info($"Retrieved Queue Name: { _queueName }");

                log.Info($"FileUpload:BlobTrigger processing Name:{name} \n Size: {myBlob.Length} Bytes");

                log.Info($"FileUpload:BlobTrigger Passing blob Name:{ myBlob2.Uri.ToString() } to Vision API.");
                GCVision imageJob = GCVision.Create( log, myBlob2.Properties.ETag, myBlob2.Uri.ToString(), myBlob2.Properties.ContentMD5, myBlob2.Container.Properties.LastModified, DateTimeOffset.UtcNow, myBlob2.Name, myBlob2.Name + "_description" );

                imageJob.DetectAll();

                log.Info($"FileUpload:BlobTrigger Placing JSON data for blob Name: {name} in queue for analysis.");

                // place JSON data in Azure storage queue for further processing
                await AddToQueue(imageJob._jsonData, log);
                log.Info("Blob analysis completed Successfully");
            }

            catch (Exception ex)
            {
                log.Info($"Trigger Exception found: {ex.Message}");
                throw ex;
            }
            return;
        }

        public static async Task AddToQueue(string messageData, TraceWriter log)
        {
            QueueHandler queueHandler = new AzureQueueHandler( log, _queueConnection, _queueName, messageData );
            await queueHandler.AddMessageToQueueAsync( _queueConnection, _queueName, messageData );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        public static string GenerateIDFromURL(string sourceUrl)
        {
            return string.Format("{0}_{1:N}", sourceUrl, Guid.NewGuid());
        }
    }
}