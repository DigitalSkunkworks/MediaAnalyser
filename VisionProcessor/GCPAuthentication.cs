
using System.IO;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Text;

using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;

namespace VisionProcessor
{
    class GCPAuthentication
    {
        /// <summary>
        /// _log
        /// Logging object instance
        /// </summary>
        private static TraceWriter          _log;

        /// <summary>
        /// _credential
        /// Google ADC - application default credentials.
        /// This is a value unique to the authorised entity to execute this code
        /// </summary>
        private static GoogleCredential     _credential;

        /// <summary>
        /// _channel
        /// RPC calling channel.
        /// </summary>
        private static Grpc.Core.Channel    _channel;

        /// <summary>
        /// _client
        /// Object that gives access to the Vision API functionality.
        /// </summary>
        private static ImageAnnotatorClient _client { get; set; } = null;

        /// <summary>
        /// ChannelState
        /// List of strings representing a lookup table of channel states to insert into log
        /// statements.
        /// </summary>
        private static readonly string[] ChannelState = {   "Channel is connecting",
                                                            "Channel is idle",
                                                            "Channel is ready for work",
                                                            "Channel has seen a failure that it cannot recover from",
                                                            "Channel has seen a failure but expects to recover" };

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
                _log.Info($"Created channel to image annotator client: { _channel.State }: { ChannelState[(int)_channel.State] }");

                GCPAuthentication._client = ImageAnnotatorClient.Create( _channel );
                _log.Info("Created annotator client");
            }
        }

        /// <summary>
        /// The Google key is an application setting. It has to be turned into a file stream instance
        /// to be used.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        /// <summary>
        /// GetGoogle|APIKey
        /// Retrieves the Vision API key from the settings for the application.
        /// </summary>
        /// <returns></returns>
        private static Stream GetGoogleAPIKey()
        {
            string apiKey;

            try
            {
                apiKey = AzureImageAnalyser._apiKey;
            }
            catch (InvalidOperationException e)
            {
                _log.Error("Missing key error: " + e.Message);
                return null;
            }

            return GenerateStreamFromString( apiKey);
        }

        /// <summary>
        /// CreateCredential
        /// Constructs a GC API credential from a value in the application settings then converts this to a stream object as 
        /// this is the only form that can be parsed by the GC API.
        /// </summary>
        /// <returns></returns>
        private static GoogleCredential CreateCredential()
        {
            using (var stream = GetGoogleAPIKey())
            {
                return GoogleCredential.FromStream(stream);
            }
        }
    }
}
