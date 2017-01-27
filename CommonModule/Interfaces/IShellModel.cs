using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using System.Windows.Threading;
using DataObjects.Interfaces;
using DataObjects;

namespace CommonModule.Interfaces
{
    public interface IShellModel : IModuleLoader
    {
        //Dispatcher UiDispatcher { get; }
        void UpdateUi(Action _update, bool _async, bool _forceDisp);
        IModule WorkSpace { get; }
        void Exit(string _tit, string _mes, Action<object> _onSubmit, Action<object> _onClose);
        void SendMessage(string _title, string _message, bool _iserr, Action<object> _onSubmit, Action<object> _onClose);
        UserInfo CurrentUserInfo { get; }
        bool IsOnline { get; }
        void CheckConnection();
        void ReadMessages();
        CompositionContainer Container { get; }
        IDbService Repository { get; }
    }
}
