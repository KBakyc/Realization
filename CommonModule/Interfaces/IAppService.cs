using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Interfaces
{
    public interface IAppService
    {
        bool IsStayInMemory { get; }
        void Start();
        void Stop();
    }
}
