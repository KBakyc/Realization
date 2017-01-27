using System.ComponentModel.Composition;
using System.Windows;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Realization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export("MainWindow",typeof(Window))]
    public partial class MainWindow : Window
    {
        [Import]
        private IShellModel shellModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = shellModel;
            var vm = shellModel as BasicViewModel;
            if (vm != null) Title = vm.Title;

            Task.Factory.StartNew(shellModel.CheckConnection)
                .ContinueWith(t => shellModel.ReadMessages());

            if (vm != null)
                vm.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ShellModel_PropertyChanged);
        }


        void ShellModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WorkSpace")
            {
                var newWS = shellModel.WorkSpace;
                IModule oldWS = null;
                ContentPresenter oldContent = null;
                foreach(var ch in this.wsPlaceholder.Children.OfType<ContentPresenter>())
                {
                    if (ch.IsVisible)
                    {
                        oldContent = ch;
                        oldWS = ch.Content as IModule;
                        if (oldWS.IsContentLoaded)
                            ch.Visibility = System.Windows.Visibility.Hidden;
                        else
                            wsPlaceholder.Children.Remove(ch);
                        break;
                    }
                }
                
                if (newWS != null)
                {
                    bool alreadyExists = false;
                    foreach (var ch in this.wsPlaceholder.Children.OfType<ContentPresenter>())
                    {
                        var ws = ch.Content as IModule;
                        if (newWS == ws)
                        {
                            ch.Visibility = System.Windows.Visibility.Visible;
                            alreadyExists = true;
                            break;
                        }
                    }
                    if (!alreadyExists)
                    {
                        var newCP = new ContentPresenter() { Content = newWS, Focusable = false };
                        wsPlaceholder.Children.Add(newCP);
                    }
                }
            }
        }

        private void ExitApp()
        {
            tbIcon.Dispose();
            var loadedModules = shellModel.Modules.Where(m => m.IsContentLoaded).ToArray();
            Array.ForEach(loadedModules, m => m.StopModule.Execute(null));
            this.Dispatcher.Invoke(new Action(delegate 
                { 
                    Application.Current.Shutdown();
                }), System.Windows.Threading.DispatcherPriority.Background);            
        }

        //private void Window_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    ExitApp();            
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var loadedModule = shellModel.Modules.FirstOrDefault(m => m.IsContentLoaded);
            if (loadedModule != null)
            {
                e.Cancel = true;
                shellModel.Exit(null, "Обнаружены незакрытые модули.\nЗакрытие приложения может привести к потере несохранённых операций.",
                                o => ExitApp(),
                                o =>
                                {
                                    if (shellModel.WorkSpace == null || !shellModel.WorkSpace.IsContentLoaded)
                                        shellModel.LoadModule(loadedModule);
                                }
                                );
            }    
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    tbIcon.TrayPopupResolved.IsOpen = false;
        //}

        private void CloseCommandHandler(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            ExitApp();  
        }
    }
}
