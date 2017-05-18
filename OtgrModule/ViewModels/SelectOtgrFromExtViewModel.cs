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
    /// <summary>
    /// Модель диалога выбора принимаемой из внешних источников отгрузки.
    /// </summary>
    public class SelectOtgrFromExtViewModel : BaseDlgViewModel
    {
        IDbService repository;

        public SelectOtgrFromExtViewModel(IDbService _repository, IEnumerable<OtgrLine> _otgrData)
        {
            if (_otgrData == null) throw (new ArgumentNullException("_otgrData", "Нет данных для отображения"));

            repository = _repository;
            otgrData = new List<OtgrLineViewModel>(_otgrData.Select(o => new OtgrLineViewModel(repository, o)));

            SelectDeselectAllCommand = new DelegateCommand(ExecSelectDeselectAll);
            SelectDeselectDocCommand = new DelegateCommand(ExecSelectDeselectDoc, () => SelectedOtgr != null && !SelectedOtgr.HasErrors);
        }

        /// <summary>
        /// Коллекция строк принимаемой отгрузки
        /// </summary>
        private List<OtgrLineViewModel> otgrData = new List<OtgrLineViewModel>();
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

        private OtgrLineViewModel selectedOtgr;
        /// <summary>
        /// Выбранная отгрузка
        /// </summary>
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    selectedOtgr = value;
                    NotifyPropertyChanged("SelectedOtgr");
                }
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
                //SelectAllOtgr();
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

        /// <summary>
        /// Комманда выделения/снятия выделения отгрузок документа
        /// </summary>
        public ICommand SelectDeselectDocCommand { get; set; }
        private void ExecSelectDeselectDoc()
        {
            if (SelectedOtgr == null) return;
            bool tostate = SelectedOtgr.IsChecked;

            foreach (var o in otgrData.Where(o => o.DocumentNumber == SelectedOtgr.DocumentNumber))
                o.IsChecked = o.HasErrors ? false : tostate;
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
