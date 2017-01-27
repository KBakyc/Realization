using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using CommonModule.Helpers;
using System.Windows.Data;
using System;
using System.Linq;
using DotNetHelper;

namespace CommonModule.Behaviours
{
    public class DataGridFixSelectionBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.LoadingRow += LoadingRow;
            AssociatedObject.UnloadingRow += UnloadingRow;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.LoadingRow -= LoadingRow;
            AssociatedObject.UnloadingRow -= UnloadingRow;
        }

        private void LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.PreviewMouseLeftButtonDown += RowMouseDownHandler;
        }


        private void UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.PreviewMouseLeftButtonDown -= RowMouseDownHandler;
        }

        private void RowMouseDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed
                && System.Windows.Input.Keyboard.PrimaryDevice.Modifiers == System.Windows.Input.ModifierKeys.Shift)
            {
                DataGridRow row = sender as DataGridRow; //GetVisualParentByType((System.Windows.FrameworkElement)e.OriginalSource, typeof(DataGridRow)) as DataGridRow;
                if (row != null && row.DataContext != null && row.DataContext is ISelectable)
                {
                    var rowItem = row.DataContext as ISelectable;
                    DataGrid dg = row.GetVisualParentOfType<DataGrid>();
                    if (dg != null && dg.ItemsSource != null && dg.SelectedItem != null)
                    {
                        if (dg.ItemsSource is System.Collections.Generic.IEnumerable<ISelectable>)
                        {
                            var dgSelItem = dg.SelectedItem as ISelectable;
                            var allItems = dg.ItemsSource as System.Collections.Generic.IEnumerable<ISelectable>;
                            var view = CollectionViewSource.GetDefaultView(allItems);
                            var itemsFromSel = view.OfType<ISelectable>().SkipWhile(i => i != dgSelItem && i != rowItem).Skip(1).TakeWhile(i => i != dgSelItem && i != rowItem).Where(i => !i.IsSelected).ToArray();
                            Array.ForEach(itemsFromSel, i => i.IsSelected = true);
                        }
                    }
                }
            }
        }        
    }
}