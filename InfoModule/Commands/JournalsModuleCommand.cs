using CommonModule.Commands;
using CommonModule.Composition;
using InfoModule.ViewModels;
using System;
using CommonModule.ViewModels;
using DataObjects;
using System.Collections.Generic;

namespace InfoModule.Commands
{
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder=98f)]
    public class JournalsModuleCommand : ModuleCommand
    {
        public JournalsModuleCommand()
        {
            Label = "Журналы продаж";
            GroupName = "Журналы";
        }

        protected override int MinParentAccess { get { return 2; } }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new GetJournalParamsDlgViewModel(Parent.Repository)
            {
                Title = "Укажите параметры журнала",
                OnSubmit = OnSubmitDlg
            });
        }

        private DateTime dto;

        private void OnSubmitDlg(object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as GetJournalParamsDlgViewModel;
            if (dlg == null) return;

            string vid = dlg.SelectedJournalType.JournalType;
            var dFrom = dlg.DateRangeSelection.DateFrom;
            dto = dlg.DateRangeSelection.DateTo;
            var isinterval = dlg.IsInterval;
            var podvid = dlg.SelectedPodvid;
            var isperev = dlg.IsPerev;
            var perevFrom = dlg.PerevDateRangeSelection.DateFrom;
            var perevTo = dlg.PerevDateRangeSelection.DateTo;
            var iswcorrsfs = dlg.IsWithCorrSfs;

            MakeJFileName(vid, dFrom.Month, isinterval, (byte)podvid, isperev);
            bool exists = Parent.Repository.IfSalesJournalExists(jFileName);

            if (exists)
                AskForAction(() => ShowJournal(),
                             () => MakeAndShowJournal(vid, dFrom, dto, isinterval, (byte)podvid, isperev, perevFrom, perevTo, iswcorrsfs));
            else
                MakeAndShowJournal(vid, dFrom, dto, isinterval, (byte)podvid, isperev, perevFrom, perevTo, iswcorrsfs);
        }

        private void AskForAction(Action _showAction, Action _makeAction)
        {
            Choice show = new Choice(){ GroupName = "Выберите", Header = "Показать", IsChecked = true, IsSingleInGroup = true };
            Choice make = new Choice(){ GroupName = "Выберите", Header = "Сформировать заново", IsChecked = false, IsSingleInGroup = true };

            var askDlg = new ChoicesDlgViewModel(show, make)
            {
                Title = "Внимание! Журнал уже существует",
                OnSubmit = d => 
                {
                    Parent.CloseDialog(d);

                    if (show.IsChecked == true)
                        _showAction();
                    else
                        _makeAction();
                }
            };

            Parent.OpenDialog(askDlg);
        }

        private void MakeAndShowJournal(string _vid, DateTime _dFrom, DateTime _dto, bool _isinterval, byte _podvid, bool _isperev, DateTime _perevFrom, DateTime _perevTo, bool _iswcorrsfs)
        {
            Action work = () =>
            {
                bool res;
                if (res = MakeJournal(_vid, _dFrom, _dto, _isinterval, (byte)_podvid, _isperev, _perevFrom, _perevTo, _iswcorrsfs))
                    ShowJournal();
                else
                    ReportError("Формирование журнала");
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Формирование журнала продаж");
        }

        private bool MakeJournal(string _vid, DateTime _dFrom, DateTime _dto, bool _isinterval, byte _podvid, bool _isperev, DateTime _perevFrom, DateTime _perevTo, bool _iswcorrsfs)
        {
            Parent.Repository.MakeSalesJournal(_vid, _dFrom, _dto, _isinterval, (byte)_podvid, _isperev, _perevFrom, _perevTo, _iswcorrsfs, jFileName);            
            return true;
        }        

        private string error = null;

        private void SetError(Exception _e)
        {
            error = _e.Message;
            if (_e.InnerException != null)
                error += Environment.NewLine + _e.InnerException.Message;
        }

        private void ReportError(string _title)
        {
            if (error == null)
                error = "Неизвестная ошибка";
            Parent.Services.ShowMsg(_title + " : ОШИБКА", error, true);
            error = null;
        }

        private void ShowJournal()
        {
            var jrepname = dto.Year < 2015 ? "SalesJournal2014" : "SalesJournal";

            var jRep = new ReportModel()
            {
                Title = String.Format("Журнал {0}", jFileName),
                Path = @"/real/Reports/" + jrepname,
                Parameters = new Dictionary<string, string> { 
                                 { "JviName", jFileName }, 
                                 { "ConnString", Parent.Repository.ConnectionString }
                }
            };
            (new ReportViewModel(Parent, jRep)).TryOpen();
        }

        private string jFileName;

        private void MakeJFileName(string _vid, int _month, bool _isinterval, byte _podvid, bool _isperev )
        {
            var crmonth = _month.ToString("00");
            jFileName = String.Format("JV{0}{1}{2}{3}", (_podvid > 0 ? _podvid.ToString() : "I"), 
                                                        _vid.PadRight(2,'_'), 
                                                        (_isperev ? "2" : (_isinterval ? "1" : "0")),
                                                        crmonth);
        }
    }
}
