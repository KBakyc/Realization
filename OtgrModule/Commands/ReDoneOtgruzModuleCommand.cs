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
    /// <summary>
    /// Команда модуля для перевыставление отгрузки на другой договор
    /// </summary>
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder = 1.9f)]
    public class ReDoneOtgruzModuleCommand : ModuleCommand
    {
        private KontrAgent selKontrAgent;
        private PoupModel selPoup;
        private DateTime selDate1;
        private DateTime selDate2;
        private PDogInfoViewModel selInPDogInfo;
        private PDogInfoViewModel selOutPDogInfo;
        private OtgrLineViewModel[] selOtgr;

        public ReDoneOtgruzModuleCommand()
        {
            Label = "Перевыставление отгрузки на другой договор";
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

            Parent.OpenDialog(new KaSelectionViewModel(Parent.Repository)
            {
                Title = "Выбор контрагента",
                OnSubmit = SelectInDog
            });

        }

        /// <summary>
        /// После выбора контрагента
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectInDog(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as KaSelectionViewModel;
            if (dlg == null) return;

            selKontrAgent = dlg.SelectedKA;

            var today = DateTime.Today;

            var newdlg = new BaseCompositeDlgViewModel()
            {
                Title = "Выбор договора для отбора отгрузки",
                OnSubmit = SelectOutDog
            };

            var inDog = new PDogSelectViewModel(Parent.Repository)
            {
                SelKa = selKontrAgent,
                FromDate = new DateTime(today.Year - 2, 1, 1)
            };

            Choice toA = new Choice() { GroupName = "Перенести на", Header = "Перенести на позднее", IsChecked = true, IsSingleInGroup = true};
            Choice toB = new Choice() { GroupName = "Перенести на", Header = "Перенести на раннее", IsChecked = false, IsSingleInGroup = true};
            Choice onlyC = new Choice() { GroupName = "Перенести на", Header = "Исправить цену", IsChecked = false, IsSingleInGroup = true };
            Choice chPoup = new Choice() { GroupName = "Перенести на", Header = "Изменить напр. реализации", IsChecked = false, IsSingleInGroup = true };

            var dopDlg = new ChoicesDlgViewModel(toA, toB, onlyC, chPoup)
            {
                Title = "Дополнительно"
            };

            newdlg.Add(inDog);
            newdlg.AddHidable(dopDlg, true);

            Parent.OpenDialog(newdlg);
        }

        /// <summary>
        /// После выбора первого договора
        /// (выбор договора для счёта)
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectOutDog(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var dogDlg = dlg.DialogViewModels[0] as PDogSelectViewModel;
            if (dogDlg == null) return;

            var dopDlg = dlg.DialogViewModels[1] as ChoicesDlgViewModel;
            if (dopDlg == null) return;

            bool isToA = true;
            bool isOnlyCena = false;
            bool isChangePoup = false;
            var toGroup = dopDlg.Groups.FirstOrDefault(g => g.Key == "Перенести на").Value;
            if (toGroup != null && toGroup.Length > 0)
                isToA = toGroup[0].IsChecked ?? false;

            if (!isToA)
            {
                isOnlyCena = toGroup[2].IsChecked ?? false;
                if (!isOnlyCena)
                    isChangePoup = toGroup[3].IsChecked ?? false;
            }

            selInPDogInfo = dogDlg.SelPDogInfo;
            selPoup = dogDlg.SelPoup;

            if (isOnlyCena)
            {
                selOutPDogInfo = selInPDogInfo;
                SelectDaterangeStep();
                return;
            }
            
            if (isChangePoup)
            {        
                var oldPoup = selInPDogInfo.ModelRef.Poup;
                var npDlg = new PoupSelectionViewModel(Parent.Repository)
                {
                    Title = "Выберите новое направление реализации",
                    OnSubmit = SelectOupPoupDog
                };
                npDlg.SetCheck(d => d.SelPoup != null && d.SelPoup.Kod != oldPoup);

                Parent.OpenDialog(npDlg);
                return;
            }

            IEnumerable<PDogInfoViewModel> schExpr = dogDlg.CachedPDogs.Where(i => BusinessLogicHelper.IsSortEqual(i.Kprod, selInPDogInfo.ModelRef.Kprod) 
                                                                && i.Iddog != selInPDogInfo.ModelRef.Iddog)
                                                                .Select(m => new PDogInfoViewModel(m, Parent.Repository));            

            if (!isToA)
                schExpr = schExpr.Where(vm => vm.PDogDate <= selInPDogInfo.PDogDate);
            else
                schExpr = schExpr.Where(vm => vm.PDogDate >= selInPDogInfo.PDogDate);

            var vModels = schExpr.OrderBy(i => (i.ModelRef.Osn == selInPDogInfo.ModelRef.Osn ? 0 : 1))
                                 .ThenBy(i => i.ModelRef.Datd).ThenBy(i => i.ModelRef.Osn).ThenBy(i => i.PDogDate)
                                 .ToArray();

            Parent.OpenDialog(new PDogListViewModel(Parent.Repository)
            {
                Title = "Выбор договора для перевыставления",
                PDogInfos = vModels,
                OnSubmit = SelectDaterange
            });
        }

        private void SelectOupPoupDog(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as PoupSelectionViewModel;
            if (dlg == null) return;


            var newPoup = dlg.SelPoup;

            var today = DateTime.Today;

            PDogInfoViewModel[] newPDogs = null;

            Action loadNewPdogs = () => 
            {
                var data = Parent.Repository.GetPDogInfosByKaPoup(selKontrAgent.Kgr, newPoup.Kod, 0);
                if (data != null && data.Length > 0)
                    newPDogs = data.Where(d => d.Kprod == selInPDogInfo.ModelRef.Kprod && d.Osn == selInPDogInfo.ModelRef.Osn)
                                   .Select(d => new PDogInfoViewModel(d, Parent.Repository))
                                   .ToArray();
            };

            Action afterLoad = () => 
            {
                if (newPDogs != null && newPDogs.Length > 0) 
                    Parent.OpenDialog(new PDogListViewModel(Parent.Repository)
                    {
                        Title = "Выбор договора для перевыставления",
                        PDogInfos = newPDogs,
                        OnSubmit = SelectDaterange
                    });
                else 
                    Parent.Services.ShowMsg("Не найден договор", "Договор для перевыставления\nна направление реализации:\n" + newPoup.Name + "\nне найден!", true);
            };

            Parent.Services.DoWaitAction(loadNewPdogs, "Подождите", "Поиск договоров для перевыставления", afterLoad);
        }

        /// <summary>
        /// После выбора договора
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectDaterange(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as PDogListViewModel;
            if (dlg == null) return;
            
            selOutPDogInfo = dlg.SelPDogInfo;

            SelectDaterangeStep();
        }

        private void SelectDaterangeStep()
        {
            Parent.OpenDialog(new DateRangeDlgViewModel()
            {
                Title = "Укажите диапазон дат отгрузки",
                OnSubmit = SelectNakls
            });
        }

        /// <summary>
        /// После выбора интервала дат
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectNakls(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as DateRangeDlgViewModel;
            if (dlg == null) return;

            selDate1 = dlg.DateFrom;
            selDate2 = dlg.DateTo;
            GetOtgrDocs(selInPDogInfo.ModelRef.Kdog, selDate1, selDate2);
        }


        private void GetOtgrDocs(int _kdog, DateTime _from, DateTime _to)
        {
            Action work = () =>
                {
                    var oDocs = Parent.Repository.GetOtgrArc(new OtgruzSearchData { Kdog = _kdog, Dfrom = _from, Dto = _to });
                    if (oDocs != null && oDocs.Length > 0)
                        Parent.OpenDialog(new ChangeOtgrForReDoneViewModel(Parent.Repository, oDocs)
                        {
                            Title = "Отгрузка для перевыставления",
                            DateFrom = _from,
                            DateTo = _to,
                            InPdogInfo = selInPDogInfo,
                            OutPdogInfo = selOutPDogInfo,
                            OnSubmit = DoSelectedOtgrReDone
                        });
                    else
                        Parent.Services.ShowMsg("Внимание!", "Не найдена отгрузка\nпо договору " + selInPDogInfo.TextOsn + "\nза период с " + selDate1.ToString("dd.MM.yy") + " по " + selDate2.ToString("dd.MM.yy"), true);
                };

            Parent.Services.DoWaitAction(work, "Выборка документов", "Обработка данных");
        }

        /// <summary>
        /// После выбора накладных
        /// </summary>
        /// <param name="_dlg"></param>
        private void DoSelectedOtgrReDone(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as ChangeOtgrForReDoneViewModel;
            if (dlg == null) return;

            selOtgr = dlg.OtgrData.Where(o => o.TrackingState != TrackingInfo.Unchanged).ToArray();

            var newdlg = new MsgDlgViewModel
            {
                Title = "Перевыставление выбранной отгрузки",
                Message = "С договора № " + dlg.InPdogInfo.TextOsn + " от " + dlg.InPdogInfo.PDogDate.ToString("dd.MM.yyyy") + Environment.NewLine
                        + "На договор № " + dlg.OutPdogInfo.TextOsn + " от " + dlg.OutPdogInfo.PDogDate.ToString("dd.MM.yyyy"),
                OnSubmit = DoRedone
            };

            Parent.OpenDialog(newdlg);
        }


        private void DoRedone(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            Action work = () =>
            {
                for (int i = 0; i < selOtgr.Length; i++)
                {
                    var otgr = selOtgr[i];
                    switch(otgr.TrackingState)
                    {
                        case TrackingInfo.Created: 
                            AddOtgruzAction(otgr);
                            break;
                        case TrackingInfo.Updated: 
                            UpdateOtgruzAction(otgr);
                            break;
                        default:
                            break;
                    }
                }
            };

            Action after = () =>
            {
                var errOtgr = selOtgr.Where(o => o.TrackingState != TrackingInfo.Unchanged);

                bool isErr = false;
                String resMess = null;

                if (errOtgr.Any())
                {
                    resMess = "Ошибка при перевыставлении следующей отгрузки:\n"
                                   + String.Join("\n", errOtgr.Select(o => String.Format("Накл:{0} Ваг:{1} Дата:{2:dd.MM.yyyy} Кол:{3:N5} Цена:{4:N2}", o.DocumentNumber, o.Nv, o.Datgr, o.Kolf, o.Cena)).ToArray());
                    isErr = true;
                }
                else
                    resMess = "Операция перевыставления отгрузки завершена успешно";

                List<OtgrLine> redoned = new List<OtgrLine>();
                foreach(var ro in selOtgr.Where(o => o.TrackingState == TrackingInfo.Unchanged))
                    redoned.Add(Parent.Repository.GetOtgrLine(ro.Otgr.Idp623, true));

                var ncontent = new OtgrArcViewModel(Parent, redoned) 
                {
                    Title = "Перевыставленная отгрузка"
                };
                ncontent.TryOpen();

                Parent.Services.ShowMsg("Результат перевыставления отгрузки", resMess, isErr);
            };

            Parent.Services.DoWaitAction(work, "Перевыставление отгрузки", "Обработка данных", after);

        }

        private bool UpdateOtgruzAction(OtgrLineViewModel otgr)
        {
            bool res = Parent.Repository.UpdateOtgruz(otgr.Otgr, "Изменение в режиме перевыставления на другой договор");
            if (res) otgr.TrackingState = TrackingInfo.Unchanged;
            return res;
        }

        private bool AddOtgruzAction(OtgrLineViewModel _otgr)
        {
            var otgr = _otgr.Otgr;
            var logDescr = String.Format("Отделена от отгрузки [idp623 = {0}] в режиме перевыставления на другой договор", otgr.Idp623);
            otgr.Idp623 = 0;
            bool res = Parent.Repository.AddOtgruz(otgr, logDescr);
            if (res) otgr.TrackingState = TrackingInfo.Unchanged;
            return res;
        }



    }
}
