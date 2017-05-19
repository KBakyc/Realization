using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using SfModule.ViewModels;
using DataObjects.Helpers;

namespace SfModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима формирования счетов-фактур.
    /// </summary>
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder=1f)]
    public class FormSfsModuleCommand : ModuleCommand
    {
        public FormSfsModuleCommand()
        {
            Label = "Формирование счетов-фактур";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            if (Parent.SelectContent<AcceptSfsViewModel>(null)) return;

            Action work = LoadUnaccepted;
            Action afterwork = () => 
            {
                if (sfs == null || sfs.Length == 0)
                    Parent.OpenDialog(new FormSfsDlgViewModel(Parent.Repository)
                    {
                        Title = "Сформировать счета-фактуры:",
                        OnSubmit = MakeAndShowSfs
                    });
                else
                    ShowUnaccepted();
            };

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Загрузка неподтверждённых счетов-фактур...", afterwork);
        }

        private int poup;
        private short pkod;
        private DateTime dateFrom;
        private DateTime dateTo;
        private SfModel[] sfs;

        /// <summary>
        /// Метод обратного вызова из диалога
        /// </summary>
        /// <param name="_dlg"></param>
        private void MakeAndShowSfs(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as FormSfsDlgViewModel;
            if (dlg == null) return;
            poup = dlg.SelPoup.Kod;
            if (dlg.PoupDatesSelection.IsPkodEnabled)
                pkod = dlg.SelPkods[0].Pkod;
            else
                pkod = 0;
            dateFrom = dlg.DateFrom;
            dateTo = dlg.DateTo;
            byte dtaccm = dlg.DtAcceptedMode;
            bool uonum = dlg.IsUseOldNumSf;
            DateTime? datesf = dlg.DateSf;
            int uid = 0;
            if (dlg.IsMyMode)
                uid = Parent.Repository.UserToken;

            Action work = () => MakeSfsByPoupAndDates(poup, pkod, dateFrom, dateTo, uid, dtaccm, uonum, datesf);

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Формирование счетов-фактур...");
        }

        /// <summary>
        /// Формирование счетов-фактур
        /// </summary>
        /// <param name="_poup"></param>
        /// <param name="_from"></param>
        /// <param name="_to"></param>
        private void MakeSfsByPoupAndDates(int _poup, short _pkod, DateTime _from, DateTime _to, int _userid, byte _dtaccmode, bool _oldnumsf, DateTime? _datesf)
        {
            sfs = Parent.Repository.MakeTempP635(_poup, _pkod, _from, _to, _userid, _dtaccmode, _oldnumsf, _datesf);
            ShowUnaccepted();
        }

        /// <summary>
        /// Загрузка неподтверждённых счетов
        /// </summary>
        private void LoadUnaccepted()
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
        }

        /// <summary>
        /// Отображение неподтверждённых счетов
        /// </summary>
        /// <param name="_sfs"></param>
        private void ShowUnaccepted()
        {
            ISfModule SfParent = Parent as ISfModule;
            if (SfParent == null) return;

            if (sfs != null && sfs.Length > 0)
            {
                var nContent = new AcceptSfsViewModel(Parent, sfs)
                {
                    Title = "Сформировано",
                    SelectedPoup = Parent.Repository.Poups[poup],
                    DateFrom = dateFrom,
                    DateTo = dateTo
                };
                sfs = null;
                nContent.TryOpen();
            }
            else
                Parent.Services.ShowMsg("Результат", "Нет данных, удовлетворяющих указанным критериям.", true);
        }
    }
}
