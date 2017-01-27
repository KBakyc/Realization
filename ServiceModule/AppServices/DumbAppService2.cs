//#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using System.ComponentModel.Composition;

namespace ServiceModule.AppServices
{
    [Export(typeof(IAppService))]
    //[PartCreationPolicy(CreationPolicy.NonShared)]
    public class DumbAppService2 : IAppService
    {
        //private System.Windows.Threading.DispatcherTimer timer;
        private System.Timers.Timer timer;
        
        //[Import("MainWindow")]
        //private System.Windows.Window mainView;

        [Import]
        private IShellModel shellModel;

        private bool isStayInMemory = false;
        public bool IsStayInMemory
        {
            get { return isStayInMemory; }
        }

        public void Stop()
        {
            StopTimer();
        }

        public void Start()
        {
            //shellModel.UpdateUi(() =>
            //    {
            //        System.Windows.MessageBox.Show(mainView, "DumbAppService2 started", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            //    }, true, true);
            //var disp = shellModel.Container.GetExportedValue<System.Windows.Threading.Dispatcher>();
            //timer = new System.Windows.Threading.DispatcherTimer( System.Windows.Threading.DispatcherPriority.Background, disp);
            timer = new System.Timers.Timer(3000);
            //timer.Interval = new TimeSpan(0, 0, 3);
            //timer.Tick += timer_Tick;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        private object lockobj = new object();

        private void StopTimer()
        {
            lock (lockobj)
            {
                if (timer != null && timer.Enabled)
                {
                    timer.Stop();
                    timer.Elapsed -= timer_Elapsed;
                    timer = null;
                }
            }
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ticks > 2)
                StopTimer();
            else
            {
                ticks++;
                ServiceActions();
            }
        }

        private int ticks = 0;

        //void timer_Tick(object sender, EventArgs e)
        //{
        //    i
        //}

        private void ServiceActions()
        {
            System.Diagnostics.Debug.WriteLine("DumbAppService2 started " + ticks.ToString());
            //shellModel.UpdateUi(() =>
            //{
            //    System.Diagnostics.Debug.WriteLine("DumbAppService2 started " + ticks.ToString());
            //}, true, true);            
        }

        public DumbAppService2()
        {
            System.Diagnostics.Debug.WriteLine("DumbAppService2 Created.");
        }

        //~DumbAppService2()
        //{
        //    System.Diagnostics.Debug.WriteLine("DumbAppService2 destroyed.");
        //}
    }
}
