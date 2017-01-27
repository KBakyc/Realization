using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwModule.Interfaces
{
    public interface IModelFilter<T>
    {
        string Label { get; set; }
        string Description { get; set; }
        Func<T, bool> Filter { get; set; }
    }
}
