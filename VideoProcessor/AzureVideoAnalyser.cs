using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

// AccountID is the same as ObjectID for a user account in Azure Portal

// API key: 69b6f85e92784e3c84441d7c1f24e988
// Endpiunt is https://westeurope.api.cognitive.microsoft.com/vision/v1.0

/// <summary>
/// This is the base class used to perform video processing on selected _content.
/// This is the Azure specific code tied to a queue trigger.
/// The processing is as follows:
/// 1) Land video as Blob Storage
/// 2) Blog trigger to place message on azure message queue to state new video has been uploaded
/// 3) Queue trigger to process file by calling Azure Video Analyser
/// 4) Return JSON file on to message queue
/// 5) Put Tracewriter in this function
/// </summary>
namespace VideoProcessor
{
    public class AzureVideoAnalyser : VideoAnalyser
    {
        // attributes
        // API V2 template: "https://api.videoindexer.ai/{location}/Accounts/{accountId}/Videos/{videoId}/Index[?accessToken][&language]";
        private static string _apiUrl = "https://api.videoindexer.ai";

        /*protected internal string _url { get; set; } = "";
        protected internal string _name { get; set; } = "";
        protected internal string _description { get; set; } = "";
        protected internal string _hash { get; set; } = ""; 
        protected internal string _jsonData { get; set; } = "";*/
        protected internal string _location { get; set; } = "UK South";
        protected internal string _accountId { get; set; } = "";
        protected internal string _apiKey { get; set; } = "";
        protected internal string _videoId { get; set; } = "";
        protected internal string _partition { get; set; } = "some_partition"; 
        protected internal string _privacy { get; set; } = "private"; 
        HttpClientHandler           _handler;
        HttpClient                  _client;
        MultipartFormDataContent    _content;
        TraceWriter                 _log = null;
        string                      _accountAccessToken;
        string                      _uploadToken;
        string                      _videoAccessToken;

        // methods
        protected internal AzureVideoAnalyser( TraceWriter log, string videoURL, string name = "", string description = "", string hash = "" ) 
            : base( videoURL, name, description, hash )
        {
            _log = log;
            _url = videoURL;
          
            _content = new MultipartFormDataContent();
            _name = name;
            _description = description;
            _hash = hash;
            _accountId = GetAppSetting("AccountId");
            _location = GetAppSetting("Location");
            _apiKey = GetAppSetting("AzureVisionAPIKey");

            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
        }

        ~AzureVideoAnalyser( ) { }

        public static AzureVideoAnalyser Create( TraceWriter log, string videoURL, string name = "", string description = "", string hash = "", string                                                  privacy="private", string partition="some_partition" )
        {
            return new AzureVideoAnalyser( log, videoURL, name, description, hash );
        }

        private string GetAppSetting(string settingName)
        {
            try
            {
                // Get Google OCR API key
                var reader = new AppSettingsReader();
                var value = reader.GetValue(settingName, typeof(string));

                return value.ToString();
            }
            catch (InvalidOperationException io)
            {
                _log.Error($"Exception caught :{ io.Message}");
            }
            return "";
        }

        void CreateHttpClient()
        {
            _handler = new HttpClientHandler();
            _handler.AllowAutoRedirect = false; 
            _client = new HttpClient(_handler);
        }

        void GetAccountAccessToken()
        {
            _log.Info("GetAccountAccessToken");
            // obtain account access token
            _log.Info($"APIKEY: {_apiKey}");
            _client.DefaultRequestHeaders.Add( "Ocp-Apim-Subscription-Key", _apiKey );
            var apiURL = $"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true";
            _log.Info($"INVOKING API URL: {apiURL}");

            var accountAccessTokenRequestResult = _client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true").Result;
            _accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");
            _client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        }

        void UploadVideo()
        {
            var content = new MultipartFormDataContent();
            _log.Info($"Uploading video: {_url}");

            var apiURL = $"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos?accessToken={_accountAccessToken}&name={_name}&description={_description}&privacy={_privacy}&partition={_partition}&videoUrl={_url}";
            _log.Info($"INVOKING API URL: {apiURL}");

            var uploadRequestResult = _client.PostAsync(string.Format($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos?accessToken={_accountAccessToken}&name={_name}&description={_description}&privacy={_privacy}&partition={_partition}&videoUrl={_url}"), content).Result;
            _uploadToken = uploadRequestResult.Content.ReadAsStringAsync().Result;
        }

        void GetVideoId()
        {
            _videoId = JsonConvert.DeserializeObject<dynamic>(_uploadToken)["id"];
            _log.Info($"Uploaded Video ID: {_videoId}");
        }

        void GetVideoAccessToken()
        {
            // obtain video access token            
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
            var videoTokenRequestResult = _client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/Videos/{_videoId}/AccessToken?allowEdit=true").Result;
            _videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");
            _client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        }

        void GetVideoIndex()
        {
            // wait for the video index to finish
            while (true)
            {
                Thread.Sleep(10000);

                var videoGetIndexRequestResult = _client.GetAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos/{_videoId}/Index?accessToken={_videoAccessToken}&language=English").Result;
                var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                _log.Info($"State: {processingState}");

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing")
                {
                    _log.Info($"Full JSON: {videoGetIndexResult}");
                    break;
                }
            }
        }

        void GetIndexResults()
        {
            // search for the video
            var searchRequestResult = _client.GetAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos/Search?accessToken={_accountAccessToken}&id={_videoId}").Result;
            var searchResult = searchRequestResult.Content.ReadAsStringAsync().Result;
            _log.Info($"Searching for video: {searchResult}");

            // get insights widget url
            var insightsWidgetRequestResult = _client.GetAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos/{_videoId}/InsightsWidget?accessToken={_videoAccessToken}&widgetType=Keywords&allowEdit=true").Result;
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            _log.Info($"Insights Widget url: {insightsWidgetLink}");

            // get player widget url
            var playerWidgetRequestResult = _client.GetAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos/{_videoId}/PlayerWidget?accessToken={_videoAccessToken}").Result;
            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            _log.Info($"Player Widget url: {playerWidgetLink}");
        }

        [FunctionName("AnalyseVideo")]
        public static void Run(
            [BlobTrigger("video/{name}",  Connection = "functionsfactory")]string myQueueItem,
            [Blob("video/{name}", FileAccess.ReadWrite, Connection = "functionsfactory")] CloudBlockBlob myBlob,
            TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed:  URL: {myBlob.Uri.ToString()} : Length: {myBlob.Properties.Length}");
            // TODO: Add file size check to issue warning written to logs should the file be below a certain size.
            // TODO: Add a check to ensure only the currently documented recognised file types are uploaded.
            // TODO: Ensure that this code is written so different queue types respond to different types of file upload 
            // TODO: e.g. image files to OCR processing queue, audio recordings to speech to text queue, text to semantic analysis 
            // TODO: queu and so on.
            log.Info($"FileUpload:BlobTrigger Processing file: {myBlob.Uri.ToString()}");
            AzureVideoAnalyser videoJob = Create( log, myBlob.Uri.ToString(), myBlob.Name, myBlob.Name + "_description", myBlob.Properties.ContentMD5 );
            videoJob.AnalyseVideo();

            // store returned json data in Cosmos Db
            log.Info($"FileUpload:BlobTrigger Storing analysis data for video Name:{myBlob.Uri.ToString()} in database.");

            // place json OCR data in Azure storage queue

            log.Info($"FileUpload:BlobTrigger Placing OCR data for blob Name:{myBlob.Uri.ToString()} in queue for analysis by AWS text analysis API.");
            AddToQueue(videoJob._jsonData);

            return;
        }

        public /*override*/ void AnalyseVideo()
        {
            CreateHttpClient();
            GetAccountAccessToken();
            UploadVideo();
            GetVideoId();
            GetVideoAccessToken();
            GetVideoIndex();
            GetIndexResults();
        }

        public /*override*/ void AnalyseVideo(Uri url )
        {
            try
            {
                if (String.IsNullOrEmpty(url.ToString()))
                {
                    return;
                }

                _url = url.ToString();

                AnalyseVideo();
            }
            catch (HttpRequestException he)
            {
                _log.Error($"Exception caught :{ he.Message}");
            }

            catch (Newtonsoft.Json.JsonReaderException ne)
            {
                _log.Error(message: $"Exception caught: {ne.Message}");
            }
        }

        /// <summary>
        /// Add a message to a queue for processing in some other part of the system.
        /// Currently the values for queues and so on are hard coded here but need to be removed and 
        /// placed in the application configuration file.
        /// </summary>
        /// <param name="ocrResult"></param>
        public static void AddToQueue(string messageContent)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("videomsgqueue"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("videoqueue");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(messageContent);
            queue.AddMessage(message);
        }
    }
}

