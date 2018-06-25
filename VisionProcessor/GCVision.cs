///
/// File: GCVision.cs
/// 
/// These are the Google Vision analyser API interface methods.
/// They take an image as input and return analytical information about the presented image,
/// that varies depending on the function.
///

using System.IO;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Text;
using Newtonsoft.Json;

using Google.Cloud.Vision.V1;

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
        public enum AnalysisMethod { DETECT_FACES = 0, DETECT_LANDMARKS, DETECT_LABELS, DETECT_SAFESEARCH, DETECT_PROPERTIES, DETECT_TEXT, DETECT_LOGOS, DETECT_CROPHINT, DETECT_WEB, DETECT_DOCTEXT, DETECT_ALL, DETECT_NONE };
        private string[] DetectFunctionNames = { "DETECT_FACES", "DETECT_LANDMARKS", "DETECT_LABELS", "DETECT_SAFESEARCH", "DETECT_PROPERTIES", "DETECT_TEXT", "DETECT_LOGOS", "DETECT_CROPHINT", "DETECT_WEB", "DETECT_DOCTEXT", "DETECT_ALL", "DETECT_NONE"};

        TraceWriter _log = null;
        public Image _image { get; set; }
        protected AnalysisMethod _detectFunctionId { get; set; } = AnalysisMethod.DETECT_NONE;

        // methods
        protected internal GCVision(TraceWriter log, string etag, string imageURL, string hash, DateTimeOffset? dateSubmitted, DateTimeOffset? dateProcessed, string name = "", string description = "" )
            : base( etag, imageURL, hash, dateSubmitted, dateProcessed, name, description )
        {
            _log = log;
            _image = ImageFromUri(imageURL);
        }

        public static GCVision Create( TraceWriter log, string etag, string imageURL, string hash, DateTimeOffset? dateSubmitted, DateTimeOffset? dateProcessed, string name = "", string description = "" )
        {
            GCPAuthentication.GetClient( log );

            return new GCVision( log, etag, imageURL, hash, dateSubmitted, dateProcessed, name, description );
        }

        /// <summary>
        /// Parent Meta data
        ///  Blob URI
        ///  Blob Date submitted
        ///  API Date processed
        ///  UID
        ///  Functions: func1, func2, ...
        ///  }
        ///  func1 payload  data
        ///  func2 payload data
        ///  func3 payload  data
        ///  func4 payload data
        ///  func5 payload  data(edited)
        /// </summary>
        /// <param name="rawJSON"></param>
        /// <returns></returns>
        private string TrimJSON(string rawJSON, bool stripJSON=false )
        {
            // fix up for incorrect JSON returned from RPC call.
            if (_detectFunctionId == AnalysisMethod.DETECT_LABELS || _detectFunctionId == AnalysisMethod.DETECT_LANDMARKS || _detectFunctionId == AnalysisMethod.DETECT_LOGOS)
            {
                rawJSON = rawJSON.Insert( 0, "{");
                var startObjAt = rawJSON.IndexOf('[', 0);
                rawJSON = rawJSON.Insert(startObjAt, "\"LabelAnnotations\": ");
                rawJSON += "}";
            }
            else if (AnalysisMethod.DETECT_DOCTEXT == _detectFunctionId)
            {
                // substitution first of special characters
                rawJSON = rawJSON.Replace("\"", "\\\\\\\"");
                rawJSON = rawJSON.Replace("\'", "\\\\\\\'");
                rawJSON = rawJSON.Insert( 0, "{ DocumentText: \"");
                rawJSON += "\" }";
            }

            bool parentStartFound   = false;
            JsonTextReader reader   = new JsonTextReader(new StringReader(rawJSON));

            StringBuilder   sb      = new StringBuilder();
            StringWriter    sw      = new StringWriter(sb);
            JsonWriter      writer  = new JsonTextWriter(sw);

            writer.Formatting = Formatting.Indented;

            while (reader.Read())
            {
                // strip elements from returned JSON data
                if (reader.TokenType == JsonToken.PropertyName && true == stripJSON)
                {
                    if (((string.Equals("mid", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase)) ||
                        (string.Equals("Topicality", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase))) ||
                        (string.Equals("entityId", reader.Value.ToString(), StringComparison.OrdinalIgnoreCase) && _detectFunctionId == AnalysisMethod.DETECT_WEB))
                    {
                        reader.Read();
                        continue;
                    }
                }

                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        // Add property name and value to output JSON.
                        writer.WritePropertyName(reader.Value.ToString());
                    }
                    else
                    {
                        writer.WriteValue(reader.Value);
                    }
                }
                else
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        writer.WriteStartObject();
                    }
                    else if (reader.TokenType == JsonToken.EndObject)
                    {
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteToken(reader.TokenType);
                    }
                    if (!parentStartFound)
                    {
                        // add JSON header data
                        writer.WritePropertyName("BLOBURI");
                        writer.WriteValue(_url);
                        writer.WritePropertyName("BLOBUID");
                        writer.WriteValue(_uid);
                        writer.WritePropertyName("BLOBDateSubmitted");
                        writer.WriteValue(_BLOBDateSubmitted);
                        writer.WritePropertyName("APIDateProcessed");
                        writer.WriteValue(_APIDateProcessed);
                        writer.WritePropertyName("APIFunction");
                        writer.WriteValue(DetectFunctionNames[(int)_detectFunctionId]);
                        parentStartFound = true;
                    }
                }
            }
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

                _detectFunctionId = detectionType;
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

        public async void DetectAll()
        {
            _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_LABELS ).ToString(), true);        // IReadOnlyCollection<EntityAnnotation>
            await AzureImageAnalyser.AddToQueue( _jsonData, _log);

            _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_DOCTEXT).Text);                     // TextAnnotation
            await AzureImageAnalyser.AddToQueue( _jsonData, _log);

            _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS ).ToString(), true);      // IReadOnlyCollection<EntityAnnotation>
            await AzureImageAnalyser.AddToQueue( _jsonData, _log);

 //           _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS ).ToString(), true);          // IReadOnlyCollection<EntityAnnotation>
 //           await AzureImageAnalyser.AddToQueue( _jsonData, _log);

            _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_WEB ).ToString(), true);                 // WebDetection
            await AzureImageAnalyser.AddToQueue( _jsonData, _log);
        }

        /// <summary>
        /// 
        /// Retains description and score.  
        /// Deletes mid and topicality
        /// </summary>
        /// <returns></returns>
        public string DetectLabels()
        {
            return _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_LABELS ).ToString());
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
            return _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS ).ToString());                         // IReadOnlyCollection<EntityAnnotation>
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
            return _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS ).ToString());                             // IReadOnlyCollection<EntityAnnotation>
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
