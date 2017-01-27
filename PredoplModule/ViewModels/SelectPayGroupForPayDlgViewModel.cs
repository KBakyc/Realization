using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;
using CommonModule.DataViewModels;
using System.Windows.Input;

namespace PredoplModule.ViewModels
{
    public class SelectPayGroupForPayDlgViewModel : BaseDlgViewModel
    {
        private List<SfPayOstViewModel> grouposts;
        private SfPayOstViewModel selectedMode;
        private int[] idsfs;
        private IEnumerable<PayAction> curPayActions;
        private IDbService repository;

        public SelectPayGroupForPayDlgViewModel(IDbService _rep, PredoplViewModel _predopl, IEnumerable<int> _idsfs, IEnumerable<PayAction> _payActions)
        {
            repository = _rep;
            idsfs = _idsfs.ToArray();
            Predopl = _predopl;
            curPayActions = _payActions;
            InitProductOsts();
            InitPayGroupChoices();            
        }

        public SelectPayGroupForPayDlgViewModel(IDbService _rep, PredoplViewModel _predopl, IEnumerable<int> _idsfs)
            :this(_rep, _predopl, _idsfs, null)
        {
        }

        public ProductOstViewModel[] SelectedProductOsts
        {
            get { return productOsts.Where(po => po.IsSelected).Select(po => po.Value).ToArray(); }
        }

        private ICommand doCheckAll;
        public ICommand DoCheckAll 
        { 
            get 
            {
                if (doCheckAll == null) doCheckAll = new DelegateCommand<bool>(ExecDoCheckAll);
                return doCheckAll; 
            } 
        }

        private void ExecDoCheckAll(bool _ischeck)
        {
            var pOsts = productOsts.Where(o => o.IsSelected != _ischeck && o.Value.IsCanBePayed).ToArray();
            foreach (var po in pOsts)
            {
                po.PropertyChanged -= ProductOstPropertyChanged;
                po.IsSelected = _ischeck;
                po.PropertyChanged += ProductOstPropertyChanged;
            }
            InitPayGroupChoices();
            NotifyPropertyChanged("GroupOsts");
        }

        private Selectable<ProductOstViewModel>[] productOsts;
        public Selectable<ProductOstViewModel>[] ProductOsts
        {
            get
            {
                if (productOsts == null)
                    InitProductOsts();
                return productOsts;
            }
        }

        private Selectable<ProductOstViewModel>[] GetProductOsts()
        {
            List<Selectable<ProductOstViewModel>> res = new List<Selectable<ProductOstViewModel>>();
            int idsf = 0;
            for (int i = 0; i < idsfs.Length; i++)
            {
                idsf = idsfs[i];
                if (repository != null && idsf > 0)
                {
                    var newdata = repository.GetSfProducts(idsf)
                                            .Select(p => new ProductOstViewModel(repository, p))
                                            .ToArray();
                    res.AddRange(newdata.Select(po => new Selectable<ProductOstViewModel>(po, po.IsCanBePayed)));
                }
            }
            return res.ToArray();
        }

        private void InitProductOsts()
        {
            productOsts = GetProductOsts();
            foreach (var po in productOsts)
            {
                if (curPayActions != null && curPayActions.Any())
                {
                    var curPoPayActions = curPayActions.Where(pa => pa.PayActionType == PayActionTypes.Sf && pa.IdPrilsf == po.Value.IdPrilSf);
                    foreach (var pa in curPoPayActions)
                    {                        
                        var payosts = po.Value.PayOsts.Where(o => (pa.PayGroupId == 0 || pa.PayGroupId == o.PayGroupId)
                                                                 && (pa.PayType == 0 || pa.PayType == o.PayType)
                                                                 && o.Summa != 0);
                        var alreadypayed = pa.SumOpl;
                        var sumost = alreadypayed;
                        foreach (var ost in payosts)
                        {
                            //if (ost.Summa < sumost)
                            if (System.Math.Abs(ost.Summa) < System.Math.Abs(sumost))
                            {
                                sumost -= ost.Summa;
                                ost.Summa = 0;
                            }
                            else
                            {
                                ost.Summa -= sumost;
                                sumost = 0;
                            }
                            if (sumost == 0) break;
                        }
                    }
                }

                po.PropertyChanged += ProductOstPropertyChanged;
            }
        }

        private void ProductOstPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                InitPayGroupChoices();
                NotifyPropertyChanged("GroupOsts");
            }
        }

        private void InitPayGroupChoices()
        {
            if (ProductOsts == null) return;

            var allpays = ProductOsts.Where(po => po.IsSelected).SelectMany(p => p.Value.PayOsts);
            var allgroups = allpays.GroupBy(go => go.PayGroupId).OrderBy(g => g.Key);

            grouposts = new List<SfPayOstViewModel>();

            foreach(var g in allgroups)
            {
                SfPayOst pgo = new SfPayOst{
                    PayGroupId = g.Key,
                    Summa = g.Sum(i => i.Summa)
                };
                
                var allpaysingroup = g.GroupBy(gp => gp.PayType);

                if (allpaysingroup.Count() > 1)
                    grouposts.Add(new SfPayOstViewModel(repository, pgo));

                foreach (var p in allpaysingroup)
                {
                    SfPayOst po = new SfPayOst
                    {
                        PayGroupId = g.Key,
                        PayType = p.Key,
                        Summa = p.Sum(i => i.Summa)
                    };
                    grouposts.Add(new SfPayOstViewModel(repository, po));
                }
            }

            if (allgroups.Count() > 1)
            {
                var allPaysOst = new SfPayOst
                {
                    Summa = allpays.Sum(o => o.Summa)
                };
                var allChoice = new SfPayOstViewModel(repository, allPaysOst) { PayName = "Всё" };
                grouposts.Insert(0, allChoice);
            }

            SelectedMode = grouposts.Count > 0 ? grouposts[0] : null;
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && grouposts != null && grouposts.Count > 0 && selectedMode != null 
                && (SumOpl > 0 && SumOpl <= selectedMode.Summa && SumOpl <= Predopl.Ostatok
                 || SumOpl < 0 && SumOpl >= selectedMode.Summa && Predopl.SumOtgr + SumOpl >= 0
                );
        }

        public PredoplViewModel Predopl { get; set; }

        public List<SfPayOstViewModel> GroupOsts
        {
            get { return grouposts; }
        }

        public SfPayOstViewModel SelectedMode
        {
            get { return selectedMode; }
            set 
            { 
                SetAndNotifyProperty("SelectedMode", ref selectedMode, value);
                if (value != null)
                {
                    if (value.Summa > 0)
                        SumOpl = value.Summa > Predopl.Ostatok ? Predopl.Ostatok : value.Summa;
                    else
                        SumOpl = value.Summa + Predopl.SumOtgr <= 0 ? -Predopl.SumOtgr : value.Summa;
                }
                else
                    SumOpl = 0;
            }
        }

        private decimal sumOpl;
        public decimal SumOpl
        {
            get { return sumOpl; }
            set { SetAndNotifyProperty("SumOpl", ref sumOpl, value); }
        }
    }
}
