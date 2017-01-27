using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface ICommandCollection
    {
        ICommand[] CommandCollection { get; set; }
    }
}