using System.Collections.Generic;
using CommonModule.Helpers;
using DAL;
using CommonModule.Interfaces;
using DataObjects.Interfaces;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using DataObjects;
using System.IO;

namespace CommonModule
{
    public static class CommonSettings
    {
        public static System.Configuration.ApplicationSettingsBase Settings
        {
            get { return Properties.Settings.Default; }
        }            

        private static IPersister persister;

        public static IDbService Repository
        {
            get { return LinqDbService.Instance; }
        }

        public static IPersister Persister
        {
            get
            {
                if (persister == null)
                    persister = new Remember();
                return persister;
            }
        }

        public static string ConnectionString { get { return Repository.ConnectionString; } }

        public static bool IsDALReadOnly { get { return DALSettings.IsReadOnly; } }

        /// <summary>
        /// Наш код контр-агента в справочнике
        /// </summary>
        public static int OurKontrAgentKod
        {
            get { return Repository.OurKgr; }
        }

        public static bool IsShowCommandLabels
        {
            get { return Properties.Settings.Default.IsShowCommandLabels; }
            set
            {
                if (Properties.Settings.Default.IsShowCommandLabels != value)
                {
                    WriteAppSetting("CommonModule.Properties.Settings", "IsShowCommandLabels", value.ToString());
                    Properties.Settings.Default.Reload();
                }
            }
        }

        private static Dictionary<int, int[]> myPoupsWithKodfs;
        public static Dictionary<int, int[]> MyPoupsWithKodfs
        {
            get
            {
                if (myPoupsWithKodfs == null)
                    GetMyPoupsWithKodfs();
                return myPoupsWithKodfs;
            }
            set
            {
                myPoupsWithKodfs = value;
                SaveMyPoupsWithKodfs();
            }
        }

        public static int[] GetMyPoups()
        {
            int[] res;
            if (MyPoupsWithKodfs.ContainsKey(0))
                res = Repository.Poups.Keys.ToArray();
            else
                res = MyPoupsWithKodfs.Keys.ToArray();
            return res;
        }

        public static int[] GetMyKodfs(int _poup)
        {
            int[] res = null;
            if (!MyPoupsWithKodfs.ContainsKey(_poup) && _poup != 0)
                _poup = 0;
            if (MyPoupsWithKodfs.ContainsKey(_poup))
                res = MyPoupsWithKodfs[_poup];
            return res;
        }

        private static void SaveMyPoupsWithKodfs()
        {
            Repository.SaveNaprSettings(myPoupsWithKodfs);
        }

        private static void GetMyPoupsWithKodfs()
        {
            myPoupsWithKodfs = Repository.LoadNaprSettings();
            if (myPoupsWithKodfs == null || myPoupsWithKodfs.Count == 0)
                myPoupsWithKodfs = new Dictionary<int, int[]> { { 0, new int[] { 0 } } };
        }

        private static Dictionary<int, ApplyFeature> myPoupsSignModes;
        public static Dictionary<int, ApplyFeature> MyPoupsSignModes
        {
            get
            {
                if (myPoupsSignModes == null)
                    GetMyPoupsSignModes();
                return myPoupsSignModes;
            }
            set
            {
                myPoupsSignModes = value;
                SaveMyPoupsSignModes();
            }
        }

        private static void GetMyPoupsSignModes()
        {
            string settings = Properties.Settings.Default.NeedSignsForPoupModes;
            if (String.IsNullOrEmpty(settings))
            {
                myPoupsSignModes = new Dictionary<int, ApplyFeature>();
                return;
            }

            try
            {
                myPoupsSignModes = settings.Split(';').Select(s => s.Trim().Split(':'))
                                               .ToDictionary(
                                                    sa => int.Parse(sa[0]),
                                                    sa => (ApplyFeature)Enum.Parse(typeof(ApplyFeature), sa[1], true));
            }
            catch
            {
                myPoupsSignModes = new Dictionary<int,ApplyFeature>();
            }
        }

        private static void SaveMyPoupsSignModes()
        {
            MyPoupsSignModesString = GenerateMyPoupsSignModesString();
        }

        private static string GenerateMyPoupsSignModesString()
        {
            string res = String.Join(";", 
                                     myPoupsSignModes.Select(kv =>
                                                 String.Join(":", new string[] { kv.Key.ToString(), kv.Value.ToString() })).ToArray()
                                    );
            return res;
        }

        private static string MyPoupsSignModesString
        {
            get { return Properties.Settings.Default.NeedSignsForPoupModes; }
            set
            {
                if (Properties.Settings.Default.NeedSignsForPoupModes != value)
                {
                    WriteAppSetting("CommonModule.Properties.Settings", "NeedSignsForPoupModes", value);
                    Properties.Settings.Default.Reload();
                }
            }
        }
        
        const string APP_SETTINGS_GROUP_NAME = "applicationSettings";

        public static bool WriteAppSetting(string _sect, string _sett, string _value)
        {
            if (String.IsNullOrEmpty(_sect) || String.IsNullOrEmpty(_sett)) return false;

            bool res = true;

            try
            {
                System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                if (config != null)
                {
                    var sgroup = config.SectionGroups[APP_SETTINGS_GROUP_NAME];
                    if (sgroup != null)
                    {
                        var section = sgroup.Sections[_sect] as System.Configuration.ClientSettingsSection;
                        if (section != null)
                        {
                            var sett = section.Settings.Get(_sett) as System.Configuration.SettingElement;
                            if (sett != null)
                            {
                                sett.Value.ValueXml.InnerText = _value;
                                section.Settings.Remove(sett);
                                section.Settings.Add(sett);
                                config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                                //Properties.Settings.Default.Reload();
                            }
                        }
                    }
                }
            }
            catch 
            {
                res = false;
            }

            return res;
        }

        public static ApplyFeature GetNeedSignsModeForPoup(int _poup)
        {
            ApplyFeature needSigns = ApplyFeature.Yes;
            if (MyPoupsSignModes.ContainsKey(_poup))
                needSigns = myPoupsSignModes[_poup];
            return needSigns;
        }

        public static string ScreenshotsPath
        {
            get 
            { 
                String ssPath = Properties.Settings.Default.ScreenshotsPath;
                return String.IsNullOrEmpty(ssPath) ? "screenshot.png" : ssPath;
            }
        }

        public static string LogPath
        {
            get
            {
                var lPath = Properties.Settings.Default.LogPath;
                if (String.IsNullOrEmpty(lPath))
                    lPath = System.Reflection.Assembly.GetExecutingAssembly().FullName + ".log";
                if (!Path.IsPathRooted(lPath))
                    lPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,lPath);
                return lPath;
            }
        }

        public static string OurKodVal { get { return "RB"; } }

        public static int MaxLogLength
        {
            get
            {
                return Properties.Settings.Default.MaxLogLength;
            }
        }
    }
}
