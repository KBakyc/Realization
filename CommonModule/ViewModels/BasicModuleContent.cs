using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using System.Linq.Expressions;

namespace CommonModule.ViewModels
{
    public class BasicModuleContent : BasicViewModel, IModuleContent
    {
        private IModule parent;
        protected bool isReadOnly;
        protected bool isEnabled;
        protected bool isLoaded;

        public BasicModuleContent(IModule _parent)
        {
            isCanClose = true;
            //isActive = InitIsActive();
            SetParent(_parent);
        }

        private void CheckAccess(IModule _parent)
        {
            int access = _parent == null ? 2 : _parent.AccessLevel;
            if (access > 0) 
            {
                var al = _parent.Repository.GetComponentAccess(this.GetType().ToString());
                access = al < access ? al : access;
            }
            isReadOnly =  access < 2;
            isEnabled = access > 0;
        }

        protected virtual bool CheckComponentAccess(Expression<Func<object>> _getCompName)
        {
            bool res = true;
            if (parent != null && _getCompName != null && _getCompName.Body is MemberExpression)
            {
                var compName = (_getCompName.Body as MemberExpression).Member.Name;
                var modContent = this.GetType().ToString();
                var fullCompName = modContent + "." + compName;
                res = parent.Repository.GetComponentAccess(fullCompName) != 0;
            }
            return res;
        }
        
        protected virtual bool CheckComponentAccess(string _compName)
        {
            return parent.Repository.GetComponentAccess(_compName) != 0;
        }

        private bool isCanClose;
        public bool IsCanClose
        {
            get { return isCanClose; }
            set { isCanClose = value; }
        }


        public bool IsReadOnly 
        {
            get { return isReadOnly; }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
        }

        public IModule Parent
        {
            get { return parent; }
        }

        public bool TryOpen()
        {
            if (isLoaded || !isEnabled || parent == null) return false;
            parent.LoadContent(this);
            return true;
        }

        protected virtual void SetParent(IModule _mod)
        {
            if (_mod != null)
            {
                CheckAccess(_mod);
                parent = isEnabled ? _mod : null;
            }
        }

        private bool isActive;

        /// <summary>
        /// Является ли контент текущим в модуле
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }

            set
            {
                isActive = value;
                NotifyPropertyChanged("IsActive");                
            }
        }

        private ICommand closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (closeCommand == null)
                    closeCommand = new DelegateCommand(CloseAction, () => Parent != null && IsCanClose);
                return closeCommand;
            }
            set { closeCommand = value; }
        }

        private void CloseAction()
        {
            if (Parent == null) return;
                        
            Parent.UnLoadContent(this);
            this.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Признак того, что содержимое загружено
        /// </summary>        
        public bool IsLoaded
        {
            get { return isLoaded; }
            set
            {
                if (value != isLoaded)
                {
                    isLoaded = value;
                    if (isLoaded && Parent.Dialog.Count > 0)
                    {
                        var waitCont = parent.Dialog[Parent.Dialog.Count - 1] as DialogContainer;
                        if (waitCont != null)
                        {
                            var waitdlg = waitCont.DialogContent as WaitDlgViewModel;
                            if (waitdlg != null)
                                Parent.CloseDialog(waitdlg);
                        }
                    }
                }
            }
        }
    }
}
