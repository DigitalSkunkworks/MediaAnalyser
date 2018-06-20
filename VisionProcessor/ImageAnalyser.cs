using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

namespace VisionProcessor
{
    public abstract class ImageAnalyser
    {
        protected internal enum status { PROCESS_SUCCESS=0, PROCESS_FAIL, URL_NOT_FOUND };

        // attributes
        protected internal string _uid { get; set; }  = "";
        protected internal string _url { get; set; }  = "";
        protected internal string _name { get; set; } = "";
        protected internal string _description { get; set; }  = "";
        protected internal string _hash {get; set; }  = "";
        protected internal string _jsonData { get; set; } = "";

        // methods

        public ImageAnalyser( string uid, string url, string hash, string name="", string description="" )
        {
            _uid = uid;
            _url = url;
            _hash = hash;
            _name = name;
            _description = description;
        }

        abstract public void AnalyseFile(string resourceFilePath);
        abstract public void AnalyseURL();
    }
}
