using CommonModule.Commands;
using CommonModule.ViewModels;
using CommonModule.Composition;
using DataObjects.Interfaces;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для просмотра курсов валют.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 3f)]
    public class KursesModuleCommand : ModuleCommand
    {
        public KursesModuleCommand()
        {
            Label = "Курсы валют";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            var dlg = MakeParamDlg();

            Parent.OpenDialog(dlg);
        }

        private BaseCompositeDlgViewModel MakeParamDlg()
        {
            BaseCompositeDlgViewModel res = new BaseCompositeDlgViewModel 
            {
                Title = "Параметры отбора",
                OnSubmit = DoShowKurses
            };


            var valSel = new ValSelectionViewModel(Parent.Repository, v=>v.Kodval != "RB") 
            {
                Name = "ValSel",
                Title = "Валюта"
            };

            var dateSel = new DateDlgViewModel() 
            {
                Name = "DateSel",
                Title = "На дату"
            };

            res.Add(valSel);
            res.Add(dateSel);

            return res;
        }

        private void DoShowKurses(object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var parDlg = _dlg as BaseCompositeDlgViewModel;
            var valSel = parDlg.DialogViewModels[0] as ValSelectionViewModel;
            var dateSel = parDlg.DialogViewModels[1] as DateDlgViewModel;

            if (dateSel.SelDate.HasValue)
            {
                var kursesDlg = new KursesListViewModel(Parent.Repository, valSel.SelVal.Kodval, dateSel.SelDate.Value)
                {
                    Title = "Курсы валюты на дату",
                    OnSubmit = (d) => Parent.CloseDialog(d)
                };
                Parent.OpenDialog(kursesDlg);
            }
        }
    }
}
