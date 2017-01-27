using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using System.ComponentModel.Composition;
using DAL;
using System.Threading;
using CommonModule.Composition;
using DataObjects.Interfaces;
using System.Configuration;
using System.Collections.ObjectModel;
using CommonModule.ModuleServices;

namespace CommonModule.ViewModels
{
    public abstract class BasicModuleViewModel : BasicViewModel, IModule, IModuleCommands
    {

        public BasicModuleViewModel()
        {            
        }

        public ModuleDescription Info { get; set; }

        #region IDialogViewModel Members

        /// <summary>
        /// Представление диалогового окна
        /// </summary>
        private Collection<Object> dialog;
        public Collection<Object> Dialog
        {
            get
            {
                if (dialog == null)
                    dialog = new ObservableCollection<object>();//new object[]{new DialogContainer(new MsgDlgViewModel{Message = "Test"})});
                return dialog;
            }
            private set
            {
                if (value != dialog)
                {
                    dialog = value;
                    NotifyPropertyChanged("Dialog");
                }
            }
        }

        public Object TopDialog 
        {
            get 
            {
                return Dialog.Count == 0 ? null : Dialog[Dialog.Count - 1];
            }
        }

        private IModuleService services;
        public IModuleService Services
        {
            get
            {
                if (services == null)
                    services = GetModuleService();
                return services;
            }
        }

        protected virtual IModuleService GetModuleService()
        {
            return new BaseModuleService(this);
        }

        /// <summary>
        /// Комманда загрузки модуля во внешнюю оболочку
        /// </summary>
        private ICommand startModule;
        public ICommand StartModule
        {
            get
            {
                if (startModule == null)
                    startModule = new DelegateCommand(() => 
                        ShellModel.LoadModule(this));
                return startModule;
            }
        }

        /// <summary>
        /// Комманда закрытия (выгрузки) модуля и его компонентов
        /// </summary>
        private ICommand stopModule;
        public ICommand StopModule
        {
            get
            {
                if (stopModule == null)
                    stopModule = new DelegateCommand(CloseModule, CanClose);
                return stopModule;
            }
        }
        protected virtual bool CanClose() { return true; }
        protected abstract void OnClose();
        private void CloseModule()
        { 
            OnClose();
            GC.Collect();
        }

        #endregion

        [Import]
        public IShellModel ShellModel
        {
            get;
            set;
        }

        public abstract void LoadContent(IModuleContent _content);

        public abstract void UnLoadContent(IModuleContent _content);

        public abstract bool IsContentLoaded { get; }

        public abstract IModuleContent GetLoadedContent<T>(Func<T, Boolean> _filter) where T: class, IModuleContent;
        public abstract bool SelectContent<T>(Func<T, Boolean> _filter) where T : class, IModuleContent;

        protected int accessLevel = -1;

        protected virtual int GetAccessLevel()
        {
            return Repository.GetComponentAccess(
                this.GetType().Assembly.FullName
                                        .Split(new char[] { ',' })
                                            .FirstOrDefault());
        }

        #region IModule Members


        public int AccessLevel
        {
            get
            {
                if (accessLevel == -1)
                    accessLevel = GetAccessLevel();
                return accessLevel;
            }
        }

        #endregion

        #region IModule Members

        public void OpenDialog(ICloseViewModel _dlg)
        {
            if (_dlg != null)
            {                
                if (_dlg.IsCanClose && _dlg.CloseCommand == null)
                    _dlg.CloseCommand = new DelegateCommand(() => CloseDialog(_dlg));
                var dialog = _dlg as BaseDlgViewModel;
                if (dialog != null)
                    dialog.Parent = this;
                var container = new DialogContainer(_dlg);
                Action update = () =>
                {
                    Dialog.Add(container);
                    NotifyPropertyChanged("TopDialog");
                    if (dialog == null || dialog.IsLoggable)
                        WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Диалог: [{1}] [Open]",this.Info.Name, dialog == null ? _dlg.ToString() : dialog.Title));
                };
                ShellModel.UpdateUi(update, false, false);                
            }
        }

        public void CloseDialog(Object _dlg)
        {
            if (Dialog == null || Dialog.Count == 0 
                || _dlg == null) 
                return;
            
            Object DlgItem = _dlg;
            Object curDlgContent = _dlg;
            if (!Dialog.Contains(_dlg))
            {
                DlgItem = Dialog.OfType<DialogContainer>().FirstOrDefault(c => c.DialogContent == _dlg);
                if (DlgItem != null)
                    curDlgContent = (DlgItem as DialogContainer).DialogContent;
            }
            if (DlgItem == null) 
                return;
            var trueDialog = curDlgContent as BaseDlgViewModel;
            
            Action update = () =>
            {
                Dialog.Remove(DlgItem);
                if (curDlgContent == null) return;               
                
                if (trueDialog != null){
                    //trueDialog.Parent = null;
                    if (trueDialog.OnClosed != null) 
                        trueDialog.OnClosed(trueDialog);
                }
                IDisposable d = curDlgContent as IDisposable;
                if (d != null) d.Dispose();
                NotifyPropertyChanged("TopDialog");
                if (trueDialog == null || trueDialog.IsLoggable)
                    WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Диалог: [{1}] [Close]", this.Info.Name, trueDialog == null ? _dlg.ToString() : trueDialog.Title));               
            };            
            ShellModel.UpdateUi(update, false, false);            
        }

        #endregion


        public ModuleCommand[] ModuleCommands { get; set; }

        /// <summary>
        /// Загрузка комманд
        /// </summary>
        protected virtual void LoadCommands(string _contract)
        {
            if (String.IsNullOrEmpty(_contract) || ShellModel == null || ShellModel.Container == null)
                return;

            var allcommands = ShellModel.Container.GetExports<ModuleCommand, IDisplayOrderMetaData>(_contract)
                            .Where(lm => lm.Metadata.DisplayOrder > 0)
                            .OrderBy(lm => lm.Metadata.DisplayOrder).Select(lm => lm.Value);

            List<ModuleCommand> comlst = new List<ModuleCommand>();
            foreach (var c in allcommands)
            {
                int al = Repository.GetComponentAccess(c.GetType().ToString());
                if (al > 0)
                {
                    c.Parent = this;
                    c.SetAccess(al);
                    comlst.Add(c);
                }
            }
            ModuleCommands = comlst.ToArray();
        }

        public IDbService Repository
        {
            get { return ShellModel.Repository; }
        }

    }
}