using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using CommonModule.ViewModels;
using DataObjects;
using CommonModule.Interfaces;
using Microsoft.Reporting.WinForms;

namespace CommonModule.Helpers
{
    public class PrintHelper
    {
        private System.Drawing.Printing.PrinterSettings printerSettings;

        public bool IsSettingsOk
        {
            get
            {
                return printerSettings != null && printerSettings.IsValid;
            }
        }

        public PrintHelper()
        {}

        public System.Drawing.Printing.PrinterSettings CurrentPrinterSettings { get { return printerSettings; } }

        public bool GetPrintSettings()//(bool? _isLandscape)
        { 
            var printDlg = new PrintDialog();
            bool res = printDlg.ShowDialog() ?? false;
            if (res)
            {
                var printQue = printDlg.PrintQueue;
                printerSettings = new System.Drawing.Printing.PrinterSettings();
                printerSettings.PrinterName = printQue.FullName;
                printerSettings.Copies = (short)(printDlg.PrintTicket.CopyCount ?? 1);//printQue.UserPrintTicket.CopyCount ?? 1);
                int dupl = (int)(printDlg.PrintTicket.Duplexing ?? 0);
                if (dupl > 0)
                    printerSettings.Duplex = (System.Drawing.Printing.Duplex)dupl;

                //if (_isLandscape.HasValue)
                //    printQue.UserPrintTicket.PageOrientation = _isLandscape.Value ? PageOrientation.Landscape : PageOrientation.Portrait;
                //else
                //    printQue.UserPrintTicket.PageOrientation = printDlg.PrintTicket.PageOrientation;
                //printQue.UserPrintTicket.CopyCount = printDlg.PrintTicket.CopyCount;
                //printQue.UserPrintTicket.Duplexing = printDlg.PrintTicket.Duplexing;
                //printTk = printQue.UserPrintTicket;
            }
            return res;
        }
        
        public void PrintReport(IModule _mod, Report _rp)
        {
            PrintReport(_mod, _rp, false, true);
        }

        public void PrintReport(IModule _mod, Report _rp, bool _isAlterTopMargin)
        {
            PrintReport(_mod, _rp, _isAlterTopMargin, true);
        }

        private bool isAsync = true;
        public bool IsAsync
        {
            get { return isAsync; }
            set { isAsync = value; }
        }
        

        public void PrintReport(IModule _mod, Report _rp, bool _isAlterTopMargin, bool _isSilent)
        {
            if (_rp != null)
            {
                ReportPrintDocument rdoc = null;
                if (_rp is ServerReport)
                    rdoc = new ReportPrintDocument((ServerReport)_rp, _isAlterTopMargin);
                else
                    rdoc = new ReportPrintDocument((LocalReport)_rp);
                rdoc.PrintController = new System.Drawing.Printing.StandardPrintController();

                _mod.ShellModel.UpdateUi(() =>
                    {
                        if (!_isSilent || !IsSettingsOk) GetPrintSettings();// (_rp.GetDefaultPageSettings().IsLandscape);
                        if (IsSettingsOk)
                        {
                            rdoc.PrinterSettings = printerSettings;
                            rdoc.Print();
                        }
                        rdoc.Dispose();
                    }, isAsync, false);
            }
        }


    }
}
