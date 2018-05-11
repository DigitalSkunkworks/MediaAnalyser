using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor
{
    /// <summary>
    ///  All functionality to access Google Cloud is here.
    /// </summary>
    public class GoogleCloud
    {
        public GoogleCloud(TraceWriter log)
        {
            _log = log;
        }

        /// <summary>
        TraceWriter _log;
        /// URL in storage to access
        /// </summary>
        public Uri _ImageURI { get; set; }
        public string _ImagePath { get; set; }
        /// <summary>
        /// languiage associated with the output text
        /// </summary>
        public string languageType { get; set; } = "en";

        /// <summary>
        /// JSON data containing coordinates of bounding boxes and text recognised.
        /// </summary>
        public string ResultJsonString { get; private set; } = "";
        /// <summary>
        /// Plain text of all OCR result data concatenated.
        /// </summary>
        public string ResultTextString { get; private set; } = "";

        /// <summary>
        /// Pass a URL to the OCR API to extract text.
        /// </summary>
        /// <param name="imageURI"></param>
        public void OCRFile(Uri imageURI)
        {
            if (string.IsNullOrEmpty(imageURI.ToString()))
            {
                _log.Info("URL is NULL or empty");
                return;
            }

            _ImageURI = imageURI;
            ResultJsonString = "";
            ResultTextString = "";

            _log.Info($"Uploading URL: {_ImageURI.ToString()} to OCR API");
            GoogleAnnotate annotate = new GoogleAnnotate(_log);
            annotate.GetText( _ImageURI, languageType + "");

            if (false == string.IsNullOrEmpty(annotate.Error))
            {
                ResultTextString = annotate.Error;
                _log.Info($"Error: {ResultTextString} uploading to OCR API using image URL: {_ImageURI.ToString()}");
            }
            else
            {
                ResultJsonString = annotate.JsonResult;
                ResultTextString = annotate.TextResult;
                _log.Info($"Success: {ResultJsonString} uploading to OCR API using image URL: {_ImageURI.ToString()}");
            }
        }

        /// <summary>
        /// Pass a string path of image on local storage to the OCR API to extract text.
        /// </summary>
        /// <param name="imagePath"></param>
        public void OCRFile(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return;
            }
            _ImagePath = imagePath;

            _log.Info($"Uploading URL: {_ImagePath} to OCR API");
            GoogleAnnotate annotate = new GoogleAnnotate(_log);

            annotate.GetText(_ImagePath, languageType + "");

            if (false == string.IsNullOrEmpty(annotate.Error))
            {
                ResultTextString = annotate.Error;
                _log.Info($"Error: {ResultTextString} uploading to OCR API using image URL: {_ImagePath}");
            }
            else
            {
                ResultJsonString = annotate.JsonResult;
                ResultTextString = annotate.TextResult;
                _log.Info($"Success: {ResultJsonString} uploading to OCR API using image URL: {_ImagePath}");
            }
        }
    }
}
