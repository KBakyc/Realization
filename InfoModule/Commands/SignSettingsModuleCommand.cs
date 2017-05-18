using CommonModule.Commands;
using CommonModule.Composition;
using InfoModule.ViewModels;

namespace InfoModule.Commands
{
    /// <summary>
    /// Команда модуля для настроек подписей документов.
    /// </summary>
    [ExportModuleCommand("InfoModule.ModuleCommand", DisplayOrder=2f)]
    public class SignSettingsModuleCommand : ModuleCommand
    {
        public SignSettingsModuleCommand()
        {
            GroupName = "Подписи документов";
            Label = "Настройка подписей документов";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new SignsSettingsViewModel(Parent.Repository)
            {
                Title = "Настройка подписей документов",
                OnSubmit = OnSubmitEdit
            });
        }

        /// <summary>
        /// Вызов после выбора контрагента
        /// </summary>
        /// <param name="obj"></param>
        private void OnSubmitEdit(object _dlg)
        {
            var dlg = _dlg as SignsSettingsViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;
            if (dlg.IsSignsChanged)
                Parent.Repository.UpdateSigns(dlg.SelPoup.Kod, dlg.SelBoss.Id, dlg.SelGlBuh.Id);
            if (dlg.IsNeedSignsModesChanged())
                dlg.SavePoupsSignModes();
        }
    }
}
