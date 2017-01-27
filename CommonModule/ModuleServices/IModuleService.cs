using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;

namespace CommonModule.ModuleServices
{
    public interface IModuleService
    {
        void ShowMsg(string _title, string _msg, bool _isError);
        void DoWaitAction(Action _work);
        void DoWaitAction(Action _work, string _title, string _msg);
        void DoWaitAction(Action _work, string _title, string _msg, Action _afterwork);
        void DoWaitAction(Action<WaitDlgViewModel> _work, string _title, string _msg);
        void DoWaitAction(Action<WaitDlgViewModel> _work, string _title, string _msg, Action _afterwork);
        void DoWaitAction(Action<ProgressDlgViewModel> _work, string _title, string _msg);
        void DoWaitAction(Action<ProgressDlgViewModel> _work, string _title, string _msg, Action _afterwork);
        void DoAsyncAction(Action _work, Action _afterwork);
    }
}
