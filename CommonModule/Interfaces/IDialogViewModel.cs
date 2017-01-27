using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CommonModule.Interfaces
{
    public interface IDialogViewModel
    {
        Collection<object> Dialog { get; }
        void OpenDialog(ICloseViewModel _dlg);
        void CloseDialog(object _dlg);
        //ICommand CloseDlgCmd { get; set; }
    }
}