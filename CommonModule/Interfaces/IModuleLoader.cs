using CommonModule.ViewModels;
using System.Windows.Input;

namespace CommonModule.Interfaces
{
    public interface IModuleLoader
    {
        IModule PreviousModule { get; }
        void LoadModule(IModule _content);
        void UnLoadModule();
        IModule[] Modules { get; }
    }
}