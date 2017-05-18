using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;
using System.Collections.ObjectModel;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель составного диалога.
    /// </summary>
    public class BaseCompositeDlgViewModel : CompositeDlgViewModel
    {

        protected override bool ItemsCorrect()
        {
            bool res = true;
            res = DialogViewModels.Length > 0 && DialogViewModels.All(vm => vm.IsValid());
            return res;
        }

        public BaseDlgViewModel[] DialogViewModels
        {
            get
            {
                return InnerParts.Where(p => !(p is SelectableDlgViewModelContainer) || (p as SelectableDlgViewModelContainer).IsSelected).Select(p => p.InnerViewModel).ToArray();
            }
        }

        //public BaseDlgViewModel this[string _name]
        //{
        //    get
        //    {
        //        return innerParts == null || innerParts.Count == 0 || string.IsNullOrWhiteSpace(_name) ? null
        //               : innerParts.Where(p => p.InnerViewModel.Name == _name).Select(p => p.InnerViewModel).FirstOrDefault();
        //    }
        //}

        public T GetByName<T>(string _name) where T : BaseDlgViewModel
        {
            if (innerParts == null || innerParts.Count == 0 || string.IsNullOrWhiteSpace(_name)) return null;
            T result = innerParts.Where(c => c.IsValid()).Select(c => c.InnerViewModel).OfType<T>().FirstOrDefault(i => i.Name == _name);
            if (result == null)
                result = innerParts.OfType<BaseCompositeDlgViewModel>().Select<BaseCompositeDlgViewModel,T>(c => c.GetByName<T>(_name)).FirstOrDefault(r => r != null);
            return result;
        }

        private ObservableCollection<DlgViewModelContainer> innerParts;
        public ObservableCollection<DlgViewModelContainer> InnerParts
        {
            get
            {
                if (innerParts == null)
                    innerParts = new ObservableCollection<DlgViewModelContainer>();
                return innerParts;
            }
        }

        public void Add(BaseDlgViewModel _dlg)
        {
            if (_dlg == null) return;
            var newCont = new DlgViewModelContainer(_dlg) { Title = _dlg.Title };
            InnerParts.Add(newCont);
        }

        public void AddHidable(BaseDlgViewModel _dlg, bool _hide)
        {
            if (_dlg == null) return;
            var newCont = new HidableDlgViewModelContainer(_dlg) { Title = _dlg.Title, IsHided = _hide };
            InnerParts.Add(newCont);
        }

        public void AddSelectable(BaseDlgViewModel _dlg, bool _select)
        {
            if (_dlg == null) return;
            var newCont = new SelectableDlgViewModelContainer(_dlg) { Title = _dlg.Title, IsSelected = _select };
            InnerParts.Add(newCont);
        }


        public void Add(DlgViewModelContainer _dlgCont)
        {
            if (_dlgCont == null) return;
            InnerParts.Add(_dlgCont);
        }
    }
}