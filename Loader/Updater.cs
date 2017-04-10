using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Loader
{
    public class Updater
    {
        private string localDir;
        private string updatesPath;
        private bool reqtranslate;
        private Resource[] curResources;
        private Resource[] newResources;

        public Updater(string _localPath, string _remotePath, bool _reqtranslate)
        {
            if (String.IsNullOrEmpty(_localPath) || String.IsNullOrEmpty(_remotePath))
                throw new ArgumentNullException("_localPath or _remotePath");
            localDir = _localPath;
            updatesPath = _remotePath;
            reqtranslate = _reqtranslate;
        }

        public Updater(string _localPath, string _remotePath)
            :this(_localPath, _remotePath, false)
        {}

        private Resource[] GetCurrentResources()
        {
            Resource[] res = null;
            try
            {
                res = EnumerateCurrentResources(localDir).ToArray();
            }
            catch
            {

            }

            return res;
        }
        private IEnumerable<Resource> EnumerateCurrentResources(string _path)
        {
            foreach (var f in Directory.GetFiles(_path))
            {
                FileInfo fi = new FileInfo(f);
                Resource fr = new Resource()
                {
                    FilePath = f,
                    IsFolder = false,
                    LastModified = fi.LastWriteTime,
                    Name = f
                };
                yield return fr;
            }
            foreach (var d in Directory.GetDirectories(_path))
            {
                var recursion = EnumerateCurrentResources(d);
                foreach (var fd in recursion)
                    yield return fd;
            }
        }

        private Resource[] GetNewResources()
        {
            Logger.Write("GetNewResources started...");
            if (String.IsNullOrEmpty(updatesPath)) return null;
            Resource[] res = null;
            try
            {
                Logger.Write("GetNewResources try WebFileLoader.GetDirectoryContents from " + updatesPath);
                res = WebFileLoader.GetDirectoryContents(updatesPath, true)
                    .GetValueList().OfType<Resource>()
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logger.Write("Error in GetNewResources!");
                Logger.Write(ex.ToString() + Environment.NewLine + ex.Message);
                throw;
            }

            Logger.Write("GetNewResources finished.");
            return res;
        }

        private void UpdateAllIfNeeded()
        {
            if (String.IsNullOrEmpty(updatesPath)
                || newResources == null || newResources.Length == 0) return;

            Logger.Write("Checking and updating resources..." + newResources.Length.ToString());
            foreach (var nr in newResources)
            {
                Logger.Write("Resource = " + nr.Url);
                try
                {
                    nr.FilePath = CalculateNewResourceLocalPath(nr.Url);
                    Logger.Write("ResourceLocalPath = " + nr.FilePath);
                    UpdateResourceIfNeeded(nr);
                }
                catch (Exception ex)
                {
                    string errMsg = nr.Url + Environment.NewLine
                                  + ex.Message + Environment.NewLine;
                    if (ex.InnerException != null)
                        errMsg += ex.InnerException.Message;
                    UpdaterError += errMsg + Environment.NewLine;
                }
            }

        }

        private void UpdateResourceIfNeeded(Resource nr)
        {
            if (nr.IsFolder)
                UpdateDirIfNeeded(nr);
            else
                UpdateFileIfNeeded(nr);
        }

        private void UpdateFileIfNeeded(Resource nr)
        {
            Logger.Write(String.Format("Resource is a file. Trying to update from {0} to {1}", nr.Url, nr.FilePath));
            WebFileLoader.UpdateFile(nr.Url, nr.FilePath, reqtranslate);
        }

        private void UpdateDirIfNeeded(Resource nr)
        {
            Logger.Write("Resource is a folder.");
            if (!Directory.Exists(nr.FilePath))
            {
                Logger.Write(nr.FilePath + " does not exists. Creating...");
                Directory.CreateDirectory(nr.FilePath);
            }
        }

        private string CalculateNewResourceLocalPath(string _rpath)
        {
            string res = localDir;
            if (!String.IsNullOrEmpty(_rpath))
            {
                string relPath = _rpath.Replace(updatesPath, String.Empty).Replace(@"/", @"\");
                if (relPath.StartsWith(@"\"))
                    relPath = relPath.Remove(0, 1);
                res = Path.Combine(localDir, relPath);
            }
            return res;
        }

        public void Update()
        {
            curResources = GetCurrentResources();
            var curResStr = String.Join("\n", curResources.Select(r => String.Format("{0} | {1:dd.MM.yy hh:mm:ss}", r.FilePath, r.LastModified)).ToArray());
            Logger.Write("curResources = " + curResStr);

            newResources = GetNewResources();
            var newResStr = String.Join("\n", newResources.Select(r => String.Format("{0} | {1:dd.MM.yy hh:mm:ss}", r.Url, r.LastModified)).ToArray());
            Logger.Write("newResources = " + newResStr);

            UpdateAllIfNeeded();
        }

        public string UpdaterError { get; set; }
    }
}
