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
            DetectFaces();
            DetectCropHint();
            DetectDocText();
            DetectLandmarks();
            DetectLogos();
            DetectProperties();
            DetectSafeSearch();
            DetectText();
            DetectWeb();
            _log.Info($"Analysis results: {_jsonData}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object DetectLabels()
        {
            try
            {
                if (!_image.Equals(null))
                {
                    var response = GCPAuthentication.GetClient().DetectLabels(_image);
                    _jsonData = response.ToString();
                    foreach (var annotation in response)
                    {
                        if (annotation.Description != null)
                            _log.Info(annotation.Description);
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
                    int count = 1;
                    foreach (var faceAnnotation in response)
                    {
                        _log.Info($"Face {count++}:");
                        _log.Info($"  Joy: { faceAnnotation.JoyLikelihood}");
                        _log.Info($"  Anger: { faceAnnotation.AngerLikelihood }");
                        _log.Info($"  Sorrow: { faceAnnotation.SorrowLikelihood }");
                        _log.Info($"  Surprise: { faceAnnotation.SurpriseLikelihood }");
                    }
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

                _log.Info($"Adult: { response.Adult.ToString() }");
                _log.Info($"Spoof: { response.Spoof.ToString() }");
                _log.Info($"Medical: { response.Medical.ToString() }");
                _log.Info($"Violence: { response.Violence.ToString() }");
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
            var response = GCPAuthentication.GetClient().DetectImageProperties(_image);
            string header = "Red\tGreen\tBlue\tAlpha\n";
            foreach (var color in response.DominantColors.Colors)
            {
                Console.Write(header);
                header = "";
                _log.Info($"{ color.Color.Red }\t{ color.Color.Green }\t{ color.Color.Blue }\t{ color.Color.Alpha }");
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectLandmarks()
        {
            var response = GCPAuthentication.GetClient().DetectLandmarks(_image);
            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                    _log.Info(annotation.Description);
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectText()
        {
            var response = GCPAuthentication.GetClient().DetectText(_image);
            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                    _log.Info(annotation.Description);
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectLogos()
        {
            var response = GCPAuthentication.GetClient().DetectLogos(_image);
            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                    _log.Info(annotation.Description);
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectCropHint()
        {
            CropHintsAnnotation annotation = GCPAuthentication.GetClient().DetectCropHints(_image);
            foreach (CropHint hint in annotation.CropHints)
            {
                _log.Info($"Confidence: { hint.Confidence }");
                _log.Info($"ImportanceFraction: { hint.ImportanceFraction }");
                _log.Info("Bounding Polygon:");
                foreach (Vertex vertex in hint.BoundingPoly.Vertices)
                {
                    _log.Info($"\tX:\t{vertex.X}\tY:\t{vertex.Y}");
                }
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectWeb()
        {
            WebDetection annotation = GCPAuthentication.GetClient().DetectWebInformation(_image);
            foreach (var matchingImage in annotation.FullMatchingImages)
            {
                _log.Info($"MatchingImage Score:\t{ matchingImage.Score }\tURL:\t{ matchingImage.Url }");
            }
            foreach (var page in annotation.PagesWithMatchingImages)
            {
                _log.Info($"PageWithMatchingImage Score:\t{page.Score}\tURL:\t{page.Url}");
            }
            foreach (var matchingImage in annotation.PartialMatchingImages)
            {
                _log.Info($"PartialMatchingImage Score:\t{ matchingImage.Score }\tURL:\t{ matchingImage.Url }");
            }
            foreach (var entity in annotation.WebEntities)
            {
                _log.Info($"WebEntity Score:\t{ entity.Score }\tId:\t{ entity.EntityId }\tDescription:\t{ entity.Description }");
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object DetectDocText()
        {
            var response = GCPAuthentication.GetClient().DetectDocumentText(_image);
            foreach (var page in response.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        _log.Info(string.Join("\n", paragraph.Words));
                    }
                }
            }
            return 0;
        }
    }
}
