using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MS_AutoPatcher
{
    static class WebHelper
    {
        public static WebRequest BuildWebRequest(string url)
        {
            var protocol = url.Substring(0, url.IndexOf(':'));
            switch (protocol)
            {
                case "http": return HttpWebRequest.Create(url);
                case "https": return HttpWebRequest.Create(url);
                case "ftp": return FtpWebRequest.Create(url);
                default: throw new NotSupportedException("Protocol used in this url is not supported: " + protocol + " (" + url + ")");
            }
        }
    }
}
