using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loader
{
    [Serializable]
    public class Resource
    {
        
        private string _Name;
        public string Name
        { get { return _Name; } set { _Name = value; } }

        private string _Url;
        public string Url
        { get { return _Url; } set { _Url = value; } }

        private string _FilePath;
        public string FilePath
        { get { return _FilePath; } set { _FilePath = value; } }

        private bool _IsFolder;
        public bool IsFolder
        { get { return _IsFolder; } set { _IsFolder = value; } }

        private DateTime _LastModified;
        public DateTime LastModified
        { get { return _LastModified; } set { _LastModified = value; } }

        private bool _AddedAtRuntime;
        public bool AddedAtRuntime
        { get { return _AddedAtRuntime; } set { _AddedAtRuntime = value; } }

    }
}
