using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using CommonModule.Helpers;

namespace CommonModule.Behaviours
{
    public class DataGridSaveOrderingsBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();            
            AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.Unloaded += new RoutedEventHandler(AssociatedObject_Unloaded);
            //AssociatedObject.Sorting += new DataGridSortingEventHandler(AssociatedObject_Sorting);
            //AssociatedObject.Initialized += new System.EventHandler(AssociatedObject_Initialized);
        }

        //void AssociatedObject_Sorting(object sender, DataGridSortingEventArgs e)
        //{
        //    AssociatedObject.SaveSortDescr();
        //}

        //void AssociatedObject_Initialized(object sender, System.EventArgs e)
        //{
        //    AssociatedObject.Initialized -= new System.EventHandler(AssociatedObject_Initialized);
        //    AssociatedObject.LoadSortDescr();
        //}

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.LoadSortDescr();
        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {            
            AssociatedObject.Unloaded -= new RoutedEventHandler(AssociatedObject_Unloaded);
            AssociatedObject.SaveSortDescr();            
        }       

        //protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        //{
        //    base.OnItemsSourceChanged(oldValue, newValue);
        //    DataGridHelper.LoadSortDescr(this);
        //}

    }
}