using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Interfaces;
using DataObjects.Interfaces;
using CommonModule.Commands;

namespace CommonModule.ViewModels
{
    public static class DlgViewModelExtensions
    {
        public static Func<BaseDlgViewModel, bool> SetCheck<T>(this T _src, Func<T, bool> _chkFunc) where T : BasicViewModel
        {
            return (BaseDlgViewModel d) => _chkFunc(_src);
        }
    }

    /// <summary>
    /// Базовая модель диалога.
    /// </summary>
    public class BaseDlgViewModel : BasicViewModel, ICloseViewModel, ISubmitViewModel, ICancelViewModel
    {
        private IPersister remember = CommonSettings.Persister;
        protected IPersister Remember { get { return remember; } }

        public IModule Parent { get; set; }

        public string Name { get; set; } // имя диалога, для идентификации в композитных моделях

        private Action<object> onClosed;
        public Action<object> OnClosed
        {
            get { return onClosed; }
            set
            {
                onClosed = value;
            }
        }

        private bool isCanClose = true;
        public bool IsCanClose 
        { 
            get { return isCanClose; }
            set { SetAndNotifyProperty("IsCanClose", ref isCanClose, value); }
        }

        private bool isEnabled = true;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetAndNotifyProperty("IsEnabled", ref isEnabled, value); }
        }

        public virtual bool IsValid()
        {
            return Check == null ? true : Check(this);
        }
        
        public Func<BaseDlgViewModel, bool> Check
        {
            get;
            set;
        }

        public override void Dispose() 
        {
            OnSubmit = null;
            OnClosed = null;
            CloseCommand = null;
            SubmitCommand = null;
        }

        private String bgColor;
        public String BgColor
        {
            get { return bgColor; }
            set { bgColor = value; }
        }

        private bool isLoggable = true;
        public bool IsLoggable
        {
            get { return isLoggable; }
            set { isLoggable = value; }
        }

        public double? Height { get; set; }
        public double? Width { get; set; }

        #region ICloseViewModel Members

        private ICommand closeCommand;
        public ICommand CloseCommand
        { 
            get { return closeCommand; }
            set { SetAndNotifyProperty("CloseCommand", ref closeCommand, value); }
        }

        #endregion

        #region ISubmitViewModel Members

        private ICommand submitCommand;
        public ICommand SubmitCommand
        {
            get { return submitCommand; }
            set { SetAndNotifyProperty("SubmitCommand", ref submitCommand, value); }
        }

        #endregion

        #region ICancelViewModel Members

        private ICommand cancelCommand;
        public ICommand CancelCommand
        {
            get { return cancelCommand; }
            set { SetAndNotifyProperty("CancelCommand", ref cancelCommand, value); }
        }

        #endregion

        protected virtual bool CanSubmit()
        {
            return IsValid();
        }

        protected virtual void ExecuteSubmit()
        {
            if (isLoggable)
                CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Диалог: [{1}] [Submit]", this.Parent == null ? "Null" : Parent.Info.Name, this.Title));
            if (OnSubmit != null)
                OnSubmit(this);

        }

        private Action<object> onSubmit;
        public Action<object> OnSubmit
        {
            get { return onSubmit; }
            set
            {
                onSubmit = value;
                SubmitCommand = onSubmit == null ? null : new LabelCommand(ExecuteSubmit, CanSubmit) { Label = "Подтвердить"};
            }
        }

        private bool isCancelable;
        public bool IsCancelable 
        {
            get { return isCancelable; }
            set 
            {
                SetAndNotifyProperty("IsCancelable", ref isCancelable, value);
                CancelCommand = isCancelable ? CreateCancelCommand() : null;
            }
        }

        private LabelCommand CreateCancelCommand()
        {
            return new LabelCommand(ExecuteCancel, CanCancel) { Label = "Отменить" };
        }

        protected virtual bool CanCancel()
        {
            return true;
        }

        protected virtual void ExecuteCancel()
        {
            if (isLoggable)
                CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Диалог: [{1}] [Cancel]", this.Parent == null ? "Null" : Parent.Info.Name, this.Title));
            if (OnCancel != null)
                OnCancel(this);
            else
                CloseThisDlg();
        }

        private void CloseThisDlg()
        {
            if (Parent != null)
                Parent.CloseDialog(this);
        }

        private Action<object> onCancel;
        public Action<object> OnCancel
        {
            get { return onCancel; }
            set
            {
                onCancel = value;
                CancelCommand = onCancel == null ? null : CreateCancelCommand();
            }
        }
    }
}
