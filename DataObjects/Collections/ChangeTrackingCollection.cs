using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;


namespace DataObjects.Collections
{
    public class ChangeTrackingCollection<T> : ObservableCollection<T>, IDisposable
        where T : ITrackable, INotifyPropertyChanged
    {
        public ChangeTrackingCollection()
            : base()
        {}

        public ChangeTrackingCollection(IEnumerable<T> collection, bool tracking) : base(collection)
        {
            Tracking = tracking;
        }

        Collection<T> deletedItems = new Collection<T>();
        
        // Listen for changes to each item
        private bool tracking;
        public bool Tracking
        {
            get { return tracking; }
            set
            {
                foreach (T item in this)
                {
                    if (value) item.PropertyChanged += OnItemChanged;
                    else item.PropertyChanged -= OnItemChanged;
                }
                tracking = value;
            }
        }

        private void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Tracking) return;
            ITrackable item = (ITrackable)sender;
            if (e.PropertyName != "TrackingState")
            {
                // Mark item as updated
                if (item.TrackingState == TrackingInfo.Unchanged)
                    item.TrackingState = TrackingInfo.Updated;
            }
        }

        // Mark item as created and listen for property changes
        protected override void InsertItem(int index, T item)
        {
            if (Tracking)
            {
                item.TrackingState = TrackingInfo.Created;
                item.PropertyChanged += OnItemChanged;
            }
            base.InsertItem(index, item);
        }

        // Mark item as deleted and cache it
        protected override void RemoveItem(int index)
        {
            if (Tracking)
            {
                T item = this.Items[index];
                if (item.TrackingState != TrackingInfo.Created)
                {
                    item.TrackingState = TrackingInfo.Deleted;
                    deletedItems.Add(item);
                }
                item.PropertyChanged -= OnItemChanged;
            }
            base.RemoveItem(index);
        }

        public T[] GetChanges()
        {
            // возвращаем изменённые и удалённые элементы

            var res = 
                this.Where(i => i.TrackingState != TrackingInfo.Unchanged)  // изменённые
                    .Union(this.deletedItems).ToArray();                        // удалённые

            return res;
        }

        #region IDisposable Members

        public void Dispose()
        {
            // отписаться от событий изменения
            foreach (var i in this)
            {
                i.PropertyChanged -= OnItemChanged;
            }
        }

        #endregion
    }
}