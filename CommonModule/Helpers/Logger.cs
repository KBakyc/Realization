using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace CommonModule.Helpers
{
    public class Logger
    {
        private string fpath;
        private static Object lockObject = new Object();

        public Logger()
        {
            SetDefaultPath();
        }

        public Logger(string _fpath)
        {
            if (!String.IsNullOrEmpty(_fpath))
            {
                fpath = _fpath.Trim();
                if (!Path.IsPathRooted(fpath))
                    fpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fpath);
            }
            else
                SetDefaultPath();
        }

        private void SetDefaultPath()
        {
            fpath = CommonModule.CommonSettings.LogPath;
            if (String.IsNullOrEmpty(Path.GetFileName(fpath)))
                fpath = System.Reflection.Assembly.GetExecutingAssembly().FullName + ".log";
            if (!Path.IsPathRooted(fpath))
                fpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fpath);
        }

        public void Log(string _mess)
        {
            System.Threading.Tasks.Task.Factory.StartNew(()=>Log(fpath, _mess));
        }

        public void Log(string _fpath, string _mess)
        {
            if (String.IsNullOrEmpty(_fpath) || String.IsNullOrEmpty(_mess)) return;
            lock (lockObject)
            {
                using (var sw = new StreamWriter(fpath, true, Encoding.GetEncoding(1251)))
                {
                    string output = String.Format("[{0:dd/MM/yyyy HH:mm:ss}]- {1}", DateTime.Now, _mess);
                    sw.WriteLine(output);
                }
            }
        }        

        public void Trim()
        {
            lock (lockObject)
            {
                WorkFlowHelper.TrimFileTo(fpath, CommonModule.CommonSettings.MaxLogLength);
            }
        }
    }
}
