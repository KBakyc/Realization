using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CommonModule.Commands;
using CommonModule.ViewModels;
//using DAL;
using DataObjects;


namespace PredoplModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора контрагента для погашения неоплаченных остатков.
    /// </summary>
    public class SelKaWithDebtsDlgViewModel : BaseDlgViewModel
    {

        public KaTotalDebtViewModel SelectedVm { get; set; }

        public SelKaWithDebtsDlgViewModel(IEnumerable<KaTotalDebtViewModel> _outst)
        {
            outstandings = new ObservableCollection<KaTotalDebtViewModel>(_outst);
        }

        private ObservableCollection<KaTotalDebtViewModel> outstandings;
        public ObservableCollection<KaTotalDebtViewModel> Outstandings
        {
            get
            {
                if (outstandings == null)
                    outstandings = new ObservableCollection<KaTotalDebtViewModel>();
                return outstandings;
            }
        }

        public PoupModel SelPoup { get; set;}
        public PkodModel SelPkod { get; set;}
        public Valuta SelVal { get; set;}
        public DateTime SelDate { get; set; }


        public override bool IsValid()
        {
            return SelectedVm != null;
        }

    }
}