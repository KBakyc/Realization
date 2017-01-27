using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetHelper
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static void ForEach<TSource>
            (this IEnumerable<TSource> source, Action<TSource> action)
        {
            foreach (var item in source)
                if (action != null) action(item);
        }
    }
}
