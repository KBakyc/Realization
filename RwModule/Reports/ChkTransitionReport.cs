using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using System.Windows.Data;
using RwModule.ViewModels;
using System.IO;
using CommonModule.Helpers;

namespace RwModule.Reports
{
    public class ChkTransitionReport : BasicViewModel, ICommandInterface
    {
        const string PATH = @"Reports/ChkTransitionReport.rdlc";

        public static ChkTransitionReport TryCreate(IModuleContent _parent)
        {
            if (!File.Exists(PATH)) return null;
            else
                return new ChkTransitionReport(_parent);
        }

        private RwListsArcViewModel parent;

        private ChkTransitionReport(IModuleContent _parent)
        {
            parent = _parent as RwListsArcViewModel;
        }

        public string Title { get { return "Протокол обработки"; } }
        public string Description { get { return "переходного перечня"; } }

        private ICommand printCommand;
        public ICommand Command
        {
            get
            {
                if (printCommand == null)
                    printCommand = new DelegateCommand(ExecPrintCommand, () => parent != null && parent.SelectedRwList != null && parent.RwDocsCollection != null && parent.RwDocsCollection.Any(d => d.Value.IsTransition));
                return printCommand;
            }
        }
        private void ExecPrintCommand()
        {
            var dInView = GetRwDocsLines().ToArray();
            Action work = () => MakeAndShowReport(dInView);

            parent.Parent.Services.DoWaitAction(work, "Подождите", "Формирование отчёта");
        }

        private IEnumerable<RwDocViewModel> GetRwDocsLines()
        {
            IEnumerable<RwDocViewModel> res = null;
            var view = CollectionViewSource.GetDefaultView(parent.RwDocsCollection);
            res = view.OfType<Selectable<RwDocViewModel>>().Where(d => d.Value.IsTransition).Select(s => s.Value);
            return res;
        }

        private void MakeAndShowReport(RwDocViewModel[] _docs)
        {            
            var ds = new List<ChkTransitionReportData>();
            foreach (var d in _docs)
            {
                var nItem = new ChkTransitionReportData()
                { 
                    Dat_doc = d.Dat_doc,
                    Note = d.Note, 
                    Num_doc = d.Num_doc, 
                    Rep_date = d.Rep_date, 
                    RwListDate = parent.SelectedRwList.Dat_inv, 
                    RwListNum = parent.SelectedRwList.Num_rwlist, 
                    Sum_doc = d.Sum_doc, 
                    Nds_rate = d.Ndsrate,
                    Sum_nds = d.Sum_nds, 
                    Sum_itog = d.Sum_itog, 
                    PoupName = d.Info == null ? "" : d.Info.PoupShortName,
                    UslType = d.Info == null ? "" : d.Info.UslType
                };
                ds.Add(nItem);
            }

            ReportModel rep = new ReportModel()
            {
                Title = this.Title,
                Description = this.Description,
                Mode = ReportModes.Local,
                DataSources = new Dictionary<string, IEnumerable<object>> { { "DS", ds } },
                //Parameters = new Dictionary<string, string> { { "Title",  this.Title}, { "Description", this.Description } },
                Path = PATH
            };
            var repDC = new ReportViewModel(parent.Parent, rep);
            if (repDC.IsValid)
                repDC.TryOpen();
            else
                parent.Parent.Services.ShowMsg("Ошибка", repDC.GetErrMsg(), true);
        }
    }
}
