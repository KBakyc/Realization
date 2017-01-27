using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace ServiceModule.Views
{
    public class DgOrdinalComparer : System.Collections.IComparer
    {
        Dictionary<DataGridColumn, ItemPropertyInfo> colprops;        

        public DgOrdinalComparer(Dictionary<DataGridColumn, ItemPropertyInfo> _cols)
        {
            colprops = _cols;
        }

        public int Compare(object x, object y)
        {
            if (colprops == null) return 0;
            int res = 0;
            foreach (var c in colprops.Where(c => c.Key.SortDirection != null))
            {
                res = CompareByProp(x, y, c.Value, c.Key.SortDirection ?? ListSortDirection.Ascending);
                if (res != 0) break;
            }
            return res;                
        }

        private int CompareByProp(object a, object b, ItemPropertyInfo _prop, ListSortDirection _dir)
        {
            if (a == b)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            if (_prop == null)
                return 0;
            var av = (_prop.Descriptor as PropertyDescriptor).GetValue(a);
            var bv = (_prop.Descriptor as PropertyDescriptor).GetValue(b);
            if (_prop.PropertyType.Name == "String")
                return _dir == ListSortDirection.Ascending ? String.CompareOrdinal((string)av, (string)bv) : String.CompareOrdinal((string)bv, (string)av);

            IComparable comparableA = av as IComparable;
            IComparable comparableB = bv as IComparable;
            if (comparableA == null || comparableB == null)
                return 0;
            return _dir == ListSortDirection.Ascending ? comparableA.CompareTo(bv) : comparableB.CompareTo(av);           
        }

    }

    public class DataGridEx : DataGrid
    {
        Dictionary<DataGridColumn, ItemPropertyInfo> colprops;

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
        }

        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            var col = eventArgs.Column;
            ListSortDirection dir = col.SortDirection == null || col.SortDirection == ListSortDirection.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending;
            if (!System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) && !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                foreach (var c in this.Columns.Where(c => c.SortDirection != null))
                    c.SortDirection = null;
            col.SortDirection = dir;
            var lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(ItemsSource);
            if (colprops == null)
                colprops = lcv.ItemProperties.Join(this.Columns, i => i.Name, c => c.SortMemberPath, (i, c) => new { Item = i, Column = c }).ToDictionary(ic => ic.Column, ic => ic.Item);
            lcv.CustomSort = new DgOrdinalComparer(colprops);

            //base.OnSorting(eventArgs);
        }        
    }
}
