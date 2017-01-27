using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.Helpers;
using OtgrModule.ViewModels;
using System.Collections.Generic;
using DataObjects.SeachDatas;

namespace OtgrModule.Commands
{
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 1.95f)]
    public class CorrSperByPerechModuleCommand : ModuleCommand
    {
        private DataObjects.Interfaces.IDbService repository;

        public CorrSperByPerechModuleCommand()
        {
            Label = "Изменение провозных платежей на основании ЖД перечня";
            GroupName = "Перевыставление отгрузки";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            repository = Parent.Repository;

            var ndlg = new BaseCompositeDlgViewModel
            {
                Title = "Приём данных ЖД",
                OnSubmit = CollectRWData
            };

            ndlg.Add(new NumDlgViewModel { Title = "Номер ЖД перечня" });
            ndlg.Add(new NumDlgViewModel { Title = "Год", Number = DateTime.Today.Year });

            Parent.OpenDialog(ndlg);
        }

        private int rwListId;
        private int rwListNum;
        private int rwListYear;
        private OtgrLine[] rwListData;
        private List<OtgrLine> otgrData;


        private void CollectRWData(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            rwListNum = (dlg.DialogViewModels[0] as NumDlgViewModel).IntValue;
            rwListYear = (dlg.DialogViewModels[1] as NumDlgViewModel).IntValue;
            
            GetRwPerechData();
        }

        private void GetRwPerechData()
        {
            Action work = () =>
                {
                    rwListData = repository.GetRwListData(rwListNum, rwListYear);

                    if (rwListData == null || rwListData.Length == 0)
                    {
                        Parent.Services.ShowMsg("Результат выбоки", String.Format("Данных по ЖД перечню № {0} за {1} год не найдено.", rwListNum, rwListYear), true);
                        return;
                    }

                    rwListId = (int)rwListData[0].Idp623;

                    var newDlg = new BaseCompositeDlgViewModel()
                    {
                        Title = "Выберите накладные для изменения",
                        OnSubmit = SubmitSelectNakl
                    };

                    var selRwDataDlg = new SelectOtgrFromRwListViewModel(repository, rwListData) 
                    { 
                        //Title = "Выберите накладные для изменения",
                        //OnSubmit = SubmitSelectNakl
                    };

                    var optDlg = new ChoicesDlgViewModel(new Choice { Header = "Автоматическое распределение суммы по вагонам", IsChecked = true, IsSingleInGroup = false, Name = "IsAuto"})
                    {
                        Title = "Дополнительно"
                    };

                    newDlg.Add(selRwDataDlg);
                    newDlg.Add(optDlg);

                    Parent.OpenDialog(newDlg);

                    //for (int i = 0; i < rwListData.Length; i++)
                    //    otgrData.AddRange(repository.GetOtgrArc(rwListData[i].Rnn, otgrFrom, otgrTo, (short)TransportTypes.Railway));
                };

            Parent.Services.DoWaitAction(work, "Выборка документов", "Обработка данных");
        }

        private void SubmitSelectNakl(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var cDlg = dlg.DialogViewModels[1] as ChoicesDlgViewModel;
            if (cDlg == null) return;
            var isAutoChoice = cDlg.GetChoiceByName("IsAuto");
            var isAuto = isAutoChoice == null ? false : (isAutoChoice.IsChecked ?? false);
            
            Action work = () =>
                {
                    otgrData = new List<OtgrLine>();
                    var checkedRwListData = rwListData.Where(d => d.IsChecked);
                    foreach (var rwd in checkedRwListData)
                        otgrData.AddRange(repository.GetOtgrArc(new OtgruzSearchData { RwBillNumber = rwd.RwBillNumber, Dfrom = rwd.Datgr, Dto = rwd.Datgr, Transportid = (short)TransportTypes.Railway }));

                    var corrOtgrDlg = new ChangeOtgrByRwListViewModel(repository, otgrData, checkedRwListData, isAuto) 
                    { 
                        Title = "Изменение данных по провозным платежам",
                        OnSubmit = SubmitEdit
                    };
                    Parent.OpenDialog(corrOtgrDlg);                   
                };

            Parent.Services.DoWaitAction(work, "Выборка документов", "Обработка данных");

        }

        private void SubmitEdit(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as ChangeOtgrByRwListViewModel;
            if (dlg == null) return;

            var chOtgr = dlg.GetOtgrForUpdate().ToArray();
            List<OtgrLine> errOtgr = new List<OtgrLine>();

            Action work = () =>
            {
                for (int i = 0; i < chOtgr.Length; i++)
                {
                    var cOtgr = chOtgr[i];
                    bool uRes = repository.UpdateOtgrByRwList(rwListId, rwListNum, cOtgr);
                    if (!uRes) errOtgr.Add(cOtgr);
                }
            };

            Action after = () => 
            {
                if (errOtgr.Count > 0)
                    ShowUpdateErrors(errOtgr);
                else
                    Parent.Services.ShowMsg("Результат", "Провозные платежи в выбранной отгрузке\n по перечню № " + rwListNum.ToString() + " обновлены успешно.", false);
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Обновление отгрузки...", after);
        }

        private void ShowUpdateErrors(IEnumerable<OtgrLine> _errOtgr)
        {
            string errMsg = "Ошибка обновления провозной платы в следующих записях:\n" + String.Join("\n", _errOtgr.Select(o => String.Format("ЖД накладная: {0} Вагон: {1}", o.RwBillNumber, o.Nv)).ToArray());
            Parent.Services.ShowMsg("Ошибка", errMsg, true);
        }
    }
}
