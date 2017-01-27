using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RwModule.Interfaces;

namespace RwModule.Helpers
{
    public class ModelFilter<T> : IModelFilter<T>
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public Func<T, bool> Filter { get; set; }
    }
}
