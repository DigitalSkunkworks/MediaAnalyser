
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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;

namespace VisionProcessor
{
    class GCPAuthentication
    {
        // attributes
        private static TraceWriter          _log;
        private static GoogleCredential     _credential;
        private static Grpc.Core.Channel    _channel;
        private static ImageAnnotatorClient _client { get; set; } = null;

        // methods
        /// <summary>
        /// The constructor is declared private so the Create method enforces singleton behaviour.
        /// </summary>
        /// <returns></returns>
        private GCPAuthentication() { }

        public static ImageAnnotatorClient GetClient( TraceWriter log = null )
        {
            if (null != log)
            {
                log.Info($"Get Client");
            }

            if (null == _client)
            {
                if (null != log)
                {
                    Create(log);
                }
                else
                {
                    throw new Exception("Cannot create an ImageAnnotatorClient without valid log instance.");
                }
                _log.Info($"Created channel for image annotator client { _channel.State }");
            }

            return _client;
        }

        /// <summary>
        /// Create
        /// Permits only a single instance of the client to be created.
        /// This may be enhanced to make it thread safe.
        /// </summary>
        /// <returns>ImageAnnotatorClient</returns>
        private static void Create( TraceWriter log = null )
        {
            log.Info($"Create Client");

            if (_client == null)
            {
                _log = log;

                GCPAuthentication._credential = CreateCredential().CreateScoped( ImageAnnotatorClient.DefaultScopes );
                _log.Info("Created credential for image annotator client");

                GCPAuthentication._channel = new Grpc.Core.Channel( ImageAnnotatorClient.DefaultEndpoint.ToString(), _credential.ToChannelCredentials() );
                _log.Info("Created channel for image annotator client");

                GCPAuthentication._client = ImageAnnotatorClient.Create( _channel );
                _log.Info("Created annotator client");
            }
        }

        /// <summary>
        /// The Google key is consumed as a file.
        /// As it will be an application setting then it has to be turned into a file stream instance
        /// to be used.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private static Stream GetGoogleAPIKey()
        {
            // Get Google  API key
            //            AppSettingsReader reader;
            string apiKey;

            try
            {
//                reader = new AppSettingsReader();
//                apiKey = (string)reader.GetValue("GoogleAPIKey", typeof(string));
//                apiKey = CloudConfigurationManager.GetSetting("GoogleAPIKey");
                apiKey = AzureImageAnalyser._apiKey;
                _log.Info($"Retrieved Google API key from application settings:\n { apiKey }.");
            }
            catch (InvalidOperationException e)
            {
                _log.Error("Missing key error: " + e.Message);
                return null;
            }

            return GenerateStreamFromString( apiKey);
        }

        private static GoogleCredential CreateCredential()
        {
            using (var stream = GetGoogleAPIKey())
            {
                return GoogleCredential.FromStream(stream);
            }
        }
    }
}
