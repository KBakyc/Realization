using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace CommonModule.Helpers
{
    public static class WorkFlowHelper
    {
        private static Logger appLogger;

        /// <summary>
        /// Останов приложения и запись протокола
        /// </summary>
        /// <param name="_mess"></param>
        public static void OnCrash(Exception _e)
        {
            OnCrash(_e, null);
        }
        
        public static void OnCrash(Exception _e, string _mess)
        {
            OnCrash(_e, _mess, false);
        }

        public static void OnCrash(Exception _e, string _mess, bool _silent)
        {
            string etype = _e.GetType().ToString();
            string message = String.IsNullOrEmpty(_mess) ? _e.Message : _mess + "\n" + _e.Message;
            if (_e.InnerException != null) message += "\n" + _e.InnerException.Message;
            string emess = String.Format("{0} : {1}\n{2}", etype, message, _e.StackTrace);
            if (!_silent)
                System.Windows.MessageBox.Show(emess, etype);
            WriteToLog(null, String.Format("{0} : {1}", etype, emess));
        }

        public static void WriteToLog(string _path, string _mess)
        {
            LogToFile(_path, _mess);
        }

        /// <summary>
        /// Если указанный файл больше указанного размера, то убирает с начала лишнюю часть.
        /// </summary>
        /// <param name="_fpath"></param>
        /// <param name="_size"></param>
        public static void TrimFileTo(string _fpath, long _size)
        {
            if (_size == 0 || !File.Exists(_fpath)) return;
            
            string newLogContent = null;
            using (var fs = new FileStream(_fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                if (fs.Length <= _size) return;
                fs.Seek(-_size, SeekOrigin.End);
                var sr = new StreamReader(fs, Encoding.GetEncoding(1251));
                sr.ReadLine();
                newLogContent = sr.ReadToEnd();
                fs.SetLength(0);
                var sw = new StreamWriter(fs, Encoding.GetEncoding(1251));
                sw.Write(newLogContent);
                sw.Close();
                sr.Close();
            }
        }

        public static void TrimLog(string _path)
        {
            Logger curLogger = GetLogger(_path);
            if (curLogger != null) 
                curLogger.Trim();
        }

        private static Logger GetLogger(string _path)
        { 
            Logger curLogger = null;
            if (String.IsNullOrEmpty(_path))
            {
                if (appLogger == null) appLogger = new Logger();
                curLogger = appLogger;
            }
            else
                curLogger = new Logger(_path);
            return curLogger;
        }

        private static void LogToFile(string _path, string _mess)
        {
            Logger curLogger = GetLogger(_path);

            if (curLogger != null)
                curLogger.Log(_mess);
        }

        //public static void DoEvents()
        //{
        //    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
        //                                          new Action(delegate { }));
        //}


        public const string CI_ENVIRONMENT = "Environment";
        public const string CI_CONTAINER = "ClientInfo";
        public const string CI_COMMSERVLOC = "CommService";
        public const string CI_EXECUTE = "Execute";
        public const string CI_MESSAGE = "Message";

        public static void CreateInfoAtStart()
        {
            var dbServ = CommonModule.CommonSettings.Repository;
            var usr = dbServ.UserToken;
            if (usr == 0) return;

            DataObjects.UserInfo curUser = dbServ.GetUserInfo(usr);
            var ci = curUser.ClientInfo == null || curUser.ClientInfo.Name != CI_CONTAINER ? new System.Xml.Linq.XElement(CI_CONTAINER) : curUser.ClientInfo;
            var env = ci.Element(CI_ENVIRONMENT);
            if (env == null)
            {
                env = new System.Xml.Linq.XElement(CI_ENVIRONMENT);
                ci.Add(env);
            }
            string dotNetVer = DotNetHelper.NetVersionDetector.GetMaxDotNetVersionInstalled();
            if (!String.IsNullOrWhiteSpace(dotNetVer))
                env.SetAttributeValue("MaxDotNetInstalled", dotNetVer);
            env.SetAttributeValue("OSVersion", Environment.OSVersion);
            env.SetAttributeValue("MachineName", Environment.MachineName);
            env.SetAttributeValue("StartTime", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            var stopAtt = env.Attribute("StopTime");
            if (stopAtt != null)
                stopAtt.Remove();            
            curUser.ClientInfo = ci;
            dbServ.UpdateUserInfo(curUser.Id, curUser);
        }

        public static void UpdateInfoOnExit()
        {
            var dbServ = CommonModule.CommonSettings.Repository;
            var usr = dbServ.UserToken;
            if (usr == 0) return;

            DataObjects.UserInfo curUser = dbServ.GetUserInfo(usr);
            var ci = curUser.ClientInfo == null || curUser.ClientInfo.Name != CI_CONTAINER ? new System.Xml.Linq.XElement(CI_CONTAINER) : curUser.ClientInfo;
            var env = ci.Element(CI_ENVIRONMENT);
            if (env == null)
            {
                env = new System.Xml.Linq.XElement(CI_ENVIRONMENT);
                ci.Add(env);
            }
            env.SetAttributeValue("StopTime", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            curUser.ClientInfo = ci;

            dbServ.UpdateUserInfo(curUser.Id, curUser);
        }

        public static void PerformMaintaince()
        {
            var dbServ = CommonModule.CommonSettings.Repository;
            var usr = dbServ.UserToken;
            if (usr == 0) return;
            DataObjects.UserInfo curUser = dbServ.GetUserInfo(usr);
            var clientInfo = curUser.ClientInfo;
            if (clientInfo == null) return;           
            var execElements = clientInfo.Name == CI_EXECUTE ? Enumerable.Repeat(clientInfo, 1) : clientInfo.Elements(CI_EXECUTE);
            foreach (var execEl in execElements.ToArray())
            {
                //var execEl = clientInfo.Name == CI_EXECUTE ? clientInfo : clientInfo.Element(CI_EXECUTE);
                //if (execEl == null) return;
                var command = execEl.Value.Trim();
                var roAtt = execEl.Attribute("RunOnce");
                var sysAtt = execEl.Attribute("IsSystem");
                bool isRunOnce = true;
                bool isDelAfter = false;
                bool isRestart = false;
                bool isSystem = false;
                bool isNoWin = true;
                if (sysAtt != null && !String.IsNullOrWhiteSpace(sysAtt.Value))
                    Boolean.TryParse(sysAtt.Value.Trim(), out isSystem);
                if (isSystem && String.IsNullOrWhiteSpace(command)) return;
                if (roAtt != null && !String.IsNullOrWhiteSpace(roAtt.Value))
                    Boolean.TryParse(roAtt.Value.Trim(), out isRunOnce);
                if (isRunOnce)
                {
                    if (!isSystem)
                    {
                        var daAtt = execEl.Attribute("DelAfter");
                        if (daAtt != null && !String.IsNullOrWhiteSpace(daAtt.Value))
                            Boolean.TryParse(daAtt.Value.Trim(), out isDelAfter);
                    }
                    if (execEl.Parent != null && execEl.Parent.Elements().Count() > 1)
                        execEl.Remove();
                    else
                        curUser.ClientInfo = null;

                    dbServ.UpdateUserInfo(curUser.Id, curUser);
                }
                var reAtt = execEl.Attribute("Restart");
                if (reAtt != null && !String.IsNullOrWhiteSpace(reAtt.Value))
                    Boolean.TryParse(reAtt.Value.Trim(), out isRestart);
                var nowinAtt = execEl.Attribute("NoWindow");
                if (nowinAtt != null && !String.IsNullOrWhiteSpace(nowinAtt.Value))
                    Boolean.TryParse(nowinAtt.Value.Trim(), out isNoWin);

                string url = null;
                if (!isSystem)
                {
                    var urlAtt = execEl.Attribute("Url");
                    if (urlAtt != null && !String.IsNullOrWhiteSpace(urlAtt.Value))
                        url = urlAtt.Value.Trim();
                }
                string cmdpath = String.IsNullOrWhiteSpace(command) ? null : Path.GetFullPath(command);
                if (!String.IsNullOrWhiteSpace(url))
                    DotNetHelper.WebFileLoader.UpdateFile(url, cmdpath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(url)));

                if (!String.IsNullOrWhiteSpace(cmdpath))
                {
                    if (isSystem || File.Exists(cmdpath))
                    {
                        RunMaintaince(cmdpath, isSystem, isNoWin);

                        if (isDelAfter && !isSystem)
                            System.Threading.Tasks.Task.Factory.StartNew(()=>DeleteFile(cmdpath));
                        if (isRestart)
                        {
                            var appPath = AppDomain.CurrentDomain.SetupInformation.ApplicationName;
                            clientInfo = curUser.ClientInfo;
                            if (clientInfo != null && clientInfo.Element(CI_EXECUTE) != execEl)
                                System.Diagnostics.Process.Start(appPath);
                            else
                                System.Diagnostics.Process.Start(appPath, "/restart");
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                            //Application.Current.Shutdown();
                        }
                    }
                }
            }            
        }

        private static void DeleteFile(string _cmdpath)
        {
            if (String.IsNullOrWhiteSpace(_cmdpath) || !File.Exists(_cmdpath)) return;
            int attempts = 0;
            bool isSuccess = false;
            while (!isSuccess && attempts++ < 10)
            {
                try
                {
                    File.Delete(_cmdpath);
                    isSuccess = true;
                }
                catch
                {
                    isSuccess = false;
                    System.Threading.Thread.Sleep(500);
                }
            }
            if (!isSuccess)
                WorkFlowHelper.WriteToLog(null, "Ошибка удаления файла: " + _cmdpath);
        }

        private static void RunMaintaince(string _fullPath, bool _issys, bool _nowin)
        {
            var cmd = Path.GetFileName(_fullPath);
            System.Diagnostics.ProcessStartInfo pi = new System.Diagnostics.ProcessStartInfo(cmd);
            pi.WorkingDirectory = Path.GetDirectoryName(_fullPath);
            pi.UseShellExecute = _issys;
            pi.CreateNoWindow = _nowin;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = pi;
            try
            {
                if (proc.Start())
                    proc.WaitForExit();
            }
            catch (Exception _e)
            {
                OnCrash(_e, String.Format("Maintaince: Не удалось запустить \"{0}\"", _fullPath), true);
            }
        }

    }
}
