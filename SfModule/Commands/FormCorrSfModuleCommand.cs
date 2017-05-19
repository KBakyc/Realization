using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using SfModule.ViewModels;
using CommonModule.Helpers;
using DataObjects.Helpers;

namespace SfModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима формирования корректировочных счетов на продукцию.
    /// </summary>
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder=1.1f)]
    public class FormCorrSfModuleCommand : ModuleCommand
    {
        private KontrAgent selKontrAgent;
        private PoupModel selPoup;
        private DateTime selDate1;
        private DateTime selDate2;
        private PDogInfoViewModel selInPDogInfo;
        private PDogInfoViewModel selOutPDogInfo;
        private OtgrDocModel[] selDocs;
        private decimal selNewCena;

        public FormCorrSfModuleCommand()
        {
            Label = "Сформировать корректировочный счёт за продукцию";
            GroupName = "Формирование корректировочных счетов-фактур";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (!Parent.SelectContent<AcceptSfsViewModel>(null))
                CheckUnacceptedAndMakeNew();          
        }

        private void CheckUnacceptedAndMakeNew()
        {
            SfModel[] sfs = null;
            DateTime dateFrom = DateTime.Now;
            DateTime dateTo = dateFrom;
            int poup = 0;

            Action work = () =>
            {
                sfs = Parent.Repository.SelectUnacceptedSfs().OrderBy(s => s.NumSf).ToArray();
                if (sfs != null && sfs.Length > 0)
                {
                    DateRange dr = Parent.Repository.GetSfDateGrRange(sfs[0].IdSf);
                    dateFrom = dr.DateFrom;
                    dateTo = dr.DateTo;
                    poup = sfs[0].Poup;
                    for (int i = 1; i < sfs.Length; i++)
                    {
                        dr = Parent.Repository.GetSfDateGrRange(sfs[i].IdSf);
                        if (dr.DateFrom < dateFrom) dateFrom = dr.DateFrom;
                        if (dr.DateTo > dateTo) dateTo = dr.DateTo;
                    }
                }
            };

            Action afterwork = () =>
            {
                if (sfs == null || sfs.Length == 0)
                    Parent.OpenDialog(new KaSelectionViewModel(Parent.Repository)
                    {
                        Title = "Выбор контрагента",
                        OnSubmit = SelectInDog
                    });
                else
                {
                    var nContent = new AcceptSfsViewModel(Parent, sfs)
                    {
                        Title = "Сформировано",
                        SelectedPoup = Parent.Repository.Poups[poup],
                        DateFrom = dateFrom,
                        DateTo = dateTo
                    };
                    nContent.TryOpen();
                }
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Загрузка неподтверждённых счетов-фактур...", afterwork);
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

            var newDlg = new BaseCompositeDlgViewModel
            {
                Title = "Выбор договора для отбора отгрузки",
                OnSubmit = SelectOutDog
            };

            var dogDlg = new PDogSelectViewModel(Parent.Repository)
            {
                SelKa = selKontrAgent
            };
            newDlg.Add(dogDlg);

            var chError = new Choice { GroupName = "Режим формирования", Header = "Исправление", Info = "Исправление ошибочно выставленной цены отгрузки/услуги", IsChecked = false, IsSingleInGroup = true, Name = "ChError"};
            var chNewDog = new Choice { GroupName = "Режим формирования", Header = "Изменение", Info = "Изменение цены согласно новым условиям договора", IsChecked = true, IsSingleInGroup = true, Name = "ChNewDog" };
            var chDlg = new ChoicesDlgViewModel(chError, chNewDog)
            {
                Title = "Режим формирования"
            };
            newDlg.Add(chDlg);

            Parent.OpenDialog(newDlg);
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

            selInPDogInfo = dogDlg.SelPDogInfo;
            selPoup = dogDlg.SelPoup;

            var chDlg = dlg.DialogViewModels[1] as ChoicesDlgViewModel;
            if (chDlg == null) return;

            var chError = chDlg.GetChoiceByName("ChError");
            if (chError != null && (chError.IsChecked ?? false)) // режим исправления цены по прежнему договору
            {
                selOutPDogInfo = selInPDogInfo;
                SelectDaterange(null);
            }
            else // режим изменения цены по новым условиям договора
            {
                var vModels = dogDlg.CachedPDogs.Where(i => (i.DatAlterDog ?? i.Datdopdog ?? i.Datd) >= selInPDogInfo.PDogDate && BusinessLogicHelper.IsSortEqual(i.Kprod, selInPDogInfo.ModelRef.Kprod) && i.Iddog != selInPDogInfo.ModelRef.Iddog)
                                             .ToArray();

                Parent.OpenDialog(new PDogListViewModel(Parent.Repository, vModels)
                {
                    Title = "Выбор договора для корректировочного счёта",
                    //PDogInfos = vModels,
                    OnSubmit = SelectDaterange
                });
            }
        }

        /// <summary>
        /// После выбора договора
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectDaterange(Object _dlg)
        {
            if (_dlg != null)
            {
                Parent.CloseDialog(_dlg);

                var dlg = _dlg as PDogListViewModel;
                if (dlg != null)
                    selOutPDogInfo = dlg.SelPDogInfo;
            }

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

            if (dlg.DateFrom.Month != dlg.DateTo.Month || dlg.DateFrom.Year != dlg.DateTo.Year)
            {
                Parent.OpenDialog(new MsgDlgViewModel()
                {
                    Title = "Ошибка",
                    Message = "Диапазон дат должен быть в пределах одного месяца одного года",
                    OnSubmit = SelectDaterange
                });
            }
            else
            {
                selDate1 = dlg.DateFrom;
                selDate2 = dlg.DateTo;
                GetOtgrDocs(selInPDogInfo.ModelRef.Kdog, selDate1, selDate2);
            }
        }


        private void GetOtgrDocs(int _kdog, DateTime _from, DateTime _to)
        {
            Action work = () =>
                {
                    var oDocs = Parent.Repository.GetOtgrDocsForCorrSf(_kdog, _from, _to);
                    Parent.OpenDialog(new CorrsfOtgrDocsViewModel(Parent.Repository, oDocs)
                    {
                        Title = "Документы для формирования корректировочного счёта",
                        InPDogInfo = selInPDogInfo,
                        OutPDogInfo = selOutPDogInfo,
                        OnSubmit = SelectCorrSfData
                    });
                };

            Parent.Services.DoWaitAction(work, "Выборка документов", "Обработка данных");
        }

        private Choice useoldnumsf = new Choice {Header="Прежние номера счетов", IsChecked=false, IsSingleInGroup = false};

        /// <summary>
        /// После выбора накладных
        /// </summary>
        /// <param name="_dlg"></param>
        private void SelectCorrSfData(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as CorrsfOtgrDocsViewModel;
            if (dlg == null) return;

            selDocs = dlg.SelectedOtgrDocs.ToArray();
            selNewCena = dlg.NewCenaProd;

            var chdlg = new ChoicesDlgViewModel(useoldnumsf) 
            {
                Title = "Доп. параметры"
            };

            var ddlg = new DateDlgViewModel()
            {
                
                Title = "Дата счёта",
                DateLabel = null,
                SelDate = DateTime.Now,                
            };

            var newdlg = new BaseCompositeDlgViewModel 
            {
                Title = "Сформировать корректировочный счёт",
                OnSubmit = MakeCorrSf
            };

            newdlg.Add(ddlg);
            newdlg.Add(chdlg);

            Parent.OpenDialog(newdlg);
        }

        /// <summary>
        /// После выбора реквизитов счёта
        /// (формирование)
        /// </summary>
        /// <param name="_dlg"></param>
        private void MakeCorrSf(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as BaseCompositeDlgViewModel;
            if (dlg == null) return;

            var ddlg = dlg.DialogViewModels[0] as DateDlgViewModel;
            if (ddlg == null) return;

            if (ddlg.SelDate == null) return;
            DateTime datpltr = ddlg.SelDate.Value;
            bool oldsf = useoldnumsf.IsChecked ?? false;

            Action work = () => 
            {
                var formedSfs = Parent.Repository.MakeCorrSf(selDocs, selOutPDogInfo.ModelRef.Kdog, selPoup.Kod, datpltr, selNewCena, oldsf);
                ShowUnaccepted(formedSfs);
            };

            Parent.Services.DoWaitAction(work, "Формирование корректировочного счёта", "Обработка данных");
         
        }

        /// <summary>
        /// Отображение неподтверждённых счетов
        /// </summary>
        /// <param name="_sfs"></param>
        private void ShowUnaccepted(SfModel[] _sfs)
        {
            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (_sfs != null && _sfs.Length > 0)
            {
                var nContent = new AcceptSfsViewModel(Parent, _sfs)
                {
                    Title = "Сформировано корр. счета",
                    SelectedPoup = selPoup,
                    DateFrom = selDate1,
                    DateTo = selDate2
                };
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }
    }
}
