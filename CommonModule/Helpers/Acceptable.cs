using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataObjects;


namespace CommonModule.Helpers
{
    public class Acceptable<T> : Selectable<T> 
        where T : class
    {
        public Acceptable(T _obj, bool _sel)
            :base(_obj, _sel)
        {}

        public AcceptableInfo Info { get; set; }
    }
}
