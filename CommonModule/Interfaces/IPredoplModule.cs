using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;

namespace CommonModule.Interfaces
{
    public interface IPredoplModule : IModule
    {
        void ListPredopls(IEnumerable<PredoplModel> ms, String t);
    }
}
