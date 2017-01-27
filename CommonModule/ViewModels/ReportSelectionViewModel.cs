using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using DAL;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;

namespace CommonModule.ViewModels
{
    public class ReportSelectionViewModel : BaseDlgViewModel
    {
        public ReportSelectionViewModel()
        {
            Title = "Выбор отчёта для формирования";            
        }

        public ReportSelectionViewModel(IDbService _repository, IEnumerable<ReportModel> _reps)
            :this()
        {
            reports = new List<ReportDataViewModel>(_reps.Select(r => new ReportDataViewModel(_repository, r)));
        }

        public ReportSelectionViewModel(IDbService _repository, IEnumerable<ReportDataViewModel> _reps)
            : this()
        {
            reports = new List<ReportDataViewModel>(_reps);
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && SelectedReport != null;
        }

        /// <summary>
        /// Список отчётов
        /// </summary>
        private List<ReportDataViewModel> reports;
        public List<ReportDataViewModel> Reports
        {
            get 
            {
                if (reports == null)
                    reports = new List<ReportDataViewModel>();
                return reports; 
            }
        }

        private ReportDataViewModel selectedReport;
        public ReportDataViewModel SelectedReport
        {
            get
            {
                return selectedReport;
            }
            set
            {
                selectedReport = value;
                NotifyPropertyChanged("SelectedReport");
            }
        }

    }
}
