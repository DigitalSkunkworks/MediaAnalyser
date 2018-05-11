using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Vision.v1;
using Google.Apis.Vision.v1.Data;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

/// <summary>
/// This code is essentially a straight lift from the example we startded with.
/// As this is intended to become a larger system there are a loto f incorrect choices here.
/// The namespace and class names need changing to more accurately reflect how the code needs
/// restructuring.
/// </summary>
namespace ImageProcessor
{
    public class GoogleAnnotate
    {
        private TraceWriter _log;
        public string ApplicationName { get { return "Ocr"; } }
        public string JsonResult { get; set; }
        public string TextResult { get; set; }
        public string Error { get; set; }

        private string JsonKeypath
        {
            // TODO: This path needs to be changed so that trhe path to the json key file is stored in the config. file.
            //get { return Application.StartupPath + "\\your file name.json"; }
            get { return "key.json"; }
//            get { return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\key.json"; }
        }

        public static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private Stream GetGoogleOCRAPIKey()
        {
            // Get Google OCR API key
            var reader = new AppSettingsReader();
            var value = reader.GetValue("GoogleOCRAPIKey", typeof(string));

            _log.Info($"GoogleOCRAPIKey: {value}");

            return GenerateStreamFromString(value.ToString());
        }

        private GoogleCredential _credential;

        public GoogleAnnotate( TraceWriter log )
        {
            _log = log;
        }

        private GoogleCredential CreateCredential()
        {
            if (_credential != null) return _credential;
//            using (var stream = new FileStream(JsonKeypath, FileMode.Open, FileAccess.Read))
            using (var stream = GetGoogleOCRAPIKey())
            {
                string[] scopes = { VisionService.Scope.CloudPlatform };
                var credential = GoogleCredential.FromStream(stream);
                
                credential = credential.CreateScoped(scopes);
                _credential = credential;
                return credential;
            }
        }

        private VisionService CreateService(GoogleCredential credential)
        {
            var service = new VisionService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
                GZipEnabled = true,
            });
            return service;
        }

        public void GetText( Uri imgURL, string language)
        {
            WebClient myWebClient = new WebClient();

            _log.Info("Created web client to read uploaded image to memory.");

            byte[] dataBuffer = myWebClient.DownloadData(imgURL);

            GetText( dataBuffer, language);
        }

        public void GetText(string imgPath, string language)
        {
            byte[] dataBuffer = File.ReadAllBytes(imgPath);

            GetText( dataBuffer, language);
        }

        public void GetText(byte[] imgData, string language)
        {
            TextResult = JsonResult = "";

            var credential = CreateCredential();
            var service = CreateService(credential);
            service.HttpClient.Timeout = new TimeSpan(1, 1, 1);

            BatchAnnotateImagesRequest batchRequest = new BatchAnnotateImagesRequest();
            batchRequest.Requests = new List<AnnotateImageRequest>();
            batchRequest.Requests.Add(new AnnotateImageRequest()
            {
                Features = new List<Feature>() { new Feature() { Type = "TEXT_DETECTION", MaxResults = 1 }, },
                ImageContext = new ImageContext() { LanguageHints = new List<string>() { language } },
                Image = new Image() { Content = Convert.ToBase64String(imgData) }
            });

            var annotate = service.Images.Annotate(batchRequest);
            BatchAnnotateImagesResponse batchAnnotateImagesResponse = annotate.Execute();
            if (batchAnnotateImagesResponse.Responses.Any())
            {
                AnnotateImageResponse annotateImageResponse = batchAnnotateImagesResponse.Responses[0];
                if (annotateImageResponse.Error != null)
                {
                    if (annotateImageResponse.Error.Message != null)
                        Error = annotateImageResponse.Error.Message;
                }
                else
                {
                    if (annotateImageResponse.TextAnnotations != null && annotateImageResponse.TextAnnotations.Any())
                    {
                        TextResult = annotateImageResponse.TextAnnotations[0].Description.Replace("\n", "\r\n");
                        JsonResult = JsonConvert.SerializeObject(annotateImageResponse.TextAnnotations[0]);
                    }
                    return;

                }

            }

            return;
            //return TextResult;
        }

    }
}
