using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace DAL
{
    /// <summary>
    /// Класс для хранения загруженных данных в целях уменьшения нагрузки на БД.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public class Cache<TKey, TVal> where TKey:struct where TVal:class
    {
        private Func<TKey, TVal> getter;
        public Cache(Func<TKey, TVal> _getter)
        {
            getter = _getter;
        }
        
        private static ConcurrentDictionary<TKey, TVal> cache = new ConcurrentDictionary<TKey, TVal>();
        public TVal GetItem(TKey _id)
        {
            return cache.GetOrAdd(_id, getter);
        }
    }
}
