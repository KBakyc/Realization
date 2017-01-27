using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OtgrModule.ViewModels
{
    public class SelectOtgrFromRwListViewModel : BaseDlgViewModel
    {
        IDbService repository;

        public SelectOtgrFromRwListViewModel(IDbService _repository, IEnumerable<OtgrLine> _otgrData)
        {
            if (_otgrData == null) throw (new ArgumentNullException("_otgrData", "Нет данных для отображения"));

            repository = _repository;
            otgrData = new List<OtgrLineViewModel>(_otgrData.Select(o => new OtgrLineViewModel(repository, o)));

            SelectDeselectAllCommand = new DelegateCommand(ExecSelectDeselectAll);            
        }

        /// <summary>
        /// Коллекция строк принимаемой отгрузки
        /// </summary>
        private List<OtgrLineViewModel> otgrData;
        public List<OtgrLineViewModel> OtgrData
        {
            set
            {
                if (value != otgrData)
                {
                    otgrData = value;
                    NotifyPropertyChanged("OtgrData");
                }
            }
            get
            {
                return otgrData;
            }
        }

        public DelegateCommand SubmitChangesCommand { get; set; }

        public override bool IsValid()
        {
            return base.IsValid()
            && otgrData.Any(o => o.IsChecked);
        }

        private bool isAllSelectMode;
        /// <summary>
        /// Выбраны все
        /// </summary>
        public bool IsAllSelectMode
        {
            get
            {
                return isAllSelectMode;
            }
            set
            {
                SetAndNotifyProperty("IsAllSelectMode", ref isAllSelectMode, value);
            }
        }

        /// <summary>
        /// Комманда выделения/снятия выделения всех отгрузок
        /// </summary>
        public ICommand SelectDeselectAllCommand { get; set; }
        private void ExecSelectDeselectAll()
        {
            SelectAllOtgr();
        }

        private void SelectAllOtgr()
        {
            if (otgrData == null) return;

            foreach (var o in otgrData)
                o.IsChecked = o.HasErrors ? false : IsAllSelectMode;
        }

        private bool isShowErrors;
        public bool IsShowErrors
        {
            get { return isShowErrors; }
            set
            {
                if (value != isShowErrors)
                {
                    isShowErrors = value;
                    ChangeFilter();
                }
            }
        }

        public void ChangeFilter()
        {
            var cv = System.Windows.Data.CollectionViewSource.GetDefaultView(otgrData);
            if (!IsShowErrors)
                cv.Filter = r => !((OtgrLineViewModel)r).HasErrors;
            else
                cv.Filter = null;
            cv.Refresh();
        }

    }
}
