using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;

namespace CommonModule.Interfaces
{
    [ServiceContract()]//(CallbackContract=typeof(IMessenger))]
    public interface IRepkaService
    {
        //[OperationContract(IsOneWay=true)]
        //void ShowMessage(string _message);

        [OperationContract]
        bool IsOnline();

        [OperationContract]
        void SendMessage(string _message, bool _exit);

        [OperationContract]
        Stream GetLog();

        [OperationContract]
        Stream GetScreen();

        [OperationContract]
        string SetShare(string _path, bool _on);

    }
}
