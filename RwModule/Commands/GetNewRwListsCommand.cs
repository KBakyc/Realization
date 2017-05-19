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
using RwModule.ViewModels;
using DataObjects;
using RwModule.Models;

namespace RwModule.Commands
{
    /// <summary>
    /// Команда модуля для запуска режима загрузки новых ЖД перечней.
    /// </summary>
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = .5f)]
    public class GetNewRwListsCommand : ModuleCommand
    {
        public GetNewRwListsCommand()
        {
            Label = "Приём новых ЖД перечней";
        }

        protected override int MinParentAccess { get { return 2; } }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            GetNewRwListsAndShow();
        }

        private void GetNewRwListsAndShow()
        {
            List<RwListViewModel> rwl = null;
            Action work = () =>
            {
                using (var db = new RealContext())
                {
                    var rwlm = db.GetNewRwLists();
                    if (rwlm != null && rwlm.Length > 0)
                    {
                        rwl = new List<RwListViewModel>(rwlm.Length);
                        rwl.AddRange(rwlm.Select(m => 
                            new RwListViewModel(Parent.Repository, m) 
                            { 
                                IsNew = !(db.RwLists.Any(l => l.Keykrt == m.Keykrt)) 
                            }));
                    }
                }
            };
            Action after = () =>
            {
                if (rwl == null || rwl.Count == 0)
                    Parent.Services.ShowMsg("Результат", "Новых ЖД перечней не найдено", true);
                else
                    ShowNewRwLists(rwl);
            };
            Parent.Services.DoWaitAction(work, "Подождите", "Запрос данных", after);
        }

        private void ShowNewRwLists(List<RwListViewModel> _rwl)
        {
            var newContent = new GetNewRwListsViewModel(Parent, _rwl)
            {
                Title = "Новые перечни Витебского отделения Белорусской железной дороги"
            };
            newContent.TryOpen();
        }        
    }
}
