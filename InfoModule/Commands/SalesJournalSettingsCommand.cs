using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using InfoModule.ViewModels;
using CommonModule.Composition;

namespace InfoModule.Commands
{
    /// <summary>
    /// Комманда модуля для настройки видов журналов продаж и бухгалтерских счетов.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 97f)]
    public class SalesJournalSettingsCommand : ModuleCommand
    {
        public SalesJournalSettingsCommand()
        {
            Label = "Настройка журналов продаж и бухгалтерских счетов";
            GroupName = "Журналы";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            var repository = Parent.Repository;

            Action load = () =>
            {
                var nDlg = new SalesJournalTypeDlgViewModel(repository)
                {
                    Title = "Настройка журналов продаж"
                };
                Parent.OpenDialog(nDlg);
            };

            Parent.Services.DoWaitAction(load);            
        }
    }
}
