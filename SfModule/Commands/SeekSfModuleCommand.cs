using System;
using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Composition;
using CommonModule.Interfaces;
using SfModule.ViewModels;
using CommonModule.DataViewModels;
using DataObjects.SeachDatas;

namespace SfModule.Commands
{
    //[Export("SfModule.ModuleCommand", typeof(ModuleCommand))]
    [ExportModuleCommand("SfModule.ModuleCommand", DisplayOrder=2f)]
    public class SeekSfModuleCommand : ModuleCommand
    {
        public SeekSfModuleCommand()
        {
            Label = "Поиск счёта по номеру";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new SeekByNumDlgViewModel()
            {               
                OnSubmit = this.SeekAndShowSfsDlgCallback
            });
        }

        /// <summary>
        /// Метод обратного вызова из диалога
        /// </summary>
        /// <param name="_dlg"></param>
        private void SeekAndShowSfsDlgCallback(object _dlg)
        {
            Parent.CloseDialog(_dlg);
            var dlg = _dlg as SeekByNumDlgViewModel;
            if (dlg == null) return;
            int num2seek = dlg.Number;
            
            Action work = () => SeekSfsByNum(num2seek, dlg.IsCurrentYear);

            Parent.Services.DoWaitAction(work, "Ожидание выполнения", "Поиск счетов-фактур...");
        }

        /// <summary>
        /// Поиск счетов и отображение результатов
        /// </summary>
        /// <param name="_num"></param>
        private void SeekSfsByNum(int _num, bool _isCurYear)
        {
            SfInListViewModel[] sfs = GetSfs(_num, _isCurYear);

            if (sfs == null || sfs.Length == 0)
            {
                Parent.Services.ShowMsg("Результат", "Счета фактуры не найдены", true);
                return;
            }

            ISfModule ISfParent = Parent as ISfModule;
            if (ISfParent == null) return;

            var nContent = new R635ViewModel(Parent, sfs)
            {
                Title = String.Format("Счета: {0}", _num),
                RefreshCommand = new DelegateCommand<R635ViewModel>(
                    vm =>
                    {
                        Action wk = () =>
                        {
                            var newsfs = GetSfs(_num, _isCurYear);
                            vm.LoadData(newsfs);
                        };
                        
                        Parent.Services.DoWaitAction(wk, "Ожидание выполнения", "Выборка и обновление списка счетов...");
                    })
            };
            nContent.TryOpen();
        }

        private SfInListViewModel[] GetSfs(int _num, bool _isCurYear)
        {
            SfInListViewModel[] res = null;
            DateTime? dfrom = null, dto = null;
            if (_isCurYear)
            {
                var curYear = DateTime.Today.Year;
                dfrom = new DateTime(curYear, 1, 1);
                dto = new DateTime(curYear, 12, 31);
            }

            var data = Parent.Repository.GetSfsList(new SfSearchData { NumsfFrom = _num, NumsfTo = _num, DateFrom = dfrom, DateTo = dto});
            if (data != null)
                res = data.Select(h => new SfInListViewModel(Parent.Repository, h)).ToArray();
            return res;
        }

    }
}
