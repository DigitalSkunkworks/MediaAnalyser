using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoProcessor
{
    public abstract class VideoAnalyser
    {
        protected internal enum status { PROCESS_SUCCESS=0, PROCESS_FAIL, URL_NOT_FOUND };

        // attributes
        protected internal string _url { get; set; }  = "";
        protected internal string _name { get; set; } = "";
        protected internal string _description { get; set; }  = "";
        protected internal string _hash {get; set; }  = "";
        protected internal string _jsonData { get; set; } = "";

        // methods
        public VideoAnalyser( string url, string name="", string description="", string hash="" )
        {
            _url = url;
            _name = name;
            _description = description;
            _url = url;
        }

        /*abstract*/
//        public virtual void AnalyseVideo(string videoURL = "") { }
    }
}
