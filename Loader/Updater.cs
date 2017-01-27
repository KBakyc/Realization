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
            if (String.IsNullOrEmpty(updatesPath)) return null;
            Resource[] res = null;
            try
            {
                res = WebFileLoader.GetDirectoryContents(updatesPath, true)
                    .GetValueList().OfType<Resource>()
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw;
            }
            return res;
        }

        private void UpdateAllIfNeeded()
        {
            if (String.IsNullOrEmpty(updatesPath)
                || newResources == null || newResources.Length == 0) return;

            foreach (var nr in newResources)
            {
                try
                {
                    nr.FilePath = CalculateNewResourceLocalPath(nr.Url);
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
            WebFileLoader.UpdateFile(nr.Url, nr.FilePath, reqtranslate);
        }

        private void UpdateDirIfNeeded(Resource nr)
        {
            if (!Directory.Exists(nr.FilePath))
                Directory.CreateDirectory(nr.FilePath);
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
            newResources = GetNewResources();
            UpdateAllIfNeeded();
        }

        public string UpdaterError { get; set; }
    }
}
