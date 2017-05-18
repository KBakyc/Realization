using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using DotNetHelper;
using RwModule.Models;
using CommonModule.Interfaces;
using CommonModule.Helpers;
using System.Windows.Input;
using CommonModule.Commands;
using DAL;
using EsfnHelper.ViewModels;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога привязки документов перечня к входящим ЭСФН.
    /// </summary>
    public class LinkRwDocsToEsfnDlgViewModel : BaseDlgViewModel
    {
        private RwDocViewModel[] docs;

        public LinkRwDocsToEsfnDlgViewModel(IModule _parent)
        {
            Title = "Привязка входящих ЭСФН к выбранным документам";
            Parent = _parent;
            LinkCommand = new DelegateCommand(ExecuteLinkCommand, CanExecuteLinkCommand);
            UndoLinkCommand = new DelegateCommand(ExecuteUndoLinkCommand, CanExecuteUndoLinkCommand);
            ApproveCommand = new DelegateCommand(ExecuteApproveCommand, CanExecuteApproveCommand);
            CancelApproveCommand = new DelegateCommand(ExecuteCancelApproveCommand, CanExecuteCancelApproveCommand);
            EsfnInfoCommand = new DelegateCommand(ExecuteEsfnInfoCommand, CanEsfnInfoCommand);
        }

        public LinkRwDocsToEsfnDlgViewModel(IModule _parent, RwDocViewModel[] _docs)
            :this(_parent)
        {            
            LoadData(_docs, null);            
        }

        public void LoadData(RwDocViewModel[] _docs, WaitDlgViewModel _wd)
        {
            if (_docs == null || _docs.Length == 0 || Parent == null) return;

            long? prevSelId = null;
            if (selLinkedDoc != null)
                prevSelId = selLinkedDoc.Value.DocVm.Id_rwdoc;

            docs = _docs;

            List<LinkRwDocToEsfnViewModel> processed = new List<LinkRwDocToEsfnViewModel>();

            for (int i = 0; i < _docs.Length; i++ )
            {
                var d = _docs[i];
                _wd.Message = String.Format("Проверка привязки данных\nпо документу № {0}\n({1})\n[{2}/{3}]", d.Num_doc, d.RwPay.Payname, i+1, _docs.Length);
                processed.Add(new LinkRwDocToEsfnViewModel(d));
            }
            linkedDocs = new ObservableCollection<Selectable<LinkRwDocToEsfnViewModel>>(processed.Select(d => new Selectable<LinkRwDocToEsfnViewModel>(d, false)));
            if (linkedDocs == null || linkedDocs.Count == 0)
                selLinkedDoc = null;
            else
            if (prevSelId.HasValue)
                selLinkedDoc = linkedDocs.FirstOrDefault(d => d.Value.DocVm.Id_rwdoc == prevSelId.Value);
            else
            if (linkedDocs.Count == 1)
                selLinkedDoc = linkedDocs[0];
            else
                selLinkedDoc = null;
            if (selLinkedDoc != null)
                UpdateVatInvoices();
        }

        public override bool IsValid()
        {
            return base.IsValid();
        }

        public ICommand LinkCommand { get; set; }

        private Func<Selectable<LinkRwDocToEsfnViewModel>, bool> linkSelector = d => d.Value.IsLinkChanged && d.Value.IsCanLink;

        private bool CanExecuteLinkCommand()
        {
            return linkedDocs.Any(linkSelector);
        }

        private void ExecuteLinkCommand()
        {
            var askDlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Изменить привязку документов к ЭСФН?",
                IsCancelable = true,
                OnSubmit = d => 
                {
                    IsWorkMade = true;
                    Parent.CloseDialog(d);
                    DoLinkCommand();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DoLinkCommand()
        {            
            Action<ProgressDlgViewModel> work = DoLinkCommandAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Привязка документов к ЭСФН");
        }

        private void DoLinkCommandAction(ProgressDlgViewModel _dlg)
        {
            var changedLinks = linkedDocs.Where(linkSelector).ToArray();
            _dlg.StartValue = 1;
            _dlg.FinishValue = changedLinks.Length;
            List<Selectable<LinkRwDocToEsfnViewModel>> linked = new List<Selectable<LinkRwDocToEsfnViewModel>>();

            var isOk = true; 

            using (var db = new RealContext())
            {
                foreach (var sl in changedLinks)
                {
                    _dlg.CurrentValue++;
                    var l = sl.Value;
                    try
                    {
                        _dlg.Message =  String.Format("Документ № {0} от {1:dd.MM.yy} : {2}", l.NumDoc, l.DatDoc, l.PayName);
                        if (db.LinkRwDocToESFN(l.DocVm.Id_rwdoc, l.SelRwDocEsfn.VatInvoiceId.Value, l.Account, l.VatAccount, l.AccountingDate.Value))
                        {
                            linked.Add(sl);
                        }
                    }
                    catch (Exception e)
                    {
                        Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", l.EsfnNumber + " привязать не удалось.\n" + e.Message, true);
                        CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
                        isOk = false;
                        break;
                    }
                }
            }

            Action updateui = () =>
            {
                foreach (var l in linked)
                {
                    var ind = linkedDocs.IndexOf(l);
                    linkedDocs.RemoveAt(ind);
                    var updated = new Selectable<LinkRwDocToEsfnViewModel>(new LinkRwDocToEsfnViewModel(l.Value.DocVm), false);
                    linkedDocs.Insert(ind, updated);
                }
                if (isOk)
                    Parent.Services.ShowMsg("Результат", "Изменение привязки документов завершено", false);
                
            };
            Parent.ShellModel.UpdateUi(updateui, false, false);
        }

        public ICommand UndoLinkCommand { get; set; }

        private Func<Selectable<LinkRwDocToEsfnViewModel>, bool> undoLinkSelector = d => d.IsSelected && d.Value.IsLinked;

        private bool CanExecuteUndoLinkCommand()
        {
            return linkedDocs != null && linkedDocs.Any(undoLinkSelector);
        }
        
        private void ExecuteUndoLinkCommand()
        {
            var askDlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Отменить привязку документов к ЭСФН?",
                IsCancelable = true,
                OnSubmit = d =>
                {
                    IsWorkMade = true;
                    Parent.CloseDialog(d);
                    DoUnLinkCommand();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DoUnLinkCommand()
        {
            Action<ProgressDlgViewModel> work = DoUnLinkCommandAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Отмена привязки документов к ЭСФН");
        }

        private void DoUnLinkCommandAction(ProgressDlgViewModel _dlg)
        {
            var links = linkedDocs.Where(undoLinkSelector).ToArray();
            _dlg.StartValue = 1;
            _dlg.FinishValue = links.Length;
            List<Selectable<LinkRwDocToEsfnViewModel>> unlinked = new List<Selectable<LinkRwDocToEsfnViewModel>>();

            var isOk = true;

            using (var db = new RealContext())
            {
                foreach (var sl in links)
                {
                    _dlg.CurrentValue++;
                    var l = sl.Value;
                    try
                    {
                        _dlg.Message = String.Format("Документ № {0} от {1:dd.MM.yy} : {2}", l.NumDoc, l.DatDoc, l.PayName);
                        db.UnLinkRwDocFromESFN(l.DocVm.Id_rwdoc, l.DocVm.Esfn.VatInvoiceId);
                        unlinked.Add(sl);
                    }
                    catch (Exception e)
                    {
                        Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", l.EsfnNumber + " отвязать не удалось.\n" + e.Message, true);
                        CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
                        isOk = false;
                        break;
                    }
                }
            }

            Action updateui = () =>
            {
                foreach (var l in unlinked)
                {
                    var ind = linkedDocs.IndexOf(l);
                    linkedDocs.RemoveAt(ind);
                    var updated = new Selectable<LinkRwDocToEsfnViewModel>(new LinkRwDocToEsfnViewModel(l.Value.DocVm), false);
                    linkedDocs.Insert(ind, updated);
                }
                if (isOk)
                    Parent.Services.ShowMsg("Результат", "Изменение привязки документов завершено", false);

            };
            Parent.ShellModel.UpdateUi(updateui, false, false);
        }

        public ICommand ApproveCommand { get; set; }

        private Func<Selectable<LinkRwDocToEsfnViewModel>, bool> approveSelector = d => d.Value.IsLinked && !d.Value.IsLinkChanged && String.IsNullOrWhiteSpace(d.Value.ApproveUser);

        private bool CanExecuteApproveCommand()
        {
            return linkedDocs.Any(approveSelector);
        }

        private void ExecuteApproveCommand()
        {
            var askDlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Подтвердить новые входящие ЭСФН?",
                IsCancelable = true,
                OnSubmit = d =>
                {
                    IsWorkMade = true;
                    Parent.CloseDialog(d);
                    DoApproveCommand();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DoApproveCommand()
        {
            Action<ProgressDlgViewModel> work = DoApproveCommandAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Подтверждение ЭСФН");
        }

        private void DoApproveCommandAction(ProgressDlgViewModel _dlg)
        {
            var toApproveDocs = linkedDocs.Where(approveSelector).ToArray();
            var newInvoices = toApproveDocs.Select(d => Tuple.Create(d.Value.LinkedEsfn.VatInvoiceId, d.Value.LinkedEsfn.VatInvoiceNumber)).Distinct().ToArray();
            _dlg.StartValue = 1;
            _dlg.FinishValue = newInvoices.Length;
            List<int> iApproved = new List<int>();

            var isOk = true;

            using (var db = new RealContext())
            {
                foreach (var vi in newInvoices)
                {
                    _dlg.CurrentValue++;
                    try
                    {
                        _dlg.Message = String.Format("ЭСФН № {0}", vi.Item2);
                        if (db.ApproveRwESFN(vi.Item1))
                            iApproved.Add(vi.Item1);
                    }
                    catch (Exception e)
                    {
                        Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", vi.Item2 + " подтвердить не удалось.\n" + e.Message, true);
                        CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
                        isOk = false;
                        break;
                    }
                }
            }

            Action updateui = () =>
            {
                var approvedDocs = toApproveDocs.Where(d => iApproved.Contains(d.Value.LinkedEsfn.VatInvoiceId));
                foreach (var l in approvedDocs)
                {
                    var ind = linkedDocs.IndexOf(l);
                    linkedDocs.RemoveAt(ind);
                    var updated = new Selectable<LinkRwDocToEsfnViewModel>(new LinkRwDocToEsfnViewModel(l.Value.DocVm), false);
                    linkedDocs.Insert(ind, updated);
                }
                if (isOk)
                    Parent.Services.ShowMsg("Результат", "Подтверждение ЭСФН завершено", false);

            };
            Parent.ShellModel.UpdateUi(updateui, false, false);
        }

        public ICommand EsfnInfoCommand { get; set; }
        private bool CanEsfnInfoCommand()
        {
            return selLinkedDoc != null && selLinkedDoc.Value.SelRwDocEsfn != null;
        }
        private void ExecuteEsfnInfoCommand()
        {
            Action work = () =>
            {
                var vi = VatInvoiceViewModel.FromId(selLinkedDoc.Value.SelRwDocEsfn.VatInvoiceId.GetValueOrDefault());
                if (vi != null)
                {
                    var dlg = new VatInvoiceDlgViewModel(vi);
                    Parent.OpenDialog(dlg);
                }
            };

            Parent.Services.DoWaitAction(work);
        }

        public ICommand CancelApproveCommand { get; set; }

        private Func<Selectable<LinkRwDocToEsfnViewModel>, bool> cancelApproveSelector = d => d.IsSelected && d.Value.IsLinked && !String.IsNullOrWhiteSpace(d.Value.ApproveUser);

        private bool CanExecuteCancelApproveCommand()
        {
            return linkedDocs.Any(cancelApproveSelector);
        }

        private void ExecuteCancelApproveCommand()
        {
            var nums = linkedDocs.Where(cancelApproveSelector).Select(d => d.Value.LinkedEsfn.VatInvoiceNumber).Distinct().ToArray();
            var askDlg = new MsgDlgViewModel
            {
                Title = "Подтверждение",
                Message = "Отменить подтверждение входящих ЭСФН?\n\n" + String.Join("\n", nums),
                IsCancelable = true,
                OnSubmit = d =>
                {
                    IsWorkMade = true;
                    Parent.CloseDialog(d);
                    DoCancelApproveCommand();
                }
            };
            Parent.OpenDialog(askDlg);
        }

        private void DoCancelApproveCommand()
        {
            Action<ProgressDlgViewModel> work = DoCancelApproveCommandAction;
            Parent.Services.DoWaitAction(work, "Подождите", "Отмена подтверждения ЭСФН");
        }

        private void DoCancelApproveCommandAction(ProgressDlgViewModel _dlg)
        {
            var toCancelApproveDocs = linkedDocs.Where(cancelApproveSelector).ToArray();
            var cancelInvoices = toCancelApproveDocs.Select(d => Tuple.Create(d.Value.LinkedEsfn.VatInvoiceId, d.Value.LinkedEsfn.VatInvoiceNumber)).Distinct().ToArray();
            _dlg.StartValue = 1;
            _dlg.FinishValue = cancelInvoices.Length;
            List<int> iCancelled = new List<int>();

            var isOk = true;

            using (var db = new RealContext())
            {
                foreach (var vi in cancelInvoices)
                {
                    _dlg.CurrentValue++;
                    try
                    {
                        _dlg.Message = String.Format("ЭСФН № {0}", vi.Item2);
                        if (db.CancelApproveRwESFN(vi.Item1))
                            iCancelled.Add(vi.Item1);
                    }
                    catch (Exception e)
                    {
                        Parent.Services.ShowMsg("Ошибка (" + e.GetType().ToString() + ")", vi.Item2 + " отменить подтверждение не удалось.\n" + e.Message, true);
                        CommonModule.Helpers.WorkFlowHelper.OnCrash(e, null, true);
                        isOk = false;
                        break;
                    }
                }
            }

            Action updateui = () =>
            {
                var cancelledDocs = linkedDocs.Where(d => iCancelled.Contains(d.Value.LinkedEsfn.VatInvoiceId)).ToArray();
                foreach (var l in cancelledDocs)
                {
                    var ind = linkedDocs.IndexOf(l);
                    linkedDocs.RemoveAt(ind);
                    var updated = new Selectable<LinkRwDocToEsfnViewModel>(new LinkRwDocToEsfnViewModel(l.Value.DocVm), false);
                    linkedDocs.Insert(ind, updated);
                }
                if (isOk)
                    Parent.Services.ShowMsg("Результат", "Отмена подтверждения ЭСФН завершено", false);

            };
            Parent.ShellModel.UpdateUi(updateui, false, false);
        }

        private ObservableCollection<Selectable<LinkRwDocToEsfnViewModel>> linkedDocs;
        public ObservableCollection<Selectable<LinkRwDocToEsfnViewModel>> LinkedDocs
        {
            get { return linkedDocs; }
        }

        private Selectable<LinkRwDocToEsfnViewModel> selLinkedDoc;
        public Selectable<LinkRwDocToEsfnViewModel> SelLinkedDoc
        {
            get { return selLinkedDoc; }
            set 
            { 
                SetAndNotifyProperty("SelLinkedDoc", ref selLinkedDoc, value);
                UpdateVatInvoicesAsync();
            }
        }

        private VatInvoiceViewModel selVatInvoiceOfSelectedDoc;
        public VatInvoiceViewModel SelVatInvoiceOfSelectedDoc
        {
            get { return selVatInvoiceOfSelectedDoc; }
            set 
            { 
                SetAndNotifyProperty("SelVatInvoiceOfSelectedDoc", ref selVatInvoiceOfSelectedDoc, value);
                SetSelectedESFN();
            }
        }

        private void SetSelectedESFN()
        {
            if (selLinkedDoc != null)// && selVatInvoiceOfSelectedDoc != null)
                selLinkedDoc.Value.SetNewLinkedESFN(selVatInvoiceOfSelectedDoc == null ? null : selVatInvoiceOfSelectedDoc.Header);
        }

        private List<VatInvoiceViewModel> vatInvoicesOfSelectedDoc;
        public List<VatInvoiceViewModel> VatInvoicesOfSelectedDoc
        {
            get { return vatInvoicesOfSelectedDoc; }
            set { SetAndNotifyProperty("VatInvoicesOfSelectedDoc", ref vatInvoicesOfSelectedDoc, value); }
        }

        private void UpdateVatInvoicesAsync()
        {
            Parent.Services.DoWaitAction(UpdateVatInvoices);
        }

        private void UpdateVatInvoices()
        {
            List<VatInvoiceViewModel> res = new List<VatInvoiceViewModel>();

            VatInvoiceViewModel newsel = null;
            var selectedDoc = selLinkedDoc;
            if (selectedDoc != null)
            {
                if (selectedDoc.Value.AllRwDocEsfns != null && selectedDoc.Value.AllRwDocEsfns.Length > 0)// || selectedDoc.Value.LinkedEsfn != null
                {
                    res.AddRange(selectedDoc.Value.AllRwDocEsfns.Select(e => VatInvoiceViewModel.FromId(e.VatInvoiceId.GetValueOrDefault())));
                }
                if (selectedDoc.Value.SelRwDocEsfn != null && !res.Any(e => e.Header.InvoiceId == selectedDoc.Value.SelRwDocEsfn.VatInvoiceId))
                    res.Add(VatInvoiceViewModel.FromId(selectedDoc.Value.SelRwDocEsfn.VatInvoiceId.GetValueOrDefault()));
                newsel = (res.Count == 0 || selectedDoc.Value.SelRwDocEsfn == null) ? null : res.FirstOrDefault(e => e.Header.InvoiceId == selectedDoc.Value.SelRwDocEsfn.VatInvoiceId);
            }
            Parent.ShellModel.UpdateUi(() => 
            {
                VatInvoicesOfSelectedDoc = res;
                SelVatInvoiceOfSelectedDoc = newsel;
            }, true, false);
        }

        public bool IsWorkMade { get; set; }
    }
}
