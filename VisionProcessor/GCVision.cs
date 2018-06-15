///
/// File: GCVision.cs
/// 
/// These are the Google Vision analyser API interface methods.
/// They take an image as input and return analytical information about the presented image,
/// that varies depending on the function.
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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;

namespace VisionProcessor
{
    /// <summary>
    /// This class was initially intended to be solely for an upload POC.
    /// However the scope of the code has increased and therefore the name of the class
    /// is now inappropriate.  It will be changed at a later date.
    /// </summary>
    public class GCVision : ImageAnalyser
    {
        // attributes
        public enum AnalysisMethod { DETECT_FACES = 0, DETECT_LANDMARKS, DETECT_LABELS, DETECT_SAFESEARCH, DETECT_PROPERTIES, DETECT_TEXT, DETECT_LOGOS, DETECT_CROPHINT, DETECT_WEB, DETECT_DOCTEXT, DETECT_ALL };

        TraceWriter _log = null;
        public Image _image { get; set; }

        // methods
        protected internal GCVision(TraceWriter log, string imageURL, string name = "", string description = "", string hash = "")
            : base(imageURL, name, description, hash)
        {
            _log = log;
            _image = ImageFromUri(imageURL);
        }

        public static GCVision Create(TraceWriter log, string imageURL, string name = "", string description = "", string hash = "")
        {
            GCPAuthentication.GetClient(log);

            return new GCVision(log, imageURL, name, description, hash);
        }

        private static string TrimJSON(string rawJSON)
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
                    ((string.Equals("mid", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase)) ||
                    (string.Equals("Topicality", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase))))
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

        static Image ImageFromUri(string uri)
        {
            return Image.FromUri(uri);
        }

        public dynamic ApplyAnalysis(AnalysisMethod detectionType)
        {
            var response = (dynamic)null;

            try
            {
                if (String.IsNullOrEmpty(_url.ToString()))
                {
                    _log.Info($"URL: {_url.ToString()} is empty.");
                    return response;
                }

                switch (detectionType)
                {
                    case AnalysisMethod.DETECT_FACES:
                        response = GCPAuthentication.GetClient().DetectFaces(_image);
                        break;
                    case AnalysisMethod.DETECT_LANDMARKS:
                        response = GCPAuthentication.GetClient().DetectLandmarks(_image);
                        break;
                    case AnalysisMethod.DETECT_LABELS:
                        response = GCPAuthentication.GetClient().DetectLabels(_image);
                        break;
                    case AnalysisMethod.DETECT_SAFESEARCH:
                        response = GCPAuthentication.GetClient().DetectSafeSearch(_image);
                        break;
                    case AnalysisMethod.DETECT_PROPERTIES:
                        response = GCPAuthentication.GetClient().DetectImageProperties(_image);
                        break;
                    case AnalysisMethod.DETECT_TEXT:
                        response = GCPAuthentication.GetClient().DetectText(_image);
                        break;
                    case AnalysisMethod.DETECT_LOGOS:
                        response = GCPAuthentication.GetClient().DetectLogos(_image);
                        break;
                    case AnalysisMethod.DETECT_CROPHINT:
                        response = GCPAuthentication.GetClient().DetectCropHints(_image);
                        break;
                    case AnalysisMethod.DETECT_WEB:
                        response = GCPAuthentication.GetClient().DetectWebInformation(_image);
                        break;
                    case AnalysisMethod.DETECT_DOCTEXT:
                        response = GCPAuthentication.GetClient().DetectDocumentText(_image);
                        break;
                    default:
                        _log.Error($"Unrecognised detection method: { detectionType } ");
                        break;
                }
            }
            catch (AnnotateImageException e)
            {
                _log.Error($"{ e.Response.Error } for image: { _url }");
            }

            return response;
        }

        public override void AnalyseFile(string filePath)
        {
        }

        public override void AnalyseURL()
        {
        }

        private IReadOnlyCollection<EntityAnnotation> TrimEntityAnnotation( IReadOnlyCollection<EntityAnnotation> response )
        {
            if (null != response)
            {
                foreach (EntityAnnotation annotation in response)
                {
 //                   annotation.Properties.Remove("mid");
 //                   annotation.Properties.Remove("Topicality");
                }
            }

            return response;
        }

        public string DetectAll()
        {
            string jsonData = "";
            jsonData += TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_LABELS ).ToString());     // IReadOnlyCollection<EntityAnnotation>
            jsonData += ApplyAnalysis( AnalysisMethod.DETECT_DOCTEXT ).ToString();                           // TextAnnotation
            jsonData += TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS ).ToString());                         // IReadOnlyCollection<EntityAnnotation>
            jsonData += TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS ).ToString());                             // IReadOnlyCollection<EntityAnnotation>
            jsonData += ApplyAnalysis( AnalysisMethod.DETECT_WEB ).ToString();                               // WebDetection
            return jsonData;
        }

        /// <summary>
        /// 
        /// Retains description and score.  
        /// Deletes mid and topicality
        /// </summary>
        /// <returns></returns>
        public string DetectLabels()
        {
            return _jsonData = TrimEntityAnnotation( ApplyAnalysis( AnalysisMethod.DETECT_LABELS )).ToString();
        }

        /// <summary>
        /// Returns the number of faces, the expressions and position in an image.
        /// </summary>
        /// <returns></returns>
        public string DetectFaces()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_FACES).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectSafeSearch()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_SAFESEARCH);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectProperties()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_PROPERTIES).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectLandmarks()
        {
            return _jsonData = TrimEntityAnnotation( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS )).ToString();                         // IReadOnlyCollection<EntityAnnotation>
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectText()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_TEXT).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectLogos()
        {
            return _jsonData = TrimEntityAnnotation( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS )).ToString();                             // IReadOnlyCollection<EntityAnnotation>
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectCropHint()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_CROPHINT).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectWeb()
        {
            return _jsonData = ApplyAnalysis( AnalysisMethod.DETECT_WEB ).ToString();                               // WebDetection
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectDocText()
        {
            return _jsonData = ApplyAnalysis( AnalysisMethod.DETECT_DOCTEXT ).ToString();                           // TextAnnotation
        }
    }
}
