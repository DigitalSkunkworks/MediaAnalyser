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
        TraceWriter _log = null;
        public Image _image { get; set; }

        // methods
        protected internal GCVision( TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
            : base(imageURL, name, description, hash)
        {
            _log = log;
            _image = ImageFromUri( imageURL );
        }

        public static GCVision Create(TraceWriter log, string imageURL, string name = "", string description = "", string hash = "" )
        {
            GCPAuthentication.GetClient( log );

            return new GCVision( log, imageURL, name, description, hash );
            //            using (var stream = new FileStream(JsonKeypath, FileMode.Open, FileAccess.Read))
        }

        static Image ImageFromUri(string uri)
        {
            return Image.FromUri(uri);
        }

        public override void AnalyseFile(string filePath)
        {
        }

        public override void AnalyseURL()
        {
            if (String.IsNullOrEmpty(_url.ToString()))
            {
                _log.Info($"URL: {_url.ToString()} is empty.");
                return;
            }

            _log.Info($"Analysing URL: { _url.ToString() }");
            DetectLabels();
            DetectDocText();
            DetectLandmarks();
            DetectLogos();
            DetectWeb();
            _log.Info($"Analysis results: {_jsonData}");
        }

        /// <summary>
        /// 
        /// Retains description and score.  
        /// Deletes mid and topicality
        /// </summary>
        /// <returns></returns>
        private object DetectLabels()
        {
            try
            {
                if (!_image.Equals(null))
                {
                    var response = GCPAuthentication.GetClient().DetectLabels(_image);
                    _jsonData += response.ToString();

                    foreach (var annotation in response)
                    {
                        if (annotation.Description != null)
                        {
                            _log.Info(annotation.Description);
                        }
                    }
                }
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectLabels: {response.Error} for image: { _url }");
            }
            return 0;
        }

        /// <summary>
        /// Returns the number of faces, the expressions and position in an image.
        /// </summary>
        /// <returns></returns>
        private object DetectFaces()
        {
            try
            {
                if (!_image.Equals(null))
                {
                    var response = GCPAuthentication.GetClient().DetectFaces(_image);
                    _jsonData += response.ToString();
                }
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectFaces: {response.Error}");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectSafeSearch()
        {
            try
            {
                var response = GCPAuthentication.GetClient().DetectSafeSearch(_image);
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectSafeSearch: {response.Error}");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectProperties()
        {
            try
            {
                if (!_image.Equals(null))
                {
                    var response = GCPAuthentication.GetClient().DetectImageProperties(_image);
                    _jsonData += response.ToString();
                }
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectProperties: {response.Error} for image: { _url }");
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectLandmarks()
        {
            try
            {
                var response = GCPAuthentication.GetClient().DetectLandmarks(_image);
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectLandmarks: {response.Error} for image: { _url }");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectText()
        {
            try
            {
                var response = GCPAuthentication.GetClient().DetectText(_image);
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectText: {response.Error} for image: { _url }");
            }
            
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectLogos()
        {
            try
            {
                var response = GCPAuthentication.GetClient().DetectLogos(_image);
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectLogos: {response.Error} for image: { _url }");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectCropHint()
        {
            try
            {
                CropHintsAnnotation annotation = GCPAuthentication.GetClient().DetectCropHints(_image);
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectLogos: {response.Error} for image: { _url }");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectWeb()
        {
            try
            {
                WebDetection annotation = GCPAuthentication.GetClient().DetectWebInformation(_image);
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectWebInformation: {response.Error} for image: { _url }");
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectDocText()
        {
            try
            {
                var response = GCPAuthentication.GetClient().DetectDocumentText(_image);
                _jsonData += response.ToString();
            }
            catch (AnnotateImageException e)
            {
                AnnotateImageResponse response = e.Response;
                _log.Error($"DetectDocumentText: {response.Error} for image: { _url }");
            }

            return 0;
        }
    }
}
