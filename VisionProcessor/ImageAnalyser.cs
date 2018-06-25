using System;

namespace VisionProcessor
{
    /// <summary>
    /// Class ImageAnalyser
    /// Base class to contain common data items seen in all cloud platform APIs.
    /// </summary>
    public abstract class ImageAnalyser
    {
        /// <summary>
        /// Indicates the overall success state of processing an image.
        /// </summary>
        protected internal enum status { PROCESS_SUCCESS=0, PROCESS_FAIL, URL_NOT_FOUND };

        // attributes
        /// <summary>
        /// _uid
        /// the unique identifier applied to this image
        /// </summary>
        protected internal string           _uid { get; set; }          = "";

        /// <summary>
        /// _url
        /// The unique URI to retrieve this image
        /// </summary>
        protected internal string           _url { get; set; }          = "";

        /// <summary>
        /// _hash
        /// The hash value for this image
        /// </summary>
        protected internal string           _hash {get; set; }          = "";

        /// <summary>
        /// _BLOBDataSubmitted
        /// The date and time a BLOB was, put on to the queu.
        /// </summary>
        protected internal DateTimeOffset?  _BLOBDateSubmitted;

        /// <summary>
        /// _APIDateProcessed
        /// Date and time the BLOB image was retrieved from the queue for processing.
        /// </summary>
        protected internal DateTimeOffset?  _APIDateProcessed;

        /// <summary>
        /// _name
        /// the name of the BLOB image.
        /// </summary>
        protected internal string           _name { get; set; }         = "";

        /// <summary>
        /// _description
        /// The description given toi the BLOB image.
        /// </summary>
        protected internal string           _description { get; set; }  = "";

        /// <summary>
        /// _jsonData
        /// The JSON that is specific to tyhis image from one of the processes of image analysis.
        /// </summary>
        protected internal string           _jsonData { get; set; }     = "";

        // methods

        public ImageAnalyser( string uid, string url, string hash, DateTimeOffset? dateSubmitted, DateTimeOffset? dateProcessed, string name="", string description="" )
        {
            _uid                = uid;
            _url                = url;
            _hash               = hash;
            _BLOBDateSubmitted  = dateSubmitted;
            _APIDateProcessed   = dateProcessed;
            _name               = name;
            _description        = description;
        }

        /// <summary>
        /// AnalyseFile
        /// Method to be used from a command line invocation 
        /// </summary>
        /// <param name="resourceFilePath"></param>
        abstract public void AnalyseFile(string resourceFilePath);

        /// <summary>
        /// AnalyseURL
        /// Method to analyse a URI from the command line
        /// </summary>
        abstract public void AnalyseURL();
    }
}
