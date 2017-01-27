using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetHelper
{
    public class GenericCompare<T> : IEqualityComparer<T> where T : class
    {
        private Func<T, object> _expr { get; set; }
        public GenericCompare(Func<T, object> expr)
        {
            this._expr = expr;
        }
        public bool Equals(T x, T y)
        {
            var first = _expr(x);
            var sec = _expr(y);
            if (first != null && first.Equals(sec))
                return true;
            else
                return false;
        }
        public int GetHashCode(T obj)
        {
            return _expr(obj).GetHashCode();
        }
    }
}
