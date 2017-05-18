using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using DotNetHelper;

namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель отображения списка предоплат.
    /// </summary>
    public class PredoplsListViewModel : BasicViewModel
    {
        private IDbService repository;

        public PredoplsListViewModel(IDbService _rep, IEnumerable<PredoplModel> _pred)
        {
            repository = _rep;
            LoadData(_pred);
        }


        /// <summary>
        /// выбранные предолаты
        /// </summary>
        private ObservableCollection<PredoplViewModel> predopls = new ObservableCollection<PredoplViewModel>();
        public ObservableCollection<PredoplViewModel> Predopls  
        {
            private set
            {
                if (value != predopls)
                {
                    predopls = value;
                    NotifyPropertyChanged("Predopls");
                }
            }
            get
            {
                return predopls;
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        public void LoadData(IEnumerable<PredoplModel> _pred)
        {
            //Predopls = new ObservableCollection<PredoplViewModel>(_pred.Select(p => new PredoplViewModel(repository, p)));
            predopls.Clear();
            predopls.AddRange(_pred.Select(p => new PredoplViewModel(repository, p)));
            if (SelectedPredopl != null)
                SelectedPredopl = Predopls.SingleOrDefault(p => p.Idpo == SelectedPredopl.Idpo);
        }

        /// <summary>
        /// Обновляет элемент коллекции
        /// </summary>
        /// <param name="_vm"></param>
        public void RefreshItem(PredoplViewModel _vm)
        {
            if (_vm == null) return;
            bool isselected = _vm == selectedPredopl;

            int itemIndex = IndexOf(_vm);
            if (itemIndex < 0) 
                return;
            else
                RemoveAt(itemIndex);
            var model = repository.GetPredoplById(_vm.Idpo);
            if (model != null)
            {
                var newvm = new PredoplViewModel(repository, model);
                Insert(itemIndex, newvm);
                if (isselected)
                    SelectedPredopl = newvm;
            }
        }

        private PredoplViewModel selectedPredopl;

        /// <summary>
        /// Выбранная предоплата
        /// </summary>
        public PredoplViewModel SelectedPredopl
        {
            get { return selectedPredopl;} 
            set
            {
                if (value != selectedPredopl)
                {
                    selectedPredopl = value;
                    NotifyPropertyChanged("SelectedPredopl");
                }
            }
        }

        /// <summary>
        /// Индекс элемента
        /// </summary>
        /// <param name="_predoplViewModel"></param>
        /// <returns></returns>
        public int IndexOf(PredoplViewModel _predoplViewModel)
        {
            return Predopls==null ? -1 : Predopls.IndexOf(_predoplViewModel);
        }

        /// <summary>
        /// Удаление элемента по индексу
        /// </summary>
        /// <param name="_ind"></param>
        public void RemoveAt(int _ind)
        {
            if (_ind < 0 || Predopls == null || Predopls.Count <= _ind) return;
            Predopls.RemoveAt(_ind);
        }

        /// <summary>
        /// Вставка элемента
        /// </summary>
        /// <param name="_ind"></param>
        /// <param name="_newPrViewModel"></param>
        public void Insert(int _ind, PredoplViewModel _newPrViewModel)
        {
            if (Predopls != null)
                Predopls.Insert(_ind, _newPrViewModel);
        }

        public bool IsShowPredoplItogs
        {
            get { return PredoplsItogs != null && PredoplsItogs.Count > 0; }
        }

        private Dictionary<string, decimal[]> predoplsItogs;

        /// <summary>
        /// Итоги предоплат по валютам
        /// </summary>        
        public Dictionary<string, decimal[]> PredoplsItogs
        {
            get
            {
                if (predoplsItogs == null)
                    predoplsItogs = CalcValItogs(0);
                return predoplsItogs;
            }
        }

        private Dictionary<string, decimal[]> CalcValItogs(short _dir)
        {
            Dictionary<string, decimal[]> res = null;
            res = this.Predopls.Where(p => p.Direction == _dir)
                                       .GroupBy(p => p.ValPropl.ShortName)
                                       .ToDictionary(g => g.Key,
                                                     g => new decimal[] { g.Sum(i => i.SumPropl), g.Sum(i => i.SumPropl - i.SumOtgr) });
            return res;
        }

        public bool IsShowVozvrItogs
        {
            get { return VozvrItogs != null && VozvrItogs.Count > 0; }
        }

        private Dictionary<string, decimal[]> vozvrItogs;

        /// <summary>
        /// Итоги возвратов по валютам
        /// </summary>
        public Dictionary<string, decimal[]> VozvrItogs
        {
            get
            {
                if (vozvrItogs == null)
                    vozvrItogs = CalcValItogs(1);
                return vozvrItogs;
            }
        }
    }
}