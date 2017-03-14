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
                var appExeName = Loader.Properties.Settings.Default.AppExeName;
                var appRelFolder = Loader.Properties.Settings.Default.AppFolderName;
                var curAppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                appExeDir = Path.Combine(curAppPath, appRelFolder);
                appExePath = Path.Combine(appExeDir, appExeName);
                appUpdatesPath = Loader.Properties.Settings.Default.AppUpdatesPath;
                isStartAfterUpdate = Loader.Properties.Settings.Default.StartAfterUpdate
                                     && !String.IsNullOrEmpty(appExeName);
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
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (String.IsNullOrEmpty(updater.UpdaterError))
            {
                lWin.Close();
            }
            else
                lWin.DataContext = new { UpdateResult = updater.UpdaterError };
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
                    HandleTerminalError(ex);
                }
        }

        private void HandleTerminalError(Exception ex)  
        {
            MessageBox.Show(ex.Message + Environment.NewLine
                          + ex.InnerException != null ? ex.InnerException.Message + Environment.NewLine : ""
                          + ex.StackTrace);
            //Environment.Exit(0);
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
