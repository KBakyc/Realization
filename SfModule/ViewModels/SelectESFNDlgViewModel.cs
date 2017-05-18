using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using DataObjects;

namespace SfModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора ЭСФН.
    /// </summary>
    public class SelectESFNDlgViewModel : BaseDlgViewModel
    {
        public SelectESFNDlgViewModel(Tuple<int, string, DateTime?, string, string, decimal?, string>[] _esfns)
        {
            LoadData(_esfns);
        }

        private Tuple<int, string, DateTime?, string, string, decimal?, string>[] allESFNs;
        public Tuple<int, string, DateTime?, string, string, decimal?, string>[] ESFNs
        {
            get { return allESFNs; }
            set { SetAndNotifyProperty("ESFNs", ref allESFNs, value); }
        }

        private Tuple<int, string, DateTime?, string, string, decimal?, string> selectedESFN;
        public Tuple<int, string, DateTime?, string, string, decimal?, string> SelectedESFN
        {
            get { return selectedESFN; }
            set { SetAndNotifyProperty("SelectedESFN", ref selectedESFN, value); }
        }

        public decimal TotalSumOfSelectedESFN { get { return selectedESFN == null ? 0M : selectedESFN.Item6.GetValueOrDefault();  }}

        public int SelectedInvoiceId { get { return selectedESFN == null ? 0 : selectedESFN.Item1; } }

        public void LoadData(Tuple<int, string, DateTime?, string, string, decimal?, string>[] _esfns)
        {
            ESFNs = _esfns;
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && selectedESFN != null;
        }
    }
}
