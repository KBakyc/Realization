using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects.Helpers
{
    public class Result<T>
    {
        public Result() { }
        public Result(T _v, string _d)
        {
            Value = _v;
            Description = _d;
        }
        public T Value { get; set; }
        public string Description { get; set; }
    }
}
