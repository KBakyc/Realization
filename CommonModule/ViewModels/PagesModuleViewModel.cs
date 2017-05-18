using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using System.Collections.ObjectModel;
using System.Threading;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Базовая модель модуля, поддерживающего одновременную загрузку нескольких режимов.
    /// </summary>
    public class PagesModuleViewModel : BasicModuleViewModel, IPagesModule
    {
        public PagesModuleViewModel()
        {
            selectPageCommand = new DelegateCommand<IModuleContent>(SelectPage);
        }
        /// <summary>
        /// Страницы модуля
        /// </summary>
        private ObservableCollection<IModuleContent> pages;
        public ObservableCollection<IModuleContent> Pages
        {
            get 
            {
                if (pages == null)
                    pages = new ObservableCollection<IModuleContent>();
                return pages;
            }
        }

        private ICommand selectPageCommand;

        public ICommand SelectPageCommand
        {
            get { return selectPageCommand; }
        }

        private void SelectPage(IModuleContent _page)
        {
            if (selectedPage != null && _page != selectedPage) selectedPage.IsActive = false;
            if (_page != null)
                _page.IsActive = true;          
            selectedPage = _page;
            NotifyPropertyChanged("SelectedPage");        
            
            var page = selectedPage as BasicViewModel;
            CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Содержимое: [{1}] [Select]", this.Info.Name, page == null ? "Null" : page.Title));
        }


        private IModuleContent selectedPage;
        public IModuleContent SelectedPage
        {
            get
            {
                return selectedPage;
            }
            set
            {
                SelectPage(value);                
            }
        }

        public override void LoadContent(IModuleContent _content)
        {
            Action update = () =>
            {
                if (!pages.Contains(_content))
                {                    
                    if (!_content.IsEnabled) return;            
                    Pages.Add(_content);
                    NotifyPropertyChanged("IsContentLoaded");
                    var page = _content as BasicViewModel;
                    CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Содержимое: [{1}] [Load]", this.Info.Name, page == null ? "Null" : page.Title));
                }
                
                SelectedPage = _content;// Pages.IndexOf(_content);
            };
            ShellModel.UpdateUi(update, true, false);
        }

        public override void UnLoadContent(IModuleContent _content)
        {
            if (!Pages.Contains(_content)) return;
            Action update = () => UnLoadContentAction(_content);
            ShellModel.UpdateUi(update, true, false);
        }

        private void UnLoadContentAction(IModuleContent _content)
        {
            string title = "Null";
            if (_content is BasicViewModel)
            {
                var vm = _content as BasicViewModel;
                title = vm.Title;
            }
            Pages.Remove(_content);
            if (selectedPage == _content)
                SelectedPage = null;
            NotifyPropertyChanged("IsContentLoaded");
                
            CommonModule.Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Содержимое: [{1}] [Unload]", this.Info.Name, title));
        }

        public override bool IsContentLoaded
        {
            get { return Pages.Count > 0; }
        }

        public override IModuleContent GetLoadedContent<T>(Func<T, Boolean> _filter)
        {
            return (pages.Count == 0) ? null : (_filter == null ? Pages.OfType<T>().FirstOrDefault() 
                                                                : Pages.OfType<T>().FirstOrDefault(_filter));
        }

        public override bool SelectContent<T>(Func<T, Boolean> _filter)
        {
            var content = GetLoadedContent<T>(_filter);
            if (content != null)
                SelectedPage = content;
            
            return content != null;
        }

        public bool RemoveSimilarContents<T>()
        {
            bool res = false;
            var om = Pages.SingleOrDefault(pg => pg is T);
            if (om != null) res = true;
            Action update = () =>
            {
                while (om != null)
                {
                    Pages.Remove(om);
                    om = Pages.SingleOrDefault(pg => pg is T);
                }
                GC.Collect();
            };
            ShellModel.UpdateUi(update, true, false);
            return res;
        }

        protected override void OnClose()
        {
            Dispose();
        }

        private ModuleMenuItemViewModel[] menuItems;
        public ModuleMenuItemViewModel[] MenuItems
        {
            get
            {
                if (menuItems == null && ModuleCommands != null && ModuleCommands.Length > 0)
                    menuItems = ModuleCommands.GroupBy(c => c.GroupName ?? c.Label)
                                              .Select(g => new ModuleMenuItemViewModel(g.Key, g))
                                              .ToArray();
                return menuItems;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (pages != null && pages.Count > 0)
                foreach (var p in pages.ToArray())
                {
                    UnLoadContentAction(p);
                    var basicContent = p as BasicModuleContent;
                    if (basicContent != null)
                        basicContent.Dispose();
                }

        }
    }
}