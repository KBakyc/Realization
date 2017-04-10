using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace Loader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string args;
        private string appExePath;
        private string appExeDir;
        private string appUpdatesPath;
        private bool isReqTranslate;
        private bool isStartAfterUpdate;
        private BackgroundWorker bgWorker;
        private Updater updater;
        private LoaderWindow lWin;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                args = String.Join(" ", e.Args);

                Logger.Write("Loader started.");
                
                var appExeName = Loader.Properties.Settings.Default.AppExeName;
                Logger.Write("appExeName = " + appExeName);

                var appRelFolder = Loader.Properties.Settings.Default.AppFolderName;
                Logger.Write("appRelFolder = " + appRelFolder);

                var curAppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Logger.Write("curAppPath = " + curAppPath);

                appExeDir = Path.Combine(curAppPath, appRelFolder);
                Logger.Write("appExeDir = " + appExeDir);

                appExePath = Path.Combine(appExeDir, appExeName);
                Logger.Write("appExePath = " + appExePath);

                appUpdatesPath = Loader.Properties.Settings.Default.AppUpdatesPath;
                Logger.Write("appUpdatesPath = " + appUpdatesPath);

                isStartAfterUpdate = Loader.Properties.Settings.Default.StartAfterUpdate
                                     && !String.IsNullOrEmpty(appExeName);
                Logger.Write("isStartAfterUpdate = " + isStartAfterUpdate.ToString());

                isReqTranslate = Loader.Properties.Settings.Default.ReqTranslate;

                lWin = new LoaderWindow();
                lWin.Closed += new EventHandler(lWin_Closed);
                lWin.Show();

                updater = new Updater(appExeDir, appUpdatesPath, isReqTranslate);
                bgWorker = new BackgroundWorker();
                bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
                bgWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                HandleTerminalError(ex);
            }
        }

        void lWin_Closed(object sender, EventArgs e)
        {
            if (isStartAfterUpdate)
                StartApplication();
            Shutdown();
            Logger.Write("Loader finished.");
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (String.IsNullOrEmpty(updater.UpdaterError))
            {
                lWin.Close();
            }
            else
            {
                lWin.DataContext = new { UpdateResult = updater.UpdaterError };
                Logger.Write("UpdaterError = " + updater.UpdaterError);
            }
        }

        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (updater != null)
                try
                {
                    updater.Update();
                }
                catch (Exception ex)
                {
                    Logger.Write("Ошибка в bgWorker_DoWork!");
                    HandleTerminalError(ex);
                }
        }

        private void HandleTerminalError(Exception ex)  
        {
            var errMsg = ex.Message + Environment.NewLine
                          + ex.InnerException != null ? ex.InnerException.Message + Environment.NewLine : ""
                          + ex.StackTrace;
            Logger.Write(errMsg);
            MessageBox.Show(errMsg);
        }

        private void StartApplication()
        {
            try
            {
                ProcessStartInfo p = new ProcessStartInfo(appExePath);
                p.WorkingDirectory = appExeDir;
                p.Arguments = args;
                Process.Start(p);
            }
            catch (Exception e)
            {
                HandleTerminalError(e);
            }
        }


    }
}
