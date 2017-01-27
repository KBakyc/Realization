using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DotNetHelper
{
    public static class DotNetExtensions
    {
        public static string Format(this string _fmt, params object[] _args)
        {
            return String.Format(_fmt, _args);
        }

        public static void AddRange<T>(this ICollection<T> _col, IEnumerable<T> _items)
        {
            foreach (var i in _items)
                _col.Add(i);
        }

        public static T GetVisualParentOfType<T>(this DependencyObject startObject)
            where T : class
        {
            DependencyObject parent = startObject;
            while (parent != null)
            {
                if (parent is T)
                    break;
                else
                    parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }


        //var comb = DotNetExtensions.GetCombinations(new decimal[] { 1, 2, 3 }, false);
        //var comb = GetCombinations(new decimal[] { 1, 2, 3, 4, 5 }, true);
        //var comb = GetCombinations(new string[] { "мама", "мыла", "раму" }, false);

        /// <summary>
        /// по массиву элементов возвращаются комбинации этих элементов
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_items"></param>
        /// <param name="_unique"></param>
        /// <returns></returns>
        public static IEnumerable<T[]> GetCombinations<T>(T[] _items, bool _unique, int _mincount, int _maxcount)
        {
            var maxidx = _maxcount - 1;
            var minidx = _mincount - 1;
            var indexes = Enumerable.Range(0, _items.Length).ToArray();
            IEnumerable<int[]> icombi = indexes.Select(i => new int[] { i }).ToArray();
            for (int i = 0; i < _items.Length - 1; i++)
            {
                if (i >= minidx)
                    foreach (var c in icombi)
                        yield return c.Select(ci => _items[ci]).ToArray();

                if (maxidx > 0 && i >= maxidx)
                    yield break;

                icombi = icombi.Join(indexes, c => 1, n => 1, (c, n) => c.Union(Enumerable.Repeat(n, 1)).ToArray());
                
                if (_unique)
                    icombi.DistinctBy(c => String.Join("|", c.OrderBy(ci => ci).Select(ci => ci.ToString())));
            }
            yield return _items;
        }  
    }
}
