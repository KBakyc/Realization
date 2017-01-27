using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using System.Threading;

namespace CommonModule.ViewModels
{
    public class ContentModuleViewModel : BasicModuleViewModel, IContentModule
    {

        /// <summary>
        /// Рабочая область модуля
        /// </summary>
        private IModuleContent content;
        public IModuleContent Content
        {
            get
            {
                return content;
            }
            set
            {
                if (value != content)
                {
                    content = value;
                    NotifyPropertyChanged("Content");
                    NotifyPropertyChanged("IsContentLoaded");
                }
            }
        }

        #region CloseWorkSpaceCmd

        private ICommand closeWorkSpaceCmd;
        /// <summary>
        /// Комманда закрытия рабочей области
        /// </summary>
        public ICommand CloseWorkSpaceCmd
        {
            get
            {
                if (closeWorkSpaceCmd == null)
                    closeWorkSpaceCmd = new DelegateCommand(() => Content = null);
                return closeWorkSpaceCmd;
            }
        }

        #endregion


        public override void LoadContent(IModuleContent _content)
        {
            if (!_content.IsEnabled) return;
            Action update = () =>
            {
                if (content.IsEnabled) 
                {
                    Content = _content;
                    var page = _content as BasicViewModel;
                    CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Содержимое: [{1}] [Load]", this.Info.Name, page == null ? "Null" : page.Title));
                }
            };
            
            ShellModel.UpdateUi(update, true, false);
        }

        public override void UnLoadContent(IModuleContent _content)
        {
            if (Content != _content) return;

            Action update = () =>
            {
                Content = null;                
                var page = _content as BasicViewModel;
                CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Содержимое: [{1}] [Unload]", this.Info.Name, page == null ? "Null" : page.Title));
            };

            ShellModel.UpdateUi(update, true, false);
        }

        public override bool IsContentLoaded
        {
            get { return content != null; }
        }

        public override IModuleContent GetLoadedContent<T>(Func<T, Boolean> _filter)
        {
            return content as T;
        }

        public override bool SelectContent<T>(Func<T, Boolean> _filter)
        {
            var content = GetLoadedContent<T>(_filter);
            return content != null;
        }

        protected override void OnClose()
        {
            if (content != null)
            {
                UnLoadContent(content);
                var basicContent = content as BasicModuleContent;
                if (basicContent != null)
                    basicContent.Dispose();
            }
        }
    }
}