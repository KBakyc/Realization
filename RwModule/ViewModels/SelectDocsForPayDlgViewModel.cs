using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using CommonModule.Helpers;
using CommonModule.Commands;
using System.Windows.Input;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора ЖД документов для погашения.
    /// </summary>
    public class SelectDocsForPayDlgViewModel : BaseDlgViewModel
    {        
        public SelectDocsForPayDlgViewModel(RwPlatViewModel _rwplat, IEnumerable<RwListViewModel> _rwlists)
        {
            rwplat = _rwplat;
            RwListsWithDocs = _rwlists.ToDictionary(l => new Selectable<RwListViewModel>(l, false), l => l.RwDocsCollection.Select(d => new Selectable<RwDocViewModel>(d, false)).ToArray());
            rwDocSumOpl = RwListsWithDocs.Values.SelectMany(v => v.Select(s => s.Value)).ToDictionary(v => v, v => 0M);
            SelectRwDocs();
            SelectDeselectRwList = new DelegateCommand<Selectable<RwListViewModel>>(ExecSelectDeselectRwList, CanSelectDeselectRwList);
            SelectDeselectRwDoc = new DelegateCommand<Selectable<RwDocViewModel>>(ExecSelectDeselectRwDoc, CanSelectDeselectRwDoc);
        }

        private void SelectRwDocs()
        {
            foreach (var sdoc in RwListsWithDocs.Values.SelectMany(v => v.Select(sd => sd)))
            {
                //if (OstToPay <= 0 && sdoc.Value.Ostatok > 0 || sump sdoc.Value.Ostatok < 0) break;
                if (CanSelectDeselectRwDoc(sdoc))
                    ExecSelectDeselectRwDoc(sdoc);
            }
        }

        private Dictionary<RwDocViewModel, decimal> rwDocSumOpl;

        private RwPlatViewModel rwplat;
        
        public RwPlatViewModel Rwplat
        {
            get { return rwplat; }
            set { rwplat = value; }
        }

        public Dictionary<Selectable<RwListViewModel>, Selectable<RwDocViewModel>[]> RwListsWithDocs { get; set; }

        public ICommand SelectDeselectRwList { get; set; }

        private void ExecSelectDeselectRwList(Selectable<RwListViewModel> _rwl)
        {
            _rwl.IsSelected = !_rwl.IsSelected;
            Array.ForEach(RwListsWithDocs[_rwl], d =>
            {
                if (_rwl.IsSelected == d.IsSelected) return;
                if (CanSelectDeselectRwDoc(d))
                {
                    d.IsSelected = _rwl.IsSelected;
                    DoStoreRwDocPay(d);
                }
            });
        }

        private bool CanSelectDeselectRwList(Selectable<RwListViewModel> _rwl)
        {
            if (_rwl == null) return false;
            else return true;
            //if (_rwl.IsSelected) return true;
            //else 
            //{
            //    var rwlsumost = RwListsWithDocs[_rwl].Sum(sd => sd.Value.Ostatok - rwDocSumOpl[sd.Value]);
            //    return OstToPay - rwlsumost >= 0 && OstToPay - rwlsumost <= rwplat.Sumplat;
            //}
            
        }

        public ICommand SelectDeselectRwDoc { get; set; }

        private void ExecSelectDeselectRwDoc(Selectable<RwDocViewModel> _rwd)
        {
            _rwd.IsSelected = !_rwd.IsSelected;
            var rwl = RwListsWithDocs.Keys.FirstOrDefault(k => k.Value.Id_rwlist == _rwd.Value.Id_rwlist);
            if (rwl != null)
                rwl.IsSelected = !_rwd.IsSelected && _rwd.Value.Ostatok != 0 ? false : RwListsWithDocs[rwl].All(d => d.IsSelected);
            DoStoreRwDocPay(_rwd);
        }

        private void DoStoreRwDocPay(Selectable<RwDocViewModel> _rwd)
        {
            if (_rwd.IsSelected)
            {
                var dsumtopay = OstToPay > _rwd.Value.Ostatok ? _rwd.Value.Ostatok : OstToPay;
                SumToPay += dsumtopay;
                rwDocSumOpl[_rwd.Value] += dsumtopay;
            }
            else
            {
                SumToPay -= rwDocSumOpl[_rwd.Value];
                rwDocSumOpl[_rwd.Value] = 0M;
            }
        }

        private bool CanSelectDeselectRwDoc(Selectable<RwDocViewModel> _rwd)
        {             
            return _rwd == null ? false : _rwd.Value.Ostatok != 0 && (_rwd.IsSelected || OstToPay > 0 || _rwd.Value.Ostatok < 0);           
        }

        public override bool IsValid()
        {
            return base.IsValid() && Validate();
        }

        private decimal sumToPay = 0;

        public decimal SumToPay
        {
            get { return sumToPay; }
            set { SetAndNotifyProperty("SumToPay", ref sumToPay, value); }
        }

        public decimal OstToPay
        {
            get { return rwplat.Ostatok - sumToPay; }
        }       

        private bool Validate()
        {
            return sumToPay != 0 && OstToPay >= 0 && OstToPay <= rwplat.Sumplat;
        }

        public RwPayActionViewModel[] GetPayActions()
        {            
            RwPayActionViewModel[] res = null;
            if (sumToPay != 0)
                res = rwDocSumOpl.Where(kv => kv.Value != 0M).Select(kv => new RwPayActionViewModel(Models.RwPayActionType.PayUsl)
                {
                    IdRwPlat = rwplat.Idrwplat,
                    IdRwList = kv.Key.Id_rwlist,
                    IdDoc = kv.Key.Id_rwdoc,
                    Summa = kv.Value,
                    Notes = null,
                    NumPlat = rwplat.Numplat.ToString(),
                    NumDoc = kv.Key.Num_doc
                }).ToArray();
            return res;
        }
    }
}
