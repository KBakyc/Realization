using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using CommonModule.Helpers;
using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using System.Linq;
using System.Windows.Data;
using System.Collections.Generic;

namespace ServiceModule.ViewModels
{
    public class DataGridSortBehavior : Behavior<DataGrid>
    {
        public IDataGridItemComparer ItemComparer { get; set; }
        private Dictionary<DataGridColumn, ItemPropertyInfo> colprops;        

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
            //AssociatedObject.Unloaded += new RoutedEventHandler(AssociatedObject_Unloaded);
            AssociatedObject.Sorting += new DataGridSortingEventHandler(AssociatedObject_Sorting);            
        }

        void AssociatedObject_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (ItemComparer != null)
            {
                e.Handled = true;
                var col = e.Column;
                ListSortDirection dir = col.SortDirection == null || col.SortDirection == ListSortDirection.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending;
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    foreach (var c in AssociatedObject.Columns.Where(c => c.SortDirection != null))
                        c.SortDirection = null;
                SortItems(col, dir);
            }
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= new RoutedEventHandler(AssociatedObject_Loaded);
            //AssociatedObject.LoadSortDescr();
            ReSort();
        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Unloaded -= new RoutedEventHandler(AssociatedObject_Unloaded);
            AssociatedObject.SaveSortDescr();
        }

        private void ReSort()
        {
            foreach (var sd in AssociatedObject.Items.SortDescriptions)
            {
                var col = AssociatedObject.Columns.FirstOrDefault(c => c.CanUserSort && c.SortMemberPath == sd.PropertyName);
                if (col != null)
                    SortItems(col, sd.Direction);
            }
        }

        private void SortItems(DataGridColumn _col, ListSortDirection _dir)
        {                                                
            _col.SortDirection = _dir;
            var lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
            if (colprops == null)
            {
                colprops = lcv.ItemProperties.Join(AssociatedObject.Columns, i => i.Name, c => c.SortMemberPath, (i, c) => new { Item = i, Column = c }).ToDictionary(ic => ic.Column, ic => ic.Item);
                if (ItemComparer != null)
                    ItemComparer.ColumnProperties = colprops;
            }
            lcv.CustomSort = ItemComparer;
        }
    }
}