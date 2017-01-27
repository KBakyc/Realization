using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ModuleServices;
using CommonModule.Interfaces;
using DataObjects;
using CommonModule.ViewModels;
using CommonModule.Helpers;
using PredoplModule.ViewModels;

namespace PredoplModule.Helpers
{
    public enum PredoplAddKind {New = 1, Copy = 5, Cut = 6 }

    public class PredoplService : BaseModuleService//, IPredoplServices
    {
        public PredoplService(IModule _parent)
            :base(_parent)
        {
        }

        public void DoAddPredopl(PredoplModel _newModel, PredoplAddKind _aKind)
        {
            bool result;
            var inserted = Parent.Repository.PredoplInsert(_newModel, (int)_aKind, out result);

            string mes = null;
            if (result && inserted != null)
            {
                mes = String.Format("Добавлена предоплата {0}", inserted.Idpo);
                var npEnumeration = Enumerable.Repeat(inserted, 1);
                var lContent = Parent.GetLoadedContent<PredoplsArcViewModel>(c => c.Title == "Добавленная предоплата") as PredoplsArcViewModel;
                if (lContent != null)
                {
                    var nplist = lContent.PredoplsList.Predopls.Select(pvm => pvm.PredoplRef).Union(npEnumeration).ToArray();
                    lContent.PredoplsList.LoadData(nplist);
                }
                else
                {
                    var nContent = new PredoplsArcViewModel(Parent, Enumerable.Repeat(inserted, 1)) { Title = "Добавленная предоплата" };
                    nContent.TryOpen();
                }
            }
            else
                mes = "Ошибка при добавлении предоплаты!";

            ShowMsg("Информация", mes, !result);
        }

        //public void ClosePredopl(int _idpo, Action _continuation)
        //{
        //    var predopl = Parent.Repository.GetPredoplById(_idpo);
        //    if (predopl == null || predopl.DatZakr != null) ShowError("ОШИБКА", "Предоплата уже закрыта!");            
        //    var ostatok = predopl.SumPropl - predopl.SumOtgr;

        //    var askdlg = new DateDlgViewModel
        //    {
        //        Title = String.Format("Списать остаток предоплаты?\nОстаток: {0} {1}", ostatok, predopl.KodVal),
        //        SelDate = DateTime.Now,
        //        OnSubmit = (o) => DoClosePredopl(o, _idpo, _continuation)
        //    };

        //    Parent.OpenDialog(askdlg);
        //}

        //private void DoClosePredopl(Object _dlg, int _idpo, Action _continuation)
        //{
        //    Parent.CloseDialog(_dlg);
        //    var dlg = _dlg as DateDlgViewModel;
        //    if (dlg == null) return;

        //    var datzakr = dlg.SelDate;
        //    Action work = () =>
        //    {
        //        ClosePredoplAction(_idpo, datzakr);
        //        _continuation();
        //    };

        //    DoWaitAction(work, "Подождите", "Списание остатков и закрытие предоплаты");
        //}

        //private void ClosePredoplAction(int _idpo, DateTime _datzakr)
        //{ 
        //    bool res = Parent.Repository.ClosePredopl(_idpo, _datzakr);
        //    if (!res)
        //        ShowError("ОШИБКА", "Произошла ошибка при закрытии предоплаты!");
        //}


        

    }
}
