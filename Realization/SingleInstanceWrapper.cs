using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Realization
{
    public class SingleInstanceApplicationWrapper :  Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    {
        public SingleInstanceApplicationWrapper()
        {
            // Enable single-instance mode. 
            this.IsSingleInstance = true;
        }
        
        // Create the WPF application class. 
        private App app;
        protected override bool OnStartup(
          Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            SplashScreen splashScreen = new SplashScreen("Resources/Splash_real.png");

            // Show the splash screen. 
            // The true parameter sets the splashScreen to fade away automatically 
            // after the first window appears. 
            splashScreen.Show(true);
            app = new App() { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Run();
            return false;
        }

        // Direct multiple instances. 
        protected override void OnStartupNextInstance(
          Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            if (app.MainWindow.WindowState == System.Windows.WindowState.Minimized)
                app.MainWindow.WindowState = System.Windows.WindowState.Maximized;
            app.MainWindow.Activate();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            const string A_RESTART = @"/restart";
            bool isRestart = args.Length > 0 && args[0].Trim().ToLower() == A_RESTART;
            string[] nargs = isRestart ? args.Where(a => a != A_RESTART).ToArray() : args;

            if (!isRestart)
                CommonModule.Helpers.WorkFlowHelper.PerformMaintaince();
            else
                if (!WaitForClose(1000)) return;

            SingleInstanceApplicationWrapper wrapper =
                new SingleInstanceApplicationWrapper();            
            wrapper.Run(nargs);
        }

        private static bool WaitForClose(int _msec)
        {
            var curP = System.Diagnostics.Process.GetCurrentProcess();
            var oldPs = System.Diagnostics.Process.GetProcessesByName(curP.ProcessName);
            bool res = true;
            try
            {
                foreach (var proc in oldPs.Where(p => p.Id != curP.Id))
                    res = proc.WaitForExit(_msec);
            }
            catch
            {
                res = false;
            }
            return res;
        }
    } 
}
