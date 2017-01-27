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
    public class ExclDocsReport : BasicViewModel, ICommandInterface
    {
        const string PATH = @"Reports/ExclDocsReport.rdlc";

        public static ExclDocsReport TryCreate(IModuleContent _parent)
        {
            if (!File.Exists(PATH)) return null;
            else
                return new ExclDocsReport(_parent);
        }

        private RwListsArcViewModel parent;

        private ExclDocsReport(IModuleContent _parent)
        {
            parent = _parent as RwListsArcViewModel;
        }

        public string Title { get { return "Протокол сумм отказов"; } }
        public string Description { get { return "по перечню"; } }

        private ICommand printCommand;
        public ICommand Command
        {
            get
            {
                if (printCommand == null)
                    printCommand = new DelegateCommand(ExecPrintCommand, () => parent != null && parent.SelectedRwList != null && parent.RwDocsCollection != null && parent.RwDocsCollection.Any(d => d.Value.Exclude));
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
            res = view.OfType<Selectable<RwDocViewModel>>().Where(sd => sd.Value.Exclude).Select(sd => sd.Value);
            return res;
        }

        private void MakeAndShowReport(RwDocViewModel[] _docs)
        {
            var ds = new List<ExclDocsReportData>();
            foreach (var d in _docs)
            {
                var nItem = new ExclDocsReportData()
                {
                    Dat_doc = d.Dat_doc,
                    Num_doc = d.Num_doc,
                    RwListNum = parent.SelectedRwList.Num_rwlist,
                    Sum_doc = d.Ndsrate == 0 ? d.Sum_excl : Math.Round(d.Sum_excl * 100 / (100 + d.Ndsrate)),
                    Nds_rate = d.Ndsrate,
                    Sum_nds = 0,
                    Sum_itog = d.Sum_excl,
                    PoupName = d.Info == null ? "" : d.Info.PoupShortName,
                    Excl_info = d.Excl_info,
                    Nkart = d.Nkrt,
                    PayType = d.RwPay.Paycode,
                    PayName = d.RwPay.Payname
                };
                if (nItem.Nds_rate != 0)
                    nItem.Sum_nds = nItem.Sum_itog - nItem.Sum_doc;
                ds.Add(nItem);
            }

            ReportModel rep = new ReportModel()
            {
                Title = this.Title,
                Description = this.Description,
                Mode = ReportModes.Local,
                DataSources = new Dictionary<string, IEnumerable<object>> { { "DS", ds } },
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
