using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора погашений платежей на счета-фактуры для отмены.
    /// </summary>
    public class SelectPaysForUndoDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private Selectable<PayAction>[] payActions;

        public SelectPaysForUndoDlgViewModel(IDbService _repository, PayAction[] _pa)
        {
            repository = _repository;

            payActions = _pa.GroupBy(pa => new
            {
                PayActionTYpe = pa.PayActionType,
                IdPo = pa.IdPo,
                Ndoc = pa.Ndoc,
                DatDoc = pa.DatDoc,
                Idsf = pa.Idsf,
                Numsf = pa.Numsf,
                DatPltr = pa.DatPltr,
                //IdPrilsf = pa.IdPrilsf,
                //PayGroupId  = (byte)pa.PayGroupId,
                Whatfor = pa.Whatfor,
                //SumOpl = pa.SumOpl ?? 0,
                PayTime = pa.PayTime,
                KodVal = pa.KodVal,
                DatOpl = pa.DatOpl
            })
            .OrderBy(g => g.Key.PayTime)
            .Select(g => new Selectable<PayAction>(
                                                new PayAction
                                                {
                                                    PayActionType = g.Key.PayActionTYpe,
                                                    IdPo = g.Key.IdPo,
                                                    Ndoc = g.Key.Ndoc,
                                                    DatDoc = g.Key.DatDoc,
                                                    Idsf = g.Key.Idsf,
                                                    Numsf = g.Key.Numsf,
                                                    DatPltr = g.Key.DatPltr,
                                                    //IdPrilsf = pa.IdPrilsf,
                                                    //PayGroupId  = (byte)pa.PayGroupId,
                                                    PayTime = g.Key.PayTime,
                                                    Whatfor = g.Key.Whatfor,
                                                    SumOpl = g.Sum(i => i.SumOpl),
                                                    KodVal = g.Key.KodVal,
                                                    DatOpl = g.Key.DatOpl
                                                }, false))
                            .ToArray();
        }

        public Selectable<PayAction>[] PayActions
        {
            get { return payActions; }
        }

        public PayAction[] SelectedPayActions
        {
            get
            {
                return payActions.Where(spa => spa.IsSelected)
                                 .Select(spa => spa.Value)
                                 .OrderByDescending(pa => pa.PayTime)
                                 .ToArray();
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && payActions.Any(pa => pa.IsSelected)
                && CheckPredopls();
        }

        private bool CheckPredopls()
        {
            var predSums = SelectedPayActions.Where(pa => pa.IdPo != 0).GroupBy(pa => pa.IdPo).ToDictionary(g => g.Key, g => g.Sum(i => i.SumOpl));
            foreach (var kv in predSums)
            { 
                var pred = repository.GetPredoplById(kv.Key);
                if (pred == null || pred.Direction != 0) return true;
                var newSumOtgr = pred.SumOtgr - kv.Value;
                if (newSumOtgr > pred.SumPropl || newSumOtgr < 0) return false;
            }
            return true;
        }

    }
}
