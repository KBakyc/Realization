using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;
using System.Windows;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using Realization.ViewModels;
using CommonModule.Interfaces;
using System.Linq;
using System.Windows.Threading;
using Realization.Properties;

namespace Realization
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application //, IPartImportsSatisfiedNotification
    {
        public App()
        {
            DispatcherUnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Главное окно приложения
        /// </summary>
        [Import("MainWindow")]
        private Window mainview;

        /// <summary>
        /// Экспортируемые ресурсы модулей
        /// </summary>
        [ImportMany("ModuleView")]
        private ResourceDictionary[] views;

        private CompositionContainer container;

        [Export]
        public CompositionContainer Container
        {
            get
            {
                if (container == null)
                {
                    MakeContainer();
                    //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    //var catalog = new AggregateCatalog(
                    //                new DirectoryCatalog(path),
                    //                new AssemblyCatalog(Assembly.GetExecutingAssembly()));
                    //container = new CompositionContainer(catalog);
                }
                return container;
            }
        }

        private void MakeContainer()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var di = new DirectoryInfo(path);
            var dlls = di.GetFileSystemInfos("*.dll");
            AggregateCatalog agc = new AggregateCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            foreach (var fi in dlls)
            {
                try
                {
                    var ac = new AssemblyCatalog(Assembly.LoadFile(fi.FullName));
                    var parts = ac.Parts.ToArray(); // throws ReflectionTypeLoadException 
                    agc.Catalogs.Add(ac);
                }
                catch (ReflectionTypeLoadException e)
                {
                    if (notLoaded == null) notLoaded = new List<string>();
                    notLoaded.Add(fi.FullName);
                    string _mess = e.Message;
                    var loaderExceptionsString = String.Join("\n", e.LoaderExceptions.Select(ex => ex.GetType().ToString() + " : " + ex.Message).ToArray());
                    string _type = e.GetType().ToString();
                    string fullmess = String.Format("{0} : {1}{2}", _type, _mess, "\nLoader exceptions:\n" + loaderExceptionsString);
                    WorkFlowHelper.WriteToLog(null, fullmess);
                }
            }
            container = new CompositionContainer(agc);
        }

        private List<string> notLoaded;

        private Dispatcher uiDispatcher;

        [Export]
        public Dispatcher UiDispatcher
        {
            get
            {
                if (uiDispatcher == null)
                    uiDispatcher = Dispatcher.CurrentDispatcher;
                return uiDispatcher;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dbServ = CommonModule.CommonSettings.Repository;
            dbServ.IsSilent = true;
            
            WorkFlowHelper.WriteToLog(null, "ApplicationStartup");
            //dbServ.LogToFile(null, "ApplicationStartup");
            var uciLogMessage = "Creating client info...";
            try
            {
                WorkFlowHelper.CreateInfoAtStart();
                uciLogMessage += "Success";
            }
            catch (Exception ex)
            {
                uciLogMessage += "Fails" + Environment.NewLine + ex.Message;
            }
            finally
            {
                WorkFlowHelper.WriteToLog(null, uciLogMessage);
            }

            // Импортируем компоненты
            //MakeContainer();
            Container.ComposeParts(this);

            // загружаем тему
            Resources.MergedDictionaries.Clear();
            Uri urd = new Uri("Themes\\" + "Generic" + ".xaml", UriKind.RelativeOrAbsolute);
            ResourceDictionary rd = new ResourceDictionary();
            rd.Source = urd;
            Resources.MergedDictionaries.Add(rd);

            // загружаем экспортируемые ресурсы модулей
            foreach (ResourceDictionary r in views)
            {
                this.Resources.MergedDictionaries.Add(r);
            }

            // загружаем сохранённые данные
            CommonModule.CommonSettings.Persister.LoadData("Memory.xml");

            mainview.Show();
            if (notLoaded != null)
            {
                System.Windows.MessageBox.Show(String.Join("\n",notLoaded.ToArray()) + "\n\nПодробности смотрите в протоколе работы АРМа.", "Ошибка при загрузке следующих модулей", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                notLoaded = null;
            }
            dbServ.IsSilent = false;

            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            PerformWriteOnExit();
        }

        private object syncObj = new object();

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);            
            PerformWriteOnExit();
        }

        private void PerformWriteOnExit()
        {
            Microsoft.Win32.SystemEvents.SessionEnding -= SystemEvents_SessionEnding;
            lock (syncObj)
            {
                if (!isExitWrited)
                {                    
                    WriteOnExitAction();
                    isExitWrited = true;
                }
            }
        }

        private bool isExitWrited = false;
        private void WriteOnExitAction()
        {
            CommonModule.CommonSettings.Persister.SaveData("Memory.xml");
            var uciLogMessage = "Updating client info...";
            try
            {
                WorkFlowHelper.UpdateInfoOnExit();
                uciLogMessage += "Success";
            }
            catch
            {
                uciLogMessage += "Fails";
            }
            finally
            {
                WorkFlowHelper.WriteToLog(null, uciLogMessage);
            }

            var shell = Container.GetExportedValue<IShellModel>() as MainViewModel;
            if (shell != null)
            {
                if (shell.Modules != null && shell.Modules.Length > 0)
                    Array.ForEach(shell.Modules.OfType<IDisposable>().ToArray(), d =>
                    {
                        d.Dispose();
                    });
                shell.CloseServices();
            }

            WorkFlowHelper.WriteToLog(null, "ApplicationExit");
            WorkFlowHelper.TrimLog(null);
            //WorkFlowHelper.TrimFileTo(CommonModule.CommonSettings.LogPath, CommonModule.CommonSettings.MaxLogLength);

            //CommonModule.CommonSettings.Repository.LogToFile(null, "ApplicationExit");
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WorkFlowHelper.WriteToLog(null, "UnhandledException!!!");
            string _mess = e.Exception.Message;
            var innerException = e.Exception.InnerException;
            string _type = e.Exception.GetType().ToString();
            //WorkFlowHelper.OnCrash(e.Exception);
            var stringToLog = String.Format("{0} : {1}{2}\n", _type, _mess, innerException == null ? "" : "\n\nInner exception: " + innerException.Message);
            WorkFlowHelper.WriteToLog(null, stringToLog + Environment.NewLine + e.Exception.StackTrace);
            var shell = Container.GetExportedValue<IShellModel>();
            if (shell != null && shell.WorkSpace != null)
            {
                shell.WorkSpace.Services.ShowMsg("КРИТИЧЕСКАЯ ОШИБКА!", stringToLog + "\nСОДЕРЖИМОЕ МОДУЛЯ БУДЕТ ОЧИЩЕНО!", true);
                
                var module = shell.WorkSpace as BasicModuleViewModel;
                if (module != null && module.StopModule != null)
                    module.StopModule.Execute(null);                
            }
            e.Handled = true;
        }
    }

}
