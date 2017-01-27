using System.Collections.Generic;
using DataObjects;
namespace CommonModule.Interfaces
{
    public interface IOtgruzModule : IModule
    {
        void ShowOtgrArc(int _idsf);
        void ShowOtgrArc(IEnumerable<OtgrLine> _otgrs);
    }
}