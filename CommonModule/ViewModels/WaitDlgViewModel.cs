using System;
using CommonModule.Commands;

namespace CommonModule.ViewModels
{
    public class WaitDlgViewModel : MsgDlgViewModel
    {
        public WaitDlgViewModel()
        {
            IsCanClose = false;
            BgColor = "Crimson";
            IsLoggable = false;
        }

    }
}