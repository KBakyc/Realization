using CommonModule.Commands;
using CommonModule.Composition;
using OtgrModule.ViewModels;

namespace OtgrModule.Commands
{
    [ExportModuleCommand("OtgrModule.ModuleCommand", DisplayOrder=4f)]
    public class SettingsModuleCommand : ModuleCommand
    {
        public SettingsModuleCommand()
        {
            Label = "Персональные настройки";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);

            if (Parent == null) return;

            Parent.OpenDialog(new SettingsViewModel(Parent.Repository)
            {
                Title = "Персональные настройки",
                OnSubmit = OnSubmitEdit
            });
        }

        private void OnSubmitEdit(object _dlg)
        {
            var dlg = _dlg as SettingsViewModel;
            Parent.CloseDialog(_dlg);
            if (dlg == null) return;
            //Properties.Settings.Default.MyKodfs = dlg.MyKodfsString;

            CommonModule.CommonSettings.WriteAppSetting("OtgrModule.Properties.Settings", "KodfsSelMode", dlg.KodfsSelMode.ToString());
            CommonModule.CommonSettings.WriteAppSetting("OtgrModule.Properties.Settings", "ShowUnchecked", dlg.IsShowUnchecked.ToString());
            Properties.Settings.Default.Reload();
        }
    }
}
