using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OtgrModule.ViewModels;
using System.Windows.Input;
using CommonModule.Commands;
using DataObjects;
using CommonModule.ViewModels;
using CommonModule.Interfaces;
using System.Windows.Data;

namespace OtgrModule.Reports
{
    public class VagListReport : ICommandInterface
    {
        private OtgrArcViewModel parent;
        
        public VagListReport(IModuleContent _parent)
        {
            parent = _parent as OtgrArcViewModel;
        }

        public string Title { get { return "Справка"; } }
        public string Description { get { return "по движению выбранных вагоноцистерн"; } }

        private ICommand printVagListCommand;
        public ICommand Command
        {
            get
            {
                if (printVagListCommand == null)
                    printVagListCommand = new DelegateCommand(ExecPrintVagListCommand, () => parent != null && parent.OtgrRows != null && parent.OtgrRows.Any(o => o.Nv > 0));
                return printVagListCommand;
            }
        }
        private void ExecPrintVagListCommand()
        {
            var oInView = GetOtgrLinesInView().ToArray();
            Action work = () => MakeAndShowVagListReport(oInView);

            parent.Parent.Services.DoWaitAction(work, "Подождите", "Формирование отчёта");
        }

        private IEnumerable<OtgrLineViewModel> GetOtgrLinesInView()
        {
            IEnumerable<OtgrLineViewModel> res = null;
            var view = CollectionViewSource.GetDefaultView(parent.OtgrRows);
            res = view.OfType<OtgrLineViewModel>();
            return res;
        }

        private void MakeAndShowVagListReport(OtgrLineViewModel[] _otgrs)
        {            
            var otgrs = _otgrs.Where(o => o.Nv > 0);
            var ds = new List<VagListReportData>();
            foreach (var otgr in otgrs)
            {
                var nItem = new VagListReportData()
                {
                    Nv = otgr.Nv,
                    Datgr = otgr.Datgr,
                    RwBillNumber = otgr.RwBillNumber,
                    DocumentNumber = otgr.DocumentNumber,
                    Kpr = otgr.Product.Kpr,
                    KprName = otgr.Product.Name,
                    Poup = otgr.Poup.Poup.Kod,
                    Kodf = otgr.Kodf.Kodf,
                    Kpok = otgr.Pokupatel.Kgr,
                    KpokName = otgr.Pokupatel.Name
                };
                if (otgr.IsSperExists)
                {
                    nItem.SumSper = otgr.Sper + otgr.Dopusl;
                    nItem.NdsSt = otgr.Nds;
                    nItem.SumNds = otgr.Ndssper + otgr.Ndsdopusl;
                    nItem.SumItog = nItem.SumSper + nItem.SumNds;
                    nItem.IsProvozSpis = otgr.Otgr.Provoz > 0;
                    if (!otgr.IsSfsLoaded) otgr.LoadSfs();
                    if (otgr.IsSfsExists)
                    {
                        var sfsSper = new List<string>();
                        foreach (var s in otgr.OtgrSfs)
                        {
                            bool isSperSf = (parent.Parent.Repository.GetSfPays(s.IdSf) ?? new SfProductPayModel[0])
                                            .Any(p => p.PayType == 5 || p.PayType == 6 || p.PayType == 7 || p.PayType == 8);
                            if (isSperSf) sfsSper.Add(s.NumSf.ToString());
                        }
                        if (sfsSper.Count > 0)
                            nItem.Numsf = String.Join(",", sfsSper);
                    }
                }
                ds.Add(nItem);
            }

            ReportModel rep = new ReportModel()
            {
                Title = this.Title,
                Description = this.Description,
                Mode = ReportModes.Local,
                DataSources = new Dictionary<string, IEnumerable<object>> { { "DS1", ds } },
                Parameters = new Dictionary<string, string> { { "Title",  this.Title}, { "Description", this.Description } },
                Path = @"Reports/VagListReport.rdlc"
            };
            var repDC = new ReportViewModel(parent.Parent, rep);
            if (repDC.IsValid)
                repDC.TryOpen();
            else
                parent.Parent.Services.ShowMsg("Ошибка", repDC.GetErrMsg(), true);
        }
    }
}
