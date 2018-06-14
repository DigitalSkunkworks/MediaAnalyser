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

        static Image ImageFromUri(string uri)
        {
            return Image.FromUri(uri);
        }

        public string ApplyAnalysis(AnalysisMethod detectionType)
        {
            string jsonData = "";
            var response = (dynamic)null;

            try
            {
                if (String.IsNullOrEmpty(_url.ToString()))
                {
                    _log.Info($"URL: {_url.ToString()} is empty.");
                    return jsonData;
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
                    case AnalysisMethod.DETECT_ALL:
                        ApplyAnalysis(AnalysisMethod.DETECT_LABELS);
                        ApplyAnalysis(AnalysisMethod.DETECT_DOCTEXT);
                        ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS);
                        ApplyAnalysis(AnalysisMethod.DETECT_LOGOS);
                        ApplyAnalysis(AnalysisMethod.DETECT_WEB);
                        break;
                    default:
                        _log.Error($"Unrecognised detection method: { detectionType } ");
                        break;
                }
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                _log.Error($"{ e.Response.Error } for image: { _url }");
            }

            return jsonData;
        }

        public override void AnalyseFile(string filePath)
        {
        }

        public override void AnalyseURL()
        {
        }

        public string DetectAll()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_ALL);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// Retains description and score.  
        /// Deletes mid and topicality
        /// </summary>
        /// <returns></returns>
        public string DetectLabels()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_LABELS);
            return _jsonData;
        }

        /// <summary>
        /// Returns the number of faces, the expressions and position in an image.
        /// </summary>
        /// <returns></returns>
        public string DetectFaces()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_FACES);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectSafeSearch()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_SAFESEARCH);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectProperties()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_PROPERTIES);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectLandmarks()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectText()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_TEXT);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectLogos()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_LOGOS);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectCropHint()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_CROPHINT);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectWeb()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_WEB);
            return _jsonData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectDocText()
        {
            ApplyAnalysis(AnalysisMethod.DETECT_DOCTEXT);
            return _jsonData;
        }
    }
}
