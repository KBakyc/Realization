using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;

namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора предоплаты.
    /// </summary>
    public class SelectPredoplDlgViewModel : BaseDlgViewModel
    {
        private PredoplsListViewModel predoplsLst;

        public SelectPredoplDlgViewModel(IDbService _repository, PredoplModel[] _docs)
        {
            predoplsLst = new PredoplsListViewModel(_repository, _docs);
        }

        public PredoplsListViewModel PredoplsList { get { return predoplsLst; } }

        public override bool IsValid()
        {
            return base.IsValid()
                && predoplsLst != null && predoplsLst.SelectedPredopl != null;
        }
    }   
}
