using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface ICommandInterface
    {
        //string Title { get; }
        string Description { get; }
        ICommand Command { get; }
   }
}
