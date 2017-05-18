using CommonModule.Commands;
using CommonModule.ViewModels;
using CommonModule.Composition;
using DataObjects.Interfaces;
using System;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для открытия пользовательской документации по АРМу.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder = 99f)]
    public class HelpModuleCommand : ModuleCommand
    {
        private string helpPath;

        public HelpModuleCommand()
        {
            Label = "Руководство пользователя";
            helpPath = Properties.Settings.Default.HelpPath;
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;     
            if (Parent.SelectContent<HtmlPageViewModel>(vm => vm.HtmlPath == helpPath)) return;
            var content = new HtmlPageViewModel(Parent, helpPath) { Title = "Руководство пользователя" };
            if (content.IsValid)
                content.TryOpen();
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) && !String.IsNullOrWhiteSpace(helpPath);
        }
    }
}
