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
        private string jTitle;

        private void OnSubmitDlg(object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var dlg = _dlg as GetJournalParamsDlgViewModel;
            if (dlg == null) return;

            string vid = dlg.SelectedJournalType.JournalType;
            jTitle = dlg.SelectedJournalType.JournalName;
            var dFrom = dlg.DateRangeSelection.DateFrom;
            dto = dlg.DateRangeSelection.DateTo;
            var podvid = (byte)dlg.SelectedPodvid;
            var issfint = dlg.IsSfInterval;
            DateTime? sfFrom = null, sfTo = null;
            if (issfint)
            {
                sfFrom = dlg.SfDateRangeSelection.DateFrom;
                sfTo = dlg.SfDateRangeSelection.DateTo;
            }
            var sftypes = (byte)dlg.SelectedSfType;

            jName = MakeJName(vid, podvid, dFrom, dto, issfint, sfFrom, sfTo, sftypes);
            bool exists = Parent.Repository.IfSalesJournalExists(jName);

            if (exists)
                AskForAction(() => ShowJournal(),
                             () => MakeAndShowJournal(vid, dFrom, dto, podvid, sftypes, issfint, sfFrom, sfTo));
            else
                MakeAndShowJournal(vid, dFrom, dto, podvid, sftypes, issfint, sfFrom, sfTo);
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

        private void MakeAndShowJournal(string _vid, DateTime _dFrom, DateTime _dto, byte _podvid, byte _sftypes, bool _issfint, DateTime? _sfFrom, DateTime? _sfTo)
        {
            Action work = () =>
            {
                Parent.Repository.MakeSalesJournal(_vid, _dFrom, _dto, _podvid, _sftypes, _issfint, _sfFrom, _sfTo, jName);
                ShowJournal();
            };

            Parent.Services.DoWaitAction(work, "Подождите", "Формирование журнала продаж");
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
                Title = String.Format("Журнал {0}", jTitle),
                Path = @"/real/Reports/" + jrepname,
                Parameters = new Dictionary<string, string> { 
                                 { "JviName", jName }, 
                                 { "ConnString", Parent.Repository.ConnectionString }
                }
            };
            (new ReportViewModel(Parent, jRep)).TryOpen();
        }

        private string jName;

        private string MakeJName(string _vid, byte _podvid, DateTime _dfrom, DateTime _dto, bool _issfint, DateTime? _sffrom, DateTime? _sfto, int _sftypes )
        {
            var jparamcombo = _vid.Trim()
                            + "&" + _podvid.ToString()
                            + "&" + _dfrom.ToString("yyyyMMdd")
                            + "&" + _dto.ToString("yyyyMMdd")
                            + "&" + _sftypes.ToString()
                            + "&" + (_issfint ? "1&" + _sffrom.Value.ToString("yyyyMMdd") + "&" + _sfto.Value.ToString("yyyyMMdd")
                                              : "0&&");
            return "J?" + jparamcombo;
                
                //"JV" 
                // + (_podvid > 0 ? _podvid.ToString() : "I") 
                // + _vid.PadRight(2,'_') 
                // + jparamcombo.GetHashCode().ToString("X8");
            //var crmonth = _month.ToString("00");
            //jFileName = String.Format("JV{0}{1}{2}{3}", (_podvid > 0 ? _podvid.ToString() : "I"), 
            //                                            _vid.PadRight(2,'_'), 
            //                                            (_isperev ? "2" : (_isinterval ? "1" : "0")),
            //                                            crmonth);
        }
    }
}
