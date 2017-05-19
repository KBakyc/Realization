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

namespace RwModule.Commands
{
    /// <summary>
    /// Команда для запуска режима просмотра архива принятых их банка платежей за ЖД услуги.
    /// </summary>
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 20f)]
    public class ShowRwPlatsCommand : ModuleCommand
    {
        public ShowRwPlatsCommand()
        {
            Label = "Просмотр архива платежей";
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
                Title = "Укажите критерии отбора платежей/возвратов",
                OnSubmit = OnParamsSubmitted
            };

            var datesDlg = new DateRangeDlgViewModel(true)
            {
                Title = "Интервал дат документов",
                Name = "datesDlg"
            };
            schDlg.AddSelectable(datesDlg, true);

            var typeChoices = Enumerations.GetAllValuesAndDescriptions<RwUslType>().Select(kv => new Choice { Header = kv.Value, GroupName = "Тип платежа", IsSingleInGroup = true, IsChecked = false, Item = kv.Key }).ToArray();
            var tPaysDlg = new ChoicesDlgViewModel(typeChoices)
            {
                Title = "Тип платежа",
                Name = "tPaysDlg"
            };
            schDlg.AddSelectable(tPaysDlg, false);

            var nDoc = new NumDlgViewModel
            {
                Title = "Номер документа",
                Name = "nDoc"
            };
            schDlg.AddSelectable(nDoc, false);

            Parent.OpenDialog(schDlg);
        }

        private void OnParamsSubmitted(Object _dlg)
        {
            Parent.CloseDialog(_dlg);

            var schDlg = _dlg as BaseCompositeDlgViewModel;
            if (schDlg == null) return;
            Expression<Func<RwPlat, bool>> predicate = d => true;

            List<string> paramInfos = new List<string>();

            var ddlg = schDlg.GetByName<DateRangeDlgViewModel>("datesDlg");
            if (ddlg != null)
            {
                DateTime? dfrom = null;
                DateTime? dto = null;
                dfrom = ddlg.DateFrom;
                dto = ddlg.DateTo;
                predicate = predicate.AndAlso(d => d.Datplat >= dfrom && d.Datplat <= dto);
                paramInfos.Add(String.Format("За период с {0:dd.MM.yy} по {1:dd.MM.yy}", dfrom, dto));
            }

            var tdlg = schDlg.GetByName<ChoicesDlgViewModel>("tPaysDlg");
            if (tdlg != null)
            {
                var rwUslType = tdlg.Groups["Тип платежа"].Where(cvm => cvm.IsChecked ?? false).Select(cvm => cvm.GetItem<RwUslType>()).SingleOrDefault();               
                predicate = predicate.AndAlso(d => d.Idusltype == rwUslType);
                paramInfos.Add(String.Format("Тип платежа: {0}", rwUslType.GetEnumDescription()));
            }

            var ndlg = schDlg.GetByName<NumDlgViewModel>("nDoc");
            if (ndlg != null)
            {
                predicate = predicate.AndAlso(d => d.Numplat == ndlg.IntValue);
                paramInfos.Add(String.Format("Номер документа: {0}", ndlg.IntValue));
            }
            
            OpenOrUpdateRwPlatsArc(null, predicate, paramInfos);
        }

        private void ShowRwPlats(RwPlat[] _rwp, System.Linq.Expressions.Expression<Func<RwPlat, bool>> _predicate, IEnumerable<string> _parInfos)
        {
            var newContent = new RwPlatsArcViewModel(Parent, _rwp)
            {
                Title = "Выбранные платежи по услугам Витебского отделения Белорусской железной дороги",
                ParamInfos = _parInfos.ToArray(),
                RefreshCommand = new DelegateCommand<RwPlatsArcViewModel>(vm => OpenOrUpdateRwPlatsArc(vm, _predicate, null))
            };
            newContent.TryOpen();
        }    

        private void OpenOrUpdateRwPlatsArc(RwPlatsArcViewModel _vm, System.Linq.Expressions.Expression<Func<RwPlat, bool>> _predicate, IEnumerable<string> _parInfos)
        {
            RwPlat[] rwp = null;
            Action work = () =>
            {
                if (_predicate != null)
                    rwp = GetRwPlatsAction(_predicate);
            };
            Action after = () =>
            {
                if (rwp == null || rwp.Length == 0)
                    Parent.Services.ShowMsg("Результат", "По указанным критериям платежей не найдено", true);
                else
                    if (_vm == null)
                        ShowRwPlats(rwp, _predicate, _parInfos);
                    else
                        Parent.ShellModel.UpdateUi(() => _vm.LoadData(rwp), true, false);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос данных", after);
        }

        private RwPlat[] GetRwPlatsAction(System.Linq.Expressions.Expression<Func<RwPlat, bool>> _predicate)
        {
            RwPlat[] rwp = null;
            using (var db = new RealContext())
                rwp = db.RwPlats.Where(_predicate).ToArray();
            return rwp;
        }
    }
}
