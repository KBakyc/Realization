using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace CommonModule.Composition
{
    public interface IDisplayOrderMetaData
    {
        [DefaultValue(1f)]
        float DisplayOrder { get; }

    }
}
