using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;
using System.Collections.ObjectModel;

namespace CommonModule.Interfaces
{
    public interface ISfModule : IPagesModule
    {
        void ShowSf(SfModel m);
        void ListSfs(IEnumerable<SfInListInfo> ms, String t);
        void ListPenalties(IEnumerable<PenaltyModel> ms, string t);
        void EditSf(SfModel m, Action<SfModel> callback);
        void PrintSfs(IEnumerable<SfModel> _sfs);
    }
}
