using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using RwModule.ViewModels;
using RwModule.Models;
using CommonModule.Composition;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда принятия предоплат из банка
    /// </summary>

    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 10f)]
    public class PayRwUslCommand : ModuleCommand
    {
        public PayRwUslCommand()
        {
            Label = "Погашение услуг платежами";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<PayRwUslByPlatViewModel>(null)) return;

            var chDlg = new ChoicesDlgViewModel(
                new Choice { GroupName = "Тип платежей", Header = "Провозные платежи", IsSingleInGroup = true, Item = RwUslType.Provoz },
                new Choice { GroupName = "Тип платежей", Header = "Доп. сборы", IsSingleInGroup = true, Item = RwUslType.DopSbor }) 
            {
                Title = "Тип услуг",
                Name = "VIDUSL"
            };

            var dDlg = new DateDlgViewModel()
            {                
                DateLabel = "Дата закрытия",
                Name = "DZAKR",
                MaxDate = DateTime.Today               
            };

            var nDlg = new BaseCompositeDlgViewModel()
            {
                Title = "Параметры погашения",
                OnSubmit = Submit            
            };

            nDlg.Add(chDlg);
            nDlg.Add(dDlg);

            Parent.OpenDialog(nDlg);
        }

        private void Submit(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            
            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var dDlg = dlg.GetByName<DateDlgViewModel>("DZAKR");            
            var dzakr = dDlg.SelDate ?? DateTime.Today;

            var chDlg = dlg.GetByName<ChoicesDlgViewModel>("VIDUSL");
            var selChoise = chDlg.Groups.Values.SelectMany(ca => ca).FirstOrDefault(cvm => cvm.IsChecked ?? false);
            if (selChoise == null) return;
            var vidusl = (RwUslType)selChoise.Item;

            Action work = () =>
            {                
                var nContent = new PayRwUslByPlatViewModel(Parent, dzakr, vidusl);
                nContent.TryOpen();
            };

            Parent.Services.DoWaitAction(work);
        }
    }
}
