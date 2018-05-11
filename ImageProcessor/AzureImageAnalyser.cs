///
/// File: FileUpload.cs
/// 
/// These are the main entry points to 'functions' that are triggers or perform general operatiojns.
///


using System.IO;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ImageProcessor
{
    /// <summary>
    /// This class was initially intended to be solely for an upload POC.
    /// However the scope of the code has increased and therefore the name of the class
    /// is now inappropriate.  It will be changed at a later date.
    /// </summary>
    public class AzureImageAnalyser // : ImageAnalyser
    {
        /// <summary>
        ///  Constant from oreiginal source code - now irrelevant but code checks need to be in here
        ///  for file size to allow the user to be noitfied that if certain uploaded file types are not 
        ///  above a specific size the OCR results are likely to be poor.
        /// </summary>
        private const int MAX_FILE_SIZE = 1024 * 1024;

        // attributes
        TraceWriter _log = null;
        protected internal string _jsonData { get; set; } = "";

        // methods
        protected internal AzureImageAnalyser( TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
   //         : base(imageURL, name, description, hash)
        {
            _log = log;
        }
        public static AzureImageAnalyser Create(TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
        {
            return new AzureImageAnalyser( log, imageURL, name, description, hash );
        }


        /// <summary>
        /// AZURE BLOB Trigger function that is triggered by certain file types placed in a queue.
        /// This needs expaning to become a template method that can be turn into a specialised method
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
        [FunctionName("AnalyseImage")]
        public static void Run(
            [BlobTrigger("ocr/{name}", Connection = "functionsfactory")]Stream myBlob,
            [Blob("ocr/{name}", FileAccess.ReadWrite, Connection = "functionsfactory")] CloudBlockBlob myBlob2,
            string name,
            TraceWriter log)
        {
            //TODO: Add file upload filter
            log.Info($"FileUpload:BlobTrigger received blob Name:{name} \n Size: {myBlob.Length} Bytes");

            // TODO: Add file size check to issue warning written to logs should the file be below a certain size.
            // TODO: Add a check to ensure only the currently documented recognised file types are uploaded.
            // TODO: Ensure that this code is written so different queue types respond to different types of file upload 
            // TODO: e.g. image files to OCR processing queue, audio recordings to speech to text queue, text to semantic analysis 
            // TODO: queu and so on.
            log.Info($"FileUpload:BlobTrigger Processing file: {name}");

            AzureImageAnalyser imageJob = Create(log, myBlob2.Uri.ToString(), myBlob2.Name, myBlob2.Name + "_description", myBlob2.Properties.ContentMD5);
            imageJob.AnalyseImage(myBlob2.Uri);


            // pass file to Google Vision API for OCR
            log.Info($"FileUpload:BlobTrigger Passing blob Name:{name} to OCR API.");

            // store returned json data in Cosmos Db
            log.Info($"FileUpload:BlobTrigger Storing OCR data for blob Name:{name} in database.");

            AddToQueue(imageJob._jsonData);

            // place json OCR data in Azure storage queue
            log.Info($"FileUpload:BlobTrigger Placing OCR data for blob Name:{name} in queue for analysis by AWS text analysis API.");

            return;
        }

        public /*override*/ void AnalyseImage(Uri url)
        {
            if (String.IsNullOrEmpty(url.ToString()))
            {
                _log.Info($"URL: {url.ToString()} is empty.");
                return;
            }

            _log.Info("Uploading...");
            GoogleCloud ocrAPI = new GoogleCloud(_log);
            ocrAPI.OCRFile(url);
            _jsonData = ocrAPI.ResultJsonString;

            _log.Info($"Uploaded...: {_jsonData}");
        }


        /// <summary>
        /// Add a message to a queue for processing in some other part of the system.
        /// Currently the values for queues and so on are hard coded here but need to be removed and 
        /// placed in the application configuration file.
        /// </summary>
        /// <param name="ocrResult"></param>
        public static void AddToQueue(string ocrResult)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("ocrmsgqueue"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("ocrqueue");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(TrimJson(ocrResult));
            queue.AddMessage(message);
        }

        /// <summary>
        /// Extract only elements of interest from OCR JSON data.
        /// Currently this is the description and locale.
        /// This needs to be rewritten to permit a configurable list of values 
        /// to be pulled in from a config file.  Also the matching loop will
        /// need amending to fit this appraoch.
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
