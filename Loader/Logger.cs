using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loader
{
    public static class Logger
    {
        public static void Write(string _msg)
        {
            var logMsg = String.Format("{0:dd.MM.yy hh:mm:ss} > {1}", DateTime.Now, _msg);
            Trace.TraceInformation(logMsg);
            Trace.Flush();
        }
    }
}
