using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Composition
{
    public interface IComponentNameMetaData
    {
        //[DefaultValue(1f)]
        string ComponentName { get; }

    }
}
