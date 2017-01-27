using CommonModule.Commands;
using CommonModule.Composition;
using OtgrModule.ViewModels;
using CommonModule.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;
using DataObjects;

namespace OtgrModule.Commands
{
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 3.9f)]
    public class SyncOtgrBySlivModuleCommand : ModuleCommand
    {
        public SyncOtgrBySlivModuleCommand()
        {
            Label = "Сверить отгрузку по акту слива";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            var dlg = new BaseCompositeDlgViewModel()
            {
                Title = "Укажите акт слива",
                OnSubmit = OnSubmitAkt
            };

            var num_dlg = new TxtDlgViewModel 
            {
                Title = "Номер"
            };

            var dat_dlg = new DateDlgViewModel 
            {
                Title = "Дата"
            };

            dlg.Add(num_dlg);
            dlg.Add(dat_dlg);

            Parent.OpenDialog(dlg);
        }
        
        private void OnSubmitAkt(object _dlg)
        {
            var dlg = _dlg as BaseCompositeDlgViewModel;
            Parent.CloseDialog(_dlg);

            var nakt = (dlg.DialogViewModels[0] as TxtDlgViewModel).Text;
            var dakt = (dlg.DialogViewModels[1] as DateDlgViewModel).SelDate;
            if (dakt == null) return;

            Action work = () => 
            {
                var data = Parent.Repository.GetOtgrByAktSliv(nakt, dakt.Value);
                if (data == null || data.Count == 0)
                    Parent.Services.ShowMsg("Результат", "Данные по указанному акту слива не найдены", true);
                else
                    ShowData(data, nakt, dakt.Value);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Выборка данных по акту");
        }

        private void ShowData(Dictionary<DataObjects.OtgrLine, decimal> _data, string _nakt, DateTime _dakt)
        {
            var asdlg = new ShowAktSlivViewModel(_data)
            {
                Title = String.Format("Данные по акту № {0} от {1:dd.MM.yy}", _nakt, _dakt),
                OnSubmit = SubmitChanges
            };

            Parent.OpenDialog(asdlg);
        }

        private void SubmitChanges(object _dlg)
        {
            var dlg = _dlg as ShowAktSlivViewModel;
            Parent.CloseDialog(_dlg);

            var data = dlg.DataInAkt;//.Where(d => d.Key.IsChecked); // обновляем все, даже с корректным кол-вом, чтобы обновить дату слива, т.к. при первоначальной приёмке она не проставляется

            if (!data.Any()) return;

            Action work = () =>
            {
                bool res = true;
                OtgrLine otgr = null;
                foreach (var d in data)
                {
                    otgr = d.Key;
                    var newkolf = d.Value;
                    res = Parent.Repository.UpdateOtgrOnSliv(otgr.Idrnn, otgr.Datgr, newkolf, otgr.Datnakl);
                    if (!res) break;
                }
                
                Parent.Services.ShowMsg("Ошибка", res ? "Обновление отгрузки завершено успешно." 
                                                        : String.Format("Ошибка при обновлении отгрузки\n ЖД накладная: {0}, вагон: {1}", otgr.RwBillNumber, otgr.Nv),
                                                        true);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Обновление отгрузки");
        }

    }
}
