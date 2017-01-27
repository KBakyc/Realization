using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface ICloseViewModel
    {
        ICommand CloseCommand { get; set; }
        bool IsCanClose { get; set; }
    }
}