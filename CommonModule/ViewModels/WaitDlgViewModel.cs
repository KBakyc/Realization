using System;
using CommonModule.Commands;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога ожидания выполнения задачи
    /// </summary>
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