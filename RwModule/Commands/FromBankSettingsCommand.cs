using System;
using System.ComponentModel.Composition;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using RwModule.ViewModels;
using CommonModule.Composition;

namespace RwModule.Commands
{
    /// <summary>
    /// Комманда для настройки режима приёмки банковских платежей.
    /// </summary>
    [ExportModuleCommand("RwModule.ModuleCommand", DisplayOrder = 98f)]
    public class FromBankSettingsCommand : ModuleCommand
    {
        public FromBankSettingsCommand()
        {
            Label = "Настройка приёмки оплат из банка";
            GroupName = "Настройки модуля";
        }

        protected override int MinParentAccess
        {
            get { return 2; }
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Action work = () =>
                {
                    var nDlg = new RwFromBankSettingsDlgViewModel(Parent.Repository)
                    {
                        Title = "Настройка бухгалтерских счетов"
                    };
                    Parent.OpenDialog(nDlg);
                };

            Parent.Services.DoWaitAction(work);
        }
    }
}
