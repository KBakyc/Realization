using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;

namespace ServiceModule.ViewModels
{
    public class DataGridOrdinalComparer : IDataGridItemComparer
    {
        public Dictionary<DataGridColumn, ItemPropertyInfo> ColumnProperties { get; set; }

        public DataGridOrdinalComparer()
        {
        }

        public int Compare(object x, object y)
        {
            if (ColumnProperties == null) return 0;
            int res = 0;
            foreach (var c in ColumnProperties.Where(c => c.Key.SortDirection != null))
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
}
