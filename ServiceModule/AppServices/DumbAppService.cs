using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using System.ComponentModel.Composition;

namespace ServiceModule.AppServices
{
    //[Export(typeof(IAppService))]
    public class DumbAppService : IAppService
    {
        [Import("MainWindow")]
        private System.Windows.Window mainView;

        [Import]
        private IShellModel shellModel;

        private bool isStayInMemory = false;
        public bool IsStayInMemory
        {
            get { return isStayInMemory; }
        }

        public void Start()
        {
            shellModel.UpdateUi(() =>
                {
                    System.Windows.MessageBox.Show(mainView, "DumbAppService started", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    //shellModel.Modules[0].StartModule.Execute(null);
                }, true, false);
        }

        public void Stop()
        {
            
        }
    }
}
