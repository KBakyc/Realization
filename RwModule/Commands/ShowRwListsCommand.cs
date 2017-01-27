using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Commands;
using System.ComponentModel.Composition;
using CommonModule.ViewModels;
using DAL;
using CommonModule.Interfaces;
using CommonModule.Composition;
using CommonModule.Helpers;
using System.Data.OleDb;
using System.Data.Entity;
using RwModule.ViewModels;
using DataObjects;
using RwModule.Models;
using System.Linq.Expressions;
using RwModule.Interfaces;
using RwModule.Helpers;
//using System.Runtime.InteropServices;

namespace RwModule.Commands
{
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 1f)]
    public class ShowRwListsCommand : ModuleCommand
    {
        public ShowRwListsCommand()
        {
            Label = "Просмотр ЖД перечней";            
        }
        
        protected override int MinParentAccess { get { return 1; } }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            GetParameters();
        }

        private void GetParameters()
        {
            var schDlg = new BaseCompositeDlgViewModel
            {
                Title = "Укажите критерии отбора перечней",
                OnSubmit = OnParamsSubmitted
            };

            var datesDlg = new DateRangeDlgViewModel(true) 
            {                
                Title = "Интервал дат бухучёта документов в перечне",
                Name = "datesDlg"
            };        
            schDlg.AddSelectable(datesDlg, true);

            var typeChoices = Enumerations.GetAllValuesAndDescriptions<RwUslType>().Select(kv => new Choice { Header = kv.Value, GroupName = "Тип перечня", IsSingleInGroup = true, IsChecked = false, Item = kv.Key }).ToArray();
            var tListDlg = new ChoicesDlgViewModel(typeChoices)
            {
                Title = "Тип перечня",
                Name = "tListDlg"
            };
            schDlg.AddSelectable(tListDlg, false);

            var nRwList = new NumDlgViewModel
            {
                Title = "Номер перечня",
                Name = "nRwList"
            };
            schDlg.AddSelectable(nRwList, false);

            var nKart = new TxtDlgViewModel
            {
                Title = "Номер накопительной карточки",
                Name = "nKart",
                TextCasing = System.Windows.Controls.CharacterCasing.Upper
            };
            schDlg.AddSelectable(nKart, false);

            var nDoc = new TxtDlgViewModel
            {
                Title = "Номер документа",
                Name = "nDoc"
            };
            schDlg.AddSelectable(nDoc, false);
            
            var nEsfn = new TxtDlgViewModel
            {
                Title = "Номер электронного счёта-фактуры",
                Name = "nEsfn",
                TextCasing = System.Windows.Controls.CharacterCasing.Upper
            };
            schDlg.AddSelectable(nEsfn, false);

            Parent.OpenDialog(schDlg);
        }

        private List<IModelFilter<RwDoc>> rwDocFilters;     

        private void OnParamsSubmitted(Object _dlg)
        {
            Parent.CloseDialog(_dlg);
            rwDocFilters = new List<IModelFilter<RwDoc>>();            
            var schDlg = _dlg as BaseCompositeDlgViewModel;
            if (schDlg == null) return;
            Expression<Func<RwDoc, bool>> predicate = d => true;

            var ddlg = schDlg.GetByName<DateRangeDlgViewModel>("datesDlg");
            if (ddlg != null) 
                predicate = predicate.AndAlso(d => d.Rep_date >= ddlg.DateFrom && d.Rep_date <= ddlg.DateTo);

            var tdlg = schDlg.GetByName<ChoicesDlgViewModel>("tListDlg");
            if (tdlg != null)
            {
                var rwUslType = tdlg.Groups["Тип перечня"].Where(cvm => cvm.IsChecked ?? false).Select(cvm => cvm.GetItem<RwUslType>()).SingleOrDefault();
                predicate = predicate.AndAlso(d => d.RwList.RwlType == rwUslType);
            }

            var ndlg = schDlg.GetByName<NumDlgViewModel>("nRwList");
            if (ndlg != null)
            {
                predicate = predicate.AndAlso(d => d.RwList.Num_rwlist == ndlg.IntValue);
            }
            
            Expression<Func<RwDoc, bool>> npredicate;
            var kdlg = schDlg.GetByName<TxtDlgViewModel>("nKart");
            if (kdlg != null)
            {
                npredicate = d => d.Nkrt == kdlg.Text;//StringComparer.OrdinalIgnoreCase.Compare(d.Nkrt, tdlg.Text)==0;
                predicate = predicate.AndAlso(npredicate);
                rwDocFilters.Add(new ModelFilter<RwDoc>()
                {
                    Label = "Карточка № " + kdlg.Text,
                    Description = "Показывать только документы, относящиеся к карточке № " + kdlg.Text,
                    Filter = d => StringComparer.OrdinalIgnoreCase.Equals(d.Nkrt, kdlg.Text)
                });
            }

            var edlg = schDlg.GetByName<TxtDlgViewModel>("nEsfn");
            if (edlg != null)
            {
                npredicate = d => d.Esfn != null && d.Esfn.VatInvoiceNumber.EndsWith(edlg.Text);
                predicate = predicate.AndAlso(npredicate);
                rwDocFilters.Add(new ModelFilter<RwDoc>()
                {
                    Label = "ЭСФН № " + edlg.Text,
                    Description = "Показывать только документы, привязанные к ЭСФН № " + edlg.Text,
                    Filter = d => d.Esfn != null && d.Esfn.VatInvoiceNumber.EndsWith(edlg.Text)
                });
            }

            var rdlg = schDlg.GetByName<TxtDlgViewModel>("nDoc");
            if (rdlg != null)
            {
                npredicate = d => d.Num_doc.EndsWith(rdlg.Text);
                predicate = predicate.AndAlso(d => d.Num_doc.EndsWith(rdlg.Text));
                rwDocFilters.Add(new ModelFilter<RwDoc>() { Label = "Документ № " + rdlg.Text, Description = "Показывать только документ № " + rdlg.Text, Filter = npredicate.Compile() });                
            }            
            
            OpenOrUpdateRwListsArc(null, predicate, rwDocFilters);
        }

        private void ShowRwLists(RwList[] _rwl, System.Linq.Expressions.Expression<Func<RwDoc, bool>> _predicate, List<IModelFilter<RwDoc>> _rwDocFilters)
        {
            var newContent = new RwListsArcViewModel(Parent, _rwl, _rwDocFilters)
            {
                Title = "Выбранные перечни Витебского отделения Белорусской железной дороги",
                RefreshCommand = new DelegateCommand<RwListsArcViewModel>(vm => OpenOrUpdateRwListsArc(vm, _predicate, null))
            };
            newContent.TryOpen();
        }

        private void OpenOrUpdateRwListsArc(RwListsArcViewModel _vm, System.Linq.Expressions.Expression<Func<RwDoc, bool>> _predicate, List<IModelFilter<RwDoc>> _rwDocFilters)
        {
            RwList[] rwl = null;
            Action work = () =>
            {
                if (_predicate != null)
                    rwl = GetRwListsAction(_predicate);
            };
            Action after = () =>
            {
                if (rwl == null || rwl.Length == 0)
                    Parent.Services.ShowMsg("Результат", "За указанный период ЖД перечней не найдено", true);
                else
                    if (_vm == null)
                        ShowRwLists(rwl, _predicate, _rwDocFilters);
                    else
                        Parent.ShellModel.UpdateUi(()=>_vm.LoadData(rwl), true, false);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос данных", after);
        }

        private RwList[] GetRwListsAction(System.Linq.Expressions.Expression<Func<RwDoc, bool>> _predicate)
        {
            RwList[] rwl = null;
            using (var db = new RealContext())
                rwl = db.RwDocs.Include(d => d.Esfn).Where(_predicate).Select(d => d.RwList).Distinct().ToArray();
            return rwl;
        }
    }
}
