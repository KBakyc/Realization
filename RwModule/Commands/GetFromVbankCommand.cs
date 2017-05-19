using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using RwModule.ViewModels;
using RwModule.Models;
using CommonModule.Composition;
using DAL;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда модуля для запуска режима приёмки платежей по ЖД услугам из банка.
    /// </summary>   
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 10f)]
    public class GetFromVbankCommand : ModuleCommand
    {
        public GetFromVbankCommand()
        {
            Label = "Загрузка платежей по банку";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<GetRwPlatsViewModel>(null)) return;

            Action work = () =>
                {
                    var nDlg = new GetRwPlatsDlgViewModel(Parent.Repository)
                    {
                        OnSubmit = SubmitGetFromVbank
                    };
                    Parent.OpenDialog(nDlg);
                };
            Parent.Services.DoWaitAction(work);
        }

        private void SubmitGetFromVbank(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as GetRwPlatsDlgViewModel;
            if (dlg == null) return;

            var idbank = (byte)dlg.SelectedBank.Id;
            var dFrom = dlg.DatesSelection.DateFrom;
            var dTo = dlg.DatesSelection.DateTo;
            var rwusl = dlg.SelRwUslType;
            
            var pm = Parent as PagesModuleViewModel;
            if (pm != null)
                pm.RemoveSimilarContents<GetRwPlatsViewModel>();

            Action work = () =>
            {
                RwPlat[] res = null;
                using (var db = new RealContext())
                {
                    res = db.GetRwPlatsFromBank(dFrom, dTo, rwusl, idbank);
                }
                var nContent = new GetRwPlatsViewModel(Parent, res)
                {
                    DateFrom = dFrom,
                    DateTo = dTo,
                };
                nContent.TryOpen();
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Выборка поступлений из банка...");
        }
    }
}
