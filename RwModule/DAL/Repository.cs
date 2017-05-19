using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL
{
    public interface IRepository<T, Tkey>
    {

        IQueryable<T> GetAll();
        void Add(T entity);
        void Delete(T entity);
        void Edit(T entity);
        T Get(Tkey i);
        void Save();
    }

    /// <summary>
    /// Обобщённый репозиторий. Не используется.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Tkey"></typeparam>
    public class Repository<T, Tkey> : IRepository<T, Tkey> where T : class
    {
        RealContext _entity = new RealContext();

        public void Add(T entity)
        {
            _entity.Set<T>().Add(entity);
            _entity.Entry(entity).State = System.Data.Entity.EntityState.Added;
        }

        public void Delete(T entity)
        {
            _entity.Set<T>().Remove(entity);
            _entity.Entry(entity).State = System.Data.Entity.EntityState.Deleted;
        }

        public void Edit(T entity)
        {
            _entity.Entry(entity).State = System.Data.Entity.EntityState.Modified;
        }

        public IQueryable<T> GetAll()
        {
            return _entity.Set<T>();
        }

        public T Get(Tkey i)
        {
            return _entity.Set<T>().Find(i);
        }

        public void Save()
        {
            _entity.SaveChanges();
        }
    }
}
