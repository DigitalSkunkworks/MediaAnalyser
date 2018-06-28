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
    /// Class GCVision 
    /// Wraps the functionality exposed from the Google Cloud Vision API.
    /// Each function in the GCAPI can be invoked separately or as a block of functions and that data can be returned as 
    /// a single set of results.
    /// </summary>
    public class GCVision : ImageAnalyser
    {
        // attributes
        /// <summary>
        /// AnalysisMethod
        /// List of methods that are exposed by the GCAPI and some additional values that extend that for additional functionality.
        /// </summary>
        public enum AnalysisMethod { DETECT_FACES = 0
                                    , DETECT_LANDMARKS
                                    , DETECT_LABELS
                                    , DETECT_SAFESEARCH
                                    , DETECT_PROPERTIES
                                    , DETECT_TEXT
                                    , DETECT_LOGOS
                                    , DETECT_CROPHINT
                                    , DETECT_WEB
                                    , DETECT_DOCTEXT
                                    , DETECT_ALL
                                    , DETECT_NONE };

        /// <summary>
        /// DetectFunctionNames
        /// Set of strings that may be inserted into the logs when a method name is required.
        /// </summary>
        private static readonly string[] DetectFunctionNames = {  "DETECT_FACES"
                                                                , "DETECT_LANDMARKS"
                                                                , "DETECT_LABELS"
                                                                , "DETECT_SAFESEARCH"
                                                                , "DETECT_PROPERTIES"
                                                                , "DETECT_TEXT"
                                                                , "DETECT_LOGOS"
                                                                , "DETECT_CROPHINT"
                                                                , "DETECT_WEB"
                                                                , "DETECT_DOCTEXT"
                                                                , "DETECT_ALL"
                                                                , "DETECT_NONE"};

        /// <summary>
        /// _log
        /// Logging object instance.
        /// </summary>
        TraceWriter _log = null;

        /// <summary>
        /// _image
        /// Image object create4d from the file name or URI that is to be presented to the GCAPI for processing.
        /// </summary>
        public Image _image { get; set; }

        /// <summary>
        /// _decetFunctionId
        /// Indicates the GCAPI method invoked on the image presented to the GCAPI.
        /// </summary>
        protected AnalysisMethod _detectFunctionId { get; set; } = AnalysisMethod.DETECT_NONE;

        // methods
        protected internal GCVision(TraceWriter log, string etag, string imageURL, string hash, DateTimeOffset? dateSubmitted, DateTimeOffset? dateProcessed, string name = "", string description = "" )
            : base( etag, imageURL, hash, dateSubmitted, dateProcessed, name, description )
        {
            _log = log;
            _image = ImageFromUri(imageURL);
        }

        /// <summary>
        /// Create
        /// Permits an external process to create an instance of this object without having direct access to the constructor,
        /// thereby permitting tighter control of the use of class instances.
        public static GCVision Create( TraceWriter log, string etag, string imageURL, string hash, DateTimeOffset? dateSubmitted, DateTimeOffset? dateProcessed, string name = "", string description = "" )
        {
            GCPAuthentication.GetClient( log );

            return new GCVision( log, etag, imageURL, hash, dateSubmitted, dateProcessed, name, description );
        }

        /// <summary>
        /// TrimJSON
        /// Amends the JSON returned from the calls to the GCAPI.
        /// Adds a header to the JSON that is not present in the data returned from the GCAPI calls.
        /// Adds missing labels to JSON data arrays that are not present in the GCAPI returned JSON, as this causes
        /// version 10.0.3 of the NewtonSoft parser to throw exceptions.
        /// This has now been fixed in a subsequent release but are unable to use it currently as they .NET core 2.x API does not fully support it.
        /// Escapes some text data to ensure it is not parsed incorrectly by the Newtonsoft JSON parser.
        /// Parses the 'rawJSON' adding the header and will remove and add content according to the 'stripJSON' setting.
        /// </summary>
        /// TrimJSON
        /// 
        /// <param name="rawJSON"></param>
        /// <param name="stripJSON"></param>
        /// <returns></returns>
        private string TrimJSON(string rawJSON, bool stripJSON=false )
        {
            // fix up for incorrect JSON returned from RPC call.
            // adds LAbelAnnotations to the start of arrays for
            // the following Jfunctions
            if (_detectFunctionId == AnalysisMethod.DETECT_LABELS 
                || _detectFunctionId == AnalysisMethod.DETECT_LANDMARKS 
                || _detectFunctionId == AnalysisMethod.DETECT_LOGOS)
            {
                rawJSON = rawJSON.Insert( 0, "{");
                var startObjAt = rawJSON.IndexOf('[', 0);
                rawJSON = rawJSON.Insert(startObjAt, "\"LabelAnnotations\": ");
                rawJSON += "}";
            }
            else if (AnalysisMethod.DETECT_DOCTEXT == _detectFunctionId)
            {
                // Add escape sequence to text to remove NewtonSoft processing issues.
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
            JsonWriter writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            // Parse the JSON remaining in the message stripping some elements. 
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
                    // Start and end objects have to be written out explicitly using NewtonSoft funcs.
                    // If not then the JSON fails to parse and an exception is thrown.
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
                        // If this is the first block of data in the JSON then prepend it
                        // with a header to account for Google not returning the correct data.
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

        /// <summary>
        /// ImageFromURI
        /// Create an Google Image object from a given URI 'url'.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        static Image ImageFromUri(string uri)
        {
            return Image.FromUri(uri);
        }

        /// <summary>
        /// ApplyAnaysis
        /// Given a specific 'detectionType' the corresponding GC Vision API function is invoked.
        /// Returns a different type from different API calls, hence the use of a dynamic type object.
        /// </summary>
        /// <param name="detectionType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// AnalyseFile
        /// Overridden ABC method that are currently unused.
        /// </summary>
        /// <param name="filePath"></param>
        public override void AnalyseFile(string filePath)
        {
        }

        /// <summary>
        /// AnalyseURL
        /// Overridden ABC method that are currently unused.
        /// </summary>
        /// <param name="filePath"></param>
        public override void AnalyseURL()
        {
        }

        /// <summary>
        /// DetectAll
        /// Parent method that invokes what is currently defined as 'ALL' methods from the GC API.
        /// It was decided that each message due to a 64K limitation of the messages size should be returned
        /// separately to the queue rather than combined as a single message.
        /// </summary>
        public async void DetectAll()
        {
            try
            {
                _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_TEXT ).ToString(), true );            // TextAnnotation
                await AzureImageAnalyser.AddToQueue( _jsonData, _log);

                _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_LABELS ).ToString(), true );          // IReadOnlyCollection<EntityAnnotation>
                await AzureImageAnalyser.AddToQueue( _jsonData, _log);

                _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_DOCTEXT ).Text );                       // TextAnnotation
                await AzureImageAnalyser.AddToQueue( _jsonData, _log);

                _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS ).ToString(), true );        // IReadOnlyCollection<EntityAnnotation>
                await AzureImageAnalyser.AddToQueue( _jsonData, _log);

 //           _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS ).ToString(), true );              // IReadOnlyCollection<EntityAnnotation>
 //           await AzureImageAnalyser.AddToQueue( _jsonData, _log);

                _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_WEB ).ToString(), true );             // WebDetection
                await AzureImageAnalyser.AddToQueue( _jsonData, _log);
            }
            catch (Exception ex)
            {
                _log.Error($"Trigger Exception found: {ex.Message}");

                throw ex;
            }
        }

        /// <summary>
        /// DetectLabels
        /// Identifies and applies labels to recognised items in an image.
        /// </summary>
        /// <returns></returns>
        public string DetectLabels()
        {
            return _jsonData = TrimJSON( ApplyAnalysis( AnalysisMethod.DETECT_LABELS ).ToString());
        }

        /// <summary>
        /// DetectFaces
        /// Returns the number of faces, the expressions and position in an image.
        /// </summary>
        /// <returns></returns>
        public string DetectFaces()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_FACES).ToString();
        }

        /// <summary>
        /// DetectSafeSearch
        /// Compares to determine if it has any material considered controversial.
        /// </summary>
        /// <returns></returns>
        public string DetectSafeSearch()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_SAFESEARCH);
        }

        /// <summary>
        /// DetectProperties
        /// </summary>
        /// <returns></returns>
        public string DetectProperties()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_PROPERTIES).ToString();
        }

        /// <summary>
        /// DetectLandmarks
        /// Recognises any image of structures that are known to the Google Vision image database.
        /// </summary>
        /// <returns></returns>
        public string DetectLandmarks()
        {
            return _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LANDMARKS ).ToString());                         // IReadOnlyCollection<EntityAnnotation>
        }

        /// <summary>
        /// DetectText
        /// Returns any recognisable written text from ab image.
        /// </summary>
        /// <returns></returns>
        public string DetectText()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_TEXT).ToString();
        }

        /// <summary>
        /// DetectLogos
        /// Finds any logos that it has in its database of logo images.
        /// </summary>
        /// <returns></returns>
        public string DetectLogos()
        {
            return _jsonData = TrimJSON( ApplyAnalysis(AnalysisMethod.DETECT_LOGOS ).ToString());                             // IReadOnlyCollection<EntityAnnotation>
        }

        /// <summary>
        /// DetectCropHint
        /// </summary>
        /// <returns></returns>
        public string DetectCropHint()
        {
            return _jsonData = ApplyAnalysis(AnalysisMethod.DETECT_CROPHINT).ToString();
        }

        /// <summary>
        /// DetectWeb
        /// Compares an image with images from the web to determine if there are similarities or exact matched images.
        /// </summary>
        /// <returns></returns>
        public string DetectWeb()
        {
            return _jsonData = ApplyAnalysis( AnalysisMethod.DETECT_WEB ).ToString();                               // WebDetection
        }

        /// <summary>
        /// DetectDocText
        /// 
        /// </summary>
        /// <returns></returns>
        public string DetectDocText()
        {
            return _jsonData = ApplyAnalysis( AnalysisMethod.DETECT_DOCTEXT ).ToString();                           // TextAnnotation
        }
    }
}
