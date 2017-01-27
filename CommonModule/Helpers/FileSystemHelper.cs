using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Permissions;
using System.Security;

namespace CommonModule.Helpers
{
    public static class FileSystemHelper
    {
        public static bool HasDirectoryAccess(string _dir)
        {
            bool res = Directory.Exists(_dir);
            //if (res)
            //{
            //    //var perm = new FileIOPermission(FileIOPermissionAccess.AllAccess, _dir);
            //    try
            //    {
            //        var acl = Directory.GetAccessControl(_dir);
            //        SecurityIdentifier 
            //        acl.GetAccessRules(true, true, )
            //        //string fp = Path.Combine(_dir, "testaccess.file");
            //        //using (var fs = new FileStream(fp, FileMode.Create, FileAccess.ReadWrite))
            //        //{
            //        //    res = fs.CanWrite;
            //        //    fs.Close();
            //        //}
            //    }
            //    catch
            //    {
            //        res = false;
            //    }
            //}
            return res;
        }

        public static string FindNextFileName(String _path)
        {
            var dir = Path.GetDirectoryName(_path);
            var name = Path.GetFileNameWithoutExtension(_path);
            var ext = Path.GetExtension(_path);

            if (String.IsNullOrEmpty(dir))
            {
                dir = AppDomain.CurrentDomain.BaseDirectory;
            }

            int maxDigit = 0;
            var files = Directory.GetFiles(dir, name + "*" + ext);
            if (files.Length > 0)
            {
                var numEndings = files.Select(f => Path.GetFileNameWithoutExtension(f).Substring(name.Length))
                                   .Where(e => !String.IsNullOrEmpty(e) && e.All(c => Char.IsDigit(c)));
                if (numEndings.Any())
                    maxDigit = numEndings.Max(e => int.Parse(e));
            }
            
            if (!dir.EndsWith(@"\")) dir += @"\";

            return dir + name + (maxDigit + 1).ToString() + ext;
        }
    }
}
