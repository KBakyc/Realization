using CommonModule.Commands;
using CommonModule.Composition;
using InfoModule.ViewModels;
using CommonModule.Interfaces;
using System;

namespace InfoModule.Commands
{
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 2.1f)]
    public class SignersModuleCommand : ModuleCommand
    {
        public SignersModuleCommand()
        {
            GroupName = "Подписи документов";
            Label = "Управление подписантами";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            PrepareAndOpenDialog();
        }

        private void PrepareAndOpenDialog()
        {
            ICloseViewModel dlg = null;
            Action prepareDlg = () => 
            {
                dlg = new SignersDlgViewModel(Parent.Repository)
                {
                    Title = "Управление подписантами"
                };
            };
            Action openDlg = () => Parent.OpenDialog(dlg);

            Parent.Services.DoWaitAction(prepareDlg, "Подождите", "Загрузка данных о подписантах...", openDlg);
        }
    }
}
