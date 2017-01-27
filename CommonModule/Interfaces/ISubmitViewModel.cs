using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface ISubmitViewModel
    {
        ICommand SubmitCommand { get; set; }
    }
}