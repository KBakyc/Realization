using System;
using CommonModule.Commands;

namespace CommonModule.ViewModels
{
    public class ProgressDlgViewModel : WaitDlgViewModel
    {
        public ProgressDlgViewModel()
        {
            IsCanClose = false;
            //BgColor = "Crimson";
            IsLoggable = false;
        }

        private decimal startValue;
        public decimal StartValue
        {
            get { return startValue; }
            set { SetAndNotifyProperty("StartValue", ref startValue, value); }
        }

        private decimal finishValue;
        public decimal FinishValue
        {
            get { return finishValue; }
            set { SetAndNotifyProperty("FinishValue", ref finishValue, value); }
        }

        private decimal currentValue;
        public decimal CurrentValue
        {
            get { return currentValue; }
            set { SetAndNotifyProperty("CurrentValue", ref currentValue, value); }
        }

    }
}