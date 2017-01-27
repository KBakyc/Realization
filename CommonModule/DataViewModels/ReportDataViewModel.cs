using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using DataObjects.Interfaces;
using DataObjects;

namespace CommonModule.DataViewModels
{
    public class ReportDataViewModel : BasicNotifier
    {
        private IDbService repository;
        private ReportModel model;        

        public ReportDataViewModel(IDbService _repository, ReportModel _report)
        {
            repository = _repository;
            model = _report;
            isFavorite = _report.IsFavorite;
        }

        public ReportModel Model { get { return model; } }

        public int Id { get { return model.ReportId; } }
        public string Title { get { return model.Title; } }
        public string Description { get { return model.Description; } }

        private bool isFavorite;
        public bool IsFavorite
        {
            get { return isFavorite; }
            set 
            { 
                if (SetAndNotifyProperty(() => IsFavorite, ref isFavorite, value))
                    SaveFavorite(); 
            }
        }

        private void SaveFavorite()
        {
            model.IsFavorite = isFavorite;
            if (!repository.SetReportFavorite(model))
            {
                model.IsFavorite = isFavorite = !isFavorite;
                NotifyPropertyChanged(() => IsFavorite);
            }

        }
        
    }
}
