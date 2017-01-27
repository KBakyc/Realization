using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace DotNetHelper
{
    public static class NetVersionDetector
    {
        public static string GetMaxDotNetVersionInstalled()
        {
            string res = null;
            try
            {
                var vers = GetDotNetVersions();
                res = vers.Keys.Max();
            }
            catch
            {
                res = "Error detecting";
            }
            return res;
        }

        public static Dictionary<string, string> GetDotNetVersions()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            RegistryKey NDPKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP", false);
            foreach (string subkey in NDPKey.GetSubKeyNames().Where(k => k.StartsWith("v")))
            {
                if (!GetDotNetVersion(NDPKey.OpenSubKey(subkey), res, subkey))
                {
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Client"), res, subkey + " Client");
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Full"), res, subkey + " Full");
                }
            }
            return res;
        }

        private static bool GetDotNetVersion(RegistryKey _parentKey, Dictionary<string, string> _versions, string _verName)
        {
            if (_parentKey == null || _versions == null || String.IsNullOrEmpty(_verName)) return false;

            string installed = Convert.ToString(_parentKey.GetValue("Install"));
            if (installed == "") return false;
            string version = Convert.ToString(_parentKey.GetValue("Version"));
            string sp = Convert.ToString(_parentKey.GetValue("SP"));

            string versionName = String.IsNullOrEmpty(sp) ? _verName : _verName + " SP" + sp;
            _versions[versionName] = String.IsNullOrEmpty(version) ? _verName.Substring(1) : version;

            return true;
        }
        

    }   
}
