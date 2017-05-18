using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using CommonModule.Helpers;

namespace CommonModule.ModuleServices
{
    /// <summary>
    /// Базовый сервис модуля.
    /// </summary>
    public class BaseModuleService : IModuleService
    {
        private IModule parent;

        public BaseModuleService(IModule _parent)
        {
            parent = _parent;
        }

        protected IModule Parent { get { return parent; } }

        public void ShowMsg(string _title, string _msg, bool _isError)
        {
            if (parent == null) return;
            var msgdlg = new MsgDlgViewModel
            {
                Title = _title,
                Message = _msg,
                MessageType = _msg != null && _msg.Length > 1000 ? MsgType.Text : MsgType.Message
            };

            if (_isError)
            {
                msgdlg.BgColor = "Crimson";
                WorkFlowHelper.WriteToLog(null, _msg);
            }

            parent.OpenDialog(msgdlg);
        }

        public void DoWaitAction(Action _work)
        {
            DoWaitAction(_work, null, null, null);
        }

        public void DoWaitAction(Action _work, string _title, string _msg)
        {
            DoWaitAction(_work, _title, _msg, null);
        }

        public void DoWaitAction(Action _work, string _title, string _msg, Action _afterwork)
        {
            var waitdlg = new WaitDlgViewModel
            {
                Title = _title ?? "Подождите",
                Message = _msg ?? "Идёт загрузка данных"
            };

            parent.OpenDialog(waitdlg);

            _work.BeginInvoke(new AsyncCallback(
                (o) =>
                {
                    parent.CloseDialog(waitdlg);
                    if (_afterwork != null)
                        _afterwork();
                }
                ), null);
        }

        public void DoWaitAction(Action<WaitDlgViewModel> _work, string _title, string _msg)
        {
            DoWaitAction(_work, _title, _msg, null);
        }

        public void DoWaitAction(Action<WaitDlgViewModel> _work, string _title, string _msg, Action _afterwork)
        {
            var waitdlg = new WaitDlgViewModel
            {
                Title = _title,
                Message = _msg
            };

            OpenWaitDlgAndInvokeAction(waitdlg, _work, _afterwork);
        }

        public void DoWaitAction(Action<ProgressDlgViewModel> _work, string _title, string _msg)
        {
            DoWaitAction(_work, _title, _msg, null);
        }
        
        public void DoWaitAction(Action<ProgressDlgViewModel> _work, string _title, string _msg, Action _afterwork)
        {
            var waitdlg = new ProgressDlgViewModel
            {
                Title = _title,
                Message = _msg
            };

            OpenWaitDlgAndInvokeAction(waitdlg, _work, _afterwork);
        }

        private void OpenWaitDlgAndInvokeAction<T>(T _dlg, Action<T> _work, Action _afterwork)
            where T: WaitDlgViewModel
        {
            parent.OpenDialog(_dlg);

            _work.BeginInvoke(_dlg,new AsyncCallback(
                (o) =>
                {
                    parent.CloseDialog(_dlg);
                    if (_afterwork != null)
                        _afterwork();
                }
                ), null);
        }

        public void DoAsyncAction(Action _work, Action _afterwork)
        {
            if (_work != null)
                _work.BeginInvoke(new AsyncCallback(
                    (o) =>
                    {
                        if (_afterwork != null)
                            _afterwork();
                    }
                    ), null);
        }
    }
}
