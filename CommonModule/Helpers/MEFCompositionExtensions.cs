using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;

namespace CommonModule.Helpers
{
    public static class MEFCompositionExtensions
    {
        public static T GetValueAndClearNonShared<T>(this CompositionContainer _container, string _contractName)
            where T:class
        {
            T res = null;
            try
            {
                var export = _container.GetExport<T>(_contractName);
                if (export != null)
                    res = export.Value;
                if (res != null)
                    _container.ReleaseExport<T>(export);
            }
            catch
            {
                res = null;
            }
            return res;
        }
    }
}
